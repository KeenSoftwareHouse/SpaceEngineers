using System;
using System.Diagnostics;
using System.Text;
using VRageMath;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Collections;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels.Planet;
using VRage.Game;
using VRage.Profiler;
using VRage.Voxels;

namespace Sandbox.Engine.Voxels
{
    public class MyPlanetShapeProvider
    {
        private const int MAX_UNCULLED_HISTORY = 10;

        // Catmull-Rom and transpose
        static Matrix CR;
        static Matrix CRT;
        static Matrix BInv;
        static Matrix BInvT;

        static float Tau;

        static MyPlanetShapeProvider()
        {
            SetTau(.5f);

            BInvT = new Matrix(1, 1, 1, 1, 0, 1 / 3f, 2 / 3f, 1, 0, 0, 1 / 3f, 1, 0, 0, 0, 1);
            BInv = Matrix.Transpose(BInvT);
        }

        public static void SetTau(float tau)
        {
            Tau = tau;

            CRT = new Matrix(0, -Tau, 2 * Tau, -Tau, 1, 0, Tau - 3, 2 - Tau, 0, Tau, 3 - 2 * Tau, Tau - 2, 0, 0, -Tau, Tau);
            CR = Matrix.Transpose(CRT);
        }

        public static float GetTau()
        {
            return Tau;
        }

        public static MyDebugHitCounter PruningStats = new MyDebugHitCounter();
        public static MyDebugHitCounter CacheStats = new MyDebugHitCounter();
        public static MyDebugHitCounter CullStats = new MyDebugHitCounter();

        private static FastResourceLock m_historyLock = new FastResourceLock();

        // List of unculled requests processed.
        public static MyQueue<MyVoxelDataRequest> UnculledRequestHistory = new MyQueue<MyVoxelDataRequest>(MAX_UNCULLED_HISTORY);

        public static MyConcurrentDictionary<int, MyConcurrentHashSet<Vector3I>> KnownLodSizes = new MyConcurrentDictionary<int, MyConcurrentHashSet<Vector3I>>();

        public static string FormatedUnculledHistory
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append('[');
                using (m_historyLock.AcquireExclusiveUsing())
                {
                    if (UnculledRequestHistory.Count > 0)
                        sb.Append(UnculledRequestHistory[0].ToStringShort());

                    for (int i = 1; i < UnculledRequestHistory.Count; i++)
                    {
                        sb.Append(", ");
                        sb.Append(UnculledRequestHistory[1].ToStringShort());
                    }
                }
                sb.Append(']');
                return sb.ToString();
            }
        }

        public static string GetKnownLodSizes()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var lod in KnownLodSizes)
            {
                sb.AppendFormat("{0}: ", lod.Key);

                using (var en = lod.Value.GetEnumerator())
                {

                    if (en.MoveNext()) sb.AppendFormat("{0}x{1}x{2}", en.Current.X, en.Current.Y, en.Current.Z);
                    while (en.MoveNext())
                    {
                        sb.AppendFormat(", {0}x{1}x{2}", en.Current.X, en.Current.Y, en.Current.Z);
                    }
                }

                sb.Append("\n");
            }

            return sb.ToString();
        }

        [Conditional("DEBUG")]
        private static void EnqueueHistory(MyVoxelDataRequest request)
        {
            using (m_historyLock.AcquireExclusiveUsing())
            {
                if (UnculledRequestHistory.Count >= MAX_UNCULLED_HISTORY)
                {
                    UnculledRequestHistory.Dequeue();
                }

                request.Target = null;
                UnculledRequestHistory.Enqueue(request);
            }

            MyConcurrentHashSet<Vector3I> sizes;
            if (!KnownLodSizes.TryGetValue(request.Lod, out sizes))
            {
                sizes = new MyConcurrentHashSet<Vector3I>();
                if (!KnownLodSizes.TryAdd(request.Lod, sizes))
                {
                    sizes = KnownLodSizes[request.Lod];
                }

            }

            sizes.Add(request.maxInLod - request.minInLod + Vector3I.One);
        }

        private int m_mapResolutionMinusOne = 0;
        private float m_radius;

        public float OuterRadius
        {
            get;
            private set;
        }

        public float InnerRadius
        {
            get;
            private set;
        }

        internal Vector3 Center()
        {
            return m_translation;
        }

        private string m_dataFileName;

        MyHeightCubemap m_heightmap;

        public bool Closed { get; private set; }

        // Detail stuffs
        private struct SurfaceDetailSampler
        {
            MyHeightDetailTexture m_detail;
            public float Factor;
            public float Size;
            public float Scale;

            float m_min;
            float m_max;

            float m_in;
            float m_out;

            float m_inRecip;
            float m_outRecip;

            float m_mid;

            public void Init(MyPlanetSurfaceDetail def, float faceSize)
            {
                m_detail = MyHeightMapLoadingSystem.Static.GetDetailMap(def.Texture);

                Size = def.Size;
                Factor = faceSize / Size;

                m_min = (float)Math.Cos(MathHelper.ToRadians(def.Slope.Max));
                m_max = (float)Math.Cos(MathHelper.ToRadians(def.Slope.Min));

                m_in = (float)Math.Cos(MathHelper.ToRadians(def.Slope.Max - def.Transition));
                m_out = (float)Math.Cos(MathHelper.ToRadians(def.Slope.Min + def.Transition));

                m_inRecip = 1f / (m_in - m_min);
                m_outRecip = 1f / (m_max - m_out);

                m_mid = (float)Math.Cos(MathHelper.ToRadians((def.Slope.Max + def.Slope.Min) / 2));

                Scale = def.Scale;
            }

            internal bool Matches(float angle)
            {
                return angle <= m_max && angle >= m_min;
            }

            internal float GetValue(float dtx, float dty, float angle)
            {
                if (m_detail == null) return 0;
                float factor = 1;
                if (angle > m_mid)
                {
                    factor = Math.Min(Math.Max(1 - (angle - m_out) * m_outRecip, 0f), 1f);
                }
                else
                {
                    factor = Math.Max(Math.Min((angle - m_in) * m_inRecip, 1f), 0);
                }

                return m_detail.GetValue(dtx, dty) * factor * Scale;
            }
        }

        SurfaceDetailSampler m_detail;

        private float m_detailSlopeRecip;
        private float m_detailFade;

        Vector3 m_translation;
        float m_maxHillHeight;
        float m_minHillHeight;
        float m_heightRatio;
        float m_heightRatioRecip;

        float m_detailScale;
        float m_pixelSize;
        float m_pixelSizeRecip;
        float m_pixelSizeRecip2;
        private double m_curvatureThresholdRecip;
        float m_pixelSize4;
        float m_voxelSize;
        float m_mapStepScale;
        float m_mapStepScaleSquare;

        private float m_mapHeightScale;

        public float MinHillHeight
        {
            get
            {
                return m_minHillHeight;
            }
        }

        public float MaxHillHeight
        {
            get
            {
                return m_maxHillHeight;
            }
        }

        public float Radius
        {
            get { return m_radius; }
        }

        public MyHeightCubemap Heightmap
        {
            get { return m_heightmap; }
        }

        public MyPlanetShapeProvider(Vector3 translation, float radius, MyPlanetGeneratorDefinition definition)
        {
            m_radius = radius;
            m_translation = translation;

            m_maxHillHeight = definition.HillParams.Max * m_radius;
            m_minHillHeight = definition.HillParams.Min * m_radius;

            InnerRadius = radius + m_minHillHeight;
            OuterRadius = radius + m_maxHillHeight;

            m_heightmap = new MyHeightCubemap(definition.FolderName, definition.Context);

            m_mapResolutionMinusOne = m_heightmap.Resolution - 1;

            m_heightRatio = m_maxHillHeight - m_minHillHeight;
            m_heightRatioRecip = 1f / m_heightRatio;

            float faceSize = (float)(radius * Math.PI * .5);

            m_pixelSize = faceSize / m_heightmap.Resolution;
            m_pixelSizeRecip = 1f / m_pixelSize;
            m_pixelSizeRecip2 = .5f / m_pixelSize;

            // Calculate maximum tolerable curvature deviation when approximating the surface by a line
            // We use this for LOD1 raycasts so the maximum deviation is 8m(half a LOD1 cell)

            // Find the angle theta that produces an arc whose secant approximation deviates from the circle at most 8 meters
            var theta = Math.Acos((radius - 1)/radius); // this produces theta/2 but we use that later anyways

            // Find the length of this secant segment.
            var threshold = Math.Sin(theta)*2*radius;

            // Store it's reciprocal because that's all we use
            m_curvatureThresholdRecip = 1d/threshold;

            m_pixelSize4 = m_pixelSize * 4;

            // Used for inflating query boxes
            m_voxelSize = (float)(2.0 / (radius * Math.PI));

            m_mapStepScale = m_pixelSize / m_heightRatio;
            m_mapStepScaleSquare = m_mapStepScale * m_mapStepScale;

            if (definition.Detail != null)
                m_detail.Init(definition.Detail, faceSize);

            Closed = false;
        }

        public void Close()
        {
            m_heightmap = null;
            Closed = true;
        }

        #region Height Computation

        #region Cache
        /**
         * Coefficient cache.
         * 
         * Bicubic sampling is computationally expensive because of the many floating point operations involved
         * (mostly matrix multiplications which are 64 muls and 48 adds). To optimize that we cache the
         * coefficients matrix (Gz) since it is re-used multiple times with different steps.
         * 
         * 
         * Additionally we also compute bounds for each pixel based on the conversion from CR to Bézier control
         * points (using the property that Bz curves lie within the convex hull of the controll points).
         * 
         * Further we have performance tracking for this cache (via ::CacheStats).
         */
        private struct Cache
        {
            private const int CACHE_BITS = 4;
            private const int CACHE_MASK = (1 << CACHE_BITS) - 1;
            private const int CACHE_SIZE = (1 << (CACHE_BITS << 1));

            public struct Cell
            {
                public Matrix Gz;
                public float Min, Max;
                public Vector3I Coord;
            }

            public Cell[] Cells;

            public string Name;

            public int CellCoord(ref Vector3I coord)
            {
                return ((coord.Y & CACHE_MASK) << CACHE_BITS) | (coord.X & CACHE_MASK);
            }

            internal void Clean()
            {
                if (Cells == null)
                    Cells = new Cell[CACHE_SIZE];
                for (int i = 0; i < CACHE_SIZE; ++i)
                {
                    Cells[i].Coord = new Vector3I(-1);
                }
            }
        }

        [ThreadStatic]
        private static Cache m_cache;

        /**
         * The cache does not store in it's cells which map does one cell belong to, so before using it we
         * have to invalidate all entries.
         * 
         * Maybe at some point we can come up with a more elegant solution for multiple planets.
         */
        public void PrepareCache()
        {
            if (m_heightmap.Name != m_cache.Name)
            {
                m_cache.Name = m_heightmap.Name;
                m_cache.Clean();
            }
        }
        #endregion

        private const bool ENABLE_BEZIER_CULL = true;

        #region Sampling

        // This is here so we don't call constructors all the time.
        [ThreadStatic]
        static Matrix s_Cz;

        /**
         * Calculate cell for the coefficient cache.
         */
        private unsafe void CalculateCacheCell(MyHeightmapFace map, Cache.Cell* cell, bool compouteBounds = false)
        {
            int sx = cell->Coord.X;
            int sy = cell->Coord.Y;

            fixed (float* row = &s_Cz.M11)
            {
                int linear = map.GetRowStart(sy - 1) + sx - 1;
                fixed (ushort* texture = map.Data)
                {
                    map.Get4Row(linear, row, texture); linear += map.RowStride;
                    map.Get4Row(linear, row + 4, texture); linear += map.RowStride;
                    map.Get4Row(linear, row + 8, texture); linear += map.RowStride;
                    map.Get4Row(linear, row + 12, texture);

                    Matrix.Multiply(ref CR, ref s_Cz, out cell->Gz);
                    Matrix.Multiply(ref cell->Gz, ref CRT, out cell->Gz);

                    if (compouteBounds)
                    {
                        float Min = float.PositiveInfinity;
                        float Max = float.NegativeInfinity;

                        Matrix heights;
                        Matrix.Multiply(ref BInv, ref cell->Gz, out heights);
                        Matrix.Multiply(ref heights, ref BInvT, out heights);

                        /* Will do good old pointer magic */
                        float* values = &heights.M11;
                        for (int i = 0; i < 16; ++i)
                        {
                            if (Max < values[i]) Max = values[i];
                            if (Min > values[i]) Min = values[i];
                        }

                        cell->Max = Max;
                        cell->Min = Min;
                    }
                    else
                    {
                        cell->Max = 1;
                        cell->Min = 0;
                    }
                }
            }
        }

        /**
         * Catmull-Rom Patch:
         *
         * Given a matrix of points C = [(a0, a1, a2, a3), (a4, a5, a6, a7), (a8, a9, a10, a11), (a12, a13, a14, a15)]
         * where each a_i is a controll point with coordinates (x,y,z). The patch is then parametrized by steps s and t.
         *
         * We can divide the operation into per dimension operations using the coordinate matrices Cx, Cy, Cz.
         *
         * For any given s and t the position on the curve is given by S * CR * C * CRt * T, where CR is the catmull-rom coefficient matrix,
         * CRt is the transpose of the coefficient matrix and S = (1, s, s^2, s^3), T = (1, t, t^2, t^3).
         * 
         * Frequently the matrix CR * C * CRT is called G, and it's submatrices Gx, Gy, Gz.
         *
         * When the controll points are in a regular grid we can map s and t to x and y.
         *  Then we can calculate the height using only Gz and the normal using Gz and the scale of the grid.
         * 
         * Notice that the difference between this method and Bézier or B-Spline is simply the coefficient matrix.
         * 
         * Also the CR matrix is parametrizable, we use the centripetal form (a = .5). This form provides the best smoothing.
         * 
         * With this explanation in mind I have optimized the code heavily to remove redundant or unnecessary multiplications.
         *
         */
        private float SampleHeightBicubic(float s, float t, ref Matrix Gz, out Vector3 Normal)
        {
            float value;

            float s2 = s * s;
            float s3 = s2 * s;

            float t2 = t * t;
            float t3 = t2 * t;

            // these intermediate results are re-used
            float pr1 = (Gz.M12 + Gz.M22 * t + Gz.M32 * t2 + Gz.M42 * t3);
            float pr2 = (Gz.M13 + Gz.M23 * t + Gz.M33 * t2 + Gz.M43 * t3);
            float pr3 = (Gz.M14 + Gz.M24 * t + Gz.M34 * t2 + Gz.M44 * t3);

            value = (Gz.M11 + Gz.M21 * t + Gz.M31 * t2 + Gz.M41 * t3)
                    + s * pr1
                    + s2 * pr2
                    + s3 * pr3;

            float zx = pr1
                       + 2 * s * pr2
                       + 3 * s2 * pr3;

            float zy = (Gz.M21 + Gz.M22 * s + Gz.M23 * s2 + Gz.M24 * s3)
                       + 2 * t * (Gz.M31 + Gz.M32 * s + Gz.M33 * s2 + Gz.M34 * s3)
                       + 3 * t2 * (Gz.M41 + Gz.M42 * s + Gz.M43 * s2 + Gz.M44 * s3);

            Normal = new Vector3(m_mapStepScale * zx, m_mapStepScale * zy, m_mapStepScaleSquare);
            Normal.Normalize();

            return value;
        }

        private float SampleHeightBilinear(MyHeightmapFace map, float lodSize, float s, float t, int sx, int sy, out Vector3 Normal)
        {
            float value;
            float lodMapSize = lodSize * m_pixelSizeRecip2;

            // Bilinear sampling
            float fx = 1 - s, fy = 1 - t;

            int sx1 = Math.Min(sx + (int)Math.Ceiling(lodMapSize), m_heightmap.Resolution);
            int sy1 = Math.Min(sy + (int)Math.Ceiling(lodMapSize), m_heightmap.Resolution);

            float h00 = map.GetValuef(sx, sy);
            float h10 = map.GetValuef(sx1, sy);
            float h01 = map.GetValuef(sx, sy1);
            float h11 = map.GetValuef(sx1, sy1);

            value = h00 * fx * fy;
            value += h10 * s * fy;
            value += h01 * fx * t;
            value += h11 * s * t;

            float zx = (h10 - h00) * fy + (h11 - h01) * t;
            float zy = (h01 - h00) * fx + (h11 - h10) * s;

            Normal = new Vector3(m_mapStepScale * zx, m_mapStepScale * zy, m_mapStepScaleSquare);
            Normal.Normalize();

            return value;
        }

        #endregion

        #region Get Value Methods

        private const bool ForceBilinear = false;

        internal float SignedDistanceLocalCacheless(Vector3 position)
        {
            float distance = position.Length();

            if (distance > .1 && distance >= InnerRadius - 1)
            {
                if (distance > OuterRadius + 1) return float.PositiveInfinity;

                float signedDistance = distance - m_radius;
                int face;
                Vector2 tex;
                MyCubemapHelpers.CalculateSampleTexcoord(ref position, out face, out tex);

                Vector3 normal;

                float value = GetValueForPositionCacheless(face, ref tex, out normal);

                if (m_detail.Matches(normal.Z))
                {
                    float dtx = tex.X * m_detail.Factor;
                    float dty = tex.Y * m_detail.Factor;

                    dtx -= (float)Math.Floor(dtx);
                    dty -= (float)Math.Floor(dty);

                    value += m_detail.GetValue(dtx, dty, normal.Z);
                }

                return (signedDistance - value) * normal.Z;
            }
            return -1;
        }


        /**
         * Gets the elevation for a position.
         *
         * Does not use the cache, does not consider details.
         */
        public unsafe float GetValueForPositionCacheless(int face, ref Vector2 texcoord, out Vector3 localNormal)
        {
            if (m_heightmap == null)
            {
                localNormal = Vector3.Zero;
                //hotfix, daniel please fix this correctly
                return 0.0f;
            }

            Cache.Cell tmp_cell;
            Cache.Cell* cell = &tmp_cell;

            Vector2 coords = texcoord * m_mapResolutionMinusOne;

            cell->Coord = new Vector3I((int)coords.X, (int)coords.Y, face);

            CalculateCacheCell(m_heightmap.Faces[face], cell);

            float value = SampleHeightBicubic(coords.X - (float)Math.Floor(coords.X), coords.Y - (float)Math.Floor(coords.Y), ref cell->Gz, out localNormal);

            return value * m_heightRatio + m_minHillHeight;
        }

        /**
         * Use a heightmap sample to produce signed distance.
         */
        public float SignedDistanceWithSample(float lodVoxelSize, float distance, float value)
        {
            return distance - m_radius - value;
        }

        /**
         * Project a position in space to the surface of the heightmap.
         */
        public void ProjectToSurface(Vector3 localPos, out Vector3 surface)
        {
            float length = localPos.Length();
            if (length.IsZero())
            {
                Debug.Fail("Cannot project to surface from the center of the planet!");
                surface = localPos;
                return;
            }
            Vector3 dir = localPos / length;

            int face;
            Vector2 tex;
            MyCubemapHelpers.CalculateSampleTexcoord(ref localPos, out face, out tex);

            Vector3 norm;
            float value = GetValueForPositionCacheless(face, ref tex, out norm);

            value += Radius;

            surface = value * dir;
        }

        /**
         * Get the height for a position using the coefficient cache. Calculated position is for LOD0.
         */
        public double GetDistanceToSurfaceWithCache(Vector3 localPos)
        {
            float length = localPos.Length();
            if (length.IsZero())
            {
                Debug.Fail("Cannot project to surface from the center of the planet!");
                return 0;
            }

            int face;
            Vector2 tex;
            MyCubemapHelpers.CalculateSampleTexcoord(ref localPos, out face, out tex);

            Vector3 norm;
            float value = GetValueForPositionWithCache(face, ref tex, out norm);

            value += Radius;

            return length - value;
        }

        /**
         * Get the height for a position using the coefficient cache. Calculated position is for LOD0.
         */
        public double GetDistanceToSurfaceCacheless(Vector3 localPos)
        {
            float length = localPos.Length();
            if (length.IsZero())
            {
                Debug.Fail("Cannot project to surface from the center of the planet!");
                return 0;
            }

            if (!length.IsValid())
            {
                Debug.Fail("Cannot project from such number!");
                return 0;
            }

            int face;
            Vector2 tex;
            MyCubemapHelpers.CalculateSampleTexcoord(ref localPos, out face, out tex);

            Vector3 norm;
            float value = GetValueForPositionCacheless(face, ref tex, out norm);

            value += Radius;

            return length - value;
        }

        /**
         * Get the height for a position using the coefficient cache. Calculated position is for LOD0.
         */
        public unsafe float GetValueForPositionWithCache(int face, ref Vector2 texcoord, out Vector3 localNormal)
        {
            Vector2 coords = texcoord * m_mapResolutionMinusOne;

            int sx = (int)coords.X;
            int sy = (int)coords.Y;

            Vector3I cellCoord = new Vector3I(sx, sy, face);

            fixed (Cache.Cell* cell = &m_cache.Cells[m_cache.CellCoord(ref cellCoord)])
            {
                if (cell->Coord != cellCoord)
                {
                    cell->Coord = cellCoord;
                    CalculateCacheCell(m_heightmap.Faces[face], cell);
                }

                float value = SampleHeightBicubic(coords.X - (float)Math.Floor(coords.X), coords.Y - (float)Math.Floor(coords.Y), ref cell->Gz, out localNormal);

                return value * m_heightRatio + m_minHillHeight;
            }
        }

        internal unsafe float GetValueForPositionInternal(int face, ref Vector2 texcoord, float lodSize, float distance, out Vector3 Normal)
        {
            float value;

            Vector2 coords = texcoord * m_mapResolutionMinusOne;

            float s = coords.X - (float)Math.Floor(coords.X);
            float t = coords.Y - (float)Math.Floor(coords.Y);

            int sx = (int)coords.X;
            int sy = (int)coords.Y;

            if (lodSize < m_pixelSize && !ForceBilinear)
            {
                Vector3I cellCoord = new Vector3I(sx, sy, face);

                fixed (Cache.Cell* cell = &m_cache.Cells[m_cache.CellCoord(ref cellCoord)])
                {
                    if (cell->Coord != cellCoord)
                    {
                        cell->Coord = cellCoord;
                        CalculateCacheCell(m_heightmap.Faces[face], cell, ENABLE_BEZIER_CULL);
                    }

                    if (ENABLE_BEZIER_CULL)
                    {
                        float rate = (distance - InnerRadius) * m_heightRatioRecip;
                        float lodHeight = lodSize * m_heightRatioRecip;

                        if (rate > cell->Max + lodHeight)
                        {
                            Normal = Vector3.Backward;
                            return float.NegativeInfinity;
                        }
                        if (rate < cell->Min - lodHeight)
                        {
                            Normal = Vector3.Backward;
                            return float.PositiveInfinity;
                        }
                    }

                    value = SampleHeightBicubic(coords.X - (float)Math.Floor(coords.X), coords.Y - (float)Math.Floor(coords.Y), ref cell->Gz, out Normal);
                }
            }
            else
            {
                value = SampleHeightBilinear(m_heightmap.Faces[face], lodSize, s, t, sx, sy, out Normal);
            }
            return value * m_heightRatio + m_minHillHeight;
        }

        internal float SignedDistanceLocal(Vector3 position, float lodVoxelSize)
        {
            float distance = position.Length();

            if (distance > .1 && distance >= InnerRadius - lodVoxelSize)
            {
                if (distance > OuterRadius + lodVoxelSize) return float.PositiveInfinity;

                float signedDistance = distance - m_radius;
                int face;
                Vector2 tex;
                MyCubemapHelpers.CalculateSampleTexcoord(ref position, out face, out tex);

                Vector3 Norm;

                float value = GetValueForPositionInternal(face, ref tex, lodVoxelSize, distance, out Norm);

                if (m_detail.Matches(Norm.Z))
                {
                    float dtx = tex.X * m_detail.Factor;
                    float dty = tex.Y * m_detail.Factor;

                    dtx -= (float)Math.Floor(dtx);
                    dty -= (float)Math.Floor(dty);

                    value += m_detail.GetValue(dtx, dty, Norm.Z);
                }

                return (signedDistance - value) * Norm.Z;
            }
            return -lodVoxelSize;
        }

        #endregion

        #endregion

        public float AltitudeToRatio(float altitude)
        {
            return (altitude - m_minHillHeight) * m_heightRatioRecip;
        }

        internal float DistanceToRatio(float distance)
        {
            return (distance - InnerRadius) * m_heightRatioRecip;
        }

        #region Storage Queries

        public ContainmentType IntersectBoundingBox(ref BoundingBox box, float lodLevel)
        {
            box.Inflate(1f);

            bool intersects;
            BoundingSphere sphere = new BoundingSphere(
                    Vector3.Zero,
                    OuterRadius + lodLevel);

            sphere.Intersects(ref box, out intersects);
            if (!intersects)
            {
                return ContainmentType.Disjoint;
            }

            sphere.Radius = InnerRadius - lodLevel;

            ContainmentType ct;
            sphere.Contains(ref box, out ct);
            if (ct == ContainmentType.Contains)
            {
                return ContainmentType.Contains;
            }

            return IntersectBoundingBoxInternal(ref box, lodLevel);
        }

        private unsafe ContainmentType IntersectBoundingBoxCornerCase(ref BoundingBox box, uint faces, Vector3* vertices, float minHeight, float maxHeight)
        {
            BoundingBox query = new BoundingBox(new Vector3(float.PositiveInfinity, float.PositiveInfinity, minHeight), new Vector3(float.NegativeInfinity, float.NegativeInfinity, maxHeight));

            query.Min.Z = ((query.Min.Z - m_radius - m_detailScale) - m_minHillHeight) * m_heightRatioRecip;
            query.Max.Z = ((query.Max.Z - m_radius) - m_minHillHeight) * m_heightRatioRecip;

            var invalid = (ContainmentType)(-1);

            ContainmentType ct = invalid;

            for (int i = 0; i <= 5; ++i)
            {
                if ((faces & (1 << i)) == 0) continue;

                query.Min.X = float.PositiveInfinity;
                query.Min.Y = float.PositiveInfinity;
                query.Max.X = float.NegativeInfinity;
                query.Max.Y = float.NegativeInfinity;

                for (int j = 0; j < 8; ++j)
                {
                    Vector2 tex;
                    MyCubemapHelpers.TexcoordCalculators[i](ref vertices[j], out tex);

                    if (tex.X < query.Min.X) query.Min.X = tex.X;
                    if (tex.X > query.Max.X) query.Max.X = tex.X;
                    if (tex.Y < query.Min.Y) query.Min.Y = tex.Y;
                    if (tex.Y > query.Max.Y) query.Max.Y = tex.Y;
                }

                var cont = m_heightmap.Faces[i].QueryHeight(ref query);
                if (cont != ct)
                {
                    if (ct == invalid) ct = cont;
                    else return ContainmentType.Intersects;
                }
            }

            return ct;
        }

        protected unsafe ContainmentType IntersectBoundingBoxInternal(ref BoundingBox box, float lodLevel)
        {
            int firstFace = -1;
            uint faces = 0;
            bool complicated = false;

            float minHeight;
            float maxHeight;

            Vector3* corners = stackalloc Vector3[8];

            box.GetCornersUnsafe(corners);

            for (int i = 0; i < 8; ++i)
            {
                int face;
                MyCubemapHelpers.GetCubeFace(ref corners[i], out face);

                if (firstFace == -1)
                    firstFace = face;
                else if (firstFace != face) complicated = true;

                faces |= (uint)(1 << face);
            }

            if (Vector3.Zero.IsInsideInclusive(ref box.Min, ref box.Max))
            {
                minHeight = 0;
            }
            else
            {
                var clamp = Vector3.Clamp(Vector3.Zero, box.Min, box.Max);
                minHeight = clamp.Length();
            }

            { // Calculate furthest point in BB
                Vector3 end;
                Vector3 c = box.Center;

                if (c.X < 0)
                    end.X = box.Min.X;
                else
                    end.X = box.Max.X;

                if (c.Y < 0)
                    end.Y = box.Min.Y;
                else
                    end.Y = box.Max.Y;

                if (c.Z < 0)
                    end.Z = box.Min.Z;
                else
                    end.Z = box.Max.Z;

                maxHeight = end.Length();
            }

            if (complicated)
                return IntersectBoundingBoxCornerCase(ref box, faces, corners, minHeight, maxHeight);

            BoundingBox query = new BoundingBox(new Vector3(float.PositiveInfinity, float.PositiveInfinity, minHeight), new Vector3(float.NegativeInfinity, float.NegativeInfinity, maxHeight));

            for (int i = 0; i < 8; ++i)
            {
                Vector2 tex;
                MyCubemapHelpers.CalculateTexcoordForFace(ref corners[i], firstFace, out tex);

                if (tex.X < query.Min.X) query.Min.X = tex.X;
                if (tex.X > query.Max.X) query.Max.X = tex.X;
                if (tex.Y < query.Min.Y) query.Min.Y = tex.Y;
                if (tex.Y > query.Max.Y) query.Max.Y = tex.Y;
            }

            query.Min.Z = ((query.Min.Z - m_radius - m_detailScale) - m_minHillHeight) * m_heightRatioRecip;
            query.Max.Z = ((query.Max.Z - m_radius) - m_minHillHeight) * m_heightRatioRecip;

            return m_heightmap.Faces[firstFace].QueryHeight(ref query);
        }

        private unsafe bool IntersectLineCornerCase(ref LineD line, uint faces, out double startOffset, out double endOffset)
        {
            startOffset = 1;
            endOffset = 0;
            return true;
        }

        public bool IntersectLineFace(ref LineD ll, int face, out double startOffset, out double endOffset)
        {
            Vector2 start, end;
            Vector3 from, to;

            from = ll.From;
            to = ll.To;

            MyCubemapHelpers.CalculateTexcoordForFace(ref from, face, out start);
            MyCubemapHelpers.CalculateTexcoordForFace(ref to, face, out end);

            int steps = (int)Math.Ceiling((end - start).Length() * m_heightmap.Resolution);

            double stepsR = 1d / steps;

            for (int i = 0; i < steps; i++)
            {
                from = ll.From + ll.Direction * ll.Length * i * stepsR;
                to = ll.From + ll.Direction * ll.Length * (i + 1) * stepsR;

                var flen = from.Length();
                var tolen = to.Length();

                MyCubemapHelpers.CalculateTexcoordForFace(ref from, face, out start);
                MyCubemapHelpers.CalculateTexcoordForFace(ref to, face, out end);

                from.X = start.X;
                from.Y = start.Y;
                from.Z = (flen - m_radius - m_minHillHeight) * m_heightRatioRecip;

                to.X = end.X;
                to.Y = end.Y;
                to.Z = (tolen - m_radius - m_minHillHeight) * m_heightRatioRecip;

                float newStart, newEnd;
                if (m_heightmap[face].QueryLine(ref from, ref to, out newStart, out newEnd))
                {
                    startOffset = Math.Max((i + newStart) * stepsR, 0);
                    endOffset = 1.0;
                    return true;
                }
            }

            startOffset = 0;
            endOffset = 1;
            return false;
        }

        public unsafe bool IntersectLine(ref LineD ll, out double startOffset, out double endOffset)
        {
            var box = new BoundingBox(ll.From, ll.From);
            box.Include(ll.To);

            int firstFace = -1;
            uint faces = 0;
            bool complicated = false;

            Vector3* corners = stackalloc Vector3[8];

            box.GetCornersUnsafe(corners);

            for (int i = 0; i < 8; ++i)
            {
                int face;
                MyCubemapHelpers.GetCubeFace(ref corners[i], out face);

                if (firstFace == -1)
                    firstFace = face;
                else if (firstFace != face) complicated = true;

                faces |= (uint)(1 << face);
            }

            if (complicated)
                return IntersectLineCornerCase(ref ll, faces, out startOffset, out endOffset);

            // Later on we will have split the line per sextant already.

            // From here on I calculate how many times to split the line to account for the surface curvature.
            // 

            return IntersectLineFace(ref ll, firstFace, out startOffset, out endOffset);
        }

        #endregion

        #region Read Range

        internal void ReadContentRange(ref MyVoxelDataRequest req)
        {
            if (Closed) return;

            float lodVoxelSize = (1 << req.Lod) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;

            Vector3I min = req.minInLod;
            Vector3I max = req.maxInLod;

            ProfilerShort.Begin("Distance field computation");
            try
            {
                Vector3I v = min;
                Vector3 localPos = v * lodVoxelSize - m_translation;
                Vector3 localPosStart = localPos;

                BoundingBox request = new BoundingBox(localPos, localPos + (max - min) * lodVoxelSize);
                request.Inflate(lodVoxelSize);

                MyVoxelRequestFlags flags = 0;

                ContainmentType cont = ContainmentType.Intersects;

                bool intersects;

                if (!req.Flags.HasFlags(MyVoxelRequestFlags.DoNotCheck))
                {
                    BoundingSphere sphere = new BoundingSphere(
                    Vector3.Zero,
                    OuterRadius + lodVoxelSize);

                    sphere.Intersects(ref request, out intersects);
                    if (!intersects)
                    {
                        cont = ContainmentType.Disjoint;
                        goto end;
                    }

                    sphere.Radius = InnerRadius - lodVoxelSize;

                    ContainmentType ct;
                    sphere.Contains(ref request, out ct);
                    if (ct == ContainmentType.Contains)
                    {
                        cont = ct;
                        goto end;
                    }

                    cont = IntersectBoundingBoxInternal(ref request, lodVoxelSize);
                    if (cont != ContainmentType.Intersects)
                        goto end;
                }


                bool hit = false;

                // store request history
                EnqueueHistory(req);

                // Setup cache for current map;
                PrepareCache();

                var writeOffsetLoc = req.Offset - min;
                for (v.Z = min.Z; v.Z <= max.Z; ++v.Z)
                {
                    for (v.Y = min.Y; v.Y <= max.Y; ++v.Y)
                    {
                        v.X = min.X;
                        var write2 = v + writeOffsetLoc;
                        var write = req.Target.ComputeLinear(ref write2);
                        for (; v.X <= max.X; ++v.X)
                        {
                            float signedDist = SignedDistanceLocal(localPos, lodVoxelSize) / lodVoxelSize;

                            var fillRatio = MathHelper.Clamp(-signedDist, -1f, 1f) * 0.5f + 0.5f;
                            byte content = (byte)(fillRatio * MyVoxelConstants.VOXEL_CONTENT_FULL);

                            if (content != 0)
                            {
                                hit = true;
                            }
                            req.Target.Content(write, content);
                            write += req.Target.StepLinear;
                            localPos.X += lodVoxelSize;
                        }
                        localPos.Y += lodVoxelSize;
                        localPos.X = localPosStart.X;
                    }
                    localPos.Z += lodVoxelSize;
                    localPos.Y = localPosStart.Y;
                }

                if (!hit)
                {
                    PruningStats.Miss();
                }
                else
                {
                    PruningStats.Hit();
                }
                CullStats.Miss();
                return;
            end: ;
                CullStats.Hit();

                if (cont == ContainmentType.Disjoint)
                {
                    if (req.RequestFlags.HasFlags(MyVoxelRequestFlags.ContentChecked))
                    {
                        flags |= MyVoxelRequestFlags.EmptyContent | MyVoxelRequestFlags.ContentCheckedDeep | MyVoxelRequestFlags.ContentChecked;
                    }
                    else
                    {
                        req.Target.BlockFillContent(req.Offset, req.Offset + max - min, MyVoxelConstants.VOXEL_CONTENT_EMPTY);
                    }
                }
                else if (cont == ContainmentType.Contains)
                {
                    if (req.RequestFlags.HasFlags(MyVoxelRequestFlags.ContentChecked))
                    {
                        flags |= MyVoxelRequestFlags.FullContent | MyVoxelRequestFlags.ContentCheckedDeep | MyVoxelRequestFlags.ContentChecked;
                    }
                    else
                    {
                        req.Target.BlockFillContent(req.Offset, req.Offset + max - min, MyVoxelConstants.VOXEL_CONTENT_FULL);
                    }
                }
                req.Flags = flags;
                PruningStats.Hit();
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        #endregion

        /// <summary>
        /// Get the minimum and maximum ranges of terrain height in the area determined by the provided points.
        /// </summary>
        /// <param name="localPoints">List of points to query</param>
        /// <param name="pointCount">Number of points</param>
        /// <param name="minHeight">The calculated minimum possible height of terrain</param>
        /// <param name="maxHeight">The calculated maximum possible height of terrain</param>
        public unsafe void GetBounds(Vector3* localPoints, int pointCount, out float minHeight, out float maxHeight)
        {
            int firstFace = -1;

            for (int i = 0; i < pointCount; ++i)
            {
                int face;
                MyCubemapHelpers.GetCubeFace(ref localPoints[i], out face);

                if (firstFace == -1)
                    firstFace = face;
            }

            BoundingBox query = new BoundingBox(new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0), new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0));

            for (int i = 0; i < pointCount; ++i)
            {
                Vector2 tex;
                MyCubemapHelpers.CalculateTexcoordForFace(ref localPoints[i], firstFace, out tex);

                if (tex.X < query.Min.X) query.Min.X = tex.X;
                if (tex.X > query.Max.X) query.Max.X = tex.X;
                if (tex.Y < query.Min.Y) query.Min.Y = tex.Y;
                if (tex.Y > query.Max.Y) query.Max.Y = tex.Y;
            }

            m_heightmap.Faces[firstFace].GetBounds(ref query);

            minHeight = query.Min.Z * m_heightRatio + InnerRadius;
            maxHeight = query.Max.Z * m_heightRatio + InnerRadius;
        }

        /**
         * Takes the x and y bounds of the box and queries the heightmap for the minimum and maximum height in the box.
         *
         * The values are returned in the query box itself as the Z bounds.
         *
         * This only works if all vertices of the box are in the same face.
         */
        public unsafe void GetBounds(ref BoundingBox box)
        {
            Vector3* corners = stackalloc Vector3[8];

            box.GetCornersUnsafe(corners);

            GetBounds(corners, 8, out box.Min.Z, out box.Max.Z);
        }
    }
}

using Sandbox.Game;
using System;
using System.Diagnostics;
using VRage.Utils;
using VRageMath;
using VRage.Native;
using System.Collections.Generic;
using VRage.Profiler;
using VRage.Voxels;

//  This class is used for precalculation of voxels into triangles and vertex buffers
//  It is not static and not thread too. But it may be called from thread.
//  This class doesn't know if it is called from multiple threads or just from main thread.

namespace Sandbox.Engine.Voxels
{
    class MyMarchingCubesMesher : IMyIsoMesher
    {
        //  Here I store vertex indices for final triangles
        class MyEdgeVertex
        {
            public ushort VertexIndex;             //  If this vertex is in the list, this is its m_notCompressedIndex
            public int CalcCounter;             //  For knowing if edge vertex was calculated in this PrecalcImmediatelly() or one of previous
        }

        //  Here I store data for edges on marching cube
        class MyEdge
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float Ambient;
            public byte Material;
        }

        //  Temporary voxel values, serve as cache between precalc and voxel map - so we don't have to always access voxel maps but can look here
        class MyTemporaryVoxel
        {
            public int IdxInCache;
            public Vector3 Position;
            public Vector3 Normal;
            public float Ambient;

            public int Normal_CalcCounter;
            public int Ambient_CalcCounter;
        }

        //  Array of vectors. Used for temporary storing interpolated vertexes on cube edges.
        const int POLYCUBE_EDGES = 12;
        readonly MyEdge[] m_edges = new MyEdge[POLYCUBE_EDGES];

        //  Array of edges in the cell
        const int CELL_EDGES_SIZE = 10 * (MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS + 1);
        MyEdgeVertex[][][][] m_edgeVertex;
        int m_edgeVertexCalcCounter;

        //  Here we store calculated vertexes and vertex info
        readonly MyVoxelVertex[] m_resultVertices = new MyVoxelVertex[MyVoxelConstants.GEOMETRY_CELL_MAX_TRIANGLES_COUNT * 3];
        int m_resultVerticesCounter;

        // Index buffer
        readonly MyVoxelTriangle[] m_resultTriangles = new MyVoxelTriangle[MyVoxelConstants.GEOMETRY_CELL_MAX_TRIANGLES_COUNT * 10];
        int m_resultTrianglesCounter;

        //  This variables are set every time PrecalcImmediatelly() is called
        Vector3I m_polygCubes;
        Vector3I m_voxelStart;

        //  Here we store voxel content values from cell we are precalculating. So we don't need to call VoxelMap.GetVoxelContent during precalculation.
        //  Items in array are nullable because we want to know, if we already retrieved/calculated that voxel/normal during current precalculation.
        const int COPY_TABLE_SIZE = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS + 3;
        int m_temporaryVoxelsCounter = 0;
        readonly MyTemporaryVoxel[] m_temporaryVoxels = new MyTemporaryVoxel[10 * COPY_TABLE_SIZE * COPY_TABLE_SIZE * COPY_TABLE_SIZE];
        const int m_sX = 1;
        const int m_sY = COPY_TABLE_SIZE;
        const int m_sZ = COPY_TABLE_SIZE * COPY_TABLE_SIZE;

        private readonly MyStorageData m_cache = new MyStorageData();

        //private MyIsoMesherArgs m_args;

        Vector3I m_sizeMinusOne;
        float m_voxelSizeInMeters;
        Vector3 m_originPosition;

        public MyMarchingCubesMesher()
        {
            //  Cube Edges
            for (int i = 0; i < m_edges.Length; i++)
            {
                m_edges[i] = new MyEdge();
            }

            //  Temporary voxel values, serve as cache between precalc and voxel map - so we don't have to always access voxel maps but can look here
            for (int i = 0; i < m_temporaryVoxels.Length; i++)
            {
                m_temporaryVoxels[i] = new MyTemporaryVoxel();
            }

            //  Array of edges in the cell
            m_edgeVertexCalcCounter = 0;
            m_edgeVertex = new MyEdgeVertex[CELL_EDGES_SIZE][][][];
            for (int x = 0; x < CELL_EDGES_SIZE; x++)
            {
                m_edgeVertex[x] = new MyEdgeVertex[CELL_EDGES_SIZE][][];
                for (int y = 0; y < CELL_EDGES_SIZE; y++)
                {
                    m_edgeVertex[x][y] = new MyEdgeVertex[CELL_EDGES_SIZE][];
                    for (int z = 0; z < CELL_EDGES_SIZE; z++)
                    {
                        m_edgeVertex[x][y][z] = new MyEdgeVertex[MyMarchingCubesConstants.CELL_EDGE_COUNT];
                        for (int w = 0; w < MyMarchingCubesConstants.CELL_EDGE_COUNT; w++)
                        {
                            m_edgeVertex[x][y][z][w] = new MyEdgeVertex();
                            m_edgeVertex[x][y][z][w].CalcCounter = 0;
                        }
                    }
                }
            }
        }

        void CalcPolygCubeSize(int lodIdx, Vector3I storageSize)
        {
            var size = storageSize;
            size >>= lodIdx;
            const int SIZE_IN_VOXELS = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            m_polygCubes.X = (m_voxelStart.X + SIZE_IN_VOXELS) >= size.X ? SIZE_IN_VOXELS : SIZE_IN_VOXELS + 1;
            m_polygCubes.Y = (m_voxelStart.Y + SIZE_IN_VOXELS) >= size.Y ? SIZE_IN_VOXELS : SIZE_IN_VOXELS + 1;
            m_polygCubes.Z = (m_voxelStart.Z + SIZE_IN_VOXELS) >= size.Z ? SIZE_IN_VOXELS : SIZE_IN_VOXELS + 1;
        }

        //  IMPORTANT: This method is called only from GetVoxelNormal(). Reason is that border voxels aren't copied during CopyVoxelContents, so we can check it now.
        byte GetVoxelContent(int x, int y, int z)
        {
            return m_cache.Content(x, y, z);
        }

        //  Get normal from lookup table or calc it
        void GetVoxelNormal(MyTemporaryVoxel temporaryVoxel, ref Vector3I coord, ref Vector3I voxelCoord, MyTemporaryVoxel centerVoxel)
        {
            if (temporaryVoxel.Normal_CalcCounter != m_temporaryVoxelsCounter)
            {
                Vector3I sampleMin = coord - 1;
                Vector3I sampleMax = coord + 1;

                var cache = m_cache;
                var clampMax = cache.Size3D - 1;
                Vector3I.Max(ref sampleMin, ref Vector3I.Zero, out sampleMin);
                Vector3I.Min(ref sampleMax, ref clampMax, out sampleMax);

                Vector3 normal = new Vector3(
                        (cache.Content(sampleMin.X, coord.Y, coord.Z) - cache.Content(sampleMax.X, coord.Y, coord.Z)) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT,
                        (cache.Content(coord.X, sampleMin.Y, coord.Z) - cache.Content(coord.X, sampleMax.Y, coord.Z)) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT,
                        (cache.Content(coord.X, coord.Y, sampleMin.Z) - cache.Content(coord.X, coord.Y, sampleMax.Z)) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT);

                if (normal.LengthSquared() <= 0.000001f)
                {
                    //  If voxels surounding voxel for which we want to get normal vector are of the same value, their subtracting leads to zero vector and that can't be used. So following line is hack.
                    temporaryVoxel.Normal = centerVoxel.Normal;
                }
                else
                {
                    MyUtils.Normalize(ref normal, out temporaryVoxel.Normal);
                }
                temporaryVoxel.Normal_CalcCounter = m_temporaryVoxelsCounter;
            }
        }

        private Vector3 ComputeVertexNormal(ref Vector3 position)
        {
            Vector3 s = (position - m_originPosition) / m_voxelSizeInMeters + 1;

            Vector3 normal;
            const float e = 0.01f;
            normal.X = SampleContent(s.X-e, s.Y, s.Z) - SampleContent(s.X+e, s.Y, s.Z);
            normal.Y = SampleContent(s.X, s.Y-e, s.Z) - SampleContent(s.X, s.Y+e, s.Z);
            normal.Z = SampleContent(s.X, s.Y, s.Z-e) - SampleContent(s.X, s.Y, s.Z+e);
            Vector3.Normalize(ref normal, out normal);
            return normal;
        }

        private float SampleContent(float x, float y, float z)
        {
            var sample = new Vector3(x, y, z);
            var minVoxel = Vector3I.Floor(sample);
            sample -= minVoxel;

            float s000 = m_cache.Content(minVoxel.X, minVoxel.Y, minVoxel.Z);
            float s100 = m_cache.Content(minVoxel.X+1, minVoxel.Y,   minVoxel.Z);
            float s010 = m_cache.Content(minVoxel.X,   minVoxel.Y+1, minVoxel.Z);
            float s110 = m_cache.Content(minVoxel.X+1, minVoxel.Y+1, minVoxel.Z);
            float s001 = m_cache.Content(minVoxel.X,   minVoxel.Y,   minVoxel.Z+1);
            float s101 = m_cache.Content(minVoxel.X+1, minVoxel.Y,   minVoxel.Z+1);
            float s011 = m_cache.Content(minVoxel.X,   minVoxel.Y+1, minVoxel.Z+1);
            float s111 = m_cache.Content(minVoxel.X+1, minVoxel.Y+1, minVoxel.Z+1);

            s000 += sample.X * (s100 - s000);
            s010 += sample.X * (s110 - s010);
            s001 += sample.X * (s101 - s001);
            s011 += sample.X * (s111 - s011);

            s000 += sample.Y * (s010 - s000);
            s001 += sample.Y * (s011 - s001);

            s000 += sample.Z * (s001 - s000);
            return s000;
        }

        //  Get sun color (or light) from lookup table or calc it
        //  IMPORTANT: At this point normals must be calculated because GetVoxelAmbientAndSun() will be retrieving them from temp table and not checking if there is actual value
        void GetVoxelAmbient(MyTemporaryVoxel temporaryVoxel, ref Vector3I coord, ref Vector3I tempVoxelCoord)
        {
            if (temporaryVoxel.Ambient_CalcCounter != m_temporaryVoxelsCounter)
            {
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //  Ambient light calculation is same for LOD and no-LOD
                //  This formula was choosen by experiments and observation, no real thought is behind it.
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (MyPerGameSettings.ConstantVoxelAmbient.HasValue)
                {
                    temporaryVoxel.Ambient = MyPerGameSettings.ConstantVoxelAmbient.Value;
                    temporaryVoxel.Ambient_CalcCounter = m_temporaryVoxelsCounter;
                    return;
                }

                // Voxel ambient occlusion
                const int VOXELS_CHECK_COUNT = 1;
                var cache = m_cache;
                float ambient = 0f;
                for (int ambientX = -VOXELS_CHECK_COUNT; ambientX <= VOXELS_CHECK_COUNT; ambientX++)
                {
                    for (int ambientY = -VOXELS_CHECK_COUNT; ambientY <= VOXELS_CHECK_COUNT; ambientY++)
                    {
                        for (int ambientZ = -VOXELS_CHECK_COUNT; ambientZ <= VOXELS_CHECK_COUNT; ambientZ++)
                        {
                            Vector3I tmpVoxelCoord = new Vector3I(coord.X + ambientX - 1, coord.Y + ambientY - 1, coord.Z + ambientZ - 1);

                            if ((tmpVoxelCoord.X < 0) || (tmpVoxelCoord.X > (m_sizeMinusOne.X)) ||
                                (tmpVoxelCoord.Y < 0) || (tmpVoxelCoord.Y > (m_sizeMinusOne.Y)) ||
                                (tmpVoxelCoord.Z < 0) || (tmpVoxelCoord.Z > (m_sizeMinusOne.Z)))
                            {
                                //  Ambient occlusion for requested voxel can't be calculated because surounding voxels are outside of the map
                            }
                            else
                            {
                                ambient += (float)cache.Content(coord.X + ambientX, coord.Y + ambientY, coord.Z + ambientZ);
                            }
                        }
                    }
                }

                //  IMPORTANT: We trace 3x3x3 voxels around our voxel. So when dividng to get <0..1> interval, divide by this number.
                ambient /= MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT * (VOXELS_CHECK_COUNT * 2 + 1) * (VOXELS_CHECK_COUNT * 2 + 1) * (VOXELS_CHECK_COUNT * 2 + 1);

                //  Flip the number, so from now dark voxels are 0.0 and light are 1.0
                ambient = 1.0f - ambient;

                //  This values are chosen by trial-and-error
                const float MIN = 0.4f;// 0.1f;
                const float MAX = 0.9f;// 0.6f;

                ambient = MathHelper.Clamp(ambient, MIN, MAX);

                //ambient = (ambient - MIN) / (MAX - MIN);
                //ambient -= 0.5f;

                temporaryVoxel.Ambient = ambient;
                temporaryVoxel.Ambient_CalcCounter = m_temporaryVoxelsCounter;
            }
        }

        //  Linearly interpolates position, normal and material on poly-cube edge. Interpolated point is where an isosurface cuts an edge between two vertices, each with their own scalar value.
        void GetVertexInterpolation(MyStorageData cache, MyTemporaryVoxel inputVoxelA, MyTemporaryVoxel inputVoxelB, int edgeIndex)
        {
            MyEdge edge = m_edges[edgeIndex];

            byte contentA = cache.Content(inputVoxelA.IdxInCache);
            byte contentB = cache.Content(inputVoxelB.IdxInCache);
            byte materialA = cache.Material(inputVoxelA.IdxInCache);
            byte materialB = cache.Material(inputVoxelB.IdxInCache);

            if (Math.Abs(MyVoxelConstants.VOXEL_ISO_LEVEL - contentA) < 0.00001f)
            {
                edge.Position = inputVoxelA.Position;
                edge.Normal = inputVoxelA.Normal;
                edge.Material = materialA;
                edge.Ambient = inputVoxelA.Ambient;
                return;
            }

            if (Math.Abs(MyVoxelConstants.VOXEL_ISO_LEVEL - contentB) < 0.00001f)
            {
                edge.Position = inputVoxelB.Position;
                edge.Normal = inputVoxelB.Normal;
                edge.Material = materialB;
                edge.Ambient = inputVoxelB.Ambient;
                return;
            }

            float mu = (float)(MyVoxelConstants.VOXEL_ISO_LEVEL - contentA) / (float)(contentB - contentA);
            Debug.Assert(mu > 0.0f && mu < 1.0f);

            edge.Position.X = inputVoxelA.Position.X + mu * (inputVoxelB.Position.X - inputVoxelA.Position.X);
            edge.Position.Y = inputVoxelA.Position.Y + mu * (inputVoxelB.Position.Y - inputVoxelA.Position.Y);
            edge.Position.Z = inputVoxelA.Position.Z + mu * (inputVoxelB.Position.Z - inputVoxelA.Position.Z);
            //edge.Normal = ComputeVertexNormal(ref edge.Position);

            edge.Normal.X = inputVoxelA.Normal.X + mu * (inputVoxelB.Normal.X - inputVoxelA.Normal.X);
            edge.Normal.Y = inputVoxelA.Normal.Y + mu * (inputVoxelB.Normal.Y - inputVoxelA.Normal.Y);
            edge.Normal.Z = inputVoxelA.Normal.Z + mu * (inputVoxelB.Normal.Z - inputVoxelA.Normal.Z);
            if (MyUtils.IsZero(edge.Normal))
                edge.Normal = inputVoxelA.Normal;
            else
                edge.Normal = MyUtils.Normalize(edge.Normal);


            if (MyUtils.IsZero(edge.Normal))
                edge.Normal = inputVoxelA.Normal;

            float mu2 = ((float)contentB) / (((float)contentA) + ((float)contentB));
            edge.Material = (mu2 <= 0.5f) ? materialA : materialB;
            edge.Ambient = inputVoxelA.Ambient + mu2 * (inputVoxelB.Ambient - inputVoxelA.Ambient);

            return;
        }

        public int AffectedRangeOffset
        {
            get { return -1; }
        }

        public int AffectedRangeSizeChange
        {
            get { return 3; }
        }

        public int InvalidatedRangeInflate
        {
            get { return 2; } // AffectedRangeSizeChange + AffectedRangeOffset
        }

        public int VertexPositionRangeSizeChange
        {
            get { return 0; }
        }

        public float VertexPositionOffsetChange
        {
            get { return MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF; }
        }

        //  Precalculate voxel cell into cache (makes triangles and vertex buffer from voxels)
        public MyIsoMesh Precalc(MyIsoMesherArgs args)
        {
            var voxelStart = args.GeometryCell.CoordInLod * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            var voxelEnd = voxelStart + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS - 1;
            return Precalc(args.Storage, args.GeometryCell.Lod, voxelStart, voxelEnd, true, true, false);
        }

        public MyIsoMesh Precalc(IMyStorage storage, int lod, Vector3I voxelStart, Vector3I voxelEnd, bool generateMaterials, bool useAmbient, bool doNotCheck, bool adviceCache = false)
        {

            m_resultVerticesCounter = 0;
            m_resultTrianglesCounter = 0;
            m_edgeVertexCalcCounter++;
            m_temporaryVoxelsCounter++;            

            CalcPolygCubeSize(lod, storage.Size);

            m_voxelStart = voxelStart;
            //voxelStart = voxelStart;
            //voxelEnd = voxelEnd;
            var ssize = storage.Size;
            m_cache.Resize(voxelStart, voxelEnd);

            // Load content first, check it if it contains isosurface, early exit if it doesn't.
            storage.ReadRange(m_cache, MyStorageDataTypeFlags.Content, lod, ref voxelStart, ref voxelEnd);

            if (!m_cache.ContainsIsoSurface())
                return null;

            storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, lod, ref voxelStart, ref voxelEnd);

            ProfilerShort.Begin("Marching cubes");
            {
                //  Size of voxel or cell (in meters) and size of voxel map / voxel cells
                ComputeSizeAndOrigin(lod, storage.Size);


                var start = Vector3I.Zero;
                var end = voxelEnd - voxelStart - 3;
                Vector3I coord0 = start;


                for (var it = new Vector3I_RangeIterator(ref start, ref end); it.IsValid(); it.GetNext(out coord0))
                {
                    int cubeIndex = 0;
                    if (m_cache.Content(coord0.X + 0, coord0.Y + 0, coord0.Z + 0) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 1;
                    if (m_cache.Content(coord0.X + 1, coord0.Y + 0, coord0.Z + 0) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 2;
                    if (m_cache.Content(coord0.X + 1, coord0.Y + 0, coord0.Z + 1) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 4;
                    if (m_cache.Content(coord0.X + 0, coord0.Y + 0, coord0.Z + 1) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 8;
                    if (m_cache.Content(coord0.X + 0, coord0.Y + 1, coord0.Z + 0) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 16;
                    if (m_cache.Content(coord0.X + 1, coord0.Y + 1, coord0.Z + 0) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 32;
                    if (m_cache.Content(coord0.X + 1, coord0.Y + 1, coord0.Z + 1) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 64;
                    if (m_cache.Content(coord0.X + 0, coord0.Y + 1, coord0.Z + 1) < MyVoxelConstants.VOXEL_ISO_LEVEL) cubeIndex |= 128;

                    //  Cube is entirely in/out of the surface
                    if (MyMarchingCubesConstants.EdgeTable[cubeIndex] == 0)
                    {
                        continue;
                    }

                    //  We can get this voxel content right from cache (not using GetVoxelContent method), because after CopyVoxelContents these array must be filled. But only content, not material, normal, etc.
                    Vector3I tempVoxelCoord0 = ComputeTemporaryVoxelData(m_cache, ref coord0, cubeIndex, lod);

                    //  Create the triangles
                    CreateTriangles(ref coord0, cubeIndex, ref tempVoxelCoord0);
                }
            }
            ProfilerShort.End();


            double numCellsHalf = 0.5f * (m_cache.Size3D.X);
            var voxelSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
            var vertexCellOffset = voxelStart - AffectedRangeOffset;

            IMyIsoMesherOutputBuffer isomesh = new MyIsoMesh();


            for (int i = 0; i < m_resultVerticesCounter; i++)
            {
                var pos = (m_resultVertices[i].Position - (Vector3)storage.Size / 2) / storage.Size;                
                m_resultVertices[i].Position = pos;
                m_resultVertices[i].PositionMorph = pos;
                m_resultVertices[i].NormalMorph = m_resultVertices[i].Normal;
                m_resultVertices[i].MaterialMorph = m_resultVertices[i].Material;
                m_resultVertices[i].AmbientMorph = m_resultVertices[i].Ambient;
            }

            for (int i = 0; i < m_resultVerticesCounter; i++)
            {
                isomesh.WriteVertex(ref m_resultVertices[i].Cell, ref m_resultVertices[i].Position, ref m_resultVertices[i].Normal, (byte)m_resultVertices[i].Material, m_resultVertices[i].Ambient);
            }

            for (int i = 0; i < m_resultTrianglesCounter; i++)
            {
                isomesh.WriteTriangle(m_resultTriangles[i].VertexIndex0, m_resultTriangles[i].VertexIndex1, m_resultTriangles[i].VertexIndex2);
            }

            var mIsoMesh = (MyIsoMesh)isomesh;
            mIsoMesh.PositionOffset = storage.Size / 2;
            mIsoMesh.PositionScale = storage.Size;
            mIsoMesh.CellStart = voxelStart;
            mIsoMesh.CellEnd = voxelEnd;

            var vertexCells = mIsoMesh.Cells.GetInternalArray();
            for (int i = 0; i < mIsoMesh.VerticesCount; i++)
            {
                vertexCells[i] += vertexCellOffset;
            }

            return (MyIsoMesh)isomesh;
        }

        private Vector3I ComputeTemporaryVoxelData(MyStorageData cache, ref Vector3I coord0, int cubeIndex, int lod)
        {
            int coord0LinIdx = coord0.X * m_sX + coord0.Y * m_sY + coord0.Z * m_sZ;

            MyTemporaryVoxel tempVoxel0 = m_temporaryVoxels[coord0LinIdx];
            MyTemporaryVoxel tempVoxel1 = m_temporaryVoxels[coord0LinIdx + m_sX];
            MyTemporaryVoxel tempVoxel2 = m_temporaryVoxels[coord0LinIdx + m_sX + m_sZ];
            MyTemporaryVoxel tempVoxel3 = m_temporaryVoxels[coord0LinIdx + m_sZ];
            MyTemporaryVoxel tempVoxel4 = m_temporaryVoxels[coord0LinIdx + m_sY];
            MyTemporaryVoxel tempVoxel5 = m_temporaryVoxels[coord0LinIdx + m_sX + m_sY];
            MyTemporaryVoxel tempVoxel6 = m_temporaryVoxels[coord0LinIdx + m_sX + m_sY + m_sZ];
            MyTemporaryVoxel tempVoxel7 = m_temporaryVoxels[coord0LinIdx + m_sY + m_sZ];



            Vector3I coord1 = new Vector3I(coord0.X + 1, coord0.Y + 0, coord0.Z + 0);
            Vector3I coord2 = new Vector3I(coord0.X + 1, coord0.Y + 0, coord0.Z + 1);
            Vector3I coord3 = new Vector3I(coord0.X + 0, coord0.Y + 0, coord0.Z + 1);
            Vector3I coord4 = new Vector3I(coord0.X + 0, coord0.Y + 1, coord0.Z + 0);
            Vector3I coord5 = new Vector3I(coord0.X + 1, coord0.Y + 1, coord0.Z + 0);
            Vector3I coord6 = new Vector3I(coord0.X + 1, coord0.Y + 1, coord0.Z + 1);
            Vector3I coord7 = new Vector3I(coord0.X + 0, coord0.Y + 1, coord0.Z + 1);

            Vector3I tempVoxelCoord0 = coord0;
            Vector3I tempVoxelCoord1 = coord1;
            Vector3I tempVoxelCoord2 = coord2;
            Vector3I tempVoxelCoord3 = coord3;
            Vector3I tempVoxelCoord4 = coord4;
            Vector3I tempVoxelCoord5 = coord5;
            Vector3I tempVoxelCoord6 = coord6;
            Vector3I tempVoxelCoord7 = coord7;


            tempVoxel0.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord0);
            tempVoxel1.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord1);
            tempVoxel2.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord2);
            tempVoxel3.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord3);
            tempVoxel4.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord4);
            tempVoxel5.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord5);
            tempVoxel6.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord6);
            tempVoxel7.IdxInCache = cache.ComputeLinear(ref tempVoxelCoord7);

            //tempVoxel0.Position.X = m_originPosition.X + (coord0.X) * m_voxelSizeInMeters;
            //tempVoxel0.Position.Y = m_originPosition.Y + (coord0.Y) * m_voxelSizeInMeters;
            //tempVoxel0.Position.Z = m_originPosition.Z + (coord0.Z) * m_voxelSizeInMeters;

            tempVoxel0.Position.X = (m_voxelStart.X + coord0.X) * m_voxelSizeInMeters;
            tempVoxel0.Position.Y = (m_voxelStart.Y + coord0.Y) * m_voxelSizeInMeters;
            tempVoxel0.Position.Z = (m_voxelStart.Z + coord0.Z) * m_voxelSizeInMeters;


            tempVoxel1.Position.X = tempVoxel0.Position.X + m_voxelSizeInMeters;
            tempVoxel1.Position.Y = tempVoxel0.Position.Y;
            tempVoxel1.Position.Z = tempVoxel0.Position.Z;

            tempVoxel2.Position.X = tempVoxel0.Position.X + m_voxelSizeInMeters;
            tempVoxel2.Position.Y = tempVoxel0.Position.Y;
            tempVoxel2.Position.Z = tempVoxel0.Position.Z + m_voxelSizeInMeters;

            tempVoxel3.Position.X = tempVoxel0.Position.X;
            tempVoxel3.Position.Y = tempVoxel0.Position.Y;
            tempVoxel3.Position.Z = tempVoxel0.Position.Z + m_voxelSizeInMeters;

            tempVoxel4.Position.X = tempVoxel0.Position.X;
            tempVoxel4.Position.Y = tempVoxel0.Position.Y + m_voxelSizeInMeters;
            tempVoxel4.Position.Z = tempVoxel0.Position.Z;

            tempVoxel5.Position.X = tempVoxel0.Position.X + m_voxelSizeInMeters;
            tempVoxel5.Position.Y = tempVoxel0.Position.Y + m_voxelSizeInMeters;
            tempVoxel5.Position.Z = tempVoxel0.Position.Z;

            tempVoxel6.Position.X = tempVoxel0.Position.X + m_voxelSizeInMeters;
            tempVoxel6.Position.Y = tempVoxel0.Position.Y + m_voxelSizeInMeters;
            tempVoxel6.Position.Z = tempVoxel0.Position.Z + m_voxelSizeInMeters;

            tempVoxel7.Position.X = tempVoxel0.Position.X;
            tempVoxel7.Position.Y = tempVoxel0.Position.Y + m_voxelSizeInMeters;
            tempVoxel7.Position.Z = tempVoxel0.Position.Z + m_voxelSizeInMeters;

            //  Normals at grid corners (calculated from gradient)
            GetVoxelNormal(tempVoxel0, ref coord0, ref tempVoxelCoord0, tempVoxel0);
            GetVoxelNormal(tempVoxel1, ref coord1, ref tempVoxelCoord1, tempVoxel0);
            GetVoxelNormal(tempVoxel2, ref coord2, ref tempVoxelCoord2, tempVoxel0);
            GetVoxelNormal(tempVoxel3, ref coord3, ref tempVoxelCoord3, tempVoxel0);
            GetVoxelNormal(tempVoxel4, ref coord4, ref tempVoxelCoord4, tempVoxel0);
            GetVoxelNormal(tempVoxel5, ref coord5, ref tempVoxelCoord5, tempVoxel0);
            GetVoxelNormal(tempVoxel6, ref coord6, ref tempVoxelCoord6, tempVoxel0);
            GetVoxelNormal(tempVoxel7, ref coord7, ref tempVoxelCoord7, tempVoxel0);

            //  Ambient occlusion colors at grid corners
            //  IMPORTANT: At this point normals must be calculated because GetVoxelAmbientAndSun() will be retrieving them from temp table and not checking if there is actual value
            GetVoxelAmbient(tempVoxel0, ref coord0, ref tempVoxelCoord0);
            GetVoxelAmbient(tempVoxel1, ref coord1, ref tempVoxelCoord1);
            GetVoxelAmbient(tempVoxel2, ref coord2, ref tempVoxelCoord2);
            GetVoxelAmbient(tempVoxel3, ref coord3, ref tempVoxelCoord3);
            GetVoxelAmbient(tempVoxel4, ref coord4, ref tempVoxelCoord4);
            GetVoxelAmbient(tempVoxel5, ref coord5, ref tempVoxelCoord5);
            GetVoxelAmbient(tempVoxel6, ref coord6, ref tempVoxelCoord6);
            GetVoxelAmbient(tempVoxel7, ref coord7, ref tempVoxelCoord7);

            //  Find the vertices where the surface intersects the cube
            int edgeVal = MyMarchingCubesConstants.EdgeTable[cubeIndex];
            if ((edgeVal & 1) == 1) { GetVertexInterpolation(cache, tempVoxel0, tempVoxel1, 0); }
            if ((edgeVal & 2) == 2) { GetVertexInterpolation(cache, tempVoxel1, tempVoxel2, 1); }
            if ((edgeVal & 4) == 4) { GetVertexInterpolation(cache, tempVoxel2, tempVoxel3, 2); }
            if ((edgeVal & 8) == 8) { GetVertexInterpolation(cache, tempVoxel3, tempVoxel0, 3); }
            if ((edgeVal & 16) == 16) { GetVertexInterpolation(cache, tempVoxel4, tempVoxel5, 4); }
            if ((edgeVal & 32) == 32) { GetVertexInterpolation(cache, tempVoxel5, tempVoxel6, 5); }
            if ((edgeVal & 64) == 64) { GetVertexInterpolation(cache, tempVoxel6, tempVoxel7, 6); }
            if ((edgeVal & 128) == 128) { GetVertexInterpolation(cache, tempVoxel7, tempVoxel4, 7); }
            if ((edgeVal & 256) == 256) { GetVertexInterpolation(cache, tempVoxel0, tempVoxel4, 8); }
            if ((edgeVal & 512) == 512) { GetVertexInterpolation(cache, tempVoxel1, tempVoxel5, 9); }
            if ((edgeVal & 1024) == 1024) { GetVertexInterpolation(cache, tempVoxel2, tempVoxel6, 10); }
            if ((edgeVal & 2048) == 2048) { GetVertexInterpolation(cache, tempVoxel3, tempVoxel7, 11); }

            return tempVoxelCoord0;
        }

        private void ComputeSizeAndOrigin(int lodIdx, Vector3I storageSize)
        {
            m_voxelSizeInMeters = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lodIdx);
            m_sizeMinusOne = (storageSize >> lodIdx) - 1;
            m_originPosition = m_voxelStart * m_voxelSizeInMeters + (0.5f * m_voxelSizeInMeters);
        }

        private void CreateTriangles(ref Vector3I coord0, int cubeIndex, ref Vector3I tempVoxelCoord0)
        {
            MyVoxelVertex tmpVertex = new MyVoxelVertex();
            int g = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            Vector3I edge = new Vector3I(coord0.X, coord0.Y, coord0.Z);
            for (int i = 0; MyMarchingCubesConstants.TriangleTable[cubeIndex, i] != -1; i += 3)
            {
                //  Edge indexes inside the cube
                int edgeIndex0 = MyMarchingCubesConstants.TriangleTable[cubeIndex, i + 0];
                int edgeIndex1 = MyMarchingCubesConstants.TriangleTable[cubeIndex, i + 1];
                int edgeIndex2 = MyMarchingCubesConstants.TriangleTable[cubeIndex, i + 2];

                MyEdge edge0 = m_edges[edgeIndex0];
                MyEdge edge1 = m_edges[edgeIndex1];
                MyEdge edge2 = m_edges[edgeIndex2];


                //  Edge indexes inside the cell
                Vector4I edgeConversion0 = MyMarchingCubesConstants.EdgeConversion[edgeIndex0];
                Vector4I edgeConversion1 = MyMarchingCubesConstants.EdgeConversion[edgeIndex1];
                Vector4I edgeConversion2 = MyMarchingCubesConstants.EdgeConversion[edgeIndex2];

                MyEdgeVertex edgeVertex0 = m_edgeVertex[edge.X + edgeConversion0.X][edge.Y + edgeConversion0.Y][edge.Z + edgeConversion0.Z][edgeConversion0.W];
                MyEdgeVertex edgeVertex1 = m_edgeVertex[edge.X + edgeConversion1.X][edge.Y + edgeConversion1.Y][edge.Z + edgeConversion1.Z][edgeConversion1.W];
                MyEdgeVertex edgeVertex2 = m_edgeVertex[edge.X + edgeConversion2.X][edge.Y + edgeConversion2.Y][edge.Z + edgeConversion2.Z][edgeConversion2.W];


                MyVoxelVertex compressedVertex0 = new MyVoxelVertex();
                compressedVertex0.Position = edge0.Position;
                MyVoxelVertex compressedVertex1 = new MyVoxelVertex();
                compressedVertex1.Position = edge1.Position;
                MyVoxelVertex compressedVertex2 = new MyVoxelVertex();
                compressedVertex2.Position = edge2.Position;

                //  We want to skip all wrong triangles, those that have two vertex at almost the same location, etc.
                //  We do it simply, by calculating triangle normal and then checking if this normal has length large enough
                if (IsWrongTriangle(ref compressedVertex0, ref compressedVertex1, ref compressedVertex2) == true)
                {
                    continue;
                }

                //  Vertex at edge 0
                if (edgeVertex0.CalcCounter != m_edgeVertexCalcCounter)
                {
                    //  If vertex at edge0 wasn't calculated for this cell during this precalc, we need to add it

                    //  Short overflow check
                    System.Diagnostics.Debug.Assert(m_resultVerticesCounter <= ushort.MaxValue);

                    edgeVertex0.CalcCounter = m_edgeVertexCalcCounter;
                    edgeVertex0.VertexIndex = (ushort)m_resultVerticesCounter;

                    tmpVertex.Position = edge0.Position;
                    tmpVertex.Normal   = edge0.Normal;
                    tmpVertex.Ambient  = edge0.Ambient;
                    tmpVertex.Material = edge0.Material;
                    tmpVertex.PositionMorph = edge0.Position;
                    m_resultVertices[m_resultVerticesCounter] = tmpVertex;

                    m_resultVerticesCounter++;
                }

                //  Vertex at edge 1
                if (edgeVertex1.CalcCounter != m_edgeVertexCalcCounter)
                {
                    //  If vertex at edge1 wasn't calculated for this cell during this precalc, we need to add it

                    //  Short overflow check
                    System.Diagnostics.Debug.Assert(m_resultVerticesCounter <= ushort.MaxValue);

                    edgeVertex1.CalcCounter = m_edgeVertexCalcCounter;
                    edgeVertex1.VertexIndex = (ushort)m_resultVerticesCounter;

                    tmpVertex.Position = edge1.Position;
                    tmpVertex.Normal   = edge1.Normal;
                    tmpVertex.Ambient  = edge1.Ambient;
                    tmpVertex.Material = edge1.Material;
                    tmpVertex.PositionMorph = edge1.Position;
                    m_resultVertices[m_resultVerticesCounter] = tmpVertex;

                    m_resultVerticesCounter++;
                }

                //  Vertex at edge 2
                if (edgeVertex2.CalcCounter != m_edgeVertexCalcCounter)
                {
                    //  If vertex at edge2 wasn't calculated for this cell during this precalc, we need to add it

                    //  Short overflow check
                    System.Diagnostics.Debug.Assert(m_resultVerticesCounter <= ushort.MaxValue);

                    edgeVertex2.CalcCounter = m_edgeVertexCalcCounter;
                    edgeVertex2.VertexIndex = (ushort)m_resultVerticesCounter;

                    tmpVertex.Position = edge2.Position;
                    tmpVertex.Normal   = edge2.Normal;
                    tmpVertex.Ambient  = edge2.Ambient;
                    tmpVertex.Material = edge2.Material;
                    tmpVertex.PositionMorph = edge2.Position;
                    tmpVertex.Cell = coord0;
;
                    m_resultVertices[m_resultVerticesCounter] = tmpVertex;
                    
                    m_resultVerticesCounter++;
                }

                //  Triangle
                m_resultTriangles[m_resultTrianglesCounter].VertexIndex0 = edgeVertex0.VertexIndex;
                m_resultTriangles[m_resultTrianglesCounter].VertexIndex1 = edgeVertex1.VertexIndex;
                m_resultTriangles[m_resultTrianglesCounter].VertexIndex2 = edgeVertex2.VertexIndex;
                
                Debug.Assert(edgeVertex0.VertexIndex < m_resultVerticesCounter);
                Debug.Assert(edgeVertex1.VertexIndex < m_resultVerticesCounter);
                Debug.Assert(edgeVertex2.VertexIndex < m_resultVerticesCounter);

                m_resultTrianglesCounter++;
            }
        }

     
        //  We want to skip all wrong triangles, those that have two vertex at almost the same location, etc.
        bool IsWrongTriangle(ref MyVoxelVertex edge0, ref MyVoxelVertex edge1, ref MyVoxelVertex edge2)
        {
            return MyUtils.IsWrongTriangle(edge0.Position, edge1.Position, edge2.Position);
        }

    }
}
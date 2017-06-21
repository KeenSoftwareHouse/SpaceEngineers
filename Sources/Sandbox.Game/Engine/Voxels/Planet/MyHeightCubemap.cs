using System;
using System.Diagnostics;
using Sandbox.Game.GameSystems;
using Sandbox.Definitions;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Voxels.Planet
{
    #region Height cubemap

    public class MyHeightCubemap : MyWrappedCubemap<MyHeightmapFace>
    {
        public MyHeightCubemap(string folderName, MyModContext context)
        {
            m_faces = new MyHeightmapFace[6];

            var useDefault = false;

            for (int i = 0; i < MyCubemapHelpers.NUM_MAPS; ++i)
            {
                m_faces[i] = MyHeightMapLoadingSystem.Static.GetHeightMap(folderName, MyCubemapHelpers.GetNameForFace(i), context);
                if (m_faces[i] == null) useDefault = true;
                else if (m_faces[i].Resolution != m_resolution && m_resolution != 0)
                {
                    useDefault = true;
                    MyLog.Default.WriteLine("Error: Cubemap faces must be all the same size!");
                }
                else m_resolution = m_faces[i].Resolution;

                if (useDefault) break;
            }

            Name = folderName;

            if (useDefault)
            {
                MyLog.Default.WriteLine(String.Format("Error loading heightmap {0}, using fallback instead. See rest of log for details.", folderName));
                for (int i = 0; i < MyCubemapHelpers.NUM_MAPS; ++i)
                {
                    m_faces[i] = MyHeightmapFace.Default;
                    m_resolution = m_faces[i].Resolution;
                }
            }

            ProfilerShort.Begin("MyHeightCubemap::PrepareSides()");
            PrepareSides();
            ProfilerShort.End();
        }
    }

    public struct MyHeightmapNormal
    {
        public ushort Dx;
        public ushort Dy;
    }

    public class MyHeightmapFace : IMyWrappedCubemapFace
    {
        public int Resolution { get; set; }
        public int ResolutionMinusOne { get; set; }

        public int RowStride { get { return m_real_resolution; } }

        private int m_real_resolution;

        private float m_pixelSizeFour;

        private float m_pixelSize;

        private Box2I m_bounds;

        public MyHeightmapFace(int resolution)
        {
            m_real_resolution = resolution + 2;
            Resolution = resolution;
            ResolutionMinusOne = Resolution - 1;

            Data = new ushort[m_real_resolution * m_real_resolution];
            //NormalData = new MyHeightmapNormal[resolution * resolution];

            m_pixelSizeFour = 4.0f / Resolution;

            m_pixelSize = 1.0f/Resolution;

            m_bounds = new Box2I(Vector2I.Zero, new Vector2I(Resolution - 1));
        }

        public static readonly MyHeightmapFace Default;

        static MyHeightmapFace()
        {
            Default = new MyHeightmapFace(HeightmapNode.HEIGHTMAP_LEAF_SIZE);
            Default.Zero();
        }

        public void Zero()
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (ushort)0;
            }
        }

        public void GetValue(int x, int y, out ushort value)
        {
            value = Data[(y + 1) * m_real_resolution + (x + 1)];
        }

        // Copy the next 4 pixels in the heightmap from a given position to a vector of floats.
        public unsafe void Get4Row(int linearOfft, float* values, ushort* map)
        {
            values[0] = (float)map[linearOfft + 0] * MyCubemapHelpers.USHORT_RECIP;
            values[1] = (float)map[linearOfft + 1] * MyCubemapHelpers.USHORT_RECIP;
            values[2] = (float)map[linearOfft + 2] * MyCubemapHelpers.USHORT_RECIP;
            values[3] = (float)map[linearOfft + 3] * MyCubemapHelpers.USHORT_RECIP;
        }

        // Copy the next 4 pixels in the heightmap from a given position to a vector of floats.
        public unsafe void Get4Row(int linearOfft, float* values)
        {
            values[0] = (float)Data[linearOfft + 0] * MyCubemapHelpers.USHORT_RECIP;
            values[1] = (float)Data[linearOfft + 1] * MyCubemapHelpers.USHORT_RECIP;
            values[2] = (float)Data[linearOfft + 2] * MyCubemapHelpers.USHORT_RECIP;
            values[3] = (float)Data[linearOfft + 3] * MyCubemapHelpers.USHORT_RECIP;
        }

        // This is ever so slightly faster than the above for bicubic interpolation
        // Get the position and normal of 2 pixels, linear offset starts right before the first.
        // We need the t parameter to interpolate the normal.
        public unsafe void GetHermiteSliceRow(int linearOfft, float* values)
        {
            values[0] = (float)Data[linearOfft + 1] * MyCubemapHelpers.USHORT_RECIP;
            values[1] = (float)((int)Data[linearOfft + 2] - Data[linearOfft + 0]) * MyCubemapHelpers.USHORT2_RECIP;
            values[2] = (float)Data[linearOfft + 2] * MyCubemapHelpers.USHORT_RECIP;
            values[3] = (float)((int)Data[linearOfft + 3] - Data[linearOfft + 1]) * MyCubemapHelpers.USHORT2_RECIP;
        }


        public ushort GetValue(int x, int y)
        {
            if (x < 0) x = 0;
            else if (x >= Resolution) x = Resolution - 1;
            if (y < 0) y = 0;
            else if (y >= Resolution) y = Resolution - 1;

            return Data[(y + 1) * m_real_resolution + (x + 1)];
        }

        public float GetValuef(int x, int y)
        {
            return Data[(y + 1) * m_real_resolution + (x + 1)] * MyCubemapHelpers.USHORT_RECIP;
        }

        public void GetNormal(int x, int y, out MyHeightmapNormal normal)
        {
            normal = NormalData[y * Resolution + x];
        }

        public void SetValue(int x, int y, ushort value)
        {
            Data[(y + 1) * m_real_resolution + (x + 1)] = value;
        }

        public ushort[] Data;

        public MyHeightmapNormal[] NormalData;

        /**
         * Compute that starting position of a row in the heightmap.
         */
        public int GetRowStart(int y)
        {
            return (y + 1) * m_real_resolution + 1;
        }

        public void CopyRange(Vector2I start, Vector2I end, MyHeightmapFace other, Vector2I oStart, Vector2I oEnd)
        {
            Vector2I myStep = MyCubemapHelpers.GetStep(ref start, ref end);
            Vector2I oStep = MyCubemapHelpers.GetStep(ref oStart, ref oEnd);
            ushort val;

            for (; start != end; start += myStep, oStart += oStep)
            {
                other.GetValue(oStart.X, oStart.Y, out val);
                SetValue(start.X, start.Y, val);
            }

            other.GetValue(oStart.X, oStart.Y, out val);
            SetValue(start.X, start.Y, val);
        }

        public void CopyRange(Vector2I start, Vector2I end, IMyWrappedCubemapFace other, Vector2I oStart, Vector2I oEnd)
        {
            Debug.Assert(other is MyHeightmapFace);

            CopyRange(start, end, other as MyHeightmapFace, oStart, oEnd);
        }

        public void FinishFace(string faceName)
        {
            int sum;
            int end = Resolution - 1;

            //
            // Smoothe corners
            //

            // BL
            sum = GetValue(0, 0);
            sum += GetValue(-1, 0);
            sum += GetValue(0, -1);
            SetValue(-1, -1, (ushort)(sum / 3));

            // BR
            sum = GetValue(end, 0);
            sum += GetValue(Resolution, 0);
            sum += GetValue(end, -1);
            SetValue(Resolution, -1, (ushort)(sum / 3));

            // TL
            sum = GetValue(0, end);
            sum += GetValue(-1, end);
            sum += GetValue(0, Resolution);
            SetValue(-1, Resolution, (ushort)(sum / 3));

            // TR
            sum = GetValue(end, end);
            sum += GetValue(Resolution, end);
            sum += GetValue(end, Resolution);
            SetValue(Resolution, Resolution, (ushort)(sum / 3));

            // Precompute normalmap
            /*int index = 0;
            for (int y = 0; y < Resolution; ++y)
            {
                for (int x = 0; x < Resolution; ++x)
                {
                    MyHeightmapNormal nrm = new MyHeightmapNormal()
                    {
                        Dx = (ushort)(((int)GetValue(x + 1, y) - GetValue(x - 1, y)) >> 1),
                        Dy = (ushort)(((int)GetValue(x, y + 1) - GetValue(x, y - 1)) >> 1)
                    };

                    NormalData[index++] = nrm;
                }
            }*/
            ProfilerShort.Begin("MyHeightCubemap::CreatePruningTree()");
            CreatePruningTree(faceName);
            ProfilerShort.End();
        }

        #region Pruning Tree

        public struct HeightmapNode
        {
            // Branch factor, this means branch factor ^2 children per node
            // The children are ordered in a grid so it is obvious which one to choose
            public static readonly int HEIGHTMAP_BRANCH_FACTOR = 4;
            public static readonly int HEIGHTMAP_LEAF_SIZE = 8;

            public float Min;
            public float Max;

            internal ContainmentType Intersect(ref BoundingBox query)
            {
                if (query.Min.Z > Max) return ContainmentType.Disjoint;
                if (query.Max.Z < Min) return ContainmentType.Contains;
                return ContainmentType.Intersects;
            }
        }

        private static float HEIGHTMAP_BRANCH_LOG_RECIP = (float)(1.0 / -Math.Log(HeightmapNode.HEIGHTMAP_BRANCH_FACTOR));

        public struct HeightmapLevel
        {
            public HeightmapNode[] Nodes;

            private uint m_res;

            public uint Res
            {
                get { return m_res; }
                set { m_res = value; Recip = 1.0f / m_res; }
            }
            public float Recip;

            public ContainmentType Intersect(int x, int y, ref BoundingBox query)
            {
                return Nodes[y * Res + x].Intersect(ref query);
            }

            public bool IsCellContained(int x, int y, ref BoundingBox box)
            {
                Vector2 cell = new Vector2(x, y) * Recip;
                Vector2 cellMax = cell + Recip;
                if (box.Min.X <= cell.X && box.Min.Y <= cell.Y && box.Max.X >= cellMax.X && box.Max.Y >= cellMax.Y)
                    return true;
                return false;
            }

            public bool IsCellNotContained(int x, int y, ref BoundingBox box)
            {
                Vector2 cell = new Vector2(x, y) * Recip;
                Vector2 cellMax = cell + Recip;
                if (box.Min.X > cell.X || box.Min.Y > cell.Y || box.Max.X < cellMax.X || box.Max.Y > cellMax.Y)
                    return true;
                return false;
            }
        }

        public HeightmapNode Root;

        public HeightmapLevel[] PruningTree;

        public void CreatePruningTree(string mapName)
        {
            int depth = 0;

            int res = Resolution;

            res /= HeightmapNode.HEIGHTMAP_LEAF_SIZE;

            // check if we get an even tree, for now we will rely on that.
            while (res != 1)
            {
                if (res % HeightmapNode.HEIGHTMAP_BRANCH_FACTOR != 0)
                {
                    MyDebug.FailRelease("Cannot build prunning tree for heightmap face {0}!", mapName);
                    MyDebug.FailRelease("Heightmap resolution must be divisible by {1}, and after that a power of {0}. Failing to achieve so will disable several important optimizations!!", HeightmapNode.HEIGHTMAP_BRANCH_FACTOR, HeightmapNode.HEIGHTMAP_LEAF_SIZE);
                    return;
                }
                depth++;
                res /= HeightmapNode.HEIGHTMAP_BRANCH_FACTOR;
            }

            PruningTree = new HeightmapLevel[depth];

            int offset = GetRowStart(0);

            if (depth == 0)
            {
                float min = float.PositiveInfinity;
                float max = float.NegativeInfinity;

                int localOffset = offset;
                for (int y = 0; y < HeightmapNode.HEIGHTMAP_LEAF_SIZE; ++y)
                {
                    for (int x = 0; x < HeightmapNode.HEIGHTMAP_LEAF_SIZE; ++x)
                    {
                        float value = ((float)Data[localOffset + x] * MyCubemapHelpers.USHORT_RECIP);
                        if (min > value) min = value;
                        if (max < value) max = value;
                    }
                    localOffset += m_real_resolution;
                }

                Root.Max = max;
                Root.Min = min;
                return;
            }

            int nodes = HeightmapNode.HEIGHTMAP_BRANCH_FACTOR;

            res = Resolution / HeightmapNode.HEIGHTMAP_LEAF_SIZE;

            // prepare leaf level
            PruningTree[0].Nodes = new HeightmapNode[res * res];
            PruningTree[0].Res = (uint)res;

            int cell = 0;
            for (int j = 0; j < res; ++j)
            {
                int coffset = offset;
                for (int i = 0; i < res; ++i)
                {
                    float min = float.PositiveInfinity;
                    float max = float.NegativeInfinity;

                    int localOffset = coffset - m_real_resolution;
                    for (int y = -1; y <= HeightmapNode.HEIGHTMAP_LEAF_SIZE; ++y)
                    {
                        for (int x = -1; x <= HeightmapNode.HEIGHTMAP_LEAF_SIZE; ++x)
                        {
                            float value = ((float)Data[localOffset + x] * MyCubemapHelpers.USHORT_RECIP);
                            if (min > value) min = value;
                            if (max < value) max = value;
                        }
                        localOffset += m_real_resolution;
                    }

                    PruningTree[0].Nodes[cell] = new HeightmapNode()
                    {
                        Max = max,
                        Min = min
                    };

                    cell++;
                    coffset += HeightmapNode.HEIGHTMAP_LEAF_SIZE;
                }
                offset += HeightmapNode.HEIGHTMAP_LEAF_SIZE * m_real_resolution;
            }

            int l = 0;
            for (int k = 1; k < depth; k++)
            {
                offset = 0;
                int levelRes = res / HeightmapNode.HEIGHTMAP_BRANCH_FACTOR;

                PruningTree[k].Nodes = new HeightmapNode[levelRes * levelRes];
                PruningTree[k].Res = (uint)levelRes;

                cell = 0;
                for (int j = 0; j < levelRes; ++j)
                {
                    int coffset = offset;
                    for (int i = 0; i < levelRes; ++i)
                    {
                        float min = float.PositiveInfinity;
                        float max = float.NegativeInfinity;

                        int localOffset = coffset;
                        for (int y = 0; y < HeightmapNode.HEIGHTMAP_BRANCH_FACTOR; ++y)
                        {
                            for (int x = 0; x < HeightmapNode.HEIGHTMAP_BRANCH_FACTOR; ++x)
                            {
                                HeightmapNode n = PruningTree[l].Nodes[localOffset + x];
                                if (min > n.Min) min = n.Min;
                                if (max < n.Max) max = n.Max;
                            }
                            localOffset += res;
                        }

                        PruningTree[k].Nodes[cell] = new HeightmapNode()
                        {
                            Max = max,
                            Min = min
                        };

                        cell++;
                        coffset += HeightmapNode.HEIGHTMAP_BRANCH_FACTOR;
                    }
                    offset += HeightmapNode.HEIGHTMAP_BRANCH_FACTOR * res;
                }

                // previous level
                l++;
                res = levelRes;
            }

            float tmin = float.PositiveInfinity;
            float tmax = float.NegativeInfinity;

            offset = 0;
            for (int y = 0; y < HeightmapNode.HEIGHTMAP_BRANCH_FACTOR; ++y)
            {
                for (int x = 0; x < HeightmapNode.HEIGHTMAP_BRANCH_FACTOR; ++x)
                {
                    HeightmapNode n = PruningTree[depth - 1].Nodes[offset++];
                    if (tmin > n.Min) tmin = n.Min;
                    if (tmax < n.Max) tmax = n.Max;
                }
            }

            Root.Max = tmax;
            Root.Min = tmin;
        }

        struct Box2I
        {
            public Vector2I Min;
            public Vector2I Max;

            public Box2I(ref BoundingBox bb, uint scale)
            {
                Min = new Vector2I((int)(bb.Min.X * scale), (int)(bb.Min.Y * scale));
                Max = new Vector2I((int)(bb.Max.X * scale), (int)(bb.Max.Y * scale));
            }

            public Box2I(Vector2I min, Vector2I max)
            {
                Min = min;
                Max = max;
            }

            public void Intersect(ref Box2I other)
            {
                Min.X = Math.Max(this.Min.X, other.Min.X);
                Min.Y = Math.Max(this.Min.Y, other.Min.Y);
                Max.X = Math.Min(this.Max.X, other.Max.X);
                Max.Y = Math.Min(this.Max.Y, other.Max.Y);
            }

            public override string ToString()
            {
                return String.Format("[({0}), ({1})]", Min, Max);
            }
        }

        #region Queries

        struct SEntry
        {
            public Box2I Bounds;
            public Vector2I Next;
            public ContainmentType Result;
            public ContainmentType Intersection;
            public uint Level;
            public bool Continue;

            public SEntry(ref BoundingBox query, uint res, Vector2I cell, ContainmentType result, uint level)
            {
                Box2I box = new Box2I(ref query, res);

                cell *= HeightmapNode.HEIGHTMAP_BRANCH_FACTOR;
                Box2I cellb = new Box2I(cell, cell + HeightmapNode.HEIGHTMAP_BRANCH_FACTOR - 1);
                box.Intersect(ref cellb);

                Bounds = box;
                Next = box.Min;

                Level = level;

                Result = result;
                Intersection = result;
                Continue = false;
            }
        }

        [ThreadStatic]
        private static SEntry[] m_queryStack;

        /**
         * Scan pruning tree for intersection with a bounding box.
         * 
         * This method is a iterative implementation of the following algorithm:
         * 
         * procedure scan(level, box, result)
         * for each $cell in $level
         *     $inter = intersects($cell, $box)
         *   
         *     if $inter = INTERSECTS and $cell is not contained and $level != 0
         *         $inter = scan($level -1, box, $result)
         * 
         *     switch on $inter
         *         case INTERSECTS
         *             return INTERSECTS
         * 
         *         case CONTAINED
         *             if $result == DISJOINT
         *                 return INTERSECTS
         *             $result = CONTAINED
         * 
         *         case DISJOINT
         *             if $result == CONTAINED
         *                 return INTERSECTS
         *             $result = DISJOINT
         * return $result
         */
        public ContainmentType QueryHeight(ref BoundingBox query)
        {
            // Fallback in case the user loads a bad heightmap.
            if (PruningTree == null) return ContainmentType.Intersects;

            if (m_queryStack == null || m_queryStack.Length < PruningTree.Length) m_queryStack = new SEntry[PruningTree.Length];

            if (query.Min.Z > Root.Max) return ContainmentType.Disjoint;
            if (query.Max.Z < Root.Min) return ContainmentType.Contains;

            if (query.Max.X < 0 || query.Max.Y < 0 || query.Min.X > 1 || query.Min.Y > 1) return ContainmentType.Disjoint;

            // Handle minimum size heightmaps
            if (PruningTree.Length == 0) return ContainmentType.Intersects;

            if (query.Max.X == 1.0) query.Max.X = .99999999f;
            if (query.Max.Y == 1.0) query.Max.Y = .99999999f;

            // state variables;
            ContainmentType result = ContainmentType.Intersects;

            float maxSize = Math.Max(query.Width, query.Height);

            // If the box is really small we can be even more precise by checking the heightmap directly.
            if (maxSize < m_pixelSizeFour)
            {
                // compute an inflated box so we account for smoothing :)
                Box2I bb = new Box2I(ref query, (uint)Resolution);
                bb.Min -= 1;
                bb.Max += 1;
                bb.Intersect(ref m_bounds);

                int min = (int)(query.Min.Z * ushort.MaxValue);
                int max = (int)(query.Max.Z * ushort.MaxValue);

                ushort height;

                GetValue(bb.Min.X, bb.Min.Y, out height);

                if (height > max) result = ContainmentType.Contains;
                else if (height < min) result = ContainmentType.Disjoint;
                else return ContainmentType.Intersects;

                int mmin = ushort.MaxValue, mmax = 0;

                for (int y = bb.Min.Y; y <= bb.Max.Y; ++y)
                {
                    for (int x = bb.Min.X; x <= bb.Max.X; ++x)
                    {
                        GetValue(x, y, out height);

                        if (height > mmax) mmax = height;
                        if (height < mmin) mmin = height;
                    }
                }

                int diff = mmax - mmin;

                diff += diff >> 1;

                mmax += diff;

                mmin -= diff;

                if (min > mmax) return ContainmentType.Disjoint;
                if (max < mmin) return ContainmentType.Contains;

                return ContainmentType.Intersects;
            }

            double log = Math.Log(maxSize * (Resolution / HeightmapNode.HEIGHTMAP_LEAF_SIZE)) / Math.Log(HeightmapNode.HEIGHTMAP_BRANCH_FACTOR);

            uint level = (uint)MathHelper.Clamp(log, 0, PruningTree.Length - 1);

            // stack index
            int ss = 0;
            var st = m_queryStack;

            Box2I rootBounds = new Box2I(Vector2I.Zero, new Vector2I((int)PruningTree[level].Res - 1));

            st[0].Bounds = new Box2I(ref query, PruningTree[level].Res);
            st[0].Bounds.Intersect(ref rootBounds);
            st[0].Next = st[0].Bounds.Min;
            st[0].Level = level;
            st[0].Result = result;
            st[0].Continue = false;

        scan:
            while (true)
            {
                SEntry state;
                if (ss == -1) break;
                else
                {
                    state = st[ss];
                }

                for (int y = state.Next.Y; y <= state.Bounds.Max.Y; ++y)
                {
                    for (int x = state.Bounds.Min.X; x <= state.Bounds.Max.X; ++x)
                    {
                        if (!state.Continue)
                        {
                            state.Intersection = PruningTree[state.Level].Intersect(x, y, ref query);

                            if (state.Intersection == ContainmentType.Intersects && PruningTree[state.Level].IsCellNotContained(x, y, ref query)
                                && state.Level != 0)
                            {
                                state.Next = new Vector2I(x, y);
                                state.Continue = true;
                                st[ss] = state;
                                ss++;
                                st[ss] = new SEntry(ref query, PruningTree[state.Level - 1].Res, new Vector2I(x, y), state.Result, state.Level - 1);
                                goto scan;
                            }
                        }
                        else
                        {
                            state.Continue = false;
                            x = state.Next.X;
                        }

                        switch (state.Intersection)
                        {
                            case ContainmentType.Intersects:
                                state.Result = ContainmentType.Intersects;
                                goto ret;
                                break;

                            case ContainmentType.Disjoint:
                                if (state.Result == ContainmentType.Contains)
                                {
                                    state.Result = ContainmentType.Intersects;
                                    goto ret;
                                }
                                state.Result = ContainmentType.Disjoint;
                                break;

                            case ContainmentType.Contains:
                                if (state.Result == ContainmentType.Disjoint)
                                {
                                    state.Result = ContainmentType.Intersects;
                                    goto ret;
                                }
                                state.Result = ContainmentType.Contains;
                                break;
                        }
                    }
                }

            ret: ;
                result = state.Result;
                ss--;
                if (ss >= 0)
                    st[ss].Intersection = result;

            }

            return result;
        }

        public void GetBounds(ref BoundingBox query)
        {
            float maxSize = Math.Max(query.Width, query.Height);


            if (maxSize < m_pixelSizeFour || PruningTree == null || PruningTree.Length == 0)
            {
                // compute an inflated box so we account for smoothing :)
                Box2I bb = new Box2I(ref query, (uint)Resolution);
                bb.Min -= 1;
                bb.Max += 1;
                bb.Intersect(ref m_bounds);

                ushort height;

                GetValue(bb.Min.X, bb.Min.Y, out height);

                int mmin = ushort.MaxValue, mmax = 0;

                for (int y = bb.Min.Y; y <= bb.Max.Y; ++y)
                {
                    for (int x = bb.Min.X; x <= bb.Max.X; ++x)
                    {
                        GetValue(x, y, out height);

                        if (height > mmax) mmax = height;
                        if (height < mmin) mmin = height;
                    }
                }

                int diff = mmax - mmin;

                diff = (diff * 2) / 3;

                mmax += diff;

                mmin -= diff;

                query.Min.Z = mmin * MyCubemapHelpers.USHORT_RECIP;
                query.Max.Z = mmax * MyCubemapHelpers.USHORT_RECIP;
                return;
            }

            // Switch to floor to grab some closer precision
            double log = Math.Log(Resolution/(maxSize * HeightmapNode.HEIGHTMAP_LEAF_SIZE)) / Math.Log(HeightmapNode.HEIGHTMAP_BRANCH_FACTOR);

            uint level = (uint)PruningTree.Length - 1 - (uint)MathHelper.Clamp(log, 0, PruningTree.Length - 1);

            Box2I rootBounds = new Box2I(Vector2I.Zero, new Vector2I((int)PruningTree[level].Res - 1));
            Box2I queryBounds = new Box2I(ref query, PruningTree[level].Res);
            queryBounds.Intersect(ref rootBounds);

            query.Min.Z = float.PositiveInfinity;
            query.Max.Z = float.NegativeInfinity;

            int lres = (int)PruningTree[level].Res;
            for (int y = queryBounds.Min.Y; y <= queryBounds.Max.Y; ++y)
            {
                for (int x = queryBounds.Min.X; x <= queryBounds.Max.X; ++x)
                {
                    var cell = PruningTree[level].Nodes[y * lres + x];
                    if (query.Min.Z > cell.Min) query.Min.Z = cell.Min;
                    if (query.Max.Z < cell.Max) query.Max.Z = cell.Max;
                }
            }
        }

        /**
         * Query a line over the heightmap for intersection.
         * 
         * The line is the segment that starts in 'from' and ends in 'to'
         * 
         * 'xy' components of the vectors are the texture coordinates.
         * The 'z' component is the height in [0,1] range.
         * 
         * If either height is above or bellow the plane more than int16.MaxValue units
         * this algorithm may yield incorrect results.
         */
        public bool QueryLine(ref Vector3 from, ref Vector3 to, out float startOffset, out float endOffset)
        {
            // Fallback in case the user loads a bad heightmap.
            if (PruningTree == null)
            {
                startOffset = 0;
                endOffset = 1;
                return true;
            }

            Vector2 s, e;

            s = new Vector2(from.X, from.Y);
            e = new Vector2(to.X, to.Y);

            s *= ResolutionMinusOne;
            e *= ResolutionMinusOne;

            int steps = (int) Math.Ceiling((e - s).Length());

            Vector3 dir = new Vector3(s - e, (to.Z - from.Z) * UInt16.MaxValue);
            float Zlen = dir.Z;

            float stepsR = 1f/steps;

            dir *= stepsR;

            Vector3 pos = new Vector3(s, from.Z * UInt16.MaxValue);
            float startZ = pos.Z;

            int i;
            for (i = 0; i < steps; ++i)
            {
                int sx = (int)Math.Round(pos.X);
                int sy = (int)Math.Round(pos.Y);

                int hmin = (int)pos.Z;
                int hmax = (int)(pos.Z + dir.Z + .5f);

                if (hmin > hmax)
                {
                    var tmp = hmax;
                    hmax = hmin;
                    hmin = tmp;
                }

                int min = int.MaxValue, max = int.MinValue;

                for (int x = -1; x < 2; ++x)
                    for (int y = -1; y < 2; ++y)
                    {
                        int val = GetValue(sx + x, sy + y);
                        min = Math.Min(val, min);
                        max = Math.Max(val, max);
                    }

                if (!(hmax < min || hmin > max))
                {
                    if (dir.Z < 0)
                    {
                        startOffset = Math.Max(-(startZ - max) / Zlen, 0);
                    }
                    else
                    {
                        startOffset = Math.Max((min - startZ) / Zlen, 0);
                    }

                    endOffset = 1.0f;
                    return startOffset < endOffset;
                }

                pos += dir;
            }
            startOffset = 0;
            endOffset = 1;
            return false;
        }
        #endregion

        #endregion
    }

    #endregion

}

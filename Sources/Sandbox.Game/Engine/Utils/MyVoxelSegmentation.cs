using ProtoBuf;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    enum MyVoxelSegmentationType
    {
        /// <summary>
        /// Fastest method, but not very efficient, there's usually 100% more shapes (compared to optimized).
        /// It's about 40x faster than optimized version and 50x faster than fast version.
        /// Often generates just long lines instead of boxes.
        /// </summary>
        ExtraSimple,

        /// <summary>
        /// Quite fast method and quite efficient, there's usually similar number of shapes (compared to optimized).
        /// It's about 3x faster than optimized version, but prefers longer boxes.
        /// </summary>
        Fast,

        /// <summary>
        /// Slowest method, generates lowest number of shapes.
        /// Prefers cubic boxes.
        /// </summary>
        Optimized,

        /// <summary>
        /// Generates a number of shapes comparable to Optimized in a time comparable to ExtraSimple.
        /// </summary>
        Simple,

        /// <summary>
        /// Little optimization added to Simple
        /// </summary>
        Simple2,
    }

    class MyVoxelSegmentation
    {
        [ProtoContract]
        public struct Segment
        {
            [ProtoMember]
            public Vector3I Min;
            [ProtoMember]
            public Vector3I Max;

            public Vector3I Size { get { return Max - Min + Vector3I.One; } }
            public int VoxelCount { get { return Size.X * Size.Y * Size.Z; } }

            // Both inclusive
            public Segment(Vector3I min, Vector3I max)
            {
                Min = min;
                Max = max;
            }

            public bool Contains(Segment b)
            {
                return Vector3I.Min(b.Min, Min) == Min && Vector3I.Max(b.Max, Max) == Max;
            }

            public void Replace(IEnumerable<Vector3I> voxels)
            {
                Min = Vector3I.MaxValue;
                Max = Vector3I.MinValue;
                foreach (var v in voxels)
                {
                    Min = Vector3I.Min(Min, v);
                    Max = Vector3I.Max(Max, v);
                }
            }
        }

        class SegmentSizeComparer : IComparer<Segment>
        {
            public int Compare(Segment x, Segment y)
            {
                return y.VoxelCount - x.VoxelCount;
            }
        }

        class Vector3IComparer : IComparer<Vector3I>
        {
            public int Compare(Vector3I x, Vector3I y)
            {
                return x.CompareTo(y);
            }
        }

        class Vector3IEqualityComparer : IEqualityComparer<Vector3I>
        {
            public bool Equals(Vector3I v1, Vector3I v2)
            {
                return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
            }

            public int GetHashCode(Vector3I obj)
            {
                unchecked
                {
                    int result = obj.X;
                    result = (result * 9767) ^ obj.Y;
                    result = (result * 9767) ^ obj.Z;
                    return result;
                }
            }
        }

        class DescIntComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y - x;
            }
        }

        HashSet<Vector3I> m_filledVoxels = new HashSet<Vector3I>(new Vector3IEqualityComparer());
        HashSet<Vector3I> m_selectionList = new HashSet<Vector3I>(new Vector3IEqualityComparer());
        List<Segment> m_segments = new List<Segment>();
        List<Segment> m_tmpSegments = new List<Segment>();
        SortedSet<Vector3I> m_sortedInput = new SortedSet<Vector3I>(new Vector3IComparer());

        public void ClearInput()
        {
            m_sortedInput.Clear();
            m_filledVoxels.Clear();
        }

        public void AddInput(Vector3I input)
        {
            m_filledVoxels.Add(input);
            m_sortedInput.Add(input);
        }

        public int InputCount { get { return m_filledVoxels.Count; } }

        /// <summary>
        /// Creates segments from voxel data.
        /// </summary>
        /// <param name="filledVoxels">Positions on filled voxels (or one material).</param>
        /// <param name="mergeIterations">Number of post-process merge iterations, one should be always sufficient, algorithm works pretty well with 0 too.</param>
        /// <param name="fastMethod">Fast method is about 3x faster, but prefers longer boxes instead of cubic and generates about 5 - 10% more segments.</param>
        /// <returns>List of segments.</returns>
        public List<Segment> FindSegments(MyVoxelSegmentationType segmentationType = MyVoxelSegmentationType.Optimized, int mergeIterations = 1)
        {
            m_segments.Clear();
            switch(segmentationType) 
            {
                case MyVoxelSegmentationType.Simple:
                    ProfilerShort.Begin("Create segments");
                    CreateSegmentsSimple();
                    ProfilerShort.End();
                    break;

                case MyVoxelSegmentationType.Simple2:
                    ProfilerShort.Begin("Create segments");
                    CreateSegmentsSimple2();
                    ProfilerShort.End();
                    break;

                case MyVoxelSegmentationType.ExtraSimple:
                    ProfilerShort.Begin("Create simple segments");
                    CreateSegmentsExtraSimple();
                    ProfilerShort.End();
                    break;

                default:
                    ProfilerShort.Begin("Create segments");
                    CreateSegments(segmentationType == MyVoxelSegmentationType.Fast);
                    ProfilerShort.End();
                    ProfilerShort.Begin("Sort segments");
                    m_segments.Sort(new SegmentSizeComparer());
                    ProfilerShort.End();
                    ProfilerShort.Begin("Remove fully contained");
                    RemoveFullyContainedOptimized();
                    ProfilerShort.End();
                    ProfilerShort.Begin("Clip segments");
                    ClipSegments();
                    ProfilerShort.End();

                    ProfilerShort.Begin("Merge segments");
                    for (int i = 0; i < mergeIterations; i++)
                    {
                        MergeSegments(); // Very optional
                    }
                    ProfilerShort.End();
                    break;
            }

            return m_segments;
        }

        private void CreateSegmentsExtraSimple()
        {
            while (m_filledVoxels.Count > 0)
            {
                var e = m_filledVoxels.GetEnumerator();
                e.MoveNext();

                Vector3I cube = e.Current;
                Vector3I current = cube;
                ExpandX(ref cube, ref current); // Find longest X
                ExpandY(ref cube, ref current); // Find longest Y for X
                ExpandZ(ref cube, ref current); // Find longest Z for XY
                m_segments.Add(new Segment(cube, current));

                for (int x = cube.X; x <= current.X; x++)
                {
                    for (int y = cube.Y; y <= current.Y; y++)
                    {
                        for (int z = cube.Z; z <= current.Z; z++)
                        {
                            m_filledVoxels.Remove(new Vector3I(x, y, z));
                        }
                    }
                }
            }
        }

        void MergeSegments()
        {
            for (int i = 0; i < m_segments.Count; i++)
            {
                for (int j = i + 1; j < m_segments.Count; )
                {
                    var seg1 = m_segments[i];
                    var seg2 = m_segments[j];

                    int matchAxis = 0;
                    if (seg1.Min.X == seg2.Min.X && seg1.Max.X == seg2.Max.X) matchAxis++;
                    if (seg1.Min.Y == seg2.Min.Y && seg1.Max.Y == seg2.Max.Y) matchAxis++;
                    if (seg1.Min.Z == seg2.Min.Z && seg1.Max.Z == seg2.Max.Z) matchAxis++;

                    if (matchAxis == 2)
                    {
                        if (seg1.Min.X == seg2.Max.X + 1 || seg1.Max.X + 1 == seg2.Min.X ||
                            seg1.Min.Y == seg2.Max.Y + 1 || seg1.Max.Y + 1 == seg2.Min.Y ||
                            seg1.Min.Z == seg2.Max.Z + 1 || seg1.Max.Z + 1 == seg2.Min.Z)
                        {
                            // Because of struct
                            seg1.Min = Vector3I.Min(seg1.Min, seg2.Min);
                            seg1.Max = Vector3I.Max(seg1.Max, seg2.Max);
                            m_segments[i] = seg1;

                            m_segments.RemoveAt(j);
                            continue;
                        }
                    }
                    j++;
                }
            }
        }

        void ClipSegments()
        {
            for (int i = m_segments.Count - 1; i >= 0; i--)
            {
                m_filledVoxels.Clear();
                AddAllVoxels(m_segments[i].Min, m_segments[i].Max);

                //for (int j = i - 1; j >= 0; j--) // Clip only by larger
                for (int j = m_segments.Count - 1; j >= 0; j--) // Clip by all, better results
                {
                    if (i == j)
                        continue;

                    RemoveVoxels(m_segments[j].Min, m_segments[j].Max);
                    if (m_filledVoxels.Count == 0)
                    {
                        break;
                    }
                }

                if (m_filledVoxels.Count == 0)
                {
                    m_segments.RemoveAt(i);
                }
                else
                {
                    // Because of struct
                    var seg = m_segments[i];
                    seg.Replace(m_filledVoxels);
                    m_segments[i] = seg;
                }
            }
        }

        void AddAllVoxels(Vector3I from, Vector3I to)
        {
            for (int x = from.X; x <= to.X; x++)
            {
                for (int y = from.Y; y <= to.Y; y++)
                {
                    for (int z = from.Z; z <= to.Z; z++)
                    {
                        m_filledVoxels.Add(new Vector3I(x, y, z));
                    }
                }
            }
        }

        void RemoveVoxels(Vector3I from, Vector3I to)
        {
            for (int x = from.X; x <= to.X; x++)
            {
                for (int y = from.Y; y <= to.Y; y++)
                {
                    for (int z = from.Z; z <= to.Z; z++)
                    {
                        m_filledVoxels.Remove(new Vector3I(x, y, z));
                    }
                }
            }
        }

        void RemoveFullyContained()
        {
            for (int i = 0; i < m_segments.Count; i++)
            {
                for (int j = i + 1; j < m_segments.Count; )
                {
                    if (m_segments[i].Contains(m_segments[j]))
                    {
                        m_segments.RemoveAt(j);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        void RemoveFullyContainedOptimized()
        {
            m_filledVoxels.Clear();
            m_tmpSegments.Clear();

            Vector3I current, from, to;

            for (int i = 0; i < m_segments.Count; i++)
            {
                bool added = false;
                from = m_segments[i].Min;
                to = m_segments[i].Max;

                for (current.X = from.X; current.X <= to.X; current.X++)
                {
                    for (current.Y = from.Y; current.Y <= to.Y; current.Y++)
                    {
                        for (current.Z = from.Z; current.Z <= to.Z; current.Z++)
                        {
                            added = m_filledVoxels.Add(current) | added;
                        }
                    }
                }

                if (added)
                    m_tmpSegments.Add(m_segments[i]);
            }

            var x = m_segments;
            m_segments = m_tmpSegments;
            m_tmpSegments = x;
        }

        void CreateSegments(bool fastMethod)
        {
            m_usedVoxels.Clear();

            Vector3I cube, current;

            foreach (var filledVoxel in m_filledVoxels) // Find all clusters for all voxels
            {
                if (m_usedVoxels.Contains(filledVoxel))
                    continue;

                cube = filledVoxel;
                current = filledVoxel;

                ExpandX(ref cube, ref current); // Find longest X
                ExpandY(ref cube, ref current); // Find longest Y for X
                ExpandZ(ref cube, ref current); // Find longest Z for XY
                AddSegment(ref cube, ref current);

                Vector3I start = current;

                if (!fastMethod)
                {
                    while (current.X > cube.X)
                    {
                        while (current.Y > cube.Y)
                        {
                            // Reverse look into Y
                            current.Y--;
                            current.Z = cube.Z;

                            ExpandZ(ref cube, ref current); // Find longest Z for XY
                            AddSegment(ref cube, ref current);
                        }

                        // Reverse look into X
                        current.X--;
                        current.Y = cube.Y;
                        current.Z = cube.Z;

                        ExpandY(ref cube, ref current); // Find longest Y for X
                        ExpandZ(ref cube, ref current); // Find longest Z for XY
                        AddSegment(ref cube, ref current);
                    }
                }
            }
        }

        HashSet<Vector3I> m_usedVoxels = new HashSet<Vector3I>();

        void AddSegment(ref Vector3I from, ref Vector3I to)
        {
            // This will make futher stages much slower
            //m_segments.Add(new Segment(from, to));
            //return;

            bool added = false;
            Vector3I current;
            for (current.X = from.X; current.X <= to.X; current.X++)
            {
                for (current.Y = from.Y; current.Y <= to.Y; current.Y++)
                {
                    for (current.Z = from.Z; current.Z <= to.Z; current.Z++)
                    {
                        added = m_usedVoxels.Add(current) | added;
                    }
                }
            }
            if (added)
                m_segments.Add(new Segment(from, to));
        }

        Vector3I ShiftVector(Vector3I vec)
        {
            return new Vector3I(vec.Z, vec.X, vec.Y);
        }

        // Both inclusive
        bool AllFilled(Vector3I from, Vector3I to)
        {
            Vector3I current;
            for (current.X = to.X; current.X >= from.X; current.X--)
            {
                for (current.Y = to.Y; current.Y >= from.Y; current.Y--)
                {
                    for (current.Z = to.Z; current.Z >= from.Z; current.Z--)
                    {
                        if (!m_filledVoxels.Contains(current))
                            return false;
                    }
                }
            }
            return true;
        }

        int Expand(Vector3I start, ref Vector3I pos, ref Vector3I expand)
        {
            int count = 0;
            while (AllFilled(start + expand, pos + expand)) // Explore expand direction
            {
                start += expand;
                pos += expand;
                count++;
            }
            return count;
        }

        int ExpandX(ref Vector3I start, ref Vector3I pos)
        {
            return Expand(start, ref pos, ref Vector3I.UnitX);
        }

        int ExpandY(ref Vector3I start, ref Vector3I pos)
        {
            return Expand(start, ref pos, ref Vector3I.UnitY);
        }

        int ExpandZ(ref Vector3I start, ref Vector3I pos)
        {
            return Expand(start, ref pos, ref Vector3I.UnitZ);
        }

        private void CreateSegmentsSimple2()
        {
            m_selectionList.Clear();
            foreach (var x in m_filledVoxels)
            {
                m_selectionList.Add(x);
            }

            CreateSegmentsSimpleCore();
        }

        private void CreateSegmentsSimple()
        {
            var tmp = m_selectionList;
            m_selectionList = m_filledVoxels;

            CreateSegmentsSimpleCore();

            m_selectionList = tmp;
        }

        private void CreateSegmentsSimpleCore()
        {
            while (m_selectionList.Count > 0)
            {
                var e = m_selectionList.GetEnumerator();
                e.MoveNext();

                bool expandPlusX = true;
                bool expandPlusY = true;
                bool expandPlusZ = true;
                bool expandMinusX = true;
                bool expandMinusY = true;
                bool expandMinusZ = true;
                Vector3I min = e.Current;
                Vector3I max = min;
                m_filledVoxels.Remove(min);
                m_selectionList.Remove(min);
                while (expandPlusX || expandPlusY || expandPlusZ || expandMinusX || expandMinusY || expandMinusZ)
                {
                    if (expandPlusX)
                        expandPlusX = ExpandByOnePlusX(ref min, ref max);

                    if (expandMinusX)
                        expandMinusX = ExpandByOneMinusX(ref min, ref max);

                    if (expandPlusY)
                        expandPlusY = ExpandByOnePlusY(ref min, ref max);

                    if (expandMinusY)
                        expandMinusY = ExpandByOneMinusY(ref min, ref max);

                    if (expandPlusZ)
                        expandPlusZ = ExpandByOnePlusZ(ref min, ref max);

                    if (expandMinusZ)
                        expandMinusZ = ExpandByOneMinusZ(ref min, ref max);
                }
                m_segments.Add(new Segment(min, max));
            }
        }

        private bool ExpandByOnePlusX(ref Vector3I min, ref Vector3I max)
        {
            int x = max.X + 1;
            for (int y = min.Y; y <= max.Y; ++y)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            max.X = x;

            for (int y = min.Y; y <= max.Y; ++y)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }

        private bool ExpandByOnePlusY(ref Vector3I min, ref Vector3I max)
        {
            int y = max.Y + 1;
            for (int x = min.X; x <= max.X; ++x)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            max.Y = y;

            for (int x = min.X; x <= max.X; ++x)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }

        private bool ExpandByOnePlusZ(ref Vector3I min, ref Vector3I max)
        {
            int z = max.Z + 1;
            for (int x = min.X; x <= max.X; ++x)
            {
                for (int y = min.Y; y <= max.Y; ++y)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            max.Z = z;

            for (int x = min.X; x <= max.X; ++x)
            {
                for (int y = min.Y; y <= max.Y; ++y)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }

        private bool ExpandByOneMinusX(ref Vector3I min, ref Vector3I max)
        {
            int x = min.X - 1;
            for (int y = min.Y; y <= max.Y; ++y)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            min.X = x;

            for (int y = min.Y; y <= max.Y; ++y)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }

        private bool ExpandByOneMinusY(ref Vector3I min, ref Vector3I max)
        {
            int y = min.Y - 1;
            for (int x = min.X; x <= max.X; ++x)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            min.Y = y;

            for (int x = min.X; x <= max.X; ++x)
            {
                for (int z = min.Z; z <= max.Z; ++z)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }

        private bool ExpandByOneMinusZ(ref Vector3I min, ref Vector3I max)
        {
            int z = min.Z - 1;
            for (int x = min.X; x <= max.X; ++x)
            {
                for (int y = min.Y; y <= max.Y; ++y)
                {
                    if (!m_filledVoxels.Contains(new Vector3I(x, y, z)))
                        return false;
                }
            }

            min.Z = z;

            for (int x = min.X; x <= max.X; ++x)
            {
                for (int y = min.Y; y <= max.Y; ++y)
                {
                    m_selectionList.Remove(new Vector3I(x, y, z));
                }
            }

            return true;
        }
    }
}

using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public class MyFourEdgeInfo
    {
        unsafe struct Data
        {
            public Vector4 LocalOrthoMatrix;
            public MyCubeEdgeType EdgeType;

            fixed uint m_data[4];
            fixed byte m_data2[4];
            fixed int m_edgeModels[4];

            public bool Full
            {
                get
                {
                    fixed (uint* i = m_data)
                    {
                        return i[0] != 0 & i[1] != 0 & i[2] != 0 & i[3] != 0;
                    }
                }
            }

            public bool Empty
            {
                get
                {
                    fixed (uint* i = m_data)
                    {
                        return ((ulong*)i)[0] == 0 & ((ulong*)i)[1] == 0;
                    }
                }
            }

            public int Count
            {
                get
                {
                    fixed (uint* i = m_data)
                    {
                        return (i[0] != 0 ? 1 : 0)
                            + (i[1] != 0 ? 1 : 0)
                            + (i[2] != 0 ? 1 : 0)
                            + (i[3] != 0 ? 1 : 0);
                    }
                }
            }

            public uint Get(int index)
            {
                fixed (uint* i = m_data)
                {
                    return i[index];
                }
            }

            public void Get(int index, out Color color, out MyStringHash edgeModel, out Base27Directions.Direction normal0, out Base27Directions.Direction normal1)
            {
                fixed (uint* i = m_data)
                fixed (byte* d = m_data2)
                fixed (int* edges = m_edgeModels)
                {
                    color = new Color(i[index]);
                    normal0 = (Base27Directions.Direction)color.A;
                    normal1 = (Base27Directions.Direction)d[index];
                    edgeModel = MyStringHash.TryGet(edges[index]);
                    Debug.Assert(edges[index] == 0 || edgeModel != MyStringHash.NullOrEmpty);
                }
            }

            public bool Set(int index, Color value, MyStringHash edgeModel, Base27Directions.Direction normal0, Base27Directions.Direction normal1)
            {
                fixed (uint* i = m_data)
                fixed (byte* d = m_data2)
                fixed (int* edges = m_edgeModels)
                {
                    value.A = (byte)normal0;
                    bool result = i[index] == 0;
                    i[index] = value.PackedValue;
                    d[index] = (byte)normal1;
                    edges[index] = (int)edgeModel;
                    return result;
                }
            }

            public bool Reset(int index)
            {
                fixed (uint* i = m_data)
                fixed (int* edges = m_edgeModels)
                {
                    bool result = i[index] != 0;
                    i[index] = 0;
                    edges[index] = 0;
                    return result;
                }
            }
        }

        static readonly int DirectionMax = MyUtils.GetMaxValueFromEnum<Base27Directions.Direction>() + 1;

        public const int MaxInfoCount = 4;

        Data m_data;

        public Vector4 LocalOrthoMatrix { get { return m_data.LocalOrthoMatrix; } }
        public MyCubeEdgeType EdgeType { get { return m_data.EdgeType; } }

        public bool Empty
        {
            get
            {
                return m_data.Empty;
            }
        }

        public bool Full
        {
            get
            {
                return m_data.Full;
            }
        }

        public int DebugCount
        {
            get
            {
                return m_data.Count;
            }
        }

        public MyFourEdgeInfo(Vector4 localOrthoMatrix, MyCubeEdgeType edgeType)
        {
            m_data.LocalOrthoMatrix = localOrthoMatrix;
            m_data.EdgeType = edgeType;
        }

        public bool AddInstance(Vector3 blockPos, Color color, MyStringHash edgeModel, Base27Directions.Direction normal0, Base27Directions.Direction normal1)
        {
            return m_data.Set(GetIndex(ref blockPos), color, edgeModel, normal0, normal1);
        }

        public bool RemoveInstance(Vector3 blockPos)
        {
            return m_data.Reset(GetIndex(ref blockPos));
        }

        /// <summary>
        /// Based on block position and edge position, calculated block index from 0 to 4 (no more than for blocks shares edge)
        /// </summary>
        int GetIndex(ref Vector3 blockPos)
        {
            const float epsilon = 0.00001f;
            var pos = blockPos - new Vector3(LocalOrthoMatrix);
            if (Math.Abs(pos.X) < epsilon)
            {
                return (pos.Y > 0 ? 1 : 0) + (pos.Z > 0 ? 2 : 0);
            }
            else if (Math.Abs(pos.Y) < epsilon)
            {
                return (pos.X > 0 ? 1 : 0) + (pos.Z > 0 ? 2 : 0);
            }
            else
            {
                Debug.Assert(Math.Abs(pos.Z) < epsilon);
                return (pos.X > 0 ? 1 : 0) + (pos.Y > 0 ? 2 : 0);
            }
        }

        public bool GetNormalInfo(int index, out Color color, out MyStringHash edgeModel, out Base27Directions.Direction normal0, out Base27Directions.Direction normal1)
        {
            m_data.Get(index, out color, out edgeModel, out normal0, out normal1);
            color.A = 0;
            return normal0 != 0;
        }
    }
}

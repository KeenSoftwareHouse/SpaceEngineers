using System;
using System.Runtime.InteropServices;

namespace Sandbox.Engine.Voxels
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyVoxelTriangle
    {
        public ushort VertexIndex0;
        public ushort VertexIndex1;
        public ushort VertexIndex2;

        public ushort this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return VertexIndex0;
                    case 1:
                        return VertexIndex1;
                    case 2:
                        return VertexIndex2;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            set
            {
                switch (i)
                {
                    case 0:
                        VertexIndex0 = value;
                        break;
                    case 1:
                        VertexIndex1 = value;
                        break;
                    case 2:
                        VertexIndex2 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public override string ToString()
        {
            return "{" + VertexIndex0 + ", " + VertexIndex1 + ", " + VertexIndex2 + "}";
        }
    }
}

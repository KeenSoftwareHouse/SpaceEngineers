using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D9;

namespace VRageRender
{
    partial class MySortedElements
    {
        class MeshMaterialComparer : IEqualityComparer<MyRenderMeshMaterial>
        {
            public bool Equals(MyRenderMeshMaterial x, MyRenderMeshMaterial y)
            {
                // Can't compare members, that would kill performance.
                // On hash collision, we're screwed (render artifacts)
                // Solution would be to use global list of unique materials and then we can do object.ReferenceEquals
                return x.HashCode == y.HashCode;
            }

            public int GetHashCode(MyRenderMeshMaterial obj)
            {
                return obj.HashCode;
            }
        }

        class VertexBufferComparer : IEqualityComparer<VertexBuffer>
        {
            public bool Equals(VertexBuffer x, VertexBuffer y)
            {
                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(VertexBuffer obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}

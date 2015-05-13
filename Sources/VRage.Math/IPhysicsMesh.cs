using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Havok
{
    /// <summary>
    /// Interface through which physics can work with graphical meshes.
    /// This has been moved here due to problems with obfuscator and to reduce dependencies of Havok!
    /// You should treat this as part of HavokWrapper (not accessible when there is no reference to HavokWrapper)!
    /// Exception to this can only be rare cases such as avoiding code duplication.
    /// </summary>
    public interface IPhysicsMesh
    {
        void SetAABB(Vector3 min, Vector3 max);
        void AddSectionData(int indexStart, int triCount, String materialName);
        void AddIndex(int index);
        void AddVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord);

        int GetSectionsCount();
        bool GetSectionData(int idx, ref int indexStart, ref int triCount, ref String matIdx);

        int GetIndicesCount();
        int GetIndex(int idx);

        int GetVerticesCount();
        bool GetVertex(int vertexId, ref Vector3 position, ref Vector3 normal, ref Vector3 tangent, ref Vector2 texCoord);

        void Transform(Matrix m);
    }
}

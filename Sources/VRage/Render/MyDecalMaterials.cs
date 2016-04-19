using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace VRageRender
{
    public static class MyDecalMaterials
    {
        private static Dictionary<MyDecalMaterialId, MyDecalMaterial> m_decalMaterials = new Dictionary<MyDecalMaterialId, MyDecalMaterial>();

        public static void AddDecalMaterial(MyDecalMaterial decalMaterial)
        {
            var decalMatId = new MyDecalMaterialId() { Target = decalMaterial.Target.String, Source = decalMaterial.Source.String };
            m_decalMaterials[decalMatId] = decalMaterial;
        }

        public static bool TryGetDecalMaterial(string target, string source, out MyDecalMaterial decalMaterial)
        {
            var decalMatId = new MyDecalMaterialId() { Target = target, Source = source };
            return TryGetDecalMaterial(decalMatId, out decalMaterial);
        }

        private static bool TryGetDecalMaterial(MyDecalMaterialId decalMatId, out MyDecalMaterial decalMaterial)
        {
            bool found = m_decalMaterials.TryGetValue(decalMatId, out decalMaterial);
            if (found)
                return true;

            if (decalMatId.Target != String.Empty)
            {
                // First fallback: try to find a source specific decal material
                MyDecalMaterialId temp = decalMatId;
                temp.Target = String.Empty;
                found = m_decalMaterials.TryGetValue(temp, out decalMaterial);
                if (found)
                    return true;
            }

            if (decalMatId.Source != String.Empty)
            {
                // First fallback: try to find a target specific decal material
                MyDecalMaterialId temp = decalMatId;
                temp.Source = String.Empty;
                found = m_decalMaterials.TryGetValue(temp, out decalMaterial);
                if (found)
                    return true;
            }

            return false;
        }
    }
}

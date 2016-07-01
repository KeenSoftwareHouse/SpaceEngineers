using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace VRageRender
{
    public static class MyDecalMaterials
    {
        private static Dictionary<string, List<MyDecalMaterial>> m_decalMaterials = new Dictionary<string, List<MyDecalMaterial>>();

        public static void AddDecalMaterial(MyDecalMaterial decalMaterial)
        {
            List<MyDecalMaterial> materials;
            bool found = m_decalMaterials.TryGetValue(decalMaterial.StringId, out materials);
            if (!found)
            {
                materials = new List<MyDecalMaterial>();
                m_decalMaterials[decalMaterial.StringId] = materials;
            }

            materials.Add(decalMaterial);
        }

        public static void ClearMaterials()
        {
            m_decalMaterials.Clear();
        }

        public static bool TryGetDecalMaterial(string source, string target, out IReadOnlyList<MyDecalMaterial> decalMaterials)
        {
            List<MyDecalMaterial> temp;
            bool found = TryGetDecalMateriald(source, target, out temp);
            decalMaterials = temp;
            return found;
        }

        private static bool TryGetDecalMateriald(string source, string target, out List<MyDecalMaterial> decalMaterial)
        {
            string decalMatId = GetStringId(source, target);
            return m_decalMaterials.TryGetValue(decalMatId, out decalMaterial);
        }

        public static string GetStringId(string source, string target)
        {
            return (String.IsNullOrEmpty(source) ? "NULL" : source) + "_"
                + (String.IsNullOrEmpty(target) ? "NULL" : target);
        }

        public static string GetStringId(MyStringHash source, MyStringHash target)
        {
            return (source == MyStringHash.NullOrEmpty ? "NULL" : source.String) + "_"
                + (target == MyStringHash.NullOrEmpty ? "NULL" : target.String);
        }
    }
}

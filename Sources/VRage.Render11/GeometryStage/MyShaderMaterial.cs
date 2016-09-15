using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyShaderMaterial
    {
        internal int m_id;

        static Dictionary<int, string> m_map = new Dictionary<int, string>();
        static Dictionary<string, MyShaderMaterial> m_cached = new Dictionary<string, MyShaderMaterial>();

        private MyShaderMaterial() { }

        internal static void ClearCache()
        {
            m_cached.Clear();
        }

        internal static MyShaderMaterial GetOrCreate(string name)
        {
            MyShaderMaterial cached;
            if (!m_cached.TryGetValue(name, out cached))
            {
                cached = new MyShaderMaterial();
                cached.m_id = m_cached.Count;
                m_map[cached.m_id] = name;
                m_cached[name] = cached;
            }

            return cached;
        }

        internal static string GetNameByID(int id)
        {
            return m_map.Get(id);
        }

        internal static int GetID(string path)
        {
            var entry = GetOrCreate(path);
            return entry != null ? entry.m_id : 0; 
        }
    }
}

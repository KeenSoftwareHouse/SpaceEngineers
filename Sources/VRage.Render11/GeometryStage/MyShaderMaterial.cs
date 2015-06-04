using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;

namespace VRageRender
{
    

    class MyShaderMaterial
    {
        internal string m_declarationsSrc;
        internal string m_vertexProgramSrc;
        internal string m_pixelProgramSrc;
        internal int m_id;

        static Dictionary<int, string> m_map = new Dictionary<int, string>();
        static Dictionary<string, MyShaderMaterial> m_cached = new Dictionary<string, MyShaderMaterial>();

        internal static void ClearCache()
        {
            m_cached.Clear();
        }

        internal static MyShaderMaterial GetOrCreate(string name)
        {
            MyShaderMaterial cached;
            bool rebuild = false;
            if (!m_cached.TryGetValue(name, out cached))
            {
                cached = new MyShaderMaterial {m_id = m_cached.Count};
                m_map[cached.m_id] = name;
                rebuild = true;
                m_cached[name] = cached;
            }

            if(rebuild)
            {
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, "Shaders/materials", name), "declarations.h"))
                {
                    cached.m_declarationsSrc = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, "Shaders/materials", name), "vertex.h"))
                {
                    cached.m_vertexProgramSrc = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, "Shaders/materials", name), "pixel.h"))
                {
                    cached.m_pixelProgramSrc = new StreamReader(stream).ReadToEnd();
                }
            }

            return cached;
        }

        internal static string DeclarationsSrc(string path)
        {
            var entry = GetOrCreate(path);
            return entry != null ? entry.m_declarationsSrc : null;
        }

        internal static string VertexProgramSrc(string path)
        {
            var entry = GetOrCreate(path);
            return entry != null ? entry.m_vertexProgramSrc : null;
        }

        internal static string PixelProgramSrc(string path)
        {
            var entry = GetOrCreate(path);
            return entry != null ? entry.m_pixelProgramSrc : null;
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

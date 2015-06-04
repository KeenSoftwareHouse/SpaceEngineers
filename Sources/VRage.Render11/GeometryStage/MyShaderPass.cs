using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;

namespace VRageRender
{
    class MyShaderPass
    {
        internal string m_vertexStageSrc;
        internal string m_pixelStageSrc;

        static Dictionary<string, MyShaderPass> m_cached = new Dictionary<string, MyShaderPass>();

        internal static void ClearCache()
        {
            m_cached.Clear();
        }

        internal static MyShaderPass GetOrCreate(string tag)
        {
            MyShaderPass cached;
            bool rebuild = false;
            if (!m_cached.TryGetValue(tag, out cached))
            {
                cached = new MyShaderPass();
                rebuild = true;
                m_cached[tag] = cached;
            }

            if(rebuild)
            {
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, "Shaders/passes", tag), "vertex_stage.hlsl"))
                {
                    cached.m_vertexStageSrc = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, "Shaders/passes", tag), "pixel_stage.hlsl"))
                {
                    cached.m_pixelStageSrc = new StreamReader(stream).ReadToEnd();
                }
            }

            return cached;
        }

        internal static string VertexStageSrc(string tag)
        {
            var entry = GetOrCreate(tag);
            return entry != null ? entry.m_vertexStageSrc : null;
        }

        internal static string PixelStageSrc(string tag)
        {
            var entry = GetOrCreate(tag);
            return entry != null ? entry.m_pixelStageSrc : null;
        }
    }
}

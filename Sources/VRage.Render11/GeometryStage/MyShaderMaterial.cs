using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using VRage.FileSystem;
using VRage.Utils;
using System.IO;
using SharpDX.D3DCompiler;
using VRage.Library.Utils;

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
                cached = new MyShaderMaterial();
                cached.m_id = m_cached.Count;
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

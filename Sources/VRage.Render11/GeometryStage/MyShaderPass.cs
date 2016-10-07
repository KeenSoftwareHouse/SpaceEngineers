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

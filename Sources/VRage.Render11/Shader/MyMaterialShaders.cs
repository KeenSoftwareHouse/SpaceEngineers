using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;


namespace VRageRender
{
    struct MyMaterialShadersBundleId
    {
        internal int Index;

        public static bool operator ==(MyMaterialShadersBundleId x, MyMaterialShadersBundleId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MyMaterialShadersBundleId x, MyMaterialShadersBundleId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MyMaterialShadersBundleId NULL = new MyMaterialShadersBundleId { Index = -1 };

        internal InputLayout IL { get { return MyMaterialShaders.Bundles[Index].IL; } }
        internal VertexShader VS { get { return MyMaterialShaders.Bundles[Index].VS; } }
        internal PixelShader PS { get { return MyMaterialShaders.Bundles[Index].PS; } }
    }

    struct MyMaterialShadersInfo
    {
        internal MyStringId Material;
        internal MyStringId Pass;
        internal VertexLayoutId Layout;
        internal MyShaderUnifiedFlags Flags;
        internal string Name { get { return String.Format("[{0}][{1}]_{2}", Pass.ToString(), Material.ToString(), Flags); } }
    }

    struct MyMaterialShadersBundle
    {
        internal InputLayout IL;
        internal VertexShader VS;
        internal PixelShader PS;
    }

    struct MyMaterialShaderInfo
    {
        internal string Declarations;
        internal string VertexShaderSource;
        internal string PixelShaderSource;
    }

    struct MyMaterialPassInfo
    {
        internal string VertexStageTemplate;
        internal string PixelStageTemplate;
    }

    static class MyMaterialShaders
    {
        internal static void AddMaterialShaderFlagMacros(StringBuilder sb, MyShaderUnifiedFlags flags)
        {
            if ((flags & MyShaderUnifiedFlags.DEPTH_ONLY) > 0)
            {
                sb.AppendLine("#define DEPTH_ONLY");
            }
            if ((flags & MyShaderUnifiedFlags.ALPHAMASK) > 0)
            {
                sb.AppendLine("#define ALPHA_MASKED");
            }
            if ((flags & MyShaderUnifiedFlags.TRANSPARENT) > 0)
            {
                sb.AppendLine("#define TRANSPARENT");
            }
            if ((flags & MyShaderUnifiedFlags.DITHERED) > 0)
            {
                sb.AppendLine("#define DITHERED");
            }
            if ((flags & MyShaderUnifiedFlags.FOLIAGE) > 0)
            {
                sb.AppendLine("#define FOLIAGE");
            }
            if ((flags & MyShaderUnifiedFlags.USE_SKINNING) > 0)
            {
                sb.AppendLine("#define USE_SKINNING");
            }
            if ((flags & MyShaderUnifiedFlags.USE_CUBE_INSTANCING) > 0)
            {
                sb.AppendLine("#define USE_CUBE_INSTANCING");
            }
            if ((flags & MyShaderUnifiedFlags.USE_DEFORMED_CUBE_INSTANCING) > 0)
            {
                sb.AppendLine("#define USE_DEFORMED_CUBE_INSTANCING");
            }
            if ((flags & MyShaderUnifiedFlags.USE_GENERIC_INSTANCING) > 0)
            {
                sb.AppendLine("#define USE_GENERIC_INSTANCING");
            }
            if ((flags & MyShaderUnifiedFlags.USE_MERGE_INSTANCING) > 0)
            {
                sb.AppendLine("#define USE_MERGE_INSTANCING");
            }
            if ((flags & MyShaderUnifiedFlags.USE_VOXEL_MORPHING) > 0)
            {
                sb.AppendLine("#define USE_VOXEL_MORPHING");
            }
        }

        static Dictionary<MyStringId, MyMaterialShaderInfo> MaterialSources = new Dictionary<MyStringId, MyMaterialShaderInfo>(MyStringId.Comparer);
        static Dictionary<MyStringId, MyMaterialPassInfo> MaterialPassSources = new Dictionary<MyStringId, MyMaterialPassInfo>(MyStringId.Comparer);

        static Dictionary<int, MyMaterialShadersBundleId> HashIndex = new Dictionary<int,MyMaterialShadersBundleId>();
        static MyFreelist<MyMaterialShadersInfo> BundleInfo = new MyFreelist<MyMaterialShadersInfo>(64);
        internal static MyMaterialShadersBundle[] Bundles = new MyMaterialShadersBundle [64];

        static string m_vertexTemplateBase;
        static string m_pixelTemplateBase;

        static MyMaterialShaders()
        {
            LoadTemplates();
        }

        static void LoadTemplates()
        {
            using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "vertex_template_base.h")))
            {
                m_vertexTemplateBase = new StreamReader(stream).ReadToEnd();
            }

            using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "pixel_template_base.h")))
            {
                m_pixelTemplateBase = new StreamReader(stream).ReadToEnd();
            }
        }

        internal static MyMaterialShadersBundleId Get(MyStringId material, MyStringId materialPass, VertexLayoutId vertexLayout, MyShaderUnifiedFlags flags)
        {
            int hash = 0;
            MyHashHelper.Combine(ref hash, material.GetHashCode());
            MyHashHelper.Combine(ref hash, materialPass.GetHashCode());
            MyHashHelper.Combine(ref hash, vertexLayout.GetHashCode());
            MyHashHelper.Combine(ref hash, unchecked((int)flags));

            if(HashIndex.ContainsKey(hash))
            {
                return HashIndex[hash];
            }

            var id = new MyMaterialShadersBundleId { Index = BundleInfo.Allocate() };
            MyArrayHelpers.Reserve(ref Bundles, id.Index + 1);

            HashIndex[hash] = id;
            BundleInfo.Data[id.Index] = new MyMaterialShadersInfo
            {
                Material = material,
                Pass = materialPass,
                Layout = vertexLayout,
                Flags = flags
            };
            Bundles[id.Index] = new MyMaterialShadersBundle { };

            InitBundle(id);

            return id;
        }

        internal static void Recompile()
        {
            LoadTemplates();
            MaterialSources.Clear();
            MaterialPassSources.Clear();

            foreach (var id in HashIndex.Values)
            {
                InitBundle(id);
            }
        }

        static void PrefetchMaterialSources(MyStringId id)
        {
            if(!MaterialSources.ContainsKey(id))
            {
                var info = new MyMaterialShaderInfo();

                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "materials", id.ToString()), "declarations.h"))
                {
                    info.Declarations = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "materials", id.ToString()), "vertex.h"))
                {
                    info.VertexShaderSource = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "materials", id.ToString()), "pixel.h"))
                {
                    info.PixelShaderSource = new StreamReader(stream).ReadToEnd();
                }

                MaterialSources[id] = info;
            }
        }

        static void PrefetchPassSources(MyStringId id)
        {
            if (!MaterialPassSources.ContainsKey(id))
            {
                var info = new MyMaterialPassInfo();

                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "passes", id.ToString()), "vertex_stage.hlsl"))
                {
                    info.VertexStageTemplate = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShaders.ShadersContentPath, "passes", id.ToString()), "pixel_stage.hlsl"))
                {
                    info.PixelStageTemplate = new StreamReader(stream).ReadToEnd();
                }

                MaterialPassSources[id] = info;
            }
        }

        static void InitBundle(MyMaterialShadersBundleId id)
        {
            Debug.Assert(m_vertexTemplateBase != null);
            Debug.Assert(m_pixelTemplateBase != null);

            var info = BundleInfo.Data[id.Index];

            PrefetchMaterialSources(info.Material);
            PrefetchPassSources(info.Pass);

            // vertex shader

            StringBuilder source = new StringBuilder();
            source.Append(MyRender11.GlobalShaderHeader);
            AddMaterialShaderFlagMacros(source, info.Flags);
            source.Append(m_vertexTemplateBase);
            source.Replace("__VERTEXINPUT_DECLARATIONS__",
                info.Layout.Info.SourceDeclarations);
            source.Replace("__VERTEXINPUT_TRANSFER__",
                info.Layout.Info.SourceDataMove);
            source.Replace("__MATERIAL_DECLARATIONS__",
                MaterialSources[info.Material].Declarations);
            source.Replace("__MATERIAL_VERTEXPROGRAM__",
                MaterialSources[info.Material].VertexShaderSource);
            source.AppendLine();
            source.AppendLine(MaterialPassSources[info.Pass].VertexStageTemplate);

            var vsName = String.Format("[{0}][{1}]_{2}_{3}", info.Pass.ToString(), info.Material.ToString(), "vs", info.Flags);

            var vsSource = source.ToString();

            source.Clear();

            // pixel shader

            source.Append(MyRender11.GlobalShaderHeader);
            AddMaterialShaderFlagMacros(source, info.Flags);
            source.Append(m_pixelTemplateBase);
            source.Replace("__MATERIAL_DECLARATIONS__", MaterialSources[info.Material].Declarations);
            source.Replace("__MATERIAL_PIXELPROGRAM__", MaterialSources[info.Material].PixelShaderSource);
            source.AppendLine();
            source.AppendLine(MaterialPassSources[info.Pass].PixelStageTemplate);

            
            var psName = String.Format("[{0}][{1}]_{2}_{3}", info.Pass.ToString(), info.Material.ToString(), "ps", info.Flags);
            var psSource = source.ToString();


            var vsBytecode = MyShaders.Compile(vsSource, "__vertex_shader", "vs_5_0", vsName, false);
            var psBytecode = MyShaders.Compile(psSource, "__pixel_shader", "ps_5_0", psName, false);


            // input layous

            bool canChangeBundle = vsBytecode != null && psBytecode != null;
            if(canChangeBundle)
            {
                if (Bundles[id.Index].IL != null)
                {
                    Bundles[id.Index].IL.Dispose();
                    Bundles[id.Index].IL = null;
                }
                if (Bundles[id.Index].VS != null)
                {
                    Bundles[id.Index].VS.Dispose();
                    Bundles[id.Index].VS = null;
                }
                if (Bundles[id.Index].PS != null)
                {
                    Bundles[id.Index].PS.Dispose();
                    Bundles[id.Index].PS = null;
                }

                try
                { 
                    Bundles[id.Index].VS = new VertexShader(MyRender11.Device, vsBytecode);
                    Bundles[id.Index].PS = new PixelShader(MyRender11.Device, psBytecode);
                    Bundles[id.Index].IL = info.Layout.Elements.Length > 0 ? new InputLayout(MyRender11.Device, vsBytecode, info.Layout.Elements) : null;
                }
                catch(SharpDXException e)
                {
                    vsBytecode = MyShaders.Compile(vsSource, "__vertex_shader", "vs_5_0", vsName, true);
                    psBytecode = MyShaders.Compile(psSource, "__pixel_shader", "ps_5_0", psName, true);

                    Bundles[id.Index].VS = new VertexShader(MyRender11.Device, vsBytecode);
                    Bundles[id.Index].PS = new PixelShader(MyRender11.Device, psBytecode);
                    Bundles[id.Index].IL = info.Layout.Elements.Length > 0 ? new InputLayout(MyRender11.Device, vsBytecode, info.Layout.Elements) : null;
                }
            }
            else if (Bundles[id.Index].VS == null && Bundles[id.Index].PS == null)
            {
                MyRender11.Log.WriteLine("Failed to compile material shader" + info.Name + " for vertex " + String.Join(", ", info.Layout.Info.Components.Select(x => x.ToString())));
                throw new MyRenderException("Failed to compile material shader" + info.Name, MyRenderExceptionEnum.Unassigned);
            }
        }

        internal static void OnDeviceEnd()
        {
            foreach(var id in HashIndex.Values)
            {
                if (Bundles[id.Index].IL != null)
                {
                    Bundles[id.Index].IL.Dispose();
                    Bundles[id.Index].IL = null;
                }
                if (Bundles[id.Index].VS != null)
                {
                    Bundles[id.Index].VS.Dispose();
                    Bundles[id.Index].VS = null;
                }
                if (Bundles[id.Index].PS != null)
                {
                    Bundles[id.Index].PS.Dispose();
                    Bundles[id.Index].PS = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            foreach (var id in HashIndex.Values)
            {
                InitBundle(id);
            }
        }
    }
}

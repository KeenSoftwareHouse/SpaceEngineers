using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using VRage;
using VRage.FileSystem;
using VRage.Utils;


namespace VRageRender
{
    [Flags]
    enum MyShaderUnifiedFlags
    {
        NONE = 0,
        DEPTH_ONLY = 1 << 0,

        // only one!
        ALPHA_MASKED = 1 << 1,
        TRANSPARENT = 1 << 2,
        DITHERED = 1 << 3,
        USE_SHADOW_CASCADES = 1 << 4,
        ALPHA_MASK_ARRAY = 1 << 5,
        DITHERED_LOD = 1 << 6,

        USE_SKINNING = 1 << 7,
        USE_VOXEL_DATA = 1 << 8,
        USE_VOXEL_MORPHING = 1 << 9,

        // only one!
        USE_CUBE_INSTANCING = 1 << 10,
        USE_DEFORMED_CUBE_INSTANCING = 1 << 11,
        USE_GENERIC_INSTANCING = 1 << 12,
        USE_MERGE_INSTANCING = 1 << 13,
    }
    
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
        internal static List<ShaderMacro> GenerateMaterialShaderFlagMacros(MyShaderUnifiedFlags flags)
        {
            var list = new List<ShaderMacro>();
            if ((flags & MyShaderUnifiedFlags.DEPTH_ONLY) > 0)
                list.Add(new ShaderMacro("DEPTH_ONLY", null));
            if ((flags & MyShaderUnifiedFlags.ALPHA_MASKED) > 0)
                list.Add(new ShaderMacro("ALPHA_MASKED", null));
            if ((flags & MyShaderUnifiedFlags.ALPHA_MASK_ARRAY) > 0)
                list.Add(new ShaderMacro("ALPHA_MASK_ARRAY", null));
            if ((flags & MyShaderUnifiedFlags.TRANSPARENT) > 0)
                list.Add(new ShaderMacro("TRANSPARENT", null));
            if ((flags & MyShaderUnifiedFlags.DITHERED) > 0)
                list.Add(new ShaderMacro("DITHERED", null));
            if ((flags & MyShaderUnifiedFlags.DITHERED_LOD) > 0)
                list.Add(new ShaderMacro("DITHERED_LOD", null));
            if ((flags & MyShaderUnifiedFlags.USE_SKINNING) > 0)
                list.Add(new ShaderMacro("USE_SKINNING", null));
            if ((flags & MyShaderUnifiedFlags.USE_CUBE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_CUBE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_DEFORMED_CUBE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_DEFORMED_CUBE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_GENERIC_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_GENERIC_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_MERGE_INSTANCING) > 0)
                list.Add(new ShaderMacro("USE_MERGE_INSTANCING", null));
            if ((flags & MyShaderUnifiedFlags.USE_VOXEL_MORPHING) == MyShaderUnifiedFlags.USE_VOXEL_MORPHING)
                list.Add(new ShaderMacro("USE_VOXEL_MORPHING", null));
            if((flags & MyShaderUnifiedFlags.USE_VOXEL_DATA) == MyShaderUnifiedFlags.USE_VOXEL_DATA)
                list.Add(new ShaderMacro("USE_VOXEL_DATA", null));
            
            return list;
        }

        static Dictionary<MyStringId, MyMaterialShaderInfo> MaterialSources = new Dictionary<MyStringId, MyMaterialShaderInfo>(MyStringId.Comparer);
        static Dictionary<MyStringId, MyMaterialPassInfo> MaterialPassSources = new Dictionary<MyStringId, MyMaterialPassInfo>(MyStringId.Comparer);

        static Dictionary<int, MyMaterialShadersBundleId> HashIndex = new Dictionary<int,MyMaterialShadersBundleId>();
        static MyFreelist<MyMaterialShadersInfo> BundleInfo = new MyFreelist<MyMaterialShadersInfo>(64);
        internal static MyMaterialShadersBundle[] Bundles = new MyMaterialShadersBundle[64];

        static string m_vertexTemplateBase;
        static string m_pixelTemplateBase;

        static MyMaterialShaders()
        {
            LoadTemplates();
        }

        static void LoadTemplates()
        {
            using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "vertex_template_base.h")))
            {
                m_vertexTemplateBase = new StreamReader(stream).ReadToEnd();
            }

            using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "pixel_template_base.h")))
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

                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "materials", id.ToString()), "declarations.h"))
                {
                    info.Declarations = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "materials", id.ToString()), "vertex.h"))
                {
                    info.VertexShaderSource = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "materials", id.ToString()), "pixel.h"))
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

                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "passes", id.ToString()), "vertex_stage.hlsl"))
                {
                    info.VertexStageTemplate = new StreamReader(stream).ReadToEnd();
                }
                using (var stream = MyFileSystem.OpenRead(Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.ShadersContentPath, "passes", id.ToString()), "pixel_stage.hlsl"))
                {
                    info.PixelStageTemplate = new StreamReader(stream).ReadToEnd();
                }

                MaterialPassSources[id] = info;
            }
        }

        static void InitBundle(MyMaterialShadersBundleId id, bool invalidateCache = false)
        {
            var info = BundleInfo.Data[id.Index];

            string vsSource;
            string psSource;
            var macroList = GenerateMaterialShaderFlagMacros(info.Flags);
            for (int i = 0; i < MyRender11.GlobalShaderMacro.Length; i++)
                macroList.Add(MyRender11.GlobalShaderMacro[i]);
            ShaderMacro[] macros = macroList.ToArray();

            ProfilerShort.Begin("MyShaders.MaterialCompile");

            Preprocess(info.Material, info.Pass, info.Layout.Info, out vsSource, out psSource);

            var descriptor = String.Format("{0}_{1}_{2}", info.Material.ToString(), info.Pass.ToString(), info.Layout.Info.Components.GetString());
            byte[] vsBytecode = MyShaders.Compile(vsSource, macros, MyShadersDefines.Profiles.vs_5_0, descriptor, invalidateCache);
            byte[] psBytecode = MyShaders.Compile(psSource, macros, MyShadersDefines.Profiles.ps_5_0, descriptor, invalidateCache);
            
            ProfilerShort.End();

            // input layous
            bool canChangeBundle = vsBytecode != null && psBytecode != null;
            if (canChangeBundle)
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
                catch (SharpDXException e)
                {
                    if (!invalidateCache)
                    {
                        InitBundle(id, true);
                        return;
                    }
                    string message = "Failed to initialize material shader" + info.Name + " for vertex " + info.Layout.Info.Components.GetString();
                    MyRender11.Log.WriteLine(message);
                    throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
                }
            }
            else if (Bundles[id.Index].VS == null && Bundles[id.Index].PS == null)
            {
                string message = "Failed to compile material shader" + info.Name + " for vertex " + info.Layout.Info.Components.GetString();
                MyRender11.Log.WriteLine(message);
                throw new MyRenderException(message, MyRenderExceptionEnum.Unassigned);
            }
        }

        internal static void Preprocess(MyStringId material, MyStringId pass, MyVertexLayoutInfo vertexLayout, out string vsSource, out string psSource)
        {
            ProfilerShort.Begin("MyShaders.MaterialPreprocess");

            Debug.Assert(m_vertexTemplateBase != null);
            Debug.Assert(m_pixelTemplateBase != null);

            PrefetchMaterialSources(material);
            PrefetchPassSources(pass);

            StringBuilder source = new StringBuilder();

            // vertex shader
            source.Append(m_vertexTemplateBase);
            source.Replace("__VERTEXINPUT_DECLARATIONS__", vertexLayout.SourceDeclarations);
            source.Replace("__VERTEXINPUT_TRANSFER__", vertexLayout.SourceDataMove);
            source.Replace("__MATERIAL_DECLARATIONS__", MaterialSources[material].Declarations);
            source.Replace("__MATERIAL_VERTEXPROGRAM__", MaterialSources[material].VertexShaderSource);
            source.AppendLine();
            source.AppendLine(MaterialPassSources[pass].VertexStageTemplate);

            vsSource = source.ToString();

            source.Clear();

            // pixel shader
            source.Append(m_pixelTemplateBase);
            source.Replace("__MATERIAL_DECLARATIONS__", MaterialSources[material].Declarations);
            source.Replace("__MATERIAL_PIXELPROGRAM__", MaterialSources[material].PixelShaderSource);
            source.AppendLine();
            source.AppendLine(MaterialPassSources[pass].PixelStageTemplate);

            psSource = source.ToString();

            ProfilerShort.End();
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

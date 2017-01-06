using SharpDX.Direct3D;
using System.Collections.Generic;
using System.Text;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Instancing;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Rendering
{
    class MyShaderBundle
    {
        public MyShaderBundle(PixelShaderId ps, VertexShaderId vs, InputLayoutId il)
        {
            PixelShader = ps;
            VertexShader = vs;
            InputLayout = il;
        }

        public PixelShaderId PixelShader { get; private set; }
        public VertexShaderId VertexShader { get; private set; }
        public InputLayoutId InputLayout { get; private set; }
    }

    class MyShaderBundleManager: IManager
    {
        struct MyShaderBundleKey
        {
            public MyRenderPassType Pass;
            public MyMeshDrawTechnique Technique;
            public bool IsCm;
            public bool IsNg;
            public bool IsExt;
            public MyInstanceLodState State;
        }

        Dictionary<MyShaderBundleKey, MyShaderBundle> m_cache = new Dictionary<MyShaderBundleKey, MyShaderBundle>();

        MyVertexInputComponent[] GetVertexInputComponents(MyRenderPassType pass)
        {
            if (pass == MyRenderPassType.GBuffer)
            {
                List<MyVertexInputComponent> listGBuffer = new List<MyVertexInputComponent>();
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.SIMPLE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.SIMPLE_INSTANCE_COLORING, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                return listGBuffer.ToArray();
            }
            else if (pass == MyRenderPassType.Depth)
            {
                List<MyVertexInputComponent> listDepth = new List<MyVertexInputComponent>();
                listDepth.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                listDepth.Add(new MyVertexInputComponent(MyVertexInputComponentType.SIMPLE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                return listDepth.ToArray();
            }
            else if (pass == MyRenderPassType.Highlight)
            {
                List<MyVertexInputComponent> listHighlight = new List<MyVertexInputComponent>();
                listHighlight.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                listHighlight.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                listHighlight.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                listHighlight.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                return listHighlight.ToArray();
            } 
            else if (pass == MyRenderPassType.Glass)
            {
                List<MyVertexInputComponent> listGBuffer = new List<MyVertexInputComponent>();
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.SIMPLE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                listGBuffer.Add(new MyVertexInputComponent(MyVertexInputComponentType.SIMPLE_INSTANCE_COLORING, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                return listGBuffer.ToArray();
            }
            
            MyRenderProxy.Error("Unknown pass");
            return null;
        }

        void AddMacrosForRenderingPass(MyRenderPassType pass, ref List<ShaderMacro> macros)
        {
            if (pass == MyRenderPassType.GBuffer)
            {
                macros.AddRange(new ShaderMacro[]
                {
                    new ShaderMacro("RENDERING_PASS", 0),
                    new ShaderMacro("USE_SIMPLE_INSTANCING", null),
                    new ShaderMacro("USE_SIMPLE_INSTANCING_COLORING", null),
                });
            }
            else if (pass == MyRenderPassType.Depth)
            {
                macros.AddRange(new ShaderMacro[]
                {
                    new ShaderMacro("RENDERING_PASS", 1),
                    new ShaderMacro("USE_SIMPLE_INSTANCING", null),
                });
            }
            else if (pass == MyRenderPassType.Highlight)
            {
                macros.AddRange(new ShaderMacro[]
                {
                    new ShaderMacro("RENDERING_PASS", 3),
                });
            }
            else if (pass == MyRenderPassType.Glass)
            {
                macros.AddRange(new ShaderMacro[]
                {
                    new ShaderMacro("RENDERING_PASS", 5), 
                    new ShaderMacro("USE_SIMPLE_INSTANCING", null),
                    new ShaderMacro("USE_SIMPLE_INSTANCING_COLORING", null),
                });
            }
            else
            {
                MyRenderProxy.Error("Unknown render pass type");
            }
        }

        void AddMacrosForTechnique(MyMeshDrawTechnique technique, bool isCm, bool isNg, bool isExt, ref List<ShaderMacro> macros)
        {
            if (technique == MyMeshDrawTechnique.MESH)
            {
                
            }
            else if (technique == MyMeshDrawTechnique.ALPHA_MASKED)
            {
                macros.Add(new ShaderMacro("ALPHA_MASKED", null));
            }
            else if (technique == MyMeshDrawTechnique.DECAL
                || technique == MyMeshDrawTechnique.DECAL_CUTOUT
                || technique == MyMeshDrawTechnique.DECAL_NOPREMULT)
            {
                if (technique == MyMeshDrawTechnique.DECAL_CUTOUT)
                    macros.Add(new ShaderMacro("STATIC_DECAL_CUTOUT", null));
                else
                    macros.Add(new ShaderMacro("STATIC_DECAL", null));

                if (isCm)
                    macros.Add(new ShaderMacro("USE_COLORMETAL_TEXTURE", null));
                if (isNg)
                    macros.Add(new ShaderMacro("USE_NORMALGLOSS_TEXTURE", null));
                if (isExt)
                    macros.Add(new ShaderMacro("USE_EXTENSION_TEXTURE", null));
            }
            else if (technique == MyMeshDrawTechnique.GLASS)
            {

            }
            else
            {
                MyRenderProxy.Error("The specific technique is not processed");
            }
        }

        void AddMacrosVertexInputComponents(MyVertexInputComponent[] components, ref List<ShaderMacro> macros)
        {
            // recycled code from MyVerteInput.cs
            var declarationBuilder = new StringBuilder();
            var sourceBuilder = new StringBuilder();
            var semanticDict = new Dictionary<string, int>();
            var elementsList = new List<InputElement>(components.Length);
            foreach (var component in components)
                MyVertexInputLayout.MapComponent[component.Type].AddComponent(component, elementsList, semanticDict, declarationBuilder, sourceBuilder);

            macros.Add(new ShaderMacro("VERTEX_COMPONENTS_DECLARATIONS", declarationBuilder));
            macros.Add(new ShaderMacro("TRANSFER_VERTEX_COMPONENTS", sourceBuilder));
        }

        void AddMacrosState(MyInstanceLodState state, ref List<ShaderMacro> macros)
        {
            if (state == MyInstanceLodState.Solid)
            {
                // nothing
            }
            else if (state == MyInstanceLodState.Transition)
            {
                macros.Add(new ShaderMacro("DITHERED", null));
                macros.Add(new ShaderMacro("DITHERED_LOD", null));
            }
            else if (state == MyInstanceLodState.Hologram)
            {
                // this is not good, but nothing...
            }
            else if (state == MyInstanceLodState.Dithered)
            {
                macros.Add(new ShaderMacro("DITHERED", null));
            }
        }

        enum MyShaderType
        {
            SHADER_TYPE_VERTEX,
            SHADER_TYPE_PIXEL,
        }

        string GetShaderFilepath(MyMeshDrawTechnique technique, MyShaderType type)
        {
            switch (technique)
            {
                case MyMeshDrawTechnique.MESH:
                case MyMeshDrawTechnique.DECAL:
                case MyMeshDrawTechnique.DECAL_CUTOUT:
                case MyMeshDrawTechnique.DECAL_NOPREMULT:
                case MyMeshDrawTechnique.GLASS:
                    if (type == MyShaderType.SHADER_TYPE_VERTEX)
                        return "Geometry\\Materials\\Standard\\Vertex.hlsl";
                    else if (type == MyShaderType.SHADER_TYPE_PIXEL)
                        return "Geometry\\Materials\\Standard\\Pixel.hlsl";
                    else
                        MyRenderProxy.Error("Unresolved condition");
                    return "";
                case MyMeshDrawTechnique.ALPHA_MASKED:
                    if (type == MyShaderType.SHADER_TYPE_VERTEX)
                        return "Geometry\\Materials\\AlphaMasked\\Vertex.hlsl";
                    else if (type == MyShaderType.SHADER_TYPE_PIXEL)
                        return "Geometry\\Materials\\AlphaMasked\\Pixel.hlsl";
                    return "";
                default:
                    MyRenderProxy.Error("Unknown technique");
                    return "";
            }
        }

        void AddDebugNameSuffix(ref StringBuilder builder, MyRenderPassType pass, MyMeshDrawTechnique technique, List<ShaderMacro> macros)
        {
            builder.Append(pass);
            builder.Append("_");
            builder.Append(technique);
            builder.Append("_");
            foreach (var macro in macros)
            {
                if (string.IsNullOrEmpty(macro.Definition))
                    builder.Append(macro.Name);
                else
                {
                    builder.Append(macro.Name);
                    builder.Append("=");
                    builder.Append(macro.Definition);
                }
                builder.Append(";");
            }
        }

        string GetVsDebugName(MyRenderPassType pass, MyMeshDrawTechnique technique, List<ShaderMacro> macros)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("GeoVsNew_");
            AddDebugNameSuffix(ref builder, pass, technique, macros);
            return builder.ToString();
        }

        string GetPsDebugName(MyRenderPassType pass, MyMeshDrawTechnique technique, List<ShaderMacro> macros)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("GeoPsNew_");
            AddDebugNameSuffix(ref builder, pass, technique, macros);
            return builder.ToString();
        }

        public MyShaderBundle GetShaderBundle(MyRenderPassType pass, MyMeshDrawTechnique technique, MyInstanceLodState state, bool isCm, bool isNg, bool isExt)
        {
            // Modify input:
            switch (technique)
            {
                case MyMeshDrawTechnique.DECAL:
                case MyMeshDrawTechnique.DECAL_CUTOUT:
                case MyMeshDrawTechnique.DECAL_NOPREMULT:
                    break;
                default:
                    isCm = true;
                    isNg = true;
                    isExt = true;
                    break;
            }

            MyShaderBundleKey key = new MyShaderBundleKey
            {
                Pass = pass,
                Technique = technique,
                IsCm = isCm,
                IsNg = isNg,
                IsExt = isExt,
                State = state,
            };

            if (m_cache.ContainsKey(key))
                return m_cache[key];

            MyVertexInputComponent[] viComps = GetVertexInputComponents(pass);
            VertexLayoutId vl = MyVertexLayouts.GetLayout(viComps);
            string vsFilepath = GetShaderFilepath(technique, MyShaderType.SHADER_TYPE_VERTEX);
            string psFilepath = GetShaderFilepath(technique, MyShaderType.SHADER_TYPE_PIXEL);
            List<ShaderMacro> macros = new List<ShaderMacro>();
            AddMacrosForRenderingPass(pass, ref macros);
            AddMacrosForTechnique(technique, isCm, isNg, isExt, ref macros);
            AddMacrosVertexInputComponents(viComps, ref macros);
            AddMacrosState(state, ref macros);

            VertexShaderId vs = MyShaders.CreateVs(vsFilepath, macros.ToArray());
            ((VertexShader)vs).DebugName = GetVsDebugName(pass, technique, macros);
            PixelShaderId ps = MyShaders.CreatePs(psFilepath, macros.ToArray());
            ((PixelShader)ps).DebugName = GetPsDebugName(pass, technique, macros); ;
            InputLayoutId il = MyShaders.CreateIL(vs.BytecodeId, vl);
            MyShaderBundle shaderBundle = new MyShaderBundle(ps, vs, il);

            m_cache.Add(key, shaderBundle);
            return shaderBundle;
        }
    }
}

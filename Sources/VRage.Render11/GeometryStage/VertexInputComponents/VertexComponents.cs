using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;
using System.Text;

namespace VRageRender
{
    #region Components

    internal abstract class MyComponent
    {
        private static int NextIndex(Dictionary<string, int> dict, string name)
        {
            int val = 0;
            if (dict.TryGetValue(name, out val))
            {
                dict[name] = val + 1;
            }
            else
            {
                dict[name] = 1;
            }

            return val;
        }

        public static ShaderMacro[] GetComponentMacros(string declaration, string transferCode, MyVertexInputComponent[] components)
        {
            ShaderMacro macroDecl = new ShaderMacro("VERTEX_COMPONENTS_DECLARATIONS", declaration);
            ShaderMacro macroTransfer = new ShaderMacro("TRANSFER_VERTEX_COMPONENTS", transferCode);
            bool isTexIndices = false;
            foreach (var comp in components)
            {
                if (comp.Type == MyVertexInputComponentType.TEXINDICES)
                    isTexIndices = true;
            }
            if (isTexIndices)
                return new ShaderMacro[] {macroDecl, macroTransfer, new ShaderMacro("USE_TEXTURE_INDICES", null), };
            else
                return new ShaderMacro[] { macroDecl, macroTransfer };
        }

        protected static void AddSingle(string name, string variable, Format format, MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict, StringBuilder declaration)
        {
            var classification = component.Freq == MyVertexInputComponentFreq.PER_VERTEX ? InputClassification.PerVertexData : InputClassification.PerInstanceData;
            var freq = component.Freq == MyVertexInputComponentFreq.PER_VERTEX ? 0 : 1;
            var index = NextIndex(dict, name);

            list.Add(new InputElement(name, index, format, InputElement.AppendAligned, component.Slot, classification, freq));
            declaration.AppendFormat("{0} : {1}{2};\\\n", variable, name, index);
        }

        internal abstract void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code);
    }

    internal sealed class MyPositionPackedComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float4 position", Format.R16G16B16A16_Float, component, list, dict, declaration);

            code.Append("__position_object = unpack_position_and_scale(input.position);\\\n");
        }
    }

    internal sealed class MyPosition2Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float2 position", Format.R32G32_Float, component, list, dict, declaration);

            code.Append("__position_object = float4(input.position, 0, 1);\\\n");
        }
    }

    internal sealed class MyPosition3Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float3 position", Format.R32G32B32_Float, component, list, dict, declaration);

            code.Append("__position_object = float4(input.position, 1);\\\n");
        }
    }

    internal sealed class MyPosition4Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float4 position", Format.R32G32B32A32_Float, component, list, dict, declaration);

            code.Append("__position_object = input.position;\\\n");
        }
    }

    internal sealed class MyPosition4HalfComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float3 position", Format.R16G16B16A16_Float, component, list, dict, declaration);

            code.Append("__position_object = float4(input.position, 1);\\\n");
        }
    }

    internal sealed class MyVoxelPositionMaterialComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float4 position", Format.R16G16B16A16_UNorm, component, list, dict, declaration);
            AddSingle("POSITION", "float4 position_morph", Format.R16G16B16A16_UNorm, component, list, dict, declaration);
            AddSingle("BLENDINDICES", "uint4 mat_info", Format.R8G8B8A8_UInt, component, list, dict, declaration);

            code.Append("__position_object = unpack_voxel_position(input.position);\\\n");
            code.Append("__material_weights = unpack_voxel_weights(input.position.w);\\\n");
            code.Append("__colorBrightnessFactor = unpack_voxel_ao(input.position.w);\\\n");
            code.Append("__position_object_morph = unpack_voxel_position(input.position_morph);\\\n");
            code.Append("__material_weights_morph = unpack_voxel_weights(input.position_morph.w);\\\n");
            code.Append("__colorBrightnessFactor_morph = unpack_voxel_ao(input.position_morph.w);\\\n");
            code.Append("__triplanar_mat_info = input.mat_info;\\\n");
        }
    }

    internal sealed class MyVoxelNormalComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("NORMAL", "uint2 normal", Format.R16G16_UInt, component, list, dict, declaration);
            AddSingle("NORMAL", "uint2 normal_morph", Format.R16G16_UInt, component, list, dict, declaration);

            code.Append("__normal = unpack_normal(input.normal);\\\n");
            code.Append("__normal_morph = unpack_normal(input.normal_morph);\\\n");
        }
    }

    internal sealed class MyTexcoord0HalfComponent : MyComponent 
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float2 texcoord0", Format.R16G16_Float, component, list, dict, declaration);

            code.Append("__texcoord0 = input.texcoord0;\\\n");
        }
    }

    internal sealed class MyTexcoord0Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float2 texcoord0", Format.R32G32_Float, component, list, dict, declaration);

            code.Append("__texcoord0 = input.texcoord0;\\\n");
        }
    }

    internal sealed class MyTexIndicesComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "uint4 texIndices", Format.R8G8B8A8_UInt, component, list, dict, declaration);

            code.Append("__texIndices = (float4) input.texIndices;\\\n");
        }
    }

    internal sealed class MyNormalComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("NORMAL", "uint2 normal", Format.R16G16_UInt, component, list, dict, declaration);

            code.Append("__normal = unpack_normal(input.normal);\\\n");
        }
    }

    internal sealed class MyTangentBitanSgnComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TANGENT", "float4 tangent4", Format.R8G8B8A8_UNorm, component, list, dict, declaration);

            code.Append("__tangent = unpack_tangent_sign(input.tangent4);\\\n");
        }
    }

    internal sealed class MyBlendWeightsComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("BLENDWEIGHT", "float4 blend_weights", Format.R16G16B16A16_Float, component, list, dict, declaration);

            code.Append("__blend_weights = input.blend_weights;\\\n");
        }
    }

    internal sealed class MyBlendIndicesComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("BLENDINDICES", "uint4 blend_indices", Format.R8G8B8A8_UInt, component, list, dict, declaration);

            code.Append("__blend_indices = input.blend_indices;\\\n");
        }
    }

    internal sealed class MyColor4Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("COLOR", "float4 color", Format.R8G8B8A8_UNorm, component, list, dict, declaration);

            code.Append("__color = input.color;\\\n");
        }
    }

    internal sealed class MyCustom4_0Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom4_0", Format.R32G32B32A32_Float, component, list, dict, declaration);
        }
    }

    internal sealed class MyCustomHalf4_0Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_half4_0", Format.R16G16B16A16_Float, component, list, dict, declaration);
        }
    }

    internal sealed class MyCustomHalf4_1Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_half4_1", Format.R16G16B16A16_Float, component, list, dict, declaration);
        }
    }

    internal sealed class MyCustomHalf4_2Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_half4_2", Format.R16G16B16A16_Float, component, list, dict, declaration);
        }
    }

    internal sealed class MyCustomUnorm4_0Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_unorm4_0", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
        }
    }

    internal sealed class MyCustomUnorm4_1Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_unorm4_1", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
        }
    }

    internal sealed class MyCubeInstanceComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 packed_bone0", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone1", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone2", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone3", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone4", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone5", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone6", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 packed_bone7", Format.R8G8B8A8_UNorm, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 cube_transformation", Format.R32G32B32A32_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 colormask", Format.R32G32B32A32_Float, component, list, dict, declaration);

            code.Append("__packed_bone0 = input.packed_bone0;\\\n");
            code.Append("__packed_bone1 = input.packed_bone1;\\\n");
            code.Append("__packed_bone2 = input.packed_bone2;\\\n");
            code.Append("__packed_bone3 = input.packed_bone3;\\\n");
            code.Append("__packed_bone4 = input.packed_bone4;\\\n");
            code.Append("__packed_bone5 = input.packed_bone5;\\\n");
            code.Append("__packed_bone6 = input.packed_bone6;\\\n");
            code.Append("__packed_bone7 = input.packed_bone7;\\\n");
            code.Append("__cube_transformation = input.cube_transformation;\\\n");
            code.Append("__colormask = input.colormask;\\\n");
        }
    }

    internal sealed class MyGenericInstanceComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 matrix_row0", Format.R16G16B16A16_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 matrix_row1", Format.R16G16B16A16_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 matrix_row2", Format.R16G16B16A16_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 colormask", Format.R16G16B16A16_Float, component, list, dict, declaration);

            code.Append("__instance_matrix = construct_matrix_43( input.matrix_row0, input.matrix_row1, input.matrix_row2);\\\n");
            code.Append("__colormask = input.colormask;\\\n");
        }
    }

    internal sealed class MySimpleInstanceComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 matrix_row0", Format.R32G32B32A32_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 matrix_row1", Format.R32G32B32A32_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 matrix_row2", Format.R32G32B32A32_Float, component, list, dict, declaration);
            
            code.Append("__instance_matrix = construct_matrix_43(input.matrix_row0, input.matrix_row1, input.matrix_row2);\\\n");
        }
    }

    internal sealed class MySimpleInstanceColoringComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 instance_keyColorDithering", Format.R16G16B16A16_Float, component, list, dict, declaration);
            AddSingle("TEXCOORD", "float4 instance_colorMultEmissivity", Format.R16G16B16A16_Float, component, list, dict, declaration);

            code.Append("__instance_keyColor = input.instance_keyColorDithering.xyz;\\\n");
            code.Append("__instance_dithering = input.instance_keyColorDithering.w;\\\n");
            code.Append("__instance_colorMult = input.instance_colorMultEmissivity.xyz;\\\n");
            code.Append("__instance_emissivity = input.instance_colorMultEmissivity.w;\\\n");
        }
    }
    #endregion

    partial class MyVertexInputLayout
    {
        internal static readonly Dictionary<MyVertexInputComponentType, MyComponent> MapComponent = new Dictionary<MyVertexInputComponentType, MyComponent>();

        static void InitComponentsMap()
        {
            MapComponent[MyVertexInputComponentType.POSITION_PACKED] = new MyPositionPackedComponent();
            MapComponent[MyVertexInputComponentType.POSITION2] = new MyPosition2Component();
            MapComponent[MyVertexInputComponentType.POSITION3] = new MyPosition3Component();
            MapComponent[MyVertexInputComponentType.POSITION4] = new MyPosition4Component();
            MapComponent[MyVertexInputComponentType.POSITION4_H] = new MyPosition4HalfComponent();
            MapComponent[MyVertexInputComponentType.VOXEL_POSITION_MAT] = new MyVoxelPositionMaterialComponent();
            MapComponent[MyVertexInputComponentType.CUBE_INSTANCE] = new MyCubeInstanceComponent();
            MapComponent[MyVertexInputComponentType.GENERIC_INSTANCE] = new MyGenericInstanceComponent();
            MapComponent[MyVertexInputComponentType.SIMPLE_INSTANCE] = new MySimpleInstanceComponent();
            MapComponent[MyVertexInputComponentType.SIMPLE_INSTANCE_COLORING] = new MySimpleInstanceColoringComponent(); 
            MapComponent[MyVertexInputComponentType.BLEND_INDICES] = new MyBlendIndicesComponent();
            MapComponent[MyVertexInputComponentType.BLEND_WEIGHTS] = new MyBlendWeightsComponent();
            MapComponent[MyVertexInputComponentType.COLOR4] = new MyColor4Component();
            MapComponent[MyVertexInputComponentType.CUSTOM_HALF4_0] = new MyCustomHalf4_0Component();
            MapComponent[MyVertexInputComponentType.CUSTOM_HALF4_1] = new MyCustomHalf4_1Component();
            MapComponent[MyVertexInputComponentType.CUSTOM_HALF4_2] = new MyCustomHalf4_2Component();
            MapComponent[MyVertexInputComponentType.CUSTOM_UNORM4_0] = new MyCustomUnorm4_0Component();
            MapComponent[MyVertexInputComponentType.CUSTOM_UNORM4_1] = new MyCustomUnorm4_1Component();
            MapComponent[MyVertexInputComponentType.TEXCOORD0_H] = new MyTexcoord0HalfComponent();
            MapComponent[MyVertexInputComponentType.TEXCOORD0] = new MyTexcoord0Component();
            MapComponent[MyVertexInputComponentType.TEXINDICES] = new MyTexIndicesComponent();
            MapComponent[MyVertexInputComponentType.NORMAL] = new MyNormalComponent();
            MapComponent[MyVertexInputComponentType.VOXEL_NORMAL] = new MyVoxelNormalComponent();
            MapComponent[MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT] = new MyTangentBitanSgnComponent();
            MapComponent[MyVertexInputComponentType.CUSTOM4_0] = new MyCustom4_0Component();
        }
    }
}

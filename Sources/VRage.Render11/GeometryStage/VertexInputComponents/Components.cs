using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Render11.Shaders;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VRageRender
{
    #region Components

    internal abstract class MyComponent
    {
        internal static int NextIndex(Dictionary<string, int> dict, string name)
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

        protected static void AddSingle(string name, string variable, Format format, MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code
            )
        {
            var classification = component.freq == MyVertexInputComponentFreq.PER_VERTEX ? InputClassification.PerVertexData : InputClassification.PerInstanceData;
            var freq = component.freq == MyVertexInputComponentFreq.PER_VERTEX ? 0 : 1;
            var index = NextIndex(dict, name);

            list.Add(new InputElement(name, index, format, InputElement.AppendAligned, component.slot, classification, freq));
            declaration.AppendFormat("{0} : {1}{2};\n", variable, name, index);
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
            AddSingle("POSITION", "float4 position", Format.R16G16B16A16_Float, component, list, dict, declaration, code);

            code.AppendLine("result.position_local_raw = unpack_position_and_scale(input.position);");
        }
    }

    internal sealed class MyPosition3Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float3 position", Format.R32G32B32_Float, component, list, dict, declaration, code);

            code.AppendLine("result.position_local_raw = float4(input.position, 1);");
        }
    }

    internal sealed class MyPosition4HalfComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float3 position", Format.R16G16B16A16_Float, component, list, dict, declaration, code);

            code.AppendLine("result.position_local_raw = float4(input.position, 1);");
        }
    }

    internal sealed class MyVoxelPositionMaterialComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("POSITION", "float4 position", Format.R16G16B16A16_UNorm, component, list, dict, declaration, code);

            code.AppendLine("result.position_local_raw = unpack_voxel_position(input.position);");
            code.AppendLine("result.material_weights = unpack_voxel_weights(input.position.w);");
        }
    }

    internal sealed class MyTexcoord0Component : MyComponent 
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float2 texcoord0", Format.R16G16_Float, component, list, dict, declaration, code);

            code.AppendLine("result.texcoord0 = input.texcoord0;");
        }
    }

    internal sealed class MyNormalComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("NORMAL", "float4 normal", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);

            code.AppendLine("result.normal = unpack_normal(input.normal);");
        }
    }

    internal sealed class MyTangentBitanSgnComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TANGENT", "float4 tangent4", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);

            code.AppendLine("result.tangent_btansign = unpack_tangent_sign(input.tangent4);");
        }
    }

    internal sealed class MyBlendWeightsComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("BLENDWEIGHT", "float4 blend_weights", Format.R16G16B16A16_Float, component, list, dict, declaration, code);

            code.AppendLine("result.blend_weights = input.blend_weights;");
        }
    }

    internal sealed class MyBlendIndicesComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("BLENDINDICES", "uint4 blend_indices", Format.R8G8B8A8_UInt, component, list, dict, declaration, code);

            code.AppendLine("result.blend_indices = input.blend_indices;");
        }
    }

    internal sealed class MyColor4Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("COLOR", "float4 color", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);

            code.AppendLine("result.color = input.color;");
        }
    }

    internal sealed class MyCustomHalf4_0Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_half4_0", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
        }
    }

    internal sealed class MyCustomHalf4_1Component : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 custom_half4_1", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
        }
    }

    internal sealed class MyCubeInstanceComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 packed_bone0", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone1", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone2", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone3", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone4", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone5", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone6", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 packed_bone7", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 cube_transformation", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 colormask", Format.R8G8B8A8_UNorm, component, list, dict, declaration, code);

            code.AppendLine("result.packed_bone0 = input.packed_bone0;");
            code.AppendLine("result.packed_bone1 = input.packed_bone1;");
            code.AppendLine("result.packed_bone2 = input.packed_bone2;");
            code.AppendLine("result.packed_bone3 = input.packed_bone3;");
            code.AppendLine("result.packed_bone4 = input.packed_bone4;");
            code.AppendLine("result.packed_bone5 = input.packed_bone5;");
            code.AppendLine("result.packed_bone6 = input.packed_bone6;");
            code.AppendLine("result.packed_bone7 = input.packed_bone7;");
            code.AppendLine("result.cube_transformation = input.cube_transformation;");
        }
    }

    internal sealed class MyGenericInstanceComponent : MyComponent
    {
        internal override void AddComponent(MyVertexInputComponent component,
            List<InputElement> list, Dictionary<string, int> dict,
            StringBuilder declaration, StringBuilder code)
        {
            AddSingle("TEXCOORD", "float4 matrix_row0", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 matrix_row1", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 matrix_row2", Format.R16G16B16A16_Float, component, list, dict, declaration, code);
            AddSingle("TEXCOORD", "float4 color_mask_hsv", Format.R16G16B16A16_Float, component, list, dict, declaration, code);

            code.AppendLine("result.matrix_row0 = input.matrix_row0;");
            code.AppendLine("result.matrix_row1 = input.matrix_row1;");
            code.AppendLine("result.matrix_row2 = input.matrix_row2;");
        }
    }

    #endregion

    partial class MyVertexInput
    {
        static Dictionary<MyVertexInputComponentType, MyComponent> m_mapComponent = new Dictionary<MyVertexInputComponentType, MyComponent>();

        static void InitComponentsMap()
        {
            m_mapComponent[MyVertexInputComponentType.POSITION_PACKED] = new MyPositionPackedComponent();
            m_mapComponent[MyVertexInputComponentType.POSITION3] = new MyPosition3Component();
            m_mapComponent[MyVertexInputComponentType.POSITION4H] = new MyPosition4HalfComponent();
            m_mapComponent[MyVertexInputComponentType.VOXEL_POSITION_MAT] = new MyVoxelPositionMaterialComponent();
            m_mapComponent[MyVertexInputComponentType.CUBE_INSTANCE] = new MyCubeInstanceComponent();
            m_mapComponent[MyVertexInputComponentType.GENERIC_INSTANCE] = new MyGenericInstanceComponent();
            m_mapComponent[MyVertexInputComponentType.BLEND_INDICES] = new MyBlendIndicesComponent();
            m_mapComponent[MyVertexInputComponentType.BLEND_WEIGHTS] = new MyBlendWeightsComponent();
            m_mapComponent[MyVertexInputComponentType.COLOR4] = new MyColor4Component();
            m_mapComponent[MyVertexInputComponentType.CUSTOM_HALF4_0] = new MyCustomHalf4_0Component();
            m_mapComponent[MyVertexInputComponentType.CUSTOM_HALF4_1] = new MyCustomHalf4_1Component();
            m_mapComponent[MyVertexInputComponentType.TEXCOORD0] = new MyTexcoord0Component();
            m_mapComponent[MyVertexInputComponentType.NORMAL] = new MyNormalComponent();
            m_mapComponent[MyVertexInputComponentType.TANGENT_BITANSGN] = new MyTangentBitanSgnComponent();
            
        }
    }
}

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
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using SharpDX.D3DCompiler;

namespace VRageRender
{
    //enum MyConstantType
    //{
    //    FLOAT,
    //    FLOAT4
    //}

    //internal static class MyConstantTypeExtension
    //{
    //    internal static int SizeOf(this MyConstantType type)
    //    {
    //        switch (type)
    //        {
    //            case MyConstantType.FLOAT:
    //                return sizeof(float);
    //            case MyConstantType.FLOAT4:
    //                return sizeof(float) * 4;
    //        }
    //        return Int32.MinValue;
    //    }
    //}

    //struct MyConstantBufferVariable
    //{
    //    internal int Offset;
    //    internal MyConstantType Type;

    //    internal MyConstantBufferVariable(int offset, MyConstantType type)
    //    {
    //        Offset = offset;
    //        Type = type;
    //    }
    //}

    //class MyShaderMaterialReflection
    //{
    //    internal Dictionary<string, int> m_textureSlots;
    //    internal int m_textureSlotsNum;
    //    internal Dictionary<string, MyConstantBufferVariable> m_constants;
    //    internal int m_constantsSize;
    //    internal MyConstantBuffer m_cbuffer;

    //    static Dictionary<string, MyShaderMaterialReflection> m_reflections = new Dictionary<string,MyShaderMaterialReflection>();
    //    static Dictionary<int, MyMaterialProxy> m_cached = new Dictionary<int, MyMaterialProxy>();

    //    static Dictionary<int, MyConstantBuffer> m_constantBufferPerSize = new Dictionary<int, MyConstantBuffer>();

    //    internal static void CreateReflection(string materialTag, Dictionary<string, int> textureSlots, Dictionary<string, MyConstantBufferVariable> constants)
    //    {
    //        Debug.Assert(!m_reflections.ContainsKey(materialTag), "material reflection already exists"); 

    //        var reflection = new MyShaderMaterialReflection();
    //        reflection.m_textureSlots = textureSlots;
    //        reflection.m_textureSlotsNum = textureSlots.Select(x => x.Value).Max() + 1;
    //        reflection.m_constants = constants;

    //        if(constants.Count > 0)
    //        {
    //            var size = constants.Values.Select(x => x.Offset + x.Type.SizeOf()).Max();
    //            size = ((size + 15) / 16) * 16;

    //            MyConstantBuffer cbuffer;
    //            if (!m_constantBufferPerSize.TryGetValue(size, out cbuffer))
    //            {
    //                cbuffer = new MyConstantBuffer(size);
    //                cbuffer.SetDebugName(String.Format("material constants for {0} bytes size", size));
    //            }
    //            reflection.m_constantsSize = size;
    //            reflection.m_cbuffer = cbuffer;
    //        }
    //        else
    //        {
    //            reflection.m_constantsSize = 0;
    //            reflection.m_cbuffer = null;
    //        }

    //        m_reflections[materialTag] = reflection;
    //    }
        
    //    internal static MyShaderMaterialReflection GetReflection(string materialTag)
    //    {
    //        return m_reflections[materialTag];
    //    }

    //    internal static MyMaterialProxy CreateBindings(string materialTag)
    //    {
    //        var reflection = GetReflection(materialTag);
    //        return new MyMaterialProxy(reflection);
    //    }

    //    internal static void Init()
    //    {
    //        CreateReflection("triplanar_single",
    //            new Dictionary<string, int>()
    //            {
    //                { "ColorMetal_XZnY_pY", 0},
    //                { "NormalGloss_XZnY_pY", 3},
    //                { "Ext_XZnY_pY", 6},

    //            },
    //            new Dictionary<string, MyConstantBufferVariable>
    //            {
    //                { "highfreq_scale", new MyConstantBufferVariable(0, MyConstantType.FLOAT) },
    //                { "lowfreq_scale", new MyConstantBufferVariable(4, MyConstantType.FLOAT) },
    //                { "transition_range", new MyConstantBufferVariable(8, MyConstantType.FLOAT) },
    //                { "mask", new MyConstantBufferVariable(12, MyConstantType.FLOAT) },
    //            });

    //        CreateReflection("triplanar_multi",
    //            new Dictionary<string, int>()
    //            {
    //                { "ColorMetal_XZnY_pY[0]", 0},
    //                { "ColorMetal_XZnY_pY[1]", 1},
    //                { "ColorMetal_XZnY_pY[2]", 2},
    //                { "NormalGloss_XZnY_pY[0]", 3},
    //                { "NormalGloss_XZnY_pY[1]", 4},
    //                { "NormalGloss_XZnY_pY[2]", 5},
    //                { "Ext_XZnY_pY[0]", 6},
    //                { "Ext_XZnY_pY[1]", 7},
    //                { "Ext_XZnY_pY[2]", 8},
    //            },
    //            new Dictionary<string, MyConstantBufferVariable>
    //            {
    //                { "material_factors[0]", new MyConstantBufferVariable(0, MyConstantType.FLOAT4) },
    //                { "material_factors[1]", new MyConstantBufferVariable(16, MyConstantType.FLOAT4) },
    //                { "material_factors[2]", new MyConstantBufferVariable(32, MyConstantType.FLOAT4) },
    //            });

    //        CreateReflection("standard",
    //            new Dictionary<string, int>()
    //            {
    //                { "ColorMetalTexture", 0},
    //                { "NormalGlossTexture", 1},
    //                { "AmbientOcclusionTexture", 2},
    //                { "AlphamaskTexture", 3 }
    //            },
    //            new Dictionary<string, MyConstantBufferVariable>
    //            {
    //                { "key_color", new MyConstantBufferVariable(0, MyConstantType.FLOAT4) }
    //            });
    //    }
    //}
}

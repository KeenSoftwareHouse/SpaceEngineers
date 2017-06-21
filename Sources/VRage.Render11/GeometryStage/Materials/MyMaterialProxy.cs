using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;


namespace VRageRender
{
    //struct MyMaterialDescription
    //{
    //    internal string TextureColorMetalPath;
    //    internal string TextureNormalGlossPath;
    //    internal string TextureAmbientOcclusionPath;
    //    internal string TextureAlphamaskPath;
    //    internal string Technique;

    //    internal int CalculateKey()
    //    {
    //        var key = 
    //            TextureColorMetalPath.GetHashCode();
    //        MyHashHelper.Combine(ref key, TextureNormalGlossPath.GetHashCode());
    //        MyHashHelper.Combine(ref key, TextureAmbientOcclusionPath.GetHashCode());
    //        if (TextureAlphamaskPath != null)
    //        {
    //            MyHashHelper.Combine(ref key, TextureAlphamaskPath.GetHashCode());
    //        }

    //        return key;
    //    }

    //    internal int CalculateMergeHash()
    //    {
    //        var key = 
    //            TextureColorMetalPath.GetHashCode();
    //        MyHashHelper.Combine(ref key, TextureNormalGlossPath.GetHashCode());
    //        MyHashHelper.Combine(ref key, TextureAmbientOcclusionPath.GetHashCode());
    //        if (TextureAlphamaskPath != null)
    //        {
    //            MyHashHelper.Combine(ref key, TextureAlphamaskPath.GetHashCode());
    //        }
    //        return key;
    //    }
    //}

    //class MyMaterialProxy
    //{
    //    internal ShaderResourceView[] SRVs;
    //    internal byte[] Constants;


    //    internal int TexturesHash;
    //    internal int ConstantsHash;

    //    internal Buffer ConstantsBuffer;

    //    internal void RecalcTexturesHash()
    //    {
    //        TexturesHash = 0;
    //        foreach (var t in SRVs)
    //        {
    //            TexturesHash = MyHashHelper.Combine(TexturesHash, t != null ? t.GetHashCode() : 0);
    //        }
    //    }

    //    internal void RecalcConstantsHash()
    //    {
    //        ConstantsHash = 0;
    //        // kind of lame to iterate over bytes
    //        if (Constants == null) return;
    //        foreach (var b in Constants)
    //        {
    //            ConstantsHash = MyHashHelper.Combine(ConstantsHash, b.GetHashCode());
    //        }
    //    }

    //    // maybe should be separate component in the future with view
    //    MyShaderMaterialReflection m_reflection;

    //    internal MyMaterialProxy(MyShaderMaterialReflection materialReflection)
    //    {
    //        m_reflection = materialReflection;

    //        SRVs = new ShaderResourceView[m_reflection.m_textureSlotsNum];

    //        if (m_reflection.m_cbuffer != null)
    //        {
    //            Constants = Enumerable.Repeat<byte>(0, m_reflection.m_constantsSize).ToArray();
    //            ConstantsBuffer = m_reflection.m_cbuffer.Buffer;
    //        }
    //    }

    //    internal unsafe void SetFloat(string var, float value)
    //    {
    //        fixed (byte* ptr = Constants)
    //        {
    //            var offset = m_reflection.m_constants[var].Offset;
    //            *(float*)(ptr + offset) = value;
    //        }
    //    }

    //    internal unsafe void SetFloat4(string var, Vector4 value)
    //    {
    //        fixed (byte* ptr = Constants)
    //        {
    //            var offset = m_reflection.m_constants[var].Offset;
    //            *(Vector4*)(ptr + offset) = value;
    //        }
    //    }

    //    internal void SetTexture(string name, ShaderResourceView view)
    //    {
    //        SRVs[m_reflection.m_textureSlots[name]] = view;
    //    }
    //}

    //class MyMaterialProxyFactory
    //{
    //    static Dictionary<int, MyMaterialProxy> m_proxies = new Dictionary<int, MyMaterialProxy>();
    //    static Dictionary<int, MyMaterialDescription> m_descriptors = new Dictionary<int, MyMaterialDescription>();

        
    //    // not very elegant design but whatever right now
    //    static Dictionary<MyMaterialProxy, MyMaterialDescription> m_reverseMapping = new Dictionary<MyMaterialProxy, MyMaterialDescription>();
    //    internal static MyMaterialDescription DescriptorFromProxy(MyMaterialProxy proxy)
    //    {
    //        return m_reverseMapping[proxy];
    //    }

    //    //internal static MyMaterialProxy Create(MyMaterialDescription ? desc, string materialTag)
    //    //{
    //    //    int key = desc.HasValue ? desc.Value.CalculateKey() : 0;
    //    //    key = MyHashHelper.Combine(key, materialTag.GetHashCode());

    //    //    var proxy = m_proxies.SetDefault(key, null);
    //    //    if (proxy != null)
    //    //        return proxy;

    //    //    proxy = MyShaderMaterialReflection.CreateBindings(materialTag);

    //    //    if(desc == null)
    //    //    {
    //    //        // TODO: change later
    //    //        desc = MyAssetMesh.GetDebugMaterialDescriptor();
    //    //    }

    //    //    LoadTextures(proxy, desc.Value);

    //    //    proxy.RecalcConstantsHash();

    //    //    m_proxies[key] = proxy;
    //    //    m_descriptors[key] = desc.Value;

    //    //    m_reverseMapping[proxy] = desc.Value;

    //    //    return proxy;
    //    //}

    //    internal static void ReloadTextures()
    //    {
    //        foreach (var kv in m_proxies)
    //        {
    //            var desc = m_descriptors[kv.Key];
    //            LoadTextures(kv.Value, desc);
    //        }
    //    }

    //    internal static void LoadTextures(MyMaterialProxy proxy, MyMaterialDescription desc)
    //    {
    //        {
    //            proxy.SetTexture("ColorMetalTexture",
    //                MyTextureManager.GetColorMetalTexture(desc.TextureColorMetalPath).ShaderView);
    //            proxy.SetTexture("NormalGlossTexture",
    //                MyTextureManager.GetNormalGlossTexture(desc.TextureNormalGlossPath).ShaderView);
    //            proxy.SetTexture("AmbientOcclusionTexture",
    //                MyTextureManager.GetExtensionsTexture(desc.TextureAmbientOcclusionPath).ShaderView);
    //            if (desc.TextureAlphamaskPath != null)
    //            {
    //                proxy.SetTexture("AlphamaskTexture",
    //                    MyTextureManager.GetAlphamaskTexture(desc.TextureAlphamaskPath).ShaderView);
    //            }
    //        }
    //        proxy.RecalcTexturesHash();
    //    }
    //}
}

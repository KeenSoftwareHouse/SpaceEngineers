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
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using SharpDX.D3DCompiler;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{

    struct MyVoxelMaterialDetailSet
    {
        // texturing part

        internal string[] ColorMetalXZnY_Filepaths;
        internal string[] ColorMetalY_Filepaths;
        internal string[] NormalGlossXZnY_Filepaths;
        internal string[] NormalGlossY_Filepaths;
        internal string[] ExtXZnY_Filepaths;
        internal string[] ExtY_Filepaths;

        internal static void RequestResources(ref MyVoxelMaterialDetailSet set)
        {
            MyFileTextureManager texManager = MyManagers.FileTextures;
            foreach (var filepath in set.ColorMetalXZnY_Filepaths)
                texManager.GetTexture(filepath, MyFileTextureEnum.COLOR_METAL);
            foreach (var filepath in set.ColorMetalY_Filepaths)
                texManager.GetTexture(filepath, MyFileTextureEnum.COLOR_METAL);

            foreach (var filepath in set.NormalGlossXZnY_Filepaths)
                texManager.GetTexture(filepath, MyFileTextureEnum.NORMALMAP_GLOSS);
            foreach (var filepath in set.NormalGlossY_Filepaths)
                texManager.GetTexture(filepath, MyFileTextureEnum.NORMALMAP_GLOSS);

            foreach (var filepath in set.ExtXZnY_Filepaths) 
                texManager.GetTexture(filepath, MyFileTextureEnum.EXTENSIONS);
            foreach (var filepath in set.ExtY_Filepaths)
                texManager.GetTexture(filepath, MyFileTextureEnum.EXTENSIONS);
        }
    }

    struct MyVoxelMaterial1
    {
        internal MyVoxelMaterialDetailSet Resource;

        // foliage 
        internal string FoliageArray_Texture;
        internal string FoliageArray_NormalTexture;

        internal IFileArrayTexture FoliageColorTextureArray;
        internal IFileArrayTexture FoliageNormalTextureArray;

        internal Vector2 FoliageScale;
        internal float FoliageDensity;
        internal float FoliageScaleVariation;
        internal int FoliageType;

        internal bool HasFoliage { get { return FoliageArray_Texture != null || FoliageColorTextureArray != null; } }
        
        internal Vector4 DistanceAndScale;
        internal Vector4 DistanceAndScaleFar;
        internal float ExtensionDetailScale;

        internal Vector2 DistanceAndScaleFar3;
        internal Color Far3Color;
    }

    struct MyVoxelMaterialTriple
    {
        internal int I0;
        internal int I1;
        internal int I2;
        internal bool IsFillingMaterialCB;

        internal MyVoxelMaterialTriple(int i0, int i1, int i2, bool isFillingMaterialCB)
        {
            I0 = i0;
            I1 = i1;
            I2 = i2;
            IsFillingMaterialCB = isFillingMaterialCB;
        }

        internal bool IsMultimaterial()
        {
            return I1 > -1 || I2 > -1;
        }

        private class comp : IEqualityComparer<MyVoxelMaterialTriple>
        {
            public bool Equals(MyVoxelMaterialTriple x, MyVoxelMaterialTriple y)
            {
                return x.I0 == y.I0 && x.I1 == y.I1 && x.I2 == y.I2 && x.IsFillingMaterialCB == y.IsFillingMaterialCB;
            }

            public int GetHashCode(MyVoxelMaterialTriple obj)
            {
                int hashIsFillingMaterialCB = obj.IsFillingMaterialCB ? 1 << 24 : 0;
                return (obj.I0 << 16 | obj.I1 << 8 | obj.I2).GetHashCode() ^ hashIsFillingMaterialCB;
            }
        }

        public static readonly IEqualityComparer<MyVoxelMaterialTriple> Comparer = new comp();
    }

    struct MaterialFoliageConstantsElem
    {
        internal Vector2 Scale;
        internal float ScaleVar;
        internal uint TexturesNum;
    }

    //deprecated
    //class MyVoxelMaterial
    //{
    //    internal int Id;
    //    internal string ColorMetal_XZ_nY_Texture;
    //    internal string ColorMetal_pY_Texture;
    //    internal string NormalGloss_XZ_nY_Texture;
    //    internal string NormalGloss_pY_Texture;
    //    internal string Ext_XZ_nY_Texture;
    //    internal string Ext_pY_Texture;

    //    internal string LowFreqColorMetal_XZ_nY_Texture;
    //    internal string LowFreqColorMetal_pY_Texture;
    //    internal string LowFreqNormalGloss_XZ_nY_Texture;
    //    internal string LowFreqNormalGloss_pY_Texture;
    //    internal string LowFreqExt_XZ_nY_Texture;
    //    internal string LowFreqExt_pY_Texture;

    //    internal float HighFreqScale;
    //    internal float LowFreqScale;
    //    internal float TransitionRange;

    //    internal MyTextureArray ColorMetalArray;
    //    internal MyTextureArray NormalGlossArray;
    //    internal MyTextureArray ExtArray;

    //    internal MyAssetTexture FoliageTextureArray1;
    //    internal MyAssetTexture FoliageTextureArray2;

    //    internal string FoliageTextureArray1_Filename;
    //    internal string FoliageTextureArray2_Filename;
    //    internal float FoliageDensity;
    //    internal Vector2 FoliageScale;
    //    internal float FoliageRandomRescaleMult;
    //    internal uint FoliageArraySize;

    //    internal bool HasFoliage { get { return FoliageTextureArray1_Filename != null; } }

    //    internal void BuildTextureArrays()
    //    {
    //        Dispose();

    //        ColorMetalArray = new MyTextureArray(new[] { 
    //            MyTextureManager.GetColorMetalTexture(ColorMetal_XZ_nY_Texture),
    //            MyTextureManager.GetColorMetalTexture(ColorMetal_pY_Texture),
    //            MyTextureManager.GetColorMetalTexture(LowFreqColorMetal_XZ_nY_Texture),
    //            MyTextureManager.GetColorMetalTexture(LowFreqColorMetal_pY_Texture),
    //        });
    //        ColorMetalArray.SetDebugName(String.Format("voxel material {0} ColorMetal array", Id));

    //        NormalGlossArray = new MyTextureArray(new[] { 
    //            MyTextureManager.GetNormalGlossTexture(NormalGloss_XZ_nY_Texture),
    //            MyTextureManager.GetNormalGlossTexture(NormalGloss_pY_Texture),
    //            MyTextureManager.GetNormalGlossTexture(LowFreqNormalGloss_XZ_nY_Texture),
    //            MyTextureManager.GetNormalGlossTexture(LowFreqNormalGloss_pY_Texture),
    //        });
    //        NormalGlossArray.SetDebugName(String.Format("voxel material {0} NormalGloss array", Id));

    //        ExtArray = new MyTextureArray(new[] { 
    //            MyTextureManager.GetExtensionsTexture(Ext_XZ_nY_Texture),
    //            MyTextureManager.GetExtensionsTexture(Ext_pY_Texture),
    //            MyTextureManager.GetExtensionsTexture(LowFreqExt_XZ_nY_Texture),
    //            MyTextureManager.GetExtensionsTexture(LowFreqExt_pY_Texture),
    //        });
    //        ExtArray.SetDebugName(String.Format("voxel material {0} Ex6 array", Id));

    //        if (FoliageTextureArray1_Filename != null)
    //        { 
    //            FoliageTextureArray1 = MyTextureManager.GetTexture(FoliageTextureArray1_Filename);
    //            FoliageArraySize = (uint)((Texture2D)FoliageTextureArray1.Resource).Description.ArraySize;
    //        }
    //        if (FoliageTextureArray2_Filename != null)
    //        { 
    //            FoliageTextureArray2 = MyTextureManager.GetTexture(FoliageTextureArray2_Filename);
    //        }
    //    }

    //    internal void Dispose()
    //    {
    //        if (ColorMetalArray != null)
    //        {
    //            ColorMetalArray.Dispose();
    //            ColorMetalArray = null;
    //        }
    //        if (NormalGlossArray != null)
    //        {
    //            NormalGlossArray.Dispose();
    //            NormalGlossArray = null;
    //        }
    //        if (ExtArray != null)
    //        {
    //            ExtArray.Dispose();
    //            ExtArray = null;
    //        }
    //    }
    //}

    //class MyVoxelMaterials
    //{
    //    static Dictionary<int, MyVoxelMaterial> m_materials = new Dictionary<int, MyVoxelMaterial>();

    //    struct MaterialFoliageConstantsElem
    //    {
    //        Vector2 Scale;
    //        float RescaleMult;
    //        uint TexturesNum;

    //        internal MaterialFoliageConstantsElem(Vector2 scale, float rescale, uint num)
    //        {
    //            Scale = scale;
    //            RescaleMult = rescale;
    //            TexturesNum = num;
    //        }
    //    }

    //    internal static void RebuildMaterialFoliageTable()
    //    {
    //        var array = new MaterialFoliageConstantsElem[256];
    //        for(int i=0; i<256; i++)
    //        {
    //            var mat = m_materials.Get(i);
    //            if(mat == null)
    //            {
    //                array[i] = new MaterialFoliageConstantsElem();
    //            }
    //            else
    //            {
    //                array[i] = new MaterialFoliageConstantsElem(mat.FoliageScale, mat.FoliageRandomRescaleMult, mat.FoliageArraySize);
    //            }
    //        }
    //        var mapping = MyMapping.MapDiscard(MyCommon.MaterialFoliageTableConstants.Buffer);
    //        mapping.stream.WriteRange(array, 0, 256);
    //        mapping.Unmap();
    //    }

    //    internal static void AddVoxelMaterial(int id, 
    //        string colorMetal_XZ_nY_Texture,
    //        string colorMetal_pY_Texture,
    //        string normalGloss_XZ_nY_Texture,
    //        string normalGloss_pY_Texture,
    //        string lowFreqColorMetal_XZ_nY_Texture,
    //        string lowFreqColorMetal_pY_Texture,
    //        string lowFreqNormalGloss_XZ_nY_Texture,
    //        string lowFreqNormalGloss_pY_Texture,
    //        string ext_XZ_nY_Texture,
    //        string ext_pY_Texture,
    //        string lowFreqExt_XZ_nY_Texture,
    //        string lowFreqExt_pY_Texture,
    //        float highFreqTextureScale,
    //        float lowFreqTextureScale,

    //        string foliageTextureArray1,
    //        string foliageTextureArray2,
    //        float foliageDensity,
    //        Vector2 foliageScale,
    //        float foliageRandomRescaleMult
    //        )
    //    {
    //        var material = new MyVoxelMaterial();

    //        material.Id = id;
            
    //        material.ColorMetal_XZ_nY_Texture           = colorMetal_XZ_nY_Texture;
    //        material.ColorMetal_pY_Texture              = colorMetal_pY_Texture;
    //        material.NormalGloss_XZ_nY_Texture          = normalGloss_XZ_nY_Texture;
    //        material.NormalGloss_pY_Texture             = normalGloss_pY_Texture;
                                                          
    //        material.LowFreqColorMetal_XZ_nY_Texture    = lowFreqColorMetal_XZ_nY_Texture;
    //        material.LowFreqColorMetal_pY_Texture       = lowFreqColorMetal_pY_Texture;
    //        material.LowFreqNormalGloss_XZ_nY_Texture   = lowFreqNormalGloss_XZ_nY_Texture;
    //        material.LowFreqNormalGloss_pY_Texture      = lowFreqNormalGloss_pY_Texture;

    //        material.Ext_XZ_nY_Texture        = ext_XZ_nY_Texture;
    //        material.Ext_pY_Texture           = ext_pY_Texture;
    //        material.LowFreqExt_XZ_nY_Texture = lowFreqExt_XZ_nY_Texture;
    //        material.LowFreqExt_pY_Texture    = lowFreqExt_pY_Texture;

    //        material.HighFreqScale = highFreqTextureScale;
    //        material.LowFreqScale = lowFreqTextureScale;
    //        material.TransitionRange = 100;

    //        material.FoliageTextureArray1_Filename = foliageTextureArray1;
    //        material.FoliageTextureArray2_Filename = foliageTextureArray2;

    //        material.FoliageDensity = foliageDensity;
    //        material.FoliageScale = foliageScale;
    //        material.FoliageRandomRescaleMult = foliageRandomRescaleMult;

    //        material.BuildTextureArrays();

    //        if(m_materials.ContainsKey(id))
    //        {
    //            m_materials[id].Dispose();
    //        }

    //        m_materials[id] = material;    
    //    }

    //    internal static void ReloadTextures()
    //    {
    //        foreach (var id in m_materials.Keys)
    //        {
    //            m_materials[id].BuildTextureArrays();
    //        }
    //    }

    //    internal static void RemoveAll()
    //    {
    //        foreach(var mat in m_materials.Values)
    //        {
    //            mat.Dispose();
    //        }
    //        m_materials.Clear();

    //        m_cachedBindings.Clear();
    //    }

    //    static Dictionary<Tuple<int, int, int>, MyMaterialProxy> m_cachedBindings = new Dictionary<Tuple<int, int, int>, MyMaterialProxy>();

    //    internal static MyVoxelMaterial GetMaterial(int id)
    //    {
    //        return m_materials[id];
    //    }

    //    internal static MyMaterialProxy GetBindings(int i0, int i1, int i2)
    //    {
    //        var key = Tuple.Create(i0, i1, i2);
    //        MyMaterialProxy bindings;
    //        if (m_cachedBindings.TryGetValue(key, out bindings))
    //            return bindings;

    //        bool isSingleMaterial = i1 == -1 && i2 == -1;

    //        var material0 = m_materials[i0];

    //        if(isSingleMaterial)
    //        {
    //            bindings = MyShaderMaterialReflection.CreateBindings(MyVoxelMesh.TRIPLANAR_SINGLE_MATERIAL_TAG);

    //            bindings.SetFloat("highfreq_scale", material0.HighFreqScale);
    //            bindings.SetFloat("lowfreq_scale", material0.LowFreqScale);
    //            bindings.SetFloat("transition_range", material0.TransitionRange);
    //            bindings.SetFloat("mask", material0.HasFoliage ? 1 : 0);

    //            bindings.SetTexture("ColorMetal_XZnY_pY", material0.ColorMetalArray.ShaderView);
    //            bindings.SetTexture("NormalGloss_XZnY_pY", material0.NormalGlossArray.ShaderView);
    //            bindings.SetTexture("Ext_XZnY_pY", material0.ExtArray.ShaderView);
    //        }
    //        else
    //        {
    //            bindings = MyShaderMaterialReflection.CreateBindings(MyVoxelMesh.TRIPLANAR_MULTI_MATERIAL_TAG);

    //            bindings.SetFloat4("material_factors[0]", new Vector4(material0.HighFreqScale, material0.LowFreqScale, material0.TransitionRange, material0.HasFoliage ? 1 : 0));
    //            bindings.SetTexture("ColorMetal_XZnY_pY[0]", material0.ColorMetalArray.ShaderView);
    //            bindings.SetTexture("NormalGloss_XZnY_pY[0]", material0.NormalGlossArray.ShaderView);
    //            bindings.SetTexture("Ext_XZnY_pY[0]", material0.ExtArray.ShaderView);

    //            var material1 = m_materials[i1];
    //            bindings.SetFloat4("material_factors[1]", new Vector4(material1.HighFreqScale, material1.LowFreqScale, material1.TransitionRange, material1.HasFoliage ? 1 : 0));
    //            bindings.SetTexture("ColorMetal_XZnY_pY[1]", material1.ColorMetalArray.ShaderView);
    //            bindings.SetTexture("NormalGloss_XZnY_pY[1]", material1.NormalGlossArray.ShaderView);
    //            bindings.SetTexture("Ext_XZnY_pY[1]", material1.ExtArray.ShaderView);

    //            if(i2 != -1)
    //            {
    //                var material2 = m_materials[i2];
    //                bindings.SetFloat4("material_factors[2]", new Vector4(material2.HighFreqScale, material2.LowFreqScale, material2.TransitionRange, material2.HasFoliage ? 1 : 0));
    //                bindings.SetTexture("ColorMetal_XZnY_pY[2]", material2.ColorMetalArray.ShaderView);
    //                bindings.SetTexture("NormalGloss_XZnY_pY[2]", material2.NormalGlossArray.ShaderView);
    //                bindings.SetTexture("Ext_XZnY_pY[2]", material2.ExtArray.ShaderView);
    //            }
    //        }

    //        bindings.RecalcConstantsHash();
    //        bindings.RecalcTexturesHash();
    //        m_cachedBindings[key] = bindings;

    //        return bindings;
    //    }
    //}
}

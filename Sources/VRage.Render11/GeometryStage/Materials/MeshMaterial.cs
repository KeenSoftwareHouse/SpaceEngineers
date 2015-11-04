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
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using SharpDX.D3DCompiler;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Import;

namespace VRageRender
{
    struct MyMeshMaterialInfo
    {
        internal MyMeshMaterialId Id; // key - direct index, out 
        internal int RepresentationKey; // key - external ref, out 
        internal MyStringId Name;
        internal string ContentPath;
        internal MyStringId ColorMetal_Texture;
        internal MyStringId NormalGloss_Texture;
        internal MyStringId Extensions_Texture;
        internal MyStringId Alphamask_Texture;
        internal string Technique;
        internal MyFacingEnum Facing;
        internal Vector2 WindScaleAndFreq;


        internal static void RequestResources(ref MyMeshMaterialInfo info)
        {
            MyTextures.GetTexture(info.ColorMetal_Texture, info.ContentPath, MyTextureEnum.COLOR_METAL, false, info.Facing == MyFacingEnum.Impostor);
            MyTextures.GetTexture(info.NormalGloss_Texture, info.ContentPath, MyTextureEnum.NORMALMAP_GLOSS);
            MyTextures.GetTexture(info.Extensions_Texture, info.ContentPath, MyTextureEnum.EXTENSIONS);
            MyTextures.GetTexture(info.Alphamask_Texture, info.ContentPath, MyTextureEnum.ALPHAMASK);
        }

        internal static MyMaterialProxy_2 CreateProxy(ref MyMeshMaterialInfo info)
        {
            var A = MyTextures.GetTexture(info.ColorMetal_Texture, info.ContentPath, MyTextureEnum.COLOR_METAL);
            var B = MyTextures.GetTexture(info.NormalGloss_Texture, info.ContentPath, MyTextureEnum.NORMALMAP_GLOSS);
            var C = MyTextures.GetTexture(info.Extensions_Texture, info.ContentPath, MyTextureEnum.EXTENSIONS);
            var D = MyTextures.GetTexture(info.Alphamask_Texture, info.ContentPath, MyTextureEnum.ALPHAMASK);

            return 
                new MyMaterialProxy_2 { 
                    MaterialSRVs = { 
                        BindFlag = MyBindFlag.BIND_PS, 
                        StartSlot = 0, 
                        SRVs = new ShaderResourceView[] {
                            MyTextures.Views[A.Index],
                            MyTextures.Views[B.Index],
                            MyTextures.Views[C.Index],
                            MyTextures.Views[D.Index] }, 
                        Version = info.Id.GetHashCode()
                    } };
        }
    }
}

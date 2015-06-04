using VRage.Utils;
using VRageRender.Resources;

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

        internal static void RequestResources(ref MyMeshMaterialInfo info)
        {
            MyTextures.GetTexture(info.ColorMetal_Texture, info.ContentPath, MyTextureEnum.COLOR_METAL);
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
                        SRVs = new[] {
                            MyTextures.Views[A.Index],
                            MyTextures.Views[B.Index],
                            MyTextures.Views[C.Index],
                            MyTextures.Views[D.Index] }, 
                        Version = info.Id.GetHashCode()
                    } };
        }
    }
}

using SharpDX.DXGI;
using VRage.Render11.Common;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    class MyGlobalResources : IManager, IManagerUnloadData
    {
        public static IDynamicFileArrayTexture FileArrayTextureVoxelCM;
        public static IDynamicFileArrayTexture FileArrayTextureVoxelNG;
        public static IDynamicFileArrayTexture FileArrayTextureVoxelExt;

        public void CreateOnStartup()
        {
            MyDynamicFileArrayTextureManager manager = MyManagers.DynamicFileArrayTextures;
            FileArrayTextureVoxelCM = manager.CreateTexture("MyGlobalResources.FileArrayTextureVoxelCM",
                MyFileTextureEnum.COLOR_METAL, MyGeneratedTexturePatterns.ColorMetal_BC7_SRgb, Format.BC7_UNorm_SRgb);
            FileArrayTextureVoxelNG = manager.CreateTexture("MyGlobalResources.FileArrayTextureVoxelNG",
                MyFileTextureEnum.NORMALMAP_GLOSS, MyGeneratedTexturePatterns.NormalGloss_BC7, Format.BC7_UNorm);
            FileArrayTextureVoxelExt = manager.CreateTexture("MyGlobalResources.FileArrayTextureVoxelExt",
                MyFileTextureEnum.EXTENSIONS, MyGeneratedTexturePatterns.Extension_BC7_SRgb, Format.BC7_UNorm_SRgb);
        }

        void IManagerUnloadData.OnUnloadData()
        {
            FileArrayTextureVoxelCM.Clear();
            FileArrayTextureVoxelNG.Clear();
            FileArrayTextureVoxelExt.Clear();
        }
    }
}

using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;
using VRage.Render11.Resources;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    static class MyMwmUtils
    {
        public static HashSet<string> NoShadowCasterMaterials = new HashSet<string>();

        public static string GetColorMetalTexture(MyMeshPartInfo mwmPart, string contentPath)
        {
            if (mwmPart.m_MaterialDesc == null)
                return "";

            var relativePath = mwmPart.m_MaterialDesc.Textures.Get("ColorMetalTexture", "");
            return MyResourceUtils.GetTextureFullPath(relativePath, contentPath);
        }

        public static string GetNormalGlossTexture(MyMeshPartInfo mwmPart, string contentPath)
        {
            if (mwmPart.m_MaterialDesc == null)
                return "";
            var relativePath = mwmPart.m_MaterialDesc.Textures.Get("NormalGlossTexture", "");
            return MyResourceUtils.GetTextureFullPath(relativePath, contentPath);
        }

        public static string GetExtensionTexture(MyMeshPartInfo mwmPart, string contentPath)
        {
            if (mwmPart.m_MaterialDesc == null)
                return "";
            var relativePath = mwmPart.m_MaterialDesc.Textures.Get("AddMapsTexture", "");
            return MyResourceUtils.GetTextureFullPath(relativePath, contentPath);
        }

        public static string GetAlphamaskTexture(MyMeshPartInfo mwmPart, string contentPath)
        {
            if (mwmPart.m_MaterialDesc == null)
                return "";
            var relativePath = mwmPart.m_MaterialDesc.Textures.Get("AlphamaskTexture", "");
            return MyResourceUtils.GetTextureFullPath(relativePath, contentPath);
        }

        public static string GetFullMwmFilepath(string mwmFilepath)
        {
            mwmFilepath = Path.IsPathRooted(mwmFilepath) ? mwmFilepath : Path.Combine(MyFileSystem.ContentPath, mwmFilepath);
            mwmFilepath = mwmFilepath.ToLower();
            if (!mwmFilepath.EndsWith(".mwm"))
                mwmFilepath += ".mwm";
            return mwmFilepath;
        }

        public static string GetFullMwmContentPath(string mwmFilePath)
        {
            string contentPath = null;
            if (Path.IsPathRooted(mwmFilePath) && mwmFilePath.ToLower().Contains("models"))
                contentPath = mwmFilePath.Substring(0, mwmFilePath.ToLower().IndexOf("models"));

            return contentPath;
        }
    }
}

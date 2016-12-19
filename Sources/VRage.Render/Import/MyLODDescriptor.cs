using System;
using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;

namespace VRageRender.Import
{
    public enum MyFacingEnum : byte
    {
        None = 0,
        Vertical = 1,
        Full = 2,
        Impostor = 3
    }

	public class MyLODDescriptor
	{
        public float Distance; //In meters
        public string Model; // Relative filePath within Content or Mod Folder
        public string RenderQuality;
        public List<int> RenderQualityList;

        public MyLODDescriptor() { }

        /// <summary>
        /// Absolute file path to the LOD model related to the parent assetFilePath.
        /// </summary>
        /// <param name="parentAssetFilePath">File path of parent asset.</param>
        /// <returns>Absolute file path.</returns>
        public string GetModelAbsoluteFilePath(string parentAssetFilePath)
        {
            if(Model == null) return null;
            var filePathL = parentAssetFilePath.ToLower();
            var modelFileName = Model;

            // Sometimes the models do not have the post fix
            if (!modelFileName.Contains(".mwm"))
            {
                modelFileName += ".mwm";
            }

            // Modded content has always absolute filepath of the asset
            if (Path.IsPathRooted(parentAssetFilePath) && filePathL.Contains("models"))
            {
                var contentPath = parentAssetFilePath.Substring(0, filePathL.IndexOf("models"));
                var possibleFilePath = Path.Combine(contentPath, modelFileName);
                // Check the existence
                if (MyFileSystem.FileExists(possibleFilePath))
                {
                    return possibleFilePath;
                } else
                {
                    possibleFilePath = Path.Combine(MyFileSystem.ContentPath, modelFileName);
                    return MyFileSystem.FileExists(possibleFilePath) ? possibleFilePath : null;
                }
            }

            // Our vanilla content with relative file paths
            return Path.Combine(MyFileSystem.ContentPath, modelFileName);
        }

		public bool Write(BinaryWriter writer)
		{
            writer.Write(Distance);
            writer.Write((Model != null) ? Model : "");
            writer.Write((RenderQuality != null) ? RenderQuality : "");
			return true;
		}

        public bool Read(BinaryReader reader)
        {
            Distance = reader.ReadSingle();
            Model = reader.ReadString();
            if (String.IsNullOrEmpty(Model))
                Model = null;
            RenderQuality = reader.ReadString();
            if (String.IsNullOrEmpty(RenderQuality))
                RenderQuality = null;
            return true;
        }
	}
}

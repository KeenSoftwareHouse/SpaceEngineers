using System;
using SharpDX.DXGI;
using System.Collections.Generic;
using System.Text;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;

namespace VRage.Render11.GeometryStage
{
    class MyGeometryTextureSystem: IManager
    {
        struct MyArrayTextureKey
        {
            public Vector2I ResolutionInFile;
            public int MipmapsCount;
            public Format Format;
            public MyChannel Channel;

            public string ToString()
            {
                StringBuilder builder = new StringBuilder();
                // dds suffix is there to "hack" mechanism in MyFileTexture
                builder.AppendFormat("{0}-{1}x{2},{3}:{4}.dds", Channel, ResolutionInFile.X, ResolutionInFile.Y, MipmapsCount, Format);
                return builder.ToString();
            }
        }

        struct MyMaterialKey
        {
            public string cmParams, ngParams, extParams, alphamaskParams;

            public MyMaterialKey(string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath)
            {
                cmParams = cmFilepath;
                ngParams = ngFilepath;
                extParams = extFilepath;
                alphamaskParams = alphamaskFilepath;
            }
        }


        const int DEFAULT_ARRAY_TEXTURE_INDEX = -1;
        Dictionary<MyArrayTextureKey, IDynamicFileArrayTexture> m_dictTextures = new Dictionary<MyArrayTextureKey, IDynamicFileArrayTexture>();
        readonly HashSet<string> m_ignoredMaterials = new HashSet<string>();

        // used to review allocated sets:
        HashSet<MyMaterialKey> m_setDebugMaterials = new HashSet<MyMaterialKey>();

        static MyFileTextureEnum GetTextureType(MyChannel channel)
        {
            MyFileTextureEnum type = MyFileTextureEnum.UNSPECIFIED;
            switch (channel)
            {
                case MyChannel.ColorMetal:
                    type = MyFileTextureEnum.COLOR_METAL;
                    break;
                case MyChannel.NormalGloss:
                    type = MyFileTextureEnum.NORMALMAP_GLOSS;
                    break;
                case MyChannel.Extension:
                    type = MyFileTextureEnum.EXTENSIONS;
                    break;
                case MyChannel.Alphamask:
                    type = MyFileTextureEnum.ALPHAMASK;
                    break;
                default:
                    MyRenderProxy.Assert(false, "Channel is not recognised");
                    break;
            }
            return type;
        }

        IDynamicFileArrayTexture GetArrayTextureFromKey(MyArrayTextureKey key)
        {
            IDynamicFileArrayTexture arrayTexture;
            if (m_dictTextures.TryGetValue(key, out arrayTexture))
                return arrayTexture;

            string name = key.ToString();
            MyFileTextureEnum texType = GetTextureType(key.Channel);
            byte[] bytePattern = MyGeneratedTexturePatterns.GetBytePattern(key.Channel, key.Format);
            arrayTexture = MyManagers.DynamicFileArrayTextures.CreateTexture(name, texType, bytePattern, key.Format);
            m_dictTextures[key] = arrayTexture;
            return arrayTexture;
        }

        IDynamicFileArrayTexture GetArrayTextureFromFilepath(string filepath, MyChannel channel, Vector2I defaultResolution)
        {
            MyFileTextureParams textureParams;
            MyArrayTextureKey key;
            key.Channel = channel;
            
            bool isLoaded = MyFileTextureParamsManager.LoadFromFile(filepath, out textureParams);
            if (isLoaded)
            {
                Format format;
                format = textureParams.Format;
                if (MyCompilationSymbols.ReinterpretFormatsStoredInFiles)
                    if (channel != MyChannel.NormalGloss)
                        format = MyResourceUtils.MakeSrgb(format);
                key.Format = format;
                key.ResolutionInFile = textureParams.Resolution;
                key.MipmapsCount = textureParams.Mipmaps;
            }
            else
            {
                Format format;
                switch(channel)
                {
                    case MyChannel.ColorMetal:
                        format = Format.BC7_UNorm_SRgb;
                        break;
                    case MyChannel.NormalGloss:
                        format = Format.BC7_UNorm;
                        break;
                    case MyChannel.Extension:
                        format = Format.BC7_UNorm_SRgb;
                        break;
                    case MyChannel.Alphamask:
                        format = Format.BC4_UNorm;
                        break;
                    default:
                        MyRenderProxy.Assert(false);
                        format = Format.Unknown;
                        break;
                }
                key.Format = format;
                key.ResolutionInFile = defaultResolution;
                key.MipmapsCount = MyResourceUtils.GetMipmapsCount(Math.Max(key.ResolutionInFile.X, key.ResolutionInFile.Y));
            }

            return GetArrayTextureFromKey(key);
        }

        Vector2I GetDefaultTextureSize(string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath)
        {
            Vector2I cmSize = MyFileTextureParamsManager.GetResolutionFromFile(cmFilepath);
            Vector2I ngSize = MyFileTextureParamsManager.GetResolutionFromFile(ngFilepath);
            Vector2I extSize = MyFileTextureParamsManager.GetResolutionFromFile(extFilepath);
            Vector2I alphamaskSize = MyFileTextureParamsManager.GetResolutionFromFile(alphamaskFilepath);

            Vector2I defaultSize = cmSize;
            if (defaultSize == Vector2I.Zero)
            {
                defaultSize = Vector2I.Max(ngSize, Vector2I.Max(extSize, alphamaskSize));
            }
            if (defaultSize == Vector2I.Zero) // if no texture cannot be found, we use "random size 1k"
                defaultSize = new Vector2I(1024, 1024);
            return defaultSize;
        }

        int GetArrayIndexFromFilepath(string filepath, MyChannel channel, Vector2I resolution)
        {
            IDynamicFileArrayTexture fileArrayTex = GetArrayTextureFromFilepath(filepath, channel, resolution);
            if (fileArrayTex == null)
                return DEFAULT_ARRAY_TEXTURE_INDEX;
            int index = fileArrayTex.GetOrAddSlice(filepath);
            return index;
        }

        public void SetMaterialsSettings(MyMaterialsSettings settings)
        {
            m_ignoredMaterials.Clear();
            foreach (var mat in settings.ChangeableMaterials)
                m_ignoredMaterials.Add(mat.MaterialName);
        }

        public bool IsMaterialAcceptableForTheSystem(MyMeshMaterialInfo info)
        {
            if (m_ignoredMaterials.Contains(info.Name.String))
                return false;

            if (MyFileTextureParamsManager.IsArrayTextureInFile(info.ColorMetal_Texture))
                return false;

            if (MyFileTextureParamsManager.IsArrayTextureInFile(info.NormalGloss_Texture))
                return false;

            if (MyFileTextureParamsManager.IsArrayTextureInFile(info.Extensions_Texture))
                return false;

            if (MyFileTextureParamsManager.IsArrayTextureInFile(info.Alphamask_Texture))
                return false;

            return true;
        }

        HashSet<string> m_checkedFilepaths = new HashSet<string>();

        void CheckTexture(string filepath, MyChannel channel, Format format, Vector2I texSize)
        {
            MyFileTextureParams parameters;
            if (m_checkedFilepaths.Contains(filepath))
                return;
            m_checkedFilepaths.Add(filepath);
            if (MyFileTextureParamsManager.LoadFromFile(filepath, out parameters))
            {
                if (parameters.Format != format)
                    MyRenderProxy.Log.WriteLineAndConsole(String.Format("{0} texture '{1}' should be {2}", channel, filepath, format));
                
                if (parameters.Resolution != texSize)
                    MyRenderProxy.Log.WriteLineAndConsole(String.Format("{0} texture '{1}' should be {2}x{3}", channel, filepath, texSize.X, texSize.Y));
            }
        }

        public void ValidateMaterialTextures(MyMeshMaterialInfo info)
        {
            Vector2I texSize = GetDefaultTextureSize(info.ColorMetal_Texture, info.NormalGloss_Texture, info.Extensions_Texture, info.Alphamask_Texture);

            CheckTexture(info.ColorMetal_Texture, MyChannel.ColorMetal, Format.BC7_UNorm_SRgb, texSize);
            CheckTexture(info.NormalGloss_Texture, MyChannel.NormalGloss, Format.BC7_UNorm, texSize);
            CheckTexture(info.Extensions_Texture, MyChannel.Extension, Format.BC7_UNorm_SRgb, texSize);
            CheckTexture(info.Alphamask_Texture, MyChannel.Alphamask, Format.BC4_UNorm, texSize);
        }

        public MyMeshMaterialId GetOrCreateMaterialId(MyMeshMaterialInfo info)
        {
            Vector2I defaultTextureSize = GetDefaultTextureSize(info.ColorMetal_Texture, info.NormalGloss_Texture,
                info.Extensions_Texture, info.Alphamask_Texture);
            IDynamicFileArrayTexture cmArrayTex = GetArrayTextureFromFilepath(info.ColorMetal_Texture,
                MyChannel.ColorMetal, defaultTextureSize);
            int cmIndex = cmArrayTex.GetOrAddSlice(info.ColorMetal_Texture);
            IDynamicFileArrayTexture ngArrayTex = GetArrayTextureFromFilepath(info.NormalGloss_Texture,
                MyChannel.NormalGloss, defaultTextureSize);
            int ngIndex = ngArrayTex.GetOrAddSlice(info.NormalGloss_Texture);
            IDynamicFileArrayTexture extArrayTex = GetArrayTextureFromFilepath(info.Extensions_Texture,
                MyChannel.Extension, defaultTextureSize);
            int extIndex = extArrayTex.GetOrAddSlice(info.Extensions_Texture);

            info.ColorMetal_Texture = cmArrayTex.Name;
            info.NormalGloss_Texture = ngArrayTex.Name;
            info.Extensions_Texture = extArrayTex.Name;

            MyMaterialKey materialKey = new MyMaterialKey(info.ColorMetal_Texture, info.NormalGloss_Texture, info.Extensions_Texture,
                info.Alphamask_Texture);
            m_setDebugMaterials.Add(materialKey);

            MyGeometryTextureSystemReference geoTextureRef;
            geoTextureRef.ColorMetalTexture = cmArrayTex;
            geoTextureRef.ColorMetalIndex = cmIndex;
            geoTextureRef.NormalGlossTexture = ngArrayTex;
            geoTextureRef.NormalGlossIndex = ngIndex;
            geoTextureRef.ExtensionTexture = extArrayTex;
            geoTextureRef.ExtensionIndex = extIndex;
            geoTextureRef.AlphamaskTexture = null;
            geoTextureRef.AlphamaskIndex = DEFAULT_ARRAY_TEXTURE_INDEX;
            if (!string.IsNullOrEmpty(info.Alphamask_Texture))
            {
                IDynamicFileArrayTexture alphamaskArrayTex = GetArrayTextureFromFilepath(info.Alphamask_Texture,
                    MyChannel.Alphamask, defaultTextureSize);
                int alphamaskIndex = alphamaskArrayTex.GetOrAddSlice(info.Alphamask_Texture);
                info.Alphamask_Texture = alphamaskArrayTex.Name;
                geoTextureRef.AlphamaskTexture = alphamaskArrayTex;
                geoTextureRef.AlphamaskIndex = alphamaskIndex;
            } 
            geoTextureRef.IsUsed = true;
            info.GeometryTextureRef = geoTextureRef;
            return MyMeshMaterials1.GetMaterialId(ref info);
        }

        Vector4I GetTextureIndices(string colorMetalTexture, string normalGlossTexture, string extensionTexture, string alphamaskTexture)
        {
            if (!MyRender11.Settings.UseGeometryArrayTextures)
                return new Vector4I(DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX);

            Vector2I defaultTextureSize = GetDefaultTextureSize(colorMetalTexture, normalGlossTexture, extensionTexture, alphamaskTexture);
            Vector4I index;
            index.X = GetArrayIndexFromFilepath(colorMetalTexture, MyChannel.ColorMetal, defaultTextureSize);
            index.Y = GetArrayIndexFromFilepath(normalGlossTexture, MyChannel.NormalGloss, defaultTextureSize);
            index.Z = GetArrayIndexFromFilepath(extensionTexture, MyChannel.Extension, defaultTextureSize);
            index.W = GetArrayIndexFromFilepath(alphamaskTexture, MyChannel.Alphamask, defaultTextureSize);
            MyRenderProxy.Assert(index.X <= 255, "It is used too many ColorMetal textures with the same resolution");
            MyRenderProxy.Assert(index.Y <= 255, "It is used too many NormalGloss textures with the same resolution");
            MyRenderProxy.Assert(index.Z <= 255, "It is used too many Extension textures with the same resolution");
            MyRenderProxy.Assert(index.W <= 255, "It is used too many Alphamask textures with the same resolution");
            return index;
        }

        public Vector4I[] CreateTextureIndices(List<MyMeshPartInfo> partInfos, int verticesNum, string contentPath)
        {
            Vector4I[] indices = new Vector4I[verticesNum];
            for (int i = 0; i < verticesNum; i++)
                indices[i] = new Vector4I(DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX);

            if (!MyRender11.Settings.UseGeometryArrayTextures) // system is disabled
                return indices; 
            
            foreach (MyMeshPartInfo partInfo in partInfos)
            {
                MyMaterialDescriptor materialDesc = partInfo.m_MaterialDesc;
                if (materialDesc == null)
                    continue;
                string cmTexture, ngTexture, extTexture, alphamaskTexture;
                if (!materialDesc.Textures.TryGetValue("ColorMetalTexture", out cmTexture))
                    continue;
                if (!materialDesc.Textures.TryGetValue("NormalGlossTexture", out ngTexture))
                    continue;
                materialDesc.Textures.TryGetValue("AddMapsTexture", out extTexture);
                materialDesc.Textures.TryGetValue("AlphamaskTexture", out alphamaskTexture);

                cmTexture = MyResourceUtils.GetTextureFullPath(cmTexture, contentPath);
                ngTexture = MyResourceUtils.GetTextureFullPath(ngTexture, contentPath);
                extTexture = MyResourceUtils.GetTextureFullPath(extTexture, contentPath);
                alphamaskTexture = MyResourceUtils.GetTextureFullPath(alphamaskTexture, contentPath);

                Vector4I textureIndices = GetTextureIndices(cmTexture, ngTexture, extTexture, alphamaskTexture);

                foreach (var offset in partInfo.m_indices)
                {
                    indices[offset] = textureIndices;
                }
            }

            return indices;
        }

        public Vector4I[] CreateTextureIndices(List<MyRuntimeSectionInfo> sectionInfos, List<int> indices, int verticesNum)
        {
            Vector4I defaultArrayTexIndex = new Vector4I(DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX, DEFAULT_ARRAY_TEXTURE_INDEX);
            Vector4I[] texIndices = new Vector4I[verticesNum];
            for (int i = 0; i < verticesNum; i++)
                texIndices[i] = defaultArrayTexIndex;

            if (!MyRender11.Settings.UseGeometryArrayTextures) // system is disabled
                return texIndices;

            foreach (MyRuntimeSectionInfo sectionInfo in sectionInfos)
            {
                MyMeshMaterialId material = MyMeshMaterials1.GetMaterialId(sectionInfo.MaterialName);
                MyRenderProxy.Assert(material != MyMeshMaterialId.NULL);
                if (!material.Info.GeometryTextureRef.IsUsed)
                    continue;
                Vector4I materialTexIndex = material.Info.GeometryTextureRef.TextureSliceIndices;

                for (int i = 0; i < sectionInfo.TriCount*3; i++)
                {
                    int index = indices[i + sectionInfo.IndexStart];
                    Vector4I prevTexIndex = texIndices[index];
                    MyRenderProxy.Assert(defaultArrayTexIndex.CompareTo(prevTexIndex) == 0 || materialTexIndex.CompareTo(prevTexIndex) == 0, "Vertex is used with different material!");
                    texIndices[index] = materialTexIndex;
                }
            }

            return texIndices;
        }
    }
}

using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;
using VRage.FileSystem;

namespace VRage.Render11.Resources
{
    internal interface IFileArrayTexture : ITexture
    {
        int SubtexturesCount { get; }
    }

    namespace Internal
    {
        class MyFileArrayTexture : IFileArrayTexture
        {
            static MyObjectsPool<List<string>> m_objectsPoolOfStringLists = new MyObjectsPool<List<string>>(16);

            // immutable data   
            string m_resourceName;
            Vector2I m_size;
            MyFileTextureEnum m_type;
            List<string> m_listSubresourceFilenames;

            struct MyErrorRecoverySystem
            {
                public bool UseErrorTexture;
                public string TextureFilepath;

                public bool UseBytePattern;
                public Format FormatBytePattern;
                public byte[] BytePattern;
            }
            MyErrorRecoverySystem m_recoverySystem = new MyErrorRecoverySystem();

            ShaderResourceView m_srv;
            Resource m_resource;

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public string Name
            {
                get { return m_resourceName; }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_size.X, m_size.Y, m_listSubresourceFilenames.Count); }
            }

            public Vector2I Size
            {
                get { return m_size; }
            }

            public int MipmapCount { get; private set; }

            public long ByteSize { get; private set; }

            public Format Format { get; private set; }

            internal List<string> SubresourceFilenames { get {return m_listSubresourceFilenames;}}

            public int SubtexturesCount
            {
                get { return m_listSubresourceFilenames.Count; }
            }

            public void Load(string resourceName, string[] filePaths, MyFileTextureEnum type, string errorTextureFilepath)
            {
                m_resourceName = resourceName;

                if (m_listSubresourceFilenames == null)
                    m_objectsPoolOfStringLists.AllocateOrCreate(out m_listSubresourceFilenames);
                m_listSubresourceFilenames.Clear();
                foreach (string path in filePaths)
                    m_listSubresourceFilenames.Add(path);
                m_type = type;

                ISrvBindable tex = MyManagers.FileTextures.GetTexture(filePaths[0], type, temporary: true);
                m_size = tex.Size;
                Format = Format.Unknown;
                m_recoverySystem.UseErrorTexture = true;
                m_recoverySystem.TextureFilepath = errorTextureFilepath;
            }

            public void Load(string resourceName, string[] filePaths, MyFileTextureEnum type, byte[] bytePattern, Format formatBytePattern)
            {
                m_resourceName = resourceName;

                if (m_listSubresourceFilenames == null)
                    m_objectsPoolOfStringLists.AllocateOrCreate(out m_listSubresourceFilenames);
                m_listSubresourceFilenames.Clear();
                foreach (string path in filePaths)
                    m_listSubresourceFilenames.Add(path);
                m_type = type;

                ISrvBindable tex = MyManagers.FileTextures.GetTexture(filePaths[0], type, temporary: true);
                m_size = tex.Size;
                Format = Format.Unknown;
                m_recoverySystem.UseBytePattern = true;
                m_recoverySystem.FormatBytePattern = formatBytePattern;
                m_recoverySystem.BytePattern = bytePattern;
            }
            
            public void Unload()
            {
                m_listSubresourceFilenames.Clear();
                m_objectsPoolOfStringLists.Deallocate(m_listSubresourceFilenames);
                m_listSubresourceFilenames = null;
                Format = Format.Unknown;
            }

            // if no file texture can be loaded, the function will return false and default value in parameters
            bool GetCorrectedFileTextureParams(out MyFileTextureParams parameters)
            {
                //parameters = new MyFileTextureParams();
                foreach (string filepath in m_listSubresourceFilenames)
                {
                    if (MyFileTextureParamsManager.LoadFromFile(filepath, out parameters))
                    {
                        if (MyCompilationSymbols.ReinterpretFormatsStoredInFiles)
                            if (m_type != MyFileTextureEnum.NORMALMAP_GLOSS)
                                parameters.Format = MyResourceUtils.MakeSrgb(parameters.Format);

                        int skipMipmaps = 0;
                        if (m_type != MyFileTextureEnum.GUI && m_type != MyFileTextureEnum.GPUPARTICLES)
                            skipMipmaps = MyRender11.Settings.User.TextureQuality.MipmapsToSkip(parameters.Resolution.X, parameters.Resolution.Y);

                        if (parameters.Mipmaps > 1)
                        {
                            parameters.Mipmaps -= skipMipmaps;
                            parameters.Resolution.X = MyResourceUtils.GetMipmapSize(parameters.Resolution.X, skipMipmaps);
                            parameters.Resolution.Y = MyResourceUtils.GetMipmapSize(parameters.Resolution.Y, skipMipmaps);
                        }
                        return true;
                    }
                }
                parameters.Format = m_recoverySystem.FormatBytePattern;
                parameters.Mipmaps = 3;
                parameters.Resolution = new Vector2I(4, 4);
                parameters.ArraySize = 1;
                return false;
            }

            public void OnDeviceInit(MyFileArrayTexture source = null)
            {
                MyFileTextureParams fileTexParams;
                bool ret = GetCorrectedFileTextureParams(out fileTexParams);
                //MyRenderProxy.Assert(ret, "It is not implemented mechanism, what to do, when none of the textures exist");

                m_size = fileTexParams.Resolution;
                Texture2DDescription desc = new Texture2DDescription();
                desc.ArraySize = m_listSubresourceFilenames.Count;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.CpuAccessFlags = CpuAccessFlags.None;
                desc.Format = fileTexParams.Format == Format.Unknown ? Format.BC1_UNorm_SRgb : fileTexParams.Format;
                desc.Height = (int) Size.Y;
                desc.Width = (int) Size.X;
                var mipmaps = desc.MipLevels = fileTexParams.Mipmaps;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;
                desc.Usage = ResourceUsage.Default;
                m_resource = new Texture2D(MyRender11.Device, desc);
                m_resource.DebugName = m_resourceName;
                Format = desc.Format;
                MipmapCount = fileTexParams.Mipmaps;

                // foreach mip
                int i = 0;
                foreach (var path in m_listSubresourceFilenames)
                {
                    Texture2DDescription description;
                    int sourceSlice;
                    IResource resource = GetResource(path, m_type, source, i, new Vector2I(desc.Width, desc.Height), out description, out sourceSlice);
                    bool consistent = MyResourceUtils.CheckTexturesConsistency(desc, description);
                    if (!consistent)
                    {
                        if (!string.IsNullOrEmpty(path) && MyFileSystem.FileExists(path))
                        {
                            string msg =
                                string.Format(
                                    "Texture {0} cannot be loaded. If this message is displayed on reloading textures, please restart the game. If it is not, please notify developers.", path);
                            MyRenderProxy.Fail(msg);
                        }
                    }

                    if (!consistent && m_recoverySystem.UseErrorTexture) // if the texture cannot be used, error texture will be used
                    {
                        var texture = MyManagers.FileTextures.GetTexture(m_recoverySystem.TextureFilepath, m_type, true, temporary: true);
                        var tex2D = texture.Resource as Texture2D;
                        MyRenderProxy.Assert(tex2D != null,
                            "MyFileArrayTexture supports only 2D textures. Inconsistent texture: " + m_recoverySystem.TextureFilepath);
                        description = tex2D.Description;
                        sourceSlice = 0;
                        consistent = MyResourceUtils.CheckTexturesConsistency(desc, description);
                    }

                    IGeneratedTexture generatedTexture = null;
                    if (!consistent && m_recoverySystem.UseBytePattern) // if the texture cannot be used, byte pattern will be used to generate texture
                    {
                        generatedTexture = MyManagers.GeneratedTextures.CreateFromBytePattern("MyFileArrayTexture.Tmp", desc.Width,
                            desc.Height, m_recoverySystem.FormatBytePattern, m_recoverySystem.BytePattern);
                        resource = generatedTexture;
                        var tex2D = generatedTexture.Resource as Texture2D;
                        description = tex2D.Description;
                        sourceSlice = 0;
                        MyRenderProxy.Assert(tex2D != null, "MyFileArrayTexture supports only 2D textures");
                        consistent = MyResourceUtils.CheckTexturesConsistency(desc, description);
                    }

                    if (!consistent)
                    {
                        Texture2DDescription desc1 = desc;
                        Texture2DDescription desc2 = description;
                        string errorMsg = string.Format("Textures ({0}) is not compatible within array texture! Width: ({1},{2}) Height: ({3},{4}) Mipmaps: ({5},{6}) Format: ({7},{8})",
                            path, desc1.Width, desc2.Width, desc1.Height, desc2.Height, desc1.MipLevels, desc2.MipLevels, desc1.Format, desc2.Format);
                        MyRenderProxy.Error(errorMsg);
                        MyRender11.Log.WriteLine(errorMsg);
                    }

                    if (consistent)
                    {
                        for (int m = 0; m < mipmaps; m++)
                        {
                            MyRender11.RC.CopySubresourceRegion(resource,
                                Resource.CalculateSubResourceIndex(m, sourceSlice, mipmaps), null, Resource,
                                Resource.CalculateSubResourceIndex(m, i, mipmaps));

                            int sizeX = Resource.CalculateMipSize(m, Size.X);
                            int sizeY = Resource.CalculateMipSize(m, Size.Y);
                            ByteSize += FormatHelper.ComputeScanlineCount(Format, sizeX) * 4 * FormatHelper.ComputeScanlineCount(Format, sizeY) * 4 * FormatHelper.SizeOfInBytes(Format);
                        }
                    }

                    if (generatedTexture != null)
                        MyManagers.GeneratedTextures.DisposeTex(generatedTexture);

                    i++;
                }

                m_srv = new ShaderResourceView(MyRender11.Device, Resource);
            }

            public void OnDeviceEnd()
            {
                ByteSize = 0;

                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }

                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }
            }

            private static IResource GetResource(string filepath, MyFileTextureEnum type, MyFileArrayTexture referenceArray, int referenceSlice, Vector2I textureSize, out Texture2DDescription description, out int sourceSlice)
            {
                if (referenceArray != null && referenceSlice < referenceArray.Size3.Z && referenceArray.Size == textureSize && filepath == referenceArray.SubresourceFilenames[referenceSlice])
                {
                    var tex2d = referenceArray.Resource as Texture2D;
                    description = tex2d.Description;
                    sourceSlice = referenceSlice;
                    return referenceArray;
                }
                else
                {
                    var texture = MyManagers.FileTextures.GetTexture(filepath, type, true, temporary: true);
                    var tex2d = texture.Resource as Texture2D;
                    if (tex2d == null)
                    {
                        var tex2D = texture.Resource as Texture2D;
                        MyRenderProxy.Assert(tex2D != null,
                            "MyFileArrayTexture supports only 2D textures. Inconsistent texture: " + filepath);

                        description = new Texture2DDescription();
                        sourceSlice = -1;
                        return null;
                    }

                    description = tex2d.Description;
                    sourceSlice = 0;
                    return texture;
                }
            }
        }
    }

    class MyFileArrayTextureManager : IManager, IManagerDevice, IManagerUnloadData
    {
        MyTextureStatistics m_statistics = new MyTextureStatistics();
        MyObjectsPool<MyFileArrayTexture> m_fileTextureArrays = new MyObjectsPool<MyFileArrayTexture>(16);
        bool m_isDeviceInit = false;
        HashSet<MyFileArrayTexture> m_texturesOnAutoDisposal = new HashSet<MyFileArrayTexture>();

        public IFileArrayTexture CreateFromFiles(string resourceName, string[] inputFiles, MyFileTextureEnum type, string errorTextureFilepath, bool autoDisposeOnUnload, MyFileArrayTexture source = null)
        {
            MyFileArrayTexture array;
            m_fileTextureArrays.AllocateOrCreate(out array);
            array.Load(resourceName, inputFiles, type, errorTextureFilepath);

            if (m_isDeviceInit)
                array.OnDeviceInit(source);

            if (autoDisposeOnUnload)
                m_texturesOnAutoDisposal.Add(array);

            m_statistics.Add(array);
            return array;
        }

        // if texture cannot be loaded, it will be used byte pattern to generate substition texture
        public IFileArrayTexture CreateFromFiles(string resourceName, string[] inputFiles, MyFileTextureEnum type, byte[] bytePatternFor4x4, Format formatBytePattern, bool autoDisposeOnUnload, MyFileArrayTexture source = null)
        {
            MyFileArrayTexture array;
            m_fileTextureArrays.AllocateOrCreate(out array);
            array.Load(resourceName, inputFiles, type, bytePatternFor4x4, formatBytePattern);

            if (m_isDeviceInit)
                array.OnDeviceInit(source);

            if (autoDisposeOnUnload)
                m_texturesOnAutoDisposal.Add(array);

            m_statistics.Add(array);
            return array;
        }
        
        public bool CheckConsistency(string[] inputFiles)
        {
            MyRenderProxy.Assert(inputFiles.Length != 0);
            MyFileTextureManager texManager = MyManagers.FileTextures;
            ISrvBindable firstSrvBindable = texManager.GetTexture(inputFiles[0], MyFileTextureEnum.GPUPARTICLES, true, temporary: true);
            Texture2D firstTex2D = firstSrvBindable.Resource as Texture2D;
            if (firstTex2D == null)
                return false;
            for (int i = 1; i < inputFiles.Length; i++)
            {
                ISrvBindable srvBindable = texManager.GetTexture(inputFiles[i], MyFileTextureEnum.GPUPARTICLES, true, temporary: true);
                Texture2D tex2D = srvBindable.Resource as Texture2D;
                if (tex2D == null)
                    return false;
                bool consistent = MyResourceUtils.CheckTexturesConsistency(firstTex2D.Description, tex2D.Description);

                if (!consistent)
                    return false;
            }
            return true;
        }

        public int GetTexturesCount()
        {
            return m_fileTextureArrays.ActiveCount;
        }

        public long GetTotalByteSizeOfResources()
        {
            long totalSize = 0;
            foreach (var tex in m_fileTextureArrays.Active)
            {
                totalSize += tex.ByteSize;
            }
            return totalSize;
        }

        public StringBuilder GetFileTexturesDesc()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Loaded file textures:");
            builder.AppendLine("[Debug name    Width x Height x Slices   Format    Internal texture type    Size]");
            foreach (var tex in m_fileTextureArrays.Active)
            {
                builder.AppendFormat("{0}    {1} x {2} x {3}    {4}    {5}    {6} bytes", tex.Name.Replace("/", @"\"), tex.Size.X, tex.Size.Y, tex.Size3.Z, tex.Format, tex.SubtexturesCount, tex.ByteSize);
                builder.AppendLine("    ");
                foreach (var subtex in tex.SubresourceFilenames)
                    builder.AppendFormat("{0}, ", subtex);
                builder.AppendLine();
            }
            return builder;
        }


        public void DisposeTex(ref IFileArrayTexture texture)
        {
            if (texture == null)
                return;

            MyFileArrayTexture textureInternal = (MyFileArrayTexture) texture;

            if (m_isDeviceInit)
                textureInternal.OnDeviceEnd();

            if (m_texturesOnAutoDisposal.Contains(textureInternal))
                m_texturesOnAutoDisposal.Remove(textureInternal);

            m_fileTextureArrays.Deallocate(textureInternal);
            m_statistics.Remove(textureInternal);
            texture = null;
        }
        
        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach (var tex in m_fileTextureArrays.Active)
                tex.OnDeviceInit();
        }

        public void OnDeviceReset()
        {
            foreach (var tex in m_fileTextureArrays.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;
            foreach (var tex in m_fileTextureArrays.Active)
                tex.OnDeviceEnd();
        }

        public void OnUnloadData()
        {
            MyFileArrayTexture[] array = new MyFileArrayTexture[m_texturesOnAutoDisposal.Count];
            m_texturesOnAutoDisposal.CopyTo(array);
            foreach (var myTex in array)
            {
                IFileArrayTexture tex = myTex;
                DisposeTex(ref tex);
            }
            m_texturesOnAutoDisposal.Clear();
        }

        public MyTextureStatistics Statistics
        {
            get { return m_statistics; }
        }
    }
}

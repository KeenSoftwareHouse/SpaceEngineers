using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using System.Collections.Generic;
using System.Text;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;

namespace VRage.Render11.Resources
{
    internal interface IFileArrayTexture : ISrvBindable
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

            public long ByteSize { get; private set; }

            public Format TextureFormat { get; private set; }

            internal List<string> SubresourceFilenames { get {return m_listSubresourceFilenames;}}

            public int SubtexturesCount
            {
                get { return m_listSubresourceFilenames.Count; }
            }

            public void Load(string resourceName, string[] filePaths, MyFileTextureEnum type)
            {
                m_resourceName = resourceName;

                if (m_listSubresourceFilenames == null)
                    m_objectsPoolOfStringLists.AllocateOrCreate(out m_listSubresourceFilenames);
                m_listSubresourceFilenames.Clear();
                foreach (string path in filePaths)
                    m_listSubresourceFilenames.Add(path);
                m_type = type;

                ISrvBindable tex = MyManagers.FileTextures.GetTexture(filePaths[0], type);
                m_size = tex.Size;
                TextureFormat = Format.Unknown;
            }

            public void Unload()
            {
                m_listSubresourceFilenames.Clear();
                m_objectsPoolOfStringLists.Deallocate(m_listSubresourceFilenames);
                m_listSubresourceFilenames = null;
                TextureFormat = Format.Unknown;
            }

            public void OnDeviceInit()
            {
                ISrvBindable firstTex = MyManagers.FileTextures.GetTexture(m_listSubresourceFilenames[0], m_type, true);
                var srcDesc = firstTex.Srv.Description;
                Vector2I Size = firstTex.Size;

                Texture2DDescription desc = new Texture2DDescription();
                desc.ArraySize = m_listSubresourceFilenames.Count;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.CpuAccessFlags = CpuAccessFlags.None;
                desc.Format = srcDesc.Format;
                desc.Height = (int) Size.Y;
                desc.Width = (int) Size.X;
                desc.MipLevels = srcDesc.Texture2D.MipLevels;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;
                desc.Usage = ResourceUsage.Default;
                m_resource = new Texture2D(MyRender11.Device, desc);
                m_resource.DebugName = m_resourceName;
                TextureFormat = srcDesc.Format;

                // foreach mip
                var mipmaps = srcDesc.Texture2D.MipLevels;

                int i = 0;
                foreach (var path in m_listSubresourceFilenames)
                {
                    ISrvBindable tex = MyManagers.FileTextures.GetTexture(path, m_type, true);
                    var tex2D = tex.Resource as Texture2D;
                    MyRenderProxy.Assert(tex2D != null,
                        "MyTextureArray supports only 2D textures. Inconsistent texture: " + tex.Name);
                    bool consistent = MyResourceUtils.CheckTexturesConsistency(desc, tex2D.Description);
                    if (!consistent)
                    {
                        string errorMsg =
                            "All MyTextureArray has to have the same pixel format, width / height and # of mipmaps. Inconsistent textures: " +
                            tex.Name + " / " + firstTex.Name;
                        MyRenderProxy.Error(errorMsg);
                        MyRender11.Log.WriteLine(errorMsg);
                    }

                    for (int m = 0; m < mipmaps; m++)
                    {
                        MyRender11.RC.CopySubresourceRegion(tex2D,
                            Resource.CalculateSubResourceIndex(m, 0, mipmaps), null, Resource,
                            Resource.CalculateSubResourceIndex(m, i, mipmaps));

                        int sizeX = Resource.CalculateMipSize(m, Size.X);
                        int sizeY = Resource.CalculateMipSize(m, Size.Y);
                        ByteSize += FormatHelper.ComputeScanlineCount(TextureFormat, sizeX) * 4 * FormatHelper.ComputeScanlineCount(TextureFormat, sizeY) * 4 * FormatHelper.SizeOfInBytes(TextureFormat);
                    }

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
        }
    }

    class MyFileArrayTextureManager : IManager, IManagerDevice
    {
        MyObjectsPool<MyFileArrayTexture> m_fileTextureArrays = new MyObjectsPool<MyFileArrayTexture>(16);
        bool m_isDeviceInit = false;

        public IFileArrayTexture CreateFromFiles(string resourceName, string[] inputFiles,
            MyFileTextureEnum type)
        {
            MyFileArrayTexture array;
            m_fileTextureArrays.AllocateOrCreate(out array);
            array.Load(resourceName, inputFiles, type);

            if (m_isDeviceInit)
                array.OnDeviceInit();

            return array;
        }

        public bool CheckConsistency(string[] inputFiles)
        {
            MyRenderProxy.Assert(inputFiles.Length != 0);
            MyFileTextureManager texManager = MyManagers.FileTextures;
            ISrvBindable firstSrvBindable = texManager.GetTexture(inputFiles[0], MyFileTextureEnum.GPUPARTICLES, true);
            Texture2D firstTex2D = firstSrvBindable.Resource as Texture2D;
            if (firstTex2D == null)
                return false;
            for (int i = 1; i < inputFiles.Length; i++)
            {
                ISrvBindable srvBindable = texManager.GetTexture(inputFiles[i], MyFileTextureEnum.GPUPARTICLES, true);
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
                builder.AppendFormat("{0}    {1} x {2} x {3}    {4}    {5}    {6} bytes", tex.Name.Replace("/", @"\"), tex.Size.X, tex.Size.Y, tex.Size3.Z, tex.TextureFormat, tex.SubtexturesCount, tex.ByteSize);
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

            m_fileTextureArrays.Deallocate(textureInternal);
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
    }
}

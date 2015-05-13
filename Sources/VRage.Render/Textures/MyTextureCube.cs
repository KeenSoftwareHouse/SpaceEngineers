using System.Diagnostics;
using System.IO;

using SharpDX.Direct3D9;
using System;
using VRageRender.Utils;

namespace VRageRender.Textures
{
    /// <summary>
    /// 
    /// </summary>
    internal class MyTextureCube: MyTexture
    {
        #region Properties

        /// <summary>
        /// Gets the size in pixels.
        /// </summary>
        public int Size 
        { 
            get
            {
                if (RequestAccess())
                {
                    return ((SharpDX.Direct3D9.CubeTexture)this.texture).GetLevelDescription(0).Width;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the memory used by texture in MB.
        /// </summary>
        public override float Memory
        {
            get
            {
                if (this.LoadState == LoadState.Loaded)
                {
                    return (float)MyUtilsRender9.GetTextureSizeInMb(this);
                }

                return 0f;
            }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="MyTexture2D"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="manager">The manager.</param>
        /// <param name="loadMethod">if set to <c>true</c> [external load].</param>
        /// <param name="flags">The flags.</param>
        public MyTextureCube(string contentDir, string path, LoadMethod loadMethod, TextureFlags flags)
            : base(contentDir, path, loadMethod, flags)
        {
        }



        protected override SharpDX.Direct3D9.BaseTexture LoadPNGTexture(string fileName)
        {
            return null;
        }

        /// <summary>
        /// Loads the DDS texture.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="quality"></param>
        /// <returns></returns>
        protected override SharpDX.Direct3D9.BaseTexture LoadDDSTexture(string name, TextureQuality quality)
        {
            try
            {
                var device = MyRender.GraphicsDevice;
                if (device == null || device.IsDisposed)
                {
                    return null;
                }

                if (this.flags.HasFlag(TextureFlags.IgnoreQuality))
                {
                    quality = TextureQuality.Full;
                }

                CubeTexture loadedTexture;
                MyDDSFile.DDSFromFile(name, device, true, (int)quality, out loadedTexture);
                loadedTexture.Tag = this;

                return loadedTexture;
            }
            catch (FileNotFoundException)
            {
                
            }
            catch (Exception ddsException)
            {
                Debug.WriteLine(string.Format("W:Texture Cube (DDS) {0}", ddsException.Message));
            }

            return null;
        }

        #region Operators
          

        /// <summary>
        /// Performs an implicit conversion from <see cref="Sandbox.AppCode.Game.Textures.MyTextureCube"/> to <see cref="VRageMath.Graphics.Texture"/>.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SharpDX.Direct3D9.CubeTexture(MyTextureCube right)
        {
            if (right == null)
            {
                return null;
            }

            right.RequestAccess();

            return (SharpDX.Direct3D9.CubeTexture)right.texture;
        }

        #endregion
    }
}
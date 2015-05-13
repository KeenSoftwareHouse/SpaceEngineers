using System;

using VRage.Utils;
using VRageMath;

//
//using VRageMath.Graphics;

using SharpDX.Direct3D9;
using VRageRender.Utils;
using System.Diagnostics;
using System.IO;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace VRageRender.Textures
{
    /// <summary>
    /// 
    /// </summary>
    internal class MyTexture2D : MyTexture
    {
        #region Properties

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                if (RequestAccess())
                {
                    return new Rectangle(0, 0, ((Texture)this.texture).GetLevelDescription(0).Width, ((Texture)this.texture).GetLevelDescription(0).Height);
                }

                return new Rectangle(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height
        {
            get
            {
                if (RequestAccess())
                {
                    return ((Texture)this.texture).GetLevelDescription(0).Height;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width
        {
            get
            {
                if (RequestAccess())
                {
                    return ((Texture)this.texture).GetLevelDescription(0).Width;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the size of texture in MB.
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
        private MyTexture2D(Texture right)
            : base(string.Empty, string.Empty, LoadMethod.Lazy, TextureFlags.None)
        {
            this.texture = right;
            this.LoadState = LoadState.Loaded;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyTexture2D"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="manager">The manager.</param>
        /// <param name="loadMethod">if set to <c>true</c> [external load].</param>
        /// <param name="flags">The flags.</param>
        public MyTexture2D(string contentDir, string path, LoadMethod loadMethod, TextureFlags flags)
            : base(contentDir, path, loadMethod, flags)
        {
        }

        protected override BaseTexture LoadPNGTexture(string fileName)
        {
            var path = Path.Combine(MyFileSystem.ContentPath, fileName);
            using (var stream = MyFileSystem.OpenRead(path))
            {
                return Texture.FromStream(MyRender.GraphicsDevice, stream);
            }
        }

        /// <summary>
        /// Loads the DDS texture.
        /// </summary>
        /// <param name="fileName">The name.</param>
        /// <param name="quality"></param>
        /// <returns></returns>
        protected override BaseTexture LoadDDSTexture(string fileName, TextureQuality quality)
        {
            var device = MyRender.GraphicsDevice;
            if (device == null || device.IsDisposed)
            {
                return null;
            }

            //cannot use profiler because of multithreading
            //int loadDDSTextureBlock = -1;
            //VRageRender.MyRender.GetRenderProfiler().StartProfilingBlock("MyTexture2D.LoadDDSTexture", ref loadDDSTextureBlock);

            //MyRender.Log.WriteLine(string.Format("Loading DDS texture {0} ...", fileName), SysUtils.LoggingOptions.LOADING_TEXTURES);

            Texture loadedTexture = null;

            if (this.flags.HasFlag(TextureFlags.IgnoreQuality))
            {
                quality = TextureQuality.Full;
            }

            MyDDSFile.DDSFromFile(fileName, device, true, (int)quality, out loadedTexture);
            loadedTexture.Tag = this;

            if (!MathHelper.IsPowerOfTwo(loadedTexture.GetLevelDescription(0).Width) || !MathHelper.IsPowerOfTwo(loadedTexture.GetLevelDescription(0).Height))
            {
                throw new FormatException("Size must be power of two!");
            }

            //cannot use profiler because of multithreading
            //VRageRender.MyRender.GetRenderProfiler().EndProfilingBlock(loadDDSTextureBlock);
            return loadedTexture;
        }


        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (MyUtilsRender9.IsTextureMipMapped(this) == false)
            {
                //MyRender.Log.IncreaseIndent();
                MyRender.Log.WriteLine("TextureNotMipMapped " + this.Name.ToString());
                //MyRender.Log.DecreaseIndent();
            }       /*
            if (MyUtils.IsTextureDxtCompressed(this) == false)
            {
                MyPerformanceCounter.PerAppLifetime.NonDxtCompressedTexturesCount++;
                //MyRender.Log.IncreaseIndent();
                MyRender.Log.WriteLine("TextureNotCompressed " + this.Name.ToString());
                //MyRender.Log.DecreaseIndent();
            }     */
        }

        protected override void OnUnloading()
        {
            base.OnUnloading();
        }

        #region Operators

        public static implicit operator MyTexture2D(Texture right)
        {
            if (right == null)
            {
                return null;
            }

            return new MyTexture2D(right);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Sandbox.AppCode.Game.Textures.MyTexture2D"/> to <see cref="VRageMath.Graphics.Texture2D"/>.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SharpDX.Direct3D9.Texture(MyTexture2D right)
        {
            if (right == null || !right.RequestAccess())
            {
                return null;
            }

            return (SharpDX.Direct3D9.Texture)right.texture;
        }

        #endregion
    }
}
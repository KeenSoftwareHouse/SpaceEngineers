using System;
using System.Diagnostics;
using System.IO;

//using VRageMath.Graphics;
using VRage.Utils;
using SharpDX.Direct3D9;
using SharpDX;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace VRageRender.Textures
{
    /// <summary>
    /// Loading flags
    /// </summary>
    [Flags]
    public enum TextureFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 1 << 0,

        /// <summary>
        /// Texture will ignore any quality override and always will use TextureQuality.Full 
        /// </summary>
        IgnoreQuality = 1 << 1,


        /// <summary>
        /// If there is no texture, it is no problem
        /// </summary>
        CanBeMissing = 1 << 2,
    }

    internal enum MyTextureClassEnum
    {
        Unknown = 0,
        DiffuseEmissive,
        NormalSpecular,
    }

    /// <summary>
    /// The load method for a texture.
    /// </summary>
    internal enum LoadMethod
    {
        /// <summary>
        /// Someone else loads me.
        /// </summary>
        External,

        /// <summary>
        /// I load myself synchronously when needed for the first time.
        /// </summary>
        Lazy,

        /// <summary>
        /// I start loading myself asynchronously when needed for the first time.
        /// </summary>
        LazyBackground,
    }


    /// <summary>
    /// Texture loading mode
    /// </summary>
    internal enum LoadingMode
    {
        /// <summary>
        /// Texture is loaded texture immidiately.
        /// </summary>
        Immediate,

        /// <summary>
        /// Texture is scheduled for load on background thread.
        /// </summary>
        Background,

        /// <summary>
        /// Texture is loaded on first access.
        /// </summary>
        Lazy,

        /// <summary>
        /// Texture is loaded on first access on background thread.
        /// </summary>
        LazyBackground,
    }

    /// <summary>
    /// Loading state of texture.
    /// </summary>
    internal enum LoadState
    {
        Loaded,
        LoadYourself,
        Pending,
        Unloaded,
        Error,
        Loading,
        LoadYourselfBackground,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="texture">The texture.</param>
    internal delegate void TextureLoadedHandler(MyTexture texture);

    /// <summary>
    /// 
    /// </summary>
    internal abstract class MyTexture
    {
        #region Fields

        /// <summary>
        /// XNA internl texture.
        /// </summary>
        protected SharpDX.Direct3D9.BaseTexture texture;

        /// <summary>
        /// Texture flags setting
        /// </summary>
        protected readonly TextureFlags flags;

        /// <summary>
        /// State of loading.
        /// </summary>
        private volatile LoadState loadState;

        public readonly MyTextureClassEnum TextureClassDebug;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [texture loaded].
        /// </summary>
        public event TextureLoadedHandler TextureLoaded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the level count.
        /// </summary>
        public int LevelCount
        {
            get
            {
                if (RequestAccess())
                {
                    return this.texture.LevelCount;
                }

                return 0;
            }
        }

        public string Name { get; protected set; }

        public string ContentDir { get; protected set; }

        /// <summary>
        /// Gets the format.
        /// </summary>
        public Format Format
        {
            get
            {
                if (RequestAccess())
                {
                    Texture tex = texture as Texture;
                    if (tex != null)
                        return tex.GetLevelDescription(0).Format;

                    CubeTexture ctex = texture as CubeTexture;
                    if (ctex != null)
                        return ctex.GetLevelDescription(0).Format;
                }
                return Format.Unknown;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid
        {
            get
            {                                                           //alocation!
                return this.texture != null && !this.texture.IsDisposed /*&& !this.texture.Device.IsDisposed*/;
            }
        }

        /// <summary>
        /// Gets or sets the state of the load.
        /// </summary>
        /// <value>
        /// The state of the load.
        /// </value>
        public LoadState LoadState
        {
            get
            {
                return this.loadState;
            }
            internal set
            {
                this.loadState = value;
            }
        }

        /// <summary>
        /// Gets the size of texture in MB.
        /// </summary>
        public abstract float Memory { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="MyTexture"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="manager">The manager.</param>
        /// <param name="loadMethod">The load method. See <code>LoadMethod</code> enum.</param>
        /// <param name="flags">The flags.</param>
        protected MyTexture(string contentDir, string path, LoadMethod loadMethod, TextureFlags flags)
        {
            this.flags = flags;
            this.Name = path;
            this.ContentDir = contentDir;

            //  this.Manager = manager;
            switch (loadMethod)
            {
                case LoadMethod.External:
                    this.LoadState = LoadState.Pending;
                    break;
                case LoadMethod.Lazy:
                    this.LoadState = LoadState.LoadYourself;
                    break;
                case LoadMethod.LazyBackground:
                    this.LoadState = LoadState.LoadYourselfBackground;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("loadMethod");
            }

            SetTextureClass(path, ref TextureClassDebug);
        }

        [Conditional("DEBUG")]
        private void SetTextureClass(string path, ref MyTextureClassEnum result)
        {
            if (path.Contains("_de", StringComparison.InvariantCultureIgnoreCase))
                result = MyTextureClassEnum.DiffuseEmissive;
            else if (path.Contains("_ns", StringComparison.InvariantCultureIgnoreCase))
                result = MyTextureClassEnum.NormalSpecular;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="MyTexture"/> is reclaimed by garbage collection.
        /// </summary>
        ~MyTexture()
        {
            //This storkovina crashes with multiple VSync changes
            // MyTextureManager.UnloadTexture(this);
        }

        public bool IgnoreQuality
        {
            get
            {
                return flags.HasFlag(TextureFlags.IgnoreQuality);
            }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        internal bool Load(TextureQuality quality = 0, bool canBeMissing = false)
        {
            // TODO: !PetrM fix skipping mipmaps
            quality = 0;

            Debug.Assert(this.LoadState != LoadState.Loaded);

            bool loaded = false;

            if (!string.IsNullOrEmpty(ContentDir))
                loaded = Load(ContentDir, quality, canBeMissing);

            if (!loaded)
                loaded = Load(MyFileSystem.ContentPath, quality, canBeMissing);

            if (loaded && this.texture != null)
            {
                this.texture.DebugName = this.Name + " (Render)";
                this.LoadState = LoadState.Loaded;

                OnLoaded();
            }

            return loaded;
        }

        bool Load(string contentDir, TextureQuality quality, bool canBeMissing)
        {
            string ext = Path.GetExtension(Name);

            if (String.IsNullOrEmpty(ext))
            {
                Debug.Fail("Texture without extension: " + Name);
                Name += ".dds";
                ext = ".dds";
            }

            string path = Path.Combine(contentDir, Name);

            if (MyFileSystem.FileExists(path))
            {
                try
                {
                    if (ext.Equals(".dds", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.texture = LoadDDSTexture(path, quality);
                    }
                    else if (ext.Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.texture = LoadPNGTexture(path);
                    }
                    else
                    {
                        Debug.Fail("Unsupported texture format: " + path);
                        MyRender.Log.WriteLine(String.Format("Unsupported texture format: {0}", path));
                    }
                }
                catch (SharpDXException )
                {
                    MyRender.Log.WriteLine(String.Format("Error decoding texture, file might be corrupt, quality {1}: {0}", path, quality));
                }
                catch (Exception e)
                {
                    MyRender.Log.WriteLine(String.Format("Error loading texture, quality {1}: {0}", path, quality));
                    throw new ApplicationException("Error loading texture: " + path, e);
                }
            }

            if (!canBeMissing && this.texture == null)
            {
                this.LoadState = LoadState.Error;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Unloads this instance.
        /// </summary>
        internal void Unload()
        {
            Debug.Assert(this.loadState == LoadState.Loaded);

            OnUnloading();

            this.texture.Dispose();
            this.texture = null;

            this.LoadState = LoadState.Unloaded;

            //slowdown
            //Debug.WriteLine(string.Format("Texture {0} unloaded.", this.Name));
        }

        /// <summary>
        /// Loads the DDS texture.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="quality"></param>
        /// <returns></returns>
        protected abstract SharpDX.Direct3D9.BaseTexture LoadDDSTexture(string name, TextureQuality quality);

        protected abstract SharpDX.Direct3D9.BaseTexture LoadPNGTexture(string name);

        /// <summary>
        /// Request access to xna texture.
        /// </summary>
        /// <returns>true if data from texture can be readed.</returns>
        protected bool RequestAccess()
        {
            lock (this)
            {
                switch (this.LoadState)
                {
                    case LoadState.Loaded:
                        if (IsValid)
                        {
                            return true;
                        }
                        else
                        {
                            Unload();
                            return Load();
                        }

                    case LoadState.Unloaded:
                    case LoadState.LoadYourself:
                        return Load();

                    case LoadState.LoadYourselfBackground:
                        {
                            bool immediate = MyTextureManager.OverrideLoadingMode.HasValue && MyTextureManager.OverrideLoadingMode.Value == LoadingMode.Immediate;

                            if (immediate)
                            {
                                return Load();
                            }
                            else
                            {
                                MyTextureManager.LoadTextureInBackground(this);
                                loadState = LoadState.Pending;
                                return false;
                            }
                        }

                    case LoadState.Pending:
                    case LoadState.Error:
                        return false;

                    default:
                        System.Diagnostics.Debug.Assert(false);
                        return false;
                }
            }
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected virtual void OnLoaded()
        {
            if (this.TextureLoaded != null)
            {
                this.TextureLoaded(this);
            }

            //var textureManager = this.Manager as MyTextureManager;

            //TODO: This is incredibly slow, solve better
            //textureManager.DbgUpdateStats();
        }

        /// <summary>
        /// Called when [unloading].
        /// </summary>
        protected virtual void OnUnloading()
        {
            // var textureManager = this.Manager as MyTextureManager;

            //TODO: This is incredibly slow, solve better
            //textureManager.DbgUpdateStats();
        }

        #endregion

        /// <summary>
        /// Performs an implicit conversion from <see cref="Sandbox.AppCode.Game.Managers.Graphics.Buffers.MyVertexBuffer"/> to <see cref="VRageMath.Graphics.VertexBuffer"/>.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SharpDX.Direct3D9.BaseTexture(MyTexture right)
        {
            if (right == null)
            {
                return null;
            }

            right.RequestAccess();

            return right.texture;
        }
    }
}
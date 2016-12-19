using System;
using System.Text;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRageMath;
using VRageRender;
using Format = SharpDX.DXGI.Format;

namespace VRage.Render11.Resources.Textures
{
    internal struct MyBorrowedTextureKey : IEquatable<MyBorrowedTextureKey>
    {
        public int Width;
        public int Height;
        public Format Format;
        public int SamplesCount;
        public int SamplesQuality;

        public override int GetHashCode()
        {
            return (Width << 1).GetHashCode() ^ (Height << 2).GetHashCode() ^ Format.GetHashCode() ^
                   (SamplesCount << 3).GetHashCode() ^ (SamplesQuality << 4).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType())
                return false;

            return Equals((MyBorrowedTextureKey)obj);
        }

        string m_toString;

        public override string ToString()
        {
            if (m_toString == null)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("{0}x{1}-{2}", Width, Height, Format);
                m_toString = builder.ToString();
            }
            return m_toString;
        }

        public bool Equals(MyBorrowedTextureKey other)
        {
            return Width == other.Width
                   && Height == other.Height
                   && Format == other.Format
                   && SamplesCount == other.SamplesCount
                   && SamplesQuality == other.SamplesQuality;
        }
    }

    internal class MyRwTexturesNameGenerator
    {
        int m_nCurrent = 0;
        string m_prefix;

        public MyRwTexturesNameGenerator(string prefix)
        {
            m_prefix = prefix;
        }

        public string GetUniqueName()
        {
            return m_prefix + m_nCurrent++;
        }
    }

    internal abstract class MyBorrowedTexture : IBorrowedSrvTexture
    {
        int m_numRefs;

        public MyBorrowedTextureKey Key { get; private set; }
        public string LastUsedDebugName { get; private set; }
        public int LastUsedInFrameNum { get; private set; }

        public bool IsBorrowed
        {
            get { return m_numRefs != 0; }
        }

        public string Name
        {
            get { return LastUsedDebugName; }
        }

        public void AddRef()
        {
            MyRenderProxy.Assert(IsBorrowed, "The texture has not been borrowed.");
            m_numRefs++;
        }

        public void Release()
        {
            MyRenderProxy.Assert(IsBorrowed, "The texture has not been borrowed.");

            if (m_numRefs != 0)
                m_numRefs--;
        }

        public void Create(MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(!IsBorrowed, "The texture cannot be borrowd during initialisation.");
            m_numRefs = 0;
            CreateTextureInternal(ref key);
            LastUsedDebugName = "None";
            LastUsedInFrameNum = 0;
            Key = key;
        }

        public void SetBorrowed(string name, int currentFrameNum)
        {
            MyRenderProxy.Assert(!IsBorrowed, "The texture has been borrowed.");
            LastUsedDebugName = name;
            LastUsedInFrameNum = currentFrameNum;
            m_numRefs = 1;
        }

        protected abstract void CreateTextureInternal(ref MyBorrowedTextureKey key);

        public abstract ShaderResourceView Srv { get; }

        public abstract Resource Resource { get; }

        public abstract Vector3I Size3 { get; }

        public abstract Vector2I Size { get; }

        public Format Format { get { return Key.Format; } }

        public int MipmapCount
        {
            get { return 1; }
        }
    }

    internal class MyBorrowedRtvTexture : MyBorrowedTexture, IBorrowedRtvTexture
    {
        static readonly MyRwTexturesNameGenerator NamesGenerator = new MyRwTexturesNameGenerator("BorrowedRtvTexture");

        public override Vector3I Size3
        {
            get { return RtvTexture.Size3; }
        }

        public override Vector2I Size
        {
            get { return RtvTexture.Size; }
        }

        public override Resource Resource
        {
            get { return RtvTexture.Resource; }
        }

        public override ShaderResourceView Srv
        {
            get { return RtvTexture.Srv; }
        }

        public RenderTargetView Rtv
        {
            get { return RtvTexture.Rtv; }
        }

        public IRtvTexture RtvTexture { get; private set; }

        protected override void CreateTextureInternal(ref MyBorrowedTextureKey key)
        {
            RtvTexture = MyManagers.RwTextures.CreateRtv(NamesGenerator.GetUniqueName(), key.Width, key.Height,
                key.Format, key.SamplesCount, key.SamplesQuality);
        }
    }

    internal class MyBorrowedUavTexture : MyBorrowedTexture, IBorrowedUavTexture
    {
        static readonly MyRwTexturesNameGenerator NamesGenerator = new MyRwTexturesNameGenerator("BorrowedUavTexture");

        public override Vector3I Size3
        {
            get { return UavTexture.Size3; }
        }

        public override Vector2I Size
        {
            get { return UavTexture.Size; }
        }

        public override Resource Resource
        {
            get { return UavTexture.Resource; }
        }

        public override ShaderResourceView Srv
        {
            get { return UavTexture.Srv; }
        }

        public UnorderedAccessView Uav
        {
            get { return UavTexture.Uav; }
        }

        public RenderTargetView Rtv
        {
            get { return UavTexture.Rtv; }
        }

        public IUavTexture UavTexture { get; private set; }

        protected override void CreateTextureInternal(ref MyBorrowedTextureKey key)
        {
            UavTexture = MyManagers.RwTextures.CreateUav(
                NamesGenerator.GetUniqueName(), key.Width, key.Height,
                key.Format, key.SamplesCount, key.SamplesQuality);
        }
    }

    internal class MyBorrowedCustomTexture : MyBorrowedTexture, IBorrowedCustomTexture
    {
        static readonly MyRwTexturesNameGenerator NamesGenerator = new MyRwTexturesNameGenerator("BorrowedCustomTexture");

        public override Vector3I Size3
        {
            get { return CustomTexture.Size3; }
        }

        public override Vector2I Size
        {
            get { return CustomTexture.Size; }
        }

        public override Resource Resource
        {
            get { return CustomTexture.Resource; }
        }

        public IRtvTexture Linear
        {
            get { return CustomTexture.Linear; }
        }

        public IRtvTexture SRgb
        {
            get { return CustomTexture.SRgb; }
        }

        public ICustomTexture CustomTexture { get; private set; }

        public override ShaderResourceView Srv
        {
            get { return CustomTexture.Linear.Srv; }
        }

        //public ShaderResourceView Srv { get { return CustomTexture.Linear.Srv; } }

        protected override void CreateTextureInternal(ref MyBorrowedTextureKey key)
        {
            CustomTexture = MyManagers.CustomTextures.CreateTexture(
                NamesGenerator.GetUniqueName(), key.Width, key.Height,
                key.SamplesCount, key.SamplesQuality);
        }
    }

    internal class MyBorrowedDepthStencilTexture : MyBorrowedTexture, IBorrowedDepthStencilTexture,
        IDepthStencilInternal
    {
        static readonly MyRwTexturesNameGenerator NamesGenerator = new MyRwTexturesNameGenerator("BorrowedCustomTexture");


        protected IDepthStencilInternal DepthStencilTextureInternal { get; set; }

        public IDepthStencil DepthStencilTexture
        {
            get { return DepthStencilTextureInternal; }
        }

        #region MyBorrowedTexture overrides

        public override Vector3I Size3
        {
            get { return DepthStencilTexture.Size3; }
        }

        public override Vector2I Size
        {
            get { return DepthStencilTexture.Size; }
        }

        public override Resource Resource
        {
            get { return DepthStencilTexture.Resource; }
        }

        public override ShaderResourceView Srv
        {
            get { return SrvDepth.Srv; }
        }

        #endregion

        #region IDepthStencilInternal overrides

        public ISrvBindable SrvDepth
        {
            get { return DepthStencilTextureInternal.SrvDepth; }
        }

        public ISrvBindable SrvStencil
        {
            get { return DepthStencilTextureInternal.SrvStencil; }
        }

        public DepthStencilView Dsv
        {
            get { return DepthStencilTextureInternal.Dsv; }
        }

        public DepthStencilView Dsv_ro
        {
            get { return DepthStencilTextureInternal.Dsv_ro; }
        }

        public DepthStencilView Dsv_roStencil
        {
            get { return DepthStencilTextureInternal.Dsv_roStencil; }
        }

        public DepthStencilView Dsv_roDepth
        {
            get { return DepthStencilTextureInternal.Dsv_roDepth; }
        }

        #endregion

        protected override void CreateTextureInternal(ref MyBorrowedTextureKey key)
        {
            DepthStencilTextureInternal = (IDepthStencilInternal)MyManagers.DepthStencils.CreateDepthStencil(
                NamesGenerator.GetUniqueName(), key.Width, key.Height,
                samplesCount: key.SamplesCount, samplesQuality: key.SamplesQuality);
        }
    }
}
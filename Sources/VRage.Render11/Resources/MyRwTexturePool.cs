using System;
using System.Collections.Generic;
using System.Text;
using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Resources.Internal;
using VRage.Render11.Tools;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal interface IBorrowedSrvTexture : ISrvTexture
    {
        void AddRef();
        void Release();
    }

    internal interface IBorrowedRtvTexture : IBorrowedSrvTexture, IRtvTexture
    {
    }

    internal interface IBorrowedUavTexture : IBorrowedSrvTexture, IUavTexture
    {
    }

    internal interface IBorrowedCustomTexture : IBorrowedSrvTexture, ICustomTexture
    {

    }

    namespace Internal
    {
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

            public abstract ShaderResourceView Srv
            {
                get;
            }

            public abstract Resource Resource
            {
                get;
            }

            public abstract Vector3I Size3
            {
                get;
            }

            public abstract Vector2I Size
            {
                get;
            }
        }

        internal class MyBorrowedRtvTexture : MyBorrowedTexture, IBorrowedRtvTexture
        {
            static MyRwTexturesNameGenerator m_namesGenerator = new MyRwTexturesNameGenerator("BorrowedRtvTexture");

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
                RtvTexture = MyManagers.RwTextures.CreateRtv(m_namesGenerator.GetUniqueName(), key.Width, key.Height,
                    key.Format, key.SamplesCount);
            }
        }

        internal class MyBorrowedUavTexture : MyBorrowedTexture, IBorrowedUavTexture
        {
            static MyRwTexturesNameGenerator m_namesGenerator = new MyRwTexturesNameGenerator("BorrowedUavTexture");

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
                UavTexture = MyManagers.RwTextures.CreateUav(m_namesGenerator.GetUniqueName(), key.Width, key.Height,
                    key.Format, key.SamplesCount);
            }
        }

        internal class MyBorrowedCustomTexture : MyBorrowedTexture, IBorrowedCustomTexture
        {
            static MyRwTexturesNameGenerator m_namesGenerator = new MyRwTexturesNameGenerator("BorrowedCustomTexture");

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

            public IRtvTexture Linear { get { return CustomTexture.Linear; } }

            public IRtvTexture SRgb { get { return CustomTexture.SRgb; } }

            public ICustomTexture CustomTexture { get; private set; }

            public override ShaderResourceView Srv { get { return CustomTexture.Linear.Srv; } }

            //public ShaderResourceView Srv { get { return CustomTexture.Linear.Srv; } }

            protected override void CreateTextureInternal(ref MyBorrowedTextureKey key)
            {
                CustomTexture = MyManagers.CustomTextures.CreateTexture(m_namesGenerator.GetUniqueName(), key.Width, key.Height,
                    key.SamplesCount);
            }
        }


        internal struct MyBorrowedTextureKey : IEquatable<MyBorrowedTextureKey>
        {
            public int Width;
            public int Height;
            public Format Format;
            public int SamplesCount;

            public override int GetHashCode()
            {
                return (Width << 1).GetHashCode() ^ (Height << 2).GetHashCode() ^ Format.GetHashCode() ^ (SamplesCount << 3).GetHashCode();
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
                       && SamplesCount == other.SamplesCount;
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
    }

    // Manager for textures used for intermediate results. Every borrowed texture needs to be returned at the end of the current frame
    // Parental interface IResourceManager is used only formarly (to follow approach in MyManagers)
    internal class MyRwTexturePool : IManager, IManagerCallback
    {
        int m_currentFrameNum;

        MyObjectsPool<MyBorrowedRtvTexture> m_objectPoolRtv = new MyObjectsPool<MyBorrowedRtvTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedRtvTexture>> m_dictionaryRtvTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedRtvTexture>>();

        MyObjectsPool<MyBorrowedUavTexture> m_objectPoolUav = new MyObjectsPool<MyBorrowedUavTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedUavTexture>> m_dictionaryUavTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedUavTexture>>();

        MyObjectsPool<MyBorrowedCustomTexture> m_objectPoolCustom = new MyObjectsPool<MyBorrowedCustomTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedCustomTexture>> m_dictionaryCustomTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedCustomTexture>>();

        List<MyBorrowedRtvTexture> m_tmpBorrowedRtvTextures = new List<MyBorrowedRtvTexture>();
        List<MyBorrowedUavTexture> m_tmpBorrowedUavTextures = new List<MyBorrowedUavTexture>();
        List<MyBorrowedCustomTexture> m_tmpBorrowedCustomTextures = new List<MyBorrowedCustomTexture>();

        protected void AddNewRtvsList(MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(!m_dictionaryRtvTextures.ContainsKey(key), "The requested item has been created, do not create them once again!");

            m_dictionaryRtvTextures.Add(key, new List<MyBorrowedRtvTexture>());
        }

        protected MyBorrowedRtvTexture CreateRtv(string debugName, MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(m_dictionaryRtvTextures.ContainsKey(key), "The key needs to be used before this call!");
            MyRenderProxy.Assert(m_dictionaryRtvTextures[key] != null, "The list needs to be allocated before this call!");

            MyBorrowedRtvTexture borrowedRtv;
            m_objectPoolRtv.AllocateOrCreate(out borrowedRtv);
            borrowedRtv.Create(key);

            m_dictionaryRtvTextures[key].Add(borrowedRtv);
            return borrowedRtv;
        }

        protected MyBorrowedUavTexture CreateUav(string debugName, MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(m_dictionaryUavTextures.ContainsKey(key), "The key needs to be used before this call!");
            MyRenderProxy.Assert(m_dictionaryUavTextures[key] != null, "The list needs to be allocated before this call!");

            MyBorrowedUavTexture borrowedUav;
            m_objectPoolUav.AllocateOrCreate(out borrowedUav);
            borrowedUav.Create(key);

            m_dictionaryUavTextures[key].Add(borrowedUav);
            return borrowedUav;
        }

        protected MyBorrowedCustomTexture CreateCustom(string debugName, MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(m_dictionaryCustomTextures.ContainsKey(key), "The key needs to be used before this call!");
            MyRenderProxy.Assert(m_dictionaryCustomTextures[key] != null, "The list needs to be allocated before this call!");

            MyBorrowedCustomTexture borrowedCustom;
            m_objectPoolCustom.AllocateOrCreate(out borrowedCustom);
            borrowedCustom.Create(key);

            m_dictionaryCustomTextures[key].Add(borrowedCustom);
            return borrowedCustom;
        }


        public IBorrowedRtvTexture BorrowRtv(string debugName, int width, int height, Format format, int samplesCount = 1)
        {
            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = format,
                SamplesCount = samplesCount,
            };
            if (!m_dictionaryRtvTextures.ContainsKey(key))
                AddNewRtvsList(key);

            List<MyBorrowedRtvTexture> list = m_dictionaryRtvTextures[key];
            foreach (var texIt in list)
            {
                if (!texIt.IsBorrowed)
                {
                    texIt.SetBorrowed(debugName, m_currentFrameNum);
                    return texIt;
                }
            }

            MyBorrowedRtvTexture createdTex = CreateRtv(debugName, key);
            createdTex.SetBorrowed(debugName, m_currentFrameNum);
            return createdTex;
        }

        public IBorrowedRtvTexture BorrowRtv(string debugName, Format format, int samplesCount = 1)
        {
            return BorrowRtv(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, format, samplesCount);
        }

        public IBorrowedUavTexture BorrowUav(string debugName, int width, int height, Format format, int samplesCount = 1)
        {
            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = format,
                SamplesCount = samplesCount,
            };
            if (!m_dictionaryUavTextures.ContainsKey(key))
                m_dictionaryUavTextures.Add(key, new List<MyBorrowedUavTexture>());

            foreach (var texIt in m_dictionaryUavTextures[key])
            {
                if (!texIt.IsBorrowed)
                {
                    texIt.SetBorrowed(debugName, m_currentFrameNum);
                    return texIt;
                }
            }

            MyBorrowedUavTexture createdTex = CreateUav(debugName, key);
            createdTex.SetBorrowed(debugName, m_currentFrameNum);
            return createdTex;
        }

        public IBorrowedUavTexture BorrowUav(string debugName, Format format, int samplesCount = 1)
        {
            return BorrowUav(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, format, samplesCount);
        }

        public IBorrowedCustomTexture BorrowCustom(string debugName, int samplesCount = 1)
        {
            return BorrowCustom(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, samplesCount);
        }

        public IBorrowedCustomTexture BorrowCustom(string debugName, int width, int height, int samplesCount = 1)
        {
            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = Format.Unknown,
                SamplesCount = samplesCount,
            };
            if (!m_dictionaryCustomTextures.ContainsKey(key))
                m_dictionaryCustomTextures.Add(key, new List<MyBorrowedCustomTexture>());

            foreach (var texIt in m_dictionaryCustomTextures[key])
            {
                if (!texIt.IsBorrowed)
                {
                    texIt.SetBorrowed(debugName, m_currentFrameNum);
                    return texIt;
                }
            }

            MyBorrowedCustomTexture createdTex = CreateCustom(debugName, key);
            createdTex.SetBorrowed(debugName, m_currentFrameNum);
            return createdTex;
        }

        void DisposeTexture(MyBorrowedRtvTexture rtv)
        {
            IRtvTexture rtvTexture = rtv.RtvTexture;
            MyManagers.RwTextures.DisposeTex(ref rtvTexture);

            MyBorrowedTextureKey key = rtv.Key;
            m_dictionaryRtvTextures[key].Remove(rtv);
            m_objectPoolRtv.Deallocate(rtv);
        }

        void DisposeTexture(MyBorrowedUavTexture uav)
        {
            IUavTexture uavTexture = uav.UavTexture;
            MyManagers.RwTextures.DisposeTex(ref uavTexture);

            MyBorrowedTextureKey key = uav.Key;
            m_dictionaryUavTextures[key].Remove(uav);
            m_objectPoolUav.Deallocate(uav);
        }

        void DisposeTexture(MyBorrowedCustomTexture custom)
        {
            ICustomTexture customTexture = custom.CustomTexture;
            MyManagers.CustomTextures.DisposeTex(ref customTexture);

            MyBorrowedTextureKey key = custom.Key;
            m_dictionaryCustomTextures[key].Remove(custom);
            m_objectPoolCustom.Deallocate(custom);
        }

        public bool IsAnyTextureBorrowed()
        {
            foreach (var itListUav in m_dictionaryUavTextures)
            {
                foreach (var uav in itListUav.Value)
                    if (uav.IsBorrowed)
                        return true;
            }

            foreach (var itListRtv in m_dictionaryRtvTextures)
            {
                foreach (var rtv in itListRtv.Value)
                    if (rtv.IsBorrowed)
                        return true;
            }

            foreach (var itListCustum in m_dictionaryCustomTextures)
            {
                foreach (MyBorrowedCustomTexture custom in itListCustum.Value)
                    if (custom.IsBorrowed)
                        return true;
            }

            return false;
        }

        // IMPORTANT: this method allocates a list for every call, if the method will be used regularly, needs to be modified!
        protected List<MyBorrowedTexture> GetBorrowedTextures()
        {
            List<MyBorrowedTexture> borrowedTextures = new List<MyBorrowedTexture>();
            foreach (var itListUav in m_dictionaryUavTextures)
            {
                foreach (var uav in itListUav.Value)
                    if (uav.IsBorrowed)
                        borrowedTextures.Add(uav);
            }

            foreach (var itListRtv in m_dictionaryRtvTextures)
            {
                foreach (var rtv in itListRtv.Value)
                    if (rtv.IsBorrowed)
                        borrowedTextures.Add(rtv);
            }

            foreach (var itListCustom in m_dictionaryCustomTextures)
            {
                foreach (var custom in itListCustom.Value)
                    if (custom.IsBorrowed)
                        borrowedTextures.Add(custom);
            }
            return borrowedTextures;
        }

        public void OnUnloadData()
        {

        }

        public void UpdateStats()
        {
            string group = "Rtv pooling";
            foreach (var itListRtv in m_dictionaryRtvTextures)
            {
                MyStatsDisplay.Write(group, itListRtv.Key.ToString(), itListRtv.Value.Count);
            }

            group = "Rtv pooling";
            foreach (var itListUav in m_dictionaryUavTextures)
            {
                MyStatsDisplay.Write(group, itListUav.Key.ToString(), itListUav.Value.Count);
            }

            group = "Custom pooling";
            foreach (var itListCustom in m_dictionaryCustomTextures)
            {
                MyStatsDisplay.Write(group, itListCustom.Key.ToString(), itListCustom.Value.Count);
            }
        }

        public void OnFrameEnd()
        {
            if (IsAnyTextureBorrowed())// this is bugcheck, if there is an error, generate error message with more details
            {
                StringBuilder builder = new StringBuilder();
                List<MyBorrowedTexture> list = GetBorrowedTextures();
                foreach (var tex in list)
                {
                    builder.AppendFormat("{0}: {1}x{2} {3};  ", tex.Name, tex.Key.Width, tex.Key.Height, tex.Key.Format);
                    while(tex.IsBorrowed)
                        tex.Release();
                }
                MyRenderProxy.Assert(!IsAnyTextureBorrowed(), "Following textures have not been returned: " + builder.ToString());
            }

            int numFramesToPreserveTexture = MyRender11.Settings.RwTexturePool_FramesToPreserveTextures;

            m_tmpBorrowedUavTextures.Clear();
            foreach (var itListUav in m_dictionaryUavTextures)
            {
                foreach (var uav in itListUav.Value)
                    if (!uav.IsBorrowed)
                    {
                        int lastUsed = uav.LastUsedInFrameNum;
                        if (lastUsed + numFramesToPreserveTexture < m_currentFrameNum)
                            m_tmpBorrowedUavTextures.Add(uav);
                    }
            }
            foreach (var uav in m_tmpBorrowedUavTextures)
                DisposeTexture(uav);
            m_tmpBorrowedUavTextures.Clear();

            m_tmpBorrowedRtvTextures.Clear();
            foreach (var itListRtv in m_dictionaryRtvTextures)
            {
                foreach (var rtv in itListRtv.Value)
                    if (!rtv.IsBorrowed)
                    {
                        int lastUsed = rtv.LastUsedInFrameNum;
                        if (lastUsed + numFramesToPreserveTexture < m_currentFrameNum)
                            m_tmpBorrowedRtvTextures.Add(rtv);
                    }
            }
            foreach (var rtv in m_tmpBorrowedRtvTextures)
                DisposeTexture(rtv);
            m_tmpBorrowedRtvTextures.Clear();

            m_tmpBorrowedCustomTextures.Clear();
            foreach (var itListCustom in m_dictionaryCustomTextures)
            {
                foreach (var custom in itListCustom.Value)
                    if (!custom.IsBorrowed)
                    {
                        int lastUsed = custom.LastUsedInFrameNum;
                        if (lastUsed + numFramesToPreserveTexture < m_currentFrameNum)
                            m_tmpBorrowedCustomTextures.Add(custom);
                    }
            }
            foreach (var custom in m_tmpBorrowedCustomTextures)
                DisposeTexture(custom);
            m_tmpBorrowedCustomTextures.Clear();

            m_currentFrameNum++;
        }
    }
}

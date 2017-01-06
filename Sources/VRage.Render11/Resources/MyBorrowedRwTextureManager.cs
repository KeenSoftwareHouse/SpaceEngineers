using System.Collections.Generic;
using System.Linq;
using System.Text;
using Format = SharpDX.DXGI.Format;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Textures;
using VRage.Render11.Tools;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal interface IBorrowedSrvTexture : ISrvTexture
    {
        void AddRef();
        void Release();
    }

    internal interface IBorrowedRtvTexture : IBorrowedSrvTexture, IRtvTexture
    { }

    internal interface IBorrowedUavTexture : IBorrowedSrvTexture, IUavTexture
    { }

    internal interface IBorrowedCustomTexture : IBorrowedSrvTexture, ICustomTexture
    { }

    internal interface IBorrowedDepthStencilTexture : IBorrowedSrvTexture, IDepthStencil
    { }


    // Manager for textures used for intermediate results. Every borrowed texture needs to be returned at the end of the current frame
    // Parental interface IResourceManager is used only formarly (to follow approach in MyManagers)
    internal class MyBorrowedRwTextureManager : IManager, IManagerFrameEnd
    {
        #region Fields

        int m_currentFrameNum;

        MyObjectsPool<MyBorrowedRtvTexture> m_objectPoolRtv = new MyObjectsPool<MyBorrowedRtvTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedRtvTexture>> m_dictionaryRtvTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedRtvTexture>>();

        MyObjectsPool<MyBorrowedUavTexture> m_objectPoolUav = new MyObjectsPool<MyBorrowedUavTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedUavTexture>> m_dictionaryUavTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedUavTexture>>();

        MyObjectsPool<MyBorrowedCustomTexture> m_objectPoolCustom = new MyObjectsPool<MyBorrowedCustomTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedCustomTexture>> m_dictionaryCustomTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedCustomTexture>>();

        MyObjectsPool<MyBorrowedDepthStencilTexture> m_objectPoolDepthStencil = new MyObjectsPool<MyBorrowedDepthStencilTexture>(16);
        Dictionary<MyBorrowedTextureKey, List<MyBorrowedDepthStencilTexture>> m_dictionaryDepthStencilTextures = new Dictionary<MyBorrowedTextureKey, List<MyBorrowedDepthStencilTexture>>();

        List<MyBorrowedRtvTexture> m_tmpBorrowedRtvTextures = new List<MyBorrowedRtvTexture>();
        List<MyBorrowedUavTexture> m_tmpBorrowedUavTextures = new List<MyBorrowedUavTexture>();
        List<MyBorrowedCustomTexture> m_tmpBorrowedCustomTextures = new List<MyBorrowedCustomTexture>();
        List<MyBorrowedDepthStencilTexture> m_tmpBorrowedDepthStencilTextures = new List<MyBorrowedDepthStencilTexture>();

        #endregion

        #region Creation

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

        protected MyBorrowedDepthStencilTexture CreateDepthStencil(string debugName, MyBorrowedTextureKey key)
        {
            MyRenderProxy.Assert(m_dictionaryDepthStencilTextures.ContainsKey(key), "The key needs to be used before this call!");
            MyRenderProxy.Assert(m_dictionaryDepthStencilTextures[key] != null, "The list needs to be allocated before this call!");

            MyBorrowedDepthStencilTexture borrowedDepthStencil;
            m_objectPoolDepthStencil.AllocateOrCreate(out borrowedDepthStencil);
            borrowedDepthStencil.Create(key);

            m_dictionaryDepthStencilTextures[key].Add(borrowedDepthStencil);
            return borrowedDepthStencil;
        }

        #endregion

        #region Borrowing

        public IBorrowedRtvTexture BorrowRtv(string debugName, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            return BorrowRtv(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, format, samplesCount, samplesQuality);
        }

        public IBorrowedRtvTexture BorrowRtv(string debugName, int width, int height, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = format,
                SamplesCount = samplesCount,
                SamplesQuality = samplesQuality,
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

        public IBorrowedUavTexture BorrowUav(string debugName, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            return BorrowUav(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, format, samplesCount, samplesQuality);
        }

        public IBorrowedUavTexture BorrowUav(string debugName, int width, int height, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = format,
                SamplesCount = samplesCount,
                SamplesQuality = samplesQuality,
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

        public IBorrowedCustomTexture BorrowCustom(string debugName, int samplesCount = 1, int samplesQuality = 0)
        {
            return BorrowCustom(debugName, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, samplesCount, samplesQuality);
        }

        public IBorrowedCustomTexture BorrowCustom(string debugName, int width, int height, int samplesCount = 1, int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = Format.Unknown,
                SamplesCount = samplesCount,
                SamplesQuality = samplesQuality,
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

        public IBorrowedDepthStencilTexture BorrowDepthStencil(string debugName, int width, int height, int samplesCount = 1, int samplesQuality = 0)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);

            MyBorrowedTextureKey key = new MyBorrowedTextureKey
            {
                Width = width,
                Height = height,
                Format = Format.Unknown,
                SamplesCount = samplesCount,
                SamplesQuality = samplesQuality,
            };

            if (!m_dictionaryDepthStencilTextures.ContainsKey(key))
                m_dictionaryDepthStencilTextures.Add(key, new List<MyBorrowedDepthStencilTexture>());

            foreach (var texIt in m_dictionaryDepthStencilTextures[key])
            {
                if (!texIt.IsBorrowed)
                {
                    texIt.SetBorrowed(debugName, m_currentFrameNum);
                    return texIt;
                }
            }

            MyBorrowedDepthStencilTexture createdTex = CreateDepthStencil(debugName, key);
            createdTex.SetBorrowed(debugName, m_currentFrameNum);
            return createdTex;
        }

        #endregion

        #region Disposal

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

        void DisposeTexture(MyBorrowedDepthStencilTexture depthStencil)
        {
            IDepthStencil customTexture = depthStencil.DepthStencilTexture;
            MyManagers.DepthStencils.DisposeTex(ref customTexture);

            MyBorrowedTextureKey key = depthStencil.Key;
            m_dictionaryDepthStencilTextures[key].Remove(depthStencil);
            m_objectPoolDepthStencil.Deallocate(depthStencil);
        }

        #endregion


        public bool IsAnyTextureBorrowed()
        {
            // GetBorrowedTextures causes memory allocations, therefore this method is "redundant"
            // return GetBorrowedTextures().Any();

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

            foreach (var itListCustom in m_dictionaryCustomTextures)
            {
                foreach (var custom in itListCustom.Value)
                    if (custom.IsBorrowed)
                        return true;
            }

            foreach (var itListDepthStencil in m_dictionaryDepthStencilTextures)
            {
                foreach (var depthStencil in itListDepthStencil.Value)
                    if (depthStencil.IsBorrowed)
                        return true;
            }

            return false;
        }

        // IMPORTANT: this method allocates a list for every call, if the method will be used regularly, needs to be modified!
        protected IEnumerable<MyBorrowedTexture> GetBorrowedTextures()
        {
            foreach (var itListUav in m_dictionaryUavTextures)
            {
                foreach (var uav in itListUav.Value)
                    if (uav.IsBorrowed)
                        yield return uav;
            }

            foreach (var itListRtv in m_dictionaryRtvTextures)
            {
                foreach (var rtv in itListRtv.Value)
                    if (rtv.IsBorrowed)
                        yield return rtv;
            }

            foreach (var itListCustom in m_dictionaryCustomTextures)
            {
                foreach (var custom in itListCustom.Value)
                    if (custom.IsBorrowed)
                        yield return custom;
            }

            foreach (var itListDepthStencil in m_dictionaryDepthStencilTextures)
            {
                foreach (var depthStencil in itListDepthStencil.Value)
                    if (depthStencil.IsBorrowed)
                        yield return depthStencil;
            }
        }


        #region IManagerCallback overrides

        void IManagerFrameEnd.OnFrameEnd()
        {
            if (IsAnyTextureBorrowed())// this is bugcheck, if there is an error, generate error message with more details
            {
                StringBuilder builder = new StringBuilder();
                IEnumerable<MyBorrowedTexture> list = GetBorrowedTextures();
                foreach (var tex in list)
                {
                    builder.AppendFormat("{0}: {1}x{2} {3};  ", tex.Name, tex.Key.Width, tex.Key.Height, tex.Key.Format);
                    while (tex.IsBorrowed)
                        tex.Release();
                }
                MyRenderProxy.Assert(!IsAnyTextureBorrowed(), "Following textures have not been returned: " + builder.ToString());
            }

            ClearHelper(m_dictionaryUavTextures, m_tmpBorrowedUavTextures);
            foreach (var uav in m_tmpBorrowedUavTextures)
                DisposeTexture(uav);
            m_tmpBorrowedUavTextures.Clear();

            ClearHelper(m_dictionaryRtvTextures, m_tmpBorrowedRtvTextures);
            foreach (var uav in m_tmpBorrowedRtvTextures)
                DisposeTexture(uav);
            m_tmpBorrowedRtvTextures.Clear();

            ClearHelper(m_dictionaryCustomTextures, m_tmpBorrowedCustomTextures);
            foreach (var uav in m_tmpBorrowedCustomTextures)
                DisposeTexture(uav);
            m_tmpBorrowedCustomTextures.Clear();

            ClearHelper(m_dictionaryDepthStencilTextures, m_tmpBorrowedDepthStencilTextures);
            foreach (var uav in m_tmpBorrowedDepthStencilTextures)
                DisposeTexture(uav);
            m_tmpBorrowedDepthStencilTextures.Clear();

            m_currentFrameNum++;
        }

        void ClearHelper<T>(Dictionary<MyBorrowedTextureKey, List<T>> dict, List<T> tmpList)
            where T : MyBorrowedTexture
        {
            int numFramesToPreserveTexture = MyRender11.Settings.RwTexturePool_FramesToPreserveTextures;

            foreach (var textureList in dict)
            {
                foreach (var texture in textureList.Value)
                    if (!texture.IsBorrowed)
                    {
                        int lastUsed = texture.LastUsedInFrameNum;
                        if (lastUsed + numFramesToPreserveTexture < m_currentFrameNum)
                            tmpList.Add(texture);
                    }
            }
        }

        #endregion


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

            group = "DepthStencil pooling";
            foreach (var itListDepthStencil in m_dictionaryDepthStencilTextures)
            {
                MyStatsDisplay.Write(group, itListDepthStencil.Key.ToString(), itListDepthStencil.Value.Count);
            }
        }
    }
}

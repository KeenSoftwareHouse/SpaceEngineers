using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRage.Render11.Resources.Textures;

namespace VRage.Render11.Resources
{
    // TODO: Not used
    internal class MyRwTextureCatalog : IManager, IManagerDevice, IManagerFrameEnd
    {
        private Dictionary<string, MyTextureKeyIdentity> m_textureKeysCatalog = new Dictionary<string, MyTextureKeyIdentity>();
        private Dictionary<string, IBorrowedSrvTexture> m_texturesMap = new Dictionary<string, IBorrowedSrvTexture>();

        public void RegisterTexture(string textureName, int width, int height, Format format, MyCatalogTextureType type)
        {
            m_textureKeysCatalog.Add(textureName, new MyTextureKeyIdentity()
            {
                Type = type,
                Key = new MyBorrowedTextureKey() { Width = width, Height = height, Format = format }
            });
        }

        public ISrvTexture GetTexture(string textureName)
        {
            IBorrowedSrvTexture texture;
            if (!m_texturesMap.TryGetValue(textureName, out texture))
            {
                MyTextureKeyIdentity key = m_textureKeysCatalog[textureName];
                switch (key.Type)
                {
                    case MyCatalogTextureType.Rtv:
                        texture = MyManagers.RwTexturesPool.BorrowRtv(textureName, key.Key.Width, key.Key.Height, key.Key.Format);
                        break;
                    case MyCatalogTextureType.Uav:
                        texture = MyManagers.RwTexturesPool.BorrowUav(textureName, key.Key.Width, key.Key.Height, key.Key.Format);
                        break;
                    default:
                        throw new Exception();
                }

                m_texturesMap.Add(textureName, texture);
            }

            return texture;
        }

        public IRtvTexture GetRtvTexture(string textureName)
        {
            return (IRtvTexture)GetTexture(textureName);
        }

        public IUavTexture GetUavTexture(string textureName)
        {
            return (IUavTexture)GetTexture(textureName);
        }

        void IManagerFrameEnd.OnFrameEnd()
        {
            foreach (var texture in m_texturesMap.Values)
                texture.Release();

            m_texturesMap.Clear();
        }

        public void OnDeviceInit() { }

        public void OnDeviceReset() { }

        public void OnDeviceEnd()
        {
            m_textureKeysCatalog.Clear();
        }

        class MyTextureKeyIdentity
        {
            public MyCatalogTextureType Type { get; set; }
            public MyBorrowedTextureKey Key { get; set; }
        }
    }

    internal enum MyCatalogTextureType
    {
        Rtv,
        Uav,
    }
}

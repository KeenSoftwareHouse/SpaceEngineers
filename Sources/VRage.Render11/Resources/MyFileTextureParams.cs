using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Toolkit.Graphics;
using VRageMath;
using VRageRender;
using VRage.FileSystem;

namespace VRage.Render11.Resources
{
    struct MyFileTextureParams
    {
        public Vector2I Resolution;
        public Format Format;
        public int Mipmaps;
        public int ArraySize;

        public static bool operator ==(MyFileTextureParams params1, MyFileTextureParams params2)
        {
            return params1.Equals(params2);
        }

        public static bool operator !=(MyFileTextureParams params1, MyFileTextureParams params2)
        {
            return !params1.Equals(params2);
        }

    }

    static class MyFileTextureParamsManager
    {
        static Dictionary<string, MyFileTextureParams> m_dictCached = new Dictionary<string, MyFileTextureParams>();

        public static bool LoadFromFile(string filepath, out MyFileTextureParams outParams)
        {
            outParams = new MyFileTextureParams();
            if (string.IsNullOrEmpty(filepath))
                return false;

            filepath = MyResourceUtils.GetTextureFullPath(filepath);
            if (m_dictCached.TryGetValue(filepath, out outParams))
                return true;
            if (!MyFileSystem.FileExists(filepath))
            {
                MyRender11.Log.WriteLine("Missing texture: " + filepath);
                return false;
            }

            try
            {
                using (var s = MyFileSystem.OpenRead(filepath))
                {
                    Image image = Image.Load(s);
                    outParams.Resolution.X = image.Description.Width;
                    outParams.Resolution.Y = image.Description.Height;
                    outParams.Format = image.Description.Format;
                    outParams.Mipmaps = image.Description.MipLevels;
                    outParams.ArraySize = image.Description.ArraySize;
                }

                m_dictCached.Add(filepath, outParams);
                return true;
            }
            catch (Exception)
            {
                MyRenderProxy.Assert(false, "The file in textures exists, but cannot be loaded. Please, investigate!");
                return false;
            }
        }

        // if the file does not exist the zero vector is returned
        public static Vector2I GetResolutionFromFile(string filepath)
        {
            MyFileTextureParams parameters;
            if (!LoadFromFile(filepath, out parameters))
                return Vector2I.Zero;
            return parameters.Resolution;
        }

        public static bool IsArrayTextureInFile(string filepath)
        {
            MyFileTextureParams parameters;
            if (!LoadFromFile(filepath, out parameters))
                return false;
            return parameters.ArraySize > 1;
        }

        public static MyFileTextureParams LoadFromSrv(ISrvBindable srv)
        {
            MyRenderProxy.Assert(srv.Srv != null);
            MyRenderProxy.Assert(srv.Srv.Description.Dimension == ShaderResourceViewDimension.Texture2D);

            MyFileTextureParams outParams;
            outParams.Resolution.X = srv.Size.X;
            outParams.Resolution.Y = srv.Size.Y; ;
            outParams.Format = srv.Srv.Description.Format;
            outParams.Mipmaps = srv.Srv.Description.Texture2D.MipLevels;
            outParams.ArraySize = 1;
            return outParams;
        }
    }
}

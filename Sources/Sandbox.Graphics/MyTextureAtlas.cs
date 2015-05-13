using System;
using System.Collections.Generic;

namespace Sandbox.Graphics
{
    using Vector4 = VRageMath.Vector4;

    /// <summary>
    /// There's so little to this class you barely need it but it
    /// saves some typing if nothing else.
    /// </summary>
    public class MyTextureAtlas : Dictionary<string, MyTextureAtlasItem>
    {
        public MyTextureAtlas(int numItems)
            : base(numItems)
        { }
    }

    public struct MyTextureAtlasItem
    {
        /// <summary>
        /// The Texture2D that this item is part of
        /// </summary>
        public string AtlasTexture;

        /// <summary>
        /// The UVOffsets describe where this item
        /// sits in the AtlasTexture. The four components
        /// are U offset, V offset, Width and Height
        /// </summary>
        public Vector4 UVOffsets;

        public  MyTextureAtlasItem(string atlasTex, Vector4 uvOffsets)
        {
            AtlasTexture = atlasTex;
            UVOffsets = uvOffsets;
        }

        /// <summary>
        /// This returns a Rectangle suitable for use
        /// with SpriteBatch.
        /// </summary>
        //public Rectangle SourceRectangle
        //{
        //    get
        //    {
        //        Vector4 v = new Vector4(
        //            AtlasTexture.Width, 
        //            AtlasTexture.Height, 
        //            AtlasTexture.Width, 
        //            AtlasTexture.Height) * UVOffsets;
                
        //        return new Rectangle(
        //            (int)v.X, (int)v.Y, 
        //            (int)v.Z, (int)v.W);
        //    }
        //}
    }    
}

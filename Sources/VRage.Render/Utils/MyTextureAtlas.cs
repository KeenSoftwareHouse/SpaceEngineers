using System;
using System.Collections.Generic;
using VRageRender.Textures;

namespace VRageRender
{
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    

    /// <summary>
    /// There's so little to this class you barely need it but it
    /// saves some typing if nothing else.
    /// </summary>
    internal class MyTextureAtlas : Dictionary<string, MyTextureAtlasItem>
    {
        public MyTextureAtlas(int numItems)
            : base(numItems)
        { }
    }

    internal struct MyTextureAtlasItem
    {
        /// <summary>
        /// The Texture2D that this item is part of
        /// </summary>
        public MyTexture2D AtlasTexture;

        /// <summary>
        /// The UVOffsets describe where this item
        /// sits in the AtlasTexture. The four components
        /// are U offset, V offset, Width and Height
        /// </summary>
        public Vector4 UVOffsets;

        internal MyTextureAtlasItem(MyTexture2D atlasTex, Vector4 uvOffsets)
        {
            AtlasTexture = atlasTex;
            UVOffsets = uvOffsets;
        }

        /// <summary>
        /// This returns a Rectangle suitable for use
        /// with SpriteBatch.
        /// </summary>
        public Rectangle SourceRectangle
        {
            get
            {
                Vector4 v = new Vector4(
                    AtlasTexture.Width, 
                    AtlasTexture.Height, 
                    AtlasTexture.Width, 
                    AtlasTexture.Height) * UVOffsets;
                
                return new Rectangle(
                    (int)v.X, (int)v.Y, 
                    (int)v.Z, (int)v.W);
            }
        }
    }    
}

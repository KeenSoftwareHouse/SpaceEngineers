using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageRender.Textures
{
    public static class MyTextureExtensions
    {
        [Conditional("DEBUG")]
        internal static void CheckTextureClass(this MyTexture texture, MyTextureClassEnum assumedClass)
        {
            Debug.Assert(texture == null || texture.TextureClassDebug == assumedClass, "Texture class is invalid");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    static class MyClipmapFactory
    {
        internal static Dictionary<uint, MyClipmapHandler> ClipmapByID = new Dictionary<uint, MyClipmapHandler>();

        internal static void RemoveAll()
        {
            foreach(var clipmap in ClipmapByID)
            {
                clipmap.Value.Base.UnloadContent();
            }

            ClipmapByID.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Common
{
    using System.Diagnostics;
    using VRage.Common.Import;

    //@ Simple stupid material class which enwrapp 2 textures and generate uniqueID form textures
    public class MyMeshMaterial
    {
        public MyMeshDrawTechnique DrawTechnique;
        public string Name;
        public Color DiffuseColor;
        public float SpecularIntensity;
        public float SpecularPower;
        public string GlassCW;
        public string GlassCCW;
        public bool GlassSmooth;
        public string DiffuseTexture;
    }
}

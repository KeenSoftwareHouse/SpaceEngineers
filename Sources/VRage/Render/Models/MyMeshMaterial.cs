using System.Collections.Generic;
using VRage.Import;
using VRageMath;

namespace VRage.Render.Models
{
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

        public Dictionary<string, string> Textures;

        public override int GetHashCode()
        {
            int result = 1;
            int modCode = 0;

            if (SpecularIntensity != 0)
            {
                result = (result * 397) ^ SpecularIntensity.GetHashCode();
                modCode += (1 << 1);
            }

            if (SpecularPower != 0)
            {
                result = (result * 397) ^ SpecularPower.GetHashCode();
                modCode += (1 << 2);
            }

            if (DiffuseColor.GetHashCode() != 0)
            {
                result = (result * 397) ^ DiffuseColor.GetHashCode();
                modCode += (1 << 3);
            }

            int i = 3;
            foreach (var pair in Textures)
            {
                result = (result * 397) ^ pair.Key.GetHashCode();
                modCode += (1 << ++i);

                result = (result * 397) ^ pair.Value.GetHashCode();
                modCode += (1 << ++i);
            }

            return (result * 397) ^ modCode;
        }
    }
}

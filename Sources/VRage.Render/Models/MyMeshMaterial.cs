using System.Collections.Generic;
using VRageMath;
using VRageRender.Import;

namespace VRageRender.Models
{
    //@ Simple stupid material class which enwrapp 2 textures and generate uniqueID form textures
    public class MyMeshMaterial
    {
        public MyMeshDrawTechnique DrawTechnique;
        public string Name;
        public string GlassCW;
        public string GlassCCW;
        public bool GlassSmooth;

        public Dictionary<string, string> Textures;

        public override int GetHashCode()
        {
            int result = 1;
            int modCode = 0;

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

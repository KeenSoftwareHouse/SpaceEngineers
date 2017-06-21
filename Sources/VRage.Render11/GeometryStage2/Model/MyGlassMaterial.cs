
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;

namespace VRage.Render11.GeometryStage2.Model
{
    class MyGlassMaterial
    {
        public string MaterialName { get; private set; }
        public ISrvBindable[] Srvs { get; private set; }
        public Vector4 Color { get; private set; }
        public float Refraction { get; private set; }

        public MyGlassMaterial(string materialName, string textureFilepath, Vector4 color, float refraction)
        {
            MaterialName = materialName;

            Srvs = new ISrvBindable[1];
            Srvs[0] = MyManagers.FileTextures.GetTexture(textureFilepath, MyFileTextureEnum.COLOR_METAL);

            Color = color;

            Refraction = refraction;
        }
    }
}

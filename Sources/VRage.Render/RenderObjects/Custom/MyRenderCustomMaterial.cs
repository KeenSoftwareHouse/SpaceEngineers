using SysUtils.Utils;
using VRage.Common;
using VRageRender.Textures;
using VRageRender.Utils;


namespace VRageRender
{
    class MyCustomMaterialTextures
    {
        public MyTexture2D TextureDiffuse;
        public MyTexture2D TextureNormal;
    }

    public class MyRenderCustomMaterial
    {
        int m_materialIndex;

        // Physics properties
        public float Mass { get; set; }
        public float Strength { get; set; }

        // Render properties
        public float SpecularIntensity { get; set; }
        public float SpecularPower { get; set; }

        MyCustomMaterialTextures m_textures;
        string m_diffuse;
        string m_normal;

        public MyRenderCustomMaterial(MyRenderCustomMaterialData data)
        {
            m_materialIndex = data.Index;

            Mass     = data.Mass;
            Strength = data.Strength;

            //  SpecularPower must be > 0, because pow() makes NaN results if called with zero
            MyDebug.AssertRelease(data.SpecularPower > 0);

            SpecularIntensity = data.SpecularShininess;
            SpecularPower     = data.SpecularPower;
            m_diffuse         = data.Diffuse;
            m_normal          = data.Normal;
        }

        internal void LoadContent()
        {
        }

        internal void UnloadContent()
        {
            m_textures = null;
        }

        internal MyCustomMaterialTextures GetTextures()
        {
            if (m_textures == null)
            {
                m_textures = new MyCustomMaterialTextures();

                m_textures.TextureDiffuse = MyTextureManager.GetTexture<MyTexture2D>(m_diffuse, "", null, LoadingMode.Lazy);

                if (MyRenderConstants.RenderQualityProfile.UseNormals)
                    m_textures.TextureNormal = MyTextureManager.GetTexture<MyTexture2D>(m_normal, "", null, LoadingMode.Lazy);
            }

            m_textures.TextureDiffuse.CheckTextureClass(MyTextureClassEnum.DiffuseEmissive);
            m_textures.TextureNormal.CheckTextureClass(MyTextureClassEnum.NormalSpecular);

            return m_textures;
        }
    }
}

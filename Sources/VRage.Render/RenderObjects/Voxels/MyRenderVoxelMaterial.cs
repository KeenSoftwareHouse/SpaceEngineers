using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender.Textures;
using VRageRender.Utils;


namespace VRageRender
{
    class MyVoxelMaterialTextures
    {
        public MyTexture2D TextureDiffuseForAxisXZ;
        public MyTexture2D TextureDiffuseForAxisY;
        public MyTexture2D TextureNormalMapForAxisXZ;
        public MyTexture2D TextureNormalMapForAxisY;
    }

    public class MyRenderVoxelMaterial
    {
        public float SpecularIntensity { get; set; }
        public float SpecularPower { get; set; }
        public bool UseFlag;
        
        public Vector4 DistancesAndScale;
        public Vector4 DistancesAndScaleFar;
        public Vector2 DistancesAndScaleFar3;
        public Color Far3Color;
        public float ExtensionDetailScale;


        byte m_materialIndex;
        MyVoxelMaterialTextures m_textures;
        string m_diffuseXZ;
        string m_diffuseY;
        string m_normalXZ;
        string m_normalY;

        
        //  Parameter 'useTwoTexturesPerMaterial' tells us if we use two textures per material. One texture for axis XZ and second for axis Y.
        //  Use it for rock/stone materials. Don't use it for gold/silver, because there you don't need to make difference between side and bottom materials.
        //  Using this we save texture memory, but pixel shader still used differenced textures (two samplers looking to same texture)
        public MyRenderVoxelMaterial(MyRenderVoxelMaterialData data)
        {
            //  SpecularPower must be > 0, because pow() makes NaN results if called with zero
            MyDebug.AssertRelease(data.SpecularPower > 0);

            m_diffuseXZ = data.DiffuseXZ;
            m_diffuseY = data.DiffuseY;
            m_normalXZ = data.NormalXZ;
            m_normalY = data.NormalY;
            SpecularIntensity = data.SpecularShininess;
            SpecularPower = data.SpecularPower;
            m_materialIndex = data.Index;

            DistancesAndScale = data.DistanceAndScale;
            DistancesAndScaleFar = data.DistanceAndScaleFar;
            ExtensionDetailScale = data.ExtensionDetailScale;
            DistancesAndScaleFar3 = data.DistanceAndScaleFar3;
            Far3Color = data.Far3Color;
        }

        //
        //force to reload all textures by dropping the old ones
        internal void LoadContent()
        {
            //m_textures = null;
        }

        internal void UnloadContent()
        {
            m_textures = null;
        }

        //  Get access to material textures with lazy loading mechanizm
        internal MyVoxelMaterialTextures GetTextures()
        {
            if (m_textures == null)
            {
                m_textures = new MyVoxelMaterialTextures();

                //  Diffuse XZ
                m_textures.TextureDiffuseForAxisXZ = MyTextureManager.GetTexture<MyTexture2D>(m_diffuseXZ, "", null, LoadingMode.Lazy);

                if (MyRenderConstants.RenderQualityProfile.UseNormals)
                {
                    //  Normal map XZ
                    m_textures.TextureNormalMapForAxisXZ = MyTextureManager.GetTexture<MyTexture2D>(m_normalXZ, "", null, LoadingMode.Lazy);
                }

                //  Diffuse Y
                if (!string.IsNullOrEmpty(m_diffuseY))
                {
                    m_textures.TextureDiffuseForAxisY = MyTextureManager.GetTexture<MyTexture2D>(m_diffuseY, "", null, LoadingMode.Lazy);

                    if (MyRenderConstants.RenderQualityProfile.UseNormals)
                    {
                        //  Normal map Y
                        m_textures.TextureNormalMapForAxisY = MyTextureManager.GetTexture<MyTexture2D>(m_normalY, "", null, LoadingMode.Lazy);
                    }

                }
                else
                {
                    m_textures.TextureDiffuseForAxisY = m_textures.TextureDiffuseForAxisXZ;

                    if (MyRenderConstants.RenderQualityProfile.UseNormals)
                    {
                        m_textures.TextureNormalMapForAxisY = m_textures.TextureNormalMapForAxisXZ;
                    }
                }
            }

            m_textures.TextureDiffuseForAxisXZ.CheckTextureClass(MyTextureClassEnum.DiffuseEmissive);
            m_textures.TextureDiffuseForAxisY.CheckTextureClass(MyTextureClassEnum.DiffuseEmissive);
            m_textures.TextureNormalMapForAxisXZ.CheckTextureClass(MyTextureClassEnum.NormalSpecular);
            m_textures.TextureNormalMapForAxisY.CheckTextureClass(MyTextureClassEnum.NormalSpecular);

            UseFlag = true;
            return m_textures;
        }

        /// <summary>
        /// Checks the normal map.
        /// </summary>
        /// <param name="texture">The texture.</param>
        private static void CheckTexture(MyTexture texture)
        {
            System.Diagnostics.Debug.Assert(texture != null, "Voxel texture missing");
            MyUtilsRender9.AssertTexture((MyTexture2D)texture);

            texture.TextureLoaded -= CheckTexture;
        }
    }
}

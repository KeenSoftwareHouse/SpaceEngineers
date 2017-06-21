using VRageRender.Import;

namespace VRageRender.Models
{
    public class MyMesh
    {
        public readonly string AssetName;
        public readonly MyMeshMaterial Material = null;

        public int IndexStart;
        public int TriStart;
        public int TriCount;

        /// <summary>
        /// c-tor - generic way for collecting resources
        /// </summary>
        /// <param name="meshInfo"></param>
        /// assetName - just for debug output
        public MyMesh(MyMeshPartInfo meshInfo, string assetName)
        {
            MyMaterialDescriptor matDesc = meshInfo.m_MaterialDesc;
            if (matDesc != null)
            {
                string texName;
                matDesc.Textures.TryGetValue("DiffuseTexture", out texName);

                var material = new MyMeshMaterial();
                material.Name = meshInfo.m_MaterialDesc.MaterialName;
                material.Textures = matDesc.Textures;
                material.DrawTechnique = meshInfo.Technique;
                material.GlassCW = meshInfo.m_MaterialDesc.GlassCW;
                material.GlassCCW = meshInfo.m_MaterialDesc.GlassCCW;
                material.GlassSmooth = meshInfo.m_MaterialDesc.GlassSmoothNormals;

                Material = material;
            }
            else
            {
                //It is OK because ie. collision meshes dont have materials
                Material = new MyMeshMaterial();
            }

            AssetName = assetName;
        }

        public MyMesh(MyMeshMaterial material, string assetName)
        {
            Material = material;
            AssetName = assetName;
        }
    }
}

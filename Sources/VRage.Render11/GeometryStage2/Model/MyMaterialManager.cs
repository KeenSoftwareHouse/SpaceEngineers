using System.Collections.Generic;
using VRage.Render11.Common;
using VRageMath;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    class MyMaterialManager : IManager, IManagerUnloadData
    {
        Dictionary<MyStandardMaterialKey, MyStandardMaterial> m_standardMaterials = new Dictionary<MyStandardMaterialKey, MyStandardMaterial>();
        Dictionary<string, MyGlassMaterial> m_glassMaterials = new Dictionary<string, MyGlassMaterial>(); // key is materialName
        
        public MyStandardMaterial GetOrCreateStandardMaterial(string materialName, MyMeshDrawTechnique technique, string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath)
        {
            MyStandardMaterialKey key = new MyStandardMaterialKey
            {
                Technique = technique,
                CmFilepath = cmFilepath,
                NgFilepath = ngFilepath,
                ExtFilepath = extFilepath,
                AlphamaskFilepath = alphamaskFilepath,
            };

            MyStandardMaterial material;
            if (m_standardMaterials.TryGetValue(key, out material))
                return material;

            material = new MyStandardMaterial();
            material.Init(key);
            
            m_standardMaterials.Add(key, material);
            
            return material;
        }

        public MyGlassMaterial GetGlassMaterial(string materialName)
        {
            MyRenderProxy.Assert(!string.IsNullOrEmpty(materialName));

            if (!m_glassMaterials.ContainsKey(materialName))
            {
                MyTransparentMaterial oldTransparentMaterial = MyTransparentMaterials.GetMaterial(materialName);
                string textureFilepath = oldTransparentMaterial.Texture;
                Vector4 color = oldTransparentMaterial.Color;
                float reflectivity = oldTransparentMaterial.Reflectivity;

                MyGlassMaterial glassMaterial = new MyGlassMaterial(materialName, textureFilepath, color, reflectivity);
                m_glassMaterials.Add(materialName, glassMaterial);
            }
            return m_glassMaterials[materialName];
        }

        void IManagerUnloadData.OnUnloadData()
        {
            m_standardMaterials.Clear();
            m_glassMaterials.Clear();
        }
    }
}

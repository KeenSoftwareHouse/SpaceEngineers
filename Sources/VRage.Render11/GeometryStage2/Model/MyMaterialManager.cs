using System;
using System.Collections.Generic;
using VRage.Generics;
using VRage.Render11.Common;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    class MyMaterialManager : IManager
    {
        Dictionary<MyMaterialKey, IMaterial> m_materials = new Dictionary<MyMaterialKey, IMaterial>();
        
        public IMaterial GetOrCreateMaterial(string materialName, MyMeshDrawTechnique technique, string cmFilepath, string ngFilepath, string extFilepath, string alphamaskFilepath)
        {
            MyMaterialKey key = new MyMaterialKey
            {
                Technique = technique,
                CmFilepath = cmFilepath,
                NgFilepath = ngFilepath,
                ExtFilepath = extFilepath,
                AlphamaskFilepath = alphamaskFilepath,
            };

            IMaterial imaterial;
            if (m_materials.TryGetValue(key, out imaterial))
                return imaterial;

            MyMaterial material = new MyMaterial();
            m_materials.Add(key, material);

            material.Init(key);

            return material;
        }
    }
}

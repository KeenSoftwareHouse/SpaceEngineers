using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Model;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    class MyModelFactory: IManager, IManagerUnloadData
    {
        Dictionary<string, MyModels> m_models = new Dictionary<string, MyModels>();
        Dictionary<string, bool> m_resultsForIsModelSuitable = new Dictionary<string, bool>(); 
        HashSet<string> m_blackListMaterials = new HashSet<string>();

        MyModels CreateModels(MyMwmData firstLodMwmData)
        {
            MyModels models = new MyModels();
            models.LoadFromMwmData(firstLodMwmData);

            return models;
        }

        public bool GetOrCreateModels(string filepath, out MyModels models)
        {
            filepath = MyMwmUtils.GetFullMwmFilepath(filepath);
            if (m_models.TryGetValue(filepath, out models))
                return true;

            // Load mwm as first lod
            MyMwmData firstLodMwmData = new MyMwmData();
            if (!firstLodMwmData.LoadFromFile(filepath))
            {
                MyRender11.Log.WriteLine(string.Format("Mwm '{0}' cannot be loaded from file", filepath));
                return false;
            }

            if (!IsModelSuitable(firstLodMwmData))
                return false;

            MyRenderProxy.Assert(!m_models.ContainsKey(firstLodMwmData.MwmFilepath));
            models = CreateModels(firstLodMwmData);
            m_models.Add(firstLodMwmData.MwmFilepath, models);

            return true;
        }

        bool IsModelSuitable(MyMwmData mwmData)
        {
            if (mwmData.HasBones)
                return false;
            if (!mwmData.IsValid2ndStream)
                return false;
            foreach (var partInfo in mwmData.PartInfos)
            {
                if (partInfo.m_MaterialDesc == null)
                    continue;
                string materialName = partInfo.m_MaterialDesc.MaterialName;
                if (m_blackListMaterials.Contains(materialName))
                    return false;

                switch (partInfo.m_MaterialDesc.Facing)
                {
                    case MyFacingEnum.None:
                        break;
                    default:
                        return false;
                }

                switch (partInfo.Technique)
                {
                    case MyMeshDrawTechnique.MESH:
                    case MyMeshDrawTechnique.ALPHA_MASKED:
                    case MyMeshDrawTechnique.DECAL:
                    case MyMeshDrawTechnique.DECAL_CUTOUT:
                    case MyMeshDrawTechnique.DECAL_NOPREMULT:
                        break;
                    default:
                        return false;
                }
            }

            // The current model is fine, let's check the lods
            foreach (var lod in mwmData.Lods)
            {
                string filepath = lod.GetModelAbsoluteFilePath(mwmData.MwmFilepath);
                if (filepath == null)
                {
                    string errMsg = string.Format("The table for lods is specified, but it is invalid (filepath for lod is missing). File: '{0}'", mwmData.MwmFilepath);
                    MyRenderProxy.Fail(errMsg);
                    MyRenderProxy.Log.WriteLine(errMsg);
                    return false;
                }

                MyMwmData lodMwmData = new MyMwmData();
                if (lodMwmData.LoadFromFile(filepath))
                {
                    bool isSuitable = IsModelSuitable(lodMwmData);
                    if (!isSuitable)
                        return false;
                }
            }
            return true;
        }

        // This function will open the file, read it and close it again. Use it only for debug!
        public bool IsModelSuitable(string filepath)
        {
            filepath = MyMwmUtils.GetFullMwmFilepath(filepath);
            if (m_resultsForIsModelSuitable.ContainsKey(filepath)) // if the results have been resolved before, reuse them!
                return m_resultsForIsModelSuitable[filepath];

            // The file has not been analyzed before, the file will be opened and analyzed:
            MyMwmData mwmData = new MyMwmData();
            if (!mwmData.LoadFromFile(filepath))
            {
                m_resultsForIsModelSuitable.Add(filepath, false);
                return false;
            }
            bool result = IsModelSuitable(mwmData);
            m_resultsForIsModelSuitable.Add(filepath, result);
            return result;
        }

        public void SetBlackListMaterialList(string[] blackListMaterials)
        {
            m_blackListMaterials.Clear();
            foreach (var mat in blackListMaterials)
                m_blackListMaterials.Add(mat);
        }

        void IManagerUnloadData.OnUnloadData()
        {
            m_models.Clear();
        }
    }

}

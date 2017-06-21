using System.Collections.Generic;
using VRage.Render11.GeometryStage2.Instancing;
using VRageMath;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Model
{
    struct MyModelInstanceMaterialStrategy
    {
        public HashSet<string> m_allMaterialNames;
        public int UniqueMaterialsCount { get; private set; }
        public Dictionary<string, int> m_instanceMaterialOffsets;

        public int GetInstanceMaterialOffset(string materialName, List<MyLod> lods)
        {
            if (!m_allMaterialNames.Contains(materialName))
                return -1;
            if (!m_instanceMaterialOffsets.ContainsKey(materialName))
            {
                int offset = m_instanceMaterialOffsets.Count;
                m_instanceMaterialOffsets.Add(materialName, offset);
                foreach (var lod in lods)
                    lod.AddInstanceMaterial(materialName, offset);
            }
            return m_instanceMaterialOffsets[materialName];
        }

        public void Init(HashSet<string> allMaterialNames)
        {
            if (m_allMaterialNames == null)
                m_allMaterialNames = new HashSet<string>();
            m_allMaterialNames.Clear();
            
            if (m_instanceMaterialOffsets == null)
                m_instanceMaterialOffsets = new Dictionary<string, int>();
            m_instanceMaterialOffsets.Clear();

            foreach (var materialName in allMaterialNames)
                m_allMaterialNames.Add(materialName);

            UniqueMaterialsCount = m_allMaterialNames.Count;
        }
    }

    // Handles switching of lods
    class MyModel
    {
        List<MyLod> m_lods;
        MyLodStrategyInfo m_lodStrategyInfo;
        MyModelInstanceMaterialStrategy m_instanceMaterialStrategy;

        void CreateFromSingleLod(MyLod lod)
        {
            MyRenderProxy.Assert(m_lods == null);
            m_lods = new List<MyLod>();
            m_lods.Add(lod);
        }

        void CreateFromMultipleLods(List<MyLod> lods)
        {
            MyRenderProxy.Assert(m_lods == null);
            m_lods = new List<MyLod>(); 
            foreach (var lod in lods)
                m_lods.Add(lod);
        }

        public MyLod GetLod(int lod)
        {
            MyRenderProxy.Assert(m_lods.Count >= 1);
            MyRenderProxy.Assert(lod < m_lodStrategyInfo.GetLodsCount());
            MyRenderProxy.Assert(lod < m_lods.Count);
            MyRenderProxy.Assert(lod >= 0);
            return m_lods[lod];
        }

        public MyLodStrategyInfo GetLodStrategyInfo()
        {
            return m_lodStrategyInfo;
        }

        public int GetInstanceMaterialOffset(string materialName)
        {
            return m_instanceMaterialStrategy.GetInstanceMaterialOffset(materialName, m_lods);
        }

        public int GetUniqueMaterialsCount()
        {
            return m_instanceMaterialStrategy.UniqueMaterialsCount;
        }

        public BoundingBox BoundingBox
        {
            get
            {
                BoundingBox box = m_lods[0].BoundingBox;
                for (int i = 1; i < m_lods.Count; i++)
                {
                    box.Min = Vector3.Min(box.Min, m_lods[i].BoundingBox.Min);
                    box.Max = Vector3.Max(box.Max, m_lods[i].BoundingBox.Max);
                }
                return box;
            }
        }

        interface ICreateLodStrategy
        {
            bool CreateLod(MyLod lod, MyMwmData lodMwmData, int lodNum, ref HashSet<string> setMaterialNames);
        }

        class MyCreateGBufferLodStrategy : ICreateLodStrategy
        {
            public bool CreateLod(MyLod lod, MyMwmData lodMwmData, int lodNum, ref HashSet<string> setMaterialNames)
            {
                return lod.CreateGBuffer(lodMwmData, lodNum, ref setMaterialNames);
            }
        }
        
        class MyCreateDepthLodStrategy : ICreateLodStrategy
        {
            public bool CreateLod(MyLod lod, MyMwmData lodMwmData, int lodNum, ref HashSet<string> setMaterialNames)
            {
                // material names set should not be applied
                return lod.CreateDepth(lodMwmData, lodNum, ref setMaterialNames);
            }
        }

        class MyCreateHighlightLodStrategy : ICreateLodStrategy
        {
            public bool CreateLod(MyLod lod, MyMwmData lodMwmData, int lodNum, ref HashSet<string> setMaterialNames)
            {
                // material names set should not be applied
                return lod.CreateHighlight(lodMwmData, lodNum, ref setMaterialNames);
            }
        }

        static MyCreateGBufferLodStrategy m_createGBufferLodStrategy = new MyCreateGBufferLodStrategy();
        static MyCreateDepthLodStrategy m_createDepthLodStrategy = new MyCreateDepthLodStrategy();
        static MyCreateHighlightLodStrategy m_createHighlightLodStrategy = new MyCreateHighlightLodStrategy();

        static HashSet<string> m_tmpAllMaterialNames = new HashSet<string>();

        // The mwmData will be loaded and if there are link to another files with lods, they will be loaded
        bool LoadFromMwmData(MyMwmData firstLodMwmData, ICreateLodStrategy strategy)
        {
            m_tmpAllMaterialNames.Clear();
            MyLod firstLods = new MyLod();
            if (!strategy.CreateLod(firstLods, firstLodMwmData, 0, ref m_tmpAllMaterialNames))
            {
                MyRender11.Log.WriteLine(string.Format("Mwm '{0}' cannot be loaded as the 1st lod", firstLodMwmData.MwmFilepath));
                return false;
            }

            // if no lods are specified inside of the mwm, models with the single lod will be created:
            if (firstLodMwmData.Lods.Length == 0)
            {
                CreateFromSingleLod(firstLods);
            }
            else // otherwise all lods will be loaded and initilised:
            {
                m_lodStrategyInfo.Init(firstLodMwmData.Lods);
                List<MyLod> multipleLods = new List<MyLod>();
                multipleLods.Add(firstLods);
                for (int i = 0; i < firstLodMwmData.Lods.Length; i++)
                {
                    string lodFilepath = firstLodMwmData.Lods[i].GetModelAbsoluteFilePath(firstLodMwmData.MwmFilepath);

                    MyLod lod = new MyLod();
                    MyMwmData mwmData = new MyMwmData();
                    if (!mwmData.LoadFromFile(lodFilepath))
                    {
                        MyRender11.Log.WriteLine(string.Format("Mwm '{0}' cannot be loaded as the other lod", lodFilepath));
                        continue;
                    }
                    strategy.CreateLod(lod, mwmData, i + 1, ref m_tmpAllMaterialNames);
                    multipleLods.Add(lod);
                }
                CreateFromMultipleLods(multipleLods);

                m_lodStrategyInfo.ReduceLodsCount(m_lods.Count);
            }

            m_instanceMaterialStrategy.Init(m_tmpAllMaterialNames);

            return true;
        }

        public bool LoadGBufferModelFromMwmData(MyMwmData firstLodMwmData)
        {
            return LoadFromMwmData(firstLodMwmData, m_createGBufferLodStrategy);
        }

        public bool LoadDepthModelFromMwmData(MyMwmData firstLodMwmData)
        {
            return LoadFromMwmData(firstLodMwmData, m_createDepthLodStrategy);
        }

        public bool LoadHighlightModelFromMwmData(MyMwmData firstLodMwmData)
        {
            return LoadFromMwmData(firstLodMwmData, m_createHighlightLodStrategy);
        }
    }

    struct MyModels
    {
        public MyModel StandardModel;
        public MyModel DepthModel;
        public MyModel HighlightModel;
        public bool IsValid { get { return StandardModel != null && DepthModel != null && HighlightModel != null; } }
        public string Filepath { get; private set; }

        public bool LoadFromMwmData(MyMwmData firstLodMwmData)
        {
            StandardModel = new MyModel();
            if (!StandardModel.LoadGBufferModelFromMwmData(firstLodMwmData))
                return false;
            DepthModel = new MyModel();
            if (!DepthModel.LoadDepthModelFromMwmData(firstLodMwmData))
                return false;
            HighlightModel = new MyModel();
            if (!HighlightModel.LoadHighlightModelFromMwmData(firstLodMwmData))
                return false;
            Filepath = firstLodMwmData.MwmFilepath;
            return true;
        }
    }
}



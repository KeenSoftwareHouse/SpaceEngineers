using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    struct MyLodInstanceMaterialsStrategy
    {
        List<int> m_instanceMaterialDataOffsets;

        public int Count { get { return m_instanceMaterialDataOffsets.Count; } }

        public void Init()
        {
            if (m_instanceMaterialDataOffsets == null)
                m_instanceMaterialDataOffsets = new List<int>();

            m_instanceMaterialDataOffsets.Clear();
        }

        public void Add(string materialName, int instanceMaterialDataOffset, List<MyPart> parts)
        {
            bool found = false;
            int lodOffset = m_instanceMaterialDataOffsets.Count;
            foreach (var part in parts)
                if (part.Name == materialName)
                {
                    part.SetInstanceMaterialOffset(lodOffset);
                    found = true;
                }
            if (found)
            {
                m_instanceMaterialDataOffsets.Add(instanceMaterialDataOffset);
            }
        }

        public List<int> InstanceMaterialDataOffsets { get { return m_instanceMaterialDataOffsets; } }
    }

    class MyLod
    {
        public int UniqueId { get; private set; }
        public int LodNum { get; private set; }

        // this member is standardly used for rendering
        public List<MyPart> Parts { get; private set; }
        public Dictionary<string, MySection> Sections { get; private set; }
        
        public IVertexBuffer VB0 { get; private set; }
        public IVertexBuffer VB1 { get; private set; }
        public IIndexBuffer IB { get; private set; }
        public List<MyVertexInputComponent> VertexInputComponents { get; private set; }
        MyLodInstanceMaterialsStrategy m_instanceMaterialsStrategy;
        
        public BoundingBox BoundingBox { get; private set; }

        public int InstanceMaterialsCount { get { return m_instanceMaterialsStrategy.Count; }}
        public void AddInstanceMaterial(string materialName, int instanceMaterialDataOffset)
        {
            m_instanceMaterialsStrategy.Add(materialName, instanceMaterialDataOffset, Parts);
        }

        public List<int> GetInstanceMaterialDataOffsets()
        {
            return m_instanceMaterialsStrategy.InstanceMaterialDataOffsets;
        }

        public bool CreateGBuffer(MyMwmData mwmData, int lodNum, ref HashSet<string> setMaterialNames)
        {
            UniqueId = MyManagers.IDGenerator.GBufferLods.Generate();
            LodNum = lodNum;

            IB = MyManagers.ModelBuffers.GetOrCreateIB(mwmData);
            VB0 = MyManagers.ModelBuffers.GetOrCreateVB0(mwmData);
            VB1 = MyManagers.ModelBuffers.GetOrCreateVB1(mwmData);
            VertexInputComponents = MyManagers.ModelBuffers.CreateStandardVertexInputComponents(mwmData);
            m_instanceMaterialsStrategy.Init();

            BoundingBox = mwmData.BoundindBox;

            Sections = null;
            Parts = new List<MyPart>();
            
            int offset = 0;
            foreach (var mwmPartInfo in mwmData.PartInfos)
            {
                string materialName = mwmPartInfo.GetMaterialName();
                MyMeshDrawTechnique technique = mwmPartInfo.Technique;
                string cmFilepath = MyMwmUtils.GetColorMetalTexture(mwmPartInfo, mwmData.MwmContentPath);
                string ngFilepath = MyMwmUtils.GetNormalGlossTexture(mwmPartInfo, mwmData.MwmContentPath);
                string extFilepath = MyMwmUtils.GetExtensionTexture(mwmPartInfo, mwmData.MwmContentPath);
                string alphamaskFilepath = MyMwmUtils.GetAlphamaskTexture(mwmPartInfo, mwmData.MwmContentPath);
                IMaterial material = MyManagers.Materials.GetOrCreateMaterial(materialName, technique, cmFilepath, ngFilepath, extFilepath, alphamaskFilepath);

                // Making of parts is connected to the creating index buffer. It will worth to do it much more connected in future
                int indicesCount = mwmPartInfo.m_indices.Count;
                MyPart part = new MyPart();
                part.InitForGBuffer(this, materialName, mwmData.MwmContentPath, mwmPartInfo, material, offset, indicesCount, 0);
                Parts.Add(part);
                offset += indicesCount;

                setMaterialNames.Add(materialName);
            }

            return true;
        }

        // this method will create parts that way, that it will concatenate following parts into single part
        public bool CreateDepth(MyMwmData mwmData, int lodNum, ref HashSet<string> setMaterialNames)
        {
            UniqueId = MyManagers.IDGenerator.DepthLods.Generate();
            LodNum = lodNum;

            IB = MyManagers.ModelBuffers.GetOrCreateIB(mwmData);
            VB0 = MyManagers.ModelBuffers.GetOrCreateVB0(mwmData);
            VB1 = null;
            VertexInputComponents = MyManagers.ModelBuffers.CreateShadowVertexInputComponents(mwmData);
            m_instanceMaterialsStrategy.Init();

            Sections = null;
            Parts = new List<MyPart>();
            int indicesStart = 0;
            int indicesCount = 0;
            foreach (var mwmPartInfo in mwmData.PartInfos)
            {
                string materialName = mwmPartInfo.GetMaterialName();
                int partIndicesCount = mwmPartInfo.m_indices.Count;
                if (MyMwmUtils.NoShadowCasterMaterials.Contains(materialName))
                { // if the current part should be skipped, we will check, whether the previous parts will create a merged part
                    if (indicesCount != 0)
                    {
                        MyPart part = new MyPart();
                        part.InitForDepth(this, "ShadowProxy", MyMeshDrawTechnique.MESH, indicesStart, indicesCount, 0);
                        Parts.Add(part);
                    }
                    indicesStart += indicesCount;
                    indicesCount = 0;
                    indicesStart += partIndicesCount;
                }
                else
                    indicesCount += partIndicesCount;

                setMaterialNames.Add(materialName);
            }
            if (indicesCount != 0)
            {
                MyPart part = new MyPart();
                part.InitForDepth(this, "ShadowProxy", MyMeshDrawTechnique.MESH, indicesStart, indicesCount, 0);
                Parts.Add(part);
            }

            return true;
        }

        public bool CreateHighlight(MyMwmData mwmData, int lodNum, ref HashSet<string> setMaterialNames)
        {
            UniqueId = -1;
            LodNum = lodNum;

            IB = MyManagers.ModelBuffers.GetOrCreateIB(mwmData);
            VB0 = MyManagers.ModelBuffers.GetOrCreateVB0(mwmData);
            VB1 = MyManagers.ModelBuffers.GetOrCreateVB1(mwmData);
            VertexInputComponents = MyManagers.ModelBuffers.CreateShadowVertexInputComponents(mwmData);
            m_instanceMaterialsStrategy.Init();

            BoundingBox = mwmData.BoundindBox;
            int offset = 0;
            Parts = new List<MyPart>();
            foreach (var mwmPartInfo in mwmData.PartInfos)
            {
                string materialName = mwmPartInfo.GetMaterialName();
                int indicesCount = mwmPartInfo.m_indices.Count;
                MyPart part = new MyPart();
                part.InitForHighlight(this, mwmPartInfo.GetMaterialName(), mwmPartInfo.Technique, offset, indicesCount, 0);
                Parts.Add(part);
                offset += indicesCount;

                setMaterialNames.Add(materialName);
            }
            Sections = null;
            if (mwmData.SectionInfos != null && mwmData.SectionInfos.Count > 0)
            {
                Sections = new Dictionary<string, MySection>(mwmData.SectionInfos.Count);
                for (int i = 0; i < mwmData.SectionInfos.Count; i++)
                {
                    MyMeshSectionInfo sectionInfo = mwmData.SectionInfos[i];
                    MySection section = new MySection();
                    section.Init(this, sectionInfo, Parts);
                    Sections.Add(sectionInfo.Name, section);
                }
            }

            return true;
        }
    }
}

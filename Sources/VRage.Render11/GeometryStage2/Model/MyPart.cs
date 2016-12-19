using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Rendering;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    // The class contains information how to draw one part
    class MyPart
    {
        public MyLod Parent { get; private set; }
        public string Name { get; private set; }
        public IMaterial Material { get; private set; }
        public int InstanceMaterialOffsetInLod { get; private set; }

        MyShaderBundle[] m_shaderBundles;
        public MyShaderBundle GetShaderBundle(MyInstanceLodState state) 
        {
            MyRenderProxy.Assert((int)state < m_shaderBundles.Length, "Shader bundle is not correctly initialised");
            MyRenderProxy.Assert(m_shaderBundles[(int)state] != null, "Shader bundle is not defined for the state");
            return m_shaderBundles[(int) state];
        }

        internal MyMeshDrawTechnique Technique { get; private set; }
        public int StartIndex { get; private set; }
        public int IndicesCount { get; private set; }
        public int StartVertex { get; private set; }

        public void SetInstanceMaterialOffset(int lodOffset)
        {
            InstanceMaterialOffsetInLod = lodOffset;
        }

        public void InitForGBuffer(MyLod parent, string name, string contentPath, MyMeshPartInfo mwmPartInfo, IMaterial material,
            int startIndex, int indicesCount, int startVertex)
        {
            MyRenderProxy.Assert(Material == null && StartIndex == 0 && IndicesCount == 0 && StartVertex == 0, "The part has been initialised before!");
            MyRenderProxy.Assert(indicesCount != 0, "Invalid input");
 
            bool isCmTexture = !string.IsNullOrEmpty(MyMwmUtils.GetColorMetalTexture(mwmPartInfo, contentPath));
            bool isNgTexture = !string.IsNullOrEmpty(MyMwmUtils.GetNormalGlossTexture(mwmPartInfo, contentPath));
            bool isExtTexture = !string.IsNullOrEmpty(MyMwmUtils.GetExtensionTexture(mwmPartInfo, contentPath));

            Technique = mwmPartInfo.Technique;
            m_shaderBundles = new MyShaderBundle[(int)MyInstanceLodState.StatesCount];
            m_shaderBundles[(int)MyInstanceLodState.Solid] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.GBuffer,
                Technique,
                MyInstanceLodState.Solid,
                isCmTexture,
                isNgTexture,
                isExtTexture);
            m_shaderBundles[(int)MyInstanceLodState.Transition] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.GBuffer,
                Technique,
                MyInstanceLodState.Transition,
                isCmTexture,
                isNgTexture,
                isExtTexture);
            m_shaderBundles[(int)MyInstanceLodState.Hologram] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.GBuffer,
                Technique,
                MyInstanceLodState.Hologram,
                isCmTexture,
                isNgTexture,
                isExtTexture);
            m_shaderBundles[(int)MyInstanceLodState.Dithered] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.GBuffer,
                Technique,
                MyInstanceLodState.Dithered,
                isCmTexture,
                isNgTexture,
                isExtTexture);
            Parent = parent;
            Name = name; 
            Material = material;
            InstanceMaterialOffsetInLod = -1; // <- not used so far
            StartIndex = startIndex;
            IndicesCount = indicesCount;
            StartVertex = startVertex;
        }

        public void InitForDepth(MyLod parent, string name, MyMeshDrawTechnique technique, int startIndex, int indicesCount,
            int startVertex)
        {
            m_shaderBundles = new MyShaderBundle[(int)1];  // only solid rendering is enabled
            m_shaderBundles[(int)MyInstanceLodState.Solid] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.Depth,
                technique,
                MyInstanceLodState.Solid,
                false,
                false,
                false);

            Parent = parent;
            Name = name;
            Technique = technique;
            Material = null;
            InstanceMaterialOffsetInLod = -1; // <- not used so far
            StartIndex = startIndex;
            IndicesCount = indicesCount;
            StartVertex = startVertex;
        }

        public void InitForHighlight(MyLod parent, string name, MyMeshDrawTechnique technique, int startIndex, int indicesCount,
            int startVertex)
        {
            m_shaderBundles = new MyShaderBundle[(int)2];  // only solid rendering is enabled
            m_shaderBundles[(int)MyInstanceLodState.Solid] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.Highlight,
                technique,
                MyInstanceLodState.Solid,
                false,
                false,
                false);

            m_shaderBundles[(int)MyInstanceLodState.Transition] = MyManagers.ShaderBundles.GetShaderBundle(MyRenderPassType.Highlight,
                technique,
                MyInstanceLodState.Transition,
                false,
                false,
                false);

            Parent = parent;
            Name = name;
            Technique = technique;
            Material = null;
            InstanceMaterialOffsetInLod = -1; // <- not used so far
            StartIndex = startIndex;
            IndicesCount = indicesCount;
            StartVertex = startVertex;
        }
    }
}

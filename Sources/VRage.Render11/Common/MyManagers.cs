using VRage.Render11.GeometryStage;
using VRage.Render11.GeometryStage2;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Culling;
using VRage.Render11.GeometryStage2.Instancing;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.GeometryStage2.Rendering;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.Common
{
    interface IManager
    {
        
    }

    interface IManagerDevice
    {
        void OnDeviceInit();
        void OnDeviceReset();
        void OnDeviceEnd();
    }

    interface IManagerUnloadData
    {
        void OnUnloadData();
    }

    interface IManagerFrameEnd
    {
        void OnFrameEnd();
    }

    interface IManagerUpdate
    {
        void OnUpdate();
    }

    class MyManagers
    {
        public static MyDeferredRenderContextManager DeferredRCs = new MyDeferredRenderContextManager();

        public static MyBlendStateManager BlendStates = new MyBlendStateManager();
        public static MyDepthStencilStateManager DepthStencilStates = new MyDepthStencilStateManager();
        public static MyRasterizerStateManager RasterizerStates = new MyRasterizerStateManager();
        public static MySamplerStateManager SamplerStates = new MySamplerStateManager();

        public static MyGeneratedTextureManager GeneratedTextures = new MyGeneratedTextureManager();
        public static MyFileTextureManager FileTextures = new MyFileTextureManager();
        public static MyFileArrayTextureManager FileArrayTextures = new MyFileArrayTextureManager();
        public static MyRwTextureManager RwTextures = new MyRwTextureManager();
        public static MyCustomTextureManager CustomTextures = new MyCustomTextureManager();
        public static MyDepthStencilManager DepthStencils = new MyDepthStencilManager();
        public static MyArrayTextureManager ArrayTextures = new MyArrayTextureManager();

        public static MyBorrowedRwTextureManager RwTexturesPool = new MyBorrowedRwTextureManager();
        public static MyDynamicFileArrayTextureManager DynamicFileArrayTextures = new MyDynamicFileArrayTextureManager();
        public static MyRwTextureCatalog RwTexturesCatalog = new MyRwTextureCatalog();
        public static MyGlobalResources GlobalResources = new MyGlobalResources();

        public static MyBufferManager Buffers = new MyBufferManager();

        public static MyShadowCoreManager ShadowCore = new MyShadowCoreManager();
        public static MyShadowManager Shadow = new MyShadowManager();
        public static MyEnvironmentProbe EnvironmentProbe = new MyEnvironmentProbe();
        public static MyGeometryTextureSystem GeometryTextureSystem = new MyGeometryTextureSystem();

        public static MyGeometrySrvResolver GeometrySrvResolver = new MyGeometrySrvResolver();
        public static MyShaderBundleManager ShaderBundles = new MyShaderBundleManager();
        public static MyIDGeneratorManager IDGenerator = new MyIDGeneratorManager(); 
        public static MyMaterialManager Materials = new MyMaterialManager();
        public static MyModelBufferManager ModelBuffers = new MyModelBufferManager();
        public static MyModelFactory ModelFactory = new MyModelFactory();
        public static MyInstanceManager Instances = new MyInstanceManager();

        public static GeometryStage2.Rendering.MyGeometryRenderer GeometryRenderer = new GeometryStage2.Rendering.MyGeometryRenderer();
        public static MyHierarchicalCulledEntitiesManager HierarchicalCulledEntities = new MyHierarchicalCulledEntitiesManager();

        static MyGeneralManager m_generalManager = new MyGeneralManager();

        static MyManagers m_instance = new MyManagers(); // the intance is created, just because of calling constructor

        public MyManagers()
        {
            m_generalManager.RegisterManager(MyManagers.DeferredRCs);

            m_generalManager.RegisterManager(MyManagers.BlendStates);
            m_generalManager.RegisterManager(MyManagers.DepthStencilStates);
            m_generalManager.RegisterManager(MyManagers.RasterizerStates);
            m_generalManager.RegisterManager(MyManagers.SamplerStates);

            m_generalManager.RegisterManager(MyManagers.GeneratedTextures);
            m_generalManager.RegisterManager(MyManagers.FileTextures);
            m_generalManager.RegisterManager(MyManagers.RwTextures);
            m_generalManager.RegisterManager(MyManagers.CustomTextures);
            m_generalManager.RegisterManager(MyManagers.DepthStencils);
            m_generalManager.RegisterManager(MyManagers.FileArrayTextures);
            m_generalManager.RegisterManager(MyManagers.DynamicFileArrayTextures);
            m_generalManager.RegisterManager(MyManagers.ArrayTextures);

            m_generalManager.RegisterManager(MyManagers.RwTexturesCatalog);
            m_generalManager.RegisterManager(MyManagers.RwTexturesPool);
            m_generalManager.RegisterManager(MyManagers.GlobalResources);

            m_generalManager.RegisterManager(MyManagers.Buffers);

            m_generalManager.RegisterManager(MyManagers.ShadowCore);
            m_generalManager.RegisterManager(MyManagers.Shadow);
            m_generalManager.RegisterManager(MyManagers.EnvironmentProbe);
            m_generalManager.RegisterManager(MyManagers.GeometryTextureSystem);

            m_generalManager.RegisterManager(MyManagers.GeometrySrvResolver);
            m_generalManager.RegisterManager(MyManagers.ShaderBundles);
            m_generalManager.RegisterManager(MyManagers.IDGenerator);
            m_generalManager.RegisterManager(MyManagers.Materials);
            m_generalManager.RegisterManager(MyManagers.ModelBuffers);
            m_generalManager.RegisterManager(MyManagers.ModelFactory);
            m_generalManager.RegisterManager(MyManagers.Instances);

            m_generalManager.RegisterManager(MyManagers.GeometryRenderer);
            m_generalManager.RegisterManager(MyManagers.HierarchicalCulledEntities);
        }

        public static void OnDeviceInit()
        {
            m_generalManager.OnDeviceInit();
        }

        public static void OnDeviceReset()
        {
            m_generalManager.OnDeviceReset();
        }

        public static void OnDeviceEnd()
        {
            m_generalManager.OnDeviceEnd();
        }

        public static void OnUnloadData()
        {
            m_generalManager.OnUnloadData();
        }

        public static void OnFrameEnd()
        {
            m_generalManager.OnFrameEnd();
        }

        public static void OnUpdate()
        {
            m_generalManager.OnUpdate();
        }
    }
}

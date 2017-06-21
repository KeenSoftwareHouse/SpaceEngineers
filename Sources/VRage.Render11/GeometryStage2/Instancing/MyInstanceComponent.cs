using System;
using System.Collections.Generic;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Culling;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.GeometryStage2.Rendering;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Instancing
{
    struct MyInstanceMaterial
    {
        public Vector3 ColorMult
        {
            get { return m_colorMult; }
            set
            {
                m_colorMult = value;
                PackedColorMultEmissivity = new HalfVector4(new Vector4(m_colorMult, m_emissivity));
            }
        }

        public float Emissivity
        {
            get { return m_emissivity; }
            set
            {
                m_emissivity = value;
                PackedColorMultEmissivity = new HalfVector4(new Vector4(m_colorMult, m_emissivity));
            }
        }

        Vector3 m_colorMult;
        float m_emissivity;
        public HalfVector4 PackedColorMultEmissivity { get; private set; }

        public static MyInstanceMaterial Default = new MyInstanceMaterial
        {
            ColorMult = Vector3.One,
            Emissivity = 0,
        };
    }

    // this is "only" container for MyInstanceMaterial
    [PooledObject]
    class MyInstanceMaterialList
    {
        public MyInstanceMaterial[] m_instanceMaterials;
        public bool[] m_explicitInstanceMaterials;

        public void SetSize(int size)
        {
            MyRenderProxy.Assert(size > 0);
            if (size == 0)
                return;
            if (m_instanceMaterials == null || size > m_instanceMaterials.Length)
                m_instanceMaterials = new MyInstanceMaterial[size];

            if (m_explicitInstanceMaterials == null || size > m_explicitInstanceMaterials.Length)
                m_explicitInstanceMaterials = new bool[size];
        }

        public int GetSize()
        {
            return m_instanceMaterials.Length;
        }

        public MyInstanceMaterial Get(int index)
        {
            return m_instanceMaterials[index];
        }

        public bool IsExplicitlySet(int index)
        {
            return m_explicitInstanceMaterials[index];
        }

        public void Set(int index, MyInstanceMaterial instanceMaterial)
        {
            m_instanceMaterials[index] = instanceMaterial;
            m_explicitInstanceMaterials[index] = true;
        }

        [PooledObjectCleaner]
        public static void Clear(MyInstanceMaterialList culledEntity)
        {
            if (culledEntity.m_explicitInstanceMaterials != null)
                for (int i = 0; i < culledEntity.m_explicitInstanceMaterials.Length; i++)
                    culledEntity.m_explicitInstanceMaterials[i] = false;
        }
    }

    struct MyInstanceVisibilityStrategy
    {
        bool m_visibility;
        MyVisibilityExtFlags m_visibilityExt;

        bool m_cachedGbuffer;
        bool m_cachedDepth;
        bool m_cachedForward;

        public bool Visibility
        {
            get { return m_visibility; }
            set
            {
                m_visibility = value;
                UpdateCache();
            }
        }

        public MyVisibilityExtFlags VisibilityExt
        {
            get
            {
                return m_visibilityExt;
            }
            private set
            {
                m_visibilityExt = value;
                UpdateCache();
            }
        }

        public void Init(bool isVisible, MyVisibilityExtFlags visibilityExt)
        {
            VisibilityExt = visibilityExt;
            Visibility = isVisible;
        }

        void UpdateCache()
        {
            if (m_visibility == true)
            {
                m_cachedGbuffer = (m_visibilityExt & MyVisibilityExtFlags.Gbuffer) == MyVisibilityExtFlags.Gbuffer;
                m_cachedDepth = (m_visibilityExt & MyVisibilityExtFlags.Depth) == MyVisibilityExtFlags.Depth;
                m_cachedForward = (m_visibilityExt & MyVisibilityExtFlags.Forward) == MyVisibilityExtFlags.Forward;
            }
            else
            {
                m_cachedGbuffer = false;
                m_cachedDepth = false;
                m_cachedForward = false;
            }
        }


        public bool GBufferVisibility { get { return m_cachedGbuffer; } }
        public bool DepthVisibility { get { return m_cachedDepth; } }
        public bool ForwardVisibility { get { return m_cachedForward; } }
    }

    interface ITransformStrategy
    {
        int Count { get; }
        void GetMatrixCols(int nInstance, out Vector4 col0, out Vector4 col1, out Vector4 col2); // because of the performance

        void SetCoreMatrix(MatrixD matrix);
        Vector3D GetCoreTranslation();
        MatrixD GetCoreMatrixD();
    }

    [PooledObject]
    class MySingleTransformStrategy : ITransformStrategy
    {
        Vector3D m_worldTranslation = Vector3.Zero;
        Vector4 m_matrixCol0 = new Vector4();
        Vector4 m_matrixCol1 = new Vector4();
        Vector4 m_matrixCol2 = new Vector4();
        MatrixD m_coreMatrixD;

        public int Count { get { return 1; } }

        public void GetMatrixCols(int nInstance, out Vector4 col0, out Vector4 col1, out Vector4 col2)
        {
            Vector3 translation = m_worldTranslation - MyRender11.Environment.Matrices.CameraPosition;
            col0 = m_matrixCol0;
            col0.W = translation.X;
            col1 = m_matrixCol1;
            col1.W = translation.Y;
            col2 = m_matrixCol2;
            col2.W = translation.Z;
        }

        public void SetCoreMatrix(MatrixD worldMatrix)
        {
            m_coreMatrixD = worldMatrix;

            m_matrixCol0 = new Vector4((float)worldMatrix.M11, (float)worldMatrix.M21, (float)worldMatrix.M31, 0);
            m_matrixCol1 = new Vector4((float)worldMatrix.M12, (float)worldMatrix.M22, (float)worldMatrix.M32, 0);
            m_matrixCol2 = new Vector4((float)worldMatrix.M13, (float)worldMatrix.M23, (float)worldMatrix.M33, 0);
            m_worldTranslation = new Vector3D(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
        }

        public Vector3D GetCoreTranslation()
        {
            return m_worldTranslation;
        }

        public MatrixD GetCoreMatrixD()
        {
            return m_coreMatrixD;
        }

        [PooledObjectCleaner]
        public static void Cleanup(MySingleTransformStrategy strategy)
        {
            strategy.SetCoreMatrix(MatrixD.Identity);
        }
    }

    [PooledObject]
    class MyMultiTransformStrategy : ITransformStrategy
    {
        Vector3D m_worldTranslation;
        Matrix m_transposedMatrix = Matrix.Identity;
        Matrix m_coreMatrixD = MatrixD.Identity;
        List<Matrix> m_instanceMatrices = new List<Matrix>();

        public int Count { get { return m_instanceMatrices.Count; } }

        public void GetMatrixCols(int nInstance, out Vector4 col0, out Vector4 col1, out Vector4 col2)
        {
            Vector3 translation = m_worldTranslation - MyRender11.Environment.Matrices.CameraPosition;

            Matrix mat = m_transposedMatrix;
            mat.M14 = translation.X;
            mat.M24 = translation.Y;
            mat.M34 = translation.Z;
            mat = mat * m_instanceMatrices[nInstance];
            col0 = mat.GetRow(0); // the matrix is transposed
            col1 = mat.GetRow(1);
            col2 = mat.GetRow(2);
        }

        public void SetInstanceData(MyInstanceData[] data, int start, int count)
        {
            m_instanceMatrices.Clear();
            for (int i = 0; i < count; i++)
                m_instanceMatrices.Add(Matrix.Transpose(data[start + i].LocalMatrix));
        }

        public void SetCoreMatrix(MatrixD worldMatrix)
        {
            m_transposedMatrix = Matrix.Transpose(worldMatrix);
            m_worldTranslation = new Vector3D(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
            m_coreMatrixD = worldMatrix;
        }

        public Vector3D GetCoreTranslation()
        {
            return m_worldTranslation;
        }

        public MatrixD GetCoreMatrixD()
        {
            return m_coreMatrixD;
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyMultiTransformStrategy strategy)
        {
            strategy.SetCoreMatrix(MatrixD.Identity);
        }
    }
    
    // Temporary data structure for the conversion from the instance component to the renderable component
    // This is hotfix
    struct MyCompatibilityDataForTheOldPipeline
    {
        public string MwmFilepath;
        public float Rescale;
        public RenderFlags RenderFlags;
        public byte DepthBias;
    }

    class MyInstanceComponent : MyActorComponent
    {
        MyCpuCulledEntity m_cpuCulledEntity;
        public MyModels Models { get; private set; }
        public MyModel StandardModel { get { return Models.StandardModel; } }
        public MyModel DepthModel { get { return Models.DepthModel; } }
        public HalfVector3 KeyColor;
        MyInstanceMaterialList m_instanceMaterials;
        MyLodStrategy m_lodStrategy = new MyLodStrategy();
        MyInstanceVisibilityStrategy m_visibilityStrategy;
        ITransformStrategy m_transformStrategy = MyObjectPoolManager.Allocate<MySingleTransformStrategy>();
        public MyCompatibilityDataForTheOldPipeline CompatibilityDataForTheOldPipeline { get; private set; }

        public HalfVector4 GlobalColorMultEmissivity { get; private set; }

        public void GetMatrixCols(int nMultiInstance, out Vector4 col0, out Vector4 col1, out Vector4 col2) // super fast!
        {
            m_transformStrategy.GetMatrixCols(nMultiInstance, out col0, out col1, out col2);
        }

        public int GetMultiTransformCount()
        {
            return m_transformStrategy.Count;
        }

        public bool IsVisible(int passId)
        {
            if (MyPassIdResolver.IsGBufferPassId(passId))
                return m_visibilityStrategy.GBufferVisibility;
            else if (MyPassIdResolver.IsDepthPassId(passId))
                return m_visibilityStrategy.DepthVisibility;
            else
                MyRenderProxy.Error("Unprocessed conditional");
            return false;
        }

        public void SetMultiInstancesTransformStrategy(MyInstanceData[] multiInstanceData, int instanceStart, int instancesCount)
        {
            if (m_transformStrategy is MySingleTransformStrategy)
            {
                MySingleTransformStrategy oldStrategy = (MySingleTransformStrategy) m_transformStrategy;
                MyMultiTransformStrategy newStrategy = MyObjectPoolManager.Allocate<MyMultiTransformStrategy>();
                newStrategy.SetCoreMatrix(m_transformStrategy.GetCoreMatrixD());
                MyObjectPoolManager.Deallocate(oldStrategy);
                m_transformStrategy = newStrategy;
            }
            
            MyRenderProxy.Assert(m_transformStrategy is MyMultiTransformStrategy);

            MyMultiTransformStrategy multiTranformStrategy = (MyMultiTransformStrategy) m_transformStrategy;
            multiTranformStrategy.SetInstanceData(multiInstanceData, instanceStart, instancesCount);
        }

        void SetSingleInstanceTransformStrategy()
        {
            if (m_transformStrategy is MySingleTransformStrategy)
                return;

            ITransformStrategy oldStrategy = m_transformStrategy;
            m_transformStrategy = MyObjectPoolManager.Allocate<MySingleTransformStrategy>();
            m_transformStrategy.SetCoreMatrix(m_transformStrategy.GetCoreMatrixD());
            if (oldStrategy is MyMultiTransformStrategy)
                MyObjectPoolManager.Deallocate((MyMultiTransformStrategy)oldStrategy);
            else if (oldStrategy is MySingleTransformStrategy)
                MyObjectPoolManager.Deallocate((MySingleTransformStrategy)oldStrategy);
            else 
                MyRenderProxy.Error("Unknown class");
        }

        public int GetLodsCount(int passId)
        {
            return m_lodStrategy.GetLodsCount(passId);
        }

        public void GetLod(int passId, int i, out MyLod lod, out MyInstanceLodState stateId, out float stateData)
        {
            int lodNum;
            m_lodStrategy.GetLod(passId, i, out lodNum, out stateId, out stateData);
            if (passId == 0)
                lod = Models.StandardModel.GetLod(lodNum);
            else
                lod = Models.DepthModel.GetLod(lodNum);
        }

        public MyLod GetHighlightLod()
        {
            return Models.HighlightModel.GetLod(0);
        }

        public bool SetInstanceMaterial(string materialName, MyInstanceMaterial instanceMaterial)
        {
            int instanceMaterialOffset = StandardModel.GetInstanceMaterialOffset(materialName);
            if (instanceMaterialOffset == -1)
                return false;

            m_instanceMaterials.Set(instanceMaterialOffset, instanceMaterial);
            return true;
        }

        public bool SetInstanceMaterialColorMult(string materialName, Vector3 colorMult)
        {
            int instanceMaterialOffset = StandardModel.GetInstanceMaterialOffset(materialName);
            if (instanceMaterialOffset == -1)
                return false;

            MyInstanceMaterial instanceMaterial = m_instanceMaterials.Get(instanceMaterialOffset);
            instanceMaterial.ColorMult = colorMult;
            m_instanceMaterials.Set(instanceMaterialOffset, instanceMaterial);
            return true;
        }

        public bool SetInstanceMaterialEmissivity(string materialName, float emissivity)
        {
            int instanceMaterialOffset = StandardModel.GetInstanceMaterialOffset(materialName);
            if (instanceMaterialOffset == -1)
                return false;

            MyInstanceMaterial instanceMaterial = m_instanceMaterials.Get(instanceMaterialOffset);
            instanceMaterial.Emissivity = emissivity;
            m_instanceMaterials.Set(instanceMaterialOffset, instanceMaterial);
            return true;
        }

        // dithered = 0 <- disables dithering
        public void SetDithered(bool isHologram, float dithered)
        {
            MyInstanceLodState state;
            float stateData;
            if (dithered == 0)
            {
                state = MyInstanceLodState.Solid;
                stateData = 0;
            }
            else if (isHologram)
            {
                state = MyInstanceLodState.Hologram;
                stateData = -dithered;
            }
            else
            {
                state = MyInstanceLodState.Dithered;
                stateData = dithered;
            }
            m_lodStrategy.SetExplicitLodState(StandardModel.GetLodStrategyInfo(), state, stateData);
        }

        public void SetGlobalEmissivity(float emissivity)
        {
            MyInstanceMaterial instanceMaterial = MyInstanceMaterial.Default;
            instanceMaterial.Emissivity = emissivity;

            // Set emissivity for parts, that are not instanced:
            GlobalColorMultEmissivity = new HalfVector4(1, 1, 1, emissivity);

            // Fill emissivity to all parts, that are not explictly set
            for (int i = 0; i < m_instanceMaterials.GetSize(); i++)
                if (!m_instanceMaterials.IsExplicitlySet(i))
                    m_instanceMaterials.Set(i, instanceMaterial);
        }

        public MyInstanceMaterial GetInstanceMaterial(int instanceMaterialOffset)
        {
            return m_instanceMaterials.Get(instanceMaterialOffset);
        }

        public void UpdateLodExplicit(List<int> activePassIds, int explicitLodNum)
        {
            MyLodStrategyInfo lodStrategyInfo = StandardModel.GetLodStrategyInfo();
            m_lodStrategy.ResolveExplicit(lodStrategyInfo, MyCommon.FrameCounter, explicitLodNum, activePassIds);
        }

        public void UpdateLodNoTransition(List<int> activePassIds, MyLodStrategyPreprocessor preprocessor)
        {
            MyLodStrategyInfo lodStrategyInfo = StandardModel.GetLodStrategyInfo();
            Vector3D cameraPos = MyRender11.Environment.Matrices.CameraPosition;
            Vector3D instancePos = m_transformStrategy.GetCoreTranslation();

            m_lodStrategy.ResolveNoTransition(lodStrategyInfo, MyCommon.FrameCounter, cameraPos, instancePos, activePassIds, preprocessor);
        }

        public void UpdateLodSmoothly(List<int> activePassIds, MyLodStrategyPreprocessor preprocessor)
        {
            MyLodStrategyInfo lodStrategyInfo = StandardModel.GetLodStrategyInfo();
            Vector3D cameraPos = MyRender11.Environment.Matrices.CameraPosition;
            Vector3D instancePos = m_transformStrategy.GetCoreTranslation();

            m_lodStrategy.ResolveSmoothly(lodStrategyInfo, MyCommon.FrameCounter, MyCommon.LastFrameDelta(), cameraPos, instancePos, activePassIds, preprocessor);
        }

        internal void InitInternal(MyModels models, bool isVisible, MyVisibilityExtFlags visibilityExt, MyCompatibilityDataForTheOldPipeline compatibilityData)
        {
            Models = models;
            KeyColor = new HalfVector3();
            m_instanceMaterials = MyObjectPoolManager.Allocate<MyInstanceMaterialList>();
            int instanceMaterialsCount = models.StandardModel.GetUniqueMaterialsCount(); // the other models do not use this mechanism
            m_instanceMaterials.SetSize(instanceMaterialsCount);
            for (int i = 0; i < instanceMaterialsCount; i++)
                m_instanceMaterials.Set(i, MyInstanceMaterial.Default);
            m_cpuCulledEntity = MyObjectPoolManager.Allocate<MyCpuCulledEntity>();

            // Bounding entity will be just registered, bounding box will be updated in the loopback OnAabbChange()
            BoundingBoxD boxTemporary = new BoundingBoxD(models.StandardModel.BoundingBox.Min, models.StandardModel.BoundingBox.Max);
            m_cpuCulledEntity.Register(boxTemporary, this);

            m_visibilityStrategy.Init(isVisible, visibilityExt);
            SetSingleInstanceTransformStrategy();

            m_lodStrategy.Init();

            Owner.SetMatrix(ref MatrixD.Identity);
            Owner.SetLocalAabb(models.StandardModel.BoundingBox);

            CompatibilityDataForTheOldPipeline = compatibilityData;
        }

        internal override void OnMatrixChange()
        {
            base.OnMatrixChange();

            m_transformStrategy.SetCoreMatrix(Owner.WorldMatrix);
        }

        internal override void OnAabbChange()
        {
            base.OnAabbChange();
            m_cpuCulledEntity.Update(ref Owner.Aabb);
        }

        internal override void OnVisibilityChange()
        {
            base.OnVisibilityChange();
            m_visibilityStrategy.Visibility = Owner.IsVisible;
        }

        internal override void OnRemove(MyActor owner)
        {
            m_lodStrategy.Destroy();
            m_cpuCulledEntity.Unregister();
            MyObjectPoolManager.Deallocate(m_instanceMaterials);
            MyObjectPoolManager.Deallocate(m_cpuCulledEntity);

            MyManagers.Instances.RemoveInternal(this);
        }
    }
}

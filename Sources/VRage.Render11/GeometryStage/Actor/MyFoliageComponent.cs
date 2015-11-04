using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using VRage.Voxels;


namespace VRageRender
{
    class MyFoliageStream
    {
        internal VertexBufferId m_stream = VertexBufferId.NULL;
        int m_allocationSize;
        internal bool m_append;

        internal void ResetAllocationSize()
        {
            m_allocationSize = 0;
        }

        internal void Reserve(int x)
        {
            m_allocationSize += x;
        }

        internal unsafe void AllocateStreamOutBuffer()
        {
            Dispose();

            var stride = sizeof(Vector3) + sizeof(uint);

            // padding to some power of 2
            m_allocationSize = ((m_allocationSize + 511) / 512) * 512;
            const int maxAlloc = 5 * 1024 * 1024;
            m_allocationSize = m_allocationSize > maxAlloc ? maxAlloc : m_allocationSize;

            m_stream = MyHwBuffers.CreateVertexBuffer(m_allocationSize, stride, BindFlags.VertexBuffer | BindFlags.StreamOutput, ResourceUsage.Default);
        }

        internal void Dispose()
        {
            if (m_stream != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_stream);
                m_stream = VertexBufferId.NULL;
            }
        }
    }

    class MyFoliageComponent : MyActorComponent
    {
        const float AllocationFactor = 3f;

        Dictionary<int, MyFoliageStream> m_streams;        

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.Foliage;

            m_streams = null;
        }

        internal void Dispose()
        {
            if (m_streams != null)
            {
                foreach (var stream in m_streams.Values)
                {
                    stream.Dispose();
                }
                m_streams = null;
            }
        }

        internal override void OnVisibilityChange()
        {
            base.OnVisibilityChange();

            if(m_owner.m_visible == false)
            {
                Dispose();
            }
        }

        internal override void Destruct()
        {
            Dispose();

            base.Destruct();
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);

            this.Deallocate();
        }

        void PrepareStream(int materialId, int triangles, int voxelLod)
        {
			float voxelSizeFactor = (float)Math.Pow(2, Math.Max(2 - voxelLod, 1) * 2) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;

            int predictedAllocation = (int)(triangles * AllocationFactor * voxelSizeFactor *
                MyVoxelMaterials1.Table[materialId].FoliageDensity);

            m_streams.SetDefault(materialId, new MyFoliageStream()).Reserve(predictedAllocation);
        }

        internal void InvalidateStreams()
        {
            Dispose();
        }

        internal void FillStreams()
        {
            var mesh = m_owner.GetRenderable().GetModel();

            int voxelLod = MyMeshes.GetVoxelInfo(mesh).Lod;
            bool voxelMeshNotReady = m_owner.m_visible == false || voxelLod > 4;
            bool alreadyFilled = m_streams != null;

            if (voxelMeshNotReady || alreadyFilled) return;

            var lodMesh = MyMeshes.GetLodMesh(mesh, 0);

            // only stream stones for lod0
            if (voxelLod > 0)
            {
                for (int i = 0; i < lodMesh.Info.PartsNum; i++)
                {
                    var triple = MyMeshes.GetVoxelPart(mesh, i).Info.MaterialTriple;
                    if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage && MyVoxelMaterials1.Table[triple.I0].FoliageType == 1)
                        return;
                    if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage && MyVoxelMaterials1.Table[triple.I1].FoliageType == 1)
                        return;
                    if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage && MyVoxelMaterials1.Table[triple.I2].FoliageType == 1)
                        return;
                }
            }

            m_streams = new Dictionary<int, MyFoliageStream>();

            // cleanup
            foreach (var kv in m_streams)
            {
                kv.Value.ResetAllocationSize();
            }

            // analyze 
            for (int i = 0; i < lodMesh.Info.PartsNum; i++ )
            {
                var partInfo = MyMeshes.GetVoxelPart(mesh, i).Info;
                var triple = partInfo.MaterialTriple;

                if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage)
                {
                    PrepareStream(triple.I0, partInfo.IndexCount / 3, voxelLod);
                }
                if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage)
                {
                    PrepareStream(triple.I1, partInfo.IndexCount / 3, voxelLod);
                }
                if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage)
                {
                    PrepareStream(triple.I2, partInfo.IndexCount / 3, voxelLod);
                }
            }

            // prepare
            foreach (var kv in m_streams)
            {
                kv.Value.AllocateStreamOutBuffer();
            }

            // analyze 
            for (int i = 0; i < lodMesh.Info.PartsNum; i++)
            {
                var partInfo = MyMeshes.GetVoxelPart(mesh, i).Info;
                var triple = partInfo.MaterialTriple;

                if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I0, 0,
                        partInfo.IndexCount, partInfo.StartIndex, 0);// kv.Value.vertexOffset);
                }
                if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I1, 1,
                        partInfo.IndexCount, partInfo.StartIndex, 0);//kv.Value.vertexOffset);
                }
                if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I2, 2,
                        partInfo.IndexCount, partInfo.StartIndex, 0);//kv.Value.vertexOffset);
                }
            }
        }

        void FillStreamWithTerrainBatch(int materialId,
            int vertexMaterialIndex, int indexCount, int startIndex, int baseVertex)
        {
            MyGPUFoliageGenerating implementation = MyGPUFoliageGenerating.GetInstance();

            // all necessary data should be same - geometry and input layout
            var renderable = m_owner.GetRenderable();
            var proxy = renderable.m_lods[0].RenderableProxies[0];
            
            // get shader for streaming

            var bundle = MyMaterialShaders.Get(X.TEXT(MyVoxelMesh.MULTI_MATERIAL_TAG), X.TEXT("foliage_streaming"), MyMeshes.VoxelLayout, MyShaderUnifiedFlags.NONE);

            //var bundle = MyShaderBundleFactory.Get(renderable.GetMesh().LODs[0].m_meshInfo.VertexLayout, MyVoxelMesh.MULTI_MATERIAL_TAG,
            //    "foliage_streaming", renderable.m_vsFlags | MyShaderUnifiedFlags.NONE);

            //var scaleMat = Matrix.CreateScale(renderable.m_voxelScale);
            //proxy.ObjectData.LocalMatrix = scaleMat;

            //var worldMat = proxy.WorldMatrix;
            //worldMat.Translation -= MyEnvironment.CameraPosition;
            //proxy.ObjectData.LocalMatrix = worldMat;

            implementation.RecordCommands(proxy, m_streams[materialId], materialId,
                bundle.VS, bundle.IL,
                vertexMaterialIndex, indexCount, startIndex, baseVertex);
        }

        internal void Render(MyFoliageRenderer impl)
        {
            if (m_streams == null || m_streams.Count == 0) 
                return;
            var renderable = m_owner.GetRenderable();
            var proxy = renderable.m_lods[0].RenderableProxies[0];

            var invScaleMat = MatrixD.CreateScale(1.0f / renderable.m_voxelScale);

            var worldMat = proxy.WorldMatrix;
            worldMat.Translation -= MyEnvironment.CameraPosition;
            proxy.ObjectData.LocalMatrix = invScaleMat * worldMat;

            foreach(var kv in m_streams)
            {
                impl.RecordCommands(proxy, kv.Value.m_stream, kv.Key);
            }
            
        }
    }

    class MyGPUFoliageGenerating : MyRenderingPass
    {
        static MyGPUFoliageGenerating m_instance = new MyGPUFoliageGenerating();
        internal static MyGPUFoliageGenerating GetInstance()
        {
            return m_instance;
        }

        //static MyGeometryShaderWithSO m_gs;
        static GeometryShaderId m_gs = GeometryShaderId.NULL;

        internal static unsafe void Init()
        {
            var soElements = new StreamOutputElement[2];
            soElements[0].Stream = 0;
            soElements[0].SemanticName = "TEXCOORD";
            soElements[0].SemanticIndex = 0;
            soElements[0].ComponentCount = 3;
            soElements[0].OutputSlot = 0;
            soElements[1].Stream = 0;
            soElements[1].SemanticName = "TEXCOORD";
            soElements[1].SemanticIndex = 1;
            soElements[1].ComponentCount = 1;
            soElements[1].OutputSlot = 0;

            var soStrides = new int[] { sizeof(Vector3) + sizeof(uint) };

            m_gs = MyShaders.CreateGs("passes/foliage_streaming/geometry_stage.hlsl", "gs", "", new MyShaderStreamOutputInfo { Elements = soElements, Strides = soStrides, RasterizerStreams = GeometryShader.StreamOutputNoRasterizedStream });

            m_instance.SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            base.Begin();

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetGS(m_gs);
            RC.SetPS(null);
            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, null);
        }

        internal sealed override void End()
        {
            CleanupPipeline();
            base.End();
        }

        internal void CleanupPipeline()
        {
            Context.GeometryShader.Set(null);
        }

        internal unsafe void RecordCommands(MyRenderableProxy proxy, MyFoliageStream stream, int voxelMatId,
            VertexShader vertexShader, InputLayout inputLayout,            
            int materialIndex, int indexCount, int startIndex, int baseVertex)
        {
            //var worldMatrix = proxy.WorldMatrix;
            //worldMatrix.Translation = Vector3D.Zero;
            //MyObjectData objectData = proxy.ObjectData;
            //objectData.LocalMatrix = Matrix.Identity;

            var worldMat = proxy.WorldMatrix;
            //worldMat.Translation -= MyEnvironment.CameraPosition;

            MyObjectData objectData = proxy.ObjectData;
            objectData.LocalMatrix = worldMat;

            MyMapping mapping;
            mapping = MyMapping.MapDiscard(RC.Context, proxy.ObjectBuffer);
            void* ptr = &objectData;
            mapping.stream.Write(new IntPtr(ptr), 0, sizeof(MyObjectData));
            mapping.Unmap();

            RC.SetCB(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            BindProxyGeometry(proxy);

            RC.SetVS(vertexShader);
            RC.SetIL(inputLayout);

            if(!stream.m_append)
            {
                Context.StreamOutput.SetTarget(stream.m_stream.Buffer, 0);
                stream.m_append = true;
            }
            else
            {
                Context.StreamOutput.SetTarget(stream.m_stream.Buffer, -1);
            }

            RC.SetCB(MyCommon.FOLIAGE_SLOT, MyCommon.FoliageConstants);

            mapping = MyMapping.MapDiscard(Context, MyCommon.FoliageConstants);
            mapping.stream.Write(MyVoxelMaterials1.Table[voxelMatId].FoliageDensity * MyRender11.Settings.GrassDensityFactor);
            mapping.stream.Write((uint)materialIndex);
            mapping.stream.Write((uint)voxelMatId);
            mapping.Unmap();

            Context.DrawIndexed(indexCount, startIndex, baseVertex);
        }
    }

    class MyFoliageRenderer : MyRenderingPass
    {
        static InputLayoutId m_inputLayout;
        static VertexShaderId m_VS;
        static GeometryShaderId[] m_GS = new GeometryShaderId[2];
        static PixelShaderId[] m_PS = new PixelShaderId[2];

        internal static void Init()
        {
            m_VS = MyShaders.CreateVs("foliage2.hlsl", "vs");
            m_GS[0] = MyShaders.CreateGs("foliage2.hlsl", "gs");
            m_PS[0] = MyShaders.CreatePs("foliage2.hlsl", "ps");
            m_GS[1] = MyShaders.CreateGs("foliage2.hlsl", "gs", MyShaderHelpers.FormatMacros("ROCK_FOLIAGE"));
            m_PS[1] = MyShaders.CreatePs("foliage2.hlsl", "ps", MyShaderHelpers.FormatMacros("ROCK_FOLIAGE"));
            m_inputLayout = MyShaders.CreateIL(m_VS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.CUSTOM_UNORM4_0));
        }

        internal MyFoliageRenderer()
        {
            SetImmediate(true);
        }
        

        internal sealed override void Begin()
        {
            base.Begin();

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            Context.InputAssembler.InputLayout = m_inputLayout;

            RC.SetCB(MyCommon.FOLIAGE_SLOT, MyCommon.MaterialFoliageTableConstants);

            RC.SetRS(MyRender11.m_nocullRasterizerState);

            RC.SetVS(m_VS);
            RC.SetIL(m_inputLayout);

            RC.BindGBufferForWrite(MyGBuffer.Main);

            RC.SetDS(MyDepthStencilState.DepthTestWrite);
        }

        internal sealed override void End()
        {
            CleanupPipeline();
            base.End();
        }

        internal void CleanupPipeline()
        {
            Context.GeometryShader.Set(null);
            Context.Rasterizer.State = null;
        }

        internal unsafe void RecordCommands(MyRenderableProxy proxy, VertexBufferId stream, int voxelMatId)
        {
            MyObjectData objectData = proxy.ObjectData;

            var foliageType = MyVoxelMaterials1.Table[voxelMatId].FoliageType;

            MyMapping mapping;
            mapping = MyMapping.MapDiscard(RC.Context, proxy.ObjectBuffer);
            void* ptr = &objectData;
            mapping.stream.Write(new IntPtr(ptr), 0, sizeof(MyObjectData));
            mapping.Unmap();

            RC.SetCB(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            RC.SetGS(m_GS[foliageType]);
            RC.SetPS(m_PS[foliageType]);

            if (MyVoxelMaterials1.Table[voxelMatId].FoliageColorTextureArray != null)
            {
                RC.BindRawSRV(0, MyVoxelMaterials1.Table[voxelMatId].FoliageColorTextureArray.ShaderView);
                RC.BindRawSRV(1, MyVoxelMaterials1.Table[voxelMatId].FoliageNormalTextureArray.ShaderView);
            }
            else
            {
                RC.BindRawSRV(0, MyTextures.GetView(MyTextures.GetTexture(MyVoxelMaterials1.Table[voxelMatId].FoliageArray_Texture, MyTextureEnum.COLOR_METAL, true)));
                RC.BindRawSRV(1, MyTextures.GetView(MyTextures.GetTexture(MyVoxelMaterials1.Table[voxelMatId].FoliageArray_NormalTexture, MyTextureEnum.NORMALMAP_GLOSS, true)));
            }

            RC.SetVB(0, stream.Buffer, stream.Stride);
            Context.DrawAuto();
            RC.Stats.DrawAuto++;
        }

        static MyFoliageRenderer m_instance = new MyFoliageRenderer();

        internal static void Render()
        {
            m_instance.ViewProjection = MyEnvironment.ViewProjectionAt0;
            m_instance.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            //m_instance.DepthBuffer = MyRender.MainGbuffer.DepthBuffer.DepthStencil;
            //m_instance.RTs = MyRender.MainGbuffer.GbufferTargets;

            m_instance.PerFrame();
            m_instance.Begin();

            var viewFrustum = new BoundingFrustumD(MyEnvironment.ViewProjectionD);
			var foliageComponents = MyComponentFactory<MyFoliageComponent>.GetAll();
            foreach(var foliageComponent in foliageComponents)
            {
				if (foliageComponent.m_owner.CalculateCameraDistance() < MyRender11.RenderSettings.FoliageDetails.GrassDrawDistance()
					&& viewFrustum.Contains(foliageComponent.m_owner.Aabb) != VRageMath.ContainmentType.Disjoint)
                {
					foliageComponent.Render(m_instance);
                }
            }

            m_instance.End();
        }
    }
}

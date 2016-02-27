using System.Diagnostics;
using SharpDX.Direct3D;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    internal sealed class MyFoliageRenderingPass : MyRenderingPass
    {
        private const string FoliageRenderShader = "foliage.hlsl";
        internal const int GrassStencilMask = 0x80;

        private static InputLayoutId m_inputLayout = InputLayoutId.NULL;
        private static VertexShaderId m_VS = VertexShaderId.NULL;
        private static GeometryShaderId[] m_GS = new GeometryShaderId[2] { GeometryShaderId.NULL, GeometryShaderId.NULL };
        private static PixelShaderId[] m_PS = new PixelShaderId[2] { PixelShaderId.NULL, PixelShaderId.NULL };

        internal MyFoliageRenderingPass()
        {
            SetImmediate(true);

            InitShaders();
        }

        private void InitShaders()
        {
            if (m_VS == VertexShaderId.NULL)
                m_VS = MyShaders.CreateVs(FoliageRenderShader);

            if (m_GS[0] == GeometryShaderId.NULL)
                m_GS[0] = MyShaders.CreateGs(FoliageRenderShader);

            if (m_PS[0] == PixelShaderId.NULL)
                m_PS[0] = MyShaders.CreatePs(FoliageRenderShader);

            var foliageMacro = new[] {new ShaderMacro("ROCK_FOLIAGE", null)};
            if (m_GS[1] == GeometryShaderId.NULL)
                m_GS[1] = MyShaders.CreateGs(FoliageRenderShader, foliageMacro);

            if (m_PS[1] == PixelShaderId.NULL)
                m_PS[1] = MyShaders.CreatePs(FoliageRenderShader, foliageMacro);

            if (m_inputLayout == InputLayoutId.NULL)
                m_inputLayout = MyShaders.CreateIL(m_VS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.CUSTOM_UNORM4_0));
        }

        internal override void Begin()
        {
            base.Begin();

            RC.BeginProfilingBlock("Foliage pass");

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            Context.InputAssembler.InputLayout = m_inputLayout;

            RC.SetCB(MyCommon.FOLIAGE_SLOT, MyCommon.MaterialFoliageTableConstants);

            RC.SetRS(MyRender11.m_nocullRasterizerState);

            RC.SetVS(m_VS);
            RC.SetIL(m_inputLayout);

            RC.BindGBufferForWrite(MyGBuffer.Main);

            RC.SetDS(MyDepthStencilState.WriteDepthAndStencil, GrassStencilMask);
        }

        internal override void End()
        {
            CleanupPipeline();
            base.End();

            RC.EndProfilingBlock();
        }

        internal void CleanupPipeline()
        {
            Context.GeometryShader.Set(null);
            Context.Rasterizer.State = null;
        }

        internal unsafe void RecordCommands(MyRenderableProxy proxy, VertexBufferId stream, int voxelMatId)
        {
            if (stream == VertexBufferId.NULL) return;

            var foliageType = MyVoxelMaterials1.Table[voxelMatId].FoliageType;

            MyMapping mapping = MyMapping.MapDiscard(RC.DeviceContext, proxy.ObjectBuffer);
            mapping.WriteAndPosition(ref proxy.NonVoxelObjectData);
            mapping.WriteAndPosition(ref proxy.CommonObjectData);
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

        internal void Render()
        {
            var foliageComponents = MyFoliageComponents.ActiveComponents;
            if (foliageComponents.Count <= 0)
                return;

            ViewProjection = MyEnvironment.ViewProjectionAt0;
            Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);

            PerFrame();
            Begin();

            var viewFrustum = new BoundingFrustumD(MyEnvironment.ViewProjectionD);

            foreach (var foliageComponent in foliageComponents)
            {
                var renderableComponent = foliageComponent.Owner.GetRenderable();
                bool removeDitheringInProgress = renderableComponent.m_objectDithering > 0 && renderableComponent.m_objectDithering < 2;
                if (!removeDitheringInProgress && foliageComponent.Owner.CalculateCameraDistance() < MyRender11.RenderSettings.FoliageDetails.GrassDrawDistance())
                {
                    if (viewFrustum.Contains(foliageComponent.Owner.Aabb) != ContainmentType.Disjoint)
                        foliageComponent.Render(this);
                }
            }

            End();
        }

        internal override void Cleanup()
        {
            base.Cleanup();
        }
    }
}

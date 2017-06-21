using System.Diagnostics;
using SharpDX.Direct3D;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Stats;
using VRageMath;
using VRageRender.Utils;

namespace VRageRender
{
    internal sealed class MyFoliageRenderingPass : MyRenderingPass
    {
        private const string FoliageRenderShader = "Geometry/Foliage/Foliage.hlsl";

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

            RC.SetPrimitiveTopology(PrimitiveTopology.PointList);
            RC.SetInputLayout(m_inputLayout);

            RC.AllShaderStages.SetConstantBuffer(MyCommon.FOLIAGE_SLOT, MyCommon.MaterialFoliageTableConstants);

            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);

            RC.VertexShader.Set(m_VS);
            RC.SetInputLayout(m_inputLayout);

            RC.SetRtvs(MyGBuffer.Main, MyDepthStencilAccess.ReadWrite);

            RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
        }

        internal override void End()
        {
            CleanupPipeline();
            base.End();

            RC.EndProfilingBlock();
        }

        internal void CleanupPipeline()
        {
            RC.GeometryShader.Set(null);
            RC.SetRasterizerState(null);
        }

        internal unsafe void RecordCommands(MyRenderableProxy proxy, IVertexBuffer stream, int voxelMatId)
        {
            if (stream == null) return;

            var foliageType = MyVoxelMaterials1.Table[voxelMatId].FoliageType;

            MyMapping mapping = MyMapping.MapDiscard(RC, proxy.ObjectBuffer);
            mapping.WriteAndPosition(ref proxy.NonVoxelObjectData);
            mapping.WriteAndPosition(ref proxy.CommonObjectData);
            mapping.Unmap();

            RC.AllShaderStages.SetConstantBuffer(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            RC.GeometryShader.Set(m_GS[foliageType]);
            RC.PixelShader.Set(m_PS[foliageType]);

            if (MyVoxelMaterials1.Table[voxelMatId].FoliageColorTextureArray != null)
            {
                RC.AllShaderStages.SetSrv(0, MyVoxelMaterials1.Table[voxelMatId].FoliageColorTextureArray);
                RC.AllShaderStages.SetSrv(1, MyVoxelMaterials1.Table[voxelMatId].FoliageNormalTextureArray);
            }
            else
            {
                MyFileTextureManager texManager = MyManagers.FileTextures;
                RC.AllShaderStages.SetSrv(0, texManager.GetTexture(MyVoxelMaterials1.Table[voxelMatId].FoliageArray_Texture, MyFileTextureEnum.COLOR_METAL, true));
                RC.AllShaderStages.SetSrv(1, texManager.GetTexture(MyVoxelMaterials1.Table[voxelMatId].FoliageArray_NormalTexture, MyFileTextureEnum.NORMALMAP_GLOSS, true));
            }

            RC.SetVertexBuffer(0, stream);
            RC.DrawAuto();
        }

        internal void Render()
        {
            var foliageComponents = MyFoliageComponents.ActiveComponents;
            if (foliageComponents.Count <= 0)
                return;

            ViewProjection = MyRender11.Environment.Matrices.ViewProjectionAt0;
            Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);

            PerFrame();
            Begin();

            var viewFrustum = new BoundingFrustumD(MyRender11.Environment.Matrices.ViewProjectionD);

            foreach (var foliageComponent in foliageComponents)
            {
                var renderableComponent = foliageComponent.Owner.GetRenderable();
                bool removeDitheringInProgress = renderableComponent.m_objectDithering > 0 && renderableComponent.m_objectDithering < 2;
                if (!removeDitheringInProgress && foliageComponent.Owner.CalculateCameraDistance() < MyRender11.Settings.User.FoliageDetails.GrassDrawDistance())
                {
                    if (viewFrustum.Contains(foliageComponent.Owner.Aabb) != ContainmentType.Disjoint)
                    {
                        foliageComponent.Render(this);
                    }
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

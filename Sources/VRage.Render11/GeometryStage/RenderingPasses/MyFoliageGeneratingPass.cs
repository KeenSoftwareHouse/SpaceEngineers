using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRender
{
    class MyFoliageGeneratingPass : MyRenderingPass
    {
        GeometryShaderId m_geometryShader = GeometryShaderId.NULL;

        internal unsafe MyFoliageGeneratingPass()
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

            if(m_geometryShader == GeometryShaderId.NULL)
                m_geometryShader = MyShaders.CreateGs("passes/foliage_streaming/geometry_stage.hlsl", null, new MyShaderStreamOutputInfo { Elements = soElements, Strides = soStrides, RasterizerStreams = GeometryShader.StreamOutputNoRasterizedStream });

            SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            base.Begin();

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetGS(m_geometryShader);
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
            if (stream.m_stream == VertexBufferId.NULL) return;

            //var worldMatrix = proxy.WorldMatrix;
            //worldMatrix.Translation = Vector3D.Zero;
            //MyObjectData objectData = proxy.ObjectData;
            //objectData.LocalMatrix = Matrix.Identity;

            var worldMat = proxy.WorldMatrix;
            //worldMat.Translation -= MyEnvironment.CameraPosition;

            MyObjectDataCommon objectData = proxy.CommonObjectData;
            objectData.LocalMatrix = worldMat;

            MyMapping mapping = MyMapping.MapDiscard(RC.DeviceContext, proxy.ObjectBuffer);
            mapping.WriteAndPosition(ref proxy.VoxelCommonObjectData);
            mapping.WriteAndPosition(ref objectData);
            mapping.Unmap();

            RC.SetCB(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            BindProxyGeometry(proxy, RC);

            RC.SetVS(vertexShader);
            RC.SetIL(inputLayout);

            int offset = -1;
            if (!stream.Append)
            {
                offset = 0;
                stream.Append = true;
            }

            Context.StreamOutput.SetTarget(stream.m_stream.Buffer, offset);
            RC.SetCB(MyCommon.FOLIAGE_SLOT, MyCommon.FoliageConstants);

            float densityFactor = MyVoxelMaterials1.Table[voxelMatId].FoliageDensity * MyRender11.Settings.GrassDensityFactor;

            mapping = MyMapping.MapDiscard(Context, MyCommon.FoliageConstants);
            mapping.WriteAndPosition(ref densityFactor);
            mapping.WriteAndPosition(ref materialIndex);
            mapping.WriteAndPosition(ref voxelMatId);
            mapping.Unmap();

            Context.DrawIndexed(indexCount, startIndex, baseVertex);
        }

        internal override void Cleanup()
        {
            base.Cleanup();
        }
    }
}

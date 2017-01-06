using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Render11.Resources;
using VRageMath;
using System.IO;

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
            {
                string stageFile = Path.Combine(MyMaterialShaders.PassesFolder, MyMaterialShaders.FOLIAGE_STREAMING_PASS, "GeometryStage.hlsl");
                m_geometryShader = MyShaders.CreateGs(stageFile, null, new MyShaderStreamOutputInfo { Elements = soElements, Strides = soStrides, RasterizerStreams = GeometryShader.StreamOutputNoRasterizedStream });
            }

            SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            base.Begin();

            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.GeometryShader.Set(m_geometryShader);
            RC.PixelShader.Set(null);
            RC.SetRtv(null);
        }

        internal sealed override void End()
        {
            CleanupPipeline();
            base.End();
        }

        internal void CleanupPipeline()
        {
            RC.GeometryShader.Set(null);
        }

        internal unsafe void RecordCommands(MyRenderableProxy proxy, MyFoliageStream stream, int voxelMatId,
            VertexShader vertexShader, InputLayout inputLayout,
            int materialIndex, int indexCount, int startIndex, int baseVertex)
        {
            if (stream.m_stream == null) return;

            //var worldMatrix = proxy.WorldMatrix;
            //worldMatrix.Translation = Vector3D.Zero;
            //MyObjectData objectData = proxy.ObjectData;
            //objectData.LocalMatrix = Matrix.Identity;

            var worldMat = proxy.WorldMatrix;
            //worldMat.Translation -= MyRender11.Environment.CameraPosition;

            MyObjectDataCommon objectData = proxy.CommonObjectData;
            objectData.LocalMatrix = worldMat;

            MyMapping mapping = MyMapping.MapDiscard(RC, proxy.ObjectBuffer);
            mapping.WriteAndPosition(ref proxy.VoxelCommonObjectData);
            mapping.WriteAndPosition(ref objectData);
            mapping.Unmap();

            RC.AllShaderStages.SetConstantBuffer(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            BindProxyGeometry(proxy, RC);

            RC.VertexShader.Set(vertexShader);
            RC.SetInputLayout(inputLayout);

            int offset = -1;
            if (!stream.Append)
            {
                offset = 0;
                stream.Append = true;
            }

            RC.SetTarget(stream.m_stream, offset);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.FOLIAGE_SLOT, MyCommon.FoliageConstants);

            float densityFactor = MyVoxelMaterials1.Table[voxelMatId].FoliageDensity * MyRender11.Settings.User.GrassDensityFactor;

            float zero = 0;
            mapping = MyMapping.MapDiscard(RC, MyCommon.FoliageConstants);
            mapping.WriteAndPosition(ref densityFactor);
            mapping.WriteAndPosition(ref materialIndex);
            mapping.WriteAndPosition(ref voxelMatId);
            mapping.WriteAndPosition(ref zero);
            mapping.Unmap();

            RC.DrawIndexed(indexCount, startIndex, baseVertex);
        }

        internal override void Cleanup()
        {
            base.Cleanup();
        }
    }
}

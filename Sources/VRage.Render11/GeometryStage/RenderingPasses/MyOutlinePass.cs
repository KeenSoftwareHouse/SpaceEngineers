using SharpDX.Direct3D11;
using System;

using Matrix = VRageMath.Matrix;
using Vector4 = VRageMath.Vector4;

namespace VRageRender
{
    struct OutlineConstantsLayout
    {
        internal Matrix WorldToVolume;
        internal Vector4 Color;
    };

    class MyOutlinePass : MyRenderingPass
    {
        internal static MyOutlinePass Instance = new MyOutlinePass();

        internal DepthStencilView DSV;
        internal RenderTargetView RTV;

        internal MyOutlinePass()
        {
            SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("highlight pass");

            base.Begin();

            //Context.OutputMerger.SetTargets(DSV, RTV);

            RC.SetDS(MyDepthStencilState.OutlineMesh, 0xFF);
            //RC.SetDS(null);
            RC.SetBS(null);

            Context.PixelShader.SetConstantBuffer(4, MyCommon.OutlineConstants);
        }

        internal unsafe override sealed void RecordCommands(MyRenderableProxy proxy)
        {
			if (proxy.Mesh.Buffers.IB == IndexBufferId.NULL || proxy.DrawSubmesh.IndexCount == 0 || proxy.SkipIfTooSmall())
            {
                return;
            }

            Stats.Meshes++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy);

            //Debug.Assert(proxy.ForwardShaders.VS != null);

            //RC.BindShaders(proxy.);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(null);

            Stats.Submeshes++;
            var submesh = proxy.DrawSubmesh;

            if (submesh.MaterialId != Locals.matTexturesID)
            {
                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                RC.MoveConstants(ref material.MaterialConstants);
                RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                RC.SetSRVs(ref material.MaterialSRVs);
            }

            if (proxy.SkinningMatrices != null)
            {
                Stats.ObjectConstantsChanges++;

                MyObjectData objectData = proxy.ObjectData;
                //objectData.Translate(-MyEnvironment.CameraPosition);

                MyMapping mapping;
                mapping = MyMapping.MapDiscard(RC.Context, proxy.ObjectBuffer);
                void* ptr = &objectData;
                mapping.stream.Write(new IntPtr(ptr), 0, sizeof(MyObjectData));

                if (proxy.SkinningMatrices != null)
                {
                    if (submesh.BonesMapping == null)
                    {
                        for (int j = 0; j < Math.Min(MyRender11Constants.SHADER_MAX_BONES, proxy.SkinningMatrices.Length); j++)
                            mapping.stream.Write(Matrix.Transpose(proxy.SkinningMatrices[j]));
                    }
                    else
                    {
                        for (int j = 0; j < submesh.BonesMapping.Length; j++)
                        {
                            mapping.stream.Write(Matrix.Transpose(proxy.SkinningMatrices[submesh.BonesMapping[j]]));
                        }
                    }
                }

                mapping.Unmap();
            }

            if (proxy.InstanceCount == 0 && submesh.IndexCount > 0)
            {
                RC.Context.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                RC.Stats.DrawIndexed++;
                Stats.Instances++;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else if (submesh.IndexCount > 0)
            {
                RC.Context.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                RC.Stats.DrawIndexedInstanced++;
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        internal override void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
            
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }
    }
}

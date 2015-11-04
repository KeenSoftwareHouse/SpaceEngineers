using SharpDX.Direct3D11;
using System;
using System.Diagnostics;

using Matrix = VRageMath.Matrix;

namespace VRageRender
{
    class MyForwardPass : MyRenderingPass
    {
        internal DepthStencilView DSV;
        internal RenderTargetView RTV;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("forward pass");

            base.Begin();

            Context.OutputMerger.SetTargets(DSV, RTV);

            RC.SetCB(4, MyShadows.m_csmConstants);
            RC.Context.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);
            RC.Context.PixelShader.SetShaderResource(60, MyShadows.m_cascadeShadowmapBackup.ShaderView);

            RC.SetDS(null);
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

            Debug.Assert(proxy.ForwardShaders.VS != null);

            RC.BindShaders(proxy.ForwardShaders);

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
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.ForwardShaders.VS != null);

            RC.BindShaders(proxy.ForwardShaders);

            for (int i = 0; i < proxy.Submeshes.Length; i++)
            {
                var submesh = proxy.Submeshes[i];
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                RC.MoveConstants(ref material.MaterialConstants);
                RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                RC.SetSRVs(ref material.MaterialSRVs);

                if (proxy.InstanceCount == 0)
                {
                    switch (submesh.DrawCommand)
                    {
                        case MyDrawCommandEnum.DrawIndexed:
                            RC.Context.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.Context.Draw(submesh.Count, submesh.Start);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (submesh.DrawCommand)
                    {
                        case MyDrawCommandEnum.DrawIndexed:
                            RC.Context.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.Context.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            break;
                        default:
                            break;
                    }
                }
            }

            base.RecordCommands(ref proxy);
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }
    }
}

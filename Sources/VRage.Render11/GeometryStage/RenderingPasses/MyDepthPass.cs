using SharpDX.Direct3D11;
using System;
using System.Diagnostics;

using Matrix = VRageMath.Matrix;

namespace VRageRender
{
    class MyDepthPass : MyRenderingPass
    {
        internal DepthStencilView DSV;
        internal RasterizerState DefaultRasterizer;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("depth pass " + DebugName);

            base.Begin();

            RC.SetRS(DefaultRasterizer);
            
            Context.OutputMerger.SetTargets(DSV);

            RC.SetPS(null);
            RC.SetDS(null);
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
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

            Debug.Assert(proxy.DepthShaders.VS != null);

            RC.BindShaders(proxy.DepthShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(DefaultRasterizer);


            //for (int i = 0; i < proxy.depthOnlySubmeshes.Length; i++)
            //{
            //    Stats.Submeshes++;

            //    var submesh = proxy.depthOnlySubmeshes[i];

            var submesh = proxy.DrawSubmesh;

                if (submesh.MaterialId != Locals.matTexturesID && (!((proxy.Flags & MyRenderableProxyFlags.DepthSkipTextures) > 0)))
                {
                    Stats.MaterialConstantsChanges++;

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
                            { 
                                mapping.stream.Write(Matrix.Transpose(proxy.SkinningMatrices[j]));
                            }
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

                if (proxy.InstanceCount == 0) {
                    RC.Context.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                    RC.Stats.DrawIndexed++;
                    Stats.Instances++;
                    Stats.Triangles += submesh.IndexCount / 3;
                }
                else { 
                    RC.Context.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                    RC.Stats.DrawIndexedInstanced++;
                    Stats.Instances += proxy.InstanceCount;
                    Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
                }
            //}
        }

        internal override void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.DepthShaders.VS != null);

            RC.SetRS(DefaultRasterizer);

            RC.BindShaders(proxy.DepthShaders);

            for (int i = 0; i < proxy.SubmeshesDepthOnly.Length; i++)
            {
                var submesh = proxy.SubmeshesDepthOnly[i];
                //RC.SetSRVs(ref proxy.Submeshes[i].MaterialProxy.MaterialSRVs);

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
    }
}

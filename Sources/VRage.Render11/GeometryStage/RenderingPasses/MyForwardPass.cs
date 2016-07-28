using SharpDX.Direct3D11;
using System.Diagnostics;

namespace VRageRender
{
    [PooledObject]
#if XB1
    class MyForwardPass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyForwardPass : MyRenderingPass
#endif // !XB1
    {
        internal DepthStencilView DSV;
        internal RenderTargetView RTV;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("forward pass");

            base.Begin();

            Context.OutputMerger.SetTargets(DSV, RTV);

            RC.SetCB(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.DeviceContext.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, SamplerStates.m_shadowmap);
            RC.DeviceContext.PixelShader.SetShaderResource(60, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapBackup.SRV);

            RC.SetDS(null);
        }

        protected unsafe override sealed void RecordCommandsInternal(MyRenderableProxy proxy)
        {
			if ((proxy.Mesh.Buffers.IB == IndexBufferId.NULL && proxy.MergedMesh.Buffers.IB == IndexBufferId.NULL) ||
                proxy.DrawSubmesh.IndexCount == 0 ||
                (proxy.DrawSubmesh.Flags & MyDrawSubmesh.MySubmeshFlags.Forward) == 0)
            {
                return;
            }

            ++Stats.Meshes;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            Debug.Assert(proxy.ForwardShaders.VS != null);

            RC.BindShaders(proxy.ForwardShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(null);

            ++Stats.Submeshes;
            var submesh = proxy.DrawSubmesh;

            if (submesh.MaterialId != Locals.matTexturesID)
            {
                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                RC.MoveConstants(ref material.MaterialConstants);
                RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                RC.SetSRVs(ref material.MaterialSRVs);
            }

            if (proxy.InstanceCount == 0 && submesh.IndexCount > 0)
            {
                RC.DeviceContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                RC.Stats.DrawIndexed++;
                Stats.Instances++;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else if (submesh.IndexCount > 0)
            {
                //MyRender11.AddDebugQueueMessage("ForwardPass DrawIndexedInstanced " + proxy.Material.ToString());
                RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                RC.Stats.DrawIndexedInstanced++;
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.ForwardShaders.MultiInstance.VS != null);

            RC.BindShaders(proxy.ForwardShaders.MultiInstance);

            SetProxyConstants(ref proxy);

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
                            RC.DeviceContext.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.DeviceContext.Draw(submesh.Count, submesh.Start);
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
                            //MyRender11.AddDebugQueueMessage("ForwardPass DrawIndexedInstanced " + proxy.VertexData.VB[0].DebugName);
                            RC.DeviceContext.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.DeviceContext.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }

#if XB1
        public void ObjectCleaner()
        {
            Cleanup();
        }
#else // !XB1
        [PooledObjectCleaner]
        public static void Cleanup(MyForwardPass pass)
        {
            pass.Cleanup();
        }
#endif // !XB1

        internal override void Cleanup()
        {
            base.Cleanup();

            DSV = null;
            RTV = null;
        }

        internal override MyRenderingPass Fork()
        {
            var renderPass = base.Fork() as MyForwardPass;

            renderPass.DSV = DSV;
            renderPass.RTV = RTV;

            return renderPass;
        }
    }
}

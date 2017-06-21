using System.Diagnostics;
using VRage.Render11.Resources;

namespace VRageRender
{
    [PooledObject]
#if XB1
    class MyForwardPass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyForwardPass : MyRenderingPass
#endif // !XB1
    {
        internal IDsvBindable Dsv;
        internal IRtvBindable Rtv;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("forward pass");

            base.Begin();

            RC.SetRtv(Dsv, Rtv);

            RC.AllShaderStages.SetConstantBuffer(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);
            RC.PixelShader.SetSrv(31, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapBackup);

            RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestWrite);
        }

        protected sealed override unsafe void RecordCommandsInternal(MyRenderableProxy proxy)
        {
            if (proxy.Mesh.Buffers.IB == null ||
                proxy.DrawSubmesh.IndexCount == 0 ||
                (proxy.DrawSubmesh.Flags & MyDrawSubmesh.MySubmeshFlags.Forward) == 0)
            {
                return;
            }

            ++Stats.Draws;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            Debug.Assert(proxy.ForwardShaders.VS != null);

            MyRenderUtils.BindShaderBundle(RC, proxy.ForwardShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            else
                RC.SetRasterizerState(null);

            var submesh = proxy.DrawSubmesh;

            if (submesh.MaterialId != Locals.matTexturesID)
            {
                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                MyRenderUtils.MoveConstants(RC, ref material.MaterialConstants);
                MyRenderUtils.SetConstants(RC, ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                MyRenderUtils.SetSrvs(RC, ref material.MaterialSrvs);
            }

            if (proxy.InstanceCount == 0 && submesh.IndexCount > 0)
            {
                RC.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                Stats.Instances++;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else if (submesh.IndexCount > 0)
            {
                //MyRender11.AddDebugQueueMessage("ForwardPass DrawIndexedInstanced " + proxy.Material.ToString());
                RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            MyRenderUtils.SetSrvs(RC, ref proxy.ObjectSrvs);

            Debug.Assert(proxy.ForwardShaders.MultiInstance.VS != null);

            MyRenderUtils.BindShaderBundle(RC, proxy.ForwardShaders.MultiInstance);

            SetProxyConstants(ref proxy);

            for (int i = 0; i < proxy.Submeshes.Length; i++)
            {
                var submesh = proxy.Submeshes[i];
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                MyRenderUtils.MoveConstants(RC, ref material.MaterialConstants);
                MyRenderUtils.SetConstants(RC, ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                MyRenderUtils.SetSrvs(RC, ref material.MaterialSrvs);

                if (proxy.InstanceCount == 0)
                {
                    switch (submesh.DrawCommand)
                    {
                        case MyDrawCommandEnum.DrawIndexed:
                            RC.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.Draw(submesh.Count, submesh.Start);
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
                            RC.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
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

        protected override MyFrustumEnum FrustumType
        {
            get { return MyFrustumEnum.EnvironmentProbe; }
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

            Dsv = null;
            Rtv = null;
        }

        internal override MyRenderingPass Fork()
        {
            var renderPass = base.Fork() as MyForwardPass;

            renderPass.Dsv = Dsv;
            renderPass.Rtv = Rtv;

            return renderPass;
        }
    }
}

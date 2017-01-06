using SharpDX.Direct3D11;
using System.Diagnostics;
using VRage.Render11.Resources;
using VRage.Render11.Tools;

namespace VRageRender
{
    [PooledObject]
#if XB1
    class MyDepthPass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyDepthPass : MyRenderingPass
#endif // !XB1
    {
        internal IDsvBindable Dsv;
        internal IRasterizerState DefaultRasterizer;
        internal bool IsCascade;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("MyDepthPass");

            base.Begin();

            RC.SetRasterizerState(DefaultRasterizer);
            
            // Only write depth
            RC.SetRtv(Dsv, null);

            RC.PixelShader.Set(null);
            RC.SetDepthStencilState(null);
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }

        private bool IsProxyValidForDraw(MyRenderableProxy proxy)
        {
            return proxy.DepthShaders != MyMaterialShadersBundleId.NULL && proxy.DrawSubmesh.BaseVertex >= 0 && proxy.DrawSubmesh.StartIndex >= 0 &&
                proxy.DrawSubmesh.IndexCount > 0;
        }

        protected sealed override void RecordCommandsInternal(MyRenderableProxy proxy)
        {
			if (proxy.Mesh.Buffers == MyMeshBuffers.Empty)
            { 
                return;
            }

            if (!IsProxyValidForDraw(proxy))
                return;

            Stats.Draws++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            Debug.Assert(proxy.DepthShaders.VS != null);

            MyRenderUtils.BindShaderBundle(RC, proxy.DepthShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            else
                RC.SetRasterizerState(DefaultRasterizer);

            var submesh = proxy.DrawSubmesh;
            if (submesh.MaterialId != Locals.matTexturesID && (!((proxy.Flags & MyRenderableProxyFlags.DepthSkipTextures) > 0)))
            {
                Stats.MaterialConstantsChanges++;

                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                MyRenderUtils.MoveConstants(RC, ref material.MaterialConstants);
                MyRenderUtils.SetConstants(RC, ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                MyRenderUtils.SetSrvs(RC, ref material.MaterialSrvs);
            }

            if (proxy.InstanceCount == 0) 
            {
                RC.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++Stats.Instances;
                Stats.Triangles += submesh.IndexCount / 3;
                ++MyStatsUpdater.Passes.DrawShadows;
            }
            else
            {
                //MyRender11.AddDebugQueueMessage("DepthPass DrawIndexedInstanced " + proxy.Material.ToString());
                RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
                MyStatsUpdater.Passes.DrawShadows++;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            MyRenderUtils.SetSrvs(RC, ref proxy.ObjectSrvs);

            Debug.Assert(proxy.DepthShaders.MultiInstance.VS != null);

            RC.SetRasterizerState(DefaultRasterizer);

            MyRenderUtils.BindShaderBundle(RC, proxy.DepthShaders.MultiInstance); 

            SetProxyConstants(ref proxy);

            for (int i = 0; i < proxy.SubmeshesDepthOnly.Length; i++)
            {
                var submesh = proxy.SubmeshesDepthOnly[i];

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
                            //MyRender11.AddDebugQueueMessage("DepthPass DrawIndexedInstanced " + proxy.VertexData.VB[0].DebugName);
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

#if XB1
        public void ObjectCleaner()
        {
            Cleanup();
        }
#else // !XB1
        [PooledObjectCleaner]
        public static void Cleanup(MyDepthPass renderPass)
        {
            renderPass.Cleanup();
        }
#endif // !XB1

        internal override void Cleanup()
        {
            base.Cleanup();

            Dsv = null;
            DefaultRasterizer = null;
            IsCascade = false;
        }

        internal override MyRenderingPass Fork()
        {
            var renderPass = base.Fork() as MyDepthPass;

            renderPass.Dsv = Dsv;
            renderPass.DefaultRasterizer = DefaultRasterizer;
            renderPass.IsCascade = IsCascade;

            return renderPass;
        }

        protected override MyFrustumEnum FrustumType
        {
            get { return IsCascade ? MyFrustumEnum.ShadowCascade : MyFrustumEnum.ShadowProjection; }
        }
    }
}

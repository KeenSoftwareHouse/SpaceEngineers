using SharpDX.Direct3D11;
using System.Diagnostics;

namespace VRageRender
{
    [PooledObject]
    class MyDepthPass : MyRenderingPass
    {
        internal DepthStencilView DSV;
        internal RasterizerState DefaultRasterizer;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("MyDepthPass");

            base.Begin();

            RC.SetRS(DefaultRasterizer);
            
            // Only write depth
            Context.OutputMerger.SetTargets(DSV, (RenderTargetView)null);

            RC.SetPS(null);
            RC.SetDS(null);
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

        protected sealed override void RecordCommandsInternal(MyRenderableProxy proxy, int section)
        {
			if ((proxy.Mesh.Buffers == MyMeshBuffers.Empty && proxy.MergedMesh.Buffers == MyMeshBuffers.Empty))
            { 
                return;
            }

            if (!IsProxyValidForDraw(proxy))
                return;

            Stats.Meshes++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            Debug.Assert(proxy.DepthShaders.VS != null);

            RC.BindShaders(proxy.DepthShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(DefaultRasterizer);

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

            if (proxy.InstanceCount == 0) 
            {
                RC.DeviceContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++Stats.Instances;
                ++RC.Stats.ShadowDrawIndexed;
            }
            else
            {
                RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                ++RC.Stats.ShadowDrawIndexedInstanced;
            }
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

            base.RecordCommands(ref proxy);
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyDepthPass renderPass)
        {
            renderPass.Cleanup();
        }

        internal override void Cleanup()
        {
            base.Cleanup();

            DSV = null;
            DefaultRasterizer = null;
        }

        internal override MyRenderingPass Fork()
        {
            var renderPass = base.Fork() as MyDepthPass;

            renderPass.DSV = DSV;
            renderPass.DefaultRasterizer = DefaultRasterizer;

            return renderPass;
        }
    }
}

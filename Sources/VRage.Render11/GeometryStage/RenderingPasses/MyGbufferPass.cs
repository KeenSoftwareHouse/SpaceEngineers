using System.Diagnostics;
using Matrix = VRageMath.Matrix;
using Vector4 = VRageMath.Vector4;

namespace VRageRender
{
    [PooledObject]
#if XB1
    class MyGBufferPass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyGBufferPass : MyRenderingPass
#endif // !XB1
    {
        internal MyGBuffer GBuffer;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("GBuffer pass");

            base.Begin();

            RC.BindGBufferForWrite(GBuffer);

            if ((!MyStereoRender.Enable) && (!MyStereoRender.EnableUsingStencilMask))
                RC.SetDS(MyDepthStencilState.DepthTestWrite);
            else
                RC.SetDS(MyDepthStencilState.StereoDepthTestWrite);
        }

        protected unsafe override sealed void RecordCommandsInternal(MyRenderableProxy proxy)
        {
            if ((proxy.Mesh.Buffers.IB == IndexBufferId.NULL && proxy.MergedMesh.Buffers.IB == IndexBufferId.NULL)
                || proxy.DrawSubmesh.IndexCount == 0
                || proxy.Flags.HasFlags(MyRenderableProxyFlags.SkipInMainView))
                return;

            ++Stats.Meshes;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            Debug.Assert(proxy.Shaders.VS != null);
            RC.BindShaders(proxy.Shaders);

//#if DEBUG
            if (MyRender11.Settings.Wireframe)
            {
                if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                    RC.SetRS(MyRender11.m_nocullWireframeRasterizerState);
                else
                    RC.SetRS(MyRender11.m_wireframeRasterizerState);
            }
            else
            {
                if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                    RC.SetRS(MyRender11.m_nocullRasterizerState);
                else
                    RC.SetRS(null);
            }
//#endif
            ++Stats.Submeshes;
            var submesh = proxy.DrawSubmesh;
            if (submesh.MaterialId != Locals.matTexturesID)
            {
                ++Stats.MaterialConstantsChanges;

                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                RC.MoveConstants(ref material.MaterialConstants);
                RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                RC.SetSRVs(ref material.MaterialSRVs);
            }

            if (proxy.InstanceCount == 0) 
            {
                if (!MyStereoRender.Enable)
                    RC.DeviceContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                else
                    MyStereoRender.DrawIndexedGBufferPass(RC, submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++RC.Stats.DrawIndexed;
                ++Stats.Instances;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else
            {
                //MyRender11.AddDebugQueueMessage("GbufferPass DrawIndexedInstanced " + proxy.Material.ToString());
                if (!MyStereoRender.Enable)
                    RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                else
                    MyStereoRender.DrawIndexedInstancedGBufferPass(RC, submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                ++RC.Stats.DrawIndexedInstanced;
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.Shaders.MultiInstance.VS != null);

            RC.BindShaders(proxy.Shaders.MultiInstance);

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
                            if (!MyStereoRender.Enable)
                                RC.DeviceContext.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            else
                                MyStereoRender.DrawIndexedGBufferPass(RC, submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            if (!MyStereoRender.Enable)
                                RC.DeviceContext.Draw(submesh.Count, submesh.Start);
                            else
                                MyStereoRender.DrawGBufferPass(RC, submesh.Count, submesh.Start);
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
                            if (!MyStereoRender.Enable)
                                RC.DeviceContext.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            else
                                MyStereoRender.DrawIndexedInstancedGBufferPass(RC, submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            if (!MyStereoRender.Enable)
                                RC.DeviceContext.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            else
                                MyStereoRender.DrawInstancedGBufferPass(RC, submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
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
        public static void Cleanup(MyGBufferPass renderPass)
        {
            renderPass.Cleanup();
        }
#endif // !XB1

        internal override void Cleanup()
        {
            base.Cleanup();
            GBuffer = null;
        }

        internal override MyRenderingPass Fork()
        {
            var renderPass = base.Fork() as MyGBufferPass;

            renderPass.GBuffer = GBuffer;

            return renderPass;
        }
    }
}

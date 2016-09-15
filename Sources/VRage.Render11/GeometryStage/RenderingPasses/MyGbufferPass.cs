using System.Diagnostics;
using VRage.Import;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender.Import;
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

            RC.SetRtvs(GBuffer, MyDepthStencilAccess.ReadWrite);
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
            MyRenderUtils.BindShaderBundle(RC, proxy.Shaders);

            if (MyRender11.Settings.Wireframe)
            {
                SetDepthStencilView(false);
                RC.SetBlendState(null);
                if (proxy.Flags.HasFlags(MyRenderableProxyFlags.DisableFaceCulling))
                    RC.SetRasterizerState(MyRasterizerStateManager.NocullWireframeRasterizerState);
                else
                    RC.SetRasterizerState(MyRasterizerStateManager.WireframeRasterizerState);
            }
            else
            {
                MyMeshDrawTechnique technique = MyMeshDrawTechnique.MESH;
                if (proxy.Material != MyMeshMaterialId.NULL)
                    technique = proxy.Material.Info.Technique;

                if (proxy.Flags.HasFlags(MyRenderableProxyFlags.DisableFaceCulling))
                {
                    switch (technique)
                    {
                        case MyMeshDrawTechnique.DECAL_CUTOUT:
                        case MyMeshDrawTechnique.DECAL:
                            SetDepthStencilView(true);
                            MyMeshMaterials1.BindMaterialTextureBlendStates(RC, proxy.Material.Info.TextureTypes);
                            RC.SetRasterizerState(MyRasterizerStateManager.NocullDecalRasterizerState);
                            break;
                        default:
                            SetDepthStencilView(false);
                            RC.SetBlendState(null);
                            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
                            break;
                    }
                }
                else
                {
                    switch (technique)
                    {
                        case MyMeshDrawTechnique.DECAL:
                        case MyMeshDrawTechnique.DECAL_CUTOUT:
                            SetDepthStencilView(true);
                            MyMeshMaterials1.BindMaterialTextureBlendStates(RC, proxy.Material.Info.TextureTypes);
                            RC.SetRasterizerState(MyRasterizerStateManager.DecalRasterizerState);
                            break;
                        default:
                            SetDepthStencilView(false);
                            RC.SetBlendState(null);
                            RC.SetRasterizerState(null);
                            break;
                    }
                }
            }

            ++Stats.Submeshes;
            var submesh = proxy.DrawSubmesh;
            if (submesh.MaterialId != Locals.matTexturesID)
            {
                ++Stats.MaterialConstantsChanges;

                Locals.matTexturesID = submesh.MaterialId;
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                MyRenderUtils.MoveConstants(RC, ref material.MaterialConstants);
                MyRenderUtils.SetConstants(RC, ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                MyRenderUtils.SetSrvs(RC, ref material.MaterialSrvs);
            }

            if (proxy.InstanceCount == 0) 
            {
                if (!MyStereoRender.Enable)
                    RC.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                else
                    MyStereoRender.DrawIndexedGBufferPass(RC, submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++Stats.Instances;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else
            {
                //MyRender11.AddDebugQueueMessage("GbufferPass DrawIndexedInstanced " + proxy.Material.ToString());
                if (!MyStereoRender.Enable)
                    RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                else
                    MyStereoRender.DrawIndexedInstancedGBufferPass(RC, submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            MyRenderUtils.SetSrvs(RC, ref proxy.ObjectSrvs);

            Debug.Assert(proxy.Shaders.MultiInstance.VS != null);

            MyRenderUtils.BindShaderBundle(RC, proxy.Shaders.MultiInstance);

            SetDepthStencilView(false);

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
                            if (!MyStereoRender.Enable)
                                RC.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            else
                                MyStereoRender.DrawIndexedGBufferPass(RC, submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            if (!MyStereoRender.Enable)
                                RC.Draw(submesh.Count, submesh.Start);
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
                                RC.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            else
                                MyStereoRender.DrawIndexedInstancedGBufferPass(RC, submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            if (!MyStereoRender.Enable)
                                RC.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            else
                                MyStereoRender.DrawInstancedGBufferPass(RC, submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void SetDepthStencilView(bool readOnly)
        {
            if (readOnly)
            {
                if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                    RC.SetDepthStencilState(MyDepthStencilStateManager.StereoDepthTestReadOnly);
                else
                    RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestReadOnly);
            }
            else
            {
                if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                    RC.SetDepthStencilState(MyDepthStencilStateManager.StereoDepthTestWrite);
                else
                    RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestWrite);
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

using System.Diagnostics;
using Matrix = VRageMath.Matrix;
using Vector4 = VRageMath.Vector4;

namespace VRageRender
{
    struct MyMatrix4x3
    {
        internal Vector4 m_row0;
        internal Vector4 m_row1;
        internal Vector4 m_row2;

        internal Matrix Matrix4x4
        {
            get
            {
                var row0 = m_row0;
                var row1 = m_row1;
                var row2 = m_row2;

                return new Matrix(
                    row0.X, row1.X, row2.X, 0,
                    row0.Y, row1.Y, row2.Y, 0,
                    row0.Z, row1.Z, row2.Z, 0,
                    row0.W, row1.W, row2.W, 1);

            }
            set
            {
                m_row0 = new Vector4(value.M11, value.M21, value.M31, value.M41);
                m_row1 = new Vector4(value.M12, value.M22, value.M32, value.M42);
                m_row2 = new Vector4(value.M13, value.M23, value.M33, value.M43);
            }
        }
    }

    [PooledObject]
    class MyGBufferPass : MyRenderingPass
    {
        internal MyGBuffer GBuffer;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("GBuffer pass");

            base.Begin();

            RC.BindGBufferForWrite(GBuffer);

            RC.SetDS(MyDepthStencilState.DepthTestWrite);
        }

        protected unsafe override sealed void RecordCommandsInternal(MyRenderableProxy proxy, int section)
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
                RC.DeviceContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++RC.Stats.DrawIndexed;
                ++Stats.Instances;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else
            { 
                RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                ++RC.Stats.DrawIndexedInstanced;
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        internal override void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.Shaders.VS != null);

            RC.BindShaders(proxy.Shaders);

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

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyGBufferPass renderPass)
        {
            renderPass.Cleanup();
        }

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

using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRender
{
    struct OutlineConstantsLayout
    {
        internal Vector4 Color;
    };

    [PooledObject]
#if XB1
    class MyOutlinePass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyOutlinePass : MyRenderingPass
#endif // !XB1
    {
        internal static MyOutlinePass Instance = new MyOutlinePass();

        internal DepthStencilView DSV;
        internal RenderTargetView RTV;

        public MyOutlinePass()
        {
            SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("Highlight Pass");

            base.Begin();

            //Context.OutputMerger.SetTargets(DSV, RTV);

            RC.SetDS(MyDepthStencilState.OutlineMesh, 0xFF);
            //RC.SetDS(null);
            RC.SetBS(null);

            Context.PixelShader.SetConstantBuffer(4, MyCommon.OutlineConstants);
        }

        public void RecordCommands(MyRenderableProxy proxy, int sectionmesh, int inctanceId)
        {
			if ((proxy.Mesh.Buffers.IB == IndexBufferId.NULL && proxy.MergedMesh.Buffers.IB == IndexBufferId.NULL)
                || proxy.DrawSubmesh.IndexCount == 0)
            {
                return;
            }

            Stats.Meshes++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(null);

            Stats.Submeshes++;

            MyDrawSubmesh submesh;
            if (sectionmesh == -1)
                submesh = proxy.DrawSubmesh;
            else
                submesh = proxy.SectionSubmeshes[sectionmesh];

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
                if (inctanceId >= 0)
                    RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, 1, submesh.StartIndex, submesh.BaseVertex, inctanceId);
                else
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

            Stats.Meshes++;
            
            if (instanceIndex == -1)
            {
                RC.BindShaders(proxy.HighlightShaders.MultiInstance);
                for (int it = 0; it < proxy.Submeshes.Length; it++)
                {
                    MyDrawSubmesh_2 submesh = proxy.Submeshes[it];
                    DrawSubmesh(ref proxy, ref submesh, sectionIndex);
                }
            }
            else
            {
                RC.BindShaders(proxy.HighlightShaders.SingleInstance);
                MyDrawSubmesh_2 submesh;
                if (sectionIndex == -1)
                    submesh = proxy.Submeshes[instanceIndex];
                else
                    submesh = proxy.SectionSubmeshes[instanceIndex][sectionIndex];

                DrawSubmesh(ref proxy, ref submesh, instanceIndex);
            }
        }

        private void DrawSubmesh(ref MyRenderableProxy_2 proxy, ref MyDrawSubmesh_2 submesh, int instanceIndex)
        {
            var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
            RC.MoveConstants(ref material.MaterialConstants);
            RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
            RC.SetSRVs(ref material.MaterialSRVs);

            MyMergeInstancingConstants constants = new MyMergeInstancingConstants();
            constants.InstanceIndex = instanceIndex;
            constants.StartIndex = submesh.Start;
            SetProxyConstants(ref proxy, constants);

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

            Stats.Submeshes++;
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
        public static void Cleanup(MyOutlinePass renderPass)
        {
            renderPass.Cleanup();
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
            var renderPass = base.Fork() as MyOutlinePass;

            renderPass.DSV = DSV;
            renderPass.RTV = RTV;

            return renderPass;
        }
    }
}

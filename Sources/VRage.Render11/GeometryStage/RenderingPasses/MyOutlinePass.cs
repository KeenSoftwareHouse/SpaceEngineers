using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRender
{
    struct OutlineConstantsLayout
    {
        internal Vector4 Color;
    };

    [PooledObject]
    class MyOutlinePass : MyRenderingPass
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

        protected unsafe override sealed void RecordCommandsInternal(MyRenderableProxy proxy, int sectionmesh)
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
                RC.DeviceContext.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                RC.Stats.DrawIndexedInstanced++;
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        internal override void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
            
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyOutlinePass renderPass)
        {
            renderPass.Cleanup();
        }

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

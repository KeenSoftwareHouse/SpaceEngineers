using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    [StructLayout(LayoutKind.Sequential)]
    struct HighlightConstantsLayout
    {
        internal Vector4 Color;
    };

    [PooledObject]
#if XB1
    class MyHighlightPass : MyRenderingPass, IMyPooledObjectCleaner
#else // !XB1
    class MyHighlightPass : MyRenderingPass
#endif // !XB1
    {
        internal static MyHighlightPass Instance = new MyHighlightPass();

        public MyHighlightPass()
        {
            SetImmediate(true);
        }

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("Highlight Pass");

            base.Begin();

            RC.SetDepthStencilState(MyDepthStencilStateManager.WriteHighlightStencil, MyHighlight.HIGHLIGHT_STENCIL_MASK);
            RC.SetBlendState(null);

            RC.PixelShader.SetConstantBuffer(4, MyCommon.HighlightConstants);
        }

        public void RecordCommands(MyRenderableProxy proxy, int sectionmesh, int inctanceId)
        {
			if (proxy.Mesh.Buffers.IB == null || proxy.DrawSubmesh.IndexCount == 0)
            {
                return;
            }

            Stats.Draws++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            MyRenderUtils.BindShaderBundle(RC, proxy.HighlightShaders);

            if ((proxy.Flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            else
                RC.SetRasterizerState(null);

            MyDrawSubmesh submesh;
            if (sectionmesh == -1)
                submesh = proxy.DrawSubmesh;
            else
                submesh = proxy.SectionSubmeshes[sectionmesh];

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
                if (inctanceId >= 0)
                    RC.DrawIndexedInstanced(submesh.IndexCount, 1, submesh.StartIndex, submesh.BaseVertex, inctanceId);
                else
                    RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount * submesh.IndexCount / 3;
            }
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            MyRenderUtils.SetSrvs(RC, ref proxy.ObjectSrvs);

            Stats.Draws++;
            
            if (instanceIndex == -1)
            {
                MyRenderUtils.BindShaderBundle(RC, proxy.HighlightShaders.MultiInstance);
                for (int it = 0; it < proxy.Submeshes.Length; it++)
                {
                    MyDrawSubmesh_2 submesh = proxy.Submeshes[it];
                    DrawSubmesh(ref proxy, ref submesh, sectionIndex);
                }
            }
            else
            {
                MyRenderUtils.BindShaderBundle(RC, proxy.HighlightShaders.SingleInstance);
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
            MyRenderUtils.MoveConstants(RC, ref material.MaterialConstants);
            MyRenderUtils.SetConstants(RC, ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
            MyRenderUtils.SetSrvs(RC, ref material.MaterialSrvs);

            MyMergeInstancingConstants constants = new MyMergeInstancingConstants();
            constants.InstanceIndex = instanceIndex;
            constants.StartIndex = submesh.Start;
            SetProxyConstants(ref proxy, constants);

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
        public static void Cleanup(MyHighlightPass renderPass)
        {
            renderPass.Cleanup();
        }
#endif // !XB1
    }
}

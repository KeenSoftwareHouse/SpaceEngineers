using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    internal class MyStaticGlassPass : MyRenderingPass
    {
        internal static MyStaticGlassPass Instance = new MyStaticGlassPass();
        static PixelShaderId m_psDepthOnly;

        public MyStaticGlassPass()
        {
            SetImmediate(true);
            m_psDepthOnly = MyShaders.CreatePs("Transparent/GlassDepthOnly.hlsl"); 
        }

        public void BeginDepthOnly()
        {
            RC.BeginProfilingBlock("StaticGlassPass - Depth Only");

            base.Begin();

            // Read-write state
            if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                RC.SetDepthStencilState(MyDepthStencilStateManager.StereoDefaultDepthState);
            else
                RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);

            RC.SetBlendState(null);
            RC.SetRasterizerState(null);
        }

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("StaticGlassPass");

            // Read-only state
            if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                RC.SetDepthStencilState(MyDepthStencilStateManager.StereoDepthTestReadOnly);
            else
                RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestReadOnly);
            RC.SetRasterizerState(null);

            base.Begin();
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }

        protected unsafe sealed override void RecordCommandsInternal(MyRenderableProxy proxy)
        {
            if (proxy.Material.Info.GeometryTextureRef.IsUsed)
            {
                MyRenderProxy.Fail(String.Format("Ensure all glass materials for model '{0}' are dynamic materials inside Environment.sbc", proxy.Mesh.Info.Name));
                return;
            }

            MyTransparentMaterial material;
            if (!MyTransparentMaterials.TryGetMaterial(proxy.Material.Info.Name.String, out material))
            {
                MyRenderProxy.Fail(String.Format("Missing transparent material '{0}'", proxy.Material.Info.Name));
                return;
            }

            Stats.Draws++;

            MyRenderUtils.BindShaderBundle(RC, proxy.Shaders);

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            ISrvBindable texture = MyManagers.FileTextures.GetTexture(material.Texture, MyFileTextureEnum.GUI, true);
            RC.PixelShader.SetSrv(0, texture);
            
            StaticGlassConstants glassConstants = new StaticGlassConstants();

            var glassCB = MyCommon.GetObjectCB(sizeof(StaticGlassConstants));
            RC.PixelShader.SetConstantBuffer(2, glassCB);
            var mapping = MyMapping.MapDiscard(glassCB);

            glassConstants.Color = material.Color;
            glassConstants.Reflective = material.Reflectivity;
            mapping.WriteAndPosition(ref glassConstants);

            mapping.Unmap();

            var submesh = proxy.DrawSubmesh;

            if (proxy.InstanceCount == 0)
            {
                RC.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                ++Stats.Instances;
                Stats.Triangles += submesh.IndexCount / 3;
            }
            else
            {
                RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
                Stats.Instances += proxy.InstanceCount;
                Stats.Triangles += proxy.InstanceCount*submesh.IndexCount/3;
            }
        }

        public void RecordCommandsDepthOnly(MyRenderableProxy proxy)
        {
            Stats.Draws++;

            MyRenderUtils.BindShaderBundle(RC, proxy.Shaders);
            RC.PixelShader.Set(m_psDepthOnly);

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy, RC);

            var submesh = proxy.DrawSubmesh;

            if (proxy.InstanceCount == 0)
                RC.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
            else
                RC.DrawIndexedInstanced(submesh.IndexCount, proxy.InstanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.StartInstance);
        }

        protected override void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instanceIndex, int sectionIndex)
        {
            throw new Exception();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct StaticGlassConstants
    {
        public Vector4 Color;
        public float Reflective;
        Vector3 __Pad;
    }
}

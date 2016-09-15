using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    internal class MyHardwareOcclusionQuery
    {
        Query m_query;

        public MyHardwareOcclusionQuery()
        {
            QueryDescription queryDesc;
            queryDesc.Flags = QueryFlags.None;
            queryDesc.Type = QueryType.Occlusion;
            m_query = new Query(MyRender11.Device, queryDesc);
        }

        internal void IssueQuery(MyCullProxy cullProxy)
        {   
            // Test code, WIP
            var renderContext = MyRender11.RC;
            var renderableProxy = cullProxy.RenderableProxies[0];
            renderContext.Begin(m_query);

            MyRenderingPass.FillBuffers(renderableProxy, renderContext);
            MyRenderingPass.BindProxyGeometry(renderableProxy, renderContext);

            MyRenderUtils.BindShaderBundle(renderContext, renderableProxy.DepthShaders);

            var submesh = renderableProxy.DrawSubmesh;
            renderContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);

            renderContext.End(m_query);
        }
    }
}

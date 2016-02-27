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
            var renderContext = MyRenderContext.Immediate;
            var deviceContext = renderContext.DeviceContext;
            var renderableProxy = cullProxy.RenderableProxies[0];
            MyRender11.DeviceContext.Begin(m_query);

            MyRenderingPass.FillBuffers(renderableProxy, deviceContext);
            MyRenderingPass.BindProxyGeometry(renderableProxy, renderContext);
            renderContext.BindShaders(renderableProxy.DepthShaders);

            var submesh = renderableProxy.DrawSubmesh;
            deviceContext.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);

            MyRender11.DeviceContext.End(m_query);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    internal class MyStaticGlassRenderer : MyImmediateRC
    {
        public static List<MyRenderCullResultFlat> Renderables = new List<MyRenderCullResultFlat>();

        static List<ulong> m_sortedKeys = new List<ulong>();
        static Dictionary<ulong, List<MyRenderableProxy>> m_batches = new Dictionary<ulong, List<MyRenderableProxy>>();
        static List<Tuple<MyRenderCullResultFlat, double>> m_squaredDistances = new List<Tuple<MyRenderCullResultFlat, double>>();

        /// <param name="handleGlassDepth">Handler for depth pass. Returns true to use the result into in the depth path</param>
        internal static void Render(Func<MyRenderCullResultFlat, double, bool> handleGlassDepth)
        {
            if (Renderables.Count == 0)
            {
                m_squaredDistances.Clear();
                return;
            }

            m_squaredDistances.Clear();
            m_sortedKeys.Clear();
            foreach (var result in Renderables)
            {
                Vector3D pos = result.RenderProxy.WorldMatrix.Translation - MyRender11.Environment.Matrices.CameraPosition;
                double distance = pos.LengthSquared();
                if (handleGlassDepth(result, distance))
                    m_squaredDistances.Add(Tuple.Create(result, distance));

                List<MyRenderableProxy> proxies;
                if (!m_batches.TryGetValue(result.SortKey, out proxies))
                {
                    proxies = new List<MyRenderableProxy>();
                    m_batches.Add(result.SortKey, proxies);
                }
                proxies.Add(result.RenderProxy);
                m_sortedKeys.Add(result.SortKey);
            }

            m_sortedKeys.Sort();

            var pass = MyStaticGlassPass.Instance;
            pass.ViewProjection = MyRender11.Environment.Matrices.ViewProjectionAt0;
            pass.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            pass.Begin();
            foreach (var key in m_sortedKeys)
            {
                var proxies = m_batches[key];
                foreach (var proxy in proxies)
                    pass.RecordCommands(proxy);

                proxies.Clear();
            }
            pass.End();

            // Sort for depth only pass
            m_squaredDistances.Sort(GlassComparer);

            Renderables.Clear();
        }


        /// <summary>Render depth and normals of windows to the specified target</summary>
        /// <returns>True if glass to be rendered found</returns>
        /// <param name="squaredDistanceMin">Squared distance internal minor</param>
        internal static bool RenderGlassDepthOnly(IDepthStencil depthStencil, IRtvBindable gbuffer1,
            float squaredDistanceMin, float squaredDistanceMax)
        {
            int offset = 0;
            int windowsCount = 0;
            for (int it = 0; it < m_squaredDistances.Count; it++)
            {
                // Interval is [min, max)
                double squaredDistance = m_squaredDistances[it].Item2;
                if (squaredDistance >= squaredDistanceMax)
                    break;

                if (squaredDistance < squaredDistanceMin)
                    offset++;
                else
                    windowsCount++;
            }

            if (windowsCount == 0)
                return false;

            var pass = MyStaticGlassPass.Instance;
            pass.ViewProjection = MyRender11.Environment.Matrices.ViewProjectionAt0;
            pass.Viewport = new MyViewport(MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            pass.BeginDepthOnly();

            RC.SetRtv(depthStencil, MyDepthStencilAccess.ReadWrite, gbuffer1);

            for (int it = offset; it < windowsCount; it++)
            {
                var renderable = m_squaredDistances[it].Item1;
                pass.RecordCommandsDepthOnly(renderable.RenderProxy);
            }

            pass.End();

            return true;
        }

        static int GlassComparer(Tuple<MyRenderCullResultFlat, double> x, Tuple<MyRenderCullResultFlat, double> y)
        {
            double distX = x.Item2;
            double distY = y.Item2;
            if (distX > distY)
                return 1;
            else if (distX == distY)
                return 0;
            else
                return -1;
        }
    }
}

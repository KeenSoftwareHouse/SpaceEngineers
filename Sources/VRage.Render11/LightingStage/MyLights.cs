using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace VRage.Render11.LightingStage
{
    internal struct MyLightInfo
    {
        internal Vector3D SpotPosition;
        internal Vector3D PointPosition;
        internal Vector3 Direction;
        internal Vector3 Up;
        internal float ShadowsDistance;

        internal Vector3D LocalSpotPosition;
        internal Vector3D LocalPointPosition;
        internal Vector3 LocalDirection;
        internal Vector3 LocalUp;

        internal int ParentGID;
        internal FlareId FlareId;
        internal bool UsedInForward;
        internal bool CastsShadows;
        internal bool CastsShadowsThisFrame;
    }

    internal struct MyPointlightInfo
    {
        internal bool Enabled;
        public MyLightLayout Light;

        internal Vector3D LastBvhUpdatePosition;
        internal int BvhProxyId;
    }

    internal struct MySpotlightInfo
    {
        internal bool Enabled;
        public MySpotLightLayout Spotlight;

        internal Vector3D LastBvhUpdatePosition;
        internal int BvhProxyId;

        internal ISrvBindable ReflectorTexture;

        internal MatrixD ViewProjection;
        internal bool ViewProjectionDirty;
        internal Vector3 LastBvhUpdateUp;
        internal Vector3 LastBvhUpdateDir;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyPointlightConstants
    {
        public MyLightLayout Light;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SpotlightConstants
    {
        internal Matrix ProxyWorldViewProj;
        internal Matrix ShadowMatrix;

        public MySpotLightLayout Spotlight;
    }

    internal struct LightId
    {
        internal int Index;

        public static bool operator ==(LightId x, LightId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(LightId x, LightId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly LightId NULL = new LightId { Index = -1 };

        internal Vector3D SpotPosition { get { return MyLights.GetLights()[Index].SpotPosition; } }
        internal Vector3D PointPosition { get { return MyLights.GetLights()[Index].PointPosition; } }
        internal bool CastsShadows { get { return MyLights.GetLights()[Index].CastsShadows; } }
        internal bool CastsShadowsThisFrame { get { return MyLights.GetLights()[Index].CastsShadowsThisFrame; } }
        internal float ShadowDistance { get { return MyLights.GetLights()[Index].ShadowsDistance; } }
        internal float ViewerDistanceSquared { get { return (float)(PointPosition - MyRender11.Environment.Matrices.CameraPosition).LengthSquared(); } }
        internal int ParentGID { get { return MyLights.GetLights()[Index].ParentGID; } }
        internal FlareId FlareId { get { return MyLights.GetLights()[Index].FlareId; } }

        #region Equals
        public class MyLightIdComparerType : IEqualityComparer<LightId>
        {
            public bool Equals(LightId left, LightId right)
            {
                return left == right;
            }

            public int GetHashCode(LightId lightId)
            {
                return lightId.Index;
            }
        }
        public static readonly MyLightIdComparerType Comparer = new MyLightIdComparerType();
        #endregion
    }

    internal static class MyLights
    {
        private const float MOVE_TOLERANCE = 6;
        private const float DIR_TOLARENCE = 0.01f;

        internal static readonly MyDynamicAABBTreeD PointlightsBvh = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        internal static readonly MyDynamicAABBTreeD SpotlightsBvh = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);

        private static readonly Dictionary<uint, LightId> m_idIndex = new Dictionary<uint, LightId>();
        private static readonly MyFreelist<MyLightInfo> m_lights = new MyFreelist<MyLightInfo>(256);

        private static MyPointlightInfo[] m_pointlights = new MyPointlightInfo[256];
        private static MySpotlightInfo[] m_spotlights = new MySpotlightInfo[256];

        private static readonly HashSet<LightId> m_dirtyPointlights = new HashSet<LightId>(LightId.Comparer);
        private static readonly HashSet<LightId> m_dirtySpotlights = new HashSet<LightId>(LightId.Comparer);

        private static readonly Dictionary<LightId, HashSet<uint>> m_ignoredShadowEntities = new Dictionary<LightId, HashSet<uint>>(LightId.Comparer);

        internal static readonly HashSet<LightId> DistantFlaresWithoutLight = new HashSet<LightId>();

        internal static LightId Create(uint GID)
        {
            var id = new LightId { Index = m_lights.Allocate() };

            m_lights.Data[id.Index] = new MyLightInfo
            {
                FlareId = FlareId.NULL
            };

            MyArrayHelpers.Reserve(ref m_pointlights, id.Index + 1);
            MyArrayHelpers.Reserve(ref m_spotlights, id.Index + 1);

			var p1 = new MyPointlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };
			m_pointlights[id.Index] = p1;

			var p2 = new MySpotlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };

            m_spotlights[id.Index] = p2;

            m_idIndex[GID] = id;

            return id;
        }

        internal static LightId Get(uint GID)
        {
            return m_idIndex.Get(GID, LightId.NULL);
        }

        internal static void Remove(uint GID)
        {
            var light = MyLights.Get(GID);
            if (light != LightId.NULL)
            {
                Remove(GID, light);
            }
            else MyRenderProxy.Assert(false);
        }

        private static void Remove(uint GID, LightId id)
        {
            m_idIndex.Remove(GID);

            if (m_pointlights[id.Index].BvhProxyId != -1)
            {
                PointlightsBvh.RemoveProxy(m_pointlights[id.Index].BvhProxyId);
            }

            if (m_spotlights[id.Index].BvhProxyId != -1)
            {
                SpotlightsBvh.RemoveProxy(m_spotlights[id.Index].BvhProxyId);
            }
            MyFlareRenderer.Remove(id.FlareId);
            m_dirtyPointlights.Remove(id);
            m_dirtySpotlights.Remove(id);
            m_ignoredShadowEntities.Remove(id);
            m_lights.Free(id.Index);
        }

        private static int UpdateBvh(MyDynamicAABBTreeD bvh, LightId lid, bool enabled, int proxy, ref BoundingBoxD aabb)
        {
            if(enabled && proxy == -1)
            {
                return bvh.AddProxy(ref aabb, lid, 0);
            }
            else if(enabled && proxy != -1)
            {
                bvh.MoveProxy(proxy, ref aabb, Vector3.Zero);
                return proxy;
            }
            else if (proxy != -1)
            {
                bvh.RemoveProxy(proxy);
            }

            return -1;
        }

        private static BoundingBoxD MakeAabbFromSpotlightCone(ref MySpotlightInfo spotlight, Vector3D position, Vector3 dir, Vector3 up)
        {
            float ratio = (float)Math.Sqrt(1 - spotlight.Spotlight.ApertureCos * spotlight.Spotlight.ApertureCos) / spotlight.Spotlight.ApertureCos;
            float h = ratio * spotlight.Spotlight.Light.Range;
            var bb = BoundingBoxD.CreateInvalid();
            bb.Include(new Vector3D(-h, -h, 0));
            bb.Include(new Vector3D(h, h, -spotlight.Spotlight.Light.Range));

            return bb.TransformFast(MatrixD.CreateLookAtInverse(position, position + dir, up));
        }

        internal static void UpdateEntity(LightId light, ref MyLightInfo info)
        {
            info.FlareId = m_lights.Data[light.Index].FlareId;
            info.LocalSpotPosition = info.SpotPosition;
            info.LocalPointPosition = info.PointPosition;
            info.LocalDirection = info.Direction;
            info.LocalUp = info.Up; 
            if (info.ParentGID != -1)
            {
                info.SpotPosition = m_lights.Data[light.Index].SpotPosition;
                info.PointPosition = m_lights.Data[light.Index].PointPosition;
                info.Direction = m_lights.Data[light.Index].Direction;
                info.Up = m_lights.Data[light.Index].Up;
            }
            m_lights.Data[light.Index] = info;
            if (light.ParentGID == -1)
            {
                m_spotlights[light.Index].ViewProjectionDirty = true;
            }
            CheckDirty(light);
        }

        internal static void UpdatePointlight(LightId light, bool enabled, float intensity, MyLightLayout data)
        {
            data.Color *= intensity;

            if (enabled && (Math.Abs(data.Range - m_pointlights[light.Index].Light.Range) > 0.1f ||
                m_pointlights[light.Index].BvhProxyId == -1))
                m_dirtyPointlights.Add(light);

            if (m_pointlights[light.Index].Enabled != enabled)
            {
                m_pointlights[light.Index].Enabled = enabled;
                m_dirtyPointlights.Add(light);
            }

            m_pointlights[light.Index].Light = data;
        }

        private static void CheckDirty(LightId light)
        {
            var enabled = m_pointlights[light.Index].Enabled;
            if (enabled)
            {
                var difference = Vector3D.RectangularDistance(ref m_pointlights[light.Index].LastBvhUpdatePosition, ref m_lights.Data[light.Index].PointPosition);

                bool dirty = difference > MOVE_TOLERANCE;

                if (dirty)
                {
                    m_dirtyPointlights.Add(light);
                }
            }

            enabled = m_spotlights[light.Index].Enabled;
            if (enabled)
            {
                var positionDifference = Vector3D.RectangularDistance(ref m_spotlights[light.Index].LastBvhUpdatePosition, ref m_lights.Data[light.Index].SpotPosition);
                var upDifference = Vector3.DistanceSquared(m_spotlights[light.Index].LastBvhUpdateUp, m_lights.Data[light.Index].Up);
                var dirDifference = Vector3.DistanceSquared(m_spotlights[light.Index].LastBvhUpdateDir, m_lights.Data[light.Index].Direction);

                bool dirty = positionDifference > MOVE_TOLERANCE || upDifference > DIR_TOLARENCE || dirDifference > DIR_TOLARENCE;

                if (dirty)
                {
                    m_dirtySpotlights.Add(light);
                }
            }
        }

        internal static MatrixD GetSpotlightViewProjection(LightId id)
        {
            if (m_spotlights[id.Index].ViewProjectionDirty)
            {
                m_spotlights[id.Index].ViewProjectionDirty = false;

                var nearPlaneDistance = 0.5f;
                var viewMatrix = MatrixD.CreateLookAt(m_lights.Data[id.Index].SpotPosition,
                    m_lights.Data[id.Index].SpotPosition + m_lights.Data[id.Index].Direction, m_lights.Data[id.Index].Up);
                var projectionMatrix = MatrixD.CreatePerspectiveFieldOfView((float)(Math.Acos(m_spotlights[id.Index].Spotlight.ApertureCos) * 2), 1.0f, nearPlaneDistance, Math.Max(id.ShadowDistance, nearPlaneDistance));
                m_spotlights[id.Index].ViewProjection = viewMatrix * projectionMatrix;
            }
            return m_spotlights[id.Index].ViewProjection;
        }

        private static Matrix CreateShadowMatrix(LightId id)
        {
            var worldMatrix = MatrixD.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition);
            MatrixD worldViewProjection = worldMatrix * GetSpotlightViewProjection(id);
            return Matrix.Transpose(worldViewProjection * MyMatrixHelpers.ClipspaceToTexture);
        }

        internal static void UpdateSpotlight(LightId light, bool enabled, float intensity, float reflectorConeMaxAnglecos, MySpotLightLayout data, ISrvBindable reflectorTexture)
        {
            float coneMaxAngleCos = 1 - reflectorConeMaxAnglecos;
            coneMaxAngleCos = (float)Math.Min(Math.Max(coneMaxAngleCos, 0.01), 0.99f);

            data.Light.Color *= intensity;
            data.ApertureCos = coneMaxAngleCos;

            var info = m_spotlights[light.Index];

            m_spotlights[light.Index].Spotlight = data;
            m_spotlights[light.Index].ReflectorTexture = reflectorTexture;

            if (m_spotlights[light.Index].Enabled != enabled || 
                enabled && (Math.Abs(info.Spotlight.Light.Range - data.Light.Range) > 0.1f || 
                    Math.Abs(info.Spotlight.ApertureCos - data.ApertureCos) > 0.01f ||
                    m_spotlights[light.Index].BvhProxyId == -1))
            {
                m_spotlights[light.Index].Enabled = enabled;
                m_dirtySpotlights.Add(light);
                m_spotlights[light.Index].ViewProjectionDirty = true;
            }
        }

        internal static void Update()
        {
            foreach (var light in m_idIndex.Values)
            {   
                if (m_pointlights[light.Index].Enabled || m_spotlights[light.Index].Enabled)
                { 
                    var gid = light.ParentGID;
                    if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
                    {
                        var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                        Vector3D.Transform(ref m_lights.Data[light.Index].LocalPointPosition, ref matrix, out m_lights.Data[light.Index].PointPosition);

                        if (m_spotlights[light.Index].Enabled)
                        {
                            Vector3D.Transform(ref m_lights.Data[light.Index].LocalSpotPosition, ref matrix, out m_lights.Data[light.Index].SpotPosition);

                            Vector3.TransformNormal(ref m_lights.Data[light.Index].LocalDirection, ref matrix, out m_lights.Data[light.Index].Direction);
                            Vector3.TransformNormal(ref m_lights.Data[light.Index].LocalUp, ref matrix, out m_lights.Data[light.Index].Up);

                            m_spotlights[light.Index].ViewProjectionDirty = true;
                        }

                        CheckDirty(light);

                        if (light.FlareId != FlareId.NULL)
                            MyFlareRenderer.Update(light.FlareId);
                    }
                }
            }

            if(m_dirtyPointlights.Count > 0)
            {
                foreach(var id in m_dirtyPointlights)
                {
                    var proxy = m_pointlights[id.Index].BvhProxyId;
                    var position = m_lights.Data[id.Index].PointPosition;
                    var range = m_pointlights[id.Index].Light.Range;

                    var aabb = new BoundingBoxD(position - range, position + range);
                    m_pointlights[id.Index].BvhProxyId = UpdateBvh(PointlightsBvh, id, m_pointlights[id.Index].Enabled, proxy, ref aabb);
                    m_pointlights[id.Index].LastBvhUpdatePosition = position;
                }

                m_dirtyPointlights.Clear();
            }

            if (m_dirtySpotlights.Count > 0)
            {
                foreach (var id in m_dirtySpotlights)
                {
                    var proxy = m_spotlights[id.Index].BvhProxyId;
                    var position = m_lights.Data[id.Index].SpotPosition;
                    var dir = m_lights.Data[id.Index].Direction;
                    var up = m_lights.Data[id.Index].Up;

                    var aabb = MakeAabbFromSpotlightCone(ref m_spotlights[id.Index], position, dir, up);
                    m_spotlights[id.Index].BvhProxyId = UpdateBvh(SpotlightsBvh, id, m_spotlights[id.Index].Enabled, proxy, ref aabb);
                    m_spotlights[id.Index].LastBvhUpdatePosition = position;
                    m_spotlights[id.Index].LastBvhUpdateDir = dir;
                    m_spotlights[id.Index].LastBvhUpdateUp = up;
                }

                m_dirtySpotlights.Clear();
            }

            BoundingFrustumD viewFrustumClippedD = MyRender11.Environment.Matrices.ViewFrustumClippedD;
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    viewFrustumClippedD = MyStereoRender.EnvMatricesLeftEye.ViewFrustumClippedD;
                else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    viewFrustumClippedD = MyStereoRender.EnvMatricesRightEye.ViewFrustumClippedD;
            }
            PointlightsBvh.OverlapAllFrustum(ref viewFrustumClippedD, MyLightsRendering.VisiblePointlights);
            SpotlightsBvh.OverlapAllFrustum(ref viewFrustumClippedD, MyLightsRendering.VisibleSpotlights);

            MyLightsRendering.VisibleSpotlights.Sort(new MyLightsCameraDistanceComparer());
        }

        internal static void OnSessionEnd()
        {
            PointlightsBvh.Clear();
            SpotlightsBvh.Clear();
            m_idIndex.Clear();
            m_lights.Clear();
            m_ignoredShadowEntities.Clear();
            m_dirtyPointlights.Clear();
            m_dirtySpotlights.Clear();
        }

        internal static void WritePointlightConstants(LightId lid, ref MyPointlightConstants data)
        {
            if(lid == LightId.NULL)
            {
                data = default(MyPointlightConstants);
                return;
            }

            data.Light = m_pointlights[lid.Index].Light;
            data.Light.Position = Vector3.Transform(m_lights.Data[lid.Index].PointPosition - MyRender11.Environment.Matrices.CameraPosition, ref MyRender11.Environment.Matrices.ViewAt0);
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    data.Light.Position = Vector3.Transform(m_lights.Data[lid.Index].PointPosition - MyStereoRender.EnvMatricesLeftEye.CameraPosition, ref MyStereoRender.EnvMatricesLeftEye.ViewAt0);
                else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    data.Light.Position = Vector3.Transform(m_lights.Data[lid.Index].PointPosition - MyStereoRender.EnvMatricesRightEye.CameraPosition, ref MyStereoRender.EnvMatricesRightEye.ViewAt0);
            }
        }

        internal static ISrvBindable WriteSpotlightConstants(LightId lid, ref SpotlightConstants data)
        {
            data.Spotlight = m_spotlights[lid.Index].Spotlight;
            data.Spotlight.ShadowsRange = m_lights.Data[lid.Index].CastsShadowsThisFrame ? m_lights.Data[lid.Index].ShadowsDistance : 0;
            data.Spotlight.Light.Position = m_lights.Data[lid.Index].SpotPosition - MyRender11.Environment.Matrices.CameraPosition;
            data.Spotlight.Direction = m_lights.Data[lid.Index].Direction;
            data.Spotlight.Up = m_lights.Data[lid.Index].Up;

            float ratio = (float)Math.Sqrt(1 - data.Spotlight.ApertureCos * data.Spotlight.ApertureCos) / data.Spotlight.ApertureCos;
            float h = ratio * data.Spotlight.Light.Range;

            Matrix viewProjAt0 = MyRender11.Environment.Matrices.ViewProjectionAt0;
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    viewProjAt0 = MyStereoRender.EnvMatricesLeftEye.ViewProjectionAt0;
                if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    viewProjAt0 = MyStereoRender.EnvMatricesRightEye.ViewProjectionAt0;
            }
            data.ProxyWorldViewProj = Matrix.Transpose(Matrix.CreateScale(2 * h, 2 * h, data.Spotlight.Light.Range) *
                Matrix.CreateWorld(data.Spotlight.Light.Position, data.Spotlight.Direction, data.Spotlight.Up) *
                viewProjAt0);

            data.ShadowMatrix = CreateShadowMatrix(lid);

            return m_spotlights[lid.Index].ReflectorTexture;
        }

        internal static void SetCastsShadowsThisFrame(LightId id, bool b)
        {
            m_lights.Data[id.Index].CastsShadowsThisFrame = b;
        }

        public static MyLightInfo[] GetLights()
        {
            return m_lights.Data;
        }

        public static MySpotlightInfo[] GetSpotlights()
        {
            return m_spotlights;
        }

        public static void UpdateFlare(LightId id, ref MyFlareDesc glare)
        {
            var oldFlareId = m_lights.Data[id.Index].FlareId;
            m_lights.Data[id.Index].FlareId = MyFlareRenderer.Set(m_lights.Data[id.Index].FlareId, glare);
            if (m_lights.Data[id.Index].FlareId != FlareId.NULL && !m_spotlights[id.Index].Enabled && !m_pointlights[id.Index].Enabled)
                DistantFlaresWithoutLight.Add(id);
            else if (oldFlareId != FlareId.NULL)
            {
                DistantFlaresWithoutLight.Remove(id);
            }
        }

        public static void IgnoreShadowForEntity(uint id, uint id2)
        {
            var light = Get(id);
            var actor = MyIDTracker<MyActor>.FindByID(id2);

            if (light != LightId.NULL && actor != null)
            {
                if (!m_ignoredShadowEntities.ContainsKey(light))
                {
                    m_ignoredShadowEntities[light] = new HashSet<uint>();
                }
                m_ignoredShadowEntities[light].Add(id2);
            }
        }

        public static HashSet<uint> GetEntitiesIgnoringShadow(LightId id)
        {
            return m_ignoredShadowEntities.ContainsKey(id) ? m_ignoredShadowEntities[id] : null;
        }

        public static void ClearIgnoredEntities(LightId id)
        {
            m_ignoredShadowEntities.Remove(id);
        }
    }
}

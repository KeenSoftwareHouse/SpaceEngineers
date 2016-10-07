using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRageMath;
using SharpDX;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector3 = VRageMath.Vector3;
using Matrix = VRageMath.Matrix;
using BoundingBox = VRageMath.BoundingBox;
using Color = VRageMath.Color;
using VRage.Generics;
using SharpDX.Direct3D11;

using System;
using VRageRender.Resources;
using VRage.Utils;
using VRage.Library.Utils;

namespace VRageRender
{
    struct MyLightInfo
    {
        internal Vector3D SpotPosition;
        internal Vector3D PointPosition;
        internal float ShadowsDistance;

        internal Vector3D LocalSpotPosition;
        internal Vector3D LocalPointPosition;
        
        internal int ParentGID;
        internal FlareId FlareId;
        internal bool UsedInForward;
        internal bool CastsShadows;
        internal bool CastsShadowsThisFrame;
    }

    struct MyPointlightInfo
    {
        internal bool Enabled;
        internal float Range;
        internal Vector3 Color;
        internal float Falloff;
        internal float SphereRadius;
        internal float GlossFactor;

        internal Vector3D LastBvhUpdatePosition;
        internal int BvhProxyId;

        internal TexId StaticShadowTexture;
    }

    struct MySpotlightInfo
    {
        internal bool Enabled;
        internal float Range;
        internal Vector3 Color;
        internal float ApertureCos;
        internal float Falloff;
        internal float GlossFactor;
        internal Vector3 Direction;
        internal Vector3 Up;

        internal Vector3D LastBvhUpdatePosition;
        internal int BvhProxyId;

        internal TexId ReflectorTexture;

        internal MatrixD ViewProjection;
        internal bool ViewProjectionDirty;
    }

    struct MyHemisphericalLightInfo
    {
        internal float Range;
        internal Vector3 Color;
        internal float Falloff;
        internal float SphereRadius;
        internal Vector3 Direction;
        internal Vector3 Up;
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    struct MyPointlightConstants
    {
        internal Vector3 VsPosition;
        internal float Range;
        internal Vector3 Color;
        internal float Falloff;
        internal float GlossFactor;
        internal Vector3 _pad;
    }

    struct LightId
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

        internal Vector3D SpotPosition { get { return MyLights.Lights.Data[Index].SpotPosition; } }
        internal Vector3D LocalSpotPosition { get { return MyLights.Lights.Data[Index].LocalSpotPosition; } }
        internal Vector3D PointPosition { get { return MyLights.Lights.Data[Index].PointPosition; } }
        internal Vector3D LocalPointPosition { get { return MyLights.Lights.Data[Index].LocalPointPosition; } }
        internal bool CastsShadows { get { return MyLights.Lights.Data[Index].CastsShadows; } }
        internal bool CastsShadowsThisFrame { get { return MyLights.Lights.Data[Index].CastsShadowsThisFrame; } }
        internal float ShadowDistance { get { return MyLights.Lights.Data[Index].ShadowsDistance; } }
        internal float ViewerDistanceSquared { get { return (float)(PointPosition - MyRender11.Environment.CameraPosition).LengthSquared(); } }
        internal int ParentGID { get { return MyLights.Lights.Data[Index].ParentGID; } }
        internal FlareId FlareId { get { return MyLights.Lights.Data[Index].FlareId; } set { MyLights.Lights.Data[Index].FlareId = value;  } }

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
        public static MyLightIdComparerType Comparer = new MyLightIdComparerType();
        #endregion
    }

    class MyLights
    {
        static readonly float MOVE_TOLERANCE = MyRender11Constants.PRUNNING_EXTENSION.X;

        internal static MyDynamicAABBTreeD PointlightsBvh = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        internal static MyDynamicAABBTreeD SpotlightsBvh = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);

        static Dictionary<uint, LightId> IdIndex = new Dictionary<uint, LightId>();
        internal static MyFreelist<MyLightInfo> Lights = new MyFreelist<MyLightInfo>(256);

        static MyPointlightInfo[] Pointlights = new MyPointlightInfo[256];
        internal static MySpotlightInfo[] Spotlights = new MySpotlightInfo[256];

        static HashSet<LightId> DirtyPointlights = new HashSet<LightId>(LightId.Comparer);
        internal static HashSet<LightId> DirtySpotlights = new HashSet<LightId>(LightId.Comparer);

        internal static Dictionary<LightId, HashSet<uint>> IgnoredEntitites = new Dictionary<LightId, HashSet<uint>>(LightId.Comparer);

        internal static LightId Create(uint GID)
        {
            var id = new LightId { Index = Lights.Allocate() };

            Lights.Data[id.Index] = new MyLightInfo
            {
                FlareId = FlareId.NULL
            };

            MyArrayHelpers.Reserve(ref Pointlights, id.Index + 1);
            MyArrayHelpers.Reserve(ref Spotlights, id.Index + 1);

			var p1 = new MyPointlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };
			Pointlights[id.Index] = p1;

			var p2 = new MySpotlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };

            Spotlights[id.Index] = p2;

            IdIndex[GID] = id;

            return id;
        }

        internal static LightId Get(uint GID)
        {
            return IdIndex.Get(GID, LightId.NULL);
        }

        private static uint FindGID(LightId id)
        {
            foreach (var item in IdIndex)
                if (item.Value.Index == id.Index)
                    return item.Key;
            return MyRenderProxy.RENDER_ID_UNASSIGNED;
        }

        internal static void Remove(uint GID, LightId light)
        {
            IdIndex.Remove(GID);

            if (Pointlights[light.Index].BvhProxyId != -1)
            {
                PointlightsBvh.RemoveProxy(Pointlights[light.Index].BvhProxyId);
            }

            if (Spotlights[light.Index].BvhProxyId != -1)
            {
                SpotlightsBvh.RemoveProxy(Spotlights[light.Index].BvhProxyId);
            }
            MyFlareRenderer.Remove(light.FlareId);
            DirtyPointlights.Remove(light);
            DirtySpotlights.Remove(light);
            Lights.Free(light.Index);
        }

        internal static int UpdateBvh(MyDynamicAABBTreeD bvh, LightId lid, bool enabled, int proxy, ref BoundingBoxD aabb)
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
            else
            {
                bvh.RemoveProxy(proxy);
            }

            return -1;
        }

        internal static BoundingBoxD MakeAabbFromSpotlightCone(ref MySpotlightInfo spotlight, Vector3D position)
        {
            float ratio = (float)Math.Sqrt(1 - spotlight.ApertureCos * spotlight.ApertureCos) / spotlight.ApertureCos;
            float h = ratio * spotlight.Range;
            var bb = BoundingBoxD.CreateInvalid();
            bb.Include(new Vector3D(-h, -h, 0));
            bb.Include(new Vector3D(h, h, -spotlight.Range));

            return bb.Transform(MatrixD.CreateLookAtInverse(position, position + spotlight.Direction, spotlight.Up));
        }

        internal static void UpdateEntity(LightId light, ref MyLightInfo info)
        {
            info.FlareId = Lights.Data[light.Index].FlareId;
            Lights.Data[light.Index] = info;

            Lights.Data[light.Index].LocalSpotPosition = info.SpotPosition;
            Lights.Data[light.Index].LocalPointPosition = info.PointPosition;
            var spotPosition = info.SpotPosition;
            var pointPosition = info.PointPosition;
            var gid = info.ParentGID;
            if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
            {
                var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                Vector3D.Transform(ref spotPosition, ref matrix, out spotPosition);
                Vector3D.Transform(ref pointPosition, ref matrix, out pointPosition);
            }

            Lights.Data[light.Index].SpotPosition = spotPosition;
            Lights.Data[light.Index].PointPosition = pointPosition;
        }

        internal static void UpdatePointlight(LightId light, bool enabled, float range, Vector3 color, float falloff, float glossFactor)
        {
            Pointlights[light.Index].Range = range;
            Pointlights[light.Index].Color = color;
            Pointlights[light.Index].Falloff = falloff;
            Pointlights[light.Index].Enabled = enabled;
            Pointlights[light.Index].GlossFactor = glossFactor;
            
            var proxy = Pointlights[light.Index].BvhProxyId;
            var difference = Vector3D.RectangularDistance(ref Pointlights[light.Index].LastBvhUpdatePosition, ref Lights.Data[light.Index].PointPosition);

            bool dirty = (enabled && ((proxy == -1) || (difference > MOVE_TOLERANCE))) || (!enabled && proxy != -1);

            if(dirty)
            {
                DirtyPointlights.Add(light);
            }
            else
            {
                DirtyPointlights.Remove(light);
            }
        }

        internal static MatrixD GetSpotlightViewProjection(LightId id)
        {
            if (Spotlights[id.Index].ViewProjectionDirty)
            {
                Spotlights[id.Index].ViewProjectionDirty = false;

                var nearPlaneDistance = 0.5f;
                var viewMatrix = MatrixD.CreateLookAt(Lights.Data[id.Index].SpotPosition, Lights.Data[id.Index].SpotPosition + Spotlights[id.Index].Direction, Spotlights[id.Index].Up);
                var projectionMatrix = MatrixD.CreatePerspectiveFieldOfView((float)(Math.Acos(Spotlights[id.Index].ApertureCos) * 2), 1.0f, nearPlaneDistance, Math.Max(id.ShadowDistance, nearPlaneDistance));
                Spotlights[id.Index].ViewProjection = viewMatrix * projectionMatrix;
            }
            return Spotlights[id.Index].ViewProjection;
        }
        internal static Matrix CreateShadowMatrix(LightId id)
        {
            var worldMatrix = MatrixD.CreateTranslation(MyRender11.Environment.CameraPosition);
            MatrixD worldViewProjection = worldMatrix * GetSpotlightViewProjection(id);
            return Matrix.Transpose(worldViewProjection * MyMatrixHelpers.ClipspaceToTexture);
        }
        internal static void UpdateSpotlight(LightId light, bool enabled, 
            Vector3 direction, float range, float apertureCos, Vector3 up,
            Vector3 color, float falloff, float glossFactor, TexId reflectorTexture)
        {
            var info = Spotlights[light.Index];

            var gid = light.ParentGID;
            if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
            {
                var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                Vector3.TransformNormal(ref direction, ref matrix, out direction);
                Vector3.TransformNormal(ref up, ref matrix, out up);
            }

            bool aabbChanged = info.Direction != direction || info.Range != range || info.ApertureCos != apertureCos || info.Up != up;

            Spotlights[light.Index].Enabled = enabled;
            Spotlights[light.Index].Direction = direction;
            Spotlights[light.Index].Range = range;
            Spotlights[light.Index].ApertureCos = apertureCos;
            Spotlights[light.Index].Up = up;
            Spotlights[light.Index].Falloff = falloff;
            Spotlights[light.Index].GlossFactor = glossFactor;
            Spotlights[light.Index].Color = color;
            Spotlights[light.Index].ReflectorTexture = reflectorTexture;

            var proxy = Spotlights[light.Index].BvhProxyId;
            var positionDifference = Vector3D.RectangularDistance(ref Spotlights[light.Index].LastBvhUpdatePosition, ref Lights.Data[light.Index].SpotPosition);

            bool dirty = (enabled && ((proxy == -1) || (positionDifference > MOVE_TOLERANCE || aabbChanged))) || (!enabled && proxy != -1);

            if (dirty)
            {
                DirtySpotlights.Add(light);
            }
            else
            {
                DirtySpotlights.Remove(light);
            }
        }

        internal static void Update()
        {
            // touch all lights again, because they don't get updated always when parent is
            foreach (var light in IdIndex.Values)
            {   
                var spotPosition = light.LocalSpotPosition;
                var pointPosition = light.LocalPointPosition;
                var gid = light.ParentGID;
                if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
                {
                    var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                    Vector3D.Transform(ref spotPosition, ref matrix, out spotPosition);
                    Vector3D.Transform(ref pointPosition, ref matrix, out pointPosition);
                }
                Lights.Data[light.Index].SpotPosition = spotPosition;
                Lights.Data[light.Index].PointPosition = pointPosition;

                Spotlights[light.Index].ViewProjectionDirty = true;
            }

            if(DirtyPointlights.Count > 0)
            {
                foreach(var id in DirtyPointlights)
                {
                    var proxy = Pointlights[id.Index].BvhProxyId;
                    var position = Lights.Data[id.Index].PointPosition;
                    var range = Pointlights[id.Index].Range;

                    var aabb = new BoundingBoxD(position - range, position + range);
                    Pointlights[id.Index].BvhProxyId = UpdateBvh(PointlightsBvh, id, Pointlights[id.Index].Enabled, proxy, ref aabb);
                    Pointlights[id.Index].LastBvhUpdatePosition = position;
                }

                DirtyPointlights.Clear();
            }

            if (DirtySpotlights.Count > 0)
            {
                foreach (var id in DirtySpotlights)
                {
                    var proxy = Spotlights[id.Index].BvhProxyId;
                    var position = Lights.Data[id.Index].SpotPosition;
                    var range = Spotlights[id.Index].Range;

                    var aabb = MakeAabbFromSpotlightCone(ref Spotlights[id.Index], position);
                    Spotlights[id.Index].BvhProxyId = UpdateBvh(SpotlightsBvh, id, Spotlights[id.Index].Enabled, proxy, ref aabb);
                    Spotlights[id.Index].LastBvhUpdatePosition = position;
                }

                DirtySpotlights.Clear();
            }
        }

        internal static void OnSessionEnd()
        {
            PointlightsBvh.Clear();
            SpotlightsBvh.Clear();
            IdIndex.Clear();
            Lights.Clear();

            DirtyPointlights.Clear();
            DirtySpotlights.Clear();
        }

        internal static void WritePointlightConstants(LightId lid, ref MyPointlightConstants data)
        {
            if(lid == LightId.NULL)
            {
                data = default(MyPointlightConstants);
                return;
            }

            data.VsPosition = Vector3.Transform(Lights.Data[lid.Index].PointPosition - MyRender11.Environment.CameraPosition, ref MyRender11.Environment.ViewAt0);
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    data.VsPosition = Vector3.Transform(Lights.Data[lid.Index].PointPosition - MyStereoRender.EnvMatricesLeftEye.CameraPosition, ref MyStereoRender.EnvMatricesLeftEye.ViewAt0);
                else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    data.VsPosition = Vector3.Transform(Lights.Data[lid.Index].PointPosition - MyStereoRender.EnvMatricesRightEye.CameraPosition, ref MyStereoRender.EnvMatricesRightEye.ViewAt0);
            }
            data.Range = Pointlights[lid.Index].Range;
            data.Color = Pointlights[lid.Index].Color;
            data.Falloff = Pointlights[lid.Index].Falloff;
            data.GlossFactor = Pointlights[lid.Index].GlossFactor;
        }

        internal static void WriteSpotlightConstants(LightId lid, ref SpotlightConstants data)
        {
            data.ApertureCos = Spotlights[lid.Index].ApertureCos;
            data.Range = Spotlights[lid.Index].Range;
            data.Color = Spotlights[lid.Index].Color;
            data.Direction = Spotlights[lid.Index].Direction;
            data.Up = Spotlights[lid.Index].Up;
            data.GlossFactor = Spotlights[lid.Index].GlossFactor;
            data.ShadowsRange = Lights.Data[lid.Index].CastsShadowsThisFrame ? Lights.Data[lid.Index].ShadowsDistance : 0;
            data.Position = Lights.Data[lid.Index].SpotPosition - MyRender11.Environment.CameraPosition;

            float ratio = (float)Math.Sqrt(1 - data.ApertureCos * data.ApertureCos) / data.ApertureCos;
            float h = ratio * data.Range;

            Matrix viewProjAt0 = MyRender11.Environment.ViewProjectionAt0;
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    viewProjAt0 = MyStereoRender.EnvMatricesLeftEye.ViewProjectionAt0;
                if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    viewProjAt0 = MyStereoRender.EnvMatricesRightEye.ViewProjectionAt0;
            }
            data.ProxyWorldViewProj = Matrix.Transpose(Matrix.CreateScale(2 * h, 2 * h, data.Range) *
                Matrix.CreateWorld(data.Position, data.Direction, data.Up) *
                viewProjAt0);

            data.ShadowMatrix = CreateShadowMatrix(lid);
        }
    }
}

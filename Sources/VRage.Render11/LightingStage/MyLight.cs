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
using VRageRender.Lights;
using VRage.Library.Utils;

namespace VRageRender
{
    struct MyLightInfo
    {
        internal Vector3D Position;
        internal Vector3D PositionWithOffset;
        internal float ShadowsDistance;

        internal Vector3D LocalPosition;
        internal Vector3D LocalPositionWithOffset;
        
        internal int ParentGID;
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
        internal Vector3 Direction;
        internal Vector3 Up;

        internal Vector3D LastBvhUpdatePosition;
        internal int BvhProxyId;

        internal TexId ReflectorTexture;
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

    struct MyGlareDesc
    {
        internal bool Enabled;
        internal Vector3 Direction;
        internal float Range;
        internal Color Color;
        internal float Intensity;
        internal MyStringId Material;
        internal float MaxDistance;
        internal float Size;
        internal float QuerySize;
        internal MyGlareTypeEnum Type;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    struct MyPointlightConstants
    {
        internal Vector3 VsPosition;
        internal float Range;
        internal Vector3 Color;
        internal float Falloff;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct MySpotlightConstants
    {
        [FieldOffset(0)]
        internal Vector3 Position;
        [FieldOffset(12)]
        internal float Range;
        [FieldOffset(16)]
        internal Vector3 Color;
        [FieldOffset(28)]
        internal float Aperture;
        [FieldOffset(32)]
        internal Vector3 Direction;
        [FieldOffset(44)]
        internal uint Id;
        [FieldOffset(48)]
        internal Vector3 Up;
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


        internal Vector3D Position { get { return MyLights.Lights.Data[Index].Position; } }
        internal Vector3D LocalPosition { get { return MyLights.Lights.Data[Index].LocalPosition; } }
        internal Vector3D PositionWithOffset { get { return MyLights.Lights.Data[Index].PositionWithOffset; } }
        internal Vector3D LocalPositionWithOffset { get { return MyLights.Lights.Data[Index].LocalPositionWithOffset; } }
        internal bool CastsShadows { get { return MyLights.Lights.Data[Index].CastsShadows; } }
        internal bool CastsShadowsThisFrame { get { return MyLights.Lights.Data[Index].CastsShadowsThisFrame; } }
        internal float ShadowDistance { get { return MyLights.Lights.Data[Index].ShadowsDistance; } }
        internal float ViewerDistanceSquared { get { return (float)(PositionWithOffset - MyEnvironment.CameraPosition).LengthSquared(); } }
        internal int ParentGID { get { return MyLights.Lights.Data[Index].ParentGID; } }
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

        static HashSet<LightId> DirtyPointlights = new HashSet<LightId>();
        internal static HashSet<LightId> DirtySpotlights = new HashSet<LightId>();

        internal static Dictionary<LightId, HashSet<uint>> IgnoredEntitites = new Dictionary<LightId, HashSet<uint>>();
        internal static Dictionary<LightId, MyGlareDesc> Glares = new Dictionary<LightId, MyGlareDesc>();

        internal static LightId Create(uint GID)
        {
            var id = new LightId { Index = Lights.Allocate() };

            Lights.Data[id.Index] = new MyLightInfo
            {

            };

            MyArrayHelpers.Reserve(ref Pointlights, id.Index + 1);
            MyArrayHelpers.Reserve(ref Spotlights, id.Index + 1);

            Pointlights[id.Index] = new MyPointlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };

            Spotlights[id.Index] = new MySpotlightInfo
            {
                LastBvhUpdatePosition = Vector3.PositiveInfinity,
                BvhProxyId = -1
            };

            IdIndex[GID] = id;

            return id;
        }

        internal static LightId Get(uint GID)
        {
            return IdIndex.Get(GID, LightId.NULL);
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
            Lights.Data[light.Index] = info;

            Lights.Data[light.Index].LocalPosition = info.Position;
            Lights.Data[light.Index].LocalPositionWithOffset = info.PositionWithOffset;
            var position = info.Position;
            var positionWithOffset = info.PositionWithOffset;
            var gid = info.ParentGID;
            if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
            {
                var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                Vector3D.Transform(ref position, ref matrix, out position);
                Vector3D.Transform(ref positionWithOffset, ref matrix, out positionWithOffset);
            }

            Lights.Data[light.Index].Position = position;
            Lights.Data[light.Index].PositionWithOffset = positionWithOffset;
        }

        internal static void UpdatePointlight(LightId light, bool enabled, float range, Vector3 color, float falloff)
        {
            Pointlights[light.Index].Range = range;
            Pointlights[light.Index].Color = color;
            Pointlights[light.Index].Falloff = falloff;
            Pointlights[light.Index].Enabled = enabled;

            
            var proxy = Pointlights[light.Index].BvhProxyId;
            var difference = Vector3D.RectangularDistance(ref Pointlights[light.Index].LastBvhUpdatePosition, ref Lights.Data[light.Index].PositionWithOffset);

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

        internal static void UpdateSpotlight(LightId light, bool enabled, 
            Vector3 direction, float range, float apertureCos, Vector3 up,
            Vector3 color, float falloff, TexId reflectorTexture)
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
            Spotlights[light.Index].Color = color;
            Spotlights[light.Index].ReflectorTexture = reflectorTexture;

            var proxy = Spotlights[light.Index].BvhProxyId;
            var positionDifference = Vector3D.RectangularDistance(ref Spotlights[light.Index].LastBvhUpdatePosition, ref Lights.Data[light.Index].PositionWithOffset);

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

        internal static void UpdateGlare(LightId light, MyGlareDesc desc)
        {
            if(desc.Enabled)
            {
                var gid = light.ParentGID;
                if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
                {
                    var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                    Vector3.TransformNormal(ref desc.Direction, ref matrix, out desc.Direction);
                }

                desc.MaxDistance = (desc.MaxDistance > 0) 
                    ? (float)Math.Min(MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE, desc.MaxDistance)
                    : MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE;

                Glares[light] = desc;
            }
            else
            {
                Glares.Remove(light);
            }
        }

        //internal static void AttachHemisphericalLightInfo(LightId light, ref MyHemisphericalLightInfo info)
        //{
        //}

        internal static void Update()
        {
            // touch all lights again, because they don't get updated always when parent is
            foreach (var light in IdIndex.Values)
            {   
                var position = light.LocalPosition;
                var positionWithOffset = light.LocalPositionWithOffset;
                var gid = light.ParentGID;
                if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
                {
                    var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                    Vector3D.Transform(ref position, ref matrix, out position);
                    Vector3D.Transform(ref positionWithOffset, ref matrix, out positionWithOffset);
                }
                Lights.Data[light.Index].Position = position;
                Lights.Data[light.Index].PositionWithOffset = positionWithOffset;
            }

            if(DirtyPointlights.Count > 0)
            {
                foreach(var id in DirtyPointlights)
                {
                    var proxy = Pointlights[id.Index].BvhProxyId;
                    var position = Lights.Data[id.Index].PositionWithOffset;
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
                    var position = Lights.Data[id.Index].PositionWithOffset;
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
            data.VsPosition = Vector3.Transform(Lights.Data[lid.Index].PositionWithOffset - MyEnvironment.CameraPosition, ref MyEnvironment.ViewAt0);
            data.Range = Pointlights[lid.Index].Range;
            data.Color = Pointlights[lid.Index].Color;
            data.Falloff = Pointlights[lid.Index].Falloff;
        }

        internal static void WriteSpotlightConstants(LightId lid, ref SpotlightConstants data)
        {
            data.ApertureCos = Spotlights[lid.Index].ApertureCos;
            data.Range = Spotlights[lid.Index].Range;
            data.Color = Spotlights[lid.Index].Color;
            data.Direction = Spotlights[lid.Index].Direction;
            data.Up = Spotlights[lid.Index].Up;
            data.ShadowsRange = Lights.Data[lid.Index].CastsShadowsThisFrame ? Lights.Data[lid.Index].ShadowsDistance : 0;
            data.Position = Lights.Data[lid.Index].PositionWithOffset - MyEnvironment.CameraPosition;


            float ratio = (float)Math.Sqrt(1 - data.ApertureCos * data.ApertureCos) / data.ApertureCos;
            float h = ratio * data.Range;
            
            data.ProxyWorldViewProj = Matrix.Transpose(Matrix.CreateScale(2 * h, 2 * h, data.Range) *
                Matrix.CreateWorld(data.Position, data.Direction, data.Up) *
                MyEnvironment.ViewProjectionAt0);
        }
    }
}

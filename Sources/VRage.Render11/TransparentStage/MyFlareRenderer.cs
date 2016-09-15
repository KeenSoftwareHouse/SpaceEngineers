using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    struct FlareId
    {
        internal int Index;
        
        public static bool operator ==(FlareId x, FlareId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(FlareId x, FlareId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly FlareId NULL = new FlareId { Index = -1 };

        #region Equals
        internal class MyFlareIdComparerType : IEqualityComparer<FlareId>
        {
            public bool Equals(FlareId left, FlareId right)
            {
                return left == right;
            }

            public int GetHashCode(FlareId flareId)
            {
                return flareId.Index;
            }
        }
        internal static MyFlareIdComparerType Comparer = new MyFlareIdComparerType();
        #endregion
    }

    internal class MyFlareRenderer
    {
        private static MyFreelist<MyFlareDesc> m_flares = new MyFreelist<MyFlareDesc>(256);

        internal static FlareId Update(int parentGID, FlareId flareId, MyFlareDesc desc)
        {
            if (desc.Enabled)
            {
                if (flareId == FlareId.NULL)
                    flareId = new FlareId { Index = m_flares.Allocate() };

                var gid = parentGID;
                if (gid != -1 && MyIDTracker<MyActor>.FindByID((uint)gid) != null)
                {
                    var matrix = MyIDTracker<MyActor>.FindByID((uint)gid).WorldMatrix;
                    Vector3.TransformNormal(ref desc.Direction, ref matrix, out desc.Direction);
                }

                desc.MaxDistance = (desc.MaxDistance > 0)
                    ? (float)Math.Min(MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE, desc.MaxDistance)
                    : MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE;

                m_flares.Data[flareId.Index] = desc;
            }
            else if (flareId != FlareId.NULL)
            {
                m_flares.Free(flareId.Index);
                flareId = FlareId.NULL;
            }
            return flareId;
        }
        internal static void Remove(FlareId flareId)
        {
            if (flareId != FlareId.NULL)
                m_flares.Free(flareId.Index);
        }
        internal static void Draw(FlareId flareId, Vector3D position)
        {
            var L = MyRender11.Environment.Matrices.CameraPosition - position;
            var distance = (float)L.Length();

            switch (m_flares.Data[flareId.Index].Type)
            {
                case Lights.MyGlareTypeEnum.Distant:
                    DrawDistantFlare(position, ref m_flares.Data[flareId.Index], distance);
                    break;
                case Lights.MyGlareTypeEnum.Normal:
                case Lights.MyGlareTypeEnum.Directional:
                    DrawNormalFlare(position, ref m_flares.Data[flareId.Index], L, distance);
                    break;
                default:
                    break;
            }
        }

        private static void DrawNormalFlare(Vector3D position, ref MyFlareDesc flare, Vector3 L, float distance)
        {
            //if (m_occlusionRatio <= MyMathConstants.EPSILON)
            //    return;

            var intensity = flare.Intensity;
            var maxDistance = flare.MaxDistance;

            //float alpha = m_occlusionRatio * intensity;
            float alpha = intensity;


            const float minFlareRadius = 0.2f;
            const float maxFlareRadius = 10;
            float radius = MathHelper.Clamp(flare.Range * 20, minFlareRadius, maxFlareRadius);

            float drawingRadius = radius * flare.Size;

            if (flare.Type == Lights.MyGlareTypeEnum.Directional)
            {
                float dot = Vector3.Dot(L, flare.Direction);
                alpha *= dot;
            }

            if (alpha <= MyMathConstants.EPSILON)
                return;

            if (distance > maxDistance * .5f)
            {
                // distance falloff
                float falloff = (distance - .5f * maxDistance) / (.5f * maxDistance);
                falloff = (float)Math.Max(0, 1 - falloff);
                drawingRadius *= falloff;
                alpha *= falloff;
            }

            if (drawingRadius <= float.Epsilon)
                return;

            var color = flare.Color;
            //color.A = 0;

            var material = MyTransparentMaterials.GetMaterial(flare.Material.ToString());
            MyBillboardsHelper.AddBillboardOriented(flare.Material.ToString(),
                color * alpha, position, MyRender11.Environment.Matrices.InvView.Left, MyRender11.Environment.Matrices.InvView.Up, drawingRadius);
        }

        private static void DrawDistantFlare(Vector3D position, ref MyFlareDesc flare, float distance)
        {
            //float alpha = m_occlusionRatio * intensity;

            float alpha = flare.Intensity * (flare.QuerySize / 7.5f);

            if (alpha < MyMathConstants.EPSILON)
                return;

            const int minFlareRadius = 5;
            const int maxFlareRadius = 150;

            //flare.QuerySize

            // parent range
            float drawingRadius = MathHelper.Clamp(flare.Range * distance / 1000.0f, minFlareRadius, maxFlareRadius);

            var startFadeout = 800;
            var endFadeout = 1000;

            if (distance > startFadeout)
            {
                var fade = (distance - startFadeout) / (endFadeout - startFadeout);
                alpha *= (1 - fade);
            }

            if (alpha < MyMathConstants.EPSILON)
                return;

            var color = flare.Color;
            //color.A = 0;

            var material = (flare.Type == Lights.MyGlareTypeEnum.Distant && distance > MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE) ? "LightGlareDistant" : "LightGlare";

            MyBillboardsHelper.AddBillboardOriented(material,
                color * alpha, position, MyRender11.Environment.Matrices.InvView.Left, MyRender11.Environment.Matrices.InvView.Up, drawingRadius);
        }
    }
}

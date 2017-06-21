using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    internal struct FlareId
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

    internal static class MyFlareRenderer
    {
        private struct Data
        {
            public MyFlareDesc Desc;
            public Vector3 DirectionWorld;
            public MyOcclusionQueryRender Query;
        }
        private static readonly MyFreelist<Data> m_flares = new MyFreelist<Data>(256);

        internal static FlareId Set(FlareId flareId, MyFlareDesc desc)
        {
            if (desc.Enabled)
            {
                if (flareId == FlareId.NULL)
                {
                    flareId = new FlareId {Index = m_flares.Allocate()};
                }
                if (Math.Abs(desc.Intensity) > 0.01f)
                {
                    if (m_flares.Data[flareId.Index].Query == null)
                        m_flares.Data[flareId.Index].Query = MyOcclusionQueryRenderer.Get(desc.Material.ToString());
                }
                else if (m_flares.Data[flareId.Index].Query != null)
                {
                    MyOcclusionQueryRenderer.Remove(m_flares.Data[flareId.Index].Query);
                    m_flares.Data[flareId.Index].Query = null;
                }

                if (desc.MaxDistance > 0)
                {
                    if (m_flares.Data[flareId.Index].Desc.Type != Lights.MyGlareTypeEnum.Distant)
                        desc.MaxDistance = Math.Min(MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE, desc.MaxDistance);
                }
                else desc.MaxDistance = MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE;

                m_flares.Data[flareId.Index].Desc = desc;
                if (desc.ParentGID == -1)
                    m_flares.Data[flareId.Index].DirectionWorld = desc.Direction;
            }
            else 
            {
                Remove(flareId);
                flareId = FlareId.NULL;
            }
            return flareId;
        }

        internal static void Update(FlareId flareId)
        {
            var gid = m_flares.Data[flareId.Index].Desc.ParentGID;
            if (gid != -1 && m_flares.Data[flareId.Index].Desc.Type == Lights.MyGlareTypeEnum.Directional)
            {
                var actor = MyIDTracker<MyActor>.FindByID((uint) gid);
                if (actor != null)
                {
                    var matrix = actor.WorldMatrix;
                    Vector3.TransformNormal(ref m_flares.Data[flareId.Index].Desc.Direction, ref matrix, out m_flares.Data[flareId.Index].DirectionWorld);
                }
            }
        }

        internal static void Remove(FlareId flareId)
        {
            if (flareId != FlareId.NULL)
            {
                if (m_flares.Data[flareId.Index].Query != null)
                {
                    MyOcclusionQueryRenderer.Remove(m_flares.Data[flareId.Index].Query);
                    m_flares.Data[flareId.Index].Query = null;
                }
                m_flares.Free(flareId.Index);
            }
        }
        internal static void Draw(FlareId flareId, Vector3D position)
        {
            var L = MyRender11.Environment.Matrices.CameraPosition - position;
            var distance = (float)L.Length();

            switch (m_flares.Data[flareId.Index].Desc.Type)
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

        private static void DrawNormalFlare(Vector3D position, ref Data flare, Vector3 L, float distance)
        {
            if (flare.Query == null)
                return;

            flare.Query.Position = position;
            flare.Query.Size = flare.Desc.QuerySize;
            flare.Query.FreqMinMs = flare.Desc.QueryFreqMinMs;
            flare.Query.FreqRndMs = flare.Desc.QueryFreqRndMs;
            flare.Query.Visible = true;

            var intensity = flare.Desc.Intensity;
            var maxDistance = flare.Desc.MaxDistance;

            float alpha = flare.Query.Result * flare.Query.Result * intensity;


            const float minFlareRadius = 0.2f;
            const float maxFlareRadius = 10;
            float radius = MathHelper.Clamp(flare.Desc.Range * 20, minFlareRadius, maxFlareRadius);

            float drawingRadius = radius * flare.Desc.Size;

            if (flare.Desc.Type == Lights.MyGlareTypeEnum.Directional)
            {
                float dot = Vector3.Dot(L, flare.DirectionWorld);
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

            var color = flare.Desc.Color;
            //color.A = 0;
            
            var material = MyTransparentMaterials.GetMaterial(flare.Desc.Material.ToString());
            MyBillboardsHelper.AddBillboardOriented(flare.Desc.Material.ToString(),
                color * alpha, position, MyRender11.Environment.Matrices.InvView.Left, MyRender11.Environment.Matrices.InvView.Up, drawingRadius, 
                MyBillboard.BlenType.AdditiveTop);
        }

        private static void DrawDistantFlare(Vector3D position, ref Data flare, float distance)
        {
            if (flare.Query == null)
                return;

            flare.Query.Position = position;
            flare.Query.Size = flare.Desc.QuerySize;
            flare.Query.FreqMinMs = flare.Desc.QueryFreqMinMs;
            flare.Query.FreqRndMs = flare.Desc.QueryFreqRndMs;
            flare.Query.Visible = true;

            const float RESULT_FLOOR = 0.01f;
            float result = (flare.Query.Result - RESULT_FLOOR) * (1 / (1 - RESULT_FLOOR));
            float alpha = result * result * flare.Desc.Intensity;

            if (alpha < MyMathConstants.EPSILON)
                return;

            /*const int minFlareRadius = 5;
            const int maxFlareRadius = 150;

            // parent range
            float drawingRadius = MathHelper.Clamp(flare.Desc.Range * distance / 1000.0f, minFlareRadius, maxFlareRadius);*/
            const float minFlareRadius = 0.2f;
            const float maxFlareRadius = 10;
            float radius = MathHelper.Clamp(flare.Desc.Range * 20, minFlareRadius, maxFlareRadius);
            float drawingRadius = radius * flare.Desc.Size;

            var startFadeout = flare.Desc.MaxDistance * 0.8f;
            var endFadeout = flare.Desc.MaxDistance;

            if (distance > startFadeout)
            {
                var fade = (distance - startFadeout) / (endFadeout - startFadeout);
                alpha *= (1 - fade);
            }

            if (alpha < MyMathConstants.EPSILON)
                return;

            var color = flare.Desc.Color;

            //var material = (flare.Desc.Type == Lights.MyGlareTypeEnum.Distant && distance > MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE) ? 
            //  "LightGlareDistant" : "LightGlare";

            MyBillboardsHelper.AddBillboardOriented(flare.Desc.Material.ToString(),
                color * alpha, position, MyRender11.Environment.Matrices.InvView.Left, MyRender11.Environment.Matrices.InvView.Up, drawingRadius, 
                MyBillboard.BlenType.AdditiveTop);
        }
    }
}


using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ReflectorLight))]
    public class MyReflectorLight : MyLightingBlock, IMyReflectorLight
    {
        private float GlareQuerySizeDef
        {
            get { return CubeGrid.GridScale * (IsLargeLight ? 3 : 1); }
        }
        private float ReflectorGlareSizeDef
        {
            get { return CubeGrid.GridScale * (IsLargeLight ? 0.650f : 0.198f); }
        }

        protected override void InitLight(MyLight light, Vector4 color, float radius, float falloff)
        {
            light.Start(MyLight.LightTypeEnum.PointLight | MyLight.LightTypeEnum.Spotlight, color, falloff, CubeGrid.GridScale * radius);

            light.ShadowDistance = 20;
            light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            light.UseInForwardRender = true;
            light.ReflectorTexture = BlockDefinition.ReflectorTexture;
            light.Falloff = 0.3f;
            light.GlossFactor = 0;
            light.PointLightOffset = 0.15f;

            light.GlareOn = true;
            light.GlareIntensity = 1f;
            light.GlareQuerySize = GlareQuerySizeDef;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            light.GlareMaterial = BlockDefinition.LightGlare;
            light.GlareSize = ReflectorGlareSizeDef;
        }

        protected override void UpdateIntensity()
        {
            ProfilerShort.Begin("UpdateIntensity");
            var intensity = Render.CurrentLightPower * Intensity * 0.3f;
            var reflIntensity = Render.CurrentLightPower * Intensity;
            m_light.ReflectorIntensity = reflIntensity;
            m_light.Intensity = intensity;
            m_light.GlareIntensity = intensity;
            Render.BulbColor = ComputeBulbColor();
            ProfilerShort.End();
        }

        public override float Falloff
        {
            get { return m_light.ReflectorFalloff; }
            set
            {
                if (m_light.ReflectorFalloff != value)
                {
                    m_light.ReflectorFalloff = value;
                    base.RaisePropertiesChanged();
                }
            }
        }

        public new MyReflectorBlockDefinition BlockDefinition
        {
            get
            {
                if (base.BlockDefinition is MyReflectorBlockDefinition)
                {
                    return (MyReflectorBlockDefinition)base.BlockDefinition;
                }

                SlimBlock.BlockDefinition = new MyReflectorBlockDefinition();
                return (MyReflectorBlockDefinition)base.BlockDefinition;
            }
        }

        public MyReflectorLight()
        {
            this.Render = new MyRenderComponentReflectorLight();
        }

        protected override void LightRadiusChanged()
        {
            base.LightRadiusChanged();
            Radius = ReflectorRadius * (RadiusBounds.Max / ReflectorRadiusBounds.Max);
        }

        private static readonly Color COLOR_OFF  = new Color(30, 30, 30);
        private bool m_wasWorking=true;
        protected override void UpdateEmissivity(bool force=false)
        {
            if (m_wasWorking == (IsWorking && m_light.ReflectorOn) && !force)
                return;
            m_wasWorking = IsWorking && m_light.ReflectorOn;
            if (m_wasWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1, Color, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0, COLOR_OFF, Color.White);
        }
    }
}

#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Lights;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRageMath;

#endregion

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_InteriorLight))]
    public class MyInteriorLight : MyLightingBlock,IMyInteriorLight
    {

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SlimBlock.HasPhysics = BlockDefinition.HasPhysics;
            base.Init(objectBuilder, cubeGrid);
        }

        protected override void InitLight(MyLight light, Vector4 color, float radius, float falloff)
        {
            light.Start(MyLight.LightTypeEnum.PointLight, color, falloff, radius);

            light.ReflectorDirection = WorldMatrix.Forward;
            light.ReflectorUp        = WorldMatrix.Up;
            light.PointLightOffset   = 0f;

            light.GlareOn = light.LightOn;
            light.GlareIntensity = 0.4f;
            light.GlareQuerySize = 0.4f;
            light.GlareMaterial  = BlockDefinition.LightGlare;
            light.GlareType      = VRageRender.Lights.MyGlareTypeEnum.Normal;
            light.GlareSize      = 0.327f;
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity(true);
        }

        protected override void UpdateEmissivity(bool force = false)
        {
            base.UpdateEmissivity(force);

            if (IsWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1, Color, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0, Color.Gray, Color.White);
        }
    }
}

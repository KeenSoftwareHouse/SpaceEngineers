#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Models;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Lights;
using VRageMath;
using System.Reflection;
using Sandbox.Common;

using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_InteriorLight))]
    class MyInteriorLight : MyLightingBlock,IMyInteriorLight
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
            light.PointLightOffset   = 0.5f;

            light.GlareOn        = true;
            light.GlareIntensity = 0.4f;
            light.GlareQuerySize = 1;
            light.GlareMaterial  = BlockDefinition.LightGlare;
            light.GlareType      = VRageRender.Lights.MyGlareTypeEnum.Normal;
            light.GlareSize      = 0.327f;
        }

    }
}

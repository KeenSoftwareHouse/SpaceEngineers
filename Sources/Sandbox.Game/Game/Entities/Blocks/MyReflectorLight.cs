using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using VRageMath;
using System.Reflection;
using Sandbox.Common;

using System.Diagnostics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ReflectorLight))]
    public class MyReflectorLight : MyLightingBlock, IMyReflectorLight
    {
        private float GlareQuerySizeDef
        {
            get { return IsLargeLight ? 3 : 1; }
        }
        private float ReflectorGlareSizeDef
        {
            get { return IsLargeLight ? 0.650f : 0.198f; }
        }

        protected override void InitLight(MyLight light, Vector4 color, float radius, float falloff)
        {
            light.Start(MyLight.LightTypeEnum.PointLight | MyLight.LightTypeEnum.Spotlight, color, falloff, radius);

            light.ShadowDistance = 20;
            light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            light.UseInForwardRender = true;
            light.ReflectorTexture = BlockDefinition.ReflectorTexture;
            light.ReflectorFalloff = 5;

            light.GlareOn = true;
            light.GlareIntensity = 1f;
            light.GlareQuerySize = GlareQuerySizeDef;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            light.GlareMaterial = BlockDefinition.LightGlare;
            light.GlareSize = ReflectorGlareSizeDef;
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

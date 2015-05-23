#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Graphics;
using Sandbox.Definitions;
using Sandbox.Game.GUI;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_CubePlacer))]
    class MyCubePlacer : MyBlockPlacerBase
    {
        private static MyDefinitionId m_handItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));

        protected override MyBlockBuilderBase BlockBuilder { get { return MyCubeBuilder.Static; } }

        public MyCubePlacer()
            : base(MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId))
        {
            //new Vector3(0.0f, 0.18f, -0.15f)
            //PhysicalObject = (MyObjectBuilder_PhysicalGunObject)Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_PhysicalGunObject), "CubePlacerItem");
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (MySession.Static.CreativeMode)
                return;

            base.Shoot(action, direction);

            if (action == MyShootActionEnum.PrimaryAction && !m_firstShot && MyPerGameSettings.EnableWelderAutoswitch)
            {
                if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastKeyPress) >= 500)
                {
                    //MyRenderProxy.DebugDrawText2D(new Vector2(50.0f, 50.0f), "Holding cube placer", Color.Red, 1.0f);
                    // CH:TODO: This should probably be done only locally
                    var block = GetTargetBlock();
                    if (block != null)
                    {
                        MyDefinitionId welderDefinition = new MyDefinitionId(typeof(MyObjectBuilder_Welder));
                        if (Owner.CanSwitchToWeapon(welderDefinition))
                        {
                            Owner.SetupAutoswitch(new MyDefinitionId(typeof(MyObjectBuilder_Welder)), new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer)));
                        }
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (Owner != null && Owner.ControllerInfo.IsLocallyHumanControlled())
            {
                if (MySession.Static.SurvivalMode && (MySession.GetCameraControllerEnum() != MyCameraControllerEnum.Spectator || MyFinalBuildConstants.IS_OFFICIAL))
                {
                    var character = ((MyCharacter)this.CharacterInventory.Owner);
                    MyCubeBuilder.Static.MaxGridDistanceFrom = character.PositionComp.GetPosition() + character.WorldMatrix.Up * 1.8f;
                }
                else
                {
                    MyCubeBuilder.Static.MaxGridDistanceFrom = null;
                }
            }
        }
    }
}

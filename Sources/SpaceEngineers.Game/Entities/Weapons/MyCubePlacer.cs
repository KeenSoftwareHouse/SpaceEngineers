#region Using

using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;


#endregion

namespace SpaceEngineers.Game.Entities.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_CubePlacer))]
    public class MyCubePlacer : MyBlockPlacerBase
    {
        private static MyDefinitionId m_handItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));

        protected override MyBlockBuilderBase BlockBuilder { get { return MyCubeBuilder.Static; } }

        public MyCubePlacer()
            : base(MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId))
        {
            //new Vector3(0.0f, 0.18f, -0.15f)
            //PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_PhysicalGunObject), "CubePlacerItem");
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (MySession.Static.CreativeMode)
                return;

            base.Shoot(action, direction, overrideWeaponPos, gunAction);

            if (action == MyShootActionEnum.PrimaryAction && !m_firstShot)
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
    }
}

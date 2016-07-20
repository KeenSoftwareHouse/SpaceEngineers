using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.EntityComponents.Renders;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.Weapons.Guns.Barrels;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Weapons.Guns
{
    [MyCubeBlockType(typeof(MyObjectBuilder_LargeGatlingTurret))]
    public class MyLargeGatlingTurret : MyLargeConveyorTurretBase, IMyLargeGatlingTurret
    {
        public int Burst { get; private set; }
    
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            if (!MyFakes.ENABLE_GATLING_TURRETS)
                return;

            // User settings:
            m_randomStandbyChangeConst_ms = MyUtils.GetRandomInt(3500,4500);

            if (m_gunBase.HasAmmoMagazines)
                m_shootingCueEnum = m_gunBase.ShootSound;
            m_rotatingCueEnum.Init("WepTurretGatlingRotate");

            Render.NeedsDraw = true;
        }

        protected override float ForwardCameraOffset { get { return 0.5f; } }
        protected override float UpCameraOffset { get { return 0.75f; } }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction)
                return;

            m_gunBase.Shoot(Parent.Physics.LinearVelocity);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            if (IsFunctional)
            {
                m_base1 = Subparts["GatlingTurretBase1"];
                m_base2 = m_base1.Subparts["GatlingTurretBase2"];
                m_barrel = new MyLargeGatlingBarrel();
                ((MyLargeGatlingBarrel)m_barrel).Init(m_base2.Subparts["GatlingBarrel"], this);
                GetCameraDummy();
            }
            else
            {
                m_base1 = null;
                m_base2 = null;
                m_barrel = null;
            }

            ResetRotation();
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyFakes.ENABLE_GATLING_TURRETS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
            {
                RotateModels();
                return;
            }

            base.UpdateAfterSimulation();

            DrawLasers();
        }

        public MyLargeGatlingTurret()
        {
            Render = new MyRenderComponentLargeTurret();
        }
    }
}

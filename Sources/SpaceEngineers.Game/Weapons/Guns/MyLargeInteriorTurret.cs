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
    [MyCubeBlockType(typeof(MyObjectBuilder_InteriorTurret))]
    public class MyLargeInteriorTurret : MyLargeTurretBase, IMyLargeInteriorTurret
    {
        public int Burst { get; private set; }
     
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            // User settings:
            m_randomStandbyChangeConst_ms = MyUtils.GetRandomInt(3500, 4500);

            if (m_gunBase.HasAmmoMagazines)
                m_shootingCueEnum = m_gunBase.ShootSound;
            m_rotatingCueEnum.Init("WepTurretInteriorRotate");

            Render.NeedsDraw = true;
        }

        protected override float ForwardCameraOffset { get { return 0.1f; } }
        protected override float UpCameraOffset { get { return 0.25f; } }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction)
                return;
        
            m_gunBase.Shoot(Vector3.Zero);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            if (IsFunctional)
            {
                m_base1 = Subparts["InteriorTurretBase1"];
                m_base2 = m_base1.Subparts["InteriorTurretBase2"];
                m_barrel = new MyLargeInteriorBarrel();
                ((MyLargeInteriorBarrel)m_barrel).Init(m_base2, this);
                GetCameraDummy();
                RotateModels();
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
            if (!MyFakes.ENABLE_INTERIOR_TURRETS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
            {
                RotateModels();
                return;
            }

            base.UpdateAfterSimulation();

            DrawLasers();
        }

        public MyLargeInteriorTurret()
        {
            Render = new MyRenderComponentLargeTurret();
        }
    }
}

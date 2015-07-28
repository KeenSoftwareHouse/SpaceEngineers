using System.Text;
using VRageMath;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;

using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Engine.Utils;
using VRage.Utils;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_InteriorTurret))]
    class MyLargeInteriorTurret : MyLargeTurretBase, IMyLargeInteriorTurret
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

        public override void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
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
            if (!MyFakes.ENABLE_INTERIOR_TURRETS || MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
            {
                RotateModels();
                return;
            }

            base.UpdateAfterSimulation();

            DrawLasers();
        }

        public MyLargeInteriorTurret()
        {
            Render = new Components.MyRenderComponentLargeTurret();
        }
    }
}

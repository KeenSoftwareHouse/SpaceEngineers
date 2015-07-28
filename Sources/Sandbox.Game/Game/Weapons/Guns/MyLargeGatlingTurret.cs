using System.Text;
using VRageMath;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;

using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Engine.Utils;
using Sandbox.Graphics;
using VRage.Utils;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_LargeGatlingTurret))]
    class MyLargeGatlingTurret : MyLargeConveyorTurretBase, IMyLargeGatlingTurret
    {
        public int Burst { get; private set; }
    
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            if (!MyFakes.ENABLE_GATLING_TURRETS)
                return;
            if (MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS)
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
            if (!MyFakes.ENABLE_GATLING_TURRETS || MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
            {
                RotateModels();
                return;
            }

            base.UpdateAfterSimulation();

            DrawLasers();
        }

        public MyLargeGatlingTurret()
        {
            Render = new Components.MyRenderComponentLargeTurret();
        }
    }
}

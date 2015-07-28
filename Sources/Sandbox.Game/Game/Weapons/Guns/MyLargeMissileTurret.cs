#region Using

using System.Collections.Generic;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Import;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI.Ingame;

#endregion

namespace Sandbox.Game.Weapons         
{
    [MyCubeBlockType(typeof(MyObjectBuilder_LargeMissileTurret))]
    class MyLargeMissileTurret : MyLargeConveyorTurretBase, IMyLargeMissileTurret
    {
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            //Matrix barrelMatrix = MyMath.NormalizeMatrix(ModelLod0.Dummies["axis"].Matrix);
            Matrix barrelMatrix = Matrix.Identity;

            // User settings:
            m_randomStandbyChangeConst_ms = 4000;
            m_rotationSpeed = MathHelper.Pi / 2000.0f;
            m_elevationSpeed = MathHelper.Pi / 2000.0f;

            if (BlockDefinition != null)
            {
                m_rotationSpeed = BlockDefinition.RotationSpeed;
                m_elevationSpeed = BlockDefinition.ElevationSpeed;
            }

            if (m_gunBase.HasAmmoMagazines)
                m_shootingCueEnum = m_gunBase.ShootSound;
            m_rotatingCueEnum.Init("WepTurretGatlingRotate");
        }

        protected override float ForwardCameraOffset { get { return 0.5f; } }
        protected override float UpCameraOffset { get { return 1.0f; } }

        public override void OnModelChange()
        {
            base.OnModelChange();

            if (IsFunctional)
            {
                m_base1 = Subparts["MissileTurretBase1"];
                m_base2 = m_base1.Subparts["MissileTurretBarrels"];
                m_barrel = new MyLargeMissileBarrel();
                ((MyLargeMissileBarrel)m_barrel).Init(m_base2, this);
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

        public override void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction)
                return;

            m_barrel.StartShooting();
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyFakes.ENABLE_MISSILE_TURRETS || MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
            {
                RotateModels();
                return;
            }

            base.UpdateAfterSimulation();

            DrawLasers();      
        }

        public override void ShootFromTerminal(Vector3 direction)
        {
            m_isControlled = true;
            m_barrel.StartShooting();
            m_isControlled = false;
        }
    }
}

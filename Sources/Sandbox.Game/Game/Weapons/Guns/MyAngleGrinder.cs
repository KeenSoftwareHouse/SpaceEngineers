#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AngleGrinder))]
    class MyAngleGrinder : MyEngineerToolBase
    {
        private MySoundPair IDLE_SOUND = new MySoundPair("ToolPlayGrindIdle");
        private MySoundPair METAL_SOUND = new MySoundPair("ToolPlayGrindMetal");

        static readonly float GRINDER_AMOUNT_PER_SECOND = 2f;
        static readonly float GRINDER_MAX_SPEED_RPM = 500f;
        static readonly float GRINDER_ACCELERATION_RPMPS = 700f;
        static readonly float GRINDER_DECELERATION_RPMPS = 500f;

        List<MyPhysicalInventoryItem> m_tmpItemList = new List<MyPhysicalInventoryItem>();
        Dictionary<int, int> m_tmpComponents = new Dictionary<int, int>();

        MyHudNotification m_grindingNotification;

        int m_lastUpdateTime;
        float m_rotationSpeed;

        static MyDefinitionId m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AngleGrinderItem");

        public MyAngleGrinder()
            : base(MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(m_physicalItemId), 0.5f, 250)
        {
            SecondaryLightIntensityLower = 0.4f;
            SecondaryLightIntensityUpper = 0.4f;
            EffectId = MyParticleEffectsIDEnum.AngleGrinder;

            HasCubeHighlight = true;
            HighlightColor = Color.Red * 0.3f;

            m_grindingNotification = new MyHudNotification(MySpaceTexts.AngleGrinderPrimaryAction, MyHudNotification.INFINITE, level: MyNotificationLevel.Control);
            m_grindingNotification.SetTextFormatArguments(MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION));

            m_rotationSpeed = 0.0f;

            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemId);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Init(null, "Models\\Weapons\\AngleGrinder.mwm", null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;
        }

        float GrinderAmount
        {
            get
            {
                return MySession.Static.GrinderSpeedMultiplier * GRINDER_AMOUNT_PER_SECOND * ToolCooldownMs / 1000.0f;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            int timeDelta = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_activated && m_rotationSpeed < GRINDER_MAX_SPEED_RPM)
            {
                m_rotationSpeed += timeDelta * 0.001f * GRINDER_ACCELERATION_RPMPS;
                if (m_rotationSpeed > GRINDER_MAX_SPEED_RPM)
                    m_rotationSpeed = GRINDER_MAX_SPEED_RPM;
            }
            else if (m_activated == false && m_rotationSpeed > 0.0f)
            {
                m_rotationSpeed -= timeDelta * 0.001f * GRINDER_DECELERATION_RPMPS;
                if (m_rotationSpeed < 0.0f)
                    m_rotationSpeed = 0.0f;
            }

            var subpart = Subparts["grinder"];
            subpart.PositionComp.LocalMatrix = Matrix.CreateRotationY(-timeDelta * m_rotationSpeed * MathHelper.RPMToRadiansPerMillisec) * subpart.PositionComp.LocalMatrix;

            if (Owner == null || MySession.ControlledEntity != Owner)
            {
                MyHud.Notifications.Remove(m_grindingNotification);
            }
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            base.Shoot(action, direction);

            if (action == MyShootActionEnum.PrimaryAction && IsPreheated && Sync.IsServer && m_activated)
            {
                Grind();
            }
            return;
        }

        protected override void AddHudInfo()
        {
            MyHud.Notifications.Add(m_grindingNotification);
        }

        protected override void RemoveHudInfo()
        {
            MyHud.Notifications.Remove(m_grindingNotification);
        }

        protected override MatrixD GetEffectMatrix(float muzzleOffset)
        {
            if (m_targetGrid == null || m_targetCube == null || !(Owner is MyCharacter))
            {
                return MatrixD.CreateWorld(m_gunBase.GetMuzzleWorldPosition(), WorldMatrix.Forward, WorldMatrix.Up);
            }

            var headMatrix = Owner.GetHeadMatrix(true);
            var aimPoint = m_targetPosition;
            var dist = Vector3.Dot(aimPoint - m_gunBase.GetMuzzleWorldPosition(), headMatrix.Forward);
            dist -= 0.1f;
            var target = m_gunBase.GetMuzzleWorldPosition() + headMatrix.Forward * (dist * muzzleOffset) + headMatrix.Up * 0.04f;

            MatrixD matrix = MatrixD.CreateWorld(dist > 0 && muzzleOffset == 0 ? m_gunBase.GetMuzzleWorldPosition() : target, WorldMatrix.Forward, WorldMatrix.Up);

            //var normal = ((MyCharacter) CharacterInventory.Owner).AimedPointNormal;

            Vector3D right = Vector3D.TransformNormal(matrix.Right, MatrixD.CreateFromAxisAngle(matrix.Up, -MathHelper.ToRadians(45)));
            right = MyUtilRandomVector3ByDeviatingVector.GetRandom(right, MyUtils.GetRandomFloat(0, 0.35f));
            Vector3D up = Vector3D.Cross(right, matrix.Forward);
            Vector3D forward = Vector3D.Cross(up, right);
            return MatrixD.CreateWorld(matrix.Translation, forward, up);
            //matrix = Matrix.CreateFromAxisAngle(matrix.Up, -MathHelper.ToRadians(75)) * matrix;
            //return Matrix.CreateWorld(matrix.Translation, matrix.Forward, Vector3.Reflect(matrix.Forward, normal));
        }

        private void Grind()
        {
            var block = GetTargetBlock();
            if (block != null)
            {
                float hackMultiplier = 1.0f;
                if (block.FatBlock != null && Owner != null && Owner.ControllerInfo.Controller != null && Owner.ControllerInfo.Controller.Player != null)
                {
                    var relation = block.FatBlock.GetUserRelationToOwner(Owner.ControllerInfo.Controller.Player.Identity.IdentityId);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                        hackMultiplier = MySession.Static.HackSpeedMultiplier;
                }

                block.DecreaseMountLevel(GrinderAmount * hackMultiplier, CharacterInventory);
                block.MoveItemsFromConstructionStockpile(CharacterInventory);

                if (block.IsFullyDismounted)
                {
                    block.SpawnConstructionStockpile();
                    block.CubeGrid.RazeBlock(block.Min);
                }
            }

            var targetDestroyable = GetTargetDestroyable();
            if (targetDestroyable != null && Sync.IsServer)
                targetDestroyable.DoDamage(20, MyDamageType.Drill, true);
        }

        protected override void StartLoopSound(bool effect)
        {
            MySoundPair cueEnum = effect ? METAL_SOUND : IDLE_SOUND;
            if (m_soundEmitter.Sound != null && m_soundEmitter.Sound.IsPlaying)
                m_soundEmitter.PlaySingleSound(cueEnum, true, false);
            else
                m_soundEmitter.PlaySound(cueEnum, true, false);
        }

        protected override void StopLoopSound()
        {
            m_soundEmitter.StopSound(false);
        }

        protected override void StopSound()
        {
            if (m_soundEmitter.Sound != null && m_soundEmitter.Sound.IsPlaying)
            m_soundEmitter.StopSound(true);
        }
    }
}

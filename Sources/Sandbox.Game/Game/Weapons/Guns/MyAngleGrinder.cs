#region Using

using System;
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
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using Sandbox.Game.Audio;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.World;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AngleGrinder))]
    public class MyAngleGrinder : MyEngineerToolBase, IMyAngleGrinder
    {
        private MySoundPair m_idleSound = new MySoundPair("ToolPlayGrindIdle");
        private MySoundPair m_actualSound = new MySoundPair("ToolPlayGrindMetal");
        private MyStringHash m_source = MyStringHash.GetOrCompute("Grinder");
        private MyStringHash m_metal = MyStringHash.GetOrCompute("Metal");

        static readonly float GRINDER_AMOUNT_PER_SECOND = 2f;
        static readonly float GRINDER_MAX_SPEED_RPM = 500f;
        static readonly float GRINDER_ACCELERATION_RPMPS = 700f;
        static readonly float GRINDER_DECELERATION_RPMPS = 500f;

        MyHudNotification m_grindingNotification;

        int m_lastUpdateTime;
        float m_rotationSpeed;

        //GK: Added in order to check for breakable environment items (e.g. trees) from hand tools (Driller,Grinder)
        private int m_lastContactTime;
        private int m_lastItemId;

        static MyDefinitionId m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AngleGrinderItem");
        private float m_grinderCameraMaxShakeIntensity = 1.5f;
        private double m_grinderCameraMeanShakeIntensity = 1.0f;

        public MyAngleGrinder()
            : base(250)
        {
            SecondaryLightIntensityLower = 0.4f;
            SecondaryLightIntensityUpper = 0.4f;
            EffectScale = 0.6f;

            HasCubeHighlight = true;
            HighlightColor = Color.Red * 0.3f;
			HighlightMaterial = "GizmoDrawLineRed";

            m_grindingNotification = new MyHudNotification(MySpaceTexts.AngleGrinderPrimaryAction, MyHudNotification.INFINITE, level: MyNotificationLevel.Control);

            m_rotationSpeed = 0.0f;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AngleGrinderItem");
            if (objectBuilder.SubtypeName !=null && objectBuilder.SubtypeName.Length>0)
                m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), objectBuilder.SubtypeName + "Item");
            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemId);
            base.Init(objectBuilder, m_physicalItemId);

            var definition=MyDefinitionManager.Static.GetPhysicalItemDefinition(m_physicalItemId);
            Init(null, definition.Model, null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;

            foreach (ToolSound toolSound in m_handItemDef.ToolSounds)
            {
                if (toolSound.type == null || toolSound.subtype == null || toolSound.sound == null)
                    continue;
                if (toolSound.type.Equals("Main"))
                {
                    if (toolSound.subtype.Equals("Idle"))
                        m_idleSound = new MySoundPair(toolSound.sound);
                }
            }
        }

        float GrinderAmount
        {
            get
            {
                return MySession.Static.GrinderSpeedMultiplier * m_speedMultiplier * GRINDER_AMOUNT_PER_SECOND * ToolCooldownMs / 1000.0f;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            int timeDelta = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (!m_activated)
                EffectId = null;

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

            if (Owner == null || MySession.Static.ControlledEntity != Owner)
            {
                MyHud.Notifications.Remove(m_grindingNotification);
            }
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            MyAnalyticsHelper.ReportActivityStartIf(!m_activated, this.Owner, "Grinding", "Character", "HandTools", "AngleGrinder", true);

            base.Shoot(action, direction, overrideWeaponPos, gunAction);

            if (action == MyShootActionEnum.PrimaryAction && IsPreheated && Sync.IsServer && m_activated)
            {
                Grind();
            }
            return;
        }

        protected override void AddHudInfo()
        {
            if (!MyInput.Static.IsJoystickConnected())
                m_grindingNotification.SetTextFormatArguments(MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION));
            else
                m_grindingNotification.SetTextFormatArguments(MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION));
            MyHud.Notifications.Add(m_grindingNotification);
        }

        protected override void RemoveHudInfo()
        {
            MyHud.Notifications.Remove(m_grindingNotification);
        }

        public override void EndShoot(MyShootActionEnum action)
        {
            MyAnalyticsHelper.ReportActivityEnd(this.Owner, "Grinding");
            base.EndShoot(action);
        }

        protected override MatrixD GetEffectMatrix(float muzzleOffset)
        {
            if (m_raycastComponent.HitCubeGrid == null || m_raycastComponent.HitBlock == null || !(Owner is MyCharacter))
            {
                return MatrixD.CreateWorld(m_gunBase.GetMuzzleWorldPosition(), WorldMatrix.Forward, WorldMatrix.Up);
            }

            var headMatrix = Owner.GetHeadMatrix(true);
            var aimPoint = m_raycastComponent.HitPosition;
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
            MyStringHash target = m_metal;
            EffectId = null;
            if (block != null && (!(MySession.Static.IsScenario || MySession.Static.Settings.ScenarioEditMode) || block.CubeGrid.BlocksDestructionEnabled))
            {
                float hackMultiplier = 1.0f;
                if (block.FatBlock != null && Owner != null && Owner.ControllerInfo.Controller != null && Owner.ControllerInfo.Controller.Player != null)
                {
                    var relation = block.FatBlock.GetUserRelationToOwner(Owner.ControllerInfo.Controller.Player.Identity.IdentityId);
                    if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                        hackMultiplier = MySession.Static.HackSpeedMultiplier;
                }

                float damage = GrinderAmount;
                MyDamageInformation damageInfo = new MyDamageInformation(false, damage * hackMultiplier, MyDamageType.Grind, EntityId);

                if (block.UseDamageSystem)
                    MyDamageSystem.Static.RaiseBeforeDamageApplied(block, ref damageInfo);

                if (block.CubeGrid.Editable)
                {
                    block.DecreaseMountLevel(damageInfo.Amount, CharacterInventory);
                    block.MoveItemsFromConstructionStockpile(CharacterInventory);
                }

                if (MySession.Static != null && Owner == MySession.Static.LocalCharacter && MyMusicController.Static != null)
                    MyMusicController.Static.Building(250);

                if (block.UseDamageSystem)
                    MyDamageSystem.Static.RaiseAfterDamageApplied(block, damageInfo);
                    
                if (block.IsFullyDismounted)
                {
                    if (block.UseDamageSystem)
                        MyDamageSystem.Static.RaiseDestroyed(block, damageInfo);

                    block.SpawnConstructionStockpile();
                    block.CubeGrid.RazeBlock(block.Min);
                }
                if (block.BlockDefinition.PhysicalMaterial.Id.SubtypeName.Length > 0)
                    target = block.BlockDefinition.PhysicalMaterial.Id.SubtypeId;

                if (Owner != null && Owner.ControllerInfo.IsLocallyControlled() && (Owner.IsInFirstPersonView || Owner.ForceFirstPersonCamera))
                    PerformCameraShake();
            }

            var targetDestroyable = GetTargetDestroyable();
            if (targetDestroyable != null)
            {
                //HACK to not grind yourself 
                if(targetDestroyable is MyCharacter && (targetDestroyable as MyCharacter) == Owner)
                {
                    return;
                }

                //damage tracking
                if (targetDestroyable is MyCharacter && MySession.Static.ControlledEntity == this.Owner && (targetDestroyable as MyCharacter).IsDead == false)
                    MySession.Static.TotalDamageDealt += 20;

                targetDestroyable.DoDamage(20, MyDamageType.Grind, true, attackerId: Owner != null ? Owner.EntityId : 0);
                if (targetDestroyable is MyCharacter)
                    target = MyStringHash.GetOrCompute((targetDestroyable as MyCharacter).Definition.PhysicalMaterial);

                if (Owner != null && Owner.ControllerInfo.IsLocallyControlled() && (Owner.IsInFirstPersonView || Owner.ForceFirstPersonCamera))
                    PerformCameraShake();
            }

            var sector = m_raycastComponent.HitEnvironmentSector;
            if (sector != null)
            {
                var itemId = m_raycastComponent.EnvironmentItem;
                if (itemId != m_lastItemId)
                {
                    m_lastItemId = itemId;
                    m_lastContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                }
                if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastContactTime > MyDebrisConstants.CUT_TREE_IN_MILISECONDS / m_speedMultiplier)
                {
                    var sectorProxy = sector.GetModule<MyBreakableEnvironmentProxy>();
                    sectorProxy.BreakAt(itemId, m_raycastComponent.HitPosition, Vector3D.Zero, 0);
                    m_lastContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    m_lastItemId = 0;
                }
                target = MyStringHash.GetOrCompute("Wood");
            }

            if (block != null || targetDestroyable != null)
            {
                m_actualSound = MyMaterialPropertiesHelper.Static.GetCollisionCue(MyMaterialPropertiesHelper.CollisionType.Start, m_handItemDef.ToolMaterial, target);
                EffectId = MyMaterialPropertiesHelper.Static.GetCollisionEffect(MyMaterialPropertiesHelper.CollisionType.Start, m_handItemDef.ToolMaterial, target);
            }
        }

        protected override void StartLoopSound(bool effect)
        {
            MySoundPair cueEnum = effect ? m_actualSound : m_idleSound;
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

        public void PerformCameraShake()
        {
            if (MySector.MainCamera == null)
                return;

            float intensity = (float)(-Math.Log(MyRandom.Instance.NextDouble()) * m_grinderCameraMeanShakeIntensity);
            intensity = MathHelper.Clamp(intensity, 0, m_grinderCameraMaxShakeIntensity);
            MySector.MainCamera.CameraShake.AddShake(intensity);
        }
    }
}

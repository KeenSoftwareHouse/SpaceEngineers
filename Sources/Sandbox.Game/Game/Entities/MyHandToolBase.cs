#region Using

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Utils;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.Gui;
using VRage.Game.Entity;
using System;
using Sandbox.Common;
using VRage;
using VRage.Game;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ModAPI;

#endregion


namespace Sandbox.Game.Entities
{
    [StaticEventOwner]
    [MyEntityType(typeof(MyObjectBuilder_HandToolBase))]
    public class MyHandToolBase : MyEntity, IMyHandheldGunObject<MyToolBase>, IStoppableAttackingTool
    {
        #region Nested

        public class MyBlockingBody : Sandbox.Engine.Physics.MyPhysicsBody
        {
            public MyHandToolBase HandTool { get; private set; }

            public MyBlockingBody(MyHandToolBase tool, MyEntity owner)
                : base(owner, RigidBodyFlag.RBF_KINEMATIC)
            {
                HandTool = tool;
            }

            public override void OnMotion(HkRigidBody rbo, float step, bool fromParent)
            {
            }

            public override void OnWorldPositionChanged(object source)
            {
            }

            public void SetWorldMatrix(MatrixD worldMatrix)
            {
               // Vector3 transformedCenter = Vector3.TransformNormal(Center, worldMatrix);

                var offset = MyPhysics.GetObjectOffset(ClusterObjectID);

                Matrix rigidBodyMatrix = Matrix.CreateWorld((Vector3)(worldMatrix.Translation - (Vector3D)offset), worldMatrix.Forward, worldMatrix.Up);

                if (RigidBody != null)
                {
                    RigidBody.SetWorldMatrix(rigidBodyMatrix);
                }
            }
        }

        #endregion

        #region Fields

        static MyStringId m_startCue = MyStringId.GetOrCompute("Start");
        static MyStringId m_hitCue = MyStringId.GetOrCompute("Hit");

        private const float AFTER_SHOOT_HIT_DELAY = 0.4f;

        private MyDefinitionId m_handItemDefinitionId;                

        private MyToolActionDefinition? m_primaryToolAction;
        private MyToolHitCondition m_primaryHitCondition;
        private MyToolActionDefinition? m_secondaryToolAction;
        private MyToolHitCondition m_secondaryHitCondition;
        
        private MyToolActionDefinition? m_shotToolAction;
        private MyToolHitCondition m_shotHitCondition;

        bool m_wasShooting = false;
        bool m_swingSoundPlayed = false;
        bool m_isHit = false;

        protected Dictionary<string, IMyHandToolComponent> m_toolComponents = new Dictionary<string, IMyHandToolComponent>();
        private MyCharacter m_owner;
        protected MyTimeSpan m_lastShot = MyTimeSpan.Zero;

        private MyTimeSpan m_lastHit = MyTimeSpan.Zero;
        private MyTimeSpan m_hitDelay = MyTimeSpan.Zero;

        MyPhysicalItemDefinition m_physItemDef;
        protected MyToolItemDefinition m_toolItemDef;
        private MyEntity3DSoundEmitter m_soundEmitter;

        Dictionary<string, MySoundPair> m_toolSounds = new Dictionary<string, MySoundPair>();

        private static MyStringId BlockId = MyStringId.Get("Block");

        MyHudNotification m_notEnoughStatNotification;

        #endregion

        #region Properties

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; private set; }

        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }
     
        public bool IsShooting
        {
            get
            {
                if (!m_shotToolAction.HasValue)
                    return false;

                return (m_lastShot <= MySandboxGame.Static.UpdateTime) &&
                    (MySandboxGame.Static.UpdateTime - m_lastShot < MyTimeSpan.FromSeconds(m_shotToolAction.Value.HitDuration) || m_shotToolAction.Value.HitDuration == 0);
            }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        public float BackkickForcePerSecond
        {
            get { return 0.0f; }
        }

        public float ShakeAmount
        {
            get { return 2.5f; }
            protected set { }
        }

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDefinitionId; }
        }

        public MyToolBase GunBase { get; private set; } 

        public virtual bool ForceAnimationInsteadOfIK { get { return true; } }
        public bool IsBlocking { get { return m_shotToolAction.HasValue && m_shotToolAction.Value.Name == MyStringId.GetOrCompute("Block"); } }


        public MyPhysicalItemDefinition PhysicalItemDefinition
        {
            get { return m_physItemDef; }
        }

        public MyCharacter Owner
        {
            get
            {
                return m_owner;
            }
        }

        #endregion

        #region Init

        public MyHandToolBase()
        {          
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            GunBase = new MyToolBase();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_handItemDefinitionId = objectBuilder.GetId();
            m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemForHandItem(m_handItemDefinitionId);
            base.Init(objectBuilder);
            Init(null, PhysicalItemDefinition.Model, null, null, null);
            
            Save = false;

            PhysicalObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(m_handItemDefinitionId.SubtypeName);
            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;

            m_toolItemDef = PhysicalItemDefinition as MyToolItemDefinition;

            m_notEnoughStatNotification = new MyHudNotification(MyCommonTexts.NotificationStatNotEnough, disappearTimeMs: 1000, font: MyFontEnum.Red, level: MyNotificationLevel.Important);

            InitToolComponents();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

			var builder = objectBuilder as MyObjectBuilder_HandToolBase;

            if (builder.DeviceBase != null)
            {
                GunBase.Init(builder.DeviceBase);
            }
        }

        protected virtual void InitToolComponents()
        {
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = base.GetObjectBuilder(copy) as MyObjectBuilder_HandToolBase;
            ob.SubtypeName = m_handItemDefinitionId.SubtypeName;
            ob.DeviceBase = GunBase.GetObjectBuilder();
            return ob;
        }

        void InitBlockingPhysics(MyEntity owner)
        {
            CloseBlockingPhysics();

            Physics = new MyBlockingBody(this, owner);
            Havok.HkShape sh;
            
            sh = new Havok.HkBoxShape(0.5f * new Vector3(0.5f, 0.7f, 0.25f));

            Physics.CreateFromCollisionObject(sh, new Vector3(0, 0.9f, -0.5f), WorldMatrix, null, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoCollisionLayer);
            Physics.MaterialType = m_physItemDef.PhysicalMaterial;

            sh.RemoveReference();

            Physics.Enabled = false;

            m_owner.PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
        }

        void CloseBlockingPhysics()
        {
            if (Physics != null)
            {
                Physics.Close();
                Physics = null;
            }
        }

        #endregion

        public virtual bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if (MySandboxGame.Static.UpdateTime - m_lastHit < m_hitDelay)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            status = MyGunStatusEnum.OK;
            if (IsShooting)
            {
                status = MyGunStatusEnum.Cooldown;
            }
            if (m_owner == null)
            {
                status = MyGunStatusEnum.Failed;
            }
            return status == MyGunStatusEnum.OK;
        }

        public virtual void Shoot(MyShootActionEnum shootAction, VRageMath.Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            m_shotToolAction = null;
            m_wasShooting = false;
            m_swingSoundPlayed = false;
            m_isHit = false;

            if (!string.IsNullOrEmpty(gunAction))
            {
                switch (shootAction)
                {
                    case MyShootActionEnum.PrimaryAction:
                        GetPreferredToolAction(m_toolItemDef.PrimaryActions, gunAction, out m_primaryToolAction, out m_primaryHitCondition);
                        break;
                    case MyShootActionEnum.SecondaryAction:
                        GetPreferredToolAction(m_toolItemDef.SecondaryActions, gunAction, out m_secondaryToolAction, out m_secondaryHitCondition);
                        break;
                }
            }

            switch (shootAction)
            {
                case MyShootActionEnum.PrimaryAction:
                    m_shotToolAction = m_primaryToolAction;
                    m_shotHitCondition = m_primaryHitCondition;
                    break;
                case MyShootActionEnum.SecondaryAction:
                    m_shotToolAction = m_secondaryToolAction;
                    m_shotHitCondition = m_secondaryHitCondition;
                    break;
                default:
                    System.Diagnostics.Debug.Fail("Unknown shooting state!");
                    break;
            }

            MyTuple<ushort, MyStringHash> message;
            if (!string.IsNullOrEmpty(m_shotHitCondition.StatsAction) && m_owner.StatComp != null && !m_owner.StatComp.CanDoAction(m_shotHitCondition.StatsAction, out message))
            {
                if (MySession.Static != null && MySession.Static.LocalCharacter == m_owner && message.Item1 == MyStatLogic.STAT_VALUE_TOO_LOW && message.Item2.String.CompareTo("Stamina") == 0)
                {
                    m_notEnoughStatNotification.SetTextFormatArguments(message.Item2);
                    MyHud.Notifications.Add(m_notEnoughStatNotification);
                }
                return;
            }

            if (m_shotToolAction.HasValue)
            {

                IMyHandToolComponent toolComponent;
                if (m_toolComponents.TryGetValue(m_shotHitCondition.Component, out toolComponent))
                    toolComponent.Shoot();

                MyFrameOption frameOption = MyFrameOption.StayOnLastFrame;

                if (m_shotToolAction.Value.HitDuration == 0)
                    frameOption = MyFrameOption.JustFirstFrame;

                // Stop upper character animation called because character can have some animation set (blocking, ...).
                m_owner.StopUpperCharacterAnimation(0.1f);
                m_owner.PlayCharacterAnimation(m_shotHitCondition.Animation, MyBlendOption.Immediate, frameOption, 0.2f, m_shotHitCondition.AnimationTimeScale, false, null, true);
                m_owner.TriggerCharacterAnimationEvent(m_shotHitCondition.Animation.ToLower(), false);

                if (m_owner.StatComp != null)
                {
                    if (!string.IsNullOrEmpty(m_shotHitCondition.StatsAction))
                        m_owner.StatComp.DoAction(m_shotHitCondition.StatsAction);
                    if (!string.IsNullOrEmpty(m_shotHitCondition.StatsModifier))
                        m_owner.StatComp.ApplyModifier(m_shotHitCondition.StatsModifier);
                }

                Physics.Enabled = m_shotToolAction.Value.Name == BlockId;

                m_lastShot = MySandboxGame.Static.UpdateTime;
            }
        }

     
        private void PlaySound(string soundName)
        {
            MyPhysicalMaterialDefinition def;
            if(MyDefinitionManager.Static.TryGetDefinition<MyPhysicalMaterialDefinition>(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalMaterialDefinition),m_physItemDef.PhysicalMaterial), out def))
            {
                MySoundPair sound;
                if (def.GeneralSounds.TryGetValue(MyStringId.GetOrCompute(soundName), out sound) && !sound.SoundId.IsNull)
                {
                    m_soundEmitter.PlaySound(sound);
                }
                else
                {
                    MySoundPair soundPair;
                    if (!m_toolSounds.TryGetValue(soundName, out soundPair))
                    {
                        soundPair = new MySoundPair(soundName);
                        m_toolSounds.Add(soundName, soundPair);
                    }

                    m_soundEmitter.PlaySound(soundPair);
                }
            }
        }

        public virtual void OnControlAcquired(Sandbox.Game.Entities.Character.MyCharacter owner)
        {
            m_owner = owner;

            InitBlockingPhysics(m_owner);

            foreach (var c in m_toolComponents.Values)
            {
                c.OnControlAcquired(owner);
            }

            this.RaiseEntityEvent(MyStringHash.GetOrCompute("ControlAcquired"), new MyEntityContainerEventExtensions.ControlAcquiredParams(owner));
        }

        void PositionComp_OnPositionChanged(MyPositionComponentBase obj)
        {
        }

        public virtual void OnControlReleased()
        {
            this.RaiseEntityEvent(MyStringHash.GetOrCompute("ControlReleased"), new MyEntityContainerEventExtensions.ControlReleasedParams(m_owner));

            if (m_owner != null)
                m_owner.PositionComp.OnPositionChanged -= PositionComp_OnPositionChanged;

            m_owner = null;

            CloseBlockingPhysics();

            foreach (var c in m_toolComponents.Values)
            {
                c.OnControlReleased();
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(400, 200), String.Format("Primary: {0}, Secondary: {1}", m_primaryToolAction, m_secondaryToolAction), Color.Magenta, 1.0f);

            bool isShooting = IsShooting;

            if (!m_isHit && IsShooting && (MySandboxGame.Static.UpdateTime - m_lastShot > MyTimeSpan.FromSeconds(m_shotToolAction.Value.HitStart)))
            {
                IMyHandToolComponent toolComponent;
                if (m_toolComponents.TryGetValue(m_shotHitCondition.Component, out toolComponent))
                {
                    MyCharacterDetectorComponent detectorComponent = m_owner.Components.Get<MyCharacterDetectorComponent>();
                    if (detectorComponent != null)
                    {
                        if (m_shotToolAction.Value.CustomShapeRadius > 0 && detectorComponent is MyCharacterShapecastDetectorComponent)
                        {
                            var shapeCastComponent = detectorComponent as MyCharacterShapecastDetectorComponent;
                            shapeCastComponent.ShapeRadius = m_shotToolAction.Value.CustomShapeRadius;
                            shapeCastComponent.DoDetectionModel();
                            shapeCastComponent.ShapeRadius = MyCharacterShapecastDetectorComponent.DEFAULT_SHAPE_RADIUS;
                        }

                        if (detectorComponent.DetectedEntity != null)
                        {
                            MyHitInfo hitInfo = new MyHitInfo();
                            hitInfo.Position = detectorComponent.HitPosition;
                            hitInfo.Normal = detectorComponent.HitNormal;
                            hitInfo.ShapeKey = detectorComponent.ShapeKey; 

                            bool isBlock = false;
                            float efficiencyMultiplier = 1.0f;
                            bool canHit = CanHit(toolComponent, detectorComponent, ref isBlock, out efficiencyMultiplier);

                            bool isHit = false;
                            if (canHit)
                            {
                                if (!string.IsNullOrEmpty(m_shotToolAction.Value.StatsEfficiency) && Owner.StatComp != null)
                                {
                                    efficiencyMultiplier *= Owner.StatComp.GetEfficiencyModifier(m_shotToolAction.Value.StatsEfficiency);
                                }

                                float efficiency = m_shotToolAction.Value.Efficiency * efficiencyMultiplier;
                                var tool = detectorComponent.DetectedEntity as MyHandToolBase;
                                if (isBlock && tool != null)
                                    isHit = toolComponent.Hit(tool.Owner, hitInfo, detectorComponent.ShapeKey, efficiency);
                                else
                                    isHit = toolComponent.Hit((MyEntity)detectorComponent.DetectedEntity, hitInfo, detectorComponent.ShapeKey, efficiency);

                                if (isHit && Sync.IsServer && Owner.StatComp != null)
                                {
                                    if (!string.IsNullOrEmpty(m_shotHitCondition.StatsActionIfHit))
                                        Owner.StatComp.DoAction(m_shotHitCondition.StatsActionIfHit);
                                    if (!string.IsNullOrEmpty(m_shotHitCondition.StatsModifierIfHit))
                                        Owner.StatComp.ApplyModifier(m_shotHitCondition.StatsModifierIfHit);
                                }
                            }

                            if (canHit || isBlock)  // real hit is not controlled now - there isn't any server-client synchronization of hit currently and hit is performed only at server
                            {
                                if (!string.IsNullOrEmpty(m_shotToolAction.Value.HitSound))
                                    PlaySound(m_shotToolAction.Value.HitSound);
                                else
                                {
                                    MyStringId collisionType = MyMaterialPropertiesHelper.CollisionType.Hit;
                                    bool showParticles = false;

                                    // If it didn't play the Sound with "Hit", it will try with "Start"
                                    if (MyAudioComponent.PlayContactSound(EntityId, m_hitCue, detectorComponent.HitPosition,
                                        m_toolItemDef.PhysicalMaterial, detectorComponent.HitMaterial))
                                        showParticles = true;
                                    else if(MyAudioComponent.PlayContactSound(EntityId, m_startCue, detectorComponent.HitPosition,
                                            m_toolItemDef.PhysicalMaterial, detectorComponent.HitMaterial))
                                    {
                                        showParticles = true;
                                        collisionType = MyMaterialPropertiesHelper.CollisionType.Start;
                                    }

                                    if (showParticles)
                                        MyMaterialPropertiesHelper.Static.TryCreateCollisionEffect(
                                            collisionType,
                                            detectorComponent.HitPosition,
                                            detectorComponent.HitNormal,
                                            m_toolItemDef.PhysicalMaterial, detectorComponent.HitMaterial);
                                    
                                }

                                this.RaiseEntityEvent(MyStringHash.GetOrCompute("Hit"), new MyEntityContainerEventExtensions.HitParams(MyStringHash.GetOrCompute(m_shotHitCondition.Component), detectorComponent.HitMaterial));
                                m_soundEmitter.StopSound(true);
                            }
                        }
                    }
                }

                m_isHit = true;
            }

            if (!m_swingSoundPlayed && IsShooting && !m_isHit && (MySandboxGame.Static.UpdateTime - m_lastShot > MyTimeSpan.FromSeconds(m_shotToolAction.Value.SwingSoundStart)))
            {
                if (!string.IsNullOrEmpty(m_shotToolAction.Value.SwingSound))
                    PlaySound(m_shotToolAction.Value.SwingSound);
                m_swingSoundPlayed = true;
            }


            if (!isShooting && m_wasShooting)
            {
                m_owner.TriggerCharacterAnimationEvent("stop_tool_action", false);
                m_owner.StopUpperCharacterAnimation(0.4f);
                m_shotToolAction = null;
            }


            m_wasShooting = isShooting;

            if (m_owner != null)
            {
                MatrixD blockingMatrix = MatrixD.CreateWorld(((MyEntity)m_owner.CurrentWeapon).PositionComp.GetPosition(), m_owner.WorldMatrix.Forward, m_owner.WorldMatrix.Up);

                ((MyBlockingBody)Physics).SetWorldMatrix(blockingMatrix);
            }


            foreach (var c in m_toolComponents.Values)
            {
                c.Update();
            }
        }

        protected bool CanHit(IMyHandToolComponent toolComponent, MyCharacterDetectorComponent detectorComponent, ref bool isBlock, out float hitEfficiency)
        {
            bool canHit = true;
            hitEfficiency = 1.0f;
            MyTuple<ushort, MyStringHash> message;
            // TODO(GoodAI/HonzaS): Take care when merging this line.
            // The null check was not encountered with hand tools different from the reward/punishment tool.
            if (detectorComponent.HitBody != null && detectorComponent.HitBody.UserObject is MyBlockingBody)
            {
                var blocking = detectorComponent.HitBody.UserObject as MyBlockingBody;
                if (blocking.HandTool.IsBlocking && blocking.HandTool.m_owner.StatComp != null 
                    && blocking.HandTool.m_owner.StatComp.CanDoAction(blocking.HandTool.m_shotHitCondition.StatsActionIfHit, out message))
                {
                    blocking.HandTool.m_owner.StatComp.DoAction(blocking.HandTool.m_shotHitCondition.StatsActionIfHit);
                    if (!string.IsNullOrEmpty(blocking.HandTool.m_shotHitCondition.StatsModifierIfHit))
                        blocking.HandTool.m_owner.StatComp.ApplyModifier(blocking.HandTool.m_shotHitCondition.StatsModifierIfHit);
                    isBlock = true;

                    if (!string.IsNullOrEmpty(blocking.HandTool.m_shotToolAction.Value.StatsEfficiency))
                    {
                        hitEfficiency = 1.0f - blocking.HandTool.m_owner.StatComp.GetEfficiencyModifier(blocking.HandTool.m_shotToolAction.Value.StatsEfficiency);
                    }
                    canHit = hitEfficiency > 0.0f;
                    MyEntityContainerEventExtensions.RaiseEntityEventOn(blocking.HandTool, MyStringHash.GetOrCompute("Hit"), new MyEntityContainerEventExtensions.HitParams(MyStringHash.GetOrCompute("Block"), this.PhysicalItemDefinition.Id.SubtypeId));
                }
            }
            if (!canHit)
            {
                hitEfficiency = 0.0f;
                return canHit;
            }

            if (!string.IsNullOrEmpty(m_shotHitCondition.StatsActionIfHit))
            {
                canHit = m_owner.StatComp != null && m_owner.StatComp.CanDoAction(m_shotHitCondition.StatsActionIfHit, out message);
                if (!canHit)
                {
                    hitEfficiency = 0.0f;
                    return canHit;
                }
            }

            float hitDistance = Vector3.Distance(detectorComponent.HitPosition, detectorComponent.StartPosition);
            canHit = hitDistance <= m_toolItemDef.HitDistance;
            if (!canHit)
            {
                hitEfficiency = 0.0f;
                return canHit;
            }

            // checking of player factions
            MyEntity attacker = m_owner.Entity;
            long attackerPlayerId = m_owner.GetPlayerIdentityId();
            var localPlayerFaction = MySession.Static.Factions.TryGetPlayerFaction(attackerPlayerId) as MyFaction;
            if (localPlayerFaction != null && !localPlayerFaction.EnableFriendlyFire )
            {
                // friendy fire isn't enabled in attacker faction
                IMyEntity otherPlayerEntity = detectorComponent.DetectedEntity;
                MyCharacter otherPlayer = otherPlayerEntity as MyCharacter;
                if ( otherPlayer != null )
                {
                    bool sameFaction = localPlayerFaction.IsMember(otherPlayer.GetPlayerIdentityId());
                    canHit = !sameFaction;
                    hitEfficiency = canHit ? hitEfficiency : 0.0f;
                }
            }
            return canHit;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            
            GetMostEffectiveToolAction(m_toolItemDef.PrimaryActions, out m_primaryToolAction, out m_primaryHitCondition);
            GetMostEffectiveToolAction(m_toolItemDef.SecondaryActions, out m_secondaryToolAction, out m_secondaryHitCondition);

            if (MySession.Static.ControlledEntity == m_owner)
            {
                MyCharacterDetectorComponent detectorComponent = m_owner.Components.Get<MyCharacterDetectorComponent>();

                bool entityDetected = false;
                float hitDistance = float.MaxValue;
                if (detectorComponent != null)
                {
                    entityDetected = detectorComponent.DetectedEntity != null;
                    hitDistance = Vector3.Distance(detectorComponent.HitPosition, PositionComp.GetPosition());
                }

                if (hitDistance > m_toolItemDef.HitDistance)
                    entityDetected = false;

                if (m_primaryToolAction != null && (m_primaryHitCondition.EntityType != null || entityDetected))
                {
                    MyHud.Crosshair.ChangeDefaultSprite(m_primaryToolAction.Value.Crosshair);
                }
                else if (m_secondaryToolAction != null && (m_secondaryHitCondition.EntityType != null || entityDetected))
                {
                    MyHud.Crosshair.ChangeDefaultSprite(m_secondaryToolAction.Value.Crosshair);
                }
                else
                {
                    MyHud.Crosshair.ChangeDefaultSprite(MyHudTexturesEnum.crosshair);
                }
            }
        }

        void GetMostEffectiveToolAction(List<MyToolActionDefinition> toolActions, out MyToolActionDefinition? bestAction, out MyToolHitCondition bestCondition)
        {
            MyCharacterDetectorComponent detectorComponent = m_owner.Components.Get<MyCharacterDetectorComponent>();
            IMyEntity hitEntity = null;
            uint shapeKey = 0;

            if (detectorComponent != null)
            {
                hitEntity = detectorComponent.DetectedEntity;
                shapeKey = detectorComponent.ShapeKey;

                float hitDistance = Vector3.Distance(detectorComponent.HitPosition, detectorComponent.StartPosition);

                if (hitDistance > m_toolItemDef.HitDistance)
                    hitEntity = null;
            }

            bestAction = null;
            bestCondition = new MyToolHitCondition();

            //Get most effective action
            foreach (var action in toolActions)
            {
                if (action.HitConditions != null)
                {
                    foreach (var condition in action.HitConditions)
                    {
                        if (condition.EntityType != null)
                        {
                            if (hitEntity != null)
                            {
                                string availableState = GetStateForTarget((MyEntity)hitEntity, shapeKey, condition.Component);
                                if (condition.EntityType.Contains(availableState))
                                {
                                    bestAction = action;
                                    bestCondition = condition;
                                    return;
                                }
                            }
                            else
                                continue;
                        }
                        else
                        {
                            bestAction = action;
                            bestCondition = condition;
                            return;
                        }
                    }
                }
            }
        }

        void GetPreferredToolAction(List<MyToolActionDefinition> toolActions, string name, out MyToolActionDefinition? bestAction, out MyToolHitCondition bestCondition)
        {
            bestAction = null;
            bestCondition = new MyToolHitCondition();

            MyStringId nameId = MyStringId.GetOrCompute(name);

            foreach (var action in toolActions)
            {
                if (action.HitConditions.Length > 0)
                {
                    if (action.Name == nameId)
                    {
                        bestAction = action;
                        bestCondition = action.HitConditions[0];
                        return;
                    }
                }
            }
        }

        public void DrawHud(IMyCameraController camera, long playerId)
		{
            if (m_primaryToolAction.HasValue)
            {
                if (m_toolComponents.ContainsKey(m_primaryHitCondition.Component))
                    m_toolComponents[m_primaryHitCondition.Component].DrawHud();
            }
		}

        private string GetStateForTarget(MyEntity targetEntity, uint shapeKey, string actionType)
        {
            if (targetEntity == null)
                return null;

            string targetState = null;
            IMyHandToolComponent comp;
            if (m_toolComponents.TryGetValue(actionType, out comp))
            {
                targetState = comp.GetStateForTarget(targetEntity, shapeKey);
                if (!string.IsNullOrEmpty(targetState))
                    return targetState;
            }

            foreach (var c in m_toolComponents)
            {
                targetState = c.Value.GetStateForTarget(targetEntity, shapeKey);
                if (!string.IsNullOrEmpty(targetState))
                    return targetState;
            }

            return null;
        }


        #region Overrides

        public VRageMath.Vector3 DirectionToTarget(VRageMath.Vector3D target)
        {
            return target;
        }

        public virtual void EndShoot(MyShootActionEnum action)
        {
            if (m_shotToolAction.HasValue)
            {
                if (m_shotToolAction.Value.HitDuration == 0)
                    m_shotToolAction = null;
            }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status) { }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status) { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status) { }

        public int GetAmmunitionAmount()
        {
            return 0;
        }

        #endregion

        public void StopShooting(MyEntity attacker)
        {
            Debug.Assert(Sync.IsServer);

            if (!IsShooting)
                return;

            float delaySec = 0;
            MyCharacter attackerCharacter = attacker as MyCharacter;
            if (attackerCharacter != null)
            {
                MyHandToolBase attackerHandTool = attackerCharacter.CurrentWeapon as MyHandToolBase;
                if (attackerHandTool != null && attackerHandTool.m_shotToolAction != null)
                {
                    delaySec = attackerHandTool.m_shotToolAction.Value.HitDuration - (float)(MySandboxGame.Static.UpdateTime - attackerHandTool.m_lastShot).Seconds;
                }
            }

            float attackDelay = delaySec > 0 ? delaySec : AFTER_SHOOT_HIT_DELAY;

            // Send stop to clients
            MyMultiplayer.RaiseStaticEvent(s => MyHandToolBase.StopShootingRequest, EntityId, attackDelay);

            // Set stop on server
            StopShooting(attackDelay);
        }

        internal void StopShooting(float hitDelaySec)
        {
            if (!IsShooting)
                return;

            m_lastHit = MySandboxGame.Static.UpdateTime;
            m_hitDelay = MyTimeSpan.FromSeconds(hitDelaySec);

            m_owner.PlayCharacterAnimation(m_shotHitCondition.Animation, MyBlendOption.Immediate, MyFrameOption.JustFirstFrame, 0.2f, m_shotHitCondition.AnimationTimeScale, false, null, true);
            m_shotToolAction = null;

            m_wasShooting = false;
        }

        [Event, Reliable, Broadcast]
        private static void StopShootingRequest(long entityId, float attackDelay)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(entityId, out entity);
            MyHandToolBase handTool = entity as MyHandToolBase;
            if (handTool == null)
                return;

            handTool.StopShooting(attackDelay);
        }

        public int CurrentAmmunition { set; get; }
        public int CurrentMagazineAmmunition { set; get; }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }
    }
}

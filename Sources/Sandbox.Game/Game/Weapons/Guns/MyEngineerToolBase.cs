using System.Globalization;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.World;

using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons.Guns;
using System.Diagnostics;
using Sandbox.Common;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;

namespace Sandbox.Game.Weapons
{
    public abstract class MyEngineerToolBase : MyEntity, IMyHandheldGunObject<MyToolBase>, IMyEngineerToolBase
    {
        public static float GLARE_SIZE = 0.068f;
        /// <summary>
        /// Default reach distance of a tool. It is modified by "distance modifier" defined in definition of a tool.
        /// </summary>
        protected float DEFAULT_REACH_DISTANCE = 1.8f;

		public bool IsDeconstructor { get { return false; } }
        public int ToolCooldownMs { get; private set; }
        public int EffectStopMs
        {
            get
            {
                return ToolCooldownMs * 2;    
            }
        }

        protected string EffectId = "Welder";
        protected float EffectScale = 1f;

        protected bool HasPrimaryEffect = true;

        protected bool HasSecondaryEffect = false;
        protected MyParticleEffectsIDEnum SecondaryEffectId = MyParticleEffectsIDEnum.Dummy;
        protected Vector4 SecondaryLightColor = new Vector4(0.4f, 0.5f, 1.0f, 1.0f);
        protected float SecondaryLightFalloff = 2;
        protected float SecondaryLightRadius = 7;
        protected float SecondaryLightIntensityLower = 0.4f;
        protected float SecondaryLightIntensityUpper = 0.5f;
        protected float SecondaryLightGlareSize = GLARE_SIZE;

        // Primary = 1; Secondary = 2; No effect = 0
        protected int CurrentEffect = 0;
        private int m_previousEffect = 0;

        protected MyEntity3DSoundEmitter m_soundEmitter;

        protected MyCharacter Owner;

        protected MyToolBase m_gunBase;
        public MyToolBase GunBase  { get { return m_gunBase;}}

        int m_lastTimeShoot;
        protected bool m_activated;

        MyParticleEffect m_toolEffect;
        MyParticleEffect m_toolSecondaryEffect;
        MyLight m_toolEffectLight;


        public Vector3I TargetCube { get { return m_raycastComponent != null && m_raycastComponent.HitBlock != null ? m_raycastComponent.HitBlock.Position : Vector3I.Zero; } }

        public bool HasHitBlock { get { return m_raycastComponent.HitBlock != null; } }

        private int m_lastMarkTime = -1;
        private int m_markedComponent = -1;

        private bool m_tryingToShoot;

        private bool m_wasPowered;

        protected MyCasterComponent m_raycastComponent;

		private MyResourceSinkComponent m_sinkComp;
		public MyResourceSinkComponent SinkComp
		{
			get { return m_sinkComp; }
            set
            {
                if (Components.Contains(typeof(MyResourceSinkComponent)))
                    Components.Remove<MyResourceSinkComponent>();

                Components.Add<MyResourceSinkComponent>(value);
                m_sinkComp = value;
            }
		}

        public bool IsShooting
        {
            get { return m_activated; }
        }

        public bool ForceAnimationInsteadOfIK { get { return false; } }

        public bool IsBlocking
        {
            get { return false; }
        }

        private int m_shootFrameCounter = 0;
        public bool IsPreheated
        {
            get { return m_shootFrameCounter >= 3; }
        }

        public Vector3 SensorDisplacement { get; set; }

        protected MyInventory CharacterInventory { get; private set; }

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; protected set; }
        
        public float BackkickForcePerSecond
        {
            get { return 0; }
        }

        public float ShakeAmount
        {
            get;
            protected set;
        }
        protected bool HasCubeHighlight { get; set; }
        public Color HighlightColor { get; set; }
		public string HighlightMaterial { get; set; }

        public bool EnabledInWorldRules { get { return true; } }

        NumberFormatInfo m_oneDecimal = new NumberFormatInfo() { NumberDecimalDigits = 1, PercentDecimalDigits = 1 };

        protected MyHandItemDefinition m_handItemDef;
        protected MyPhysicalItemDefinition m_physItemDef;

        protected float m_speedMultiplier = 1f;
        protected float m_distanceMultiplier = 1f;

        public bool CanBeDrawn()
        {
            return (Owner != null && Owner == MySession.Static.ControlledEntity && m_raycastComponent.HitCubeGrid != null && m_raycastComponent.HitCubeGrid != null && HasCubeHighlight && MyFakes.HIDE_ENGINEER_TOOL_HIGHLIGHT == false);
        }

        public MyEngineerToolBase(int cooldownMs)
        {
            ToolCooldownMs = cooldownMs;
            m_activated = false;
            m_wasPowered = false;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Render.NeedsDraw = true;

            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            Render = new Components.MyRenderComponentEngineerTool();
            AddDebugRenderComponent(new MyDebugRenderComponentEngineerTool(this));

        }

        public void Init(MyObjectBuilder_EntityBase builder,MyDefinitionId id)
        {
            Init(builder, MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(id));
        }
        public void Init(MyObjectBuilder_EntityBase builder, MyHandItemDefinition definition)
        {
            m_handItemDef = definition;
            System.Diagnostics.Debug.Assert(definition != null, "Missing definition for tool!");
            if (definition != null)
            {
                m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemForHandItem(definition.Id);
                m_gunBase = new MyToolBase(m_handItemDef.MuzzlePosition, WorldMatrix);
            }
            else
            {
                m_gunBase = new MyToolBase(Vector3.Zero, WorldMatrix);
            }
            base.Init(builder);

            if (PhysicalObject != null)
            {
                PhysicalObject.GunEntity = builder;
            }
            if ((definition as MyEngineerToolBaseDefinition) != null)
            {
                m_speedMultiplier = (m_handItemDef as MyEngineerToolBaseDefinition).SpeedMultiplier;
                m_distanceMultiplier = (definition as MyEngineerToolBaseDefinition).DistanceMultiplier;
            }
            MyDrillSensorRayCast raycaster = new MyDrillSensorRayCast(0f, DEFAULT_REACH_DISTANCE * m_distanceMultiplier);

            m_raycastComponent = new MyCasterComponent(raycaster);
            m_raycastComponent.SetPointOfReference(m_gunBase.GetMuzzleWorldPosition());
            Components.Add<MyCasterComponent>(m_raycastComponent);

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute("Utility"),
                MyEnergyConstants.REQUIRED_INPUT_ENGINEERING_TOOL,
                CalculateRequiredPower);
	        SinkComp = sinkComp;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
        }

        protected virtual bool ShouldBePowered()
        {
            return m_tryingToShoot;
        }

        protected float CalculateRequiredPower()
        {
            return ShouldBePowered() ? SinkComp.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0.0f;
        }

        private void UpdatePower()
        {
            bool shouldBePowered = ShouldBePowered();
            if (shouldBePowered != m_wasPowered)
            {
                m_wasPowered = shouldBePowered;
				SinkComp.Update();
            }
        }

        protected IMyDestroyableObject GetTargetDestroyable()
        {
            return m_raycastComponent.HitDestroyableObj;
        }

        /// <summary>
        /// Action distance is taken into account
        /// </summary>
        protected MySlimBlock GetTargetBlock()
        {
            if (ReachesCube() && m_raycastComponent.HitCubeGrid != null)
            {
                return m_raycastComponent.HitBlock;
            }
            return null;
        }

        public MyCubeGrid GetTargetGrid()
        {
            return m_raycastComponent.HitCubeGrid;
        }

        protected bool ReachesCube()
        {
            return m_raycastComponent.HitBlock != null;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
        }

        public override void OnRemovedFromScene(object source)
        {
            RemoveHudInfo();
            base.OnRemovedFromScene(source);
            StopSecondaryEffect();
            StopEffect();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            //MyRenderProxy.DebugDrawSphere(m_gunBase.PositionMuzzleWorld, 0.2f, new Vector3(1, 0, 0), 1.0f, true);


            if (Owner == null)
                return;

            Vector3 weaponLocalPosition = Owner.GetLocalWeaponPosition();
            Vector3D localDummyPosition = m_gunBase.GetMuzzleLocalPosition();
            MatrixD weaponWorld = WorldMatrix;
            Vector3D localDummyPositionRotated;
            Vector3D.Rotate(ref localDummyPosition, ref weaponWorld, out localDummyPositionRotated);
            m_raycastComponent.SetPointOfReference(Owner.PositionComp.GetPosition() + weaponLocalPosition + localDummyPositionRotated);
            
			SinkComp.Update();

            if (IsShooting && !SinkComp.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                EndShoot(MyShootActionEnum.PrimaryAction);
            }

            UpdateEffect();
            CheckEffectType();

			if (Owner != null && Owner.ControllerInfo.IsLocallyHumanControlled())
			{
				if (MySession.Static.SurvivalMode && (MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.Spectator || MyFinalBuildConstants.IS_OFFICIAL))
				{
					var character = ((MyCharacter)this.CharacterInventory.Owner);
					MyCubeBuilder.Static.MaxGridDistanceFrom = character.PositionComp.GetPosition() + character.WorldMatrix.Up * 1.8f;
				}
				else
				{
					MyCubeBuilder.Static.MaxGridDistanceFrom = null;
				}
			}

            //MyTrace.Watch("MyEngineerToolBase.RequiredPowerInput", RequiredPowerInput);            
        }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }

        private void WorldPositionChanged(object source)
        {
            m_gunBase.OnWorldPositionChanged(PositionComp.WorldMatrix);
            UpdateSensorPosition();
        }

        public void UpdateSensorPosition()
        {
            if (Owner != null)
            {
                Debug.Assert(Owner != null && Owner is MyCharacter, "An engineer tool is not held by a character");
                MyCharacter character = Owner as MyCharacter;

                MatrixD sensorWorldMatrix = MatrixD.Identity;
                sensorWorldMatrix.Translation = character.WeaponPosition.LogicalPositionWorld;
                sensorWorldMatrix.Right = character.WorldMatrix.Right;
                sensorWorldMatrix.Forward = character.WeaponPosition.LogicalOrientationWorld;
                sensorWorldMatrix.Up = Vector3.Cross(sensorWorldMatrix.Right, sensorWorldMatrix.Forward);
                
                // MZ: removing code requiring synchronization

                //if (character.ControllerInfo.IsLocallyControlled())
                //{
                //    sensorWorldMatrix = character.GetHeadMatrix(false, true);
                //    character.SyncHeadToolTransform(ref sensorWorldMatrix);
                //}
                //else
                //{
                //    sensorWorldMatrix = character.GetSyncedToolTransform();
                //}

                // VRageRender.MyRenderProxy.DebugDrawAxis(sensorWorldMatrix, 0.2f, false);
                m_raycastComponent.OnWorldPosChanged(ref sensorWorldMatrix);
            }
        }
       
        public virtual bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if (action == MyShootActionEnum.PrimaryAction)
            {
                if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot < ToolCooldownMs)
                {
                    status = MyGunStatusEnum.Cooldown;
                    return false;
                }
                status = MyGunStatusEnum.OK;
                return true;
            }

            status = MyGunStatusEnum.Failed;
            return false;
        }

        public virtual void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction)
            {
                return;
            }

            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_shootFrameCounter++;
            m_tryingToShoot = true;
			SinkComp.Update();
            if (!SinkComp.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                CurrentEffect = 0;
                return;
            }

            m_activated = true;

            var targetBlock = GetTargetBlock();
            if (targetBlock == null)
            {
                CurrentEffect = 2;
                ShakeAmount = m_handItemDef.ShakeAmountNoTarget;
            }
            
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (targetBlock != null)
            {
                ShakeAmount = m_handItemDef.ShakeAmountTarget;
                CurrentEffect = 1;
            }
            return;
        }

        public virtual void EndShoot(MyShootActionEnum action)
        {
            if (action == MyShootActionEnum.PrimaryAction)
            {
                CurrentEffect = 0;
                StopLoopSound();
                ShakeAmount = 0.0f;
                m_tryingToShoot = false;
				SinkComp.Update();
                m_activated = false;
                m_shootFrameCounter = 0;
            }
        }

        public virtual void OnFailShoot(MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.Failed)
            {
                CurrentEffect = 2;
            }
        }

        protected virtual void StartLoopSound(bool effect) { }
        protected virtual void StopLoopSound() { }
        protected virtual void StopSound() { }

        protected virtual MatrixD GetEffectMatrix(float muzzleOffset)
        {
            if (m_raycastComponent.HitCubeGrid == null || m_raycastComponent.HitBlock == null)
            {
                return MatrixD.CreateWorld(m_gunBase.GetMuzzleWorldPosition(), PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
            }

            var aimPoint = m_raycastComponent.HitPosition;
            var dist = Vector3.Dot(aimPoint - m_gunBase.GetMuzzleWorldPosition(), PositionComp.WorldMatrix.Forward);
            var target = m_gunBase.GetMuzzleWorldPosition() + PositionComp.WorldMatrix.Forward * (dist * muzzleOffset);

            return MatrixD.CreateWorld(dist > 0 && muzzleOffset == 0 ? m_gunBase.GetMuzzleWorldPosition() : target, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
        }

        void CheckEffectType()
        {
            if (m_previousEffect != 0 && m_toolEffect == null)
                m_previousEffect = 0;

            if (CurrentEffect != m_previousEffect)
            {
                if (m_previousEffect != 0)
                {
                    StopEffect();
                }
                m_previousEffect = 0;

                if (CurrentEffect == 0) {
                    return;
                }

                bool canSeeEffect = MySector.MainCamera.GetDistanceFromPoint(PositionComp.GetPosition()) < 150;
                if (canSeeEffect)
                {
                    if (CurrentEffect == 1 && HasPrimaryEffect)
                    {
                        StartEffect();
                        m_previousEffect = 1;
                        return;
                    }
                    else if (CurrentEffect == 2 && HasSecondaryEffect)
                    {
                        StartSecondaryEffect();
                        m_previousEffect = 2;
                        return;
                    }
                }
            }
        }

        void StartEffect()
        {
            StopEffect();
            if (!string.IsNullOrEmpty(EffectId))
            {
                MyParticlesManager.TryCreateParticleEffect(EffectId, out m_toolEffect);
                if (m_toolEffect != null)
                    m_toolEffect.UserScale = EffectScale;
                m_toolEffectLight = CreatePrimaryLight();
            }
            UpdateEffect();
        }

        protected virtual MyLight CreatePrimaryLight()
        {
            MyLight light = MyLights.AddLight();
            light.Start(MyLight.LightTypeEnum.PointLight, Vector3.Zero, m_handItemDef.LightColor, m_handItemDef.LightFalloff, m_handItemDef.LightRadius);
            light.GlareMaterial = "GlareWelder";
            light.GlareOn = light.LightOn;
            light.GlareQuerySize = 1;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            return light;
        }

        void StartSecondaryEffect()
        {
            StopSecondaryEffect();
            MyParticlesManager.TryCreateParticleEffect((int)SecondaryEffectId, out m_toolSecondaryEffect);            
            m_toolEffectLight = CreateSecondaryLight();
            UpdateEffect();
        }

        protected virtual MyLight CreateSecondaryLight()
        {
            MyLight light = MyLights.AddLight();
            light.Start(MyLight.LightTypeEnum.PointLight, Vector3.Zero, SecondaryLightColor, SecondaryLightFalloff, SecondaryLightRadius);
            light.GlareMaterial = "GlareWelder";
            light.GlareOn = light.LightOn;
            light.GlareQuerySize = 1;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            return light;
        }

        private void UpdateEffect()
        {
            // Disable active effect when tool is too far (this is so that effects of other players in MP look better)
            if (CurrentEffect == 1 && m_raycastComponent.HitCubeGrid == null)
            {
                CurrentEffect = 2;
            }
            if (CurrentEffect == 2 && (m_raycastComponent.HitCharacter != null || m_raycastComponent.HitEnvironmentSector != null))
                CurrentEffect = 1;

            //MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Updating effect!", Color.Red, 1.0f);
            switch (CurrentEffect)
            {
                case 0:
                    if (m_soundEmitter.IsPlaying)
                        StopLoopSound();
                    break;
                case 1:
                    //MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 30.0f), "Primary", Color.Red, 1.0f);
                    StartLoopSound(true);
                    break;
                case 2:
                    //MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f), "Secondary", Color.Red, 1.0f);
                    StartLoopSound(false);
                    break;
            }

            const float lightMuzzleOffset = 0.0f;
            const float particleMuzzleOffset = 0.5f;            

            if (m_toolEffect != null)
            {
                m_toolEffect.WorldMatrix = GetEffectMatrix(particleMuzzleOffset);
            }
            if (m_toolSecondaryEffect != null)
            {
                m_toolSecondaryEffect.WorldMatrix = GetEffectMatrix(particleMuzzleOffset);
            }

            if (m_toolEffectLight != null)
            {
                m_toolEffectLight.Position = GetEffectMatrix(lightMuzzleOffset).Translation;
                if (CurrentEffect == 1)
                {
                    m_toolEffectLight.Intensity = MyUtils.GetRandomFloat(m_handItemDef.LightIntensityLower, m_handItemDef.LightIntensityUpper);
                    m_toolEffectLight.GlareIntensity = m_toolEffectLight.Intensity;
                    m_toolEffectLight.GlareSize = m_toolEffectLight.Intensity * m_handItemDef.LightGlareSize;
                }
                else
                {
                    m_toolEffectLight.Intensity = MyUtils.GetRandomFloat(SecondaryLightIntensityLower, SecondaryLightIntensityUpper);
                    m_toolEffectLight.GlareIntensity = m_toolEffectLight.Intensity;
                    m_toolEffectLight.GlareSize = m_toolEffectLight.Intensity * SecondaryLightGlareSize;
                }
                m_toolEffectLight.UpdateLight();
            }
        }

        void StopEffect()
        {
            if (m_toolEffect != null)
            {
                m_toolEffect.Stop();
                m_toolEffect = null;
            }
            if (m_toolEffectLight != null)
            {
                MyLights.RemoveLight(m_toolEffectLight);
                m_toolEffectLight = null;
            }
        }

        void StopSecondaryEffect()
        {
            if (m_toolSecondaryEffect != null)
            {
                m_toolSecondaryEffect.Stop();
                m_toolSecondaryEffect = null;
            }
        }

        protected override void Closing()
        {
            StopEffect();
            StopSecondaryEffect();
            StopLoopSound();
            base.Closing();
        }

        protected abstract void AddHudInfo();

        protected abstract void RemoveHudInfo();

        public virtual void OnControlAcquired(MyCharacter owner)
        {
            Owner = owner;
            System.Diagnostics.Debug.Assert(Owner.GetInventory() as MyInventory != null, "Null or unexpected inventory type returned!");
            CharacterInventory = Owner.GetInventory() as MyInventory;

            if (owner.ControllerInfo.IsLocallyHumanControlled())
            {
                AddHudInfo();
            }
        }

        public virtual void OnControlReleased()
        {
            RemoveHudInfo();
            Owner = null;
            CharacterInventory = null;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Recenter();

            DrawHud();
            UpdateHudComponentMark();
        }

        protected virtual void DrawHud()
        {
            MyHud.BlockInfo.Visible = false;

            if (m_raycastComponent.HitCubeGrid == null || m_raycastComponent.HitBlock == null)
                return;

            var block = m_raycastComponent.HitBlock;
            if (block == null)
                return;

            // Get first block from compound.
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                if (compoundBlock.GetBlocksCount() > 0)
                {
                    block = compoundBlock.GetBlocks().First();
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            MyHud.BlockInfo.Visible = true;

            MyHud.BlockInfo.MissingComponentIndex = -1;
            MyHud.BlockInfo.BlockName = block.BlockDefinition.DisplayNameText;
            MyHud.BlockInfo.BlockIcons = block.BlockDefinition.Icons;
            MyHud.BlockInfo.BlockIntegrity = block.Integrity / block.MaxIntegrity;
            MyHud.BlockInfo.CriticalIntegrity = block.BlockDefinition.CriticalIntegrityRatio;
            MyHud.BlockInfo.CriticalComponentIndex = block.BlockDefinition.CriticalGroup;
            MyHud.BlockInfo.OwnershipIntegrity = block.BlockDefinition.OwnershipIntegrityRatio;
            MyHud.BlockInfo.BlockBuiltBy = block.BuiltBy;

            MySlimBlock.SetBlockComponents(MyHud.BlockInfo, block);
        }

        protected void UnmarkMissingComponent()
        {
            m_lastMarkTime = -1;
            m_markedComponent = -1;
        }

        protected void MarkMissingComponent(int componentIdx)
        {
            m_lastMarkTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_markedComponent = componentIdx;
        }

        private void UpdateHudComponentMark()
        {
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastMarkTime > 2500)
            {
                UnmarkMissingComponent();
                return;
            }

            MyHud.BlockInfo.MissingComponentIndex = m_markedComponent;
        }

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDef.Id; }
        }
        
        int IMyGunObject<MyToolBase>.ShootDirectionUpdateTime
        {
            get { return 0; }
        }
 
        public Vector3 DirectionToTarget(Vector3D target)
        {
            return PositionComp.WorldMatrix.Forward;
        }

        public virtual void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public virtual void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public virtual void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (action == MyShootActionEnum.PrimaryAction && status == MyGunStatusEnum.Failed)
            {
                CurrentEffect = 2;
            }
        }

        public int GetAmmunitionAmount()
        {
            return 0;
        }

        public IMyDestroyableObject m_targetDestroyable { get; set; }

        public MyPhysicalItemDefinition PhysicalItemDefinition
        {
            get { return m_physItemDef; }
        }

        public int CurrentAmmunition { set; get; }
        public int CurrentMagazineAmmunition { set; get; }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_EntityBase ob = base.GetObjectBuilder(copy);

            ob.SubtypeName = m_handItemDef.Id.SubtypeName;
            return ob;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.Screens;
using Sandbox.Game.World;

using VRageMath;
using VRageRender;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.GameSystems.Electricity;
using System.Diagnostics;
using VRage.Trace;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.ModAPI;

namespace Sandbox.Game.Weapons
{
    public abstract class MyEngineerToolBase : MyEntity, IMyHandheldGunObject<MyToolBase>, IMyPowerConsumer
    {
        public static float GLARE_SIZE = 0.068f;

		public bool IsDeconstructor { get { return false; } }
        public int ToolCooldownMs { get; private set; }
        public int EffectStopMs
        {
            get
            {
                return ToolCooldownMs * 2;    
            }
        }

        protected MyParticleEffectsIDEnum EffectId = MyParticleEffectsIDEnum.Welder;

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
        protected float m_toolActionDistance;

        int m_lastTimeShoot;
        protected bool m_activated;

        MyParticleEffect m_toolEffect;
        MyParticleEffect m_toolSecondaryEffect;
        MyLight m_toolEffectLight;

        protected MyCubeGrid m_targetGrid;
        protected MyFloatingObject m_targetFloatingObject;
        protected MyCharacter m_targetCharacter;
        protected Vector3I m_targetCube;
        public Vector3I TargetCube { get {  return m_targetCube; } }
        protected float m_targetDistanceSq;
        protected Vector3D m_targetPosition;

        private int m_lastMarkTime = -1;
        private int m_markedComponent = -1;

        private MyDrillSensorBase m_sensor;
        public MyDrillSensorBase Sensor { get { return m_sensor; } }
        private bool m_tryingToShoot;

        private bool m_wasPowered;
        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public bool IsShooting
        {
            get { return m_activated; }
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

        public bool EnabledInWorldRules { get { return true; } }

        NumberFormatInfo m_oneDecimal = new NumberFormatInfo() { NumberDecimalDigits = 1, PercentDecimalDigits = 1 };

        MyHandItemDefinition m_handItemDef;
        MyPhysicalItemDefinition m_physItemDef;

        public bool CanBeDrawn()
        {
            return (Owner != null && Owner == MySession.ControlledEntity && m_targetGrid != null && m_targetCube != null && HasCubeHighlight && MyFakes.HIDE_ENGINEER_TOOL_HIGHLIGHT == false);
        }

        public MyEngineerToolBase(MyHandItemDefinition definition, float toolDistance, int cooldownMs)
        {
            ToolCooldownMs = cooldownMs;
            m_toolActionDistance = toolDistance;
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
            m_activated = false;
            m_wasPowered = false;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Render.NeedsDraw = true;

            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            Render = new Components.MyRenderComponentEngineerTool();
            AddDebugRenderComponent(new MyDebugRenderComponentEngineerTool(this));

        }

        public override void Init(MyObjectBuilder_EntityBase builder)
        {
            base.Init(builder);

            if (PhysicalObject != null)
            {
                PhysicalObject.GunEntity = builder;
            }

            m_sensor = new MyDrillSensorRayCast(0f, 1.8f);

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                MyEnergyConstants.REQUIRED_INPUT_ENGINEERING_TOOL,
                CalculateRequiredPower);
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
        }

        protected virtual bool ShouldBePowered()
        {
            return m_tryingToShoot;
        }

        protected float CalculateRequiredPower()
        {
            return ShouldBePowered() ? PowerReceiver.MaxRequiredInput : 0.0f;
        }

        private void UpdatePower()
        {
            bool shouldBePowered = ShouldBePowered();
            if (shouldBePowered != m_wasPowered)
            {
                m_wasPowered = shouldBePowered;
                PowerReceiver.Update();
            }
        }

        protected IMyDestroyableObject GetTargetDestroyable()
        {
            return m_targetDestroyable;
        }

        /// <summary>
        /// Action distance is taken into account
        /// </summary>
        protected MySlimBlock GetTargetBlock()
        {
            if (ReachesCube(m_toolActionDistance) && m_targetGrid != null)
            {
                return m_targetGrid.GetCubeBlock(m_targetCube);
            }
            return null;
        }

        public MyCubeGrid GetTargetGrid()
        {
            return m_targetGrid;
        }

        protected bool ReachesCube(float distance)
        {
            return m_targetDistanceSq < distance * distance;
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

            m_targetGrid = null;
            m_targetDestroyable = null;
            m_targetFloatingObject = null;
            m_targetCharacter = null;

            if (Owner == null)
                return;

            var entitiesInRange = m_sensor.EntitiesInRange;
            int closestEntityIndex = 0;
            float closestDistance = float.MaxValue;
            if (entitiesInRange != null && entitiesInRange.Count > 0)
            {
                int i = 0;
                foreach (var entity in entitiesInRange.Values)
                {
                    var targetGrid = entity.Entity as MyCubeGrid;
                    var distanceSq = (float)Vector3D.DistanceSquared(entity.DetectionPoint, m_gunBase.GetMuzzleWorldPosition());
                    if (entity.Entity.Physics != null && entity.Entity.Physics.Enabled)
                    {
                        if (distanceSq < closestDistance)
                        {
                            m_targetGrid = targetGrid;
                            m_targetDistanceSq = (float)distanceSq;
                            m_targetDestroyable = entity.Entity as IMyDestroyableObject;
                            m_targetFloatingObject = entity.Entity as MyFloatingObject;
                            m_targetCharacter = entity.Entity as MyCharacter;
                            closestDistance = m_targetDistanceSq;
                            closestEntityIndex = i;
                        }
                    }
                    ++i;
                }
            }

            if (m_targetGrid != null)
            {
                m_targetPosition = entitiesInRange.Values.ElementAt(closestEntityIndex).DetectionPoint;
                var invWorld = m_targetGrid.PositionComp.GetWorldMatrixNormalizedInv();
                var gridLocalPos = Vector3D.Transform(m_targetPosition, invWorld);
                var gridSpacePos = Vector3I.Round(gridLocalPos / m_targetGrid.GridSize);
                m_targetGrid.FixTargetCube(out m_targetCube, gridLocalPos / m_targetGrid.GridSize);

                var head = PositionComp.WorldMatrix;
                var aimToMuzzle = Vector3D.Normalize(m_targetPosition - m_gunBase.GetMuzzleWorldPosition());
                if (Vector3.Dot(aimToMuzzle, head.Forward) > 0)
                {
                    m_targetDistanceSq = 0;
                }
                else
                {
                    m_targetDistanceSq = (float)Vector3D.DistanceSquared(m_targetPosition, m_gunBase.GetMuzzleWorldPosition());
                }
            }
            PowerReceiver.Update();

            if (IsShooting && !PowerReceiver.IsPowered)
            {
                EndShoot(MyShootActionEnum.PrimaryAction);
            }

            UpdateEffect();
            CheckEffectType();

			if (Owner != null && Owner.ControllerInfo.IsLocallyHumanControlled())
			{
				if (MySession.Static.SurvivalMode && (MySession.GetCameraControllerEnum() != MyCameraControllerEnum.Spectator || MyFinalBuildConstants.IS_OFFICIAL))
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

        private void WorldPositionChanged(object source)
        {
            m_gunBase.OnWorldPositionChanged(PositionComp.WorldMatrix);
            UpdateSensorPosition();
        }

        public void UpdateSensorPosition()
        {
            if (m_sensor != null)
            {
                Debug.Assert(Owner != null && Owner is MyCharacter, "An engineer tool is not held by a character");
                MyCharacter character = Owner as MyCharacter;
                MatrixD sensorWorldMatrix = character.GetHeadMatrix(false, true);
                m_sensor.OnWorldPositionChanged(ref sensorWorldMatrix);
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

        public virtual void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (action != MyShootActionEnum.PrimaryAction)
            {
                return;
            }

            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_shootFrameCounter++;
            m_tryingToShoot = true;
            PowerReceiver.Update();
            if (!PowerReceiver.IsPowered)
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
                PowerReceiver.Update();
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
            if (m_targetGrid == null || m_targetCube == null)
            {
                return MatrixD.CreateWorld(m_gunBase.GetMuzzleWorldPosition(), PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
            }

            var aimPoint = m_targetPosition;
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

                bool canSeeEffect = MySector.MainCamera.GetDistanceWithFOV(PositionComp.GetPosition()) < 150;
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
            MyParticlesManager.TryCreateParticleEffect((int)EffectId, out m_toolEffect);            
            m_toolEffectLight = CreatePrimaryLight();
            UpdateEffect();
        }

        protected virtual MyLight CreatePrimaryLight()
        {
            MyLight light = MyLights.AddLight();
            light.Start(MyLight.LightTypeEnum.PointLight, Vector3.Zero, m_handItemDef.LightColor, m_handItemDef.LightFalloff, m_handItemDef.LightRadius);
            light.GlareMaterial = "GlareWelder";
            light.GlareOn = true;
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
            light.GlareOn = true;
            light.GlareQuerySize = 1;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            return light;
        }

        private void UpdateEffect()
        {
            // Disable active effect when tool is too far (this is so that effects of other players in MP look better)
            if (CurrentEffect == 1 && m_targetGrid == null)
            {
                CurrentEffect = 2;
            }

            //MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Updating effect!", Color.Red, 1.0f);
            switch (CurrentEffect)
            {
                case 0:
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
            const float particleMuzzleOffset = 1.0f;            

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
            CharacterInventory = Owner.GetInventory();

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

        public void DrawHud(Sandbox.ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;

            DrawHud();
            UpdateHudComponentMark();
        }

        protected virtual void DrawHud()
        {
            MyHud.BlockInfo.Visible = false;

            if (m_targetCube == null || m_targetGrid == null)
                return;

            var block = m_targetGrid.GetCubeBlock(m_targetCube);
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
            MyHud.BlockInfo.BlockIcon = block.BlockDefinition.Icon;
            MyHud.BlockInfo.BlockIntegrity = block.Integrity / block.MaxIntegrity;
            MyHud.BlockInfo.CriticalIntegrity = block.BlockDefinition.CriticalIntegrityRatio;
            MyHud.BlockInfo.CriticalComponentIndex = block.BlockDefinition.CriticalGroup;
            MyHud.BlockInfo.OwnershipIntegrity = block.BlockDefinition.OwnershipIntegrityRatio;

            MySlimBlock.SetBlockComponents(MyHud.BlockInfo, block);

            if (m_targetDistanceSq > m_toolActionDistance * m_toolActionDistance)
            {
                // TODO: Show some error?
                //MyHud.BlockInfo.Error.Append("Out of reach");
            }
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
    }
}


using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Lights;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Entity;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipGrinder))]
    public class MyShipGrinder : MyShipToolBase, IMyShipGrinder
    {
        private static MySoundPair IDLE_SOUND = new MySoundPair("ToolPlayGrindIdle");
        private static MySoundPair METAL_SOUND = new MySoundPair("ToolPlayGrindMetal");

        private const MyParticleEffectsIDEnum PARTICLE_EFFECT = MyParticleEffectsIDEnum.AngleGrinder;

        private float m_rotationSpeed;
        private bool m_sparks = true;
        private bool m_wantsToGrind = true;
        
        MyParticleEffect m_particleEffect1;
        MyParticleEffect m_particleEffect2;
        MyLight m_effectLight;

        const float RANDOM_IMPULSE_SCALE = 500.0f;

        private static List<MyPhysicalInventoryItem> m_tmpItemList = new List<MyPhysicalInventoryItem>();

        bool m_wantsToShake = false;
        MyCubeGrid m_otherGrid = null;

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            if (CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                IDLE_SOUND.Init("ToolLrgGrindIdle");
                METAL_SOUND.Init("ToolLrgGrindMetal");
            }
            m_rotationSpeed = 0.0f;

            HeatUpFrames = MyShipGrinderConstants.GRINDER_HEATUP_FRAMES;
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void OnControlAcquired(Sandbox.Game.Entities.Character.MyCharacter owner)
        {
            base.OnControlAcquired(owner);

            if (owner == null || owner.Parent == null)
                return;

            if (owner == MySession.Static.LocalCharacter && !owner.Parent.Components.Contains(typeof(MyCasterComponent)))
            {
                MyDrillSensorRayCast raycaster = new MyDrillSensorRayCast(0, DEFAULT_REACH_DISTANCE);
                MyCasterComponent raycastingComponent = new MyCasterComponent(raycaster);
                owner.Parent.Components.Add(raycastingComponent);
                controller = owner;
            }
        }

        public override void OnControlReleased()
        {
            base.OnControlReleased();

            if (controller == null || controller.Parent == null)
                return;

            if (controller == MySession.Static.LocalCharacter && controller.Parent.Components.Contains(typeof(MyCasterComponent)))
            {
                controller.Parent.Components.Remove(typeof(MyCasterComponent));
            }
        }

        protected override bool Activate(HashSet<MySlimBlock> targets)
        {
            int successTargets = targets.Count;
            m_otherGrid = null;
            if (targets.Count > 0)
            {
                m_otherGrid = targets.FirstElement().CubeGrid;
            }
            if (Sync.IsServer)
            {             
                float coefficient = (MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f) / targets.Count;             
                foreach (var block in targets)
                {
                    if ((MySession.Static.IsScenario || MySession.Static.Settings.ScenarioEditMode) && !block.CubeGrid.BlocksDestructionEnabled)
                        continue;

                    m_otherGrid = block.CubeGrid;

                    bool tmp2 = m_otherGrid.Physics == null || !m_otherGrid.Physics.Enabled;
                    if (tmp2)
                    {
                        successTargets--;
                        continue;
                    }

                    float damage = MySession.Static.GrinderSpeedMultiplier * MyShipGrinderConstants.GRINDER_AMOUNT_PER_SECOND * coefficient;
                    MyDamageInformation damageInfo = new MyDamageInformation(false, damage, MyDamageType.Grind, EntityId);

                    if (block.UseDamageSystem)
                        MyDamageSystem.Static.RaiseBeforeDamageApplied(block, ref damageInfo);

                    if (block.CubeGrid.Editable)
                    {
                        block.DecreaseMountLevel(damageInfo.Amount, this.GetInventory());
                        block.MoveItemsFromConstructionStockpile(this.GetInventory());
                    }

                    if (block.UseDamageSystem)
                        MyDamageSystem.Static.RaiseAfterDamageApplied(block, damageInfo);
                    
                    if (block.IsFullyDismounted)
                    {
                        if (block.FatBlock != null && block.FatBlock.HasInventory)
                        {
                            EmptyBlockInventories(block.FatBlock);
                        }

                        if(block.UseDamageSystem)
                            MyDamageSystem.Static.RaiseDestroyed(block, damageInfo);

                        block.SpawnConstructionStockpile();
                        block.CubeGrid.RazeBlock(block.Min);
                    }
                }
                if (successTargets > 0)
                    SetBuildingMusic(200);
            }
            m_wantsToShake = successTargets != 0;
            return successTargets != 0;
        }

        private void EmptyBlockInventories(MyCubeBlock block)
        {
            for (int i = 0; i < block.InventoryCount; ++i)
            {
                var blockInventory = block.GetInventory(i) as MyInventory;
                System.Diagnostics.Debug.Assert(blockInventory != null, "Null or other inventory type!");
                if (blockInventory.Empty()) continue;

                m_tmpItemList.Clear();
                m_tmpItemList.AddList(blockInventory.GetItems());

                foreach (var item in m_tmpItemList)
                {
                    MyInventory.Transfer(blockInventory, this.GetInventory(), item.ItemId);
                }
            }
        }

        protected override void UpdateAnimation(bool activated)
        {
            if (activated && m_rotationSpeed < MyShipGrinderConstants.GRINDER_MAX_SPEED_RPM)
            {
                m_rotationSpeed += 0.017f * MyShipGrinderConstants.GRINDER_ACCELERATION_RPMPS;
                if (m_rotationSpeed > MyShipGrinderConstants.GRINDER_MAX_SPEED_RPM)
                    m_rotationSpeed = MyShipGrinderConstants.GRINDER_MAX_SPEED_RPM;
            }
            else if (!activated && m_rotationSpeed > 0.0f)
            {
                m_rotationSpeed -= 0.017f * MyShipGrinderConstants.GRINDER_DECELERATION_RPMPS;
                if (m_rotationSpeed < 0.0f)
                {
                    m_rotationSpeed = 0.0f;
                }
            }

            var subpart1 = Subparts["grinder1"];
            var subpart2 = Subparts["grinder2"];
            subpart1.PositionComp.LocalMatrix = Matrix.CreateRotationX(-17.0f * m_rotationSpeed * MathHelper.RPMToRadiansPerMillisec) * subpart1.PositionComp.LocalMatrix;
            subpart2.PositionComp.LocalMatrix = Matrix.CreateRotationX(17.0f * m_rotationSpeed * MathHelper.RPMToRadiansPerMillisec) * subpart2.PositionComp.LocalMatrix;
        }
       
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (m_wantsToShake && m_otherGrid != null && MySession.Static.EnableToolShake && MyFakes.ENABLE_TOOL_SHAKE)
            {
                //apply random shake force
                Vector3 randomForce = MyUtils.GetRandomVector3();
                ApplyImpulse(m_otherGrid, randomForce);
                ApplyImpulse(CubeGrid, randomForce);
            }

            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeActivate >= MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS)
            {
                m_wantsToShake = false;
                m_otherGrid = null;
            }

            if (!IsShooting && !IsHeatingUp && m_rotationSpeed <= float.Epsilon)
            {
                // Doesn't actually do anything, switch each frame update off
                NeedsUpdate &= ~VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            if(m_rotationSpeed != 0f)
                IsCloseEnough();
            if (Sync.IsServer && IsFunctional && UseConveyorSystem && this.GetInventory().GetItems().Count > 0)
            {
                MyGridConveyorSystem.PushAnyRequest(this, this.GetInventory(), OwnerId);
            }
        }

        protected override void StartEffects()
        {
            StopEffects();
            m_wantsToGrind = true;
            if (m_sparks)
            {
                MyParticlesManager.TryCreateParticleEffect((int)PARTICLE_EFFECT, out m_particleEffect1);
                MyParticlesManager.TryCreateParticleEffect((int)PARTICLE_EFFECT, out m_particleEffect2);
                UpdateParticleMatrices();
            }
            m_effectLight = CreateEffectLight();
        }

        private void IsCloseEnough()
        {
            m_sparks = (Vector3D.DistanceSquared(MySector.MainCamera.Position, this.PositionComp.GetPosition()) < 10000f);
        }

        private MyLight CreateEffectLight()
        {
            MyLight light = MyLights.AddLight();
            light.Start(MyLight.LightTypeEnum.PointLight, Vector3.Zero, new Vector4(1.0f, 0.8f, 0.6f, 1.0f), 2.0f, 10.0f);
            light.GlareMaterial = "GlareWelder";
            light.GlareOn = light.LightOn;
            light.GlareQuerySize = 0.4f;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            return light;
        }

        private void StopSparks()
        {
            if (m_particleEffect1 != null)
            {
                m_particleEffect1.Stop();
                m_particleEffect1 = null;
            }
            if (m_particleEffect2 != null)
            {
                m_particleEffect2.Stop();
                m_particleEffect2 = null;
            }
        }

        protected override void StopEffects()
        {
            m_wantsToGrind = false;
            StopSparks();
            if (m_effectLight != null)
            {
                MyLights.RemoveLight(m_effectLight);
                m_effectLight = null;
            }
        }

        protected override void UpdateEffects()
        {
            if ((m_particleEffect1 != null || m_particleEffect2 != null) && m_sparks == false)
            {
                StopSparks();
            }
            else if ((m_particleEffect1 == null || m_particleEffect2 == null) && m_sparks && m_wantsToGrind)
            {
                StartEffects();
            }
            else 
            {
                UpdateParticleMatrices();
            }

            if (m_effectLight != null)
            {
                m_effectLight.Position = GetLightPosition();
                m_effectLight.UpdateLight();
            }
        }

        private void UpdateParticleMatrices()
        {
            if (m_particleEffect1 != null)
                m_particleEffect1.WorldMatrix = GetEffectMatrix(1);
            if (m_particleEffect2 != null)
                m_particleEffect2.WorldMatrix = GetEffectMatrix(2);
        }

        private Matrix GetEffectMatrix(int effectNum)
        {
            Matrix retval = WorldMatrix;
            retval.Translation += retval.Forward * 1.5f;
            retval.Translation += retval.Up * (effectNum == 1 ? 0.25f : -0.25f);

            float zRotation = effectNum == 1 ? MyUtils.GetRandomFloat(0.3f, 0.7f) : MyUtils.GetRandomFloat(-0.7f, -0.3f);
            float yRotation = (float)Math.PI * -MyUtils.GetRandomFloat(0.47f, 0.53f);

            retval = Matrix.CreateRotationZ(zRotation) * Matrix.CreateRotationY(yRotation) * retval;
            return retval;
        }

        private Vector3 GetLightPosition()
        {
            return WorldMatrix.Translation + WorldMatrix.Forward * 2.0f;
        }

        protected override void StopLoopSound()
        {
            if(m_soundEmitter != null)
                m_soundEmitter.StopSound(false);
        }

        protected override void PlayLoopSound(bool activated)
        {
            if (m_soundEmitter == null)
                return;
            MySoundPair cueEnum = activated ? METAL_SOUND : IDLE_SOUND;
            if (m_soundEmitter.Sound != null && (m_soundEmitter.SoundPair.Equals(METAL_SOUND) || m_soundEmitter.SoundPair.Equals(IDLE_SOUND)) && m_soundEmitter.Sound.IsPlaying)
                m_soundEmitter.PlaySingleSound(cueEnum, true, true);
            else
                m_soundEmitter.PlaySound(cueEnum);
        }

        private void ApplyImpulse(MyCubeGrid grid, Vector3 force)
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(grid);
            //apply impulse only on server, position is synchorized on clients
            if ((Sync.IsServer && controllingPlayer == null) || MySession.Static.LocalHumanPlayer == controllingPlayer)
            {
                if (grid.Physics != null)
                {
                    grid.Physics.ApplyImpulse(force * CubeGrid.GridSize * RANDOM_IMPULSE_SCALE, PositionComp.GetPosition());
                }
            }
        }


        #region IMyConveyorEndpointBlock implementation

        public override Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public override Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = this.GetInventory();
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = pullInformation.Inventory.Constraint;
            return pullInformation;
        }

        #endregion
    }
}

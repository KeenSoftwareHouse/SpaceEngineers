
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Lights;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipGrinder))]
    class MyShipGrinder : MyShipToolBase, IMyShipGrinder
    {
        private static MySoundPair IDLE_SOUND = new MySoundPair("ToolPlayGrindIdle");
        private static MySoundPair METAL_SOUND = new MySoundPair("ToolPlayGrindMetal");

        private const MyParticleEffectsIDEnum PARTICLE_EFFECT = MyParticleEffectsIDEnum.AngleGrinder;

        private float m_rotationSpeed;
        
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
        }

        protected override bool Activate(HashSet<MySlimBlock> targets)
        {
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
                    m_otherGrid = block.CubeGrid;
                    block.DecreaseMountLevel(MySession.Static.GrinderSpeedMultiplier * MyShipGrinderConstants.GRINDER_AMOUNT_PER_SECOND * coefficient, Inventory);
                    block.MoveItemsFromConstructionStockpile(Inventory);
                   
                    
                    if (block.IsFullyDismounted)
                    {
                        if (block.FatBlock is IMyInventoryOwner) EmptyBlockInventories(block.FatBlock as IMyInventoryOwner);
                        block.SpawnConstructionStockpile();
                        block.CubeGrid.RazeBlock(block.Min);
                    }
                }
                
            }
            m_wantsToShake = targets.Count != 0;
            return targets.Count != 0;
        }

        private void EmptyBlockInventories(IMyInventoryOwner block)
        {
            for (int i = 0; i < block.InventoryCount; ++i)
            {
                var blockInventory = block.GetInventory(i);
                if (blockInventory.Empty()) continue;

                m_tmpItemList.Clear();
                m_tmpItemList.AddList(blockInventory.GetItems());

                foreach (var item in m_tmpItemList)
                {
                    MyInventory.Transfer(blockInventory, Inventory, item.ItemId);
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
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (Sync.IsServer && IsFunctional && UseConveyorSystem && Inventory.GetItems().Count > 0)
            {
                MyGridConveyorSystem.PushAnyRequest(this, Inventory, OwnerId);
            }
        }

        protected override void StartEffects()
        {
            StopEffects();
            MyParticlesManager.TryCreateParticleEffect((int)PARTICLE_EFFECT, out m_particleEffect1);
            MyParticlesManager.TryCreateParticleEffect((int)PARTICLE_EFFECT, out m_particleEffect2);
            UpdateParticleMatrices();

            m_effectLight = CreateEffectLight();
        }

        private MyLight CreateEffectLight()
        {
            MyLight light = MyLights.AddLight();
            light.Start(MyLight.LightTypeEnum.PointLight, Vector3.Zero, new Vector4(1.0f, 0.8f, 0.6f, 1.0f), 2.0f, 10.0f);
            light.GlareMaterial = "GlareWelder";
            light.GlareOn = true;
            light.GlareQuerySize = 1;
            light.GlareType = VRageRender.Lights.MyGlareTypeEnum.Normal;
            return light;
        }

        protected override void StopEffects()
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
            if (m_effectLight != null)
            {
                MyLights.RemoveLight(m_effectLight);
                m_effectLight = null;
            }
        }

        protected override void UpdateEffects()
        {
            UpdateParticleMatrices();

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
            retval.Translation += retval.Forward * 1.9f;
            retval.Translation += retval.Up * (effectNum == 1 ? 0.65f : -0.65f);

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
            m_soundEmitter.StopSound(false);
        }

        protected override void PlayLoopSound(bool activated)
        {
            MySoundPair cueEnum = activated ? METAL_SOUND : IDLE_SOUND;
            if (m_soundEmitter.Sound != null && (m_soundEmitter.Sound.CueEnum == METAL_SOUND.SoundId || m_soundEmitter.Sound.CueEnum == IDLE_SOUND.SoundId) && m_soundEmitter.Sound.IsPlaying)
                m_soundEmitter.PlaySingleSound(cueEnum, true, true);
            else
                m_soundEmitter.PlaySound(cueEnum);
        }

        private void ApplyImpulse(MyCubeGrid grid, Vector3 force)
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(grid);
            //apply impulse only on server, position is synchorized on clients
            if ((Sync.IsServer && controllingPlayer == null) || MySession.LocalHumanPlayer == controllingPlayer)
            {
                if (grid.Physics != null)
                {
                    grid.Physics.ApplyImpulse(force * CubeGrid.GridSize * RANDOM_IMPULSE_SCALE, PositionComp.GetPosition());
                }
            }
        }
    }
}

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipWelder))]
    class MyShipWelder : MyShipToolBase, IMyShipWelder
    {
        private static MySoundPair METAL_SOUND = new MySoundPair("ToolLrgWeldMetal");
        private static MySoundPair IDLE_SOUND = new MySoundPair("ToolLrgWeldIdle");
        private const MyParticleEffectsIDEnum PARTICLE_EFFECT = MyParticleEffectsIDEnum.Welder;
        private bool m_helpOthers = false;

        public static readonly float WELDER_AMOUNT_PER_SECOND = 2f;
        public static readonly float WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED = 0.6f;

        private Dictionary<String, int> m_missingComponents;

        //Used for finding projection blocks
        List<MyWelder.ProjectionRaycastData> m_raycastData = new List<MyWelder.ProjectionRaycastData>();
        HashSet<MySlimBlock> m_projectedBlock = new HashSet<MySlimBlock>();

        MyParticleEffect m_particleEffect;
        MyLight m_effectLight;

        static MyShipWelder()
        {
            if (MyFakes.ENABLE_WELDER_HELP_OTHERS)
            {
                var helpOthersCheck = new MyTerminalControlCheckbox<MyShipWelder>("helpOthers", MySpaceTexts.ShipWelder_HelpOthers, MySpaceTexts.ShipWelder_HelpOthers);
                helpOthersCheck.Getter = (x) => x.m_helpOthers;
                helpOthersCheck.Setter = (x, v) =>
                {
                    x.m_helpOthers = v;
                };
                helpOthersCheck.EnableAction();
                MyTerminalControlFactory.AddControl(helpOthersCheck);
            }
        }

        protected override bool CanInteractWithSelf
        {
            get
            {
                return true;
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_missingComponents = new Dictionary<string, int>();
        }

        protected override bool Activate(HashSet<MySlimBlock> targets)
        {
            bool welding = false;
            bool unweldedBlocksDetected = false;
            int targetCount = targets.Count;

            m_missingComponents.Clear();

            foreach (var block in targets)
            {
                if (block.BuildLevelRatio == 1.0f)
                {
                    targetCount--;
                    continue;
                }

                block.GetMissingComponents(m_missingComponents);
            }

            foreach (var component in m_missingComponents)
            {
                var componentId = new MyDefinitionId(typeof(MyObjectBuilder_Component), component.Key);
                int amount = Math.Max(component.Value - (int)Inventory.GetItemAmount(componentId), 0);
                if (amount == 0) continue;
                
                if (Sync.IsServer && UseConveyorSystem)
                    MyGridConveyorSystem.ItemPullRequest(this, Inventory, OwnerId, componentId, component.Value);
            }

            if (Sync.IsServer)
            {
                float coefficient = (MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f) / (targetCount>0?targetCount:1);
                foreach (var block in targets)
                {
                    if (!block.IsFullIntegrity)
                        unweldedBlocksDetected = true;
                    
                    if (block.CanContinueBuild(Inventory))
                        welding = true;

                    block.MoveItemsToConstructionStockpile(Inventory);

                    // Allow welding only for blocks with deformations or unfinished/damaged blocks
                    if (block.MaxDeformation > 0.0f || !block.IsFullIntegrity)
                    {
                        float maxAllowedBoneMovement = WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED * MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f;
                        block.IncreaseMountLevel(MySession.Static.WelderSpeedMultiplier * WELDER_AMOUNT_PER_SECOND * coefficient, OwnerId, Inventory, maxAllowedBoneMovement, m_helpOthers, IDModule.ShareMode);
                    }
                }
            }
            else
            {
                foreach (var block in targets)
                {
                    if (block.CanContinueBuild(Inventory))
                        welding = true;
                }
            }

            m_missingComponents.Clear();

            if (!unweldedBlocksDetected && Sync.IsServer)
            {
                //Try to build blocks for projections
                var blocks = FindProjectedBlocks();

                //Try to acquire materials first, but only if it uses the conveyor system
                if (UseConveyorSystem)
                {
                    foreach (var info in blocks)
                    {
                        var componentId = info.hitCube.BlockDefinition.Components[0].Definition.Id;
                        MyGridConveyorSystem.ItemPullRequest(this, Inventory, OwnerId, componentId, 1);
                    }
                }

                var locations = new HashSet<MyCubeGrid.MyBlockLocation>();

                foreach (var info in blocks)
                {
                    if (MySession.Static.CreativeMode || Inventory.ContainItems(1, info.hitCube.BlockDefinition.Components[0].Definition.Id))
                    {
                        info.cubeProjector.Build(info.hitCube, OwnerId, EntityId);
                        welding = true;
                    }
                }
            }


            return welding;
        }

        private MyWelder.ProjectionRaycastData[] FindProjectedBlocks()
        {
            BoundingSphereD globalSphere = new BoundingSphereD(Vector3D.Transform(m_detectorSphere.Center, CubeGrid.WorldMatrix), m_detectorSphere.Radius);

            var m_raycastData = new List<MyWelder.ProjectionRaycastData>();

            var entitiesInSphere = MyEntities.GetEntitiesInSphere(ref globalSphere);
            foreach (var entity in entitiesInSphere)
            {
                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    if (grid.Projector != null)
                    {
                        grid.GetBlocksInsideSphere(ref globalSphere, m_projectedBlock);
                        foreach (var block in m_projectedBlock)
                        {
                            var canBuild = grid.Projector.CanBuild(block, true);
                            if (canBuild == MyProjector.BuildCheckResult.OK)
                            {
                                var cubeBlock = grid.GetCubeBlock(block.Position);
                                if (cubeBlock != null)
                                {
                                    m_raycastData.Add(new MyWelder.ProjectionRaycastData(MyProjector.BuildCheckResult.OK, cubeBlock, grid.Projector));
                                }
                            }
                        }
                        m_projectedBlock.Clear();
                    }
                }
            }

            m_projectedBlock.Clear();
            entitiesInSphere.Clear();

            return m_raycastData.ToArray();
        }

        protected override void UpdateAnimation(bool activated)
        { }

        protected override void StartShooting()
        {
            base.StartShooting();

            SetEmissivity(Color.Red);
        }

        protected override void StopShooting()
        {
            base.StopShooting();

            SetEmissivity(Color.Black);
        }

        protected override void StartEffects()
        {
            StopEffects();
            MyParticlesManager.TryCreateParticleEffect((int)PARTICLE_EFFECT, out m_particleEffect);
            UpdateParticleMatrix();

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
            if (m_particleEffect != null)
            {
                m_particleEffect.Stop();
                m_particleEffect = null;
            }

            if (m_effectLight != null)
            {
                MyLights.RemoveLight(m_effectLight);
                m_effectLight = null;
            }
        }

        private void SetEmissivity(Color emissiveColor)
        {
            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, emissiveColor, Color.White);
        }

        protected override void UpdateEffects()
        {
            UpdateParticleMatrix();

            if (m_effectLight != null)
            {
                m_effectLight.Position = GetLightPosition();
                m_effectLight.UpdateLight();
            }
        }

        private void UpdateParticleMatrix()
        {
            if (m_particleEffect == null) return;

            MatrixD matrix = WorldMatrix;
            matrix.Translation += matrix.Forward * 2.5f;
            //retval.Translation += retval.Up * (effectNum == 1 ? 0.65f : -0.65f);

            //float zRotation = MyVRageUtils.GetRandomFloat(-0.7f, 0.7f);
            //float yRotation = (float)Math.PI * -MyVRageUtils.GetRandomFloat(0.47f, 0.53f);

            m_particleEffect.WorldMatrix = /*Matrix.CreateRotationZ(zRotation) * Matrix.CreateRotationY(yRotation) **/ matrix;
        }

        private Vector3 GetLightPosition()
        {
            return WorldMatrix.Translation + WorldMatrix.Forward * 2.0f;
        }

        protected override void StopLoopSound()
        {
                m_soundEmitter.StopSound(true);
        }

        protected override void PlayLoopSound(bool activated)
        {
            if (activated)
                m_soundEmitter.PlaySingleSound(METAL_SOUND, true);
            else
                m_soundEmitter.PlaySingleSound(IDLE_SOUND, true);
        }
    }
}

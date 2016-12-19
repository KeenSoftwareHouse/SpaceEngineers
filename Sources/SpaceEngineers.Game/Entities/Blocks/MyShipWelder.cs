using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Sync;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipWelder))]
    public class MyShipWelder : MyShipToolBase, IMyShipWelder
    {
        private static MySoundPair METAL_SOUND = new MySoundPair("ToolLrgWeldMetal");
        private static MySoundPair IDLE_SOUND = new MySoundPair("ToolLrgWeldIdle");
        private const MyParticleEffectsIDEnum PARTICLE_EFFECT = MyParticleEffectsIDEnum.Welder;
        private Sync<bool> m_helpOthers;

        public static readonly float WELDER_AMOUNT_PER_SECOND = 2f;
        public static readonly float WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED = 0.6f;

        private Dictionary<String, int> m_missingComponents;

        //Used for finding projection blocks
        List<MyWelder.ProjectionRaycastData> m_raycastData = new List<MyWelder.ProjectionRaycastData>();
        HashSet<MySlimBlock> m_projectedBlock = new HashSet<MySlimBlock>();

        MyParticleEffect m_particleEffect;
        MyLight m_effectLight;

        public bool HelpOthers
        {
            get { return m_helpOthers; }
        }

        protected override bool CanInteractWithSelf
        {
            get
            {
                return true;
            }
        }

        public MyShipWelder()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_helpOthers = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyShipWelder>())
                return;
            base.CreateTerminalControls();
            if (MyFakes.ENABLE_WELDER_HELP_OTHERS)
            {
                var helpOthersCheck = new MyTerminalControlCheckbox<MyShipWelder>("helpOthers", MyCommonTexts.ShipWelder_HelpOthers, MyCommonTexts.ShipWelder_HelpOthers);
                helpOthersCheck.Getter = (x) => x.HelpOthers;
                helpOthersCheck.Setter = (x, v) => x.m_helpOthers.Value = v;
                helpOthersCheck.EnableAction();
                MyTerminalControlFactory.AddControl(helpOthersCheck);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            m_missingComponents = new Dictionary<string, int>();

            var builder = (MyObjectBuilder_ShipWelder)objectBuilder;
            m_helpOthers.Value = builder.HelpOthers;

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

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_ShipWelder)base.GetObjectBuilderCubeBlock(copy);
            builder.HelpOthers = m_helpOthers;
            return builder;
        }

        /// <summary>
        /// Determines whether the projected grid still fits within block limits set by server after a new block is added
        /// </summary>
        private bool IsWithinWorldLimits(MyProjectorBase projector, string name)
        {
            if (!MySession.Static.EnableBlockLimits) return true;

            bool withinLimits = true;
            var identity = MySession.Static.Players.TryGetIdentity(BuiltBy);
            if (MySession.Static.MaxBlocksPerPlayer > 0)
            {
                withinLimits &= BuiltBy == 0 || IDModule.GetUserRelationToOwner(BuiltBy) != MyRelationsBetweenPlayerAndBlock.Enemies; // Don't allow stolen enemy welders to build
                withinLimits &= projector.BuiltBy == 0 || IDModule.GetUserRelationToOwner(projector.BuiltBy) != MyRelationsBetweenPlayerAndBlock.Enemies; // Don't allow welders to build from enemy projectors
                withinLimits &= identity == null || identity.BlocksBuilt < MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
            }
            withinLimits &= MySession.Static.MaxGridSize == 0 || projector.CubeGrid.BlocksCount < MySession.Static.MaxGridSize;
            short typeLimit = MySession.Static.GetBlockTypeLimit(name);
            int typeBuilt;
            if (identity != null && typeLimit > 0)
            {
                withinLimits &= (identity.BlockTypeBuilt.TryGetValue(name, out typeBuilt) ? typeBuilt : 0) < typeLimit;
            }
            return withinLimits;
        }
        
        protected override bool Activate(HashSet<MySlimBlock> targets)
        {
            bool welding = false;
            bool unweldedBlocksDetected = false;
            int targetCount = targets.Count;

            m_missingComponents.Clear();
            
            foreach (var block in targets)
            {
                if (block.BuildLevelRatio == 1.0f || block == SlimBlock)
                {
                    targetCount--;
                    continue;
                }

                block.GetMissingComponents(m_missingComponents);
            }

            foreach (var component in m_missingComponents)
            {
                var componentId = new MyDefinitionId(typeof(MyObjectBuilder_Component), component.Key);
                int amount = Math.Max(component.Value - (int)this.GetInventory().GetItemAmount(componentId), 0);
                if (amount == 0) continue;

                if (Sync.IsServer && UseConveyorSystem)
                {
                    var group = MyDefinitionManager.Static.GetGroupForComponent(componentId, out amount);
                    if (group == null)
                    {
                        MyComponentSubstitutionDefinition substitutions;
                        if (MyDefinitionManager.Static.TryGetComponentSubstitutionDefinition(componentId, out substitutions))
                        {
                            foreach (var providingComponent in substitutions.ProvidingComponents)
                            {
                                MyFixedPoint substituionAmount = (int)component.Value / providingComponent.Value;
                                MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, providingComponent.Key, substituionAmount);
                            }
                        }
                        else
                        {
                            MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, componentId, component.Value);    
                        }
                    }
                    else
                    {
                        MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, componentId, component.Value);              
                    }
                    
                }
            }

            if (Sync.IsServer)
            {
                float coefficient = (MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f) / (targetCount>0?targetCount:1);
                foreach (var block in targets)
                {
                    // remove projected blocks
                    if (block.CubeGrid.Physics == null || !block.CubeGrid.Physics.Enabled)
                        continue;

                    // Don't weld yourself
                    if (block == SlimBlock) 
                        continue;

                    if (!block.IsFullIntegrity)
                        unweldedBlocksDetected = true;
                    
                    if (block.CanContinueBuild(this.GetInventory()))
                        welding = true;

                    block.MoveItemsToConstructionStockpile(this.GetInventory());

                    // Allow welding only for blocks with deformations or unfinished/damaged blocks
                    if ((block.HasDeformation || block.MaxDeformation > 0.0001f) || !block.IsFullIntegrity)
                    {
                        float maxAllowedBoneMovement = WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED * MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f;
                        block.IncreaseMountLevel(MySession.Static.WelderSpeedMultiplier * WELDER_AMOUNT_PER_SECOND * coefficient, OwnerId, this.GetInventory(), maxAllowedBoneMovement, m_helpOthers, IDModule.ShareMode);
                    }
                }
            }
            else
            {
                foreach (var block in targets)
                {
                    // Don't weld yourself
                    if (block == SlimBlock)
                        continue;

                    if (block.CanContinueBuild(this.GetInventory()))
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
                        var components=info.hitCube.BlockDefinition.Components;
                        if (components == null || components.Length == 0)
                            continue;
                        var componentId = components[0].Definition.Id;
                        MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, componentId, 1);
                    }
                }

                var locations = new HashSet<MyCubeGrid.MyBlockLocation>();

                foreach (var info in blocks)
                {
                    if (IsWithinWorldLimits(info.cubeProjector, info.hitCube.BlockDefinition.BlockPairName) && (MySession.Static.CreativeMode || this.GetInventory().ContainItems(1, info.hitCube.BlockDefinition.Components[0].Definition.Id)))
                    {
                        if (MySession.Static.MaxBlocksPerPlayer == 0 || BuiltBy != 0)
                        {
                            info.cubeProjector.Build(info.hitCube, OwnerId, EntityId, builtBy: BuiltBy);
                            welding = true;
                        }
                        else if (OwnerId != 0)
                        {
                            info.cubeProjector.Build(info.hitCube, OwnerId, EntityId, builtBy: OwnerId);
                            welding = true;
                        }
                    }
                }
            }

            if (welding)
                SetBuildingMusic(150);

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
                            if (canBuild == BuildCheckResult.OK)
                            {
                                var cubeBlock = grid.GetCubeBlock(block.Position);
                                if (cubeBlock != null)
                                {
                                    m_raycastData.Add(new MyWelder.ProjectionRaycastData(BuildCheckResult.OK, cubeBlock, grid.Projector));
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
            light.GlareOn = light.LightOn;
            light.GlareQuerySize = 0.4f;
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

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (!IsShooting && !IsHeatingUp)
            {
                // Doesn't actually do anything, switch each frame update off
                NeedsUpdate &= ~VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
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
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
        }

        protected override void PlayLoopSound(bool activated)
        {
            if (m_soundEmitter == null)
                return;
            if (activated)
                m_soundEmitter.PlaySingleSound(METAL_SOUND, true);
            else
                m_soundEmitter.PlaySingleSound(IDLE_SOUND, true);
        }

        #region IMyConveyorEndpointBlock implementation

        public override Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = this.GetInventory(0);
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = new MyInventoryConstraint("Empty constraint");
            return pullInformation;
        }

        public override Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}

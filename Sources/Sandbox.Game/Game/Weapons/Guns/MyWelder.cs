#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Linq;
using VRage.Input;
using VRageMath;
using VRage.ObjectBuilders;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.Game.Entity;
using Sandbox.Game.Audio;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Audio;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_Welder))]
    public class MyWelder : MyEngineerToolBase, IMyWelder
    {
        private MySoundPair weldSoundIdle = new MySoundPair("ToolPlayWeldIdle");
        private MySoundPair weldSoundWeld = new MySoundPair("ToolPlayWeldMetal");

        public static readonly float WELDER_AMOUNT_PER_SECOND = 1f;
        public static readonly float WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED = 0.6f;

        private static MyHudNotification m_weldingHintNotification = new MyHudNotification(MySpaceTexts.WelderPrimaryActionBuild, MyHudNotification.INFINITE, level: MyNotificationLevel.Control);
        private static MyHudNotificationBase m_missingComponentNotification = new MyHudNotification(MyCommonTexts.NotificationMissingComponentToPlaceBlockFormat, font: MyFontEnum.Red);

        static MyDefinitionId m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "WelderItem");

        private MySlimBlock m_failedBlock;
        private bool m_playedFailSound = false;
        private MySlimBlock m_failedBlockSound = null;

        private Vector3I m_targetProjectionCube;
        private MyCubeGrid m_targetProjectionGrid;

        public struct ProjectionRaycastData
        {
            public BuildCheckResult raycastResult;
            public MySlimBlock hitCube;
            public MyProjectorBase cubeProjector;

            public ProjectionRaycastData(BuildCheckResult result, MySlimBlock cubeBlock, MyProjectorBase projector)
            {
                raycastResult = result;
                hitCube = cubeBlock;
                cubeProjector = projector;
            }
        }

        public MyWelder()
            : base(250)
        {
            HasCubeHighlight = true;
            HighlightColor = Color.Green * 0.75f;
			HighlightMaterial = "GizmoDrawLine";

            SecondaryLightIntensityLower = 0.4f;
            SecondaryLightIntensityUpper = 0.4f;

            SecondaryEffectId = MyParticleEffectsIDEnum.WelderSecondary;
            HasSecondaryEffect = false;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "WelderItem");
            if (objectBuilder.SubtypeName != null && objectBuilder.SubtypeName.Length > 0)
                m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), objectBuilder.SubtypeName + "Item");
            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemId);
            base.Init(objectBuilder, m_physicalItemId);

            var definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(m_physicalItemId);
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
                    if(toolSound.subtype.Equals("Idle"))
                        weldSoundIdle = new MySoundPair(toolSound.sound);
                    if (toolSound.subtype.Equals("Weld"))
                        weldSoundWeld = new MySoundPair(toolSound.sound);
                }
            }
        }

        protected override bool ShouldBePowered()
        {
            if (!base.ShouldBePowered()) return false;

            var block = GetTargetBlock();
            if (block == null) return false;
            
            MyCharacter character = Owner as MyCharacter;
            if (block.IsFullIntegrity)
            {
                if (!block.HasDeformation) return false;
                else return true;
            }
            if (!MySession.Static.CreativeMode && !block.CanContinueBuild(character.GetInventory() as MyInventory)) return false;

            return true;
        }

        protected override void DrawHud()
        {
            MyHud.BlockInfo.Visible = false;

            if (m_targetProjectionCube == null || m_targetProjectionGrid == null)
            {
                base.DrawHud();
                return;
            }

            var block = m_targetProjectionGrid.GetCubeBlock(m_targetProjectionCube);
            if (block == null)
            {
                base.DrawHud();
                return;
            }

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

            MyHud.BlockInfo.MissingComponentIndex = 0;
            MyHud.BlockInfo.BlockName = block.BlockDefinition.DisplayNameText;
            MyHud.BlockInfo.BlockIcons = block.BlockDefinition.Icons;
            MyHud.BlockInfo.BlockIntegrity = 0.01f;
            MyHud.BlockInfo.CriticalIntegrity = block.BlockDefinition.CriticalIntegrityRatio;
            MyHud.BlockInfo.CriticalComponentIndex = block.BlockDefinition.CriticalGroup;
            MyHud.BlockInfo.OwnershipIntegrity = block.BlockDefinition.OwnershipIntegrityRatio;

            //SetBlockComponents(MyHud.BlockInfo, block);
            MyHud.BlockInfo.Components.Clear();

            for (int i = 0; i < block.ComponentStack.GroupCount; i++)
            {
                var info = block.ComponentStack.GetGroupInfo(i);
                var component = new MyHudBlockInfo.ComponentInfo();
                component.DefinitionId = info.Component.Id;
                component.ComponentName = info.Component.DisplayNameText;
                component.Icons = info.Component.Icons;
                component.TotalCount = info.TotalCount;
                component.MountedCount = 0;
                component.StockpileCount = 0;

                MyHud.BlockInfo.Components.Add(component);
            }
        }

        float WeldAmount
        {
            get
            {
                return MySession.Static.WelderSpeedMultiplier * m_speedMultiplier * WELDER_AMOUNT_PER_SECOND * ToolCooldownMs / 1000.0f;
            }
        }

        public override bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            //if (action == MyShootActionEnum.SecondaryAction)
            //{
            //    status = MyGunStatusEnum.OK;
            //    return true;
            //}

            if (!base.CanShoot(action, shooter, out status))
            {
                return false;
            }


            status = MyGunStatusEnum.OK;
            var block = GetTargetBlock();
            if (block == null)
            {
                var info = FindProjectedBlock();
                if (info.raycastResult == BuildCheckResult.OK)
                {
                    return true;
                }

                status = MyGunStatusEnum.Failed;
                return false;
            }

            Debug.Assert(Owner is MyCharacter, "Only character can use welder!");
            if (Owner == null)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            if (MySession.Static.CreativeMode && (!block.IsFullIntegrity || block.HasDeformation))
            {
                return true;
            }

            if (block.IsFullIntegrity && block.HasDeformation)
            {
                return true;
            }

            {
                var info = FindProjectedBlock();
                if (info.raycastResult == BuildCheckResult.OK)
                {
                    return true;
                }
            }

            MyCharacter character = Owner as MyCharacter;
            System.Diagnostics.Debug.Assert(character.GetInventory() as MyInventory != null, "Null or unexpected inventory type returned!");
            if (!block.CanContinueBuild(character.GetInventory() as MyInventory))
            {
                status = MyGunStatusEnum.Failed;
                if (!block.IsFullIntegrity)
                    if (Owner != null && Owner == MySession.Static.LocalCharacter)
                        BeginFailReactionLocal(0, 0);
                return false;
            }

            return true;
        }

        private bool CanWeld(MySlimBlock block)
        {
            if (!block.IsFullIntegrity || block.HasDeformation)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the projected grid still fits within block limits set by server after a new block is added
        /// </summary>
        private bool IsWithinWorldLimits(MyCubeGrid grid, long ownerID, string name)
        {
            if (!MySession.Static.EnableBlockLimits) return true;

            bool withinLimits = true;
            withinLimits &= MySession.Static.MaxGridSize == 0 || grid.BlocksCount <= MySession.Static.MaxGridSize;
            var identity = MySession.Static.Players.TryGetIdentity(ownerID);
            if (MySession.Static.MaxBlocksPerPlayer != 0 && identity != null)
            {
                withinLimits &= identity.BlocksBuilt < MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
            }
            short typeLimit = MySession.Static.GetBlockTypeLimit(name);
            int typeBuilt;
            if (identity != null && typeLimit > 0)
            {
                withinLimits &= (identity.BlockTypeBuilt.TryGetValue(name, out typeBuilt) ? typeBuilt : 0) < typeLimit;
            }
            return withinLimits;
        }

        private MyProjectorBase GetProjector(MySlimBlock block)
        {
            var projectorSlimBlock = block.CubeGrid.GetBlocks().FirstOrDefault(b => b.FatBlock is MyProjectorBase);
            if (projectorSlimBlock != null)
            {
                return projectorSlimBlock.FatBlock as MyProjectorBase;
            }

            return null;
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            MyAnalyticsHelper.ReportActivityStartIf(!m_activated, this.Owner, "Welding", "Character", "HandTools","Welder",true);

            base.Shoot(action, direction, overrideWeaponPos, gunAction);

            if (action == MyShootActionEnum.PrimaryAction/* && IsPreheated*/  )
            {
                var block = GetTargetBlock();
                if (block != null && m_activated)
                {
                    if (Sync.IsServer && CanWeld(block))
                    {
                        Weld();
                    }
                }
                else if (Owner == MySession.Static.LocalCharacter)
                {
                    var info = FindProjectedBlock();
                    if (info.raycastResult == BuildCheckResult.OK)
                    {
                        if (IsWithinWorldLimits(info.cubeProjector.CubeGrid, Owner.ControllerInfo.Controller.Player.Identity.IdentityId, info.hitCube.BlockDefinition.BlockPairName))
                        {
                            if (MySession.Static.CreativeMode || MyBlockBuilderBase.SpectatorIsBuilding || Owner.CanStartConstruction(info.hitCube.BlockDefinition) || MySession.Static.CreativeToolsEnabled(Sync.MyId))
                            {
                                info.cubeProjector.Build(info.hitCube, Owner.ControllerInfo.Controller.Player.Identity.IdentityId, Owner.EntityId, builtBy: Owner.ControllerInfo.Controller.Player.Identity.IdentityId);
                            }
                            else
                            {
                                MyBlockPlacerBase.OnMissingComponents(info.hitCube.BlockDefinition);
                            }
                        }
                        else
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                            MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                        }
                    }
                }
            }
			else if(action == MyShootActionEnum.SecondaryAction && Sync.IsServer)
			{
				FillStockpile();
			}
            return;
        }

        public override void EndShoot(MyShootActionEnum action)
        {
            if (m_activated)
            {
                MyAnalyticsHelper.ReportActivityEnd(this.Owner, "Welding");
            }
            m_playedFailSound = false;
            base.EndShoot(action);
        }

        public override void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            base.BeginFailReaction(action, status);

            m_soundEmitter.PlaySingleSound(weldSoundIdle, true, true);

            FillStockpile();
        }

        public override void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            var block = GetTargetBlock();

            if (block != m_failedBlock)
            {
                UnmarkMissingComponent();
                MyHud.Notifications.Remove(m_missingComponentNotification);
            }

            m_failedBlock = block;

            if (block == null)
                return;

            if (block.IsFullIntegrity)
                return;

            int missingGroupIndex, missingGroupAmount;
            block.ComponentStack.GetMissingInfo(out missingGroupIndex, out missingGroupAmount);

            var missingGroup = block.ComponentStack.GetGroupInfo(missingGroupIndex);
            MarkMissingComponent(missingGroupIndex);
            m_missingComponentNotification.SetTextFormatArguments(
                string.Format("{0} ({1}x)", missingGroup.Component.DisplayNameText, missingGroupAmount),
                block.BlockDefinition.DisplayNameText.ToString());
            MyHud.Notifications.Add(m_missingComponentNotification);
            if ((m_playedFailSound && m_failedBlockSound != block) || m_playedFailSound == false)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                m_playedFailSound = true;
                m_failedBlockSound = block;
            }
        }

        protected override void AddHudInfo()
        {
            if (!MyInput.Static.IsJoystickConnected())
                m_weldingHintNotification.SetTextFormatArguments(MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION));
            else
                m_weldingHintNotification.SetTextFormatArguments(MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION));

            MyHud.Notifications.Add(m_weldingHintNotification);
        }

        protected override void RemoveHudInfo()
        {
            MyHud.Notifications.Remove(m_weldingHintNotification);
        }

        private void FillStockpile()
        {
            var block = GetTargetBlock();
            if (block != null)
            {
                if (Sync.IsServer)
                {
                    block.MoveItemsToConstructionStockpile(CharacterInventory);
                }
                else
                {
                    block.RequestFillStockpile(CharacterInventory);
                }
            }
        }

        private void Weld()
        {
            var block = GetTargetBlock();
            if (block != null)
            {
                block.MoveItemsToConstructionStockpile(CharacterInventory);
                block.MoveUnneededItemsFromConstructionStockpile(CharacterInventory);

                // Allow welding only for blocks with deformations or unfinished/damaged blocks
                if ((block.HasDeformation || block.MaxDeformation > 0.0f) || !block.IsFullIntegrity)
                {
                    float maxAllowedBoneMovement = WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED * ToolCooldownMs * 0.001f;
                    if (Owner != null && Owner.ControllerInfo != null)
                    {
                        block.IncreaseMountLevel(WeldAmount, Owner.ControllerInfo.ControllingIdentityId, CharacterInventory, maxAllowedBoneMovement);
                        if (MySession.Static != null && Owner == MySession.Static.LocalCharacter && MyMusicController.Static != null)
                            MyMusicController.Static.Building(250);
                    }
                }
            }
            
            var targetDestroyable = GetTargetDestroyable();
            if (targetDestroyable is MyCharacter && Sync.IsServer)
                targetDestroyable.DoDamage(20, MyDamageType.Weld, true, attackerId: EntityId);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (Owner != null && Owner == MySession.Static.LocalCharacter)
            {
                CheckProjection();
            }

            if (Owner == null || MySession.Static.ControlledEntity != Owner)
            {
                RemoveHudInfo();
            }
        }

        private void CheckProjection()
        {
            var weldBlock = GetTargetBlock();
            if (weldBlock != null && CanWeld(weldBlock))
            {
                m_targetProjectionGrid = null;
                return;
            }

            var info = FindProjectedBlock();
            if (info.raycastResult != BuildCheckResult.NotFound)
            {
                if (info.raycastResult == BuildCheckResult.OK)
                {
                    MyCubeBuilder.DrawSemiTransparentBox(info.hitCube.CubeGrid, info.hitCube, Color.Green.ToVector4(), true);
                    m_targetProjectionCube = info.hitCube.Position;
                    m_targetProjectionGrid = info.hitCube.CubeGrid;

                    return;
                }
                else if (info.raycastResult == BuildCheckResult.IntersectedWithGrid || info.raycastResult == BuildCheckResult.IntersectedWithSomethingElse)
                {
                    MyCubeBuilder.DrawSemiTransparentBox(info.hitCube.CubeGrid, info.hitCube, Color.Red.ToVector4(), true);
                }
                else if (info.raycastResult == BuildCheckResult.NotConnected)
                {
                    MyCubeBuilder.DrawSemiTransparentBox(info.hitCube.CubeGrid, info.hitCube, Color.Yellow.ToVector4(), true);
                }
            }

            m_targetProjectionGrid = null;
        }

        private ProjectionRaycastData FindProjectedBlock()
        {
            if (Owner != null)
            {
                Vector3D startPosition = m_raycastComponent.Caster.Center;
                Vector3D forward = m_raycastComponent.Caster.FrontPoint - m_raycastComponent.Caster.Center;
                forward.Normalize();

                // Welder distance is now in caster.
                float welderDistance = DEFAULT_REACH_DISTANCE * m_distanceMultiplier;
                Vector3D endPosition = startPosition + forward * welderDistance;
                LineD line = new LineD(startPosition, endPosition);
                MyCubeGrid projectionGrid;
                Vector3I blockPosition;
                double distanceSquared;
                if (MyCubeGrid.GetLineIntersection(ref line, out projectionGrid, out blockPosition, out distanceSquared))
                {
                    if (projectionGrid.Projector != null)
                    {
                        var projector = projectionGrid.Projector;

                        var blocks = projectionGrid.RayCastBlocksAllOrdered(startPosition, endPosition);

                        ProjectionRaycastData? farthestVisibleBlock = null;

                        for (int i = blocks.Count - 1; i >= 0; i--)
                        {
                            var projectionBlock = blocks[i];
                            var canBuild = projector.CanBuild(projectionBlock.CubeBlock, true);
                            if (canBuild == BuildCheckResult.OK)
                            {
                                farthestVisibleBlock = new ProjectionRaycastData
                                {
                                    raycastResult = canBuild,
                                    hitCube = projectionBlock.CubeBlock,
                                    cubeProjector = projector,
                                };
                            }
                            else if (canBuild == BuildCheckResult.AlreadyBuilt)
                            {
                                farthestVisibleBlock = null;
                            }
                        }

                        if (farthestVisibleBlock.HasValue)
                        {
                            return farthestVisibleBlock.Value;
                        }
                    }
                }
            }
            return new ProjectionRaycastData
            {
                raycastResult = BuildCheckResult.NotFound,
            };
        }

        protected override void StartLoopSound(bool effect)
        {
            MySoundPair cueEnum = effect ? weldSoundWeld : weldSoundIdle;
            if(effect)
                m_soundEmitter.PlaySingleSound(weldSoundWeld, true, true);
        }

        protected override void StopLoopSound()
        {
            StopSound();
        }

        protected override void StopSound()
        {
            m_soundEmitter.StopSound(true);
        }

    }
}

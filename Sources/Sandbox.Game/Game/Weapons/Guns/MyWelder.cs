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
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Linq;
using VRage.Input;
using VRageMath;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_Welder))]
    public class MyWelder : MyEngineerToolBase
    {
        private static MySoundPair IDLE_SOUND = new MySoundPair("ToolPlayWeldIdle");
        private static MySoundPair METAL_SOUND = new MySoundPair("ToolPlayWeldMetal");

        public static readonly float WELDER_AMOUNT_PER_SECOND = 1f;
        public static readonly float WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED = 0.6f;

        private static MyHudNotification m_weldingHintNotification = new MyHudNotification(MySpaceTexts.WelderPrimaryActionBuild, MyHudNotification.INFINITE, level: MyNotificationLevel.Control);
        private static MyHudNotificationBase m_missingComponentNotification = new MyHudNotification(MySpaceTexts.NotificationMissingComponentToPlaceBlockFormat, font: MyFontEnum.Red);

        static MyDefinitionId m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "WelderItem");

        private MySlimBlock m_failedBlock;
        private bool m_playedFailSound = false;

        private Vector3I m_targetProjectionCube;
        private MyCubeGrid m_targetProjectionGrid;

        public struct ProjectionRaycastData
        {
            public MyProjector.BuildCheckResult raycastResult;
            public MySlimBlock hitCube;
            public MyProjector cubeProjector;

            public ProjectionRaycastData(MyProjector.BuildCheckResult result, MySlimBlock cubeBlock, MyProjector projector)
            {
                raycastResult = result;
                hitCube = cubeBlock;
                cubeProjector = projector;
            }
        }

        public MyWelder()
            : base(MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(m_physicalItemId), 0.5f, 250)
        {
            HasCubeHighlight = true;
            HighlightColor = Color.Green * 0.45f;

            SecondaryLightIntensityLower = 0.4f;
            SecondaryLightIntensityUpper = 0.4f;

            SecondaryEffectId = MyParticleEffectsIDEnum.WelderSecondary;
            HasSecondaryEffect = false;

            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemId.TypeId, m_physicalItemId.SubtypeName);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Init(null, "Models\\Weapons\\Welder.mwm", null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;
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
            if (!MySession.Static.CreativeMode && !block.CanContinueBuild(character.GetInventory())) return false;

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
            MyHud.BlockInfo.BlockIcon = block.BlockDefinition.Icon;
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
                component.Icon = info.Component.Icon;
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
                return MySession.Static.WelderSpeedMultiplier * WELDER_AMOUNT_PER_SECOND * ToolCooldownMs / 1000.0f;
            }
        }

        public override bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if (!base.CanShoot(action, shooter, out status))
            {
                return false;
            }

            if (action == MyShootActionEnum.SecondaryAction)
            {
                return true;
            }


            status = MyGunStatusEnum.OK;
            var block = GetTargetBlock();
            if (block == null)
            {
                var info = FindProjectedBlock();
                if (info.raycastResult == MyProjector.BuildCheckResult.OK)
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
                if (info.raycastResult == MyProjector.BuildCheckResult.OK)
                {
                    return true;
                }
            }

            MyCharacter character = Owner as MyCharacter;
            if (!block.CanContinueBuild(character.GetInventory()))
            {
                status = MyGunStatusEnum.Failed;
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

        private MyProjector GetProjector(MySlimBlock block)
        {
            var projectorSlimBlock = block.CubeGrid.GetBlocks().FirstOrDefault(b => b.FatBlock is MyProjector);
            if (projectorSlimBlock != null)
            {
                return projectorSlimBlock.FatBlock as MyProjector;
            }

            return null;
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            base.Shoot(action, direction);
            
            if (action == MyShootActionEnum.PrimaryAction/* && IsPreheated*/ && Sync.IsServer)
            {
                var block = GetTargetBlock();
                if (block != null && CanWeld(block) && m_activated)
                {
                    Weld();
                }
                else
                {
                    var info = FindProjectedBlock();
                    if (info.raycastResult == MyProjector.BuildCheckResult.OK)
                    {
                        if (MySession.Static.CreativeMode || MyBlockBuilderBase.SpectatorIsBuilding || Owner.CanStartConstruction(info.hitCube.BlockDefinition))
                        {
                            info.cubeProjector.Build(info.hitCube, Owner.ControllerInfo.Controller.Player.Identity.IdentityId, Owner.EntityId);
                        }
                        else
                        {
                            MyBlockPlacerBase.OnMissingComponents(info.hitCube.BlockDefinition);
                        }
                    }
                }
            }
            return;
        }

        public override void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            base.BeginFailReaction(action, status);

            m_soundEmitter.PlaySingleSound(IDLE_SOUND, true, true);

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
        }

        protected override void AddHudInfo()
        {
            if (MyInput.Static.IsJoystickConnected())
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
                if (block.MaxDeformation > 0.0f || !block.IsFullIntegrity)
                {
                    float maxAllowedBoneMovement = WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED * ToolCooldownMs * 0.001f;
                    block.IncreaseMountLevel(WeldAmount, Owner.ControllerInfo.ControllingIdentityId, CharacterInventory, maxAllowedBoneMovement);
                }
            }
            
            var targetDestroyable = GetTargetDestroyable();
            if(targetDestroyable is MyCharacter && Sync.IsServer)
                targetDestroyable.DoDamage(20, MyDamageType.Drill, true);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (Owner != null && Owner == MySession.LocalCharacter)
            {
                CheckProjection();
            }

            if (Owner == null || MySession.ControlledEntity != Owner)
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
            if (info.raycastResult != MyProjector.BuildCheckResult.NotFound)
            {
                if (info.raycastResult == MyProjector.BuildCheckResult.OK)
                {
                    MyCubeBuilder.DrawSemiTransparentBox(info.hitCube.CubeGrid, info.hitCube, Color.Green.ToVector4(), true);
                    m_targetProjectionCube = info.hitCube.Position;
                    m_targetProjectionGrid = info.hitCube.CubeGrid;

                    return;
                }
                else if (info.raycastResult == MyProjector.BuildCheckResult.IntersectedWithGrid || info.raycastResult == MyProjector.BuildCheckResult.IntersectedWithSomethingElse)
                {
                    MyCubeBuilder.DrawSemiTransparentBox(info.hitCube.CubeGrid, info.hitCube, Color.Red.ToVector4(), true);
                }
                else if (info.raycastResult == MyProjector.BuildCheckResult.NotConnected)
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
                Vector3D startPosition = Sensor.Center;
                Vector3D forward = Sensor.FrontPoint - Sensor.Center;
                forward.Normalize();

                //Increased welder distance when projecting because it was hard to build on large grids
                float welderDistance = m_toolActionDistance * m_toolActionDistance * 2.0f;
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
                            if (canBuild == MyProjector.BuildCheckResult.OK)
                            {
                                farthestVisibleBlock = new ProjectionRaycastData
                                {
                                    raycastResult = canBuild,
                                    hitCube = projectionBlock.CubeBlock,
                                    cubeProjector = projector,
                                };
                            }
                            else if (canBuild == MyProjector.BuildCheckResult.AlreadyBuilt)
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
                raycastResult = MyProjector.BuildCheckResult.NotFound,
            };
        }

        protected override void StartLoopSound(bool effect)
        {
            MySoundPair cueEnum = effect ? METAL_SOUND : IDLE_SOUND;
            if(effect)
                m_soundEmitter.PlaySingleSound(METAL_SOUND, true, true);
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

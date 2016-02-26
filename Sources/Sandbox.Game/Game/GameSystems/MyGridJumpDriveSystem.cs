using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Entity;
using VRage.Game;

namespace Sandbox.Game.GameSystems
{
    public class MyGridJumpDriveSystem
    {
        public const float JUMP_DRIVE_DELAY = 10.0f; // seconds

        public const double MIN_JUMP_DISTANCE = 5000.0;

        private MyCubeGrid m_grid;
        
        private HashSet<MyJumpDrive> m_jumpDrives = new HashSet<MyJumpDrive>();
        private HashSet<MyCubeGrid> m_connectedGrids = new HashSet<MyCubeGrid>();
        private Dictionary<MyCubeGrid, Vector3D> m_shipInfo = new Dictionary<MyCubeGrid, Vector3D>();

        private List<MyEntity> m_entitiesInRange = new List<MyEntity>();
        private List<MyObjectSeed> m_objectsInRange = new List<MyObjectSeed>();
        private List<BoundingBoxD> m_obstaclesInRange = new List<BoundingBoxD>();
        private List<MyCharacter> m_characters = new List<MyCharacter>();

        private Vector3D m_selectedDestination;
        private Vector3D m_jumpDirection;
        private bool m_isJumping = false;
        private float m_prevJumpTime = 0f;
        private bool m_jumped = false;
        private bool m_effectPlayed;
        
        private float m_jumpTimeLeft;
        private bool m_playEffect = false;  // is local character affected by effect?
        
        private Vector3D? m_savedJumpDirection;
        private float? m_savedRemainingJumpTime;

        private MySoundPair m_chargingSound = new MySoundPair("ShipJumpDriveCharging");
        private MySoundPair m_jumpInSound = new MySoundPair("ShipJumpDriveJumpIn");
        private MySoundPair m_jumpOutSound = new MySoundPair("ShipJumpDriveJumpOut");
        protected MyEntity3DSoundEmitter m_soundEmitter;

        private MySyncJumpDriveSystem SyncObject;

        public MyGridJumpDriveSystem(MyCubeGrid grid)
        {
            m_grid = grid;

            SyncObject = new MySyncJumpDriveSystem(m_grid);
            m_soundEmitter = new MyEntity3DSoundEmitter(m_grid);
        }

        public void Init(Vector3D? jumpDriveDirection, float? remainingTimeForJump)
        {
            m_savedJumpDirection = jumpDriveDirection;
            m_savedRemainingJumpTime = remainingTimeForJump;

        }

        public Vector3D? GetJumpDriveDirection()
        {
            if (m_isJumping && !m_jumped)
            {
                return m_jumpDirection;
            }
            return null;
        }

        internal float? GetRemainingJumpTime()
        {
            if (m_isJumping && !m_jumped)
            {
                return m_jumpTimeLeft;
            }
            return null;
        }

        public void RegisterJumpDrive(MyJumpDrive jumpDrive)
        {
            m_jumpDrives.Add(jumpDrive);
        }

        public void UnregisterJumpDrive(MyJumpDrive jumpDrive)
        {
            m_jumpDrives.Remove(jumpDrive);
        }

        public void UpdateBeforeSimulation()
        {
            if (m_savedJumpDirection.HasValue)
            {
                UpdateConnectedGrids();
                m_shipInfo.Clear();

                foreach (var grid in m_connectedGrids)
                {
                    m_shipInfo.Add(grid, grid.WorldMatrix.Translation + m_savedJumpDirection.Value);
                }

                m_isJumping = true;
                m_jumped = false;
                m_jumpTimeLeft = m_savedRemainingJumpTime != null ? m_savedRemainingJumpTime.Value : 0f; 
                m_effectPlayed = false;

                m_savedJumpDirection = null;
                m_savedRemainingJumpTime = null;
            }

            UpdateJumpDriveSystem();
        }

        public double GetMaxJumpDistance(long userId)
        {
            UpdateConnectedGrids();
            double absoluteMaxDistance = 0f;
            double maxDistance = 0f;
            double mass = GetMass();
            foreach (var jumpDrive in m_jumpDrives)
            {
                if (jumpDrive.CanJumpAndHasAccess(userId))
                {
                    absoluteMaxDistance += jumpDrive.BlockDefinition.MaxJumpDistance;
                    maxDistance += jumpDrive.BlockDefinition.MaxJumpDistance * (jumpDrive.BlockDefinition.MaxJumpMass / mass);
                }
            }
            return Math.Min(absoluteMaxDistance, maxDistance);
        }

        private void DepleteJumpDrives(double distance, long userId)
        {
            double mass = GetMass();
            foreach (var jumpDrive in m_jumpDrives)
            {
                if (jumpDrive.CanJumpAndHasAccess(userId))
                {
                    jumpDrive.IsJumping = true;

                    double massRatio = (jumpDrive.BlockDefinition.MaxJumpMass) / (mass);
                    if (massRatio > 1.0)
                    {
                        massRatio = 1.0;
                    }
                    double jumpDistance = jumpDrive.BlockDefinition.MaxJumpDistance * massRatio;
                    if (jumpDistance < distance)
                    {
                        distance -= jumpDistance;
                        jumpDrive.SetStoredPower(0f);
                    }
                    else
                    {
                        double ratio = distance / jumpDistance;
                        jumpDrive.SetStoredPower(1.0f - (float)ratio);
                        return;
                    }
                }
            }
        }

        private bool IsJumpValid(long userId)
        {
            if (m_grid.MarkedForClose)
                return false;
            UpdateConnectedGrids();
            foreach (var grid in m_connectedGrids)
            {
                if (grid.IsStatic)
                {
                    return false;
                }
                if (grid.GridSystems.JumpSystem.m_isJumping)
                {
                    return false;
                }
            }

            double maxJumpDistance = GetMaxJumpDistance(userId);
            if (maxJumpDistance < MIN_JUMP_DISTANCE)
            {
                return false;
            }

            return true;
        }

        public void RequestAbort()
        {
            if (m_isJumping && !m_jumped)
            {
                SyncObject.SendAbortJump();
                AbortJump();
            }
        }

        public void RequestJump(string destinationName, Vector3D destination, long userId)
        {
            if (!Vector3.IsZero(MyGravityProviderSystem.CalculateNaturalGravityInPoint(m_grid.WorldMatrix.Translation)))
            {
                var notification = new MyHudNotification(MySpaceTexts.NotificationCannotJumpFromGravity, 1500);
                MyHud.Notifications.Add(notification);
                return;
            }
            if (!Vector3.IsZero(MyGravityProviderSystem.CalculateNaturalGravityInPoint(destination)))
            {
                var notification = new MyHudNotification(MySpaceTexts.NotificationCannotJumpIntoGravity, 1500);
                MyHud.Notifications.Add(notification);
                return;
            }

            if (!IsJumpValid(userId))
            {
                return;
            }

            m_selectedDestination = destination;
            double maxJumpDistance = GetMaxJumpDistance(userId);
            m_jumpDirection = destination - m_grid.WorldMatrix.Translation;
            double jumpDistance = m_jumpDirection.Length();
            double actualDistance = jumpDistance;
            if (jumpDistance > maxJumpDistance)
            {
                double ratio = maxJumpDistance / jumpDistance;
                actualDistance = maxJumpDistance;
                m_jumpDirection *= ratio;
            }

            if (actualDistance < MIN_JUMP_DISTANCE)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageText: GetWarningText(actualDistance),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning)
                    ));
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: GetConfimationText(destinationName, jumpDistance, actualDistance, userId),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    size: new Vector2(0.839375f, 0.3675f), callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES && IsJumpValid(userId))
                        {
                            SyncObject.RequestJump(m_selectedDestination, userId);
                        }
                        else
                            AbortJump();
                    }
                    ));
            }

        }

        private StringBuilder GetConfimationText(string name, double distance, double actualDistance, long userId)
        {
            int totalJumpDrives = m_jumpDrives.Count;
            int operationalJumpDrives = m_jumpDrives.Count((x) => x.CanJumpAndHasAccess(userId));

            distance /= 1000.0;
            actualDistance /= 1000.0;

            float percent = (float)(actualDistance / distance);
            if (percent > 1.0f)
            {
                percent = 1.0f;
            }

            GetCharactersInBoundingBox(GetAggregateBBox(), m_characters);
            int totalCharacters = 0;
            int seatedCharacters = 0;
            foreach (var character in m_characters)
            {
                if (!character.IsDead)
                {
                    totalCharacters++;
                    if (character.Parent != null)
                    {
                        seatedCharacters++;
                    }
                }
            }
            m_characters.Clear();

            StringBuilder result = new StringBuilder();

            result.Append("Jump destination: ").Append(name).Append("\n");
            result.Append("Distance to the proximity of coordinate: ").Append(distance.ToString("N")).Append(" Kilometers\n");
            result.Append("Achievable percentage of the jump: ").Append(percent.ToString("P")).Append(" (").Append(actualDistance.ToString("N")).Append(" Kilometers)\n");
			result.Append("Weight of transported mass: ").Append(MyHud.ShipInfo.Mass.ToString("N")).Append(" kg\n");
            result.Append("Operational jump drives: ").Append(operationalJumpDrives).Append("/").Append(totalJumpDrives).Append("\n");
            result.Append("Seated crew on board: ").Append(seatedCharacters).Append("/").Append(totalCharacters).Append("\n");

            return result;
        }

        private StringBuilder GetWarningText(double actualDistance)
        {
            StringBuilder result = new StringBuilder();
            result.Append("Distance to destination: ").Append(actualDistance.ToString("N")).Append(" Meters\n");
            result.Append("Minimum jump distance: ").Append(MIN_JUMP_DISTANCE.ToString("N")).Append(" Meters\n");
            return result;
        }

        private double GetMass()
        {
            double mass = 0f;

            foreach (var grid in m_connectedGrids)
            {
                mass += grid.Physics.Mass;
            }
            return mass;
        }

        private void UpdateConnectedGrids()
        {
            m_connectedGrids.Clear();
            var group = MyCubeGridGroups.Static.Physical.GetGroup(m_grid);
            if (group == null)
                Debug.Fail("Group is null");
            else
            {
                foreach (var node in group.Nodes)
                {
                    if (node.NodeData.Physics != null)
                    {
                        m_connectedGrids.Add(node.NodeData);
                    }
                }

            }
            var lgroup = MyCubeGridGroups.Static.Logical.GetGroup(m_grid);
            if (lgroup == null)
                Debug.Fail("Group is null");
            else
            {
                foreach (var node in lgroup.Nodes)
                {
                    if (!m_connectedGrids.Contains(node.NodeData))
                    {
                        if (node.NodeData.Physics != null)
                        {
                            m_connectedGrids.Add(node.NodeData);
                        }
                    }
                }
            }
        }

        private BoundingBoxD GetAggregateBBox()
        {
            if (m_grid.MarkedForClose)
                return BoundingBoxD.CreateInvalid();
            BoundingBoxD bbox = m_grid.PositionComp.WorldAABB;
            foreach (var grid in m_connectedGrids)
            {
                if(grid.PositionComp != null)
                    bbox.Include(grid.PositionComp.WorldAABB);
            }
            return bbox;
        }

        private void GetCharactersInBoundingBox(BoundingBoxD boundingBox, List<MyCharacter> characters)
        {
            MyGamePruningStructure.GetAllEntitiesInBox(ref boundingBox, m_entitiesInRange);
            foreach (var entity in m_entitiesInRange)
            {
                var character = entity as MyCharacter;
                if (character != null)
                {
                    characters.Add(character);
                }
            }
            m_entitiesInRange.Clear();
        }

        private Vector3D? FindSuitableJumpLocation(Vector3D desiredLocation)
        {
            BoundingBoxD shipBBox = GetAggregateBBox();
            // 1 Km distante to other objects to prevent spawning in bases
            shipBBox.Inflate(1000f);

            BoundingBoxD regionBBox = shipBBox.GetInflated(shipBBox.HalfExtents * 10);
            regionBBox.Translate(desiredLocation - regionBBox.Center);

            
            MyProceduralWorldGenerator.Static.OverlapAllPlanetSeedsInSphere(new BoundingSphereD(regionBBox.Center, regionBBox.HalfExtents.AbsMax()), m_objectsInRange);
            Vector3D currentSearchPosition = desiredLocation;
            foreach (var planet in m_objectsInRange)
            {
                if (planet.BoundingVolume.Contains(currentSearchPosition) != ContainmentType.Disjoint)
                {
                    Vector3D v = currentSearchPosition - planet.BoundingVolume.Center;
                    v.Normalize();
                    v *= planet.BoundingVolume.HalfExtents * 1.5;
                    currentSearchPosition = planet.BoundingVolume.Center + v;
                    break;
                }
            }
            m_objectsInRange.Clear();

            MyProceduralWorldGenerator.Static.OverlapAllAsteroidSeedsInSphere(new BoundingSphereD(regionBBox.Center, regionBBox.HalfExtents.AbsMax()), m_objectsInRange);
            foreach (var asteroid in m_objectsInRange)
            {
                m_obstaclesInRange.Add(asteroid.BoundingVolume);
            }
            m_objectsInRange.Clear();

            MyGamePruningStructure.GetTopMostEntitiesInBox(ref regionBBox, m_entitiesInRange);

            // Inflate the obstacles so we only need to check the center of the ship for collisions
            foreach (var entity in m_entitiesInRange)
            {
                if (!(entity is MyPlanet))
                {
                    m_obstaclesInRange.Add(entity.PositionComp.WorldAABB.GetInflated(shipBBox.HalfExtents));
                }
            }

            int maxStepCount = 10;
            int stepCount = 0;

            // When we collide with an obsticle, we add it here
            BoundingBoxD? aggregateCollidedObstacles = null;
            bool obstructed = false;
            bool found = false;

            while (stepCount < maxStepCount)
            {
                stepCount++;
                obstructed = false;
                foreach (var obstacle in m_obstaclesInRange)
                {
                    var contains = obstacle.Contains(currentSearchPosition);
                    if (contains == ContainmentType.Contains || 
                        contains == ContainmentType.Intersects)
                    {
                        if (!aggregateCollidedObstacles.HasValue)
                        {
                            aggregateCollidedObstacles = obstacle;
                        }
                        aggregateCollidedObstacles = aggregateCollidedObstacles.Value.Include(obstacle);
                        aggregateCollidedObstacles = aggregateCollidedObstacles.Value.Inflate(1.0);
                        currentSearchPosition = ClosestPointOnBounds(aggregateCollidedObstacles.Value, currentSearchPosition);
                        obstructed = true;
                        break;
                    }
                }

                if (!obstructed)
                {
                    // No obstacle found, return current search position
                    found = true;
                    break;
                }
            }

            m_obstaclesInRange.Clear();
            m_entitiesInRange.Clear();
            m_objectsInRange.Clear();

            if (found)
            {
                return currentSearchPosition;
            }
            else
            {
                return null;
            }

        }

        private Vector3D ClosestPointOnBounds(BoundingBoxD b, Vector3D p)
        {
            Vector3D pp = (p - b.Center) / b.HalfExtents;
            int maxComp = pp.AbsMaxComponent();
            if (maxComp == 0)
            {
                if (pp.X > 0) p.X = b.Max.X;
                else p.X = b.Min.X;
            }
            else if (maxComp == 1)
            {
                if (pp.Y > 0) p.Y = b.Max.Y;
                else p.Y = b.Min.Y;
            }
            else if (maxComp == 2)
            {
                if (pp.Z > 0) p.Z = b.Max.Z;
                else p.Z = b.Min.Z;
            }

            return p;
        }

        private bool IsLocalCharacterAffectedByJump(bool forceRecompute = false)
        {
            if (MySession.Static.LocalCharacter == null)
            {
                m_playEffect = false;
                return false;
            }

            if (m_playEffect && !forceRecompute) // cached value
                return true;

            GetCharactersInBoundingBox(GetAggregateBBox(), m_characters);
            foreach (var character in m_characters)
            {
                if (character == MySession.Static.LocalCharacter)
                {
                    if (character.Parent != null)
                    {
                        m_characters.Clear();
                        m_playEffect = true;
                        return true;
                    }
                }
            }
            m_characters.Clear();
            m_playEffect = false;
            return false;
        }

        private void Jump(Vector3D jumpTarget, long userId)
        {
            UpdateConnectedGrids();

            double maxJumpDistance = GetMaxJumpDistance(userId);
            Vector3D m_jumpDirection = jumpTarget - m_grid.WorldMatrix.Translation;
            double jumpDistance = m_jumpDirection.Length();
            double actualDistance = jumpDistance;
            if (jumpDistance > maxJumpDistance)
            {
                double ratio = maxJumpDistance / jumpDistance;
                actualDistance = maxJumpDistance;
                m_jumpDirection *= ratio;
            }

            DepleteJumpDrives(actualDistance, userId);

            m_isJumping = true;
            m_jumped = false;
            m_effectPlayed = false;
            m_jumpTimeLeft = JUMP_DRIVE_DELAY;

            m_shipInfo.Clear();
            
            foreach (var grid in m_connectedGrids)
            {
                m_shipInfo.Add(grid, grid.WorldMatrix.Translation + m_jumpDirection);
                grid.GridSystems.JumpSystem.m_jumpTimeLeft = m_jumpTimeLeft;
            }

            m_soundEmitter.PlaySound(m_chargingSound);
            m_prevJumpTime = 0f;
        }

        private void UpdateJumpDriveSystem()
        {
            if (m_isJumping)
            {
                float jumpTime = m_jumpTimeLeft;
                const float startJumpTime = 1.2f;
                const float particleTime = 0.3f;
                const float endJumpTime = -0.3f;

                m_jumpTimeLeft -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (jumpTime > startJumpTime)
                {
                    double roundTime = Math.Round(jumpTime);
                    if (roundTime != m_prevJumpTime)
                        if (IsLocalCharacterAffectedByJump())
                        {
                            var notification = new MyHudNotification(MySpaceTexts.NotificationJumpWarmupTime, 500);
                            notification.SetTextFormatArguments(roundTime);
                            MyHud.Notifications.Add(notification);
                        }
                }
                else if (jumpTime > 0)
                {
                    IsLocalCharacterAffectedByJump();
                    if (m_soundEmitter.SoundId != m_jumpOutSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(m_jumpOutSound);
                    }
                    UpdateJumpEffect(jumpTime / startJumpTime);

                    if (jumpTime < particleTime)
                    {
                        PlayParticleEffect();
                    }
                }
                else if (!m_jumped)
                {
                    if (Sync.IsServer)
                    {
                        if (m_shipInfo.ContainsKey(m_grid))
                        {
                            Vector3? suitableLocation = FindSuitableJumpLocation(m_shipInfo[m_grid]);
                            if (suitableLocation.HasValue)
                            {
                                SyncObject.SendPerformJump(suitableLocation.Value);
                                PerformJump(suitableLocation.Value);
                            }
                            else
                            {
                                SyncObject.SendAbortJump();
                                AbortJump();
                            }
                        }
                        else
                        {
                            SyncObject.SendAbortJump();
                            AbortJump();
                        }
                    }
                }
                else if (jumpTime > endJumpTime)
                {
                    UpdateJumpEffect(jumpTime / endJumpTime);
                }
                else
                {
                    CleanupAfterJump();
                    if (m_soundEmitter.SoundId != m_jumpInSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(m_jumpInSound);
                    }
                }
                m_prevJumpTime = (float)Math.Round(jumpTime);
            }
        }

        private void PlayParticleEffect()
        {
            if (m_effectPlayed) return;

            m_effectPlayed = true;
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(53, out effect))
            {
                effect.WorldMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, m_grid.WorldMatrix.Translation, Vector3D.One);
                effect.UserScale = (float)m_grid.PositionComp.WorldAABB.HalfExtents.AbsMax() / 25f;
                effect.AutoDelete = true;
            }
        }

        private void PerformJump(Vector3D jumpTarget)
        {
            m_jumpDirection = jumpTarget - m_grid.WorldMatrix.Translation;

            BoundingBoxD aggregateBox = m_grid.PositionComp.WorldAABB;
            foreach (var grid in m_shipInfo.Keys)
            {
                aggregateBox.Include(grid.PositionComp.WorldAABB);
            }
            MyPhysics.Clusters.EnsureClusterSpace(aggregateBox + m_jumpDirection);

            bool updateSpectator = false;
            if (IsLocalCharacterAffectedByJump())
            {
                updateSpectator = true;
            }

            if (updateSpectator)
            {
                MyThirdPersonSpectator.Static.ResetPosition(0.0, null);
                MyThirdPersonSpectator.Static.ResetDistance();
                MyThirdPersonSpectator.Static.RecalibrateCameraPosition();
            }

            m_jumped = true;

            foreach (var grid in m_shipInfo.Keys)
            {
                MatrixD gridMatrix = grid.WorldMatrix;
                gridMatrix.Translation = grid.WorldMatrix.Translation + m_jumpDirection;
                grid.WorldMatrix = gridMatrix;
            }

            if (updateSpectator)
            {
                MyThirdPersonSpectator.Static.ResetPosition(0.0, null);
                MyThirdPersonSpectator.Static.ResetDistance();
                MyThirdPersonSpectator.Static.RecalibrateCameraPosition();
            }
        }

        public void AbortJump()
        {
            m_soundEmitter.StopSound(true, true);
            if (m_isJumping && IsLocalCharacterAffectedByJump())
            {
                var notification = new MyHudNotification(MySpaceTexts.NotificationJumpAborted, 1500, MyFontEnum.Red, level: MyNotificationLevel.Important);
                MyHud.Notifications.Add(notification);
            }

            CleanupAfterJump();
        }

        private void CleanupAfterJump()
        {
            foreach (var jumpDrive in m_jumpDrives)
            {
                jumpDrive.IsJumping = false;
            }

            if (IsLocalCharacterAffectedByJump())
                MySector.MainCamera.FieldOfView = MySandboxGame.Config.FieldOfView;
            m_jumped = false;
            m_isJumping = false;
        }

        public void AfterGridClose()
        {
            if (m_isJumping)
            {
                m_soundEmitter.StopSound(true, true);
                CleanupAfterJump();
            }
        }

        private void UpdateJumpEffect(float t)
        {
            if (m_playEffect)
            {
                float maxFov = MathHelper.ToRadians(170.0f);
                float fov = MathHelper.SmoothStep(MySandboxGame.Config.FieldOfView, maxFov, 1f - t);
                MySector.MainCamera.FieldOfView = fov;
            }
        }

        public bool CheckReceivedCoordinates(ref Vector3D pos)
        {
            if (m_jumpTimeLeft > 20)
                return true;
            if (Vector3D.DistanceSquared(m_grid.PositionComp.GetPosition(), pos) > 10000 * 10000)
            {
                //most likely comes from packet created before jump
                MySandboxGame.Log.WriteLine(string.Format("Wrong position packet received, dist={0}, T={1})", Vector3D.Distance(m_grid.PositionComp.GetPosition(), pos), m_jumpTimeLeft));
                return false;
            }
            return true;

        }

        #region Sync
        private void OnRequestJumpFromClient(Vector3D jumpTarget, long userId)
        {
            Debug.Assert(Sync.IsServer);

            if (!IsJumpValid(userId))
            {
                SyncObject.SendJumpFailure();
                return;
            }

            double maxJumpDistance = GetMaxJumpDistance(userId);
            Vector3D m_jumpDirection = jumpTarget - m_grid.WorldMatrix.Translation;
            double jumpDistance = m_jumpDirection.Length();
            double actualDistance = jumpDistance;
            if (jumpDistance > maxJumpDistance)
            {
                double ratio = maxJumpDistance / jumpDistance;
                actualDistance = maxJumpDistance;
                m_jumpDirection *= ratio;
            }

            if (actualDistance < MIN_JUMP_DISTANCE-200)
            {
                SyncObject.SendJumpFailure();
                return;
            }

            Vector3D? suitableJumpLocation = FindSuitableJumpLocation(jumpTarget);
            if (!suitableJumpLocation.HasValue)
            {
                SyncObject.SendJumpFailure();
                return;
            }

            SyncObject.SendJumpSuccess(suitableJumpLocation.Value, userId);
        }

        [PreloadRequired]
        internal class MySyncJumpDriveSystem
        {
            [MessageIdAttribute(8500, P2PMessageEnum.Reliable)]
            protected struct RequestJumpMsg
            {
                public long EntityId;
                public long UserId;
                public Vector3D JumpTarget;
            }

            [MessageIdAttribute(8501, P2PMessageEnum.Reliable)]
            protected struct JumpSuccessMsg
            {
                public long EntityId;
                public long UserId;
                public Vector3D JumpTarget;
            }

            [MessageIdAttribute(8502, P2PMessageEnum.Reliable)]
            protected struct JumpFailureMsg
            {
                public long EntityId;
            }

            [MessageIdAttribute(8503, P2PMessageEnum.Reliable)]
            protected struct PerformJumpMsg
            {
                public long EntityId;
                public Vector3D JumpTarget;
            }

            [MessageIdAttribute(8504, P2PMessageEnum.Reliable)]
            protected struct AbortJumpMsg
            {
                public long EntityId;
            }

            private MyCubeGrid m_grid;

            static MySyncJumpDriveSystem()
            {
                MySyncLayer.RegisterMessage<RequestJumpMsg>(OnJumpRequested, MyMessagePermissions.ToServer);
                MySyncLayer.RegisterMessage<JumpSuccessMsg>(OnJumpSuccess, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<JumpFailureMsg>(OnJumpFailure, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<PerformJumpMsg>(OnPerformJump, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<AbortJumpMsg>(OnAbortJump, MyMessagePermissions.FromServer);
            }

            public MySyncJumpDriveSystem(MyCubeGrid cubeGrid)
            {
                m_grid = cubeGrid;
            }

            public void RequestJump(Vector3D jumpTarget, long userId)
            {
                var msg = new RequestJumpMsg();
                msg.EntityId = m_grid.EntityId;
                msg.JumpTarget = jumpTarget;
                msg.UserId = userId;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnJumpRequested(ref RequestJumpMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    cubeGrid.GridSystems.JumpSystem.OnRequestJumpFromClient(msg.JumpTarget, msg.UserId);
                }
            }

            public void SendJumpSuccess(Vector3D jumpTarget, long userId)
            {
                var msg = new JumpSuccessMsg();
                msg.EntityId = m_grid.EntityId;
                msg.JumpTarget = jumpTarget;
                msg.UserId = userId;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            private static void OnJumpSuccess(ref JumpSuccessMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    cubeGrid.GridSystems.JumpSystem.Jump(msg.JumpTarget, msg.UserId);
                }
            }

            public void SendJumpFailure()
            {
                var msg = new JumpFailureMsg();
                msg.EntityId = m_grid.EntityId;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            private static void OnJumpFailure(ref JumpFailureMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    //TODO(AF) Add a notification, maybe a reason
                }
            }

            public void SendPerformJump(Vector3D jumpTarget)
            {
                var msg = new PerformJumpMsg();
                msg.EntityId = m_grid.EntityId;
                msg.JumpTarget = jumpTarget;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnPerformJump(ref PerformJumpMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    cubeGrid.GridSystems.JumpSystem.PerformJump(msg.JumpTarget);
                }
            }

            public void SendAbortJump()
            {
                var msg = new AbortJumpMsg();
                msg.EntityId = m_grid.EntityId;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnAbortJump(ref AbortJumpMsg msg, MyNetworkClient sender)
            {
                MyCubeGrid cubeGrid;
                MyEntities.TryGetEntityById(msg.EntityId, out cubeGrid);
                if (cubeGrid != null)
                {
                    cubeGrid.GridSystems.JumpSystem.AbortJump();
                }
            }
        }
        #endregion
    }
}

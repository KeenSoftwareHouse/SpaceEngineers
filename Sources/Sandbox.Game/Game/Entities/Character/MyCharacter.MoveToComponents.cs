#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Utils;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Components;
using VRage.FileSystem;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;

#endregion

namespace Sandbox.Game.Entities.Character
{
    #region Structs


    public struct MyFeetIKSettings
    {
        public bool Enabled;
        public float BelowReachableDistance; // distance reachable below the character's rigid body
        public float AboveReachableDistance; // distance reachable above character's ground (how high foot can be placed)
        public float VerticalShiftUpGain; // how quickly can shift character's root
        public float VerticalShiftDownGain; // how quickly can crouch..
        public Vector3 FootSize; // x = foot width, y = foot height, z = foot lenght/size
    }

    #endregion


    public partial class MyCharacter : 
        MySkinnedEntity,
        IMyCameraController, 
        IMyControllableEntity, 
        IMyInventoryOwner, 
        IMyPowerConsumer, 
        IMyComponentOwner<MyDataBroadcaster>, 
        IMyComponentOwner<MyDataReceiver>, 
        IMyUseObject, 
        IMyDestroyableObject, 
        Sandbox.ModAPI.IMyCharacter
    {

        public float EnvironmentOxygenLevel;

        private float m_suitOxygenAmount;
        public float SuitOxygenAmount
        {
            get
            {
                return m_suitOxygenAmount;
            }
            set
            {
                m_suitOxygenAmount = value;
                if (m_suitOxygenAmount > Definition.OxygenCapacity)
                {
                    m_suitOxygenAmount = Definition.OxygenCapacity;
                }
            }
        }
        public float SuitOxygenAmountMissing
        {
            get
            {
                return Definition.OxygenCapacity - SuitOxygenAmount;
            }
        }
        public float SuitOxygenLevel
        {
            get
            {
                if (Definition.OxygenCapacity == 0)
                {
                    return 0;
                }
                return m_suitOxygenAmount / Definition.OxygenCapacity;
            }
            set
            {
                m_suitOxygenAmount = value * Definition.OxygenCapacity;
            }
        }
        private float m_oldSuitOxygenLevel;
        bool m_needsOxygen;

        public static readonly float LOW_OXYGEN_RATIO = 0.2f;
        MyHudNotification m_lowOxygenNotification;
        MyHudNotification m_criticalOxygenNotification;
        MyHudNotification m_oxygenBottleRefillNotification;






        private void UpdateChat()
        {
            if (MySession.LocalCharacter == this)
            {
                MyChatHistory chatHistory;
                if (MySession.Static.ChatHistory.TryGetValue(MySession.LocalPlayerId, out chatHistory))
                {
                    foreach (var chatPlayerHistory in chatHistory.PlayerChatHistory)
                    {
                        foreach (var chatItem in chatPlayerHistory.Value.Chat)
                        {
                            if (!chatItem.Sent)
                            {
                                MyPlayer.PlayerId playerId;
                                if (MySession.Static.Players.TryGetPlayerId(chatPlayerHistory.Key, out playerId))
                                {
                                    SyncObject.SendNewPlayerMessage(MySession.LocalHumanPlayer.Id, playerId, chatItem.Text, chatItem.Timestamp);
                                }
                                else
                                {
                                    Debug.Fail("Message to send has invalid IdentityId!");
                                }
                            }
                        }
                    }

                }
            }
        }

      

        #region Oxygen
        private void UpdateOxygen()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return;
            }

            // Try to find grids that might contain oxygen
            var entities = new List<MyEntity>();
            var aabb = PositionComp.WorldAABB;
            MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref aabb, entities);
            bool lowOxygenDamage = true;
            bool noOxygenDamage = true;
            bool isInEnvironment = true;

            EnvironmentOxygenLevel = MyOxygenProviderSystem.GetOxygenInPoint(PositionComp.GetPosition());

            var cockpit = Parent as MyCockpit;
            if (cockpit != null && cockpit.BlockDefinition.IsPressurized)
            {
                if (Sync.IsServer && MySession.Static.SurvivalMode)
                {
                    if (!Definition.NeedsOxygen && m_suitOxygenAmount > Definition.OxygenConsumption)
                    {
                        m_suitOxygenAmount -= Definition.OxygenConsumption;
                        if (m_suitOxygenAmount < 0f)
                        {
                            m_suitOxygenAmount = 0f;
                        }
                    }

                    if (cockpit.OxygenLevel > 0f)
                    {
                        if (Definition.NeedsOxygen)
                        {
                            if (cockpit.OxygenAmount >= Definition.OxygenConsumption)
                            {
                                cockpit.OxygenAmount -= Definition.OxygenConsumption;

                                noOxygenDamage = false;
                                lowOxygenDamage = false;
                            }
                        }
                        else
                        {
                            float oxygenTransferred = Math.Min(SuitOxygenAmountMissing, cockpit.OxygenAmount);
                            oxygenTransferred = Math.Min(oxygenTransferred, MyOxygenConstants.OXYGEN_REGEN_PER_SECOND);

                            cockpit.OxygenAmount -= oxygenTransferred;
                            SuitOxygenAmount += oxygenTransferred;

                            noOxygenDamage = false;
                            lowOxygenDamage = false;
                        }
                    }
                }
                EnvironmentOxygenLevel = cockpit.OxygenLevel;
                isInEnvironment = false;
            }
            else
            {
                Vector3D pos = PositionComp.WorldMatrix.Translation;
                if (m_headBoneIndex != -1)
                {
                    pos = (BoneAbsoluteTransforms[m_headBoneIndex] * WorldMatrix).Translation;
                }
                foreach (var entity in entities)
                {
                    var grid = entity as MyCubeGrid;
                    // Oxygen can be present on small grids as well because of mods
                    if (grid != null)
                    {
                        var oxygenBlock = grid.GridSystems.OxygenSystem.GetSafeOxygenBlock(pos);
                        if (oxygenBlock.Room != null)
                        {
                            if (oxygenBlock.Room.OxygenLevel(grid.GridSize) > Definition.PressureLevelForLowDamage)
                            {
                                if (Definition.NeedsOxygen)
                                {
                                    lowOxygenDamage = false;
                                }
                            }

                            if (oxygenBlock.Room.IsPressurized)
                            {
                                EnvironmentOxygenLevel = oxygenBlock.Room.OxygenLevel(grid.GridSize);
                                if (oxygenBlock.Room.OxygenAmount > Definition.OxygenConsumption)
                                {
                                    if (Definition.NeedsOxygen)
                                    {
                                        noOxygenDamage = false;
                                        oxygenBlock.PreviousOxygenAmount = oxygenBlock.OxygenAmount() - Definition.OxygenConsumption;
                                        oxygenBlock.OxygenChangeTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                                        oxygenBlock.Room.OxygenAmount -= Definition.OxygenConsumption;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                EnvironmentOxygenLevel = oxygenBlock.Room.EnvironmentOxygen;
                                if (EnvironmentOxygenLevel > Definition.OxygenConsumption)
                                {
                                    if (Definition.NeedsOxygen)
                                    {
                                        noOxygenDamage = false;
                                    }
                                    break;
                                }
                            }

                            isInEnvironment = false;
                        }
                    }
                }
            }

            if (MySession.LocalCharacter == this)
            {
                if (m_oldSuitOxygenLevel >= 0.25f && SuitOxygenLevel < 0.25f)
                {
                    MyHud.Notifications.Add(m_lowOxygenNotification);
                }
                else if (m_oldSuitOxygenLevel >= 0.05f && SuitOxygenLevel < 0.05f)
                {
                    MyHud.Notifications.Add(m_criticalOxygenNotification);
                }
            }
            m_oldSuitOxygenLevel = SuitOxygenLevel;

            // Cannot early exit before calculations because of UI
            if (!Sync.IsServer || MySession.Static.CreativeMode)
            {
                return;
            }

            //TODO(AF) change this to a constant
            //Try to refill the suit from bottles in inventory
            if (SuitOxygenLevel < 0.3f && !Definition.NeedsOxygen)
            {
                var items = Inventory.GetItems();
                bool bottlesUsed = false;
                foreach (var item in items)
                {
                    var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                    if (oxygenContainer != null)
                    {
                        if (oxygenContainer.OxygenLevel == 0f)
                        {
                            continue;
                        }

                        var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                        float oxygenAmount = oxygenContainer.OxygenLevel * physicalItem.Capacity;

                        float transferredAmount = Math.Min(oxygenAmount, SuitOxygenAmountMissing);
                        oxygenContainer.OxygenLevel = (oxygenAmount - transferredAmount) / physicalItem.Capacity;

                        if (oxygenContainer.OxygenLevel < 0f)
                        {
                            oxygenContainer.OxygenLevel = 0f;
                        }


                        if (oxygenContainer.OxygenLevel > 1f)
                        {
                            Debug.Fail("Incorrect value");
                        }

                        Inventory.UpdateOxygenAmount();
                        Inventory.SyncOxygenContainerLevel(item.ItemId, oxygenContainer.OxygenLevel);

                        bottlesUsed = true;

                        SuitOxygenAmount += transferredAmount;
                        if (SuitOxygenLevel == 1f)
                        {
                            break;
                        }
                    }
                }
                if (bottlesUsed)
                {
                    if (MySession.LocalCharacter == this)
                    {
                        ShowRefillFromBottleNotification();
                    }
                    else
                    {
                        SyncObject.SendRefillFromBottle();
                    }
                }
            }

            // No oxygen found in room, try to get it from suit
            if (noOxygenDamage || lowOxygenDamage)
            {
                if (!Definition.NeedsOxygen && m_suitOxygenAmount > Definition.OxygenConsumption)
                {
                    m_suitOxygenAmount -= Definition.OxygenConsumption;
                    if (m_suitOxygenAmount < 0f)
                    {
                        m_suitOxygenAmount = 0f;
                    }
                    noOxygenDamage = false;
                    lowOxygenDamage = false;
                }

                if (isInEnvironment)
                {
                    if (EnvironmentOxygenLevel > Definition.PressureLevelForLowDamage)
                    {
                        lowOxygenDamage = false;
                    }
                    if (EnvironmentOxygenLevel > 0f)
                    {
                        noOxygenDamage = false;
                    }
                }
            }

            if (noOxygenDamage)
            {
                DoDamage(Definition.DamageAmountAtZeroPressure, MyDamageType.LowPressure, true);
            }
            else if (lowOxygenDamage)
            {
                DoDamage(1f, MyDamageType.Asphyxia, true);
            }

            SyncObject.UpdateOxygen(SuitOxygenAmount);
        }

        public void ShowRefillFromBottleNotification()
        {
            MyHud.Notifications.Add(m_oxygenBottleRefillNotification);
        }
        #endregion


        #region Fields


        float m_verticalFootError = 0;
        float m_cummulativeVerticalFootError = 0;

        #endregion

        #region IK
            
        void CalculateHandIK(int startBoneIndex, int endBoneIndex, ref MatrixD targetTransform)
        {
            MyCharacterBone endBone = Bones[endBoneIndex];
            MyCharacterBone startBone = Bones[startBoneIndex];

            // Solve IK Problem
            List<MyCharacterBone> bones = new List<MyCharacterBone>();

            for (int i = startBoneIndex; i <= endBoneIndex; i++)
                bones.Add(Bones[i]);

            MatrixD invWorld = PositionComp.WorldMatrixNormalizedInv;
            Matrix localFinalTransform = targetTransform * invWorld;
            Vector3 finalPos = localFinalTransform.Translation;
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(targetTransform.Translation, "Hand target transform", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(targetTransform.Translation, 0.03f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawAxis((MatrixD)targetTransform, 0.03f, false);
            }

            MyInverseKinematics.SolveCCDIk(ref finalPos, bones, 0.0005f, 5, 0.5f, ref localFinalTransform, endBone);
            //MyInverseKinematics.SolveTwoJointsIk(ref finalPos, bones[0], bones[1], bones[2], ref localFinalTransform, WorldMatrix, bones[3],false);

        }

        void CalculateHandIK(int upperarm, int forearm, int palm, ref MatrixD targetTransform)
        {
            Debug.Assert(Bones.IsValidIndex(upperarm), "UpperArm index for IK is invalid");
            Debug.Assert(Bones.IsValidIndex(forearm), "ForeArm index for IK is invalid");
            Debug.Assert(Bones.IsValidIndex(palm), "Palm index for IK is invalid");

            MatrixD invWorld = PositionComp.WorldMatrixNormalizedInv;
            Matrix localFinalTransform = targetTransform * invWorld;
            Vector3 finalPos = localFinalTransform.Translation;
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_IKSOLVERS)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(targetTransform.Translation, "Hand target transform", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(targetTransform.Translation, 0.03f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawAxis((MatrixD)targetTransform, 0.03f, false);
            }

            //MyInverseKinematics.SolveCCDIk(ref finalPos, bones, 0.0005f, 5, 0.5f, ref localFinalTransform, endBone);
            if (Bones.IsValidIndex(upperarm) && Bones.IsValidIndex(forearm) && Bones.IsValidIndex(palm))
            {
                MyInverseKinematics.SolveTwoJointsIkCCD(ref finalPos, Bones[upperarm], Bones[forearm], Bones[palm], ref localFinalTransform, WorldMatrix, Bones[palm], false);
            }

        }

        /// <summary>
        /// This updates the foot placement in the world using raycasting and finding closest support. 
        /// </summary>
        /// <param name="upDirection">This direction is used to raycast from feet - must be normalized!</param>
        /// <param name="underFeetReachableDistance">How below the original position can character reach down with legs</param>
        /// <param name="maxFootHeight">How high from the original position can be foot placed</param>
        /// <param name="verticalChangeGainUp">How quickly we raise up the character</param>
        /// <param name="verticalChangeGainDown">How quickly we crouch down</param>
        /// <param name="maxDistanceSquared">This is the maximal error in foot placement</param>
        /// <param name="footDimensions">This is foot dimensions, in Y axis is the ankle's height</param>
        /// <param name="footPlacementDistanceSquared">This is the distance limit between calculated and current position to start IK on foot placement</param>
        void UpdateFeetPlacement(Vector3 upDirection, float belowCharacterReachableDistance, float aboveCharacterReachableDistance, float verticalShiftUpGain, float verticalShiftDownGain, Vector3 footDimensions)
        {
            Debug.Assert(footDimensions != Vector3.Zero, "void UpdateFeetPlacement(...) : foot dimensions can not be zero!");

            float ankleHeight = footDimensions.Y;

            // get the current foot matrix and location
            Matrix invWorld = PositionComp.WorldMatrixInvScaled;
            MyCharacterBone rootBone = Bones[m_rootBone]; // root bone is used to transpose the model up or down
            Matrix modelRootBoneMatrix = rootBone.AbsoluteTransform;
            float verticalShift = modelRootBoneMatrix.Translation.Y;
            Matrix leftFootMatrix = Bones[m_leftAnkleBone].AbsoluteTransform;
            Matrix rightFootMatrix = Bones[m_rightAnkleBone].AbsoluteTransform;

            // ok first we get the closest support to feet and we need to know from where to raycast for each foot in world coords
            // we need to raycast from original ground position of the feet, no from the character shifted position            
            // we cast from the ground of the model space, assuming the model's local space up vector is in Y axis
            Vector3 leftFootGroundPosition = new Vector3(leftFootMatrix.Translation.X, 0, leftFootMatrix.Translation.Z);
            Vector3 rightFootGroundPosition = new Vector3(rightFootMatrix.Translation.X, 0, rightFootMatrix.Translation.Z);
            Vector3 fromL = Vector3.Transform(leftFootGroundPosition, WorldMatrix);  // we get this position in the world
            Vector3 fromR = Vector3.Transform(rightFootGroundPosition, WorldMatrix);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetClosestFootPosition");

            // find the closest ground support, raycasting from Up to Down
            var contactLeft = MyInverseKinematics.GetClosestFootSupportPosition(this, null, fromL, upDirection, footDimensions, WorldMatrix, belowCharacterReachableDistance, aboveCharacterReachableDistance, Physics.CharacterCollisionFilter);        // this returns world coordinates of support for left foot
            var contactRight = MyInverseKinematics.GetClosestFootSupportPosition(this, null, fromR, upDirection, footDimensions, WorldMatrix, belowCharacterReachableDistance, aboveCharacterReachableDistance, Physics.CharacterCollisionFilter);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Characters root shift estimation");

            // if we got hit only for one feet, we do nothing, but slowly return back root bone (character vertical shift) position if it was changed from original
            // that happends very likely when the support below is too far for one leg
            if (contactLeft == null || contactRight == null)
            {
                modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                rootBone.SetBindTransform(modelRootBoneMatrix);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            // Here we recalculate if we shift the root of the character to reach bottom or top
            // get the desired foot world coords

            Vector3 supportL = contactLeft.Value.Position;
            Vector3 supportR = contactRight.Value.Position;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_CLOSESTSUPPORTPOSITION)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(supportL, "Foot support position", Color.Blue, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(supportR, "Foot support position", Color.Blue, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(supportL, 0.03f, Color.Blue, 0, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(supportR, 0.03f, Color.Blue, 0, false);
            }

            // Get the vector between actual feet position and possible support in local model coords
            //                                  local model space coord of desired position + shift it up of ankle heights
            Vector3 leftAnkleDesiredPosition = Vector3.Transform(supportL, invWorld) + ((leftFootMatrix.Translation.Y - verticalShift) * upDirection);
            Vector3 rightAnkleDesiredPosition = Vector3.Transform(supportR, invWorld) + ((rightFootMatrix.Translation.Y - verticalShift) * upDirection);

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_DESIREDPOSITION)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(supportL, "Ankle desired position", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(supportR, "Ankle desired position", Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(leftAnkleDesiredPosition, WorldMatrix), 0.03f, Color.Purple, 0, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(rightAnkleDesiredPosition, WorldMatrix), 0.03f, Color.Purple, 0, false);
            }

            // Get the height of found support related to character's position in model's local space, assuming it's Y axis
            float leftAnkleDesiredHeight = leftAnkleDesiredPosition.Y;
            float rightAnkleDesiredHeight = rightAnkleDesiredPosition.Y;

            // if we the distances are too big, so we will not be able to set the position, we can skip it
            if (Math.Abs(leftAnkleDesiredHeight - rightAnkleDesiredHeight) > aboveCharacterReachableDistance)
            {
                modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                rootBone.SetBindTransform(modelRootBoneMatrix);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            // if we got one of the supports below the character root, we must check whether we can reach it, if yes, we need to crouch to reach it            
            if ((((leftAnkleDesiredHeight > -belowCharacterReachableDistance) && (leftAnkleDesiredHeight < ankleHeight)) ||      // left support is below model and is reachable
                ((rightAnkleDesiredHeight > -belowCharacterReachableDistance) && (rightAnkleDesiredHeight < ankleHeight))) &&    // right support is below model and is reachable
                // finally check if character is shifted down, the other feet won't get too high
                (Math.Max(leftAnkleDesiredHeight, rightAnkleDesiredHeight) - Math.Min(leftAnkleDesiredHeight, rightAnkleDesiredHeight) < aboveCharacterReachableDistance))
            {
                // then we can try to reach down according to the difference
                float distanceBelow = Math.Min(leftAnkleDesiredHeight, rightAnkleDesiredHeight) - ankleHeight;
                Vector3 verticalTranslation = upDirection * distanceBelow;
                Vector3 translation = modelRootBoneMatrix.Translation;
                translation.Interpolate3(modelRootBoneMatrix.Translation, verticalTranslation, verticalShiftDownGain);
                modelRootBoneMatrix.Translation = translation;
                rootBone.SetBindTransform(modelRootBoneMatrix);
            }
            else // if both supports are up, we need to get up, however, that should be done by rigid body as well.. we limit it only by reachable distance, so it is bounded to rigid body position
                if ((leftAnkleDesiredHeight > ankleHeight) && (leftAnkleDesiredHeight < aboveCharacterReachableDistance) &&
                    (rightAnkleDesiredHeight > ankleHeight) && (rightAnkleDesiredHeight < aboveCharacterReachableDistance))
                {
                    // move up to reach the highest support
                    float distanceAbove = Math.Max(leftAnkleDesiredHeight, rightAnkleDesiredHeight) - ankleHeight;
                    Vector3 verticalTranslation = upDirection * distanceAbove;
                    Vector3 translation = modelRootBoneMatrix.Translation;
                    translation.Interpolate3(modelRootBoneMatrix.Translation, verticalTranslation, verticalShiftUpGain);
                    modelRootBoneMatrix.Translation = translation;
                    rootBone.SetBindTransform(modelRootBoneMatrix);
                }
                // finally if we can not get into right vertical position for foot placement, slowly reset the vertical shift
                else
                {
                    modelRootBoneMatrix.Translation -= modelRootBoneMatrix.Translation * verticalShiftUpGain;
                    rootBone.SetBindTransform(modelRootBoneMatrix);
                }

            // Hard limit to root's shift in vertical position
            //if (characterVerticalShift < -underFeetReachableDistance)
            //{
            //    modelRootBoneMatrix.Translation = -upDirection * underFeetReachableDistance;
            //    rootBone.SetBindTransform(modelRootBoneMatrix);
            //    characterVerticalShift = -underFeetReachableDistance; // get the new height
            //}
            //if (characterVerticalShift > underFeetReachableDistance)
            //{
            //    modelRootBoneMatrix.Translation = upDirection * underFeetReachableDistance;
            //    rootBone.SetBindTransform(modelRootBoneMatrix);
            //    characterVerticalShift = underFeetReachableDistance; // get the new height
            //}

            // Then we need to recalculate all other bones matrices so we get proper data for children, since we changed the root position
            //foreach (var b in m_bones) b.ComputeAbsoluteTransform();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CalculateFeetPlacement");

            // Then recalculate feet positions only if we can reach the final and if we are in the limits
            if ((-belowCharacterReachableDistance < leftAnkleDesiredHeight) && (leftAnkleDesiredHeight < aboveCharacterReachableDistance)) // and the found foot support height is not over limit                
                CalculateFeetPlacement(
                    m_leftHipBone,
                    m_leftKneeBone,
                    m_leftAnkleBone,
                    leftAnkleDesiredPosition,
                    contactLeft.Value.Normal,
                    footDimensions,
                    leftFootMatrix.Translation.Y - verticalShift <= ankleHeight);

            if ((-belowCharacterReachableDistance < rightAnkleDesiredHeight) && (rightAnkleDesiredHeight < aboveCharacterReachableDistance))
                CalculateFeetPlacement(
                    m_rightHipBone,
                    m_rightKneeBone,
                    m_rightAnkleBone,
                    rightAnkleDesiredPosition,
                    contactRight.Value.Normal,
                    footDimensions,
                    rightFootMatrix.Translation.Y - verticalShift <= ankleHeight);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_BONES)
            {
                List<Matrix> left = new List<Matrix> { Bones[m_leftHipBone].AbsoluteTransform, Bones[m_leftKneeBone].AbsoluteTransform, Bones[m_leftAnkleBone].AbsoluteTransform };
                List<Matrix> right = new List<Matrix> { Bones[m_rightHipBone].AbsoluteTransform, Bones[m_rightKneeBone].AbsoluteTransform, Bones[m_rightAnkleBone].AbsoluteTransform };
                debugDrawBones(left);
                debugDrawBones(right);
                VRageRender.MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation, "Rigid body", Color.Yellow, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(WorldMatrix.Translation, 0.05f, Color.Yellow, 0, false);
                VRageRender.MyRenderProxy.DebugDrawText3D((modelRootBoneMatrix * WorldMatrix).Translation, "Character root bone", Color.Yellow, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere((modelRootBoneMatrix * WorldMatrix).Translation, 0.07f, Color.Red, 0, false);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Storing bones transforms");

            // After the feet placement we need to save new bone transformations
            for (int i = 0; i < Bones.Count; i++)
            {
                MyCharacterBone bone = Bones[i];
                BoneRelativeTransforms[i] = bone.ComputeBoneTransform();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        void debugDrawBones(List<Matrix> boneTransforms)
        {
            Vector3 p1 = Vector3.Zero;
            Vector3 p2;
            foreach (var boneMat in boneTransforms)
            {

                VRageRender.MyRenderProxy.DebugDrawAxis(boneMat * WorldMatrix, 1f, false);
                p2 = (boneMat * PositionComp.WorldMatrix).Translation;

                if (!Vector3.IsZero(p1)) VRageRender.MyRenderProxy.DebugDrawLine3D(p1, p2, Color.Yellow, Color.Yellow, false);

                p1 = p2;
            }
        }


        /// <summary>
        /// This calculates new bone rotations for legs so the foot is placed on supported ground or object, which is found by RayCast
        /// </summary>
        /// <param name="hipBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="kneeBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="FeetBoneIndex">it is the index of the hip bone in m_bones list</param>
        /// <param name="footPosition">this is the model space of the final foot placement, it is then recalculated to local model space</param>
        void CalculateFeetPlacement(int hipBoneIndex, int kneeBoneIndex, int ankleBoneIndex, Vector3 footPosition, Vector3 footNormal, Vector3 footDimensions, bool setFootTransform)
        {

            Vector3 endPos = footPosition;

            List<MyCharacterBone> boneTransforms = new List<MyCharacterBone>();
            boneTransforms.Add(Bones[hipBoneIndex]);
            boneTransforms.Add(Bones[kneeBoneIndex]);
            boneTransforms.Add(Bones[ankleBoneIndex]);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate foot transform");

            Matrix finalAnkleTransform = Matrix.Identity;

            if (setFootTransform)
            {
                // compute rotation based on normal of foot support position
                Vector3 currentUp = WorldMatrix.Up;
                currentUp.Normalize();
                Vector3 crossResult = Vector3.Cross(currentUp, footNormal);
                crossResult.Normalize();
                double cosAngle = currentUp.Dot(footNormal);
                cosAngle = MathHelper.Clamp(cosAngle, -1, 1);
                double turnAngle = Math.Acos(cosAngle);	// get the angle
                Matrix rotation = Matrix.CreateFromAxisAngle((Vector3)crossResult, (float)turnAngle);

                // now rotate the model world to the rotation
                if (rotation.IsValid()) finalAnkleTransform = WorldMatrix * rotation;
                else finalAnkleTransform = WorldMatrix;
                // but the position of this world will be different
                finalAnkleTransform.Translation = Vector3.Transform(footPosition, WorldMatrix);

                // compute transformation in model space
                MatrixD invWorld = PositionComp.WorldMatrixNormalizedInv;
                finalAnkleTransform = finalAnkleTransform * invWorld;

                // get the original ankleSpace
                MatrixD originalAngleTransform = Bones[ankleBoneIndex].BindTransform * Bones[ankleBoneIndex].Parent.AbsoluteTransform;

                // now it needs to be related to rig transform
                finalAnkleTransform = originalAngleTransform.GetOrientation() * finalAnkleTransform.GetOrientation();

            }

            finalAnkleTransform.Translation = footPosition;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("IK Calculation");

            MyInverseKinematics.SolveTwoJointsIk(ref endPos, Bones[hipBoneIndex], Bones[kneeBoneIndex], Bones[ankleBoneIndex], ref finalAnkleTransform, WorldMatrix, setFootTransform ? Bones[ankleBoneIndex] : null, false);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            // draw our foot placement transformation
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_ANKLE_FINALPOS)
            {
                Matrix debug = finalAnkleTransform * WorldMatrix;
                Matrix debug2 = Bones[ankleBoneIndex].AbsoluteTransform * WorldMatrix;
                VRageRender.MyRenderProxy.DebugDrawText3D(debug.Translation, "Final ankle position", Color.Red, 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimensions) * debug, Color.Red, 1, false, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(debug2.Translation, "Actual ankle position", Color.Green, 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(footDimensions) * debug2, Color.Green, 1, false, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(debug.Translation, debug.Translation + debug.Forward * 0.5f, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(debug.Translation, debug.Translation + debug.Up * 0.2f, Color.Purple, Color.Purple, false);
            }

        }


        /// <summary>
        /// Updates feet bones positions, locations and rotation using IK, based on current character state
        /// </summary>
        private void UpdateFeet()
        {

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateFeetPlacement standing");

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_SETTINGS)
            {
                MyFeetIKSettings feetDebugSettings;
                m_characterDefinition.FeetIKSettings.TryGetValue(MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE, out feetDebugSettings);
                Matrix leftFootMatrix = Bones[m_leftAnkleBone].AbsoluteTransform;
                Matrix rightFootMatrix = Bones[m_rightAnkleBone].AbsoluteTransform;
                Vector3 upDirection = WorldMatrix.Up;
                Vector3 leftFootGroundPosition = new Vector3(leftFootMatrix.Translation.X, 0, leftFootMatrix.Translation.Z);
                Vector3 rightFootGroundPosition = new Vector3(rightFootMatrix.Translation.X, 0, rightFootMatrix.Translation.Z);
                Vector3 fromL = Vector3.Transform(leftFootGroundPosition, WorldMatrix);  // we get this position in the world
                Vector3 fromR = Vector3.Transform(rightFootGroundPosition, WorldMatrix);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromL, fromL + upDirection * feetDebugSettings.AboveReachableDistance, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromL, fromL - upDirection * feetDebugSettings.BelowReachableDistance, Color.Red, Color.Red, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromR, fromR + upDirection * feetDebugSettings.AboveReachableDistance, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromR, fromR - upDirection * feetDebugSettings.BelowReachableDistance, Color.Red, Color.Red, false);
                Matrix leftFoot = Matrix.CreateScale(feetDebugSettings.FootSize) * WorldMatrix;
                Matrix rightFoot = Matrix.CreateScale(feetDebugSettings.FootSize) * WorldMatrix;
                leftFoot.Translation = fromL;
                rightFoot.Translation = fromR;
                VRageRender.MyRenderProxy.DebugDrawOBB(leftFoot, Color.White, 1f, false, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(rightFoot, Color.White, 1f, false, false);
            }

            MyFeetIKSettings feetSettings;

            if (m_characterDefinition.FeetIKSettings.TryGetValue(GetCurrentMovementState(), out feetSettings))
            {
                if (feetSettings.Enabled)
                {
                    UpdateFeetPlacement(WorldMatrix.Up,
                        feetSettings.BelowReachableDistance,
                        feetSettings.AboveReachableDistance,
                        feetSettings.VerticalShiftUpGain,
                        feetSettings.VerticalShiftDownGain,
                        feetSettings.FootSize);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }
        #endregion
    }
}

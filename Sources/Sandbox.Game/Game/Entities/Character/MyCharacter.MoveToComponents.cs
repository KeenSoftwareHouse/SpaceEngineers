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
        IMyComponentOwner<MyDataBroadcaster>, 
        IMyComponentOwner<MyDataReceiver>, 
        IMyUseObject, 
        IMyDestroyableObject, 
        Sandbox.ModAPI.IMyCharacter
    {
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

        #endregion
    }
}

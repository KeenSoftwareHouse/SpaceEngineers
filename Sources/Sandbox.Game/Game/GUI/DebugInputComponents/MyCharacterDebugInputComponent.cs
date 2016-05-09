#region Using

using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Animations;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions.Animation;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Game.Models;
using VRage.Game.SessionComponents;
using VRage.Generics;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.Game.Gui
{
    class MyCharacterInputComponent : MyDebugComponent
    {
        private bool m_toggleMovementState = false;
        private bool m_toggleShowSkeleton = false;

        private readonly List<MyStateMachineNode> m_animControllerCurrentNodes = new List<MyStateMachineNode>();
        private readonly List<int[]> m_animControllerTreePath = new List<int[]>();
        private const int m_editorSendCounterInterval = 60;
        private int m_editorSendCounter = m_editorSendCounterInterval;
        private string m_lastAnimationControllerName = null;

        public override string GetName()
        {
            return "Character";
        }

        public MyCharacterInputComponent()
        {
            AddShortcut(MyKeys.U, true, false, false, false,
               () => "Spawn new character",
               delegate
               {
                   var character = SpawnCharacter();
                   return true;
               });

            AddShortcut(MyKeys.NumPad1, false, false, false, false,
               () => "Kill everyone around you",
               delegate
               {
                   KillEveryoneAround();
                   return true;
               });

            AddShortcut(MyKeys.NumPad5, true, false, false, false,
               () => "Reconnect to the AC editor",
               delegate
               {
                   SendControllerNameToEditor();
                   return true;
               });

            AddShortcut(MyKeys.NumPad7, true, true, false, false,
               () => "Use next ship",
               delegate
               {
                   UseNextShip();
                   return true;
               });

            AddShortcut(MyKeys.NumPad8, true, false, false, false,
               () => "Toggle skeleton view",
               delegate
               {
                   ToggleSkeletonView();
                   return true;
               });

            AddShortcut(MyKeys.NumPad9, true, false, false, false,
                () => "Reload animations (old system)",
                delegate
                {
                    ReloadAnimationsOldSystem();
                    return true;
                });

            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Toggle character movement status", () => { ShowMovementState(); return true; });
        }

        private void KillEveryoneAround()
        {
            if (MySession.Static.LocalCharacter == null || !Sync.IsServer || !MySession.Static.IsAdmin ||
                !MySession.Static.IsAdminMenuEnabled)
                return;

            Vector3D myPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();
            Vector3D offset = new Vector3D(25, 25, 25);
            BoundingBoxD bb = new BoundingBoxD(myPosition - offset, myPosition + offset);

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInBox(ref bb, entities);

            foreach (var entity in entities)
            {
                var character = entity as MyCharacter;
                if (character != null && entity != MySession.Static.LocalCharacter)
                {
                    character.DoDamage(1000000, MyDamageType.Unknown, true);
                }
            }

            MyRenderProxy.DebugDrawAABB(bb, Color.Red, 0.5f, 1f, true, true);
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;

            return base.HandleInput();
        }

        private void ToggleSkeletonView()
        {
            m_toggleShowSkeleton = !m_toggleShowSkeleton;
        }

        private void SendControllerNameToEditor()
        {
            if (MySessionComponentExtDebug.Static == null || MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.Definition.AnimationController == null
                || MySession.Static.LocalCharacter.AnimationController.Controller == null)
                return;

            var msg = new MyExternalDebugStructures.ACConnectToEditorMsg()
            {
                ACName = MySession.Static.LocalCharacter.Definition.AnimationController
            };
            
            m_lastAnimationControllerName = msg.ACName;
            MySessionComponentExtDebug.Static.SendMessageToClients(msg);
            if (!MySessionComponentExtDebug.Static.IsHandlerRegistered(ReceivedMessageHandler))
                MySessionComponentExtDebug.Static.ReceivedMsg += ReceivedMessageHandler;
        }
        
        private void ReloadAnimationsOldSystem()
        {
            if (MySession.Static.LocalCharacter != null)
            foreach (var animPlayer in MySession.Static.LocalCharacter.GetAllAnimationPlayers())
            {
                MySession.Static.LocalCharacter.PlayerStop(animPlayer.Key, 0.0f);
            }

            foreach (var animationDefinition in MyDefinitionManager.Static.GetAnimationDefinitions())
            {
                MyModel model = MyModels.GetModel(animationDefinition.AnimationModel);
                if (model != null)
                    model.UnloadData();
                MyModel modelFps = MyModels.GetModel(animationDefinition.AnimationModelFPS);
                if (modelFps != null)
                    modelFps.UnloadData();
            }
        }

        private void ReceivedMessageHandler(MyExternalDebugStructures.CommonMsgHeader messageHeader, IntPtr messageData)
        {
            MyExternalDebugStructures.ACReloadInGameMsg msgReload;
            if (MyExternalDebugStructures.ReadMessageFromPtr(ref messageHeader, messageData, out msgReload))
            {
                try
                {
                    string acAddress = msgReload.ACAddress;
                    string acName = msgReload.ACName;

                    MyObjectBuilder_Definitions allDefinitions; // = null;
                    // load animation controller definition from SBC file
                    if (MyObjectBuilderSerializer.DeserializeXML(acAddress, out allDefinitions) &&
                        allDefinitions.Definitions != null &&
                        allDefinitions.Definitions.Length > 0)
                    {
                        var firstDef = allDefinitions.Definitions[0];
                        MyModContext context = new MyModContext();
                        context.Init("AnimationControllerDefinition", Path.GetFileName(acAddress));
                        MyAnimationControllerDefinition animationControllerDefinition = new MyAnimationControllerDefinition();
                        animationControllerDefinition.Init(firstDef, context);

                        // swap animation controller for each entity
                        foreach (MyEntity entity in MyEntities.GetEntities())
                        {
                            MyCharacter character = entity as MyCharacter;
                            if (character != null && character.Definition.AnimationController == acName)
                            {
                                character.AnimationController.Clear();
                                character.AnimationController.InitFromDefinition(animationControllerDefinition);
                                character.ObtainBones();
                            }
                        }

                        // update in def. manager
                        MyStringHash animSubtypeNameHash = MyStringHash.GetOrCompute(acName);
                        MyAnimationControllerDefinition animControllerDefInManager =
                            MyDefinitionManager.Static.GetDefinition<MyAnimationControllerDefinition>(animSubtypeNameHash);
                        animControllerDefInManager.Init(firstDef, context);
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e);
                }
            }
        }

        private void SendAnimationStateChangesToEditor()
        {
            if (MySession.Static == null || MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.Definition.AnimationController == null
                || !MySessionComponentExtDebug.Static.HasClients)
                return;

            var animController = MySession.Static.LocalCharacter.AnimationController.Controller;
            if (animController == null)
                return;

            int layerCount = animController.GetLayerCount();
            if (layerCount != m_animControllerCurrentNodes.Count)
            {
                m_animControllerCurrentNodes.Clear();
                for (int i = 0; i < layerCount; i++)
                    m_animControllerCurrentNodes.Add(null);

                m_animControllerTreePath.Clear();
                for (int i = 0; i < layerCount; i++)
                {
                    m_animControllerTreePath.Add(new int[animController.GetLayerByIndex(i).VisitedTreeNodesPath.Length]);
                }
            }

            for (int i = 0; i < layerCount; i++)
            {
                var layerVisitedTreeNodesPath = animController.GetLayerByIndex(i).VisitedTreeNodesPath;
                if (animController.GetLayerByIndex(i).CurrentNode != m_animControllerCurrentNodes[i]
                    || !CompareAnimTreePathSeqs(layerVisitedTreeNodesPath, m_animControllerTreePath[i]))
                {
                    Array.Copy(layerVisitedTreeNodesPath, m_animControllerTreePath[i], layerVisitedTreeNodesPath.Length); // local copy
                    m_animControllerCurrentNodes[i] = animController.GetLayerByIndex(i).CurrentNode;
                    if (m_animControllerCurrentNodes[i] != null)
                    {
                        var msg =
                            MyExternalDebugStructures.ACSendStateToEditorMsg.Create(m_animControllerCurrentNodes[i].Name, m_animControllerTreePath[i]);
                        MySessionComponentExtDebug.Static.SendMessageToClients(msg);
                    }
                }
            }
        }

        private static bool CompareAnimTreePathSeqs(int[] seq1, int[] seq2)
        {
            if (seq1 == null || seq2 == null || seq1.Length != seq2.Length)
                return false;

            for (int i = 0; i < seq1.Length; i++)
            {
                if (seq1[i] != seq2[i])
                    return false;
                if (seq1[i] == 0 && seq2[i] == 0)
                    return true;
            }

            return true;
        }

        public static MyCharacter SpawnCharacter(string model = null)
        {
            var charObject = MySession.Static.LocalHumanPlayer == null ? null : MySession.Static.LocalHumanPlayer.Identity.Character;
            Vector3? colorMask = null;

            string name = MySession.Static.LocalHumanPlayer == null ? "" : MySession.Static.LocalHumanPlayer.Identity.DisplayName;
            string currentModel = MySession.Static.LocalHumanPlayer == null ? MyCharacter.DefaultModel : MySession.Static.LocalHumanPlayer.Identity.Model;

            if (charObject != null)
                colorMask = charObject.ColorMask;

            var character = MyCharacter.CreateCharacter(MatrixD.CreateTranslation(MySector.MainCamera.Position), Vector3.Zero, name, model ?? currentModel, colorMask, null, false);
            return character;
        }


        public static void UseNextShip()
        {
            MyCockpit first = null;
            object previous = null;
            foreach (var g in MyEntities.GetEntities().OfType<MyCubeGrid>())
            {
                //if (g.GridSizeEnum == CommonLib.ObjectBuilders.MyCubeSize.Large)
                {
                    foreach (var cockpit in g.GetBlocks().Select(s => s.FatBlock as MyCockpit).Where(s => (s != null)))
                    {
                        if (first == null && cockpit.Pilot == null)
                            first = cockpit;

                        if (previous == MySession.Static.ControlledEntity)
                        {
                            if (cockpit.Pilot == null)
                            {
                                UseCockpit(cockpit);
                                return;
                            }
                        }
                        else
                        {
                            previous = cockpit;
                        }
                    }
                }
            }

            if (first != null)
            {
                UseCockpit(first);
            }
        }

        private static void UseCockpit(MyCockpit cockpit)
        {
            if (MySession.Static.LocalHumanPlayer == null) return;

            // Leave current cockpit if controlling any
            if (MySession.Static.ControlledEntity is MyCockpit)
            {
                MySession.Static.ControlledEntity.Use();
            }
            cockpit.RequestUse(UseActionEnum.Manipulate, MySession.Static.LocalHumanPlayer.Identity.Character);
            cockpit.RemoveOriginalPilotPosition();
        }

        private void ShowMovementState()
        {
            m_toggleMovementState = !m_toggleMovementState;
        }

        public override void Draw()
        {
            base.Draw();

            if (MySession.Static != null && MySession.Static.LocalCharacter != null)
            {
                m_editorSendCounter--;
                if (m_editorSendCounter <= 0)
                {
                    m_editorSendCounter = m_editorSendCounterInterval;
                    SendControllerNameToEditor();
                }
            }

            if (m_toggleMovementState)
            {
                var allCharacters = MyEntities.GetEntities().OfType<MyCharacter>();
                Vector2 initPos = new Vector2(10, 200);
                foreach (var character in allCharacters)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(initPos, character.GetCurrentMovementState().ToString(), Color.Green, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    initPos += new Vector2(0, 20);
                }
            }

            if (MySession.Static != null && MySession.Static.LocalCharacter != null)
                Text("Character look speed: {0}", MySession.Static.LocalCharacter.RotationSpeed);

            if (MySession.Static != null && MySession.Static.LocalCharacter != null)
            {
                var animController = MySession.Static.LocalCharacter.AnimationController;
                System.Text.StringBuilder str = new System.Text.StringBuilder(1024);
                if (animController != null && animController.Controller != null && animController.Controller.GetLayerByIndex(0) != null)
                {
                    str.Clear();
                    foreach (int seqNum in animController.Controller.GetLayerByIndex(0).VisitedTreeNodesPath)
                    {
                        if (seqNum == 0)
                            break;
                        str.Append(seqNum);
                        str.Append(",");
                    }
                    Text(str.ToString());
                }

                if (animController != null && animController.Variables != null)
                    foreach (var variable in animController.Variables.AllVariables)
                    {
                        str.Clear();
                        str.Append(variable.Key);
                        str.Append(" = ");
                        str.Append(variable.Value);
                        Text(str.ToString());
                    }
            }

            if (m_toggleShowSkeleton)
                DrawSkeleton();

            if (MySession.Static != null && MySession.Static.LocalCharacter != null &&
                MySession.Static.LocalCharacter.Definition.AnimationController != null
                && MySession.Static.LocalCharacter.Definition.AnimationController != m_lastAnimationControllerName)
            {
                SendControllerNameToEditor();
            }
            VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(300, 10), "Debugging AC " + m_lastAnimationControllerName, Color.Yellow, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            SendAnimationStateChangesToEditor();

            // debugging old animation system
            if (MySession.Static != null && MySession.Static.LocalCharacter != null
                && MySession.Static.LocalCharacter.Definition != null 
                && MySession.Static.LocalCharacter.Definition.AnimationController == null)
            {
                var allAnimationPlayers = MySession.Static.LocalCharacter.GetAllAnimationPlayers();
                float posY = 40;
                foreach (var animationPlayer in allAnimationPlayers)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(400, posY), (animationPlayer.Key != "" ? animationPlayer.Key : "Body") + ": "
                        + animationPlayer.Value.ActualPlayer.AnimationNameDebug + " (" + animationPlayer.Value.ActualPlayer.AnimationMwmPathDebug + ")", 
                        Color.Lime, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    posY += 30;
                }
            }
        }
         
        private Dictionary<MyCharacterBone, int> m_boneRefToIndex = null;

        // Terrible, unoptimized function for visual debugging.
        // Draw skeleton using raw data from animation controller.
        private void DrawSkeleton()
        {
            if (m_boneRefToIndex == null)
            { 
                // lazy initialization of this debugging feature
                m_boneRefToIndex = new Dictionary<MyCharacterBone, int>(256);
            }

            MyCharacter character = MySession.Static != null ? MySession.Static.LocalCharacter : null;
            if (character == null)
                return;
            List<MyAnimationClip.BoneState> bones = character.AnimationController.LastRawBoneResult;
            MyCharacterBone[] characterBones = character.AnimationController.CharacterBones;
            m_boneRefToIndex.Clear();
            for (int i = 0; i < characterBones.Length; i++)
            {
                m_boneRefToIndex.Add(character.AnimationController.CharacterBones[i], i);
            }


            for (int i = 0; i < characterBones.Length; i++)
                if (characterBones[i].Parent == null)
                {
                    MatrixD worldMatrix = character.PositionComp.WorldMatrix;
                    DrawBoneHierarchy(character, ref worldMatrix, characterBones, bones, i);
                }
        }

        private void DrawBoneHierarchy(MyCharacter character, ref MatrixD parentTransform, MyCharacterBone[] characterBones, List<MyAnimationClip.BoneState> rawBones, int boneIndex)
        {
            // ----------------------------
            // raw animation data
            MatrixD currentTransform = rawBones != null ? Matrix.CreateTranslation(rawBones[boneIndex].Translation) * parentTransform : MatrixD.Identity;
            currentTransform = rawBones != null ? Matrix.CreateFromQuaternion(rawBones[boneIndex].Rotation) * currentTransform : currentTransform;
            if (rawBones != null)
            {
                MyRenderProxy.DebugDrawLine3D(currentTransform.Translation, parentTransform.Translation, Color.Green, Color.Green, false);
            }
            bool anyChildren = false;
            for (int i = 0; characterBones[boneIndex].GetChildBone(i) != null; i++)
            {
                var childBone = characterBones[boneIndex].GetChildBone(i);
                DrawBoneHierarchy(character, ref currentTransform, characterBones, rawBones,
                    m_boneRefToIndex[childBone]);
                anyChildren = true;
            }
            if (!anyChildren && rawBones != null)
            {
                MyRenderProxy.DebugDrawLine3D(currentTransform.Translation, currentTransform.Translation + currentTransform.Left * 0.05f, Color.Green, Color.Cyan, false);
            }

            // ----------------------------
            // final animation data - after IK, ragdoll...
            MyRenderProxy.DebugDrawText3D(Vector3D.Transform(characterBones[boneIndex].AbsoluteTransform.Translation, character.PositionComp.WorldMatrix), characterBones[boneIndex].Name, Color.Lime, 0.4f, false);
            if (characterBones[boneIndex].Parent != null)
            {
                Vector3D boneStartPos = Vector3D.Transform(characterBones[boneIndex].AbsoluteTransform.Translation, character.PositionComp.WorldMatrix);
                Vector3D boneEndPos = Vector3D.Transform(characterBones[boneIndex].Parent.AbsoluteTransform.Translation, character.PositionComp.WorldMatrix);
                MyRenderProxy.DebugDrawLine3D(boneStartPos, boneEndPos, Color.Purple, Color.Purple, false);
            }
            if (!anyChildren)
            {
                Vector3D boneStartPos = Vector3D.Transform(characterBones[boneIndex].AbsoluteTransform.Translation, character.PositionComp.WorldMatrix);
                Vector3D boneEndPos = Vector3D.Transform(characterBones[boneIndex].AbsoluteTransform.Translation + characterBones[boneIndex].AbsoluteTransform.Left * 0.05f, character.PositionComp.WorldMatrix);
                MyRenderProxy.DebugDrawLine3D(boneStartPos, boneEndPos, Color.Purple, Color.Red, false);
            }
        }
    }
}

#region Using

using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using VRageRender.Animations;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Game.Models;
using VRage.Game.SessionComponents;
using VRage.Input;
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

        private const int m_maxLastAnimationActions = 20;
        private List<string> m_lastAnimationActions = new List<string>(m_maxLastAnimationActions);

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
                   SpawnCharacter();
                   return true;
               });

            AddShortcut(MyKeys.NumPad1, false, false, false, false,
               () => "Kill everyone around you",
               delegate
               {
                   KillEveryoneAround();
                   return true;
               });

            AddShortcut(MyKeys.NumPad7, true, false, false, false,
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
                () => "Reload animation tracks",
                delegate
                {
                    ReloadAnimations();
                    return true;
                });

            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Toggle character movement status", () => { ShowMovementState(); return true; });
        }

        private void KillEveryoneAround()
        {
            if (MySession.Static.LocalCharacter == null || !Sync.IsServer || !MySession.Static.HasCreativeRights ||
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
        
        private void ReloadAnimations()
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

            MySessionComponentAnimationSystem.Static.ReloadMwmTracks();
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
                MyAnimationInverseKinematics.DebugTransform = MySession.Static.LocalCharacter.WorldMatrix;
            }

            if (m_toggleMovementState)
            {
                var allCharacters = MyEntities.GetEntities().OfType<MyCharacter>();
                Vector2 initPos = new Vector2(10, 200);
                foreach (var character in allCharacters)
                {
                    MyRenderProxy.DebugDrawText2D(initPos, character.GetCurrentMovementState().ToString(), Color.Green, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
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

                if (animController != null)
                {
                    if (animController.LastFrameActions != null)
                    {
                        foreach (MyStringId actionId in animController.LastFrameActions)
                            m_lastAnimationActions.Add(actionId.ToString());

                        if (m_lastAnimationActions.Count > m_maxLastAnimationActions)
                            m_lastAnimationActions.RemoveRange(0, m_lastAnimationActions.Count - m_maxLastAnimationActions);
                    }

                    Text(Color.Red, "--- RECENTLY TRIGGERED ACTIONS ---");
                    foreach (var action in m_lastAnimationActions)
                        Text(Color.Yellow, action);
                } 
                 
            }

            if (m_toggleShowSkeleton)
                DrawSkeleton();

            MyRenderProxy.DebugDrawText2D(new Vector2(300, 10), "Debugging AC " + m_animationControllerName, Color.Yellow, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            
            // debugging old animation system
            if (MySession.Static != null && MySession.Static.LocalCharacter != null
                && MySession.Static.LocalCharacter.Definition != null 
                && MySession.Static.LocalCharacter.Definition.AnimationController == null)
            {
                var allAnimationPlayers = MySession.Static.LocalCharacter.GetAllAnimationPlayers();
                float posY = 40;
                foreach (var animationPlayer in allAnimationPlayers)
                {
                    MyRenderProxy.DebugDrawText2D(new Vector2(400, posY), (animationPlayer.Key != "" ? animationPlayer.Key : "Body") + ": "
                        + animationPlayer.Value.ActualPlayer.AnimationNameDebug + " (" + animationPlayer.Value.ActualPlayer.AnimationMwmPathDebug + ")", 
                        Color.Lime, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    posY += 30;
                }
            }
        }
         
        private Dictionary<MyCharacterBone, int> m_boneRefToIndex = null;
        private string m_animationControllerName;

        // Terrible, unoptimized function for visual debugging.
        // Draw skeleton using raw data from animation controller.
        private void DrawSkeleton()
        {
            if (m_boneRefToIndex == null)
            { 
                // lazy initialization of this debugging feature
                m_boneRefToIndex = new Dictionary<MyCharacterBone, int>(256);
            }

            if (MySessionComponentAnimationSystem.Static == null)
                return;


            foreach (var animComp in MySessionComponentAnimationSystem.Static.RegisteredAnimationComponents)
            {
                MyCharacter character = animComp != null ? (animComp.Entity as MyCharacter) : null;
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

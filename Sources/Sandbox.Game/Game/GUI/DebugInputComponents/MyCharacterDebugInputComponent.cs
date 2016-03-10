#region Using

using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using VRage.Animations;
using VRage.Game.Entity.UseObject;
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

            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Toggle character movement status", () => { ShowMovementState(); return true; });
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;

            if (base.HandleInput())
                return true;
            
            bool handled = false;
            return handled;
        }

        private void ToggleSkeletonView()
        {
            m_toggleShowSkeleton = !m_toggleShowSkeleton;
        }

        public static MyCharacter SpawnCharacter(string model = null)
        {
            var charObject = MySession.Static.LocalHumanPlayer == null ? null : MySession.Static.LocalHumanPlayer.Identity.Character as MyCharacter;
            Vector3? colorMask = null;

            string name = MySession.Static.LocalHumanPlayer == null ? "" : MySession.Static.LocalHumanPlayer.Identity.DisplayName;
            string currentModel = MySession.Static.LocalHumanPlayer == null ? MyCharacter.DefaultModel : MySession.Static.LocalHumanPlayer.Identity.Model;

            if (charObject != null)
                colorMask = charObject.ColorMask;

            var character = MyCharacter.CreateCharacter(MatrixD.CreateTranslation(MySector.MainCamera.Position), Vector3.Zero, name, model == null ? currentModel : model, colorMask, null, false);
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
            cockpit.RequestUse(UseActionEnum.Manipulate, (MyCharacter)MySession.Static.LocalHumanPlayer.Identity.Character);
            cockpit.RemoveOriginalPilotPosition();
        }

        private void ShowMovementState()
        {
            m_toggleMovementState = !m_toggleMovementState;
        }

        public override void Draw()
        {
            base.Draw();

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

            if (m_toggleShowSkeleton)
                DrawSkeleton();
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
            if (bones == null)
                return;
            m_boneRefToIndex.Clear();
            for (int i = 0; i < bones.Count; i++)
            {
                m_boneRefToIndex.Add(character.AnimationController.CharacterBones[i], i);
            }


            for (int i = 0; i < bones.Count; i++)
                if (characterBones[i].Parent == null)
                {
                    MatrixD worldMatrix = character.PositionComp.WorldMatrix;
                    DrawBoneHierarchy(ref worldMatrix, characterBones, bones, i);
                }
        }

        private void DrawBoneHierarchy(ref MatrixD parentTransform, MyCharacterBone[] characterBones, List<MyAnimationClip.BoneState> rawBones, int boneIndex)
        {
            MatrixD currentTransform = Matrix.CreateTranslation(rawBones[boneIndex].Translation) * parentTransform;
            currentTransform = Matrix.CreateFromQuaternion(rawBones[boneIndex].Rotation) * currentTransform;
            MyRenderProxy.DebugDrawLine3D(currentTransform.Translation, parentTransform.Translation, Color.Green, Color.Green, false);
            if (characterBones[boneIndex].Parent == null)
                MyRenderProxy.DebugDrawText3D(currentTransform.Translation, characterBones[boneIndex].Name, Color.Green, 1.0f, false);
            for (int i = 0; characterBones[boneIndex].GetChildBone(i) != null; i++)
            {
                var childBone = characterBones[boneIndex].GetChildBone(i);
                DrawBoneHierarchy(ref currentTransform, characterBones, rawBones,
                    m_boneRefToIndex[childBone]);
            }
        }
    }
}

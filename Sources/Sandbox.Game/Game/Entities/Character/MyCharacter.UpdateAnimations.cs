#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Graphics;
using VRage.Animations;
using VRage.Import;
using VRageMath;
using VRage.Utils;
using VRageRender;
using VRage;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Havok;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Definitions.Animation;
using VRage.Game.Entity;


#endregion

namespace Sandbox.Game.Entities.Character
{
    public partial class MyCharacter
    {
        #region Fields

        static string TopBody = "LeftHand RightHand LeftFingers RightFingers Head Spine";

        bool m_resetWeaponAnimationState;

        #endregion

        #region Animations init


        void InitAnimations()
        {
            foreach (var bones in m_characterDefinition.BoneSets)
            {
                AddAnimationPlayer(bones.Key, bones.Value);
            }

            SetBoneLODs(m_characterDefinition.BoneLODs);

            AnimationController.FindBone(m_characterDefinition.HeadBone, out m_headBoneIndex);
            AnimationController.FindBone(m_characterDefinition.Camera3rdBone, out m_camera3rdBoneIndex);
            if (m_camera3rdBoneIndex == -1)
                m_camera3rdBoneIndex = m_headBoneIndex;
            AnimationController.FindBone(m_characterDefinition.LeftHandIKStartBone, out m_leftHandIKStartBone);
            AnimationController.FindBone(m_characterDefinition.LeftHandIKEndBone, out m_leftHandIKEndBone);
            AnimationController.FindBone(m_characterDefinition.RightHandIKStartBone, out m_rightHandIKStartBone);
            AnimationController.FindBone(m_characterDefinition.RightHandIKEndBone, out m_rightHandIKEndBone);

            AnimationController.FindBone(m_characterDefinition.LeftUpperarmBone, out m_leftUpperarmBone);
            AnimationController.FindBone(m_characterDefinition.LeftForearmBone, out m_leftForearmBone);
            AnimationController.FindBone(m_characterDefinition.RightUpperarmBone, out m_rightUpperarmBone);
            AnimationController.FindBone(m_characterDefinition.RightForearmBone, out m_rightForearmBone);

            AnimationController.FindBone(m_characterDefinition.WeaponBone, out m_weaponBone);
            AnimationController.FindBone(m_characterDefinition.LeftHandItemBone, out m_leftHandItemBone);
            AnimationController.FindBone(m_characterDefinition.RighHandItemBone, out m_rightHandItemBone);
            AnimationController.FindBone(m_characterDefinition.SpineBone, out m_spineBone);
            
            UpdateAnimation(0);
        }


        public override void UpdateToolPosition()
        {
            if (m_currentWeapon != null)
            {
                if (!MyPerGameSettings.CheckUseAnimationInsteadOfIK(m_currentWeapon))
                {
                    UpdateWeaponPosition();
                }
            }
        }

        protected override void CalculateTransforms(float distance)
        {
            ProfilerShort.Begin("MyCharacter.CalculateTransforms");

            base.CalculateTransforms(distance);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateLeftHandItemPosition");
            if (m_leftHandItem != null)
            {
                UpdateLeftHandItemPosition();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Calculate Hand IK");


            if (this == MySession.Static.ControlledEntity)
            {
                // (OM) Note: only controlled character can get it's aimed point from camera, otherwise all character's will aim the same direction
                // set the aimed point explicitly using AimedPoint property
                m_aimedPoint = GetAimedPointFromCamera();
            }
            
            if (m_currentWeapon != null)
            {
                if (!MyPerGameSettings.CheckUseAnimationInsteadOfIK(m_currentWeapon))
                {
                    UpdateWeaponPosition(); //mainly IK and some zoom + ironsight stuff
                    if (m_handItemDefinition.SimulateLeftHand && m_leftHandIKStartBone != -1 && m_leftHandIKEndBone != -1 && (!UseAnimationForWeapon && m_animationToIKState == 0))
                    {
                        MatrixD leftHand = (MatrixD)m_handItemDefinition.LeftHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                        CalculateHandIK(m_leftHandIKStartBone, m_leftForearmBone, m_leftHandIKEndBone, ref leftHand);
                    }

                    if (m_handItemDefinition.SimulateRightHand && m_rightHandIKStartBone != -1 && m_rightHandIKEndBone != -1 && (!UseAnimationForWeapon || m_animationToIKState != 0) && IsSitting == false)
                    {
                        MatrixD rightHand = (MatrixD)m_handItemDefinition.RightHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                        CalculateHandIK(m_rightHandIKStartBone, m_rightForearmBone, m_rightHandIKEndBone, ref rightHand);
                    }
                }
                else
                {
                    Debug.Assert(m_rightHandItemBone != -1, "Invalid bone for weapon.");
                    if (m_rightHandItemBone != -1)
                    {
                        //use animation for right hand item
                        MatrixD rightHandItemMatrix = AnimationController.CharacterBones[m_rightHandItemBone].AbsoluteTransform * WorldMatrix;
                        //var rightHandItemMatrix = ((MyEntity)m_currentWeapon).PositionComp.WorldMatrix; //use with UpdateWeaponPosition() but not working for barbarians
                        Vector3D up = rightHandItemMatrix.Up;
                        rightHandItemMatrix.Up = rightHandItemMatrix.Forward;
                        rightHandItemMatrix.Forward = up;
                        rightHandItemMatrix.Right = -rightHandItemMatrix.Right;
                        ((MyEntity)m_currentWeapon).PositionComp.WorldMatrix = rightHandItemMatrix;
                    }
                }
            }


            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("ComputeBoneTransform");

            var characterBones = AnimationController.CharacterBones;
            for (int i = 0; i < characterBones.Length; i++)
            {
                MyCharacterBone bone = characterBones[i];
                BoneRelativeTransforms[i] = bone.ComputeBoneTransform();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            ProfilerShort.End();
        }



        #endregion

        #region Animations update

        public override void UpdateAnimation(float distance)
        {
            if (MySandboxGame.IsDedicated && MyPerGameSettings.DisableAnimationsOnDS)
                return;
            MyAnimationPlayerBlendPair leftHandPlayer;
            if (TryGetAnimationPlayer("LeftHand", out leftHandPlayer))
            {
                if (leftHandPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped && m_leftHandItem != null)
                {
                    m_leftHandItem.Close();
                    m_leftHandItem = null;
                }
            }

            base.UpdateAnimation(distance);

            Render.UpdateThrustMatrices(BoneAbsoluteTransforms);

            // check if animations on hands stopped and whether we need to reset the state for weapon positioning
            if (m_resetWeaponAnimationState)
            {
                //if ((m_rightHandPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped) &&
                //    (m_rightFingersPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped))
                {
                    m_resetWeaponAnimationState = false;
                    UseAnimationForWeapon = false;
                }
            }
        }

        
        /*
         * This was hacked so the character would have a fluid movement (when shooting still and then start another movement - jump, strafe, running, etc...).
         * Before this, the attack animation would be overriden, but still with normal physical/damage effects.
         */
        MyStringHash medievelMaleSubtypeId = MyStringHash.GetOrCompute("Medieval_male");
        string m_lastBonesArea = string.Empty;
        protected override void OnAnimationPlay(MyAnimationDefinition animDefinition,MyAnimationCommand command, ref string bonesArea, ref MyFrameOption frameOption, ref bool useFirstPersonVersion)
        {
            var currentMovementState = GetCurrentMovementState();

            if (DefinitionId.Value.SubtypeId == medievelMaleSubtypeId)
            {
                if (command.ExcludeLegsWhenMoving)
                {
                    //In this case, we must stop all upper animations correctly

                    if (currentMovementState == MyCharacterMovementEnum.RotatingLeft ||
                        currentMovementState == MyCharacterMovementEnum.RotatingRight ||
                        currentMovementState == MyCharacterMovementEnum.Standing)
                        bonesArea = TopBody + " LowerBody";
                    else
                        bonesArea = TopBody;
                    frameOption = frameOption != MyFrameOption.JustFirstFrame ? MyFrameOption.PlayOnce : frameOption;
                }
                else if (m_lastBonesArea == TopBody + " LowerBody")
                    StopLowerCharacterAnimation(0.2f);

                m_lastBonesArea = bonesArea;
            }
            else
            {
                if (currentMovementState != MyCharacterMovementEnum.Standing &&
                    currentMovementState != MyCharacterMovementEnum.RotatingLeft &&
                    currentMovementState != MyCharacterMovementEnum.RotatingRight &&
                     command.ExcludeLegsWhenMoving)
                {
                    //In this case, we must stop all upper animations correctly
                    bonesArea = TopBody;
                    frameOption = frameOption != MyFrameOption.JustFirstFrame ? MyFrameOption.PlayOnce : frameOption;
                }
            }

            
            useFirstPersonVersion = IsInFirstPersonView;

            if (animDefinition.AllowWithWeapon)
            {
                if (!UseAnimationForWeapon)
                {
                    StoreWeaponRelativeMatrix();
                    UseAnimationForWeapon = true;
                    m_resetWeaponAnimationState = true;
                }
            }

            if (!animDefinition.LeftHandItem.TypeId.IsNull)
            {
                if (m_leftHandItem != null)
                {
                    (m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>).OnControlReleased();
                    m_leftHandItem.Close();
                }

                m_leftHandItem = MyEntityFactory.CreateEntity(animDefinition.LeftHandItem.TypeId);
                var ob = MyEntityFactory.CreateObjectBuilder(m_leftHandItem);
                m_leftHandItem.Init(ob);

                var leftHandTool = m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>;
                if (leftHandTool != null)
                {
                    leftHandTool.OnControlAcquired(this);
                }

                (m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>).OnControlAcquired(this);
                UpdateLeftHandItemPosition();

                MyEntities.Add(m_leftHandItem);
            }
        }


        private void StopUpperAnimation(float blendTime)
        {
            PlayerStop("Head", blendTime);
            PlayerStop("Spine", blendTime);
            PlayerStop("LeftHand", blendTime);
            PlayerStop("RightHand", blendTime);
        }

        private void StopFingersAnimation(float blendTime)
        {
            PlayerStop("LeftFingers", blendTime);
            PlayerStop("RightFingers", blendTime);
        }

        public override void AddCommand(MyAnimationCommand command, bool sync = false)
        {
            base.AddCommand(command, sync);

            if (sync)
                SyncObject.SendAnimationCommand(ref command);
        }


        public void SetSpineAdditionalRotation(Quaternion rotation, Quaternion rotationForClients, bool updateSync = true)
        {
            if (!string.IsNullOrEmpty(Definition.SpineBone))
            {
                bool valueChanged = GetAdditionalRotation(Definition.SpineBone) != rotation;
                if (valueChanged)
                {
                    m_additionalRotations[Definition.SpineBone] = rotation;
                }
            }
        }

        public void SetHeadAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            if (!string.IsNullOrEmpty(Definition.HeadBone))
            {
                bool valueChanged = GetAdditionalRotation(Definition.HeadBone) != rotation;

                if (valueChanged)
                {
                    m_additionalRotations[Definition.HeadBone] = rotation;
                }
            }
        }

        public void SetHandAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            if (!string.IsNullOrEmpty(Definition.LeftForearmBone))
            {
                bool valueChanged = GetAdditionalRotation(Definition.LeftForearmBone) != rotation;
                if (valueChanged)
                {
                    m_additionalRotations[Definition.LeftForearmBone] = rotation;
                    m_additionalRotations[Definition.RightForearmBone] = Quaternion.Inverse(rotation);
                }
            }
        }

        public void SetUpperHandAdditionalRotation(Quaternion rotation, bool updateSync = true)
        {
            if (!string.IsNullOrEmpty(Definition.LeftUpperarmBone))
            {
                bool valueChanged = GetAdditionalRotation(Definition.LeftUpperarmBone) != rotation;
                if (valueChanged)
                {
                    m_additionalRotations[Definition.LeftUpperarmBone] = rotation;
                    m_additionalRotations[Definition.RightUpperarmBone] = Quaternion.Inverse(rotation);
                }
            }
        }

        public bool HasAnimation(string animationName)
        {
            return Definition.AnimationNameToSubtypeName.ContainsKey(animationName);
        }


        #endregion

        #region Animation commands

        public void DisableAnimationCommands()
        {
            m_animationCommandsEnabled = false;
        }

        public void EnableAnimationCommands()
        {
            m_animationCommandsEnabled = true;
        }

        public void PlayCharacterAnimation(
           string animationName,
           MyBlendOption blendOption,
           MyFrameOption frameOption,
           float blendTime,           
           float timeScale = 1,
           bool sync = false,
           string influenceArea = null, //use defined boneset area from character definitions
           bool excludeLegsWhenMoving = false
           )
        {
            bool disableAnimations = MySandboxGame.IsDedicated && MyPerGameSettings.DisableAnimationsOnDS;
            if (disableAnimations && !sync)
            {
                return;
            }

            if (!m_animationCommandsEnabled) return;

            if (animationName == null)
            {
                System.Diagnostics.Debug.Fail("Cannot play null animation!");
                return;
            }

            string animationSubtype = null;
            if (!m_characterDefinition.AnimationNameToSubtypeName.TryGetValue(animationName, out animationSubtype))
            {
                animationSubtype = animationName;
            }

            var command = new MyAnimationCommand()
            {
                AnimationSubtypeName = animationSubtype,
                PlaybackCommand = MyPlaybackCommand.Play,
                BlendOption = blendOption,
                FrameOption = frameOption,
                BlendTime = blendTime,
                TimeScale = timeScale,
                Area = influenceArea,
                ExcludeLegsWhenMoving = excludeLegsWhenMoving
            };

            // CH: If we don't want to play the animation ourselves, but it has to be synced, we have to send it to clients at least
            if (disableAnimations && sync)
            {
                SyncObject.SendAnimationCommand(ref command);
            }
            else
            {
                AddCommand(command, sync);
            }
        }

        public void StopUpperCharacterAnimation(float blendTime)
        {
            AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = null,
                    PlaybackCommand = MyPlaybackCommand.Stop,
                    Area = TopBody,
                    BlendTime = blendTime,
                    TimeScale = 1
                });
        }

        public void StopLowerCharacterAnimation(float blendTime)
        {
            AddCommand(new MyAnimationCommand()
            {
                AnimationSubtypeName = null,
                PlaybackCommand = MyPlaybackCommand.Stop,
                Area = "LowerBody",
                BlendTime = blendTime,
                TimeScale = 1
            });
        }

        #endregion
    }
}

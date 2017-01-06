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
using VRageRender.Animations;
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
using Sandbox.Game.Multiplayer;
using VRage.Profiler;

#endregion

namespace Sandbox.Game.Entities.Character
{
    public partial class MyCharacter
    {
        #region Fields

        static string TopBody = "LeftHand RightHand LeftFingers RightFingers Head Spine";

        bool m_resetWeaponAnimationState;
        private Quaternion m_lastRotation;

        #endregion

        #region Animations init


        // temporary solution, resolving jittering
        private readonly Vector3[] m_animationSpeedFilter = new Vector3[4];
        private int m_animationSpeedFilterCursor = 0;

        void InitAnimations()
        {
            m_animationSpeedFilterCursor = 0;
            for (int i = 0; i < m_animationSpeedFilter.Length; i++)
                m_animationSpeedFilter[i] = Vector3.Zero;

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


        protected override void CalculateTransforms(float distance)
        {
            ProfilerShort.Begin("MyCharacter.CalculateTransforms");

            base.CalculateTransforms(distance);
            if (m_headBoneIndex >= 0 && AnimationController.CharacterBones != null && (IsInFirstPersonView || ForceFirstPersonCamera) && ControllerInfo.IsLocallyControlled() && !IsBot)
            {
                Vector3 headHorizontalTranslation = AnimationController.CharacterBones[m_headBoneIndex].AbsoluteTransform.Translation;
                headHorizontalTranslation.Y = 0;
                MyCharacterBone.TranslateAllBones(AnimationController.CharacterBones, -headHorizontalTranslation);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate Hand IK");


            if (this == MySession.Static.ControlledEntity)
            {
                // (OM) Note: only controlled character can get it's aimed point from camera, otherwise all character's will aim the same direction
                // set the aimed point explicitly using AimedPoint property
                m_aimedPoint = GetAimedPointFromCamera();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Update anim IK");

            AnimationController.UpdateInverseKinematics(); // since we already have absolute transforms

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("UpdateLeftHandItemPosition");
            if (m_leftHandItem != null)
            {
                UpdateLeftHandItemPosition();
            }

            if (m_currentWeapon != null && WeaponPosition != null && m_handItemDefinition != null)
            {
                WeaponPosition.Update();
                //mainly IK and some zoom + ironsight stuff
                if (m_handItemDefinition.SimulateLeftHand && m_leftHandIKStartBone != -1 && m_leftHandIKEndBone != -1)
                {
                    MatrixD leftHand = (MatrixD)m_handItemDefinition.LeftHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                    CalculateHandIK(m_leftHandIKStartBone, m_leftForearmBone, m_leftHandIKEndBone, ref leftHand);
                }

                if (m_handItemDefinition.SimulateRightHand && m_rightHandIKStartBone != -1 && m_rightHandIKEndBone != -1 && IsSitting == false)
                {
                    MatrixD rightHand = (MatrixD)m_handItemDefinition.RightHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                    CalculateHandIK(m_rightHandIKStartBone, m_rightForearmBone, m_rightHandIKEndBone, ref rightHand);
                }
            }

            MyRenderProxy.GetRenderProfiler().StartNextBlock("UpdateTransformations");

            AnimationController.UpdateTransformations();

            MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            ProfilerShort.End();
        }



        #endregion

        #region Animations update

        public override void UpdateAnimation(float distance)
        {
            if (UseNewAnimationSystem)
                UpdateAnimationNewSystem();
            if (MySandboxGame.IsDedicated && MyPerGameSettings.DisableAnimationsOnDS)
                return;
            MyAnimationPlayerBlendPair leftHandPlayer;
            if (TryGetAnimationPlayer("LeftHand", out leftHandPlayer))
            {
                if (leftHandPlayer.GetState() == MyAnimationPlayerBlendPair.AnimationBlendState.Stopped 
                    && m_leftHandItem != null && !UseNewAnimationSystem)
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
                }
            }
        }

        private void UpdateAnimationNewSystem()
        {
            var variableStorage = AnimationController.Variables;
            // character speed
            if (Physics != null && Physics.CharacterProxy != null)
            {
                Vector3 localSpeedWorldRotUnfiltered = (Physics.CharacterProxy.LinearVelocity - Physics.CharacterProxy.GroundVelocity);

                //Minimize walking bug during standing on floating object
                if ((GetCurrentMovementState() == MyCharacterMovementEnum.Standing) /*&& Physics.CharacterProxy.CharacterRigidBody.IsSupportedByFloatingObject()*/)
                {
                   float r = Physics.CharacterProxy.Up.Dot(localSpeedWorldRotUnfiltered);
                   if (r < 0.0f)
                   {
                       localSpeedWorldRotUnfiltered -= Physics.CharacterProxy.Up * r;
                   }
                }
                                               
                var localSpeedWorldRot = FilterLocalSpeed(localSpeedWorldRotUnfiltered);
                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSpeed, localSpeedWorldRot.Length());
            
                float localSpeedX = localSpeedWorldRot.Dot(PositionComp.WorldMatrix.Right);
                float localSpeedY = localSpeedWorldRot.Dot(PositionComp.WorldMatrix.Up);
                float localSpeedZ = localSpeedWorldRot.Dot(PositionComp.WorldMatrix.Forward);

                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSpeedX, localSpeedX);
                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSpeedY, localSpeedY);
                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSpeedZ, localSpeedZ);
                const float speedThreshold2 = 0.1f * 0.1f;
                float speedangle = localSpeedWorldRot.LengthSquared() > speedThreshold2 ? (float)(-Math.Atan2(localSpeedZ, localSpeedX) * 180.0f / Math.PI) + 90.0f : 0.0f;
                while (speedangle < 0.0f)
                    speedangle += 360.0f;
                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSpeedAngle, speedangle);

                Quaternion currentRotation = this.GetRotation();
                m_animTurningSpeed = (Quaternion.Inverse(currentRotation) * m_lastRotation).Y / (CHARACTER_X_ROTATION_SPEED * CHARACTER_Y_ROTATION_FACTOR / 2) * 180.0f / (float)Math.PI;
                variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdTurningSpeed, m_animTurningSpeed);
                m_lastRotation = currentRotation;

                if (OxygenComponent != null)
                    variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdHelmetOpen, OxygenComponent.HelmetEnabled ? 0.0f : 1.0f);

                if (Parent is MyCockpit)
                    variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdLean, 0);
                else
                    variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdLean, m_animLeaning);
            }

            if (JetpackComp != null)
                AnimationController.Variables.SetValue(MyAnimationVariableStorageHints.StrIdFlying, JetpackComp.Running ? 1.0f : 0.0f);

            MyCharacterMovementEnum movementState = GetCurrentMovementState();
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdFlying, movementState == MyCharacterMovementEnum.Flying ? 1.0f : 0.0f);
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdFalling, IsFalling || movementState == MyCharacterMovementEnum.Falling ? 1.0f : 0.0f);
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdCrouch, (WantsCrouch && !WantsSprint) ? 1.0f : 0.0f);
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdSitting, movementState == MyCharacterMovementEnum.Sitting ? 1.0f : 0.0f);
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdJumping, movementState == MyCharacterMovementEnum.Jump ? 1.0f : 0.0f);

            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdFirstPerson, m_isInFirstPerson ? 1.0f : 0.0f);
            variableStorage.SetValue(MyAnimationVariableStorageHints.StrIdHoldingTool, m_currentWeapon != null ? 1.0f : 0.0f);
        }

        private Vector3 FilterLocalSpeed(Vector3 localSpeedWorldRotUnfiltered)
        {
            m_animationSpeedFilter[m_animationSpeedFilterCursor++] = localSpeedWorldRotUnfiltered;
            if (m_animationSpeedFilterCursor >= m_animationSpeedFilter.Length)
                m_animationSpeedFilterCursor = 0;

            Vector3 localSpeedWorldRot = Vector3.Zero;
            for (int i = 0; i < m_animationSpeedFilter.Length; i++)
            {
                if (m_animationSpeedFilter[i].X*m_animationSpeedFilter[i].X > localSpeedWorldRot.X*localSpeedWorldRot.X)
                    localSpeedWorldRot.X = m_animationSpeedFilter[i].X;
                if (m_animationSpeedFilter[i].Y*m_animationSpeedFilter[i].Y > localSpeedWorldRot.Y*localSpeedWorldRot.Y)
                    localSpeedWorldRot.Y = m_animationSpeedFilter[i].Y;
                if (m_animationSpeedFilter[i].Z*m_animationSpeedFilter[i].Z > localSpeedWorldRot.Z*localSpeedWorldRot.Z)
                    localSpeedWorldRot.Z = m_animationSpeedFilter[i].Z;
            }
            return localSpeedWorldRot;
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
                m_resetWeaponAnimationState = true;
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
            if (UseNewAnimationSystem)
                return;

            base.AddCommand(command, sync);

            if (sync)
            {
                SendAnimationCommand(ref command);
            }
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

        public void TriggerCharacterAnimationEvent(string eventName, bool sync)
        {
            if (!UseNewAnimationSystem || string.IsNullOrEmpty(eventName))
                return;

            if (sync)
            {
                SendAnimationEvent(eventName);
            }
            else
            {
                AnimationController.TriggerAction(MyStringId.GetOrCompute(eventName));
            }
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
            if (UseNewAnimationSystem)
                return;
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
            // MZ: when sync is on, we always want to send the message
            if (sync)
            {
                SendAnimationCommand(ref command);
            }
            else
            {
                // if animations are disabled and sync is off, don't do anything
                if (!disableAnimations)
                    AddCommand(command, sync);
            }
        }

        public void StopUpperCharacterAnimation(float blendTime)
        {
            if (UseNewAnimationSystem)
                return;
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
            if (UseNewAnimationSystem)
                return;
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

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
            Debug.Assert(TestCharacterBoneDefinitions(), "Warning! Bone definitions in model " + this.ModelName + " are incorrect.");
            
            foreach (var bones in m_characterDefinition.BoneSets)
            {
                AddAnimationPlayer(bones.Key, bones.Value);
            }

            SetBoneLODs(m_characterDefinition.BoneLODs);

            FindBone(m_characterDefinition.HeadBone, out m_headBoneIndex);
            FindBone(m_characterDefinition.Camera3rdBone, out m_camera3rdBoneIndex);
            if (m_camera3rdBoneIndex == -1)
                m_camera3rdBoneIndex = m_headBoneIndex;            
            FindBone(m_characterDefinition.LeftHandIKStartBone, out m_leftHandIKStartBone);
            FindBone(m_characterDefinition.LeftHandIKEndBone, out m_leftHandIKEndBone);
            FindBone(m_characterDefinition.RightHandIKStartBone, out m_rightHandIKStartBone);
            FindBone(m_characterDefinition.RightHandIKEndBone, out m_rightHandIKEndBone);

            FindBone(m_characterDefinition.LeftUpperarmBone, out m_leftUpperarmBone);
            FindBone(m_characterDefinition.LeftForearmBone, out m_leftForearmBone);
            FindBone(m_characterDefinition.RightUpperarmBone, out m_rightUpperarmBone);
            FindBone(m_characterDefinition.RightForearmBone, out m_rightForearmBone);

            FindBone(m_characterDefinition.WeaponBone, out m_weaponBone);
            FindBone(m_characterDefinition.LeftHandItemBone, out m_leftHandItemBone);
            FindBone(m_characterDefinition.RighHandItemBone, out m_rightHandItemBone);
            FindBone(m_characterDefinition.SpineBone, out m_spineBone);
            
            UpdateAnimation(0);
        }


        // FOR DEBUG ONLY - TEST THE LOADED DEFINITIONS OF BONES
        private bool TestCharacterBoneDefinitions()
        {
           bool isOk = true;
            // Test only mandatory bones - add if needed
           if (m_characterDefinition.HeadBone == null)
           {
               Debug.Fail("Warning! Headbone definition is missing! Model: " + this.ModelName);
               isOk = false;                
           }
           //if (m_characterDefinition.ModelRootBoneName == null)
           //{
           //    Debug.Fail("Warning! Rootbone definition is missing! Model: " + this.ModelName);
           //    isOk = false;
           //}
           if (m_characterDefinition.SpineBone == null)
           {
               Debug.Fail("Warning! Spine bone definition is missing! Model: " + this.ModelName);
               isOk = false;
           }
           return isOk;
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


            if (this == MySession.ControlledEntity)
            {
                // (OM) Note: only controlled character can get it's aimed point from camera, otherwise all character's will aim the same direction
                // set the aimed point explicitly using AimedPoint property
                m_aimedPoint = GetAimedPointFromCamera();
            }
            
            if (m_currentWeapon != null)
            {
                if (!MyPerGameSettings.UseAnimationInsteadOfIK)
                {
                    UpdateWeaponPosition(); //mainly IK and some zoom + ironsight stuff
                    if (m_handItemDefinition.SimulateLeftHand && m_leftHandIKStartBone != -1 && m_leftHandIKEndBone != -1 && (!UseAnimationForWeapon && m_animationToIKState == 0))
                    {
                        MatrixD leftHand = (MatrixD)m_handItemDefinition.LeftHand * ((MyEntity)m_currentWeapon).WorldMatrix;
                        CalculateHandIK(m_leftHandIKStartBone, m_leftForearmBone, m_leftHandIKEndBone, ref leftHand);
                    }

                    if (m_handItemDefinition.SimulateRightHand && m_rightHandIKStartBone != -1 && m_rightHandIKEndBone != -1 && (!UseAnimationForWeapon || m_animationToIKState != 0))
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
                        MatrixD rightHandItemMatrix = Bones[m_rightHandItemBone].AbsoluteTransform * WorldMatrix;
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

            for (int i = 0; i < Bones.Count; i++)
            {
                MyCharacterBone bone = Bones[i];
                BoneRelativeTransforms[i] = bone.ComputeBoneTransform();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            ProfilerShort.End();
        }



        #endregion

        #region Animations update

        public override void UpdateAnimation(float distance)
        {
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

        protected override void OnAnimationPlay(MyAnimationDefinition animDefinition,MyAnimationCommand command, ref string bonesArea, ref MyFrameOption frameOption, ref bool useFirstPersonVersion)
        {
            var currentMovementState = GetCurrentMovementState();
            if (currentMovementState != MyCharacterMovementEnum.Standing &&
                    currentMovementState != MyCharacterMovementEnum.RotatingLeft &&
                    currentMovementState != MyCharacterMovementEnum.RotatingRight &&                    
                     command.ExcludeLegsWhenMoving)
                            {
                                //In this case, we must stop all upper animations correctly
                                bonesArea = TopBody;
                                frameOption = frameOption != MyFrameOption.JustFirstFrame ? MyFrameOption.PlayOnce : frameOption;
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

                    if (updateSync)
                    {
                        SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                            rotationForClients, GetAdditionalRotation(Definition.HeadBone), GetAdditionalRotation(Definition.LeftForearmBone), GetAdditionalRotation(Definition.LeftUpperarmBone));
                    }
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

                    if (updateSync)
                    {
                        SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                            GetAdditionalRotation(Definition.SpineBone), rotation, GetAdditionalRotation(Definition.LeftForearmBone), GetAdditionalRotation(Definition.LeftUpperarmBone));
                    }
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

                    if (updateSync)
                    {
                        SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                           GetAdditionalRotation(Definition.SpineBone), GetAdditionalRotation(Definition.HeadBone), rotation, GetAdditionalRotation(Definition.LeftUpperarmBone));
                    }
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

                    if (updateSync)
                    {
                        SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                           GetAdditionalRotation(Definition.SpineBone), GetAdditionalRotation(Definition.HeadBone), GetAdditionalRotation(Definition.LeftForearmBone), rotation);
                    }
                }
            }
        }

        public bool HasAnimation(string animationName)
        {
            return Definition.AnimationNameToSubtypeName.ContainsKey(animationName);
        }


        #endregion

        #region Animation commands

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

            AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = animationSubtype,
                    PlaybackCommand = MyPlaybackCommand.Play,
                    BlendOption = blendOption,
                    FrameOption = frameOption,
                    BlendTime = blendTime,
                    TimeScale = timeScale,
                    Area = influenceArea,
                    ExcludeLegsWhenMoving = excludeLegsWhenMoving
                }, sync);
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


        #endregion
    }
}

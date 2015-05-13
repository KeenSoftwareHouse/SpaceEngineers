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


#endregion

namespace Sandbox.Game.Entities.Character
{
    class MyAnimationPlayerBlendPair
    {
        public enum AnimationBlendState
        {
            Stopped,
            BlendIn,
            Playing,
            BlendOut
        }

        public MyAnimationPlayerBlendPair(MyCharacter character, string[] bones)
        {
            m_bones = bones != null ? bones : new string[0];
            m_character = character;
        }

        #region Fields

        public AnimationPlayer BlendPlayer = new AnimationPlayer();
        public AnimationPlayer ActualPlayer = new AnimationPlayer();

        AnimationBlendState m_state = AnimationBlendState.Stopped;

        public float m_currentBlendTime = 0;
        public float m_totalBlendTime = 0;

        string[] m_bones;
        MyCharacter m_character;

        #endregion

        #region Properties

        public Quaternion SpineAdditionalRotation
        {
            set
            {
                BlendPlayer.SpineAdditionalRotation = value;
                ActualPlayer.SpineAdditionalRotation = value;
            }
        }

        public Quaternion HeadAdditionalRotation
        {
            set
            {
                BlendPlayer.HeadAdditionalRotation = value;
                ActualPlayer.HeadAdditionalRotation = value;
            }
        }

        public Quaternion HandAdditionalRotation
        {
            set
            {
                BlendPlayer.HandAdditionalRotation = value;
                ActualPlayer.HandAdditionalRotation = value;
            }
        }

        public Quaternion UpperHandAdditionalRotation
        {
            set
            {
                BlendPlayer.UpperHandAdditionalRotation = value;
                ActualPlayer.UpperHandAdditionalRotation = value;
            }
        }

        #endregion

        public void UpdateBones()
        {
            if (m_state != AnimationBlendState.Stopped)
            {
                if (BlendPlayer.IsInitialized)
                    BlendPlayer.UpdateBones();

                if (ActualPlayer.IsInitialized)
                    ActualPlayer.UpdateBones();
            }
        }

        public void Advance()
        {
            if (m_state != AnimationBlendState.Stopped)
            {
                float stepTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                m_currentBlendTime += stepTime * ActualPlayer.TimeScale;
                ActualPlayer.Advance(stepTime);

                if (!ActualPlayer.Looping && (ActualPlayer.Position >= ActualPlayer.Duration) && m_state == AnimationBlendState.Playing)
                {
                    Stop(m_totalBlendTime);
                }
            }
        }

        public void UpdateAnimation()
        {
            //Upper body blend
            float upperBlendRatio = 0;
            if (ActualPlayer.IsInitialized && m_currentBlendTime > 0)
            {
                upperBlendRatio = 1;
                if (m_totalBlendTime > 0)
                    upperBlendRatio = MathHelper.Clamp(m_currentBlendTime / m_totalBlendTime, 0, 1);
            }
            if (ActualPlayer.IsInitialized)
            {
                if (m_state == AnimationBlendState.BlendOut)
                {
                    ActualPlayer.Weight = 1 - upperBlendRatio;

                    if (upperBlendRatio == 1)
                    {
                        ActualPlayer.Done();
                        m_state = AnimationBlendState.Stopped;
                    }
                }
                if (m_state == AnimationBlendState.BlendIn)
                {
                    ActualPlayer.Weight = upperBlendRatio;

                    if (BlendPlayer.IsInitialized)
                        BlendPlayer.Weight = 1;

                    if (upperBlendRatio == 1)
                    {
                        m_state = AnimationBlendState.Playing;
                    }
                }
            }
        }

        public void Play(MyAnimationDefinition animationDefinition, bool loop, float blendTime, float timeScale, bool justFirstFrame)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(animationDefinition.AnimationModel));

            if (string.IsNullOrEmpty(animationDefinition.AnimationModel))
                return;

            MyModel animation = MyModels.GetModelOnlyAnimationData(animationDefinition.AnimationModel);

            System.Diagnostics.Debug.Assert(animation.Animations.Clips.Count > 0);
            if (animation.Animations.Clips.Count == 0)
                return;

            System.Diagnostics.Debug.Assert(animationDefinition.ClipIndex < animation.Animations.Clips.Count);
            if (animation.Animations.Clips.Count <= animationDefinition.ClipIndex)
                return;

            AnimationClip clip = animation.Animations.Clips[animationDefinition.ClipIndex];

            if (ActualPlayer.IsInitialized)
            {
                BlendPlayer.Initialize(ActualPlayer);
            }

            // Create a clip player and assign it to this model
            ActualPlayer.Initialize(clip, m_character, 1, timeScale, justFirstFrame, m_bones);
            ActualPlayer.Looping = loop;

            m_state = AnimationBlendState.BlendIn;
            m_currentBlendTime = 0;
            m_totalBlendTime = blendTime;
        }

        public void Stop(float blendTime)
        {
            if (m_state != AnimationBlendState.Stopped)
            {
                BlendPlayer.Done();

                m_state = AnimationBlendState.BlendOut;
                m_currentBlendTime = 0;
                m_totalBlendTime = blendTime;
            }
        }

        public AnimationBlendState GetState()
        {
            return m_state;
        }
    }
}

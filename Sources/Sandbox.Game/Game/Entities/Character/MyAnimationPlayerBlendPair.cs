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
using VRage.FileSystem;
using VRage.Game.Definitions.Animation;
using VRage.Game.Models;


#endregion

namespace Sandbox.Game.Entities
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

        #region Fields

        public AnimationPlayer BlendPlayer = new AnimationPlayer();
        public AnimationPlayer ActualPlayer = new AnimationPlayer();

        AnimationBlendState m_state = AnimationBlendState.Stopped;

        public float m_currentBlendTime = 0;
        public float m_totalBlendTime = 0;

        string[] m_bones;
        MySkinnedEntity m_skinnedEntity;
        string m_name;

        Dictionary<float, string[]> m_boneLODs;

        #endregion

        #region Properties

        #endregion


        public MyAnimationPlayerBlendPair(MySkinnedEntity skinnedEntity, string[] bones, Dictionary<float, string[]> boneLODs, string name)
        {
            m_bones = bones;
            m_skinnedEntity = skinnedEntity;
            m_boneLODs = boneLODs;
            m_name = name;
        }


        public void UpdateBones(float distance)
        {
            if (m_state != AnimationBlendState.Stopped)
            {
                if (BlendPlayer.IsInitialized)
                    BlendPlayer.UpdateBones(distance);

                if (ActualPlayer.IsInitialized)
                    ActualPlayer.UpdateBones(distance);
            }
        }

        public bool Advance()
        {
            if (m_state != AnimationBlendState.Stopped)
            {
                float stepTime = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                m_currentBlendTime += stepTime * ActualPlayer.TimeScale;
                ActualPlayer.Advance(stepTime);

                if (!ActualPlayer.Looping && ActualPlayer.AtEnd && m_state == AnimationBlendState.Playing)
                {
                    Stop(m_totalBlendTime);
                }
                return true;
            }
            else
            {
                return false;
            }

            //UpdateAnimation();
        }

        public void UpdateAnimationState()
        {

            float blendRatio = 0;
            if (ActualPlayer.IsInitialized && m_currentBlendTime > 0)
            {
                blendRatio = 1;
                if (m_totalBlendTime > 0)
                    blendRatio = MathHelper.Clamp(m_currentBlendTime / m_totalBlendTime, 0, 1);
            }           
            if (ActualPlayer.IsInitialized)
            {
                if (m_state == AnimationBlendState.BlendOut)
                {
                    ActualPlayer.Weight = 1 - blendRatio;

                    if (blendRatio == 1)
                    {
                        ActualPlayer.Done();
                        m_state = AnimationBlendState.Stopped;
                    }
                }
                if (m_state == AnimationBlendState.BlendIn)
                {
                    if (m_totalBlendTime == 0)
                    {
                        blendRatio = 1;
                    }

                    ActualPlayer.Weight = blendRatio;

                    if (BlendPlayer.IsInitialized)
                        BlendPlayer.Weight = 1;

                    if (blendRatio == 1)
                    {
                        m_state = AnimationBlendState.Playing;
                        BlendPlayer.Done();
                    }
                }
            }
        }

        public void Play(MyAnimationDefinition animationDefinition, bool firstPerson, MyFrameOption frameOption, float blendTime, float timeScale)
        {
            string model = firstPerson && !string.IsNullOrEmpty(animationDefinition.AnimationModelFPS) ? animationDefinition.AnimationModelFPS : animationDefinition.AnimationModel;
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(model));


            if (string.IsNullOrEmpty(animationDefinition.AnimationModel))
                return;


            if (animationDefinition.Status == MyAnimationDefinition.AnimationStatus.Unchecked)
            {
                var fsPath = System.IO.Path.IsPathRooted(model) ? model : System.IO.Path.Combine(MyFileSystem.ContentPath, model);
                if (!MyFileSystem.FileExists(fsPath))
                {
                    animationDefinition.Status = MyAnimationDefinition.AnimationStatus.Failed;
                    return;
                }
            }

            animationDefinition.Status = MyAnimationDefinition.AnimationStatus.OK;

            MyModel animation = VRage.Game.Models.MyModels.GetModelOnlyAnimationData(model);
            Debug.Assert(animation != null && animation.Animations != null && animation.Animations.Clips.Count > 0);
            if (animation != null && animation.Animations == null || animation.Animations.Clips.Count == 0)
                return;

            Debug.Assert(animationDefinition.ClipIndex < animation.Animations.Clips.Count);
            if (animation.Animations.Clips.Count <= animationDefinition.ClipIndex)
                return;

            if (ActualPlayer.IsInitialized)
            {
                BlendPlayer.Initialize(ActualPlayer);
            }

            // Create a clip player and assign it to this model                        
            ActualPlayer.Initialize(animation, m_name, animationDefinition.ClipIndex, m_skinnedEntity, 1, timeScale, frameOption, m_bones, m_boneLODs);
            ActualPlayer.AnimationMwmPathDebug = model;
            ActualPlayer.AnimationNameDebug = animationDefinition.Id.SubtypeName;

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

        public void SetBoneLODs(Dictionary<float, string[]> boneLODs)
        {
            m_boneLODs = boneLODs;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using VRage.Animations;
using VRageMath;
using Sandbox.Engine.Utils;


namespace Sandbox.Game.Entities
{
    #region Enums


    public enum MyPlaybackCommand
    {
        Play,
        Stop
    }

    public enum MyBlendOption
    {
        Immediate,
        WaitForPreviousEnd,
    }

    public enum MyFrameOption
    {
        None,
        JustFirstFrame,
        StayOnLastFrame,
        Loop
    }

    #endregion

    /// <summary>
    /// Animation clip player. It maps an animation clip onto a model
    /// </summary>
    internal class AnimationPlayer
    {
        #region Fields

        /// <summary>
        /// Current position in time in the clip
        /// </summary>
        private float m_position = 0;

        /// <summary>
        /// The clip we are playing
        /// </summary>
        private AnimationClip m_clip = null;

        /// <summary>
        /// We maintain a BoneInfo class for each bone. This class does
        /// most of the work in playing the animation.
        /// </summary>
        private BoneInfo[] m_boneInfos;

        /// <summary>
        /// The number of bones
        /// </summary>
        private int m_boneCount;

        /// <summary>
        /// An assigned model
        /// </summary>
        private MySkinnedEntity m_skinnedEntity = null;

        /// <summary>
        /// The looping option
        /// </summary>
        private MyFrameOption m_frameOption = MyFrameOption.None;

        private float m_weight = 1;
        private float m_timeScale = 1;

        private bool m_initialized = false;

        #endregion

        #region Properties

        public void Advance(float value)
        {
            if (m_frameOption != MyFrameOption.JustFirstFrame)
            {
                Position += value * m_timeScale;

                if (m_frameOption == MyFrameOption.StayOnLastFrame && Position > Duration)
                    Position = Duration;
            }
            else
                Position = 0;
        }
        /// <summary>
        /// The position in the animation
        /// </summary>
        [Browsable(false)]
        public float Position
        {
            get { return m_position; }
            set
            {
                float newVal = value;

                if (newVal > Duration)
                {
                    if (Looping)
                        newVal = newVal - Duration;
                    else
                        value = Duration;
                }

                m_position = newVal;

                //VRage.Trace.MyTrace.Watch("m_timeScale", m_timeScale.ToString());
            }
        }

        public float Weight
        {
            get { return m_weight; }
            set { m_weight = value; }
        }

        public float TimeScale
        {
            get { return m_timeScale; }
            set { m_timeScale = value; }
        }

        public void UpdateBones()
        {
            for (int i = 0; i < m_boneCount; i++)
            {
                BoneInfo bone = m_boneInfos[i];
                bone.SetPosition(m_position);
            }
        }

        /// <summary>
        /// The associated animation clip
        /// </summary>
        [Browsable(false)]
        public AnimationClip Clip { get { return m_clip; } }

        //public MyCharacter Model { get { return m_skinnedEntity; } }

        /// <summary>
        /// The clip duration
        /// </summary>
        [Browsable(false)]
        public float Duration { get { return (float)m_clip.Duration; } }


        /// <summary>
        /// The looping option. Set to true if you want the animation to loop
        /// back at the end
        /// </summary>
        public bool Looping { get { return m_frameOption == MyFrameOption.Loop ; }  }


        public bool AtEnd
        {
            get { return Position >= Duration && m_frameOption != MyFrameOption.StayOnLastFrame; }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Constructor for the animation player. It makes the 
        /// association between a clip and a model and sets up for playing
        /// </summary>
        /// <param name="clip"></param>
        public AnimationPlayer()
        {
        }


        public void Initialize(AnimationPlayer player)
        {
            m_clip = player.Clip;
            m_skinnedEntity = player.m_skinnedEntity;
            m_weight = player.Weight;
            m_timeScale = player.m_timeScale;
            m_frameOption = player.m_frameOption;

            m_boneCount = player.m_boneCount;
            if (m_boneInfos == null || m_boneInfos.Length < m_boneCount)
                m_boneInfos = new BoneInfo[m_boneCount];


            Position = player.Position;

            for (int b = 0; b < m_boneCount; b++)
            {
                if (m_boneInfos[b] == null)
                    m_boneInfos[b] = new BoneInfo();
                // Create it
                m_boneInfos[b].ClipBone = player.m_boneInfos[b].ClipBone;
                m_boneInfos[b].Player = this;
                    
                // Assign it to a model bone
                m_boneInfos[b].SetModel(m_skinnedEntity);
                m_boneInfos[b].CurrentKeyframe = player.m_boneInfos[b].CurrentKeyframe;
                m_boneInfos[b].SetPosition(Position);

                if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
                {
                    int index;
                    System.Diagnostics.Debug.Assert(m_skinnedEntity.FindBone(m_boneInfos[b].ClipBone.Name, out index) != null, "Can not find clip bone with name: " + m_boneInfos[b].ClipBone.Name + " in model: " + m_skinnedEntity.Name);
                }
            }


            m_initialized = true;
        }

        public void Initialize(AnimationClip clip, MySkinnedEntity skinnedEntity, float weight, float timeScale, MyFrameOption frameOption, string[] explicitBones = null)
        {
            m_clip = clip;
            m_skinnedEntity = skinnedEntity;
            m_weight = weight;
            m_timeScale = timeScale;
            m_frameOption = frameOption;
            
            // Create the bone information classes
            var maxBoneCount = explicitBones == null ? clip.Bones.Count : explicitBones.Length;
            if (m_boneInfos == null || m_boneInfos.Length < maxBoneCount)
                m_boneInfos = new BoneInfo[maxBoneCount];

            int neededBonesCount = 0;

            for (int b = 0; b < maxBoneCount; b++)
            {
                var bone = explicitBones == null ? clip.Bones[b] : FindBone(clip.Bones, explicitBones[b]);
                if (bone == null)
                    continue;

                if (bone.Keyframes.Count == 0) 
                    continue;

                // Create it
                m_boneInfos[neededBonesCount] = new BoneInfo(bone, this);

                // Assign it to a model bone
                m_boneInfos[neededBonesCount].SetModel(skinnedEntity);

                neededBonesCount++;
            }

            m_boneCount = neededBonesCount;

            Position = 0;

            m_initialized = true;
        }

        public void Done()
        {
            m_initialized = false;
        }

        public bool IsInitialized
        {
            get { return m_initialized; }
        }

        AnimationClip.Bone FindBone(List<AnimationClip.Bone> bones, string name)
        {
            foreach (AnimationClip.Bone bone in bones)
            {
                if (bone.Name == name)
                    return bone;
            }

            return null;
        }



        #endregion


        #region BoneInfo class


        /// <summary>
        /// Information about a bone we are animating. This class connects a bone
        /// in the clip to a bone in the model.
        /// </summary>
        private class BoneInfo
        {
            #region Fields

            /// <summary>
            /// The current keyframe. Our position is a time such that the 
            /// we are greater than or equal to this keyframe's time and less
            /// than the next keyframes time.
            /// </summary>
            private int m_currentKeyframe = 0;

            bool m_isConst = false;
            
            public int CurrentKeyframe
            {
                get { return m_currentKeyframe; }
                set
                { 
                    m_currentKeyframe = value; 
                    SetKeyframes();
                }
            }

            /// <summary>
            /// Bone in a model that this keyframe bone is assigned to
            /// </summary>
            private MyCharacterBone m_assignedBone = null;

            /// <summary>
            /// Current animation rotation
            /// </summary>
            private Quaternion m_rotation;

            /// <summary>
            /// Current animation translation
            /// </summary>
            public Vector3 m_translation;

            public AnimationPlayer Player;

            /// <summary>
            /// We are at a location between Keyframe1 and Keyframe2 such 
            /// that Keyframe1's time is less than or equal to the current position
            /// </summary>
            public AnimationClip.Keyframe Keyframe1;

            /// <summary>
            /// Second keyframe value
            /// </summary>
            public AnimationClip.Keyframe Keyframe2;

            #endregion

            #region Properties

            /// <summary>
            /// The bone in the actual animation clip
            /// </summary>
            AnimationClip.Bone m_clipBone;
            public AnimationClip.Bone ClipBone
            {
                get
                {
                    return m_clipBone;
                }
                
                set
                {
                    m_clipBone = value;
                } 
            }

            /// <summary>
            /// The bone this animation bone is assigned to in the model
            /// </summary>
            public MyCharacterBone ModelBone { get { return m_assignedBone; } }
            
            #endregion

            #region Constructor

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="bone"></param>
            public BoneInfo()
            {
            }
            public BoneInfo(AnimationClip.Bone bone, AnimationPlayer player)
            {
                this.ClipBone = bone;
                Player = player;

                SetKeyframes();
                SetPosition(0);

                m_isConst = bone.Keyframes.Count == 1;
            }


            #endregion

            #region Position and Keyframes

            /// <summary>
            /// Set the bone based on the supplied position value
            /// </summary>
            /// <param name="position"></param>
            public void SetPosition(float position)
            {
                if (ClipBone == null)
                    return;

                List<AnimationClip.Keyframe> keyframes = ClipBone.Keyframes;
                if (keyframes.Count == 0)
                    return;

                if (!m_isConst)
                {
                    // If our current position is less that the first keyframe
                    // we move the position backward until we get to the right keyframe
                    while (position < Keyframe1.Time && m_currentKeyframe > 0)
                    {
                        // We need to move backwards in time
                        m_currentKeyframe--;
                        SetKeyframes();
                    }

                    // If our current position is greater than the second keyframe
                    // we move the position forward until we get to the right keyframe
                    while (position >= Keyframe2.Time && m_currentKeyframe < ClipBone.Keyframes.Count - 2)
                    {
                        // We need to move forwards in time
                        m_currentKeyframe++;
                        SetKeyframes();
                    }

                    if (Keyframe1 == Keyframe2)
                    {
                        // Keyframes are equal
                        m_rotation = Keyframe1.Rotation;
                        m_translation = Keyframe1.Translation;
                    }
                    else
                    {
                        // Interpolate between keyframes
                        float t = (float)((position - Keyframe1.Time) * Keyframe2.TimeDiff);

                        t = MathHelper.Clamp(t, 0, 1);

                        Quaternion.Slerp(ref Keyframe1.Rotation, ref Keyframe2.Rotation, t, out m_rotation);
                        Vector3.Lerp(ref Keyframe1.Translation, ref Keyframe2.Translation, t, out m_translation);
                    }
                }

                if (m_assignedBone != null)
                {
                    Quaternion rotation = m_rotation;

                    Quaternion additionalRotation = Player.m_skinnedEntity.GetAdditionalRotation(m_assignedBone.Name);
                    rotation = m_rotation * additionalRotation;

                    m_assignedBone.SetCompleteTransform(ref m_translation, ref rotation, Player.Weight);
                }
            }



            /// <summary>
            /// Set the keyframes to a valid value relative to 
            /// the current keyframe
            /// </summary>
            void SetKeyframes()
            {
                if (ClipBone == null)
                    return;
                if (ClipBone.Keyframes.Count > 0)
                {
                    Keyframe1 = ClipBone.Keyframes[m_currentKeyframe];
                    if (m_currentKeyframe == ClipBone.Keyframes.Count - 1)
                        Keyframe2 = Keyframe1;
                    else
                        Keyframe2 = ClipBone.Keyframes[m_currentKeyframe + 1];
                }
                else
                {
                    // If there are no keyframes, set both to null
                    Keyframe1 = null;
                    Keyframe2 = null;
                }
            }

            /// <summary>
            /// Assign this bone to the correct bone in the model
            /// </summary>
            /// <param name="model"></param>
            public void SetModel(MySkinnedEntity skinnedEntity)
            {
                if (ClipBone == null)
                    return;

                // Find this bone
                int index;
                m_assignedBone = skinnedEntity.FindBone(ClipBone.Name, out index);
            }

            #endregion
        }

        #endregion

    }
}

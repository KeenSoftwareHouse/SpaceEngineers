using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using VRage.Animations;
using VRageMath;
using Sandbox.Engine.Utils;


namespace Sandbox.Game.Entities.Character
{
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
        private MyCharacter m_model = null;

        /// <summary>
        /// The looping option
        /// </summary>
        private bool m_looping = false;

        private float m_weight = 1;
        private float m_timeScale = 1;

        private bool m_initialized = false;

        private bool m_justFirstFrame = false;

        #endregion

        #region Properties

        public void Advance(float value)
        {
            if (!m_justFirstFrame)
                Position += value * m_timeScale;
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

        public Quaternion SpineAdditionalRotation = Quaternion.Identity;
        public Quaternion HeadAdditionalRotation = Quaternion.Identity;
        public Quaternion HandAdditionalRotation = Quaternion.Identity;
        public Quaternion UpperHandAdditionalRotation = Quaternion.Identity;

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

        public MyCharacter Model { get { return m_model; } }

        /// <summary>
        /// The clip duration
        /// </summary>
        [Browsable(false)]
        public float Duration { get { return (float)m_clip.Duration; } }


        /// <summary>
        /// The looping option. Set to true if you want the animation to loop
        /// back at the end
        /// </summary>
        public bool Looping { get { return m_looping; } set { m_looping = value; } }

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
            m_model = player.Model;
            m_weight = player.Weight;
            m_timeScale = player.m_timeScale;
            m_justFirstFrame = player.m_justFirstFrame;

            m_boneCount = player.m_boneCount;
            if (m_boneInfos == null || m_boneInfos.Length < m_boneCount)
                m_boneInfos = new BoneInfo[m_boneCount];


            Position = player.Position;
            Looping = player.Looping;

            for (int b = 0; b < m_boneCount; b++)
            {
                if (m_boneInfos[b] == null)
                    m_boneInfos[b] = new BoneInfo();
                // Create it
                m_boneInfos[b].ClipBone = player.m_boneInfos[b].ClipBone;
                m_boneInfos[b].Player = this;
                    
                // Assign it to a model bone
                m_boneInfos[b].SetModel(m_model);
                m_boneInfos[b].CurrentKeyframe = player.m_boneInfos[b].CurrentKeyframe;
                m_boneInfos[b].SetKeyframes();
                m_boneInfos[b].SetPosition(Position);

                 if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
                 {
                     int index;
                     System.Diagnostics.Debug.Assert(m_model.FindBone(m_boneInfos[b].ClipBone.Name, out index) != null, "Can not find clip bone with name: "+m_boneInfos[b].ClipBone.Name+" in model: " + m_model.Name );
                 }
            }


            m_initialized = true;
        }

        public void Initialize(AnimationClip clip, MyCharacter model, float weight, float timeScale, bool justFirstFrame, string[] explicitBones = null)
        {
            m_clip = clip;
            m_model = model;
            m_weight = weight;
            m_timeScale = timeScale;
            m_justFirstFrame = justFirstFrame;
            
            // Create the bone information classes
            m_boneCount = explicitBones == null ? clip.Bones.Count : explicitBones.Length;
            if (m_boneInfos == null || m_boneInfos.Length < m_boneCount)
                m_boneInfos = new BoneInfo[m_boneCount];

            for (int b = 0; b < m_boneCount; b++)
            {
                // Create it
                m_boneInfos[b] = new BoneInfo(explicitBones == null ? clip.Bones[b] : FindBone(clip.Bones, explicitBones[b]), this);

                // Assign it to a model bone
                m_boneInfos[b].SetModel(model);
            }

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


            public bool CalculateSpineAdditionalRotation = false;
            public bool CalculateHeadAdditionalRotation = false;
            public bool CalculateHandAdditionalRotation = false;
            public int CalculateUpperHandAdditionalRotation = 0;

            /// <summary>
            /// The current keyframe. Our position is a time such that the 
            /// we are greater than or equal to this keyframe's time and less
            /// than the next keyframes time.
            /// </summary>
            private int m_currentKeyframe = 0;
            
            public int CurrentKeyframe
            {
                get { return m_currentKeyframe; }
                set { m_currentKeyframe = value; }
            }

            /// <summary>
            /// Bone in a model that this keyframe bone is assigned to
            /// </summary>
            private MyCharacterBone m_assignedBone = null;

            /// <summary>
            /// We are not valid until the rotation and translation are set.
            /// If there are no keyframes, we will never be valid
            /// </summary>
            public bool m_valid = false;

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
                    float t = (float)((position - Keyframe1.Time) / (Keyframe2.Time - Keyframe1.Time));

                    if (t > 1)
                    {
                        t = 1;
                    }
                    if (t < 0)
                    {
                        t = 0;
                    }

                    Quaternion.Slerp(ref Keyframe1.Rotation, ref Keyframe2.Rotation, t, out m_rotation);
                    Vector3.Lerp(ref Keyframe1.Translation, ref Keyframe2.Translation, t, out m_translation);
                }

                m_valid = true;
                if (m_assignedBone != null)
                {
                    Quaternion rotation = m_rotation;

                    //if (m_assignedBone.Name == "Soldier Spine2")
                    if (CalculateSpineAdditionalRotation)
                    {
                        //rotation = m_rotation * Quaternion.CreateFromAxisAngle(Vector3.Forward, -1.2f);
                        rotation = m_rotation * Player.SpineAdditionalRotation;
                    }

                    if (CalculateHeadAdditionalRotation)
                    {
                        //rotation = m_rotation * Quaternion.CreateFromAxisAngle(Vector3.Forward, -1.2f);
                        rotation = m_rotation * Player.HeadAdditionalRotation;
                    }

                    if (CalculateHandAdditionalRotation)
                    {
                        //rotation = m_rotation * Quaternion.CreateFromAxisAngle(Vector3.Forward, -1.2f);
                        rotation = m_rotation * Player.HandAdditionalRotation;
                    }

                    if (CalculateUpperHandAdditionalRotation != 0)
                    {
                        //rotation = m_rotation * Quaternion.CreateFromAxisAngle(Vector3.Forward, -1.2f);
                        Quaternion rot = Player.UpperHandAdditionalRotation;
                        if (CalculateUpperHandAdditionalRotation == -1)
                            rot = Quaternion.Inverse(Player.UpperHandAdditionalRotation);

                        rotation = m_rotation * rot;
                    }

                    // Send to the model
                    // Make it a matrix first
                    Matrix m;
                    Matrix.CreateFromQuaternion(ref rotation, out m);
                    m.Translation = m_translation;

                    m_assignedBone.SetCompleteTransform(m, Player.Weight);
                }
            }



            /// <summary>
            /// Set the keyframes to a valid value relative to 
            /// the current keyframe
            /// </summary>
            public void SetKeyframes()
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
            public void SetModel(MyCharacter model)
            {
                if (ClipBone == null)
                    return;

                // Find this bone
                int index;
                m_assignedBone = model.FindBone(ClipBone.Name, out index);

                if (ClipBone.Name == model.Definition.SpineBone)
                    CalculateSpineAdditionalRotation = true;
                else
                    CalculateSpineAdditionalRotation = false;

                if (ClipBone.Name == model.Definition.HeadBone)
                    CalculateHeadAdditionalRotation = true;
                else
                    CalculateHeadAdditionalRotation = false;

                 //"l_Forearm") || (ClipBone.Name == "r_Forearm"))
                if ((ClipBone.Name == model.Definition.LeftForearmBone) || (ClipBone.Name == model.Definition.RightForearmBone))
                    CalculateHandAdditionalRotation = true;
                else
                    CalculateHandAdditionalRotation = false;

                if (ClipBone.Name == model.Definition.LeftUpperarmBone) 
                    CalculateUpperHandAdditionalRotation = 1;
                else
                    if (ClipBone.Name == model.Definition.RightUpperarmBone)
                    CalculateUpperHandAdditionalRotation = -1;
                else
                    CalculateUpperHandAdditionalRotation = 0;
            }

            #endregion
        }

        #endregion

    }
}

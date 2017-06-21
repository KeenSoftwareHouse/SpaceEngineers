using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using VRageRender.Animations;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using VRage.Utils;
using VRage.Game.Models;


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
        Default,
        PlayOnce,
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
        #region Static

        public static bool ENABLE_ANIMATION_CACHE = false;
        public static bool ENABLE_ANIMATION_LODS = true;

        static int GetAnimationPlayerHash(AnimationPlayer player)
        {
            float positionPrecision = 10;
            return (player.Name.GetHashCode() * 397) ^
                (MyUtils.GetHash(player.m_skinnedEntity.Model.UniqueId) * 397) ^
                    (((int)(player.m_position.GetHashCode() * positionPrecision)) * 397) ^ 
                     player.m_currentLODIndex.GetHashCode();
        }

        static Dictionary<int, AnimationPlayer> CachedAnimationPlayers = new Dictionary<int, AnimationPlayer>();


        #endregion


        #region Fields

        /// <summary>
        /// Current position in time in the clip
        /// </summary>
        private float m_position = 0;

        private float m_duration = 0;

        /// <summary>
        /// We maintain a BoneInfo class for each bone. This class does
        /// most of the work in playing the animation.
        /// </summary>
        private BoneInfo[] m_boneInfos;

        public MyStringId Name { get; private set; }

        Dictionary<float, List<BoneInfo>> m_boneLODs = new Dictionary<float, List<BoneInfo>>();

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
        private MyFrameOption m_frameOption = MyFrameOption.PlayOnce;

        private float m_weight = 1;
        private float m_timeScale = 1;

        private bool m_initialized = false;

        private int m_currentLODIndex = 0;
        private List<BoneInfo> m_currentLOD;

        private int m_hash = 0;

        // mwm path stored for debugging purposes
        public string AnimationMwmPathDebug = null;
        public string AnimationNameDebug = null;

        #endregion

        #region Properties

        public void Advance(float value)
        {
            if (m_frameOption != MyFrameOption.JustFirstFrame)
            {
                Position += value * m_timeScale;

                if (m_frameOption == MyFrameOption.StayOnLastFrame && Position > m_duration)
                    Position = m_duration;
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

                if (newVal > m_duration)
                {
                    if (Looping)
                        newVal = newVal - m_duration;
                    else
                        value = m_duration;
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


        bool t = true;
        public void UpdateBones(float distance)
        {
            if (!ENABLE_ANIMATION_LODS)
            {
                for (int i = 0; i < m_boneCount; i++)
                {
                    BoneInfo bone = m_boneInfos[i];
                    bone.SetPosition(m_position);
                }
                return;
            }


            m_currentLODIndex = -1;
            m_currentLOD = null;
            int tmpLOD = 0;

     
            List<BoneInfo> boneInfos = null;

            foreach (var boneLOD in m_boneLODs)
            {
                if (distance > boneLOD.Key)
                {
                    boneInfos = boneLOD.Value;
                    m_currentLODIndex = tmpLOD;
                    m_currentLOD = boneInfos;
                }
                else
                    break;

                tmpLOD++;
            }

            if (boneInfos != null)
            {
                AnimationPlayer cachedPlayer;
                if (CachedAnimationPlayers.TryGetValue(m_hash, out cachedPlayer) && cachedPlayer == this)
                    CachedAnimationPlayers.Remove(m_hash);

                m_hash = GetAnimationPlayerHash(this);

                if (CachedAnimationPlayers.TryGetValue(m_hash, out cachedPlayer))
                {
                    System.Diagnostics.Debug.Assert(cachedPlayer != this, "Cannot be cached like this");

                    var cachedHash = GetAnimationPlayerHash(cachedPlayer);
                    if (m_hash != cachedHash)
                    {
                        CachedAnimationPlayers.Remove(m_hash);
                        cachedPlayer = null;
                    }
                }

                if (cachedPlayer != null)
                {
                    for (int b = 0; b < boneInfos.Count; b++)
                    {
                        boneInfos[b].Translation = cachedPlayer.m_currentLOD[b].Translation;
                        boneInfos[b].Rotation = cachedPlayer.m_currentLOD[b].Rotation;
                        boneInfos[b].AssignToCharacterBone();
                    }
                }
                else
                {
                    if (boneInfos.Count > 0)
                    {
                        if (ENABLE_ANIMATION_CACHE)
                            CachedAnimationPlayers[m_hash] = this;

                        foreach (var bone in boneInfos)
                        {
                            bone.SetPosition(m_position);
                        }
                    }                    
                }
            }
        }


        /// <summary>
        /// The looping option. Set to true if you want the animation to loop
        /// back at the end
        /// </summary>
        public bool Looping { get { return m_frameOption == MyFrameOption.Loop ; }  }


        public bool AtEnd
        {
            get { return Position >= m_duration && m_frameOption != MyFrameOption.StayOnLastFrame; }
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
            if (m_hash != 0)
            {
                CachedAnimationPlayers.Remove(m_hash);
                m_hash = 0;
            }

            Name = player.Name;
            m_duration = player.m_duration;
            m_skinnedEntity = player.m_skinnedEntity;
            m_weight = player.Weight;
            m_timeScale = player.m_timeScale;
            m_frameOption = player.m_frameOption;

            foreach(var list in m_boneLODs.Values)
                list.Clear();
            //m_boneLODs.Clear();

            m_boneCount = player.m_boneCount;
            if (m_boneInfos == null || m_boneInfos.Length < m_boneCount)
                m_boneInfos = new BoneInfo[m_boneCount];


            Position = player.Position;


            for (int b = 0; b < m_boneCount; b++)
            {
                var boneInfo = m_boneInfos[b];
                if (boneInfo == null)
                {
                    boneInfo = new BoneInfo();
                    m_boneInfos[b] = boneInfo;
                }

                // Create it
                boneInfo.ClipBone = player.m_boneInfos[b].ClipBone;
                boneInfo.Player = this;
                    
                // Assign it to a model bone
                boneInfo.SetModel(m_skinnedEntity);
                boneInfo.CurrentKeyframe = player.m_boneInfos[b].CurrentKeyframe;
                boneInfo.SetPosition(Position);


                if (player.m_boneLODs != null && boneInfo.ModelBone != null && ENABLE_ANIMATION_LODS)
                {
                    foreach (var boneLOD in player.m_boneLODs)
                    {
                        List<BoneInfo> lodBones;
                        if (!m_boneLODs.TryGetValue(boneLOD.Key, out lodBones))
                        {
                            lodBones = new List<BoneInfo>();
                            m_boneLODs.Add(boneLOD.Key, lodBones);
                        }

                        foreach (var boneName in boneLOD.Value)
                        {
                            if (boneName.ModelBone == null)
                                continue;

                            if (boneInfo.ModelBone.Name == boneName.ModelBone.Name)
                            {
                                lodBones.Add(boneInfo);
                                break;
                            }
                        }
                    }
                }

                if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
                {
                    int index;
                    System.Diagnostics.Debug.Assert(m_skinnedEntity.AnimationController.FindBone(boneInfo.ClipBone.Name, out index) != null, "Can not find clip bone with name: " + boneInfo.ClipBone.Name + " in model: " + m_skinnedEntity.Name);
                }
            }


            m_initialized = true;
        }

        public void Initialize(MyModel animationModel, string playerName, int clipIndex, MySkinnedEntity skinnedEntity, float weight, float timeScale, MyFrameOption frameOption, string[] explicitBones = null, Dictionary<float, string[]> boneLODs = null)
        {
            if (m_hash != 0)
            {
                CachedAnimationPlayers.Remove(m_hash);
                m_hash = 0;
            }

            var clip = animationModel.Animations.Clips[clipIndex];
            Name = MyStringId.GetOrCompute(animationModel.AssetName + " : " + playerName);
            m_duration = (float)clip.Duration;
            m_skinnedEntity = skinnedEntity;
            m_weight = weight;
            m_timeScale = timeScale;
            m_frameOption = frameOption;

            foreach (var list in m_boneLODs.Values)
                list.Clear();
            //m_boneLODs.Clear();

            List<BoneInfo> lod0;
            if (!m_boneLODs.TryGetValue(0, out lod0))
            {
                lod0 = new List<BoneInfo>();
                m_boneLODs.Add(0, lod0);
            }

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
                BoneInfo boneInfo = m_boneInfos[neededBonesCount];
                if(m_boneInfos[neededBonesCount] == null)
                    boneInfo = new BoneInfo(bone, this);
                else
                {
                    boneInfo.Clear();
                    boneInfo.Init(bone, this);
                }

                m_boneInfos[neededBonesCount] = boneInfo;

                // Assign it to a model bone
                m_boneInfos[neededBonesCount].SetModel(skinnedEntity);

                if (boneInfo.ModelBone != null)
                {
                    lod0.Add(boneInfo);

                    if (boneLODs != null)
                    {
                        foreach (var boneLOD in boneLODs)
                        {
                            List<BoneInfo> lodBones;
                            if (!m_boneLODs.TryGetValue(boneLOD.Key, out lodBones))
                            {
                                lodBones = new List<BoneInfo>();
                                m_boneLODs.Add(boneLOD.Key, lodBones);
                            }

                            foreach (var boneName in boneLOD.Value)
                            {
                                if (boneInfo.ModelBone.Name == boneName)
                                {
                                    lodBones.Add(boneInfo);
                                    break;
                                }
                            }
                        }
                    }
                }

                neededBonesCount++;
            }

            m_boneCount = neededBonesCount;

            Position = 0;

            m_initialized = true;
        }

        public void Done()
        {
            m_initialized = false;
            CachedAnimationPlayers.Remove(m_hash);
        }

        public bool IsInitialized
        {
            get { return m_initialized; }
        }

        MyAnimationClip.Bone FindBone(List<MyAnimationClip.Bone> bones, string name)
        {
            foreach (MyAnimationClip.Bone bone in bones)
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
            private int m_currentKeyframe = 0; // new anim sys: supported

            bool m_isConst = false; // new anim sys: supported (differently)
            
            public int CurrentKeyframe // new anim sys: omitted
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
            public Quaternion Rotation;

            /// <summary>
            /// Current animation translation
            /// </summary>
            public Vector3 Translation;

            public AnimationPlayer Player;

            /// <summary>
            /// We are at a location between Keyframe1 and Keyframe2 such 
            /// that Keyframe1's time is less than or equal to the current position
            /// </summary>
            public MyAnimationClip.Keyframe Keyframe1;

            /// <summary>
            /// Second keyframe value
            /// </summary>
            public MyAnimationClip.Keyframe Keyframe2;

            #endregion

            #region Properties

            /// <summary>
            /// The bone in the actual animation clip
            /// </summary>
            MyAnimationClip.Bone m_clipBone;
            public MyAnimationClip.Bone ClipBone
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
            public BoneInfo(MyAnimationClip.Bone bone, AnimationPlayer player)
            {
                Init(bone, player);
            }

            public void Init(MyAnimationClip.Bone bone, AnimationPlayer player)
            {
                this.ClipBone = bone;
                Player = player;

                SetKeyframes();
                SetPosition(0);

                m_isConst = ClipBone.Keyframes.Count == 1;
            }

            public void Clear()
            {
                m_currentKeyframe = 0;
                m_isConst = false;
                m_assignedBone = null;
                Rotation = default(Quaternion);
                Translation = Vector3.Zero;
                Player = null;
                Keyframe1 = null;
                Keyframe2 = null;
                m_clipBone = null;
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

                List<MyAnimationClip.Keyframe> keyframes = ClipBone.Keyframes;
                if (keyframes == null || Keyframe1 == null || Keyframe2 == null || keyframes.Count == 0)
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
                        Rotation = Keyframe1.Rotation;
                        Translation = Keyframe1.Translation;
                    }
                    else
                    {
                        // Interpolate between keyframes
                        float t = (float)((position - Keyframe1.Time) * Keyframe2.InvTimeDiff);

                        t = MathHelper.Clamp(t, 0, 1);

                        Quaternion.Slerp(ref Keyframe1.Rotation, ref Keyframe2.Rotation, t, out Rotation);
                        Vector3.Lerp(ref Keyframe1.Translation, ref Keyframe2.Translation, t, out Translation);
                    }
                }

                AssignToCharacterBone();
            }

            public void AssignToCharacterBone()
            {
                if (m_assignedBone != null)
                {
                    Quaternion rotation = Rotation;

                    Quaternion additionalRotation = Player.m_skinnedEntity.GetAdditionalRotation(m_assignedBone.Name);
                    rotation = Rotation * additionalRotation;

                    m_assignedBone.SetCompleteTransform(ref Translation, ref rotation, Player.Weight);
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
                m_assignedBone = skinnedEntity.AnimationController.FindBone(ClipBone.Name, out index);
            }

            #endregion
        }

        #endregion

    }
}

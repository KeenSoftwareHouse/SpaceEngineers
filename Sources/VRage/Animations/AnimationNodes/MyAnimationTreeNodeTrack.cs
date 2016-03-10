using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRage.Animations.AnimationNodes
{
    /// <summary>
    /// Node of animation tree: single track. Contains reference to animation clip.
    /// </summary>
    public class MyAnimationTreeNodeTrack : MyAnimationTreeNode
    {
        // Animation clip bound to this track.
        private MyAnimationClip m_animationClip = null;
        // Local time (we need to store it because it can be different in each track!)
        private double m_localTime = 0;
        // Speed - local time multiplier.
        private double m_speed = 1;
        // Current keyframe indices (for each bone of clip).
        private int[] m_currentKeyframes;
        // Indices of bones in entity skeleton (for each bone of clip).
        // Note: We can (maybe) store this mapping on some reused place like MyModel?
        //       It is not a really big cpu/memory issue now, but can improve the game a bit.
        private int[] m_boneIndicesMapping;
        // If true, animation will loop automatically.
        private bool m_loop = true;
        // If true, animation will interpolate between keyframes.
        public bool m_interpolate = true;

        // --------------- properties ---------------------------------------------------------

        // If true, animation will loop automatically.
        public bool Loop 
        {
            get { return m_loop; }
            set { m_loop = value; }
        }
        // Local time multiplayer (playing speed, 1 is default).
        public double Speed
        {
            get { return m_speed; }
            set { m_speed = value; }
        }

        // If true, animation will interpolate between keyframes.
        public bool Interpolate
        {
            get { return m_interpolate; }
            set { m_interpolate = value; }
        }

        // --------------- constructor --------------------------------------------------------

        // Default constructor.
        public MyAnimationTreeNodeTrack()
        {
        }

        // --------------- methods (public) --------------------------------------------------

        // Pass clip that should be bound to this track. Returns false on failure.
        // Note: Passing null makes this node invalid and may result in invalid animation system result.
        public bool SetClip(MyAnimationClip animationClip)
        {
            m_animationClip = animationClip;
            m_currentKeyframes = animationClip != null ? new int[animationClip.Bones.Count] : null;
            m_boneIndicesMapping = null; // will be initialized later, we do not have sufficient data here
            return true;
        }

        // Update animation node = compute bones positions and orientations from the track. 
        public override void Update(ref MyAnimationUpdateData data)
        {
            // allocate result array from pool
            data.BonesResult = data.Controller.ResultBonesPool.Alloc();
            if (m_animationClip != null && m_animationClip.Bones != null)
            {
                // if necessary, then rebuild bone indices mapping
                if (m_boneIndicesMapping == null)
                {
                    RebuildBoneIndices(data.CharacterBones);
                    Debug.Assert(m_boneIndicesMapping != null);
                }

                // advance local time
                m_localTime += data.DeltaTimeInSeconds * Speed;
                if (m_loop)
                {
                    while (m_localTime >= m_animationClip.Duration)
                        m_localTime -= m_animationClip.Duration;
                }
                else
                {
                    if (m_localTime >= m_animationClip.Duration)
                    {
                        m_localTime = m_animationClip.Duration;
                    }
                }
            
                // update indices of keyframes (for every bone)
                UpdateKeyframeIndices();
                // ok, compute for every bone
                for (int i = 0; i < m_animationClip.Bones.Count; i++)
                {
                    var currentBone = m_animationClip.Bones[i];
                    // two keyframe indices to be blended together (i, i+1)
                    // blending N-1 to 0 if looped, staying on the last frame if not looped
                    var currentKeyFrameIndex = m_currentKeyframes[i];
                    var currentKeyFrameIndex2 = currentKeyFrameIndex + 1;
                    if (currentKeyFrameIndex2 >= currentBone.Keyframes.Count) // safety
                        currentKeyFrameIndex2 = Math.Max(0, currentBone.Keyframes.Count - 1);

                    int charBoneIndex = m_boneIndicesMapping[i]; // use mapping clip bone -> char bone
                    if (charBoneIndex < 0 || data.LayerBoneMask[charBoneIndex] == false) // unaffected bone?
                        continue;

                    if (currentKeyFrameIndex != currentKeyFrameIndex2 && m_interpolate)
                    {
                        // interpolate between two keyframes
                        var keyframe1 = currentBone.Keyframes[currentKeyFrameIndex];
                        var keyframe2 = currentBone.Keyframes[currentKeyFrameIndex2];

                        float t = (float)((m_localTime - keyframe1.Time) * keyframe2.InvTimeDiff);
                        t = MathHelper.Clamp(t, 0, 1);
                        Quaternion.Slerp(ref keyframe1.Rotation, ref keyframe2.Rotation, t, out data.BonesResult[charBoneIndex].Rotation);
                        Vector3.Lerp(ref keyframe1.Translation, ref keyframe2.Translation, t, out data.BonesResult[charBoneIndex].Translation);
                    }
                    else if (currentBone.Keyframes.Count != 0)
                    {
                        // just copy keyframe, because currentKeyFrameIndex == currentKeyFrameIndex2
                        data.BonesResult[charBoneIndex].Rotation = currentBone.Keyframes[currentKeyFrameIndex].Rotation;
                        data.BonesResult[charBoneIndex].Translation = currentBone.Keyframes[currentKeyFrameIndex].Translation;
                    }
                    else
                    {
                        // zero keyframe count -> rest pose, leave it be (it is there from allocation)
                    }
                }
            }
        }

        // Get local time in normalized format (from 0 to 1).
        // May fail for more complicated structure - there can be more independent local times (each track has its own).
        public override float GetLocalTimeNormalized()
        {
            if (m_animationClip != null && m_animationClip.Duration > 0.0)
                return m_localTime < m_animationClip.Duration ? (float)(m_localTime / m_animationClip.Duration) : 1.0f;
            else
                return 0.0f;
        }

        // Set local time in normalized format (from 0 to 1).
        public override void SetLocalTimeNormalized(float normalizedTime)
        {
            if (m_animationClip != null)
            {
                m_localTime = normalizedTime * m_animationClip.Duration;
            }
        }

        // ----------------- methods (private) ------------------------------------------------

        // Update animation node = compute bones positions and orientations from the track. 
        private void UpdateKeyframeIndices()
        {
            if (m_animationClip == null || m_animationClip.Bones == null)
                return;

            // find keyframe, because time advanced
            for (int i = 0; i < m_animationClip.Bones.Count; i++)
            {
                var currentBone = m_animationClip.Bones[i];
                int currentKeyFrameIndex = m_currentKeyframes[i];

                // current frame index is late, go forward, last keyframe is currently never used as base
                while (currentKeyFrameIndex < currentBone.Keyframes.Count - 2 &&  m_localTime > currentBone.Keyframes[currentKeyFrameIndex + 1].Time)
                    currentKeyFrameIndex++;
                // current frame index is ahead, go back
                while (currentKeyFrameIndex > 0 && m_localTime < currentBone.Keyframes[currentKeyFrameIndex].Time)
                    currentKeyFrameIndex--;

                m_currentKeyframes[i] = currentKeyFrameIndex;
            }

        }

        // Rebuild mapping clip bone indices to character bone indices.
        private void RebuildBoneIndices(MyCharacterBone[] characterBones)
        {
            // find indices of clip bones 
            // optimize? see m_boneIndices declaration
            m_boneIndicesMapping = new int[m_animationClip.Bones.Count];
            for (int i = 0; i < m_animationClip.Bones.Count; i++)
            {
                m_boneIndicesMapping[i] = -1;
                for (int j = 0; j < characterBones.Length; j++)
                {
                    if (m_animationClip.Bones[i].Name == characterBones[j].Name)
                    {
                        m_boneIndicesMapping[i] = j;
                        break;
                    }
                }
                if (m_boneIndicesMapping[i] == -1)
                {
                    // VRAGE TODO: at least log it? this is unfortunatelly a common situation
                    //Debug.Fail(String.Format("Clip bone not found in character. {0} in {1}", 
                    //    m_animationClip.Bones[i].Name,
                    //    string.Join(",", (from item in characterBones select item.Name).ToArray())));
                }
            }
        }
    }
}

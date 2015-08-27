using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Animations
{
    /// <summary>
    /// An animation clip is a set of keyframes with associated bones.
    /// </summary>
    public class AnimationClip
    {
        #region Keyframe and Bone nested class

        /// <summary>
        /// An Keyframe is a rotation and translation for a moment in time.
        /// It would be easy to extend this to include scaling as well.
        /// </summary>
        public class Keyframe
        {
            public double Time = 0;                             // The keyframe time
            public Quaternion Rotation = Quaternion.Identity;   // The rotation for the bone
            public Vector3 Translation;                         // The translation for the bone

            public double TimeDiff;
        }

        /// <summary>
        /// Keyframes are grouped per bone for an animation clip
        /// </summary>
        public class Bone
        {
            /// <summary>
            /// Each bone has a name so we can associate it with a runtime model
            /// </summary>
            private string m_name = "";

            /// <summary>
            /// The keyframes for this bone
            /// </summary>
            private List<Keyframe> m_keyframes = new List<Keyframe>();

            /// <summary>
            /// The bone name for these keyframes
            /// </summary>
            public string Name { get { return m_name; } set { m_name = value; } }

            /// <summary>
            /// The keyframes for this bone
            /// </summary>
            public List<Keyframe> Keyframes { get { return m_keyframes; } }

            public override string ToString()
            {
                return m_name + " (" + Keyframes.Count + " keys)";
            }
        }

        #endregion

        /// <summary>
        /// The bones for this animation
        /// </summary>
        private List<Bone> bones = new List<Bone>();

        /// <summary>
        /// Name of the animation clip
        /// </summary>
        public string Name;

        /// <summary>
        /// Duration of the animation clip
        /// </summary>
        public double Duration;

        /// <summary>
        /// The bones for this animation clip with their keyframes
        /// </summary>
        public List<Bone> Bones { get { return bones; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender.Animations
{
    /// <summary>
    /// Class that contains additional information attached to the model and
    /// shared with the runtime.
    /// </summary>
    public class ModelAnimations
    {
        #region Fields

        /// <summary>
        /// The bone indices for the skeleton associated with any
        /// skinned model.
        /// </summary>
        private List<int> skeleton = new List<int>();
        
        /// <summary>
        /// Any associated animation clips
        /// </summary>
        List<MyAnimationClip> clips = new List<MyAnimationClip>();

        #endregion

        #region Properties

        /// <summary>
        /// The bone indices for the skeleton associated with any
        /// skinned model.
        /// </summary>
        public List<int> Skeleton { get { return skeleton; } set { skeleton = value; } }

        /// <summary>
        /// Animation clips associated with this model
        /// </summary>
        public List<MyAnimationClip> Clips { get { return clips; } set { clips = value; } }

        #endregion
    }
}

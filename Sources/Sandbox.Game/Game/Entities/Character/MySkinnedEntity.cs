#region Using

using Havok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Components;
using VRage.ModAPI;
using VRage.Import;

#endregion

namespace Sandbox.Game.Entities
{
    public class MySkinnedEntity : MyEntity
    {
        #region Fields

        private List<MyCharacterBone> m_bones = new List<MyCharacterBone>();
        public List<MyCharacterBone> Bones { get { return m_bones; } }

        public Matrix[] BoneTransforms;
         


        protected ulong m_actualUpdateFrame = 0;
        protected ulong m_actualDrawFrame = 0;
        protected bool m_characterBonesReady = false;

        List<Matrix> m_simulatedBones = new List<Matrix>();
        
        #endregion

        #region Init


        public MySkinnedEntity()
        {
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
        }


        protected void InitBones()
        {
            ObtainBones();
            BoneTransforms = new Matrix[m_bones.Count];
        }


        /// <summary>
        /// Get the bones from the model and create a bone class object for
        /// each bone. We use our bone class to do the real animated bone work.
        /// </summary>
        protected void ObtainBones()
        {
            m_bones.Clear();
            foreach (MyModelBone bone in Model.Bones)
            {
                Matrix boneTransform = bone.Transform;

                // Create the bone object and add to the heirarchy
                MyCharacterBone newBone = new MyCharacterBone(bone.Name, boneTransform, bone.Parent != -1 ? m_bones[bone.Parent] : null);

                // Add to the bones for this model
                m_bones.Add(newBone);
            }
        }

          
        #endregion

        #region Simulation




        #endregion

    }
}

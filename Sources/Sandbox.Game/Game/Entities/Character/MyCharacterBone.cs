using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Bones in this model are represented by this class, which
    /// allows a bone to have more detail associatd with it.
    /// 
    /// This class allows you to manipulate the local coordinate system
    /// for objects by changing the scaling, translation, and rotation.
    /// These are indepenent of the bind transformation originally supplied
    /// for the model. So, the actual transformation for a bone is
    /// the product of the:
    /// 
    /// Scaling
    /// Bind scaling (scaling removed from the bind transform)
    /// Rotation
    /// Translation
    /// Bind Transformation
    /// Parent Absolute Transformation
    /// 
    /// </summary>
	public class MyCharacterBone
    {
        #region Fields

        /// <summary>
        /// Any parent for this bone
        /// </summary>
        private MyCharacterBone m_parent = null;

        private List<MyCharacterBone> m_children;

        /// <summary>
        /// The bind transform is the transform for this bone
        /// as loaded from the original model. It's the base pose.
        /// I do remove any scaling, though.
        /// </summary>
        private Matrix m_bindTransform = Matrix.Identity;

        /// <summary>
        /// Any translation applied to the bone
        /// </summary>
        private Vector3 m_translation = Vector3.Zero;

        /// <summary>
        /// Any rotation applied to the bone
        /// </summary>
        private Quaternion m_rotation = Quaternion.Identity;

        /// <summary>
        /// Any scaling applied to the bone
        /// </summary>
        private Vector3 m_scale = Vector3.One;

        /// <summary>
        /// computed bone transform
        /// </summary>
        private Matrix m_transform = Matrix.Identity;

        /// <summary>
        /// indicates whether bone needs recalculation
        /// </summary>
        private bool m_changed = true;

        /// <summary>
        /// indicates whether hierarchy of bones is updating
        /// </summary>
        private bool m_hierarchyUpdate = false;

        #endregion 

        #region Properties

        /// <summary>
        /// The bone name
        /// </summary>
		public string Name = "";

        /// <summary>
        /// The bone bind transform
        /// </summary>
        public Matrix BindTransform { get {return m_bindTransform;} }

        /// <summary>
        /// Inverse of absolute bind transform for skinnning
        /// </summary>
        public Matrix SkinTransform { get; set; }

        /// <summary>
        /// Bone rotation
        /// </summary>
        public Quaternion Rotation { get { return m_rotation; } set { m_rotation = value; m_changed = true; } }

        /// <summary>
        /// Any translations
        /// </summary>
        public Vector3 Translation { get { return m_translation; } set { m_translation = value; m_changed = true; } }

        /// <summary>
        /// Any scaling
        /// </summary>
        public Vector3 Scale { get { return m_scale; } set { m_scale = value; m_changed = true; } }

        /// <summary>
        /// The parent bone or null for the root bone
        /// </summary>
        public MyCharacterBone Parent { get { return m_parent; } }

        /// <summary>
        /// The bone absolute transform
        /// </summary>
        public Matrix AbsoluteTransform = Matrix.Identity;

        bool HasChanged
        {
            get
            {
                if (m_parent != null) 
                    return Parent.HasChanged || m_changed || m_hierarchyUpdate;

                return m_changed || m_hierarchyUpdate;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Constructor for a bone object
        /// </summary>
        /// <param name="name">The name of the bone</param>
        /// <param name="bindTransform">The initial bind transform for the bone</param>
        /// <param name="parent">A parent for this bone</param>
        public MyCharacterBone(string name, Matrix bindTransform, MyCharacterBone parent)
        {
            this.Name = name;
            this.m_parent = parent;
            this.m_bindTransform = bindTransform;
            this.m_children = new List<MyCharacterBone>();
            if (this.m_parent != null) this.m_parent.AddChild(this);

            // Set the skinning bind transform
            // That is the inverse of the absolute transform in the bind pose

            ComputeAbsoluteTransform();

            SkinTransform = Matrix.Invert(AbsoluteTransform);
        }

        /// <summary>
        /// Compute the absolute transformation for this bone.
        /// </summary>
        public void ComputeAbsoluteTransform()
        {
            if (!HasChanged) return;

            Matrix transform = ComputeBoneTransform();

            if (Parent != null)
            {
                // If we have a parent bone, we need to make sure, it has correct absolute transform as well, so recalculate whole chain
               // Parent.ComputeAbsoluteTransform();

                // This bone has a parent bone
                Matrix.Multiply(ref transform, ref Parent.AbsoluteTransform, out AbsoluteTransform);
            }
            else
            {   // The root bone
                AbsoluteTransform = transform;
            }

            m_hierarchyUpdate = true;
            PropagateTransform();
            m_hierarchyUpdate = false;
        }

        public Matrix ComputeBoneTransform()
        {
            if (m_changed)
            {
                m_transform = Matrix.CreateScale(Scale) *
                    Matrix.CreateFromQuaternion(Rotation) *
                    Matrix.CreateTranslation(Translation);

                m_transform *= BindTransform;
                m_changed = false;
            }
            return m_transform;
        }

        /// <summary>
        /// This sets the rotation and translation such that the
        /// rotation times the translation times the bind after set
        /// equals this matrix. This is used to set animation values.
        /// </summary>
        /// <param name="m">A matrix include translation and rotation</param>
        public void SetCompleteTransform(Matrix m, float weight)
        {
            m_changed = true;

            Matrix setTo = m * Matrix.Invert(BindTransform);

            //Translation += setTo.Translation * weight;
            Translation = Vector3.Lerp(Translation, setTo.Translation, weight);
            //Translation = Vector3.Zero;

            Quaternion newRotation = Quaternion.CreateFromRotationMatrix(setTo);
            Rotation = Quaternion.Slerp(Rotation, newRotation, weight);
            //Rotation = Quaternion.Identity;
        }

        #endregion


        internal void SetBindTransform(Matrix bindTransform)
        {
            m_changed = true;
            m_bindTransform = bindTransform;
        }

        internal void AddChild(MyCharacterBone child)
        {
            m_children.Add(child);
        }

        private void PropagateTransform()
        {            
            foreach (var bone in m_children)
            {
                bone.ComputeAbsoluteTransform();
            }
        }


        /// <summary>
        /// Returns bone's rig absolute transform - including transforms of all parent bones
        /// </summary>
        /// <returns></returns>
        internal Matrix GetAbsoluteRigTransform()
        {
            MyCharacterBone parentBone = m_parent;
            if (parentBone != null)
            {
                Matrix absoluteRigTransform = m_bindTransform;
                while (parentBone != null)
                {
                    absoluteRigTransform = absoluteRigTransform * parentBone.m_bindTransform;
                    parentBone = parentBone.Parent;
                }
                return absoluteRigTransform;

            }
            return m_bindTransform;            
        }
    }
}

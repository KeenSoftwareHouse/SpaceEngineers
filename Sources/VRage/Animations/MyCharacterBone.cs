using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Animations
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
        private Matrix m_bindTransformInv = Matrix.Identity;
        private Quaternion m_bindRotationInv = Quaternion.Identity;

        /// <summary>
        /// Any translation applied to the bone
        /// </summary>
        private Vector3 m_translation = Vector3.Zero;

        /// <summary>
        /// Any rotation applied to the bone
        /// </summary>
        private Quaternion m_rotation = Quaternion.Identity;

        /// <summary>
        /// computed bone transform
        /// </summary>
        private Matrix m_transform = Matrix.Identity;

        /// <summary>
        /// indicates whether bone needs recalculation
        /// </summary>
        private bool m_changed = true;

        #endregion 

        #region Properties

        /// <summary>
        /// The bone name
        /// </summary>
		public string Name = "";

        public int Index { get; private set; }

        /// <summary>
        /// The bone bind transform
        /// </summary>
        public Matrix BindTransform { get {return m_bindTransform;} }
        public Matrix BindTransformInv { get { return m_bindTransformInv; } }

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
        /// The parent bone or null for the root bone
        /// </summary>
        public MyCharacterBone Parent { get { return m_parent; } }

        /// <summary>
        /// The bone absolute transform
        /// </summary>
        public Matrix AbsoluteTransform = Matrix.Identity;

        /// <summary>
        /// The bone absolute transform
        /// </summary>
        public Matrix RelativeTransform
        {
            get { return m_transform; }
        }

        /// <summary>
        /// Has this bone or any parent bone changed?
        /// </summary>
        bool HasThisOrAnyParentChanged
        {
            get
            {
                MyCharacterBone current = this;
                do
                {
                    if (current.m_changed)
                        return true;
                    current = current.Parent;
                } while (current != null);

                return false;
            }
        }

        public int Depth
        {
            get;
            private set;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Constructor for a bone object
        /// </summary>
        /// <param name="name">The name of the bone</param>
        /// <param name="bindTransform">The initial bind transform for the bone</param>
        /// <param name="parent">A parent for this bone</param>
        public MyCharacterBone(int index, string name, Matrix bindTransform, MyCharacterBone parent)
        {
            Index = index;
            this.Name = name;
            this.m_parent = parent;
            Depth = GetHierarchyDepth();
            this.m_bindTransform = bindTransform;
            this.m_bindTransformInv = Matrix.Invert(bindTransform);
            this.m_bindRotationInv = Quaternion.CreateFromRotationMatrix(m_bindTransformInv); 
            this.m_children = new List<MyCharacterBone>();
            if (this.m_parent != null) this.m_parent.AddChild(this);

            // Set the skinning bind transform
            // That is the inverse of the absolute transform in the bind pose

            ComputeAbsoluteTransform();

            SkinTransform = Matrix.Invert(AbsoluteTransform);
        }

        static MyCharacterBone()
        {
            Debug.Assert(typeof(MyCharacterBone).IsValueType == false);
        }

        private int GetHierarchyDepth()
        {
            int depth = 0;
            MyCharacterBone current = m_parent;
            while (current != null)
            {
                current = current.Parent;
                depth++;
            }
            return depth;
        }

        /// <summary>
        /// Compute absolute bone transforms for whole hierarchy.
        /// Expects the array to be sorted by depth in hiearachy.
        /// </summary>
        /// <param name="bones"></param>
        public static void ComputeAbsoluteTransforms(MyCharacterBone[] bones)
        {
            foreach (MyCharacterBone bone in bones)
            {
                if (bone.Parent != null)
                {
                    bone.m_changed = bone.ComputeBoneTransform() || bone.Parent.m_changed; // propagate the change to children
                    if (bone.m_changed)
                        Matrix.Multiply(ref bone.m_transform, ref bone.Parent.AbsoluteTransform, out bone.AbsoluteTransform);
                }
                else
                {
                    bone.m_changed = bone.ComputeBoneTransform();
                    if (bone.m_changed)
                        bone.AbsoluteTransform = bone.m_transform;
                }
            }

            foreach (MyCharacterBone bone in bones)
                bone.m_changed = false;
        }

        /// <summary>
        /// Compute the absolute transformation for this bone.
        /// </summary>
        public void ComputeAbsoluteTransform()
        {
            if (!HasThisOrAnyParentChanged) return;
            m_changed = ComputeBoneTransform(); // updates m_transform
            if (Parent != null)
            {
                // If we have a parent bone, we need to make sure, it has correct absolute transform as well, so recalculate whole chain
               // Parent.ComputeAbsoluteTransform();

                // This bone has a parent bone
                Matrix.Multiply(ref m_transform, ref Parent.AbsoluteTransform, out AbsoluteTransform);
            }
            else
            {   // The root bone
                AbsoluteTransform = m_transform;
            }

            PropagateTransform();
            m_changed = false;
        }

        // Update relative transform (m_transform).
        // Returns if the relative transform was changed.
        public bool ComputeBoneTransform()
        {
            if (m_changed)
            {
                //Matrix rotationMatrix;
                //Matrix.CreateFromQuaternion(ref m_rotation, out rotationMatrix);

                //Matrix translationMatrix;
                //Matrix.CreateTranslation(ref m_translation, out translationMatrix);
                //Matrix.Multiply(ref rotationMatrix, ref translationMatrix, out m_transform);

                Matrix.CreateFromQuaternion(ref m_rotation, out m_transform);
                m_transform.M41 = m_translation.X;
                m_transform.M42 = m_translation.Y;
                m_transform.M43 = m_translation.Z;
                Matrix.Multiply(ref m_transform, ref m_bindTransform, out m_transform);

                m_changed = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This sets the rotation and translation such that the
        /// rotation times the translation times the bind after set
        /// equals this matrix. This is used to set animation values.
        /// </summary>
        public void SetCompleteTransform(ref Vector3 translation, ref Quaternion rotation, float weight)
        {
            m_changed = true;

            Vector3 newTranslation;
            Vector3.Transform(ref translation, ref m_bindTransformInv, out newTranslation);
            Translation = Vector3.Lerp(Translation, newTranslation, weight);

            Quaternion newRotation;
            Quaternion.Multiply(ref m_bindRotationInv, ref rotation, out newRotation);
            Rotation = Quaternion.Slerp(Rotation, newRotation, weight);
        }

        /// <summary>
        /// This sets the rotation and translation such that the
        /// rotation times the translation times the bind after set
        /// equals this matrix. This is used to set animation values.
        /// </summary>
        public void SetCompleteTransform(ref Vector3 translation, ref Quaternion rotation)
        {
            Vector3.Transform(ref translation, ref m_bindTransformInv, out m_translation);
            Quaternion.Multiply(ref m_bindRotationInv, ref rotation, out m_rotation);
            m_changed = true;
        }

        /// <summary>
        /// Same as SetCompleteTransform, but result is not stored internally, it is returned instead.
        /// </summary>
        public void GetCompleteTransform(ref Vector3 translation, ref Quaternion rotation, 
            out Vector3 completeTranslation, out Quaternion completeRotation)
        {
            Vector3.Transform(ref translation, ref m_bindTransformInv, out completeTranslation);
            Quaternion.Multiply(ref m_bindRotationInv, ref rotation, out completeRotation);
        }

        #endregion


        internal void SetBindTransform(Matrix bindTransform)
        {
            m_changed = true;
            m_bindTransform = bindTransform;
            m_bindTransformInv = Matrix.Invert(bindTransform);
            m_bindRotationInv = Quaternion.CreateFromRotationMatrix(m_bindTransformInv);
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
        public Matrix GetAbsoluteRigTransform()
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

        public MyCharacterBone GetChildBone(int childIndex)
        {
            if (m_children == null || childIndex < 0 || childIndex >= m_children.Count)
                return null;
            return m_children[childIndex];
        }
    }
}

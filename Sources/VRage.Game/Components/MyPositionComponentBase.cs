using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.Utils;
using VRageMath;

namespace VRage.Components
{
    public abstract class MyPositionComponentBase : MyEntityComponentBase
    {
        /// <summary>
        /// Internal world matrix of entity.
        /// </summary>
        
        static private BoundingBoxD m_invalidBox = BoundingBoxD.CreateInvalid();
        protected MatrixD m_worldMatrix = MatrixD.Identity;
        //protected MatrixD m_previousWorldMatrix = MatrixD.Identity;
        /// <summary>
        /// Internal local matrix relative to parent of entity.
        /// </summary>
        protected MatrixD m_localMatrix = MatrixD.Identity;

        //Bounding boxes
        protected BoundingBox m_localAABB;   //untransformed aabb of this entity (mostly LOD0 model) 
        protected BoundingSphere m_localVolume;   //untransformed sphere of this entity (mostly LOD0 model) 
        protected Vector3 m_localVolumeOffset;
        protected BoundingBoxD m_worldAABB;   //world AABB of this entity
        protected BoundingSphereD m_worldVolume;   //sphere volume
        protected bool m_worldVolumeDirty = false;
        private BoundingBoxD m_worldAABBHr;   //world AABB of this entity including children
        private BoundingSphereD m_worldVolumeHr;   //sphere volume including children
        float? m_scale;
        //protected bool m_localMatrixChanged = false;
        #region Properties
        /// <summary>
        /// World matrix of this physic object. Use it whenever you want to do world-matrix transformations with this physic objects.
        /// </summary>
        public MatrixD WorldMatrix
        {
            get 
            {
                return this.m_worldMatrix; 
            }
            set { SetWorldMatrix(value); }
        }

        /// <summary>
        /// Gets or sets the local matrix.
        /// </summary>
        /// <value>
        /// The local matrix.
        /// </value>
        public Matrix LocalMatrix
        {
            get { return this.m_localMatrix; }
            set { SetLocalMatrix(value); }
        }

        /// <summary>
        /// Gets the world aabb.
        /// </summary>
        public BoundingBoxD WorldAABB
        {
            get 
            {
                if (m_worldVolumeDirty)
                {
                    UpdateWorldVolume();
                }
                return m_worldAABB; 
            }
            //Protected
            set 
            { 
                m_worldAABB = value;
                //not true, what is the correct response here?
                //m_localAABB = new BoundingBox(value.Transform(WorldMatrixNormalizedInv));
                m_worldVolumeDirty = false; 
            }
        }

        /// <summary>
        /// Gets the world volume.
        /// </summary>
        public BoundingSphereD WorldVolume
        {
            get 
            { 
                if(m_worldVolumeDirty)
                {
                    UpdateWorldVolume();
                }
                return m_worldVolume; 
            }
            //Protected
            set { m_worldVolume = value; }
        }

        /// <summary>
        /// Gets the hiearchical box in world.
        /// </summary>
        public BoundingBoxD WorldAABBHr
        {
            get
            {
                return m_worldAABBHr;
            }
        }

        /// <summary>
        /// Gets the hiearchical volume in world.
        /// </summary>
        public BoundingSphereD WorldVolumeHr
        {
            get
            {
                return m_worldVolumeHr;
            }
        }

        /// <summary>
        /// Sets the local aabb.
        /// </summary>
        /// <value>
        /// The local aabb.
        /// </value>
        public virtual BoundingBox LocalAABB
        {
            get
            {
                return m_localAABB;
            }
            set
            {
                m_localAABB = value;
                m_localVolume = BoundingSphere.CreateFromBoundingBox(m_localAABB);
                m_worldVolumeDirty = true;
            }
        }

        /// <summary>
        /// Sets the local aabb.
        /// </summary>
        /// <value>
        /// The local aabb.
        /// </value>
        public virtual BoundingBox LocalAABBHr
        {
            get
            {
                return m_localAABB;
            }
        }


        /// <summary>
        /// Sets the local volume.
        /// </summary>
        /// <value>
        /// The local volume.
        /// </value>
        public BoundingSphere LocalVolume
        {
            get
            {
                return m_localVolume;
            }
            set
            {
                m_localVolume = value;
                m_localAABB = MyMath.CreateFromInsideRadius(value.Radius);
                m_localAABB = m_localAABB.Translate(value.Center);
                m_worldVolumeDirty = true;
            }
        }


        /// <summary>
        /// Gets or sets the local volume offset.
        /// </summary>
        /// <value>
        /// The local volume offset.
        /// </value>
        public Vector3 LocalVolumeOffset
        {
            get
            {
                return this.m_localVolumeOffset;
            }
            set
            {
                this.m_localVolumeOffset = value;
                m_worldVolumeDirty = true;
            }
        }

        public event Action<MyPositionComponentBase> OnPositionChanged;

        protected void RaiseOnPositionChanged(MyPositionComponentBase component)
        {
            var handle = OnPositionChanged;
            if (handle != null)
            {
                handle(component);
            }
        }

        protected virtual bool ShouldSync
        {
            get
            {
                return Container.Get<MySyncComponentBase>() != null;
            }
        }

        public float? Scale
        {
            get { return m_scale; }
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;

                    var localMatrix = LocalMatrix;
                    if (m_scale != null)
                    {
                        System.Diagnostics.Debug.Assert(!MyUtils.IsZero(m_scale.Value));
                        var worldMatrix = WorldMatrix;

                        if (Container.Entity.Parent == null)
                        {
                            MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                            WorldMatrix = Matrix.CreateScale(m_scale.Value) * worldMatrix;
                        }
                        else
                        {
                            MyUtils.Normalize(ref localMatrix, out localMatrix);
                            LocalMatrix = Matrix.CreateScale(m_scale.Value) * localMatrix;
                        }
                    }
                    else
                    {
                        MyUtils.Normalize(ref localMatrix, out localMatrix);
                        LocalMatrix = localMatrix;
                    }

                    UpdateWorldMatrix();
                }
            }
        }

        #endregion

        #region Position And Movement Methods
        /// <summary>
        /// Sets the world matrix.
        /// </summary>
        /// <param name="worldMatrix">The world matrix.</param>
        /// <param name="source">The source object that caused this change or null when not important.</param>
        public virtual void SetWorldMatrix(MatrixD worldMatrix, object source = null, bool forceUpdate = false)
        {
            if (Entity.Parent != null && source != Entity.Parent)
                return;

            if (Scale != null)
            {
                MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                worldMatrix = MatrixD.CreateScale(Scale.Value) * worldMatrix;
            }

            if (m_worldMatrix.EqualsFast(ref worldMatrix) && !forceUpdate)
                return;


            if (this.Container.Entity.Parent == null)
            {
                this.m_worldMatrix = worldMatrix;
                m_localMatrix = worldMatrix;
            }
            else
            {
                MatrixD matParentInv = MatrixD.Invert(this.Container.Entity.Parent.WorldMatrix);
                m_localMatrix = worldMatrix * matParentInv;
            }

            //if (!m_localMatrix.EqualsFast(ref localMatrix))
            {
                //m_localMatrixChanged = true;
                //this.m_localMatrix = localMatrix;
                UpdateWorldMatrix(source);
            }       
        }

        /// <summary>
        /// Sets the local matrix.
        /// </summary>
        /// <param name="localMatrix">The local matrix.</param>
        /// <param name="source">The source object that caused this change or null when not important.</param>
        public void SetLocalMatrix(MatrixD localMatrix, object source = null)
        {
            if (this.m_localMatrix != localMatrix)
            {
                //m_localMatrixChanged = true;
                this.m_localMatrix = localMatrix;
                UpdateWorldMatrix(source);
            }
        }

        /// <summary>
        /// Gets the entity position.
        /// </summary>
        /// <returns></returns>
        public Vector3D GetPosition()
        {
            return this.m_worldMatrix.Translation;
        }


        /// <summary>
        /// Sets the position.
        /// </summary>
        /// <param name="pos">The pos.</param>
        public void SetPosition(Vector3D pos)
        {
            if (!MyUtils.IsZero(this.m_worldMatrix.Translation - pos))
            {
                this.m_worldMatrix.Translation = pos;
                UpdateWorldMatrix();
            }
        }


        protected bool m_normalizedInvMatrixDirty = true;
        private MatrixD m_normalizedWorldMatrixInv;

        public MatrixD WorldMatrixNormalizedInv
        {
            get
            {
                if (m_normalizedInvMatrixDirty)
                {
                    //If world matrix is scaled, normalize it first
                    if (!MyUtils.IsZero(m_worldMatrix.Left.LengthSquared() - 1.0))
                    {
                        MatrixD normalizedWorld = MatrixD.Normalize(m_worldMatrix);
                        MatrixD.Invert(ref normalizedWorld, out m_normalizedWorldMatrixInv);
                    }
                    else
                    {
                        MatrixD.Invert(ref m_worldMatrix, out m_normalizedWorldMatrixInv);
                    }

                    m_normalizedInvMatrixDirty = false;
                }
                return m_normalizedWorldMatrixInv;
            }
            private set
            {
                m_normalizedWorldMatrixInv = value;
            }
        }

        protected bool m_invScaledMatrixDirty = true;
        private MatrixD m_worldMatrixInvScaled;

        public MatrixD WorldMatrixInvScaled
        {
            get
            {
                if (m_invScaledMatrixDirty)
                {
                    //If world matrix is scaled, normalize it first
                    MatrixD wm = m_worldMatrix;
                    if (!MyUtils.IsZero(m_worldMatrix.Left.LengthSquared() - 1.0f))
                    {
                        wm = MatrixD.Normalize(m_worldMatrix);
                    }

                    if (Scale.HasValue)
                    {
                        wm = wm * Matrix.CreateScale(Scale.Value);
                    }

                    MatrixD.Invert(ref wm, out m_worldMatrixInvScaled);

                    m_invScaledMatrixDirty = false;
                }
                return m_worldMatrixInvScaled;
            }

            private set
            {
                m_worldMatrixInvScaled = value;
            }
        }

        /// <summary>
        /// Inverted world matrix of this physic object calculated on-demand (non cached).
        /// Use it whenever you want to do world-matrix transformations with this physic objects.
        /// </summary>
        [Obsolete("Use WorldMatrixInverted property")]
        public MatrixD GetWorldMatrixNormalizedInv()
        {
            return WorldMatrixNormalizedInv;
        }

        public virtual MatrixD GetViewMatrix()
        {
            return WorldMatrixNormalizedInv;
        }

        /// <summary>
        /// Updates the world matrix (change caused by this entity)
        /// </summary>
        protected virtual void UpdateWorldMatrix(object source = null)
        {
            if (this.Container.Entity.Parent != null)
            {
                MatrixD parentWorldMatrix = this.Container.Entity.Parent.WorldMatrix;
                UpdateWorldMatrix(ref parentWorldMatrix, source);
                return;
            }

            //UpdateWorldVolume();
            OnWorldPositionChanged(source);

            // NotifyEntityChange(source);
        }

        /// <summary>
        /// Updates the world matrix (change caused by parent)
        /// </summary>
        public virtual void UpdateWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
        {
            MatrixD.Multiply(ref this.m_localMatrix, ref parentWorldMatrix, out this.m_worldMatrix);
            OnWorldPositionChanged(source);


            //MatrixD oldWorldMatrix = m_worldMatrix;
            //MatrixD.Multiply(ref this.m_localMatrix, ref parentWorldMatrix, out this.m_worldMatrix);
            //SetDirty();
            //return;
            ////parent matrix changed significantly 
            ////if (!m_worldMatrix.EqualsFast(ref oldWorldMatrix))
            //{
            //    OnWorldPositionChanged(source);
            //    //if (this.m_physics != null && this.m_physics.Enabled && this.m_physics != source)
            //    //{
            //    //    this.m_physics.OnWorldPositionChanged(source);
            //    //}
            //    m_normalizedInvMatrixDirty = true;
            //    m_invScaledMatrixDirty = true;
            //}
            //NotifyEntityChange(source);
        }

        /// <summary>
        /// Updates the volume of this entity.
        /// </summary>
        protected virtual void UpdateWorldVolume()
        {
            BoundingBoxD oldWorldAABB = m_worldAABB;

            m_worldAABB = m_localAABB.Transform(ref this.m_worldMatrix);
            MatrixD mat = MatrixD.CreateTranslation((Vector3D)m_localVolume.Center);
            MatrixD.Multiply(ref mat, ref m_worldMatrix, out mat); //mat = mat * this.WorldMatrix;

            m_worldVolume = new BoundingSphereD(mat.Translation, m_localVolume.Radius);

        }

        /// <summary>
        /// Update volume hr and of all children.
        /// </summary>
        /// <param name="volume"></param>
        private void UpdateAABBHr(ref BoundingBoxD volume)
        {
            UpdateWorldVolume();

            BoundingBoxD.CreateMerged(ref m_invalidBox, ref this.m_worldAABB, out m_worldAABBHr);

            m_worldVolumeHr = BoundingSphereD.CreateFromBoundingBox(m_worldAABBHr);

            BoundingBoxD.CreateMerged(ref m_worldAABBHr, ref volume, out volume);
        }

        public void UpdateAABBHr()
        {
            UpdateAABBHr(ref m_invalidBox);
        }

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        protected virtual void OnWorldPositionChanged(object source)
        {
            Debug.Assert(source != this && (Container.Entity == null || source != Container.Entity), "Recursion detected!");

            m_worldVolumeDirty = true;
            m_normalizedInvMatrixDirty = true;
            m_invScaledMatrixDirty = true;

            RaiseOnPositionChanged(this);
        }

        
        #endregion

        public override string ComponentTypeDebugString
        {
            get { return "Position"; }
        }
    }
}

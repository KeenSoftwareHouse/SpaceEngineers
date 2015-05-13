using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using VRageMath;
using VRage.Utils;
using VRageRender.Utils;

namespace VRageRender.Shadows
{
    /// <summary>
    /// Abstract base class for all camera types
    /// </summary>
    public abstract class MyBaseShadowCamera
    {
        protected MatrixD m_viewMatrix = MatrixD.Identity;
        public Matrix ViewMatrixAtZero = Matrix.Identity;
        public Matrix ViewProjMatrixAtZero = Matrix.Identity;
        protected MatrixD m_worldMatrix = MatrixD.Identity;
        protected Matrix m_projectionMatrix = Matrix.Identity;
        protected MatrixD m_unscaledProjectionMatrix = MatrixD.Identity;
        protected MatrixD m_viewProjMatrix = MatrixD.Identity;

        protected BoundingFrustumD m_boundingFrustum;

        protected double m_nearClip;
        protected double m_farClip;

        public void GetWorldMatrix(out MatrixD worldMatrix)
        {
            worldMatrix = this.m_worldMatrix;
        }

        public void SetWorldMatrix(ref MatrixD worldMatrix)
        {
            this.m_worldMatrix = worldMatrix;
            MyUtils.AssertIsValid(m_worldMatrix);
            Update();
        }

        public void GetViewMatrix(out MatrixD viewMatrix)
        {
            viewMatrix = this.m_viewMatrix;
        }

        public void SetViewMatrix(ref MatrixD viewMatrix)
        {
            MyUtils.AssertIsValid(viewMatrix);
            this.m_viewMatrix = viewMatrix;
            MatrixD.Invert(ref viewMatrix, out m_worldMatrix);
            MyUtils.AssertIsValid(m_worldMatrix);
            Update();
        }

        public void GetProjectionMatrix(out Matrix projectionMatrix)
        {
            projectionMatrix = this.m_projectionMatrix;
        }

        public void GetViewProjMatrix(out MatrixD viewProjMatrix)
        {
            viewProjMatrix = this.m_viewProjMatrix;
        }

        public MatrixD WorldMatrix
        {
            get { return m_worldMatrix; }
            set
            {
                m_worldMatrix = value;
                MyUtils.AssertIsValid(m_worldMatrix);
                Update();
            }
        }

        public MatrixD ViewMatrix
        {
            get { return m_viewMatrix; }
            set
            {
                m_viewMatrix = value;
                MyUtils.AssertIsValid(value);
                MatrixD.Invert(ref m_viewMatrix, out m_worldMatrix);
                Update();
            }
        }

        public Matrix ProjectionMatrix
        {
            get { return m_projectionMatrix; }
            set { m_projectionMatrix = value; }
        }

        public MatrixD ViewProjectionMatrix
        {
            get { return m_viewProjMatrix; }
            set { m_viewProjMatrix = value; }
        }

        public virtual double NearClip
        {
            get { return m_nearClip; }
            set { }
        }

        public virtual double FarClip
        {
            get { return m_farClip; }
            set { }
        }

        public Vector3D Position
        {
            get { return m_worldMatrix.Translation; }
            set
            {
                m_worldMatrix.Translation = value;
                Update();
            }
        }

        public BoundingFrustumD BoundingFrustum
        {
            get { return m_boundingFrustum; }
        }

        public Quaternion Orientation
        {
            get
            {
                Quaternion orientation;
                Quaternion.CreateFromRotationMatrix(ref m_worldMatrix, out orientation);
                return orientation;
            }
            set
            {
                Quaternion orientation = value;
                Vector3D position = m_worldMatrix.Translation;
                MatrixD.CreateFromQuaternion(ref orientation, out m_worldMatrix);
                m_worldMatrix.Translation = position;
                Update();
            }
        }

        public Matrix FrustumProjectionMatrix;

        /// <summary>
        /// Base constructor
        /// </summary>
        public MyBaseShadowCamera()
        {
            m_boundingFrustum = new BoundingFrustumD(m_viewProjMatrix);
            m_worldMatrix = MatrixD.Identity;
            m_viewMatrix = MatrixD.Identity;
        }

        /// <summary>
        /// Applies a transform to the camera's world matrix,
        /// with the new transform applied first
        /// </summary>
        /// <param name="transform">The transform to be applied</param>
        public void PreTransform(ref MatrixD transform)
        {
            MatrixD.Multiply(ref transform, ref m_worldMatrix, out m_worldMatrix);
            Update();
        }

        /// <summary>
        /// Applies a transform to the camera's world matrix,
        /// with the new transform applied second
        /// </summary>
        /// <param name="transform">The transform to be applied</param>
        public void PostTransform(ref MatrixD transform)
        {
            MatrixD.Multiply(ref m_worldMatrix, ref transform, out m_worldMatrix);
            Update();
        }

        /// <summary>
        /// Updates the view-projection matrix and frustum coordinates based on
        /// the current camera position/orientation and projection parameters.
        /// </summary>
        protected void Update()
        {
            // Make our view matrix
            MatrixD.Invert(ref m_worldMatrix, out m_viewMatrix);

            ViewMatrixAtZero = Matrix.CreateLookAt(Vector3.Zero, m_worldMatrix.Forward, m_worldMatrix.Up);

            // Create the combined view-projection matrix
            MatrixD.Multiply(ref m_viewMatrix, ref m_projectionMatrix, out m_viewProjMatrix);

            Matrix.Multiply(ref ViewMatrixAtZero, ref m_projectionMatrix, out ViewProjMatrixAtZero);

            // Create the bounding frustum
            m_boundingFrustum.Matrix = m_viewProjMatrix;
        }
    }
}

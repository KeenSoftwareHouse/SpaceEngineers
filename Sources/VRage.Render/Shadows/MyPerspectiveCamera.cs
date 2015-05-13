using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using VRageMath;


namespace VRageRender.Shadows
{
    /// <summary>
    /// Camera using a perspective projection
    /// </summary>
    public class MyPerspectiveCamera : MyBaseShadowCamera
    {
        protected float m_fieldOfView;
        protected float m_aspectRatio;

        public float FieldOfView
        {
            get { return m_fieldOfView; }
            set
            {
                m_fieldOfView = value;
                Matrix.CreatePerspectiveFieldOfView(m_fieldOfView, m_aspectRatio, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public float AspectRatio
        {
            get { return m_aspectRatio; }
            set
            {
                m_aspectRatio = value;
                Matrix.CreatePerspectiveFieldOfView(m_fieldOfView, m_aspectRatio, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public override double NearClip
        {
            get { return m_nearClip; }
            set
            {
                m_nearClip = value;
                Matrix.CreatePerspectiveFieldOfView(m_fieldOfView, m_aspectRatio, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public override double FarClip
        {
            get { return m_farClip; }
            set
            {
                m_farClip = value;
                Matrix.CreatePerspectiveFieldOfView(m_fieldOfView, m_aspectRatio, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public MyPerspectiveCamera()
            : base()
        {
            this.m_fieldOfView = 1.0f;
            this.m_aspectRatio = 1.0f;
            this.m_nearClip = 50;
            this.m_farClip = 100;
        }

        /// <summary>
        /// Creates a camera using a perspective projection
        /// </summary>
        /// <param name="fieldOfView">The vertical field of view</param>
        /// <param name="aspectRatio">Aspect ratio of the projection</param>
        /// <param name="nearClip">Distance to near clipping plane</param>
        /// <param name="farClip">Distance to far clipping plane</param>
        public MyPerspectiveCamera(float fieldOfView, float aspectRatio, float nearClip, float farClip)
            : base()
        {
            this.m_fieldOfView = fieldOfView;
            this.m_aspectRatio = aspectRatio;
            this.m_nearClip = nearClip;
            this.m_farClip = farClip;
            Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearClip, farClip, out m_projectionMatrix);
            Update();
        }

    }

}

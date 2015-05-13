using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using VRageMath;

namespace VRageRender.Shadows
{
    /// <summary>
    /// Camera that uses an orthographic projection
    /// </summary>
    public class MyOrthographicCamera : MyBaseShadowCamera
    {
        double m_width;
        double m_height;

        double m_xMin;
        double m_xMax;
        double m_yMin;
        double m_yMax;

        BoundingFrustumD m_unscaledFrustum = new BoundingFrustumD(MatrixD.Identity);
        Vector3D[] m_unscaledCorners = new Vector3D[8];
        BoundingBoxD m_unscaledBoundingBox = new BoundingBoxD();
        BoundingBoxD m_boundingBox = new BoundingBoxD();
        List<MyRender.MyRenderElement> m_castingRenderElements = new List<MyRender.MyRenderElement>(1024);

        internal List<MyRender.MyRenderElement> CastingRenderElements
        {
            set
            {
                m_castingRenderElements.Clear();
                for (int i = 0; i < value.Count; i++)
                {
                    MyRender.MyRenderElement element = value[i];
                    m_castingRenderElements.Add(element);
                }
            }

            get
            {
                return m_castingRenderElements;
            }
        }


        public double Width
        {
            get { return m_width; }
        }

        public double Height
        {
            get { return m_height; }
        }

        public double XMin
        {
            get { return m_xMin; }
            set
            {
                m_xMin = value;
                m_width = m_xMax - m_xMin;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public double XMax
        {
            get { return m_xMax; }
            set
            {
                m_xMax = value;
                m_width = m_xMax - m_xMin;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public double YMin
        {
            get { return m_xMin; }
            set
            {
                m_yMin = value;
                m_height = m_yMax - m_yMin;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public double YMax
        {
            get { return m_xMin; }
            set
            {
                m_yMax = value;
                m_height = m_yMax - m_yMin;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public override double NearClip
        {
            get { return m_nearClip; }
            set
            {
                m_nearClip = value;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        public override double FarClip
        {
            get { return m_farClip; }
            set
            {
                m_farClip = value;
                Matrix.CreateOrthographicOffCenter((float)m_xMin, (float)m_xMax, (float)m_yMin, (float)m_yMax, (float)m_nearClip, (float)m_farClip, out m_projectionMatrix);
                Update();
            }
        }

        /// <summary>
        /// Creates a camera using an orthographic projection
        /// </summary>
        /// <param name="width">Width of the projection volume</param>
        /// <param name="height">Height of the projection volume</param>
        /// <param name="nearClip">Distance to near clip plane</param>
        /// <param name="farClip">Distance to far clip plane</param>
        public MyOrthographicCamera(float width, float height, float nearClip, float farClip)
            : base()
        {
            this.m_width = width;
            this.m_height = height;
            this.m_nearClip = nearClip;
            this.m_farClip = farClip;
            this.m_xMax = width / 2;
            this.m_yMax = height / 2;
            this.m_xMin = -width / 2;
            this.m_yMin = -height / 2;
            Matrix.CreateOrthographic((float)width, (float)height, (float)nearClip, (float)farClip, out m_projectionMatrix);
            Update();
        }

        public MyOrthographicCamera(float xMin, float xMax, float yMin, float yMax, float nearClip, float farClip)
            : base()
        {
            Update(xMin, xMax, yMin, yMax, nearClip, farClip);
        }

        public void Update(double xMin, double xMax, double yMin, double yMax, double nearClip, double farClip)
        {
            this.m_xMin = xMin;
            this.m_yMin = yMin;
            this.m_xMax = xMax;
            this.m_yMax = yMax;
            this.m_width = xMax - xMin;
            this.m_height = yMax - yMin;
            this.m_nearClip = nearClip;
            this.m_farClip = farClip;
            Matrix.CreateOrthographicOffCenter((float)xMin, (float)xMax, (float)yMin, (float)yMax, (float)nearClip, (float)farClip, out m_projectionMatrix);

            Update();

            Debug.Assert(xMax > xMin && yMax > yMin, "Invalid ortho camera params");
        }

        public void UpdateUnscaled(double xMin, double xMax, double yMin, double yMax, double nearClip, double farClip)
        {
            this.m_xMin = xMin;
            this.m_yMin = yMin;
            this.m_xMax = xMax;
            this.m_yMax = yMax;
            this.m_width = xMax - xMin;
            this.m_height = yMax - yMin;
            this.m_nearClip = nearClip;
            this.m_farClip = farClip;
            MatrixD.CreateOrthographicOffCenter(xMin, xMax, yMin, yMax, nearClip, farClip, out m_unscaledProjectionMatrix);

            Debug.Assert(xMax > xMin && yMax > yMin, "Invalid ortho camera params");
        }


        protected void UpdateUnscaled()
        {
            // Make our view matrix
            MatrixD.Invert(ref m_worldMatrix, out m_viewMatrix);

            // Create the combined view-projection matrix
            MatrixD.Multiply(ref m_viewMatrix, ref m_unscaledProjectionMatrix, out m_viewProjMatrix);

            // Create the bounding frustum
            m_unscaledFrustum.Matrix = m_viewProjMatrix;
        }

        public void SetViewMatrixUnscaled(ref MatrixD viewMatrix)
        {
            this.m_viewMatrix = viewMatrix;
            MatrixD.Invert(ref viewMatrix, out m_worldMatrix);
            UpdateUnscaled();
        }

        public BoundingFrustumD UnscaledBoundingFrustum
        {
            get { return m_unscaledFrustum; }
        }

        public BoundingBoxD UnscaledBoundingBox
        {
            get
            {
                UnscaledBoundingFrustum.GetCorners(m_unscaledCorners);
                m_unscaledBoundingBox = BoundingBoxD.CreateInvalid();
                foreach (var corner in m_unscaledCorners)
                {
                    var cornerInst = corner;
                    m_unscaledBoundingBox = m_unscaledBoundingBox.Include(ref cornerInst);
                }

                return m_unscaledBoundingBox;
            }
        }


        public BoundingBoxD BoundingBox
        {
            get
            {
                BoundingFrustum.GetCorners(m_unscaledCorners);
                m_boundingBox = BoundingBoxD.CreateInvalid();
                foreach (var corner in m_unscaledCorners)
                {
                    var cornerInst = corner;
                    m_boundingBox = m_boundingBox.Include(ref cornerInst);
                }

                return m_boundingBox;
            }
        }
    }
}

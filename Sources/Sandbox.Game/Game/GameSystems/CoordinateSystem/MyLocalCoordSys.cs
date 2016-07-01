using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Library.Utils;
using VRageMath;

namespace Sandbox.Game.GameSystems.CoordinateSystem
{

    /// <summary>
    /// Local coord system.
    /// </summary>
    public class MyLocalCoordSys
    {

        private const float COLOR_ALPHA = 0.4f;

        private const int LOCAL_COORD_SIZE = 1000;
        private const float BBOX_BORDER_THICKNESS_MODIF = 0.0015f;

        /// <summary>
        /// Origin transform.
        /// </summary>
        private MyTransformD m_origin;

        /// <summary>
        /// Bouding box of the coord system.
        /// </summary>
        private MyOrientedBoundingBoxD m_boundingBox;

        /// <summary>
        /// Cached corner of the bbox in world coordinates.
        /// </summary>
        private Vector3D[] m_corners = new Vector3D[8];


        /// <summary>
        /// Gets origin transformation of the coord system.
        /// </summary>
        public MyTransformD Origin { get { return m_origin; } }

        /// <summary>
        /// Indicates how many entities are in this coord system.
        /// </summary>
        public long EntityConuter { get; set; }

        internal MyOrientedBoundingBoxD BoundingBox { get { return m_boundingBox; } }

        /// <summary>
        /// Color of the bounding box.
        /// </summary>
        public Color RenderColor { get; set; }

        /// <summary>
        /// Id if this coord system
        /// </summary>
        public long Id { get; set; }

        public MyLocalCoordSys(int size = LOCAL_COORD_SIZE)
        {
            m_origin = new MyTransformD(MatrixD.Identity);

            float halfSizeFloat = size / 2.0f;
            Vector3 halfSize = new Vector3(halfSizeFloat, halfSizeFloat, halfSizeFloat);
            BoundingBoxD tempBB = new BoundingBoxD(-halfSize, halfSize);
            m_boundingBox = new MyOrientedBoundingBoxD(tempBB, m_origin.TransformMatrix);
            m_boundingBox.GetCorners(m_corners, 0);

            RenderColor = this.GenerateRandomColor();

        }

        public MyLocalCoordSys(MyTransformD origin, int size = LOCAL_COORD_SIZE)
        {
            m_origin = origin;

            Vector3 halfSize = new Vector3(size / 2, size / 2, size / 2);
            BoundingBoxD tempBB = new BoundingBoxD(-halfSize, halfSize);         
            m_boundingBox = new MyOrientedBoundingBoxD(tempBB, m_origin.TransformMatrix);

            m_boundingBox.GetCorners(m_corners, 0);

            RenderColor = this.GenerateRandomColor();
        }

        private Color GenerateRandomColor() 
        {
            // Set random color.
            float r = (MyRandom.Instance.Next(0, 100) / 100.0f) * COLOR_ALPHA;
            float g = (MyRandom.Instance.Next(0, 100) / 100.0f) * COLOR_ALPHA;
            float b = (MyRandom.Instance.Next(0, 100) / 100.0f) * COLOR_ALPHA;

            return new Vector4(r, g, b, COLOR_ALPHA);
        }

        public bool Contains(ref Vector3D vec)
        {
            return this.m_boundingBox.Contains(ref vec);
        }

        public void Draw()
        {
            MatrixD transfromMatrix = this.Origin.TransformMatrix;
            Vector3D min = Vector3D.One;
            Vector3D max = Vector3D.Zero;

            for(int i=0; i < 8; i++)
            {
                Vector3D corner = World.MySector.MainCamera.WorldToScreen(ref m_corners[i]);
                min = Vector3D.Min(min, corner);
                max = Vector3D.Max(max, corner);
            }

            Vector3D distProj = max - min; // distance vec in screen coordinates (from 0 to 1, where 1 is screen width(for X) or height(for Y)
            float lineWidth = BBOX_BORDER_THICKNESS_MODIF / (float)MathHelper.Clamp(distProj.Length(), 0.01, 1);

            Color boxColor = RenderColor;
            
            BoundingBoxD box = new BoundingBoxD(-m_boundingBox.HalfExtent, m_boundingBox.HalfExtent);
            MySimpleObjectDraw.DrawTransparentBox(ref transfromMatrix, ref box, ref boxColor, MySimpleObjectRasterizer.SolidAndWireframe, 1, lineWidth, "Square", "Square");

            if (Engine.Utils.MyFakes.ENABLE_DEBUG_DRAW_COORD_SYS)
            {
                //x
                for (int i = -10; i < 11; i++)
                {
                    Vector3D v1 = this.Origin.Position + transfromMatrix.Forward * 20 + transfromMatrix.Right * (i * 2.5);
                    Vector3D v2 = this.Origin.Position - transfromMatrix.Forward * 20 + transfromMatrix.Right * (i * 2.5);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(v1, v2, boxColor, boxColor, false);
                }
                //y
                for (int i = -10; i < 11; i++)
                {
                    Vector3D v1 = this.Origin.Position + transfromMatrix.Right * 20 + transfromMatrix.Forward * (i * 2.5);
                    Vector3D v2 = this.Origin.Position - transfromMatrix.Right * 20 + transfromMatrix.Forward * (i * 2.5);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(v1, v2, boxColor, boxColor, false);
                }
            }
        }
    }
}

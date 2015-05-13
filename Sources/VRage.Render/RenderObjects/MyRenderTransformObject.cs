#region Using Statements

using System.Collections.Generic;
using VRageMath;
using System;
using ParallelTasks;
using VRage.Utils;
using VRageRender.Shadows;
using VRage.Import;
using System.Diagnostics;
using VRageRender.Textures;
using VRageRender.Utils;
using VRage;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{
    class MyRenderTransformObject : MyRenderObject
    {
        protected BoundingBoxD m_localAABB = BoundingBoxD.CreateInvalid();
        protected BoundingSphereD m_localVolume;
        protected Vector3D m_localVolumeOffset;

        protected MatrixD m_worldMatrix = MatrixD.Identity;

        private MyInterpolationQueue<MatrixD> m_interpolation = new MyInterpolationQueue<MatrixD>(3, MatrixD.SlerpScale);

        public MyRenderTransformObject(uint id, string debugName,
            MatrixD worldMatrix, RenderFlags renderFlags)
            : base(id, debugName, renderFlags)
        {
            WorldMatrix = worldMatrix;
        }

        public void ClearInterpolator()
        {
            m_interpolation.Clear();
        }

        public virtual MatrixD WorldMatrix
        {
            get
            {
                if (ParentCullObject != null)
                    return MatrixD.Multiply(m_worldMatrix, ParentCullObject.WorldMatrix);
                return m_worldMatrix;
            }
            set
            {
                MyRender.AddAndInterpolateObjectMatrix(m_interpolation, ref value);

                if (ParentCullObject != null)
                {
                    m_worldMatrix = value * ParentCullObject.InvWorldMatrix;
                }
                else
                    m_worldMatrix = value;

                Flags |= MyElementFlag.EF_AABB_DIRTY;
            }
        }

        public override BoundingSphereD WorldVolume
        {
            get
            {
                if (ParentCullObject != null)
                {
                    var worldMatrix = WorldMatrix;
                    var aabb = m_localAABB.Transform(ref worldMatrix);
                    BoundingSphereD.CreateFromBoundingBox(ref aabb, out m_volume);
                }

                return m_volume;
            }
        }

        /// <summary>
        /// Gets the world matrix for draw.
        /// </summary>
        /// <returns></returns>
        public virtual MatrixD GetWorldMatrixForDraw()
        {
            MatrixD outMatrix;
            var inMatrix = WorldMatrix;

            MatrixD.Multiply(ref inMatrix, ref MyRenderCamera.InversePositionTranslationMatrix, out outMatrix);

            return outMatrix;
        }

        public override void UpdateWorldAABB()
        {
            m_aabb = m_localAABB.Transform(ref m_worldMatrix);

            base.UpdateWorldAABB();
        }

        public unsafe override void GetCorners(Vector3D* corners)
        {
            LocalAABB.GetCornersUnsafe(corners);
            for (int i = 0; i < 8; i++)
            {
                corners[i] = Vector3D.Transform(corners[i], WorldMatrix);
            }
        }

        public BoundingBoxD LocalAABB
        {
            get { return m_localAABB; }
        }


        /// <summary>
        /// Debug draw box of this entity.
        /// </summary>
        [Conditional("DEBUG"), Conditional("DEVELOP")]
        public virtual void DebugDrawOBB()
        {
            var boundingBoxSize = m_localAABB.Size;

            const float alpha = 1.0f;
            MyDebugDraw.DrawHiresBoxWireframe(MatrixD.CreateScale(boundingBoxSize) * MatrixD.CreateTranslation(m_localVolumeOffset) * WorldMatrix, Color.DarkRed.ToVector3(), alpha, false);
        }


        #region Intersection Methods

        //  Calculates intersection of line with object.
        public override bool GetIntersectionWithLine(ref LineD line)
        {
            var t = m_aabb.Intersects(new RayD(line.From, line.Direction));
            if (t.HasValue && t.Value < line.Length && t.Value > 0)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}

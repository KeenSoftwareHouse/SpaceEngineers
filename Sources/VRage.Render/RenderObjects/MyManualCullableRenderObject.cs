#region Using Statements

using System.Collections.Generic;
using VRageMath;
using VRage.Utils;
using System;
using VRage;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{
    internal class MyManualCullableRenderObject : MyCullableRenderObject
    {
        List<MyElement> m_list = new List<MyElement>();
        BoundingBoxD m_localAABB;

        MatrixD m_worldMatrix;
        MatrixD m_invWorldMatrix;

        MyInterpolationQueue<MatrixD> m_interpolation = new MyInterpolationQueue<MatrixD>(3, MatrixD.SlerpScale);

        public void ClearInterpolator()
        {
            m_interpolation.Clear();
        }

        public MatrixD WorldMatrix
        {
            get { return m_worldMatrix; }
            set
            {
                MyRender.AddAndInterpolateObjectMatrix(m_interpolation, ref value);

                m_worldMatrix = value;
                m_invWorldMatrix = MatrixD.Invert(m_worldMatrix);
                SetDirty();
            }
        }

        public MatrixD InvWorldMatrix
        {
            get { return m_invWorldMatrix; }
        }


        public MyManualCullableRenderObject(uint id, MatrixD worldMatrix) : base(id, "ManualCullObject") 
        {
            WorldMatrix = worldMatrix;
        }

        public override void UpdateWorldAABB()
        {
            CulledObjects.GetAll(m_list, true);

            m_localAABB = BoundingBoxD.CreateInvalid();
            

            foreach (var element in m_list)
            {
                m_localAABB = m_localAABB.Include(element.WorldAABB);
            }

            m_aabb = m_localAABB.Transform(ref m_worldMatrix);

            base.UpdateWorldAABB();
        }

        public void AddRenderObject(MyRenderObject renderObject, MatrixD? childToParent = null)
        {
            MyRenderEntity renderEntity = renderObject as MyRenderEntity;
            MatrixD childMatrix;
            if (childToParent != null)
                childMatrix = childToParent.Value;
            else
                childMatrix = renderEntity.WorldMatrix * m_invWorldMatrix;

            renderEntity.WorldMatrix = childMatrix;
            renderObject.ParentCullObject = this;

            var box = renderObject.WorldAABB;
            renderObject.ProxyData = CulledObjects.AddProxy(ref box, renderObject, 0);

            EntitiesContained++;

            SetDirty();
        }

        public void RemoveRenderObject(MyRenderObject renderObject)
        {
            MyRenderEntity renderEntity = renderObject as MyRenderEntity;

            CulledObjects.RemoveProxy(renderObject.ProxyData);
            renderObject.ParentCullObject = null;
            renderEntity.WorldMatrix = renderEntity.WorldMatrix * WorldMatrix;
            renderObject.ProxyData = MyElement.PROXY_UNASSIGNED;

            EntitiesContained--;

            SetDirty();
        }

        public void MoveRenderObject(MyRenderObject renderObject)
        {
            MyRenderEntity renderEntity = renderObject as MyRenderEntity;

            BoundingBoxD aabb = renderEntity.WorldAABB;

            CulledObjects.MoveProxy(renderObject.ProxyData, ref aabb, Vector3D.Zero);

            SetDirty();
        }

        /// <summary>
        /// Gets the world matrix for draw.
        /// </summary>
        /// <returns></returns>
        public virtual MatrixD GetWorldMatrixForDraw()
        {
            MatrixD outMatrix;
            MatrixD inMatrix = WorldMatrix;

            MatrixD.Multiply(ref inMatrix, ref MyRenderCamera.InversePositionTranslationMatrix, out outMatrix);

            return outMatrix;
        }

    }
}

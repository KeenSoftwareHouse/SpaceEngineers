#define DYNAMIC_CHARACTER_CONTROLLER

#region Using

using System.Diagnostics;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Linq;
using System.Collections.Generic;

using VRageRender;
using Sandbox.AppCode.Game;
using Sandbox.Game.Utils;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Character;
using VRage;

using VRageMath.Spatial;
using Sandbox.Game;
using VRage.Profiler;

#endregion

namespace Sandbox.Engine.Physics
{
    //using MyHavokCluster = MyClusterTree<HkWorld>;
    using MyHavokCluster = MyClusterTree;
    using Sandbox.ModAPI;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.Multiplayer;
    using VRage.Library.Utils;
    using System;
    using Sandbox.Definitions;
    using VRage.ModAPI;
    using VRage.Game.Components;
    using VRage.Trace;

    /// <summary>
    /// Abstract engine physics body object.
    /// </summary>
    public partial class MyPhysicsBody
    {

        #region Welding

        public class MyWeldInfo
        {
            public MyPhysicsBody Parent = null;
            public Matrix Transform = Matrix.Identity;
            /// <summary>
            /// This does NOT contain all welded bodies, see @MyWeldGroupData
            /// </summary>
            public readonly HashSet<MyPhysicsBody> Children = new HashSet<MyPhysicsBody>();
            public HkMassElement MassElement;

            internal void UpdateMassProps(HkRigidBody rb)
            {
                var mp = new HkMassProperties();
                mp.InertiaTensor = rb.InertiaTensor;
                mp.Mass = rb.Mass;
                mp.CenterOfMass = rb.CenterOfMassLocal;
                MassElement = new HkMassElement();
                MassElement.Properties = mp;
                MassElement.Tranform = Transform;
                //MassElement.Tranform.Translation = Vector3.Transform(rb.CenterOfMassLocal, Transform);
            }

            internal void SetMassProps(HkMassProperties mp)
            {
                MassElement = new HkMassElement();
                MassElement.Properties = mp;
                MassElement.Tranform = Transform;
            }
        }

        public bool IsWelded { get { return WeldInfo.Parent != null; } }

        public readonly MyWeldInfo WeldInfo = new MyWeldInfo();

        public void Weld(MyPhysicsComponentBase other, bool recreateShape = true)
        {
            Weld(other as MyPhysicsBody, recreateShape);
        }

        public void Weld(MyPhysicsBody other, bool recreateShape = true)
        {
            if (other.WeldInfo.Parent == this) //already welded to this
                return;

            if (other.IsWelded && !IsWelded)
            {
                other.Weld(this);
                return;
            }

            if (IsWelded)
            {
                WeldInfo.Parent.Weld(other);
                return;
            }
            if(other.WeldInfo.Children.Count > 0)
            {
                Debug.Fail("Welding other welded gorup");
                other.UnweldAll(false); //they should end in same group and get welded
            }
            ProfilerShort.Begin("Weld");
            HkShape thisShape;
            bool firstWelded = WeldInfo.Children.Count == 0;
            if (firstWelded)
            {
                //RemoveConstraints(RigidBody);
                WeldedRigidBody = RigidBody;
                thisShape = RigidBody.GetShape();
                if (HavokWorld != null)
                    HavokWorld.RemoveRigidBody(WeldedRigidBody);
                RigidBody = HkRigidBody.Clone(WeldedRigidBody);
                if (HavokWorld != null)
                    HavokWorld.AddRigidBody(RigidBody);
                HkShape.SetUserData(thisShape, RigidBody);
                Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
                WeldInfo.UpdateMassProps(RigidBody);
                //Entity.OnClose += Entity_OnClose;
            }
            else
                thisShape = GetShape();

            other.Deactivate();

            var transform = other.Entity.WorldMatrix*Entity.WorldMatrixInvScaled;
            other.WeldInfo.Transform = transform;
            other.WeldInfo.UpdateMassProps(other.RigidBody);
            Debug.Assert(other.WeldedRigidBody == null);
            other.WeldedRigidBody = other.RigidBody;
            other.RigidBody = RigidBody;
            other.WeldInfo.Parent = this;
            other.ClusterObjectID = ClusterObjectID;
            WeldInfo.Children.Add(other);

            //if(recreateShape)
            //    RecreateWeldedShape(thisShape);

            ProfilerShort.BeginNextBlock("OnPhysicsChanged");
            //(other.Entity as MyEntity).RaisePhysicsChanged();
            //other.Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
            //Debug.Assert(other.m_constraints.Count == 0, "Constraints left in welded body");
            ProfilerShort.BeginNextBlock("RemoveConstraints");
            ProfilerShort.End();
            OnWelded(other);
            other.OnWelded(this);
        }

        void Entity_OnClose(IMyEntity obj)
        {
            UnweldAll(true);
        }

        void WeldedEntity_OnPhysicsChanged(IMyEntity obj)
        {
            if (Entity == null || Entity.Physics == null)
                return;
            foreach (var child in WeldInfo.Children)
            {
                if (child.Entity == null) //Physics component was replaced
                {
                    child.WeldInfo.Parent = null;
                    WeldInfo.Children.Remove(child);
                    if (obj.Physics != null)
                        Weld(obj.Physics as MyPhysicsBody);
                    break;
                }
            }
            //this breaks welded MP computation since bodys inertia tensor is diagonalized
            //(obj as MyEntity).GetPhysicsBody().WeldInfo.UpdateMassProps((obj as MyEntity).GetPhysicsBody().WeldedRigidBody);

            RecreateWeldedShape(GetShape());
        }

        public void RecreateWeldedShape()
        {
            //Debug.Assert(WeldInfo.Children.Count > 0);
            if (WeldInfo.Children.Count == 0)
                return;
            RecreateWeldedShape(GetShape());
        }

        private List<HkMassElement> m_tmpElements = new List<HkMassElement>();

        public void UpdateMassProps()
        {
            Debug.Assert(m_tmpElements.Count == 0, "mass elements not cleared!");
            if (RigidBody.IsFixedOrKeyframed)
                return;
            if (WeldInfo.Parent != null)
            {
                WeldInfo.Parent.UpdateMassProps();
                return;
            }
            m_tmpElements.Add(WeldInfo.MassElement);
            foreach (var child in WeldInfo.Children)
            {
                m_tmpElements.Add(child.WeldInfo.MassElement);
            }
            var mp = HkInertiaTensorComputer.CombineMassProperties(m_tmpElements);
            RigidBody.SetMassProperties(ref mp);
            m_tmpElements.Clear();
        }

        private void RecreateWeldedShape(HkShape thisShape)
        {
            if (RigidBody == null || RigidBody.IsDisposed)
            {
                Debug.Fail("Missing rigid body");
                MyTrace.Send(TraceWindow.Analytics, "Recreating welded shape without RB", string.Format("{0},{1}", Entity.MarkedForClose, WeldInfo.Children.Count));
                return;
            }

            ProfilerShort.Begin("RecreateWeldedShape");
            //me.Tranform.Translation = Entity.PositionComp.LocalAABB.Center;

            if (WeldInfo.Children.Count == 0)
            {
                RigidBody.SetShape(thisShape);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(thisShape);
            }
            else
            {
                ProfilerShort.Begin("Create shapes");
                //m_tmpElements.Add(WeldInfo.MassElement);
                m_tmpShapeList.Add(thisShape);
                foreach (var child in WeldInfo.Children)
                {
                    var transformShape = new HkTransformShape(child.WeldedRigidBody.GetShape(), ref child.WeldInfo.Transform);
                    HkShape.SetUserData(transformShape, child.WeldedRigidBody);
                    m_tmpShapeList.Add(transformShape);

                    //TODO:this saves from crash but disables the collision of excessive entities
                    if(m_tmpShapeList.Count == HkSmartListShape.MaxChildren)
                        break;
                    //m_tmpElements.Add(child.WeldInfo.MassElement);
                }
                //var list = new HkListShape(m_tmpShapeList.ToArray(), HkReferencePolicy.None);
                var list = new HkSmartListShape(0);
                foreach (var shape in m_tmpShapeList)
                    list.AddShape(shape);
                RigidBody.SetShape(list);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(list);
                list.Base.RemoveReference();

                WeldedMarkBreakable();

                for (int i = 1; i < m_tmpShapeList.Count; i++)
                    m_tmpShapeList[i].RemoveReference();
                m_tmpShapeList.Clear();
                ProfilerShort.End();

                ProfilerShort.Begin("CalcMassProps");
                UpdateMassProps();
                //m_tmpElements.Clear();
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        private void WeldedMarkBreakable()
        {
            if (HavokWorld == null)
                return;
            MyGridPhysics gp = this as MyGridPhysics;
            if (gp != null && (gp.Entity as MyCubeGrid).BlocksDestructionEnabled)
            {
                HavokWorld.BreakOffPartsUtil.MarkPieceBreakable(RigidBody, 0, gp.Shape.BreakImpulse);
            }

            uint shapeKey = 1;
            foreach (var child in WeldInfo.Children)
            {
                gp = child as MyGridPhysics;
                if (gp != null && (gp.Entity as MyCubeGrid).BlocksDestructionEnabled)
                    HavokWorld.BreakOffPartsUtil.MarkPieceBreakable(RigidBody, shapeKey, gp.Shape.BreakImpulse);
                shapeKey++;
            }
        }

        public void UnweldAll(bool insertInWorld)
        {
            while (WeldInfo.Children.Count > 1)
                Unweld(WeldInfo.Children.First(), insertInWorld, false);
            if (WeldInfo.Children.Count > 0)
                Unweld(WeldInfo.Children.First(), insertInWorld);
        }

        private List<HkShape> m_tmpShapeList = new List<HkShape>();
        public void Unweld(MyPhysicsBody other, bool insertToWorld = true, bool recreateShape = true)
        {
            Debug.Assert(other.IsWelded && RigidBody != null && other.WeldedRigidBody != null, "Invalid welding state!");
            
            if (IsWelded)
            {
                WeldInfo.Parent.Unweld(other, insertToWorld, recreateShape);
                Debug.Assert(other.IsWelded);
                return;
            }

            if (other.IsInWorld || RigidBody == null || other.WeldedRigidBody == null)
            {
                WeldInfo.Children.Remove(other);
                return;
            }
            var rbWorldMatrix = RigidBody.GetRigidBodyMatrix();
            //other.Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;

            other.WeldInfo.Parent = null;
            Debug.Assert(WeldInfo.Children.Contains(other));
            WeldInfo.Children.Remove(other);

            var body = other.RigidBody;
            Debug.Assert(body == RigidBody);
            other.RigidBody = other.WeldedRigidBody;
            other.WeldedRigidBody = null;
            if (!other.RigidBody.IsDisposed)
            {
                other.RigidBody.SetWorldMatrix(other.WeldInfo.Transform * rbWorldMatrix);
                other.RigidBody.LinearVelocity = body.LinearVelocity;
                other.WeldInfo.MassElement.Tranform = Matrix.Identity;
                other.WeldInfo.Transform = Matrix.Identity;
            }
            else
            {
                Debug.Fail("Disposed welded body");
            }
            //RemoveConstraints(other.RigidBody);

            other.ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;
            if (insertToWorld)
            {
                other.Activate();
                other.OnMotion(other.RigidBody, 0);
            }

            if (WeldInfo.Children.Count == 0)
            {
                recreateShape = false;
                Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;
                Entity.OnClose -= Entity_OnClose;
                WeldedRigidBody.LinearVelocity = RigidBody.LinearVelocity;
                WeldedRigidBody.AngularVelocity = RigidBody.AngularVelocity;
                if (HavokWorld != null)
                    HavokWorld.RemoveRigidBody(RigidBody);
                RigidBody.Dispose();
                RigidBody = WeldedRigidBody;
                WeldedRigidBody = null;
                RigidBody.SetWorldMatrix(rbWorldMatrix);
                WeldInfo.Transform = Matrix.Identity;
                if (HavokWorld != null)
                    HavokWorld.AddRigidBody(RigidBody);
                else if (!Entity.MarkedForClose)
                    Activate();

                if (RigidBody2 != null)
                    RigidBody2.SetShape(RigidBody.GetShape());
            }
            if (RigidBody != null && recreateShape)
                RecreateWeldedShape(GetShape());
            OnUnwelded(other);
            other.OnUnwelded(this);
            Debug.Assert(!other.IsWelded);
        }

        public void Unweld(bool insertInWorld = true)
        {
            Debug.Assert(WeldInfo.Parent != null);
            WeldInfo.Parent.Unweld(this, insertInWorld);
            Debug.Assert(!IsWelded);
        }

        public HkRigidBody WeldedRigidBody { get; protected set; }

        protected virtual void OnWelded(MyPhysicsBody other)
        {

        }

        protected virtual void OnUnwelded(MyPhysicsBody other)
        {

        }

        private void RemoveConstraints(HkRigidBody hkRigidBody)
        {
            foreach (var constraint in m_constraints)
            {
                if (constraint.IsDisposed || (constraint.RigidBodyA == hkRigidBody || constraint.RigidBodyB == hkRigidBody))
                    m_constraintsRemoveBatch.Add(constraint);
            }
            foreach (var constraint in m_constraintsRemoveBatch)
            {
                m_constraints.Remove(constraint);
                if (!constraint.IsDisposed && constraint.InWorld)
                {
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyA), "Object was removed prior to constraint");
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyB), "Object was removed prior to constraint");
                    HavokWorld.RemoveConstraint(constraint);
                }
            }
            m_constraintsRemoveBatch.Clear();
        }
        #endregion

    }
}
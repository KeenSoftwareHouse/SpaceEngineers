using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using Sandbox.Common.Components;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyEntity : IMyEntity
    {
        public EntityFlags Flags { get; set; }
        IMyEntity IMyEntity.Parent
        {
            get { return Parent; }
        }

        IMyEntity IMyEntity.GetTopMostParent(Type type)
        {
            return GetTopMostParent(type);
        }

        void IMyEntity.GetChildren(List<IMyEntity> children, Func<IMyEntity, bool> collect)
        {
            foreach (var entity in children)
                if (collect == null || collect(entity))
                    children.Add(entity);
        }

        string IMyEntity.Name
        {
            get { return Name; }
            set { Name = value; }
        }

        Action<MyEntity> GetDelegate(Action<IMyEntity> value)
        {
            return (Action<MyEntity>)Delegate.CreateDelegate(typeof(Action<MyEntity>), value.Target, value.Method);
        }

        event Action<IMyEntity> IMyEntity.OnClose
        {
            add { OnClose += GetDelegate(value); }
            remove { OnClose -= GetDelegate(value); }
        }

        event Action<IMyEntity> IMyEntity.OnClosing
        {
            add { OnClosing += GetDelegate(value); }
            remove { OnClosing -= GetDelegate(value); }
        }

        event Action<IMyEntity> IMyEntity.OnMarkForClose
        {
            add { OnMarkForClose += GetDelegate(value); }
            remove { OnMarkForClose -= GetDelegate(value); }
        }

        event Action<IMyEntity> IMyEntity.OnPhysicsChanged
        {
            add { OnPhysicsChanged += GetDelegate(value); }
            remove { OnPhysicsChanged -= GetDelegate(value); }
        }

        long IMyEntity.EntityId
        {
            get
            {
                return EntityId;
            }
            set
            {
                EntityId = value;
            }
        }

        string IMyEntity.DisplayName
        {
            get
            {
                return DisplayName;
            }
            set
            {
                DisplayName = value;
            }
        }

        string IMyEntity.GetFriendlyName()
        {
            return GetFriendlyName();
        }

        void IMyEntity.Close()
        {
            Close();
        }

        bool IMyEntity.MarkedForClose
        {
            get { return MarkedForClose; }
        }

        void IMyEntity.Delete()
        {
            Delete();
        }

        bool IMyEntity.Closed
        {
            get { return Closed; }
        }

        MyEntityComponentBase IMyEntity.GameLogic
        {
            get
            {
                return GameLogic;
            }
            set
            {
                GameLogic = (MyGameLogicComponent)value;
            }
        }

        MyEntityUpdateEnum IMyEntity.NeedsUpdate
        {
            get
            {
                return NeedsUpdate;
            }
            set
            {
                NeedsUpdate = value;
            }
        }

        bool IMyEntity.NearFlag
        {
            get
            {
                return this.Render.NearFlag;
            }
            set
            {
                Render.NearFlag = value;
            }
        }

        bool IMyEntity.CastShadows
        {
            get
            {
                return Render.CastShadows;
            }
            set
            {
                Render.CastShadows = value;
            }
        }

        bool IMyEntity.FastCastShadowResolve
        {
            get
            {
                return Render.FastCastShadowResolve;
            }
            set
            {
                Render.FastCastShadowResolve = value;
            }
        }

        bool IMyEntity.NeedsResolveCastShadow
        {
            get
            {
                return Render.NeedsResolveCastShadow;
            }
            set
            {
                Render.NeedsResolveCastShadow = value;
            }
        }

        VRageMath.Vector3 IMyEntity.GetDiffuseColor()
        {
            return Render.GetDiffuseColor();
        }

        float IMyEntity.MaxGlassDistSq
        {
            get { return MaxGlassDistSq; }
        }

        bool IMyEntity.NeedsDraw
        {
            get
            {
                return Render.NeedsDraw;
            }
            set
            {
                Render.NeedsDraw = value;
            }
        }

        bool IMyEntity.NeedsDrawFromParent
        {
            get
            {
                return Render.NeedsDrawFromParent;
            }
            set
            {
                Render.NeedsDrawFromParent = value;
            }
        }

        bool IMyEntity.Transparent
        {
            get
            {
                return Render.Transparency != 0f;
            }
            set
            {
                Render.Transparency = Sandbox.Engine.Utils.MyGridConstants.BUILDER_TRANSPARENCY;
            }
        }

        bool IMyEntity.ShadowBoxLod
        {
            get
            {
                return Render.ShadowBoxLod;
            }
            set
            {
                Render.ShadowBoxLod = value;
            }
        }

        bool IMyEntity.SkipIfTooSmall
        {
            get
            {
                return Render.SkipIfTooSmall;
            }
            set
            {
                Render.SkipIfTooSmall = value;
            }
        }

        bool IMyEntity.Visible
        {
            get
            {
                return Render.Visible;
            }
            set
            {
                Render.Visible = value;
            }
        }

        float IMyEntity.GetDistanceBetweenCameraAndBoundingSphere()
        {
            return (float)GetDistanceBetweenCameraAndBoundingSphere();
        }

        float IMyEntity.GetDistanceBetweenCameraAndPosition()
        {
            return (float)GetDistanceBetweenCameraAndPosition();
        }

        float IMyEntity.GetLargestDistanceBetweenCameraAndBoundingSphere()
        {
            return (float)GetLargestDistanceBetweenCameraAndBoundingSphere();
        }

        float IMyEntity.GetSmallestDistanceBetweenCameraAndBoundingSphere()
        {
            return (float)GetSmallestDistanceBetweenCameraAndBoundingSphere();
        }

        VRageMath.Vector3? IMyEntity.GetIntersectionWithLineAndBoundingSphere(ref VRageMath.LineD line, float boundingSphereRadiusMultiplier)
        {
            return GetIntersectionWithLineAndBoundingSphere(ref line, boundingSphereRadiusMultiplier);
        }

        bool IMyEntity.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere)
        {
            return GetIntersectionWithSphere(ref sphere);
        }

        void IMyEntity.GetTrianglesIntersectingSphere(ref VRageMath.BoundingSphereD sphere, VRageMath.Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles)
        {
            GetTrianglesIntersectingSphere(ref sphere, referenceNormalVector, maxAngle, retTriangles, maxNeighbourTriangles);
        }

        bool IMyEntity.DoOverlapSphereTest(float sphereRadius, VRageMath.Vector3D spherePos)
        {
            return DoOverlapSphereTest(sphereRadius, spherePos);
        }

        MyObjectBuilder_EntityBase IMyEntity.GetObjectBuilder(bool copy)
        {
            return GetObjectBuilder(copy);
        }

        bool IMyEntity.Save
        {
            get
            {
                return Save;
            }
            set
            {
                Save = value;
            }
        }

        MyPersistentEntityFlags2 IMyEntity.PersistentFlags
        {
            get
            {
                return Render.PersistentFlags;
            }
            set
            {
                Render.PersistentFlags = value;
            }
        }

        bool IMyEntity.InScene
        {
            get { return InScene; }
            set { InScene = value; }
        }

        bool IMyEntity.InvalidateOnMove
        {
            get { return InvalidateOnMove; }
        }

        bool IMyEntity.IsCCDForProjectiles
        {
            get { return IsCCDForProjectiles; }
        }

        bool IMyEntity.IsVisible()
        {
            return Render.IsVisible();
        }

        bool IMyEntity.IsVolumetric
        {
            get { return IsVolumetric; }
        }

        VRageMath.MatrixD IMyEntity.GetViewMatrix()
        {
            return GetViewMatrix();
        }

        VRageMath.MatrixD IMyEntity.GetWorldMatrixNormalizedInv()
        {
            return PositionComp.GetWorldMatrixNormalizedInv();
        }

        VRageMath.BoundingBox IMyEntity.LocalAABB
        {
            get
            {
                return PositionComp.LocalAABB;
            }
            set
            {
                PositionComp.LocalAABB = value;
            }
        }

        VRageMath.BoundingBox IMyEntity.LocalAABBHr
        {
            get { return PositionComp.LocalAABBHr; }
        }

        VRageMath.Matrix IMyEntity.LocalMatrix
        {
            get
            {
                return PositionComp.LocalMatrix;
            }
            set
            {
                PositionComp.LocalMatrix = value;
            }
        }

        VRageMath.BoundingSphere IMyEntity.LocalVolume
        {
            get
            {
                return PositionComp.LocalVolume;
            }
            set
            {
                PositionComp.LocalVolume = value;
            }
        }

        VRageMath.Vector3 IMyEntity.LocalVolumeOffset
        {
            get
            {
                return PositionComp.LocalVolumeOffset;
            }
            set
            {
                PositionComp.LocalVolumeOffset = value;
            }
        }

        VRageMath.Vector3 IMyEntity.LocationForHudMarker
        {
            get { return LocationForHudMarker; }
        }

        void IMyEntity.SetLocalMatrix(VRageMath.Matrix localMatrix, object source)
        {
            PositionComp.SetLocalMatrix(localMatrix, source);
        }

        void IMyEntity.SetWorldMatrix(VRageMath.MatrixD worldMatrix, object source)
        {
            PositionComp.SetWorldMatrix(worldMatrix, source);
        }

        VRageMath.BoundingBoxD IMyEntity.WorldAABB
        {
            get { return PositionComp.WorldAABB; }
        }

        VRageMath.BoundingBoxD IMyEntity.WorldAABBHr
        {
            get { return PositionComp.WorldAABBHr; }
        }

        VRageMath.MatrixD IMyEntity.WorldMatrix
        {
            get
            {
                return PositionComp.WorldMatrix;
            }
            set
            {
                PositionComp.WorldMatrix = value;
            }
        }

        VRageMath.MatrixD IMyEntity.WorldMatrixInvScaled
        {
            get { return PositionComp.WorldMatrixInvScaled; }
        }

        VRageMath.MatrixD IMyEntity.WorldMatrixNormalizedInv
        {
            get { return PositionComp.WorldMatrixNormalizedInv; }
        }

        VRageMath.BoundingSphereD IMyEntity.WorldVolume
        {
            get { return PositionComp.WorldVolume; }
        }

        VRageMath.BoundingSphereD IMyEntity.WorldVolumeHr
        {
            get { return PositionComp.WorldVolumeHr; }
        }

        VRageMath.Vector3D IMyEntity.GetPosition()
        {
            return PositionComp.GetPosition();
        }

        void IMyEntity.SetPosition(VRageMath.Vector3D pos)
        {
            PositionComp.SetPosition(pos);
        }

        void IMyEntity.EnableColorMaskForSubparts(bool value)
        {
            if (Subparts != null)
            {
                foreach (var subPart in Subparts)
                {
                    subPart.Value.Render.EnableColorMaskHsv = value;
                }
            }
        }

        void IMyEntity.SetColorMaskForSubparts(VRageMath.Vector3 colorMaskHsv)
        {
            if (Subparts != null)
            {
                foreach (var subPart in Subparts)
                {
                    subPart.Value.Render.ColorMaskHsv = colorMaskHsv;
                }
            }
        }
    }
}

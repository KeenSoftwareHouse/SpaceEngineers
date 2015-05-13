using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public sealed class MySyncVoxel : MySyncEntity
    {
        [MessageId(13, P2PMessageEnum.Reliable)]
        struct VoxelCutoutMsg : IEntityMessage
        {
            public long EntityId;
            public Vector3D Center;
            public float Radius;
            public BoolBlit CreateDebris;

            public long GetEntityId() { return EntityId; }

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(36, P2PMessageEnum.Reliable)]
        struct MeteorCraterMsg : IEntityMessage
        {
            public Vector3D Center;
            public float Radius;
            public byte Material;
            public Vector3 Normal;
            public long EntityId;

            public long GetEntityId() { return EntityId; }
        }

        public enum PaintType:byte
        {
            Fill,
            Paint,
            Cut
        }

        [MessageIdAttribute(16282, P2PMessageEnum.Reliable)]
        struct PaintSphereMessage : IEntityMessage
        {
            public float Radius;
            public Vector3D Center;
            public byte Material;
            public long EntityId;
            public PaintType Type;

            public long GetEntityId() { return EntityId; }
        }

        [MessageIdAttribute(16283, P2PMessageEnum.Reliable)]
        struct PaintBoxMessage : IEntityMessage
        {
            public MatrixD Transformation;
            public Vector3D Min;
            public Vector3D Max;
            public byte Material;
            public long EntityId;
            public PaintType Type;

            public long GetEntityId() { return EntityId; }
        }

        [MessageIdAttribute(16284, P2PMessageEnum.Reliable)]
        struct PaintRampMessage : IEntityMessage
        {
            public MatrixD Transformation;
            public Vector3D Min;
            public Vector3D Max;
            public Vector3D RampNormal;
            public double RampNormalW;

            public byte Material;
            public long EntityId;
            public PaintType Type;

            public long GetEntityId() { return EntityId; }
        }

        [MessageIdAttribute(16285, P2PMessageEnum.Reliable)]
        struct PaintCapsuleMessage : IEntityMessage
        {
            public MatrixD Transformation;
            public Vector3D A;
            public Vector3D B;
            public float Radius;

            public byte Material;
            public long EntityId;
            public PaintType Type;

            public long GetEntityId() { return EntityId; }
        }

        [MessageIdAttribute(16297, P2PMessageEnum.Reliable)]
        struct PaintEllipsoidMessage : IEntityMessage
        {
            public MatrixD Transformation;
            public Vector3 Radius;

            public byte Material;
            public long EntityId;
            public PaintType Type;

            public long GetEntityId() { return EntityId; }
        }

        static MyShapeSphere m_sphereShape = new MyShapeSphere();
        static MyShapeBox m_boxShape = new MyShapeBox();
        static MyShapeRamp m_rampShape = new MyShapeRamp();
        static MyShapeCapsule m_capsuleShape = new MyShapeCapsule();
        static MyShapeEllipsoid m_ellipsoidShape = new MyShapeEllipsoid();

        static MySyncVoxel()
        {
            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintCapsuleMessage>(VoxelPaintCapsuleRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintCapsuleMessage>(VoxelPaintCapsuleSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintRampMessage>(VoxelPaintRampRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintRampMessage>(VoxelPaintRampSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintBoxMessage>(VoxelPaintBoxRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintBoxMessage>(VoxelPaintBoxSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintSphereMessage>(VoxelPaintSphereRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel,PaintSphereMessage>(VoxelPaintSphereSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintEllipsoidMessage>(VoxelPaintEllipsoidRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel, PaintEllipsoidMessage>(VoxelPaintEllipsoidSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncVoxel,VoxelCutoutMsg>(VoxelCutoutSphereSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncVoxel,MeteorCraterMsg>(VoxelMeteorCraterSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncVoxel(MyVoxelBase voxel)
                :base(voxel)
        {
        }

        public new MyVoxelBase Entity
        { get { return (MyVoxelBase)base.Entity; } }

        public void RequestVoxelCutoutSphere(Vector3D center, float radius, bool createDebris)
        {
            if (Sync.IsServer)
            {
                var msg = new VoxelCutoutMsg();
                msg.EntityId = Entity.EntityId;
                msg.Center = center;
                msg.Radius = radius;
                msg.CreateDebris = createDebris;
               
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void VoxelCutoutSphereSuccess(MySyncVoxel sync, ref VoxelCutoutMsg msg, MyNetworkClient sender)
        {
            MyExplosion.CutOutVoxelMap(msg.Radius, msg.Center, sync.Entity, msg.CreateDebris && MySession.Ready);
        }

        public void CreateVoxelMeteorCrater(Vector3D center, float radius, Vector3 normal, MyVoxelMaterialDefinition material)
        {
            var msg = new MeteorCraterMsg();
            msg.EntityId = Entity.EntityId;
            msg.Center = center;
            msg.Radius = radius;
            msg.Normal = normal;
            msg.Material = material.Index;

            MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void VoxelMeteorCraterSuccess(MySyncVoxel sync, ref MeteorCraterMsg msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                MyVoxelGenerator.MakeCrater(voxel, new BoundingSphere(msg.Center, msg.Radius), msg.Normal, MyDefinitionManager.Static.GetVoxelMaterialDefinition(msg.Material));
            }
        }

        public void RequestVoxelPaintSphere(Vector3D center, float radius, byte material,PaintType Type)
        {
            var msg = new PaintSphereMessage();
            msg.EntityId = Entity.EntityId;
            msg.Center = center;
            msg.Radius = radius;
            msg.Type = Type;
            msg.Material = material;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void VoxelPaintSphereSuccess(MySyncVoxel sync, ref PaintSphereMessage msg, MyNetworkClient sender)
        {
            m_sphereShape.Center = msg.Center;
            m_sphereShape.Radius = msg.Radius;
            var amountChanged = UpdateVoxelShape(sync, msg.Type, m_sphereShape, msg.Material);
            if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
        }

        static void VoxelPaintSphereRequest(MySyncVoxel sync, ref PaintSphereMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_sphereShape.Center = msg.Center;
                m_sphereShape.Radius = msg.Radius;           
                if (CanPlaceInArea(msg.Type, m_sphereShape))
                {
                    var amountChanged = UpdateVoxelShape(sync, msg.Type, m_sphereShape, msg.Material);
                    if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        public void RequestVoxelPaintBox(BoundingBoxD box,MatrixD Transformation, byte material, PaintType Type)
        {
            var msg = new PaintBoxMessage();
            msg.EntityId = Entity.EntityId;
            msg.Min = box.Min;
            msg.Max = box.Max;
            msg.Type = Type;
            msg.Material = material;
            msg.Transformation = Transformation;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void VoxelPaintBoxSuccess(MySyncVoxel sync, ref PaintBoxMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_boxShape.Transformation = msg.Transformation;
                m_boxShape.Boundaries.Max = msg.Max;
                m_boxShape.Boundaries.Min = msg.Min;
                var amountChanged = UpdateVoxelShape(sync, msg.Type, m_boxShape, msg.Material);
                if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                    MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        static void VoxelPaintBoxRequest(MySyncVoxel sync, ref PaintBoxMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_boxShape.Transformation = msg.Transformation;
                m_boxShape.Boundaries.Max = msg.Max;
                m_boxShape.Boundaries.Min = msg.Min;
               
                if (CanPlaceInArea(msg.Type, m_boxShape))
                {
                    var amountChanged = UpdateVoxelShape(sync, msg.Type, m_boxShape, msg.Material);
                    if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        public void RequestVoxelPaintRamp(BoundingBoxD box, Vector3D rampNormal,double rampNormalW, MatrixD Transformation, byte material, PaintType Type)
        {
            var msg = new PaintRampMessage();
            msg.EntityId = Entity.EntityId;
            msg.Min = box.Min;
            msg.Max = box.Max;
            msg.RampNormal = rampNormal;
            msg.RampNormalW = rampNormalW;
            msg.Type = Type;
            msg.Material = material;
            msg.Transformation = Transformation;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void VoxelPaintRampSuccess(MySyncVoxel sync, ref PaintRampMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_rampShape.Transformation = msg.Transformation;
                m_rampShape.Boundaries.Max = msg.Max;
                m_rampShape.Boundaries.Min = msg.Min;
                m_rampShape.RampNormal = msg.RampNormal;
                m_rampShape.RampNormalW = msg.RampNormalW;
                var amountChanged = UpdateVoxelShape(sync, msg.Type, m_rampShape, msg.Material);
                if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                    MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        static void VoxelPaintRampRequest(MySyncVoxel sync, ref PaintRampMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_rampShape.Transformation = msg.Transformation;
                m_rampShape.Boundaries.Max = msg.Max;
                m_rampShape.Boundaries.Min = msg.Min;
                m_rampShape.RampNormal = msg.RampNormal;
                m_rampShape.RampNormalW = msg.RampNormalW;
                
                if (CanPlaceInArea(msg.Type, m_rampShape))
                {
                    var amountChanged = UpdateVoxelShape(sync, msg.Type, m_rampShape, msg.Material);
                    if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        public void RequestVoxelPaintCapsule(Vector3D A, Vector3D B,float radius, MatrixD Transformation, byte material, PaintType Type)
        {
            var msg = new PaintCapsuleMessage();
            msg.EntityId = Entity.EntityId;
            msg.A = A;
            msg.B = B;
            msg.Radius = radius;
            msg.Type = Type;
            msg.Material = material;
            msg.Transformation = Transformation;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void VoxelPaintCapsuleSuccess(MySyncVoxel sync, ref PaintCapsuleMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_capsuleShape.Transformation = msg.Transformation;
                m_capsuleShape.A = msg.A;
                m_capsuleShape.B = msg.B;
                m_capsuleShape.Radius = msg.Radius;
                var amountChanged = UpdateVoxelShape(sync, msg.Type, m_capsuleShape, msg.Material);
                if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                    MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        static void VoxelPaintCapsuleRequest(MySyncVoxel sync, ref PaintCapsuleMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelBase;
            if (voxel != null)
            {
                m_capsuleShape.Transformation = msg.Transformation;
                m_capsuleShape.A = msg.A;
                m_capsuleShape.B = msg.B;
                m_capsuleShape.Radius = msg.Radius;
                if (CanPlaceInArea(msg.Type, m_capsuleShape))
                {
                    var amountChanged = UpdateVoxelShape(sync, msg.Type, m_capsuleShape, msg.Material);
                    if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        public void RequestVoxelPaintEllipsoid(Vector3 radius, MatrixD Transformation, byte material, PaintType Type)
        {
            var msg = new PaintEllipsoidMessage();
            msg.EntityId = Entity.EntityId;
            msg.Radius = radius;
            msg.Type = Type;
            msg.Material = material;
            msg.Transformation = Transformation;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void VoxelPaintEllipsoidSuccess(MySyncVoxel sync, ref PaintEllipsoidMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelMap;
            if (voxel != null)
            {
                m_ellipsoidShape.Transformation = msg.Transformation;
                m_ellipsoidShape.Radius = msg.Radius;
                var amountChanged = UpdateVoxelShape(sync, msg.Type, m_ellipsoidShape, msg.Material);
                if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                    MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        static void VoxelPaintEllipsoidRequest(MySyncVoxel sync, ref PaintEllipsoidMessage msg, MyNetworkClient sender)
        {
            var voxel = sync.Entity as MyVoxelMap;
            if (voxel != null)
            {
                m_ellipsoidShape.Transformation = msg.Transformation;
                m_ellipsoidShape.Radius = msg.Radius;
                if (CanPlaceInArea(msg.Type, m_ellipsoidShape))
                {
                    var amountChanged = UpdateVoxelShape(sync, msg.Type, m_ellipsoidShape, msg.Material);
                    if (msg.Type == PaintType.Cut || msg.Type == PaintType.Fill)
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }


        static List<MyEntity> m_foundElements = new List<MyEntity>();

        private static bool CanPlaceInArea(PaintType type, MyShape Shape)
        {
            if (type == PaintType.Fill)
            { 
                m_foundElements.Clear();
                BoundingBoxD box = Shape.GetWorldBoundaries();
                MyEntities.GetElementsInBox(ref box, m_foundElements);
                foreach (var entity in m_foundElements)
                {
                    if (IsForbiddenEntity(entity))
                    {
                        if (entity.PositionComp.WorldAABB.Intersects(box))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool IsForbiddenEntity(MyEntity entity)
        {
           return  (entity is MyCharacter ||
                       (entity is MyCubeGrid && (entity as MyCubeGrid).IsStatic == false) ||
                       (entity is MyCockpit && (entity as MyCockpit).Pilot != null));
        }

        private static ulong UpdateVoxelShape(MySyncVoxel sync, PaintType type, MyShape Shape, byte Material)
        {
            var voxel = sync.Entity as MyVoxelBase;
            ulong changedVoxelAmount = 0;
            if (voxel != null)
            {
                switch (type)
                {
                    case PaintType.Paint:
                        MyVoxelGenerator.PaintInShape(voxel, Shape, Material);
                        break;
                    case PaintType.Fill:
                        changedVoxelAmount = MyVoxelGenerator.FillInShape(voxel, Shape, Material);
                        break;
                    case PaintType.Cut:
                        changedVoxelAmount = MyVoxelGenerator.CutOutShape(voxel, Shape);
                        break;
                }
            }

            return changedVoxelAmount;
        }     
    }
}

#region Using

using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Components;
using Sandbox.Game.EntityComponents;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public static class MySyncDestructions
    {
        [MessageId(3247, P2PMessageEnum.Unreliable)]
        struct AddDestructionEffectMsg
        {
            public Vector3D Position;
            public Vector3 Direction;
            public float Scale;
            public MyParticleEffectsIDEnum EffectId;
        }

        [MessageId(3248, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct CreateFracturePieceMsg
        {
            [ProtoMember]
            public MyObjectBuilder_FracturedPiece FracturePiece;

            public override string ToString()
            {
                return "CreateFracturePieceMsg: " + FracturePiece.EntityId;
            }
        }

        [MessageId(3249, P2PMessageEnum.Reliable)]
        struct RemoveFracturePieceMsg
        {
            public long EntityId;
            public float BlendTime;

            public override string ToString()
            {
                return "RemoveFracturePieceMsg: " + EntityId + ", " + BlendTime;
            }
        }

        [MessageId(3250, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct CreateFractureBlockMsg
        {
            [ProtoMember]
            public MyObjectBuilder_FracturedBlock FracturedBlock;

            [ProtoMember]
            public long Grid;

            [ProtoMember]
            public Vector3I Position;
        }

        [MessageId(3253, P2PMessageEnum.Reliable)]
        struct FPManagerDbgMsg
        {
            public long CreatedId;
            public long RemovedId;
        }

        [MessageId(3254, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct CreateFractureComponentMsg
        {
            [ProtoMember]
            public MyObjectBuilder_FractureComponentBase FractureComponent;

            [ProtoMember]
            public long Grid;

            [ProtoMember]
            public Vector3I Position;

            [ProtoMember]
            public ushort CompoundBlockId;
        }

        [MessageId(3265, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct RemoveShapeFromFractureComponentMsg
        {
            [ProtoMember]
            public long Grid;

            [ProtoMember]
            public Vector3I Position;

            [ProtoMember]
            public ushort CompoundBlockId;

            [ProtoMember]
            public string[] ShapeNames;
        }


        static MySyncDestructions()
        {
            //MySyncLayer.RegisterMessage<EnableGeneratorsMsg>(OnEnableGeneratorsMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<AddDestructionEffectMsg>(OnAddDestructionEffectMessage, MyMessagePermissions.ToServer|MyMessagePermissions.FromServer|MyMessagePermissions.ToSelf, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateFracturePieceMsg>(OnCreateFracturePieceMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveFracturePieceMsg>(OnRemoveFracturePieceMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateFractureBlockMsg>(OnCreateFracturedBlockMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateFractureComponentMsg>(OnCreateFractureComponentMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveShapeFromFractureComponentMsg>(OnRemoveShapeFromFractureComponentMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);

            MySyncLayer.RegisterMessage<FPManagerDbgMsg>(OnFPManagerDbgMessage, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
        }


        public static void AddDestructionEffect(MyParticleEffectsIDEnum effectId, Vector3D position, Vector3 direction, float scale)
        {
            AddDestructionEffectMsg msg = new AddDestructionEffectMsg();
            msg.EffectId = effectId;
            msg.Position = position;
            msg.Direction = direction;
            msg.Scale = scale;
            MySession.Static.SyncLayer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnAddDestructionEffectMessage(ref AddDestructionEffectMsg msg, MyNetworkClient sender)
        {
            MyGridPhysics.CreateDestructionEffect(msg.EffectId, msg.Position, msg.Direction, msg.Scale);
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public static void CreateFracturePiece(MyObjectBuilder_FracturedPiece fracturePiece)
        {
            Debug.Assert(Sync.IsServer);
            var msg = new CreateFracturePieceMsg();
            msg.FracturePiece = fracturePiece;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnCreateFracturePieceMessage(ref CreateFracturePieceMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            var fracturedPiece = MyFracturedPiecesManager.Static.GetPieceFromPool(msg.FracturePiece.EntityId, true);
            Debug.Assert(msg.FracturePiece.EntityId != 0, "Fracture piece without ID");
            try
            {
                fracturedPiece.Init(msg.FracturePiece);
                MyEntities.Add(fracturedPiece);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Cannot add fracture piece: " + e.Message);
                if (fracturedPiece == null)
                    return;

                MyFracturedPiecesManager.Static.RemoveFracturePiece(fracturedPiece, 0, true, false);
                StringBuilder sb = new StringBuilder();
                foreach(var shape in msg.FracturePiece.Shapes)
                {
                    sb.Append(shape.Name).Append(" ");
                }
                Debug.Fail("Recieved fracture piece not added");
                MyLog.Default.WriteLine("Received fracture piece not added, no shape found. Shapes: " + sb.ToString());
            }
        }

        public static void RemoveFracturePiece(long entityId, float blendTime)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new RemoveFracturePieceMsg();
            msg.EntityId = entityId;
            msg.BlendTime = blendTime;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnRemoveFracturePieceMessage(ref RemoveFracturePieceMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyFracturedPiece fracturePiece;
            if (MyEntities.TryGetEntityById(msg.EntityId, out fracturePiece))
            {
                MyFracturedPiecesManager.Static.RemoveFracturePiece(fracturePiece, msg.BlendTime, fromServer: true, sync: false);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Not existing fracture piece");
            }
        }

        [Conditional("DEBUG")]
        public static void FPManagerDbgMessage(long createdId, long removedId)
        {
            var msg = new FPManagerDbgMsg();
            msg.CreatedId = createdId;
            msg.RemovedId = removedId;

            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        static void OnFPManagerDbgMessage(ref FPManagerDbgMsg msg, MyNetworkClient sender)
        {
            MyFracturedPiecesManager.Static.DbgCheck(msg.CreatedId, msg.RemovedId);
        }

        public static void CreateFracturedBlock(MyObjectBuilder_FracturedBlock fracturedBlock, long grid, Vector3I position)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new CreateFractureBlockMsg();
            msg.FracturedBlock = fracturedBlock;
            msg.Grid = grid;
            msg.Position = position;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnCreateFracturedBlockMessage(ref CreateFractureBlockMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyCubeGrid grid;
            if (MyEntities.TryGetEntityById(msg.Grid, out grid))
            {
                grid.EnableGenerators(false, true);
                grid.CreateFracturedBlock(msg.FracturedBlock, msg.Position);
                grid.EnableGenerators(true, true);
            }
        }

        public static void CreateFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, MyObjectBuilder_FractureComponentBase component)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(component.Shapes != null && component.Shapes.Count > 0);

            var msg = new CreateFractureComponentMsg();
            msg.Grid = gridId;
            msg.Position = position;
            msg.CompoundBlockId = compoundBlockId;
            msg.FractureComponent = component;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnCreateFractureComponentMessage(ref CreateFractureComponentMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.Grid, out entity))
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                var cubeBlock = grid.GetCubeBlock(msg.Position);
                if (cubeBlock != null && cubeBlock.FatBlock != null)
                {
                    var compound = cubeBlock.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        var blockInCompound = compound.GetBlock(msg.CompoundBlockId);
                        if (blockInCompound != null)
                            AddFractureComponent(msg.FractureComponent, blockInCompound.FatBlock);
                    }
                    else
                    {
                        AddFractureComponent(msg.FractureComponent, cubeBlock.FatBlock);
                    }
                }
            }
        }

        private static void AddFractureComponent(MyObjectBuilder_FractureComponentBase obFractureComponent, MyEntity entity)
        {
            var component = MyComponentFactory.CreateInstance(obFractureComponent.GetType());
            var fractureComponent = component as MyFractureComponentBase;
            Debug.Assert(fractureComponent != null);
            if (fractureComponent != null)
            {
                bool hasComponent = entity.Components.Has<MyFractureComponentBase>();
                Debug.Assert(!hasComponent);
                if (!hasComponent)
                {
                    entity.Components.Add<MyFractureComponentBase>(fractureComponent);
                    fractureComponent.Deserialize(obFractureComponent);
                }
            }
        }

        public static void RemoveShapeFromFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, string shapeName)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(shapeName != null && !string.IsNullOrEmpty(shapeName));

            var msg = new RemoveShapeFromFractureComponentMsg();
            msg.Grid = gridId;
            msg.Position = position;
            msg.CompoundBlockId = compoundBlockId;
            msg.ShapeNames = new string[] { shapeName };
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        public static void RemoveShapesFromFractureComponent(long gridId, Vector3I position, ushort compoundBlockId, List<string> shapeNames)
        {
            Debug.Assert(Sync.IsServer);
            Debug.Assert(gridId != 0);
            Debug.Assert(shapeNames != null && shapeNames.Count > 0);

            var msg = new RemoveShapeFromFractureComponentMsg();
            msg.Grid = gridId;
            msg.Position = position;
            msg.CompoundBlockId = compoundBlockId;
            msg.ShapeNames = shapeNames.ToArray();
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnRemoveShapeFromFractureComponentMessage(ref RemoveShapeFromFractureComponentMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            Debug.Assert(msg.ShapeNames != null && msg.ShapeNames.Length > 0);

            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.Grid, out entity))
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                var cubeBlock = grid.GetCubeBlock(msg.Position);
                if (cubeBlock != null && cubeBlock.FatBlock != null)
                {
                    var compound = cubeBlock.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        var blockInCompound = compound.GetBlock(msg.CompoundBlockId);
                        if (blockInCompound != null)
                        {
                            var component = blockInCompound.GetFractureComponent();
                            if (component != null)
                                component.RemoveChildShapes(msg.ShapeNames);
                        }
                    }
                    else
                    {
                        var component = cubeBlock.GetFractureComponent();
                        if (component != null)
                            component.RemoveChildShapes(msg.ShapeNames);
                    }
                }
            }
        }
    }
}

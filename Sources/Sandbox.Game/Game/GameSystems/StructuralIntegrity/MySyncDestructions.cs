#region Using

using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Serializer;
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

        [MessageId(3251, P2PMessageEnum.Reliable)]
        struct RemoveEnvironmentItemMsg
        {
            public long EntityId;
            public int ItemInstanceId;
        }

        [MessageId(3253, P2PMessageEnum.Reliable)]
        struct FPManagerDbgMsg
        {
            public long CreatedId;
            public long RemovedId;
        }

        public static Action<MyEntity, int> OnRemoveEnvironmentItem;

        static MySyncDestructions()
        {
            //MySyncLayer.RegisterMessage<EnableGeneratorsMsg>(OnEnableGeneratorsMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<AddDestructionEffectMsg>(OnAddDestructionEffectMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateFracturePieceMsg>(OnCreateFracturePieceMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveFracturePieceMsg>(OnRemoveFracturePieceMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<CreateFractureBlockMsg>(OnCreateFracturedBlockMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<RemoveEnvironmentItemMsg>(OnRemoveEnvironmentItemMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);

            MySyncLayer.RegisterMessage<FPManagerDbgMsg>(OnFPManagerDbgMessage, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
        }


        public static void AddDestructionEffect(MyParticleEffectsIDEnum effectId, Vector3D position, Vector3 direction, float scale)
        {
            AddDestructionEffectMsg msg = new AddDestructionEffectMsg();
            msg.EffectId = effectId;
            msg.Position = position;
            msg.Direction = direction;
            msg.Scale = scale;
            MySession.Static.SyncLayer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnAddDestructionEffectMessage(ref AddDestructionEffectMsg msg, MyNetworkClient sender)
        {
            MyGridPhysics.CreateDestructionEffect(msg.EffectId, msg.Position, msg.Direction, msg.Scale);
        }

        public static void CreateFracturePiece(MyObjectBuilder_FracturedPiece fracturePiece)
        {
            var msg = new CreateFracturePieceMsg();
            msg.FracturePiece = fracturePiece;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnCreateFracturePieceMessage(ref CreateFracturePieceMsg msg, MyNetworkClient sender)
        {
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
            var msg = new RemoveFracturePieceMsg();
            msg.EntityId = entityId;
            msg.BlendTime = blendTime;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnRemoveFracturePieceMessage(ref RemoveFracturePieceMsg msg, MyNetworkClient sender)
        {
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
            var msg = new CreateFractureBlockMsg();
            msg.FracturedBlock = fracturedBlock;
            msg.Grid = grid;
            msg.Position = position;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnCreateFracturedBlockMessage(ref CreateFractureBlockMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid grid;
            if (MyEntities.TryGetEntityById(msg.Grid, out grid))
            {
                grid.EnableGenerators(false, true);
                grid.CreateFracturedBlock(msg.FracturedBlock, msg.Position);
                grid.EnableGenerators(true, true);
            }
        }

        public static void RemoveEnvironmentItem(long entityId, int itemInstanceId)
        {
            var msg = new RemoveEnvironmentItemMsg();
            msg.EntityId = entityId;
            msg.ItemInstanceId = itemInstanceId;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnRemoveEnvironmentItemMessage(ref RemoveEnvironmentItemMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                if (OnRemoveEnvironmentItem != null)
                    OnRemoveEnvironmentItem(entity, msg.ItemInstanceId);
            }
        }
    }
}

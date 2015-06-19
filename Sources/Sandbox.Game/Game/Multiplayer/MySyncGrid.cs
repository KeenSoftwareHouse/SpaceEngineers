using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRage;
using VRage.Serialization;
using VRage.Trace;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Common.ObjectBuilders;
using VRageMath.PackedVector;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    delegate void BuildBlocksAreaRequestDelegate(ref MyCubeGrid.MyBlockBuildArea area, long ownerId);
    delegate void BuildBlocksAreaSuccessDelegate(ref MyCubeGrid.MyBlockBuildArea area, int entityIdSeed, HashSet<Vector3UByte> resultFailList, long ownerId);

    delegate void RazeBlocksAreaRequestDelegate(ref Vector3I pos, ref Vector3UByte size);
    delegate void RazeBlocksAreaSuccessDelegate(ref Vector3I pos, ref Vector3UByte size, HashSet<Vector3UByte> resultFailList);

    delegate void BuildBlocksDelegate(Vector3 colorMaskHsv, HashSet<MyCubeGrid.MyBlockLocation> locations, HashSet<MyCubeGrid.MyBlockLocation> resultBlocks, MyEntity builder);
    delegate void AfterBuildBlocksDelegate(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks);
    delegate void BuildBlockDelegate(Vector3 colorMaskHsv, MyCubeGrid.MyBlockLocation location, MyObjectBuilder_CubeBlock objectBuilder, ref MyCubeGrid.MyBlockLocation? resultBlock, MyEntity builder);
    delegate void AfterBuildBlockDelegate(MyCubeGrid.MyBlockLocation builtBlock);
    delegate void RazeBlockDelegate(List<Vector3I> blocksToRemove, List<Vector3I> removedBlocks);
    delegate void RazeBlockInCompoundDelegate(List<Tuple<Vector3I, ushort>> blocksToRemove, List<Tuple<Vector3I, ushort>> removedBlocks);
    delegate void ColorBlocksDelegate(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound);
    delegate void RemoveBlockDelegate(List<Vector3I> blocksToRemove);
    delegate void MultipleStateDelegate(MyMultipleEnabledEnum enabled);
    delegate void PowerChangedStateDelegate(MyMultipleEnabledEnum enabled, long playerId);
    delegate void BlockIntegrityChangedDelegate(Vector3I pos, ushort subBlockId, float buildIntegrity, float integrity, MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, long toolOwner);
    delegate void BlockStockpileChangedDelegate(Vector3I pos, ushort subBlockId, List<MyStockpileItem> diff);

    [PreloadRequired]
    partial class MySyncGrid : MySyncEntity
    {
        [ProtoContract]
        [MessageId(14, P2PMessageEnum.Reliable)]
        struct BuildBlocksMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            //[ProtoMember]
            //public SerializableDefinitionId DefinitionId;

            [ProtoMember]
            public HashSet<MyCubeGrid.MyBlockLocation> Locations;

            [ProtoMember]
            public uint ColorMaskHsv;

            [ProtoMember]
            public long BuilderEntityId;
        }

        [MessageId(4711, P2PMessageEnum.Reliable)]
        struct BuildBlocksAreaRequestMsg : IEntityMessage
        {
            // Total: 50 B
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
            public MyCubeGrid.MyBlockBuildArea Area; // 34B
            public long OwnerId; // 8B
        }

        [MessageId(4712, P2PMessageEnum.Reliable)]
        struct BuildBlocksAreaSuccessMsg : IEntityMessage
        {
            // Total: 56B + 3B x size of fail list
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
            public MyCubeGrid.MyBlockBuildArea Area; // 34B
            public long OwnerId;
            public int EntityIdSeed;
            public HashSet<Vector3UByte> FailList; // 2B + ?x 3B
        }

        [MessageId(4713, P2PMessageEnum.Reliable)]
        struct RazeBlocksAreaRequestMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3I Pos;
            public Vector3UByte Size;
        }

        [MessageId(4714, P2PMessageEnum.Reliable)]
        struct RazeBlocksAreaSuccessMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3I Pos;
            public Vector3UByte Size;
            public HashSet<Vector3UByte> FailList; // 2B + ?x 3B
        }

        [ProtoContract]
        [MessageId(4715, P2PMessageEnum.Reliable)]
        struct BuildBlockMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public MyCubeGrid.MyBlockLocation Location;

            [ProtoMember]
            public uint ColorMaskHsv;

            [ProtoMember]
            public MyObjectBuilder_CubeBlock BlockObjectBuilder;

            [ProtoMember]
            public long BuilderEntityId;
        }

        [MessageId(15, P2PMessageEnum.Reliable)]
        struct ColorBlocksMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3I Min;
            public Vector3I Max;
            public uint HSV;
            public BoolBlit PlaySound;
        }

        [MessageId(16, P2PMessageEnum.Reliable)]
        struct BonesMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3I MinBone;
            public Vector3I MaxBone;
            public List<byte> Bones;

            public override string ToString()
            {
                return base.ToString() + ", bone count: " + (Bones != null ? Bones.Count : 0);
            }
        }

        [MessageId(29, P2PMessageEnum.Reliable)]
        struct BonesMultiplyMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3I Location;

            public float Multiplier;
        }

        [ProtoContract]
        [MessageId(17, P2PMessageEnum.Reliable)]
        struct RazeBlocksMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public List<Vector3I> Locations;
        }

        [ProtoContract]
        [MessageId(18, P2PMessageEnum.Reliable)]
        struct RemoveBlocksMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public List<Vector3I> LocationsWithGenerator;

            [ProtoMember]
            public List<Vector3I> DestroyLocations;

            [ProtoMember]
            public List<Vector3I> DestructionDeformationLocations;

            [ProtoMember]
            public List<Vector3I> LocationsWithoutGenerator;


            private int LocationWithGeneratorCount { get { return LocationsWithGenerator != null ? LocationsWithGenerator.Count : 0; } }
            private int LocationWithoutGeneratorCount { get { return LocationsWithoutGenerator != null ? LocationsWithoutGenerator.Count : 0; } }
            private int DestroyCount { get { return DestroyLocations != null ? DestroyLocations.Count : 0; } }
            private int DeformationCount { get { return DestructionDeformationLocations != null ? DestructionDeformationLocations.Count : 0; } }

            public override string ToString()
            {
                return base.ToString() + String.Format(", {0}, {1}, {2}, {3}", LocationWithGeneratorCount, LocationWithoutGeneratorCount, DestroyCount, DeformationCount);
            }
        }

        [MessageId(19, P2PMessageEnum.Reliable)]
        struct ReflectorsStateMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public MyMultipleEnabledEnum Enabled;
        }

        [MessageId(25, P2PMessageEnum.Reliable)]
        struct IntegrityChangedMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
            public Vector3I BlockPosition;
            public float BuildIntegrity;
            public float Integrity;
            public MyCubeGrid.MyIntegrityChangeEnum IntegrityChangeType;
            public long ToolOwner;
			public ushort SubBlockId;
        }

        [ProtoContract]
        [MessageId(26, P2PMessageEnum.Reliable)]
        public struct StockpileChangedMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

			[ProtoMember]
			public ushort SubBlockId;

            [ProtoMember]
            public Vector3I BlockPosition;

            [ProtoMember]
            public List<MyStockpileItem> Changes;
        }

        [ProtoContract]
        [MessageId(27, P2PMessageEnum.Reliable)]
        public struct StockpileFillRequestMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public Vector3I BlockPosition;

            [ProtoMember]
            public long OwnerEntityId;

            [ProtoMember]
            public byte InventoryIndex;
        }

        [ProtoContract]
        [MessageId(28, P2PMessageEnum.Reliable)]
        public struct SetToConstructionRequestMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public Vector3I BlockPosition;

            [ProtoMember]
            public long OwnerEntityId;

            [ProtoMember]
            public byte InventoryIndex;

            [ProtoMember]
            public long RequestingPlayer;
        }

        [MessageId(15271, P2PMessageEnum.Reliable)]
        struct PowerProducerStateMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
            public long PlayerId;

            public MyMultipleEnabledEnum Enabled;
        }

        [MessageId(15262, SteamSDK.P2PMessageEnum.Unreliable)]
        struct ThrustAndTorqueMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3 Thrust;
            public Vector3 Torque;

            // For sync interpolation
            public ushort ClientFrameId;
        }

        [MessageId(11212, SteamSDK.P2PMessageEnum.Unreliable)]
        struct ThrustMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public Vector3B ThrustData;

            public Vector3 Thrust
            {
                get { return new Vector3(ThrustData.X, ThrustData.Y, ThrustData.Z) / 127.0f; }
                set { ThrustData = Vector3B.Round(value * 127.0f); }
            }
        }

        [MessageId(11218, P2PMessageEnum.Reliable)]
        struct ThrustZeroMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
        }

        [ProtoContract]
        [MessageId(15263, P2PMessageEnum.Reliable)]
        struct ModifyBlockGroupMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public String Name;
            [ProtoMember]
            public long[] Blocks;
        }

        [ProtoContract]
        [MessageId(15264, P2PMessageEnum.Reliable)]
        struct ConvertToShipMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }
        }

        [ProtoContract]
        [MessageId(15265, P2PMessageEnum.Reliable)]
        struct MergeMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public long OtherEntityId;

            [ProtoMember]
            public SerializableVector3I GridOffset;

            [ProtoMember]
            public Base6Directions.Direction GridForward;

            [ProtoMember]
            public Base6Directions.Direction GridUp;
        }

        [ProtoContract]
        [MessageId(15266, P2PMessageEnum.Reliable)]
        struct ChangeOwnershipMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public long BlockId;

            [ProtoMember]
            public long Owner; //PlayerId

            [ProtoMember]
            public long RequestingPlayer; // PlayerId

            [ProtoMember]
            public MyOwnershipShareModeEnum ShareMode;
        }

        [ProtoContract]
        public struct MySingleOwnershipRequest
        {
            [ProtoMember]
            public long BlockId;

            [ProtoMember]
            public long Owner; //PlayerId

            public MySingleOwnershipRequest(long blockId, long owner)
            {
                BlockId = blockId;
                Owner = owner;
            }
        }

        [ProtoContract]
        [MessageId(15166, P2PMessageEnum.Reliable)]
        struct ChangeOwnershipsMsg
        {
            [ProtoMember]
            public long RequestingPlayer; // PlayerId

            [ProtoMember]
            public MyOwnershipShareModeEnum ShareMode;

            [ProtoMember]
            public List<MySingleOwnershipRequest> Requests;
        }

        [MessageId(15267, P2PMessageEnum.Reliable)]
        struct ChangeGridOwnershipMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public long Owner;
            public MyOwnershipShareModeEnum ShareMode;
        }

        [MessageId(15275, P2PMessageEnum.Reliable)]
        struct SetHandbrakeMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public BoolBlit Handbrake;
        }

        [ProtoContract]
        [MessageId(15279, P2PMessageEnum.Reliable)]
        struct ChangeDisplayNameMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public String DisplayName;
        }

        // CH: TODO: Create a better serializer (e.g. compress the list of grid positions)
        [ProtoContract]
        [MessageId(15280, P2PMessageEnum.Reliable)]
        struct CreateSplitMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public long NewGridEntityId;

            [ProtoMember]
            public List<Vector3I> SplitBlocks;
        }

        [ProtoContract]
        [MessageId(15281, P2PMessageEnum.Reliable)]
        struct RemoveSplitMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public List<Vector3I> RemovedBlocks;
        }

        [ProtoContract]
        [MessageId(15282, P2PMessageEnum.Reliable)]
        struct RazeBlockInCompoundBlockMsg : IEntityMessage
        {
            [ProtoMember]
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            [ProtoMember]
            public List<Tuple<Vector3I, ushort>> LocationsAndIds;
        }

        [MessageId(15283, P2PMessageEnum.Reliable)]
        struct ChangeDestructibleBlocksMsg : IEntityMessage
        {
            public long GridEntityId;
            public long GetEntityId() { return GridEntityId; }

            public BoolBlit DestructionEnabled;
        }

        static HashSet<MyCubeGrid.MyBlockLocation> m_tmpBuildList = new HashSet<MyCubeGrid.MyBlockLocation>();

        static List<Vector3I> m_tmpPositionListSend = new List<Vector3I>(1);
        static List<Vector3I> m_tmpPositionListReceive = new List<Vector3I>();
        static List<MySlimBlock> m_tmpBlockListReceive = new List<MySlimBlock>();
        static List<Tuple<Vector3I, ushort>> m_tmpLocationsAndIdsReceive = new List<Tuple<Vector3I, ushort>>();
        static List<MyDisconnectHelper.Group> m_tmpGroupsReceive = new List<MyDisconnectHelper.Group>();

        static List<byte> m_boneByteList = new List<byte>();

        static MySyncGrid()
        {
            MySyncLayer.RegisterEntityMessage<MySyncGrid, BuildBlocksMsg>(OnBuildBlocksRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, BuildBlocksAreaRequestMsg>(OnBuildBlocksAreaRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, BuildBlocksAreaSuccessMsg>(OnBuildBlocksAreaSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new BuildBlocksAreaSuccessSerializer());
            MySyncLayer.RegisterEntityMessage<MySyncGrid, RazeBlocksMsg>(OnRazeBlocksRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, RazeBlockInCompoundBlockMsg>(OnRazeBlockInCompoundBlockRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, BuildBlockMsg>(OnBuildBlockRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, RazeBlocksAreaRequestMsg>(OnRazeBlocksAreaRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, RazeBlocksAreaSuccessMsg>(OnRazeBlocksAreaSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new RazeBlocksAreaSuccessSerializer());

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ColorBlocksMsg>(OnColorBlocksRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, BonesMsg>(OnBonesReceived, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new BonesMsgSerializer());
            MySyncLayer.RegisterEntityMessage<MySyncGrid, BonesMultiplyMsg>(OnBonesMultiplied, MyMessagePermissions.FromServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, RemoveBlocksMsg>(OnRemoveBlocks, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new MySyncGridRemoveSerializer());

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ReflectorsStateMsg>(OnReflectorStateRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, PowerProducerStateMsg>(OnPowerProducerStateRequest, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ThrustAndTorqueMsg>(OnThrustTorqueReceived, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, ThrustMsg>(OnThrustReceived, MyMessagePermissions.Any);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, IntegrityChangedMsg>(OnIntegrityChanged, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, StockpileChangedMsg>(OnStockpileChanged, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new MySyncGridStockpileSerializer());

            MySyncLayer.RegisterEntityMessage<MySyncGrid, StockpileFillRequestMsg>(OnStockpileFillRequest, MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, SetToConstructionRequestMsg>(OnSetToConstructionRequest, MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ModifyBlockGroupMsg>(OnModifyGroupSuccess, MyMessagePermissions.Any);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ConvertToShipMsg>(OnConvertedToShipRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, ConvertToShipMsg>(OnConvertedToShipSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, MergeMsg>(OnMergeGridSuccess, MyMessagePermissions.FromServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeOwnershipMsg>(OnChangeOwnerRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeOwnershipMsg>(OnChangeOwner, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeOwnershipsMsg>(OnChangeOwnersRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeOwnershipsMsg>(OnChangeOwners, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeGridOwnershipMsg>(OnChangeGridOwner, MyMessagePermissions.FromServer);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, SetHandbrakeMsg>(OnSetHandbrake, MyMessagePermissions.Any);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeDisplayNameMsg>(OnChangeDisplayNameRequest, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeDisplayNameMsg>(OnChangeDisplayName, MyMessagePermissions.Any, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncGrid, CreateSplitMsg>(OnCreateSplit, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, RemoveSplitMsg>(OnRemoveSplit, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncGrid, CreateSplitsMsg>(OnCreateSplits, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request, new MySyncGridSplitsSerializer());

            MySyncLayer.RegisterEntityMessage<MySyncGrid, ChangeDestructibleBlocksMsg>(OnChangeDestructibleBlocks, MyMessagePermissions.Any);
        }

        public event BuildBlocksAreaRequestDelegate BlocksBuiltAreaRequest;
        public event BuildBlocksAreaSuccessDelegate BlocksBuiltAreaSuccess;

        public event RazeBlocksAreaRequestDelegate BlocksRazeAreaRequest;
        public event RazeBlocksAreaSuccessDelegate BlocksRazeAreaSuccess;

        public event BuildBlocksDelegate BlocksBuilt;
        public event AfterBuildBlocksDelegate AfterBlocksBuilt;
        public event BuildBlockDelegate BlockBuilt;
        public event AfterBuildBlockDelegate AfterBlockBuilt;
        public event RazeBlockDelegate BlocksRazed;
        public event RazeBlockInCompoundDelegate RazedBlockInCompoundBlock;
        public event ColorBlocksDelegate BlocksColored;
        public event RemoveBlockDelegate BlocksRemovedWithGenerator;
        public event RemoveBlockDelegate BlocksRemovedWithoutGenerator;
        public event RemoveBlockDelegate BlocksDestroyed;
        public event RemoveBlockDelegate BlocksDeformed;
        public event MultipleStateDelegate ReflectorStateChanged;
        public event PowerChangedStateDelegate PowerProducerStateChanged;
        public event BlockIntegrityChangedDelegate BlockIntegrityChanged;
        public event BlockStockpileChangedDelegate BlockStockpileChanged;


        private List<Vector3I> m_removeBlockQueueWithGenerators = new List<Vector3I>();
        private List<Vector3I> m_removeBlockQueueWithoutGenerators = new List<Vector3I>();


        private List<Vector3I> m_destroyBlockQueue = new List<Vector3I>();
        private List<Vector3I> m_destructionDeformationQueue = new List<Vector3I>();
        private List<long> m_tmpBlockIdList = new List<long>();

        private MySyncGridThrustState m_thrustState = new MySyncGridThrustState();

        public new MyCubeGrid Entity
        {
            get { return (MyCubeGrid)base.Entity; }
        }

        public MySyncGrid(MyCubeGrid grid)
            : base(grid)
        {
        }

        //interface IBlockMessage : IEntityMessage
        //{
        //    Vector3I GetBlockPosition();
        //}

        //delegate void BlockMessageCallback<TBlock, TMsg>(TBlock block, ref TMsg message, MyPlayer sender)
        //     where TBlock : MyCubeBlock
        //    where TMsg : struct, IBlockMessage;

        //class MyCallback<TBlock, TMsg> : MySyncLayer.MyCallbackBase<TMsg>
        //    where TBlock : MyCubeBlock
        //    where TMsg : struct, IBlockMessage
        //{
        //    public readonly BlockMessageCallback<TBlock, TMsg> Callback;

        //    public MyCallback(MySyncLayer layer, BlockMessageCallback<TBlock, TMsg> callback, MyMessagePermissions permission, ISerializer<TMsg> serializer)
        //        : base(layer, permission, serializer)
        //    {
        //        Callback = callback;
        //    }

        //    protected override void OnHandle(ref TMsg msg, MyPlayer player)
        //    {
        //        MySyncGrid sync = Layer.GetSyncEntity<MySyncGrid, TMsg>(msg.GetEntityId());
        //        if (sync != null)
        //        {
        //            var block = sync.Entity.GetBlock(msg.GetBlockPosition());
        //            if (block != null)
        //            {
        //                var fatBlock = block.FatBlock as TBlock;
        //                if (fatBlock != null)
        //                {
        //                    Callback(fatBlock, ref msg, player);
        //                }
        //            }
        //        }
        //    }
        //}

        public void BuildBlock(Vector3 colorMaskHsv, MyCubeGrid.MyBlockLocation location, MyObjectBuilder_CubeBlock blockObjectBuilder, long builderEntityId)
        {
            var msg = new BuildBlockMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Location = location;
            msg.ColorMaskHsv = colorMaskHsv.PackHSVToUint();
            msg.BlockObjectBuilder = blockObjectBuilder;
            msg.BuilderEntityId = builderEntityId;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnBuildBlockRequest(MySyncGrid sync, ref BuildBlockMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid.MyBlockLocation? builtBlock = null;

            MyEntity builder = null;
            MyEntities.TryGetEntityById(msg.BuilderEntityId, out builder);

            var buildHandler = sync.BlockBuilt;
            if (buildHandler != null) buildHandler(ColorExtensions.UnpackHSVFromUint(msg.ColorMaskHsv), msg.Location, msg.BlockObjectBuilder, ref builtBlock, builder);
            
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
            
            var afterHandler = sync.AfterBlockBuilt;
            if (afterHandler != null && builtBlock != null) afterHandler(builtBlock.Value);
        }
       
        public void BuildBlocks(long ownerId, ref MyCubeGrid.MyBlockBuildArea area)
        {
            var msg = new BuildBlocksAreaRequestMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Area = area;
            msg.OwnerId = ownerId;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void BuildBlocksSuccess(ref MyCubeGrid.MyBlockBuildArea area, HashSet<Vector3UByte> failList, long ownerId, int entityIdSeed)
        {
            if (Sync.IsServer)
            {
                var successMsg = new BuildBlocksAreaSuccessMsg();
                successMsg.GridEntityId = Entity.EntityId;
                successMsg.Area = area;
                successMsg.OwnerId = ownerId;
                successMsg.EntityIdSeed = entityIdSeed;
                successMsg.FailList = failList;

                Sync.Layer.SendMessageToAll(ref successMsg);
            }
        }

        private static void OnBuildBlocksAreaRequest(MySyncGrid sync, ref BuildBlocksAreaRequestMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(sync.BlocksBuiltAreaRequest != null, "Handler should not be null, build messages will be ignored!");

            var handler = sync.BlocksBuiltAreaRequest;
            if (handler != null) handler(ref msg.Area, msg.OwnerId);
        }

        private static void OnBuildBlocksAreaSuccess(MySyncGrid sync, ref BuildBlocksAreaSuccessMsg successMsg, MyNetworkClient sender)
        {
            Debug.Assert(sync.BlocksBuiltAreaSuccess != null, "Handler should not be null, build messages will be ignored!");

            var handler = sync.BlocksBuiltAreaSuccess;
            if (handler != null) handler(ref successMsg.Area, successMsg.EntityIdSeed, successMsg.FailList, successMsg.OwnerId);
        }

        public void RazeBlocksArea(ref Vector3I pos, ref Vector3UByte size)
        {
            var msg = new RazeBlocksAreaRequestMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Pos = pos;
            msg.Size = size;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void RazeBlocksAreaSuccess(ref Vector3I pos, ref Vector3UByte size, HashSet<Vector3UByte> failList)
        {
            if (Sync.IsServer)
            {
                var successMsg = new RazeBlocksAreaSuccessMsg();
                successMsg.GridEntityId = Entity.EntityId;
                successMsg.Pos = pos;
                successMsg.Size = size;
                successMsg.FailList = failList;

                Sync.Layer.SendMessageToAll(ref successMsg);
            }
        }

        private static void OnRazeBlocksAreaRequest(MySyncGrid sync, ref RazeBlocksAreaRequestMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(sync.BlocksRazeAreaRequest != null, "Handler should not be null, Raze messages will be ignored!");

            var handler = sync.BlocksRazeAreaRequest;
            if (handler != null) handler(ref msg.Pos, ref msg.Size);
        }

        private static void OnRazeBlocksAreaSuccess(MySyncGrid sync, ref RazeBlocksAreaSuccessMsg successMsg, MyNetworkClient sender)
        {
            Debug.Assert(sync.BlocksRazeAreaSuccess != null, "Handler should not be null, Raze messages will be ignored!");

            var handler = sync.BlocksRazeAreaSuccess;
            if (handler != null) handler(ref successMsg.Pos, ref successMsg.Size, successMsg.FailList);
        }

        public void BuildBlocks(Vector3 colorMaskHsv, HashSet<MyCubeGrid.MyBlockLocation> locations, long builderEntityId)
        {
            var msg = new BuildBlocksMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Locations = locations;
            msg.ColorMaskHsv = colorMaskHsv.PackHSVToUint();
            msg.BuilderEntityId = builderEntityId;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnBuildBlocksRequest(MySyncGrid sync, ref BuildBlocksMsg msg, MyNetworkClient sender)
        {
            if (msg.Locations == null)
            {
                Debug.Fail("Invalid message");
                return;
            }

            m_tmpBuildList.Clear();
            Debug.Assert(m_tmpBuildList != msg.Locations, "The build block message was received via loopback using the temporary build list. This causes erasing ot the message.");

            MyEntity builder = null;
            MyEntities.TryGetEntityById(msg.BuilderEntityId, out builder);

            MyCubeBuilder.BuildComponent.GetBlocksPlacementMaterials(msg.Locations, sync.Entity);
            if (!MyCubeBuilder.BuildComponent.HasBuildingMaterials(builder))
                return;

            {
                var handler = sync.BlocksBuilt;
                if (handler != null) handler(ColorExtensions.UnpackHSVFromUint(msg.ColorMaskHsv), msg.Locations, m_tmpBuildList, builder);
            }

            if (Sync.IsServer && m_tmpBuildList.Count > 0)
            {
                // Broadcast to clients, use result collection
                msg.Locations = m_tmpBuildList;
                Sync.Layer.SendMessageToAll(ref msg);
            }

            {
                var handler = sync.AfterBlocksBuilt;
                if (handler != null) handler(m_tmpBuildList);
            }
        }

        public void RazeBlock(Vector3I position)
        {
            m_tmpPositionListSend.Clear();
            m_tmpPositionListSend.Add(position);
            RazeBlocks(m_tmpPositionListSend);
        }

        public void RazeBlocks(List<Vector3I> locations)
        {
            var msg = new RazeBlocksMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Locations = locations;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnRazeBlocksRequest(MySyncGrid sync, ref RazeBlocksMsg msg, MyNetworkClient sender)
        {
            m_tmpPositionListReceive.Clear();
            Debug.Assert(m_tmpPositionListReceive != msg.Locations, "The raze block message was received via loopback using the same list. This causes erasing of the message.");
            var handler = sync.BlocksRazed;
            if (handler != null) handler(msg.Locations, m_tmpPositionListReceive);

            if (Sync.IsServer && m_tmpPositionListReceive.Count > 0)
            {
                // Broadcast to clients, use result collection
                msg.Locations = m_tmpPositionListReceive;
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        public void ColorBlocks(Vector3I min, Vector3I max, Vector3 newHSV, bool playSound)
        {
            var msg = new ColorBlocksMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.HSV = newHSV.PackHSVToUint();
            msg.Min = min;
            msg.Max = max;
            msg.PlaySound = playSound;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnColorBlocksRequest(MySyncGrid sync, ref ColorBlocksMsg msg, MyNetworkClient sender)
        {
            var handler = sync.BlocksColored;
            if (handler != null) handler(msg.Min, msg.Max, ColorExtensions.UnpackHSVFromUint(msg.HSV), msg.PlaySound);

            if (Sync.IsServer)
            {
                // Broadcast to clients, use result collection
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        public void RazeBlockInCompoundBlock(List<Tuple<Vector3I, ushort>> locationsAndIds)
        {
            var msg = new RazeBlockInCompoundBlockMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.LocationsAndIds = locationsAndIds;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnRazeBlockInCompoundBlockRequest(MySyncGrid sync, ref RazeBlockInCompoundBlockMsg msg, MyNetworkClient sender)
        {
            m_tmpLocationsAndIdsReceive.Clear();
            Debug.Assert(m_tmpLocationsAndIdsReceive != msg.LocationsAndIds, "The raze block in compound block message was received via loopback using the same list. This causes erasing of the message.");
            var handler = sync.RazedBlockInCompoundBlock;
            if (handler != null) handler(msg.LocationsAndIds, m_tmpLocationsAndIdsReceive);

            if (Sync.IsServer && m_tmpLocationsAndIdsReceive.Count > 0)
            {
                // Broadcast to clients, use result collection
                msg.LocationsAndIds = m_tmpLocationsAndIdsReceive;
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }


        public void SendDirtyBones(Vector3I minBone, Vector3I maxBone, MyGridSkeleton skeleton)
        {
            if (Sync.IsServer)
            {
                m_boneByteList.Clear();
                skeleton.SerializePart(minBone, maxBone, Entity.GridSize, m_boneByteList);

                if (m_boneByteList.Count > 0)
                {
                    var msg = new BonesMsg();
                    msg.GridEntityId = Entity.EntityId;
                    msg.MinBone = minBone;
                    msg.MaxBone = maxBone;
                    msg.Bones = m_boneByteList;

                    Sync.Layer.SendMessageToAll(ref msg);
                }
            }
        }

        private static void OnBonesReceived(MySyncGrid sync, ref BonesMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Skeleton.DeserializePart(msg.MinBone, msg.MaxBone, sync.Entity.GridSize, msg.Bones);

            Vector3I minCube = Vector3I.Zero;
            Vector3I maxCube = Vector3I.Zero;

            // To hit incident cubes
            Vector3I min = msg.MinBone;// -Vector3I.One;
            Vector3I max = msg.MaxBone;// +Vector3I.One;

            sync.Entity.Skeleton.Wrap(ref minCube, ref min);
            sync.Entity.Skeleton.Wrap(ref maxCube, ref max);

            minCube -= Vector3I.One;
            maxCube += Vector3I.One;

            Vector3I pos;
            for (pos.X = minCube.X; pos.X <= maxCube.X; pos.X++)
            {
                for (pos.Y = minCube.Y; pos.Y <= maxCube.Y; pos.Y++)
                {
                    for (pos.Z = minCube.Z; pos.Z <= maxCube.Z; pos.Z++)
                    {
                        sync.Entity.SetCubeDirty(pos);
                    }
                }
            }
        }

        public void SendBonesMultiplied(Vector3I blockLocation, float multiplier)
        {
            var msg = new BonesMultiplyMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Location = blockLocation;
            msg.Multiplier = multiplier;

            Sync.Layer.SendMessageToAll(msg);
        }

        private static void OnBonesMultiplied(MySyncGrid sync, ref BonesMultiplyMsg msg, MyNetworkClient sender)
        {
            var block = sync.Entity.GetCubeBlock(msg.Location);

            Debug.Assert(block != null, "Block was null in OnBonesMultiplied handler!");
            if (block == null) return;

            sync.Entity.MultiplyBlockSkeleton(block, msg.Multiplier);
        }

        /// <summary>
        /// Server method, adds removed block into queue
        /// </summary>
        public void EnqueueRemovedBlock(Vector3I position, bool generatorsEnabled)
        {
            if (Sync.IsServer)
            {
                if (generatorsEnabled)
                    m_removeBlockQueueWithGenerators.Add(position);
                else
                    m_removeBlockQueueWithoutGenerators.Add(position);
            }
        }

        public void EnqueueDestroyedBlock(Vector3I position)
        {
            if (Sync.IsServer)
            {
                m_destroyBlockQueue.Add(position);
            }
        }

        public void EnqueueDestructionDeformationBlock(Vector3I position)
        {
            if (Sync.IsServer)
            {
                m_destructionDeformationQueue.Add(position);
            }
        }

        /// <summary>
        /// Server method, sends queued blocks to clients
        /// Can be optimized by using VoxelSegmentation
        /// </summary>
        public void SendRemovedBlocks()
        {
            if (Sync.IsServer && (m_removeBlockQueueWithGenerators.Count > 0 || m_removeBlockQueueWithoutGenerators.Count > 0 || m_destroyBlockQueue.Count > 0 || m_destructionDeformationQueue.Count > 0))
            {
                var msg = new RemoveBlocksMsg();
                msg.GridEntityId = Entity.EntityId;
                msg.LocationsWithGenerator = m_removeBlockQueueWithGenerators;
                msg.LocationsWithoutGenerator = m_removeBlockQueueWithoutGenerators;
                msg.DestroyLocations = m_destroyBlockQueue;
                msg.DestructionDeformationLocations = m_destructionDeformationQueue;
                Sync.Layer.SendMessageToAll(ref msg);

                m_removeBlockQueueWithGenerators.Clear();
                m_removeBlockQueueWithoutGenerators.Clear();
                m_destroyBlockQueue.Clear();
                m_destructionDeformationQueue.Clear();
            }
        }

        private static void OnRemoveBlocks(MySyncGrid sync, ref RemoveBlocksMsg msg, MyNetworkClient sender)
        {
            var handler2 = sync.BlocksDestroyed;
            if (handler2 != null && msg.DestroyLocations != null) handler2(msg.DestroyLocations);

            var handler = sync.BlocksRemovedWithGenerator;
            if (handler != null && msg.LocationsWithGenerator != null) handler(msg.LocationsWithGenerator);

            var handler4 = sync.BlocksRemovedWithoutGenerator;
            if (handler4 != null && msg.LocationsWithoutGenerator != null) handler4(msg.LocationsWithoutGenerator);

            var handler3 = sync.BlocksDeformed;
            if (handler3 != null && msg.DestructionDeformationLocations != null) handler3(msg.DestructionDeformationLocations);
        }

        private static void OnReflectorStateRequest(MySyncGrid sync, ref ReflectorsStateMsg msg, MyNetworkClient sender)
        {
            var handler = sync.ReflectorStateChanged;
            if (handler != null) handler(msg.Enabled);

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        public void SendReflectorState(MyMultipleEnabledEnum enabledState)
        {
            var msg = new ReflectorsStateMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Enabled = enabledState;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnPowerProducerStateRequest(MySyncGrid sync, ref PowerProducerStateMsg msg, MyNetworkClient sender)
        {
            var handler = sync.PowerProducerStateChanged;
            if (handler != null) handler(msg.Enabled, msg.PlayerId);

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        public void SendPowerDistributorState(MyMultipleEnabledEnum enabledState, long playerId)
        {
            var msg = new PowerProducerStateMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Enabled = enabledState;
            msg.PlayerId = playerId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void SendControlThrustAndTorque(Vector3 controlThrust, Vector3 controlTorque)
        {
            SendControlThrust(controlThrust);
        }

        private static void OnThrustTorqueReceived(MySyncGrid sync, ref ThrustAndTorqueMsg msg, MyNetworkClient sender)
        {
            sender.ClientFrameId = msg.ClientFrameId;

            //if (false)
            {
                sync.Entity.GridSystems.ThrustSystem.ControlThrust = msg.Thrust;
                sync.Entity.GridSystems.GyroSystem.ControlTorque = msg.Torque;
            }
        }

        public void SendControlThrust(Vector3 controlThrust)
        {
            var msg = new ThrustMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.Thrust = controlThrust;

            if (m_thrustState.ShouldSend(msg.ThrustData))
            {
                // Send to all because we want thrust animation on other ships
                if (msg.ThrustData == Vector3B.Zero)
                {
                    // Send zero thrust as reliable
                    var zeroMsg = new ThrustZeroMsg();
                    zeroMsg.GridEntityId = Entity.EntityId;
                    Sync.Layer.SendMessageToAll(ref msg);
                }
                else
                {
                    Sync.Layer.SendMessageToAll(ref msg);
                }
            }
        }

        private static void OnThrustReceived(MySyncGrid sync, ref ThrustMsg msg, MyNetworkClient sender)
        {
            sync.Entity.GridSystems.ThrustSystem.ControlThrust = msg.Thrust;
        }

		private ushort GetSubBlockId(MySlimBlock slimBlock)
		{
			var block = slimBlock.CubeGrid.GetCubeBlock(slimBlock.Position);
			MyCompoundCubeBlock compoundBlock = null;
			if (block != null)
				compoundBlock = block.FatBlock as MyCompoundCubeBlock;
			if (compoundBlock != null)
			{
				var subBlockId = compoundBlock.GetBlockId(slimBlock);
				return subBlockId ?? 0;
			}

			return 0;
		}

        public void SendIntegrityChanged(MySlimBlock mySlimBlock, MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, long toolOwner)
        {
            Debug.Assert(Sync.IsServer, "Other player than server is trying to send integrity changes");

            var msg = new IntegrityChangedMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.BuildIntegrity = mySlimBlock.BuildIntegrity;
            msg.Integrity = mySlimBlock.Integrity;
            msg.BlockPosition = mySlimBlock.Position;
            msg.IntegrityChangeType = integrityChangeType;
            msg.ToolOwner = toolOwner;
			msg.SubBlockId = GetSubBlockId(mySlimBlock);

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnIntegrityChanged(MySyncGrid sync, ref IntegrityChangedMsg msg, MyNetworkClient sender)
        {
            if (sync.BlockIntegrityChanged != null)
                sync.BlockIntegrityChanged(msg.BlockPosition, msg.SubBlockId, msg.BuildIntegrity, msg.Integrity, msg.IntegrityChangeType, msg.ToolOwner);
        }

        public void SendStockpileChanged(MySlimBlock mySlimBlock, List<MyStockpileItem> list)
        {
            Debug.Assert(Sync.IsServer, "Other player than server is trying to send stockpile changes");
            if (list.Count() == 0) return;

            var msg = new StockpileChangedMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.BlockPosition = mySlimBlock.Position;
            msg.Changes = list;
            Debug.Assert(list != null, "List of stockpile changes was null!");
			msg.SubBlockId = GetSubBlockId(mySlimBlock);

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnStockpileChanged(MySyncGrid sync, ref StockpileChangedMsg msg, MyNetworkClient sender)
        {
            if (sync.BlockStockpileChanged != null)
                sync.BlockStockpileChanged(msg.BlockPosition, msg.SubBlockId, msg.Changes);
        }

        public void RequestFillStockpile(Vector3I blockPosition, MyInventory fromInventory)
        {
            Debug.Assert(fromInventory.Owner != null, "Inventory owner was null");

            var msg = new StockpileFillRequestMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.BlockPosition = blockPosition;
            msg.InventoryIndex = fromInventory.InventoryIdx;
            msg.OwnerEntityId = fromInventory.Owner.EntityId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnStockpileFillRequest(MySyncGrid sync, ref StockpileFillRequestMsg msg, MyNetworkClient sender)
        {
            var block = sync.Entity.GetCubeBlock(msg.BlockPosition);
            Debug.Assert(block != null, "Could not find block whose stockpile fill was requested");
            if (block == null) return;

            MyEntity ownerEntity = null;
            if (!MyEntities.TryGetEntityById(msg.OwnerEntityId, out ownerEntity))
            {
                Debug.Assert(false, "Stockpile fill inventory owner entity was null");
                return;
            }

            var owner = (ownerEntity as IMyInventoryOwner);
            Debug.Assert(owner != null, "Stockpile fill inventory owner was not an inventory owner");

            var inventory = owner.GetInventory(msg.InventoryIndex);
            Debug.Assert(inventory != null, "Stockpile fill inventory owner did not have the given inventory");

            block.MoveItemsToConstructionStockpile(inventory);
        }

        public void RequestSetToConstruction(Vector3I blockPosition, MyInventory fromInventory)
        {
            var msg = new SetToConstructionRequestMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.BlockPosition = blockPosition;
            msg.InventoryIndex = fromInventory.InventoryIdx;
            msg.OwnerEntityId = fromInventory.Owner.EntityId;
            // CH: TODO: Allow bots to set to construction
            msg.RequestingPlayer = MySession.LocalPlayerId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnSetToConstructionRequest(MySyncGrid sync, ref SetToConstructionRequestMsg msg, MyNetworkClient sender)
        {
            var block = sync.Entity.GetCubeBlock(msg.BlockPosition);
            Debug.Assert(block != null, "Could not find block to set to construction site");
            if (block == null) return;

            block.SetToConstructionSite();

            MyEntity ownerEntity = null;
            if (!MyEntities.TryGetEntityById(msg.OwnerEntityId, out ownerEntity))
            {
                Debug.Assert(false, "Set to construction site inventory owner entity was null");
                return;
            }

            var owner = (ownerEntity as IMyInventoryOwner);
            Debug.Assert(owner != null, "Set to construction site inventory owner was not an inventory owner");

            var inventory = owner.GetInventory(msg.InventoryIndex);
            Debug.Assert(inventory != null, "Set to construction site inventory owner did not have the given inventory");

            block.MoveItemsToConstructionStockpile(inventory);
            block.IncreaseMountLevel(MyWelder.WELDER_AMOUNT_PER_SECOND * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, msg.RequestingPlayer);
        }

        public void ModifyGroup(MyBlockGroup group)
        {
            var msg = new ModifyBlockGroupMsg();

            msg.GridEntityId = Entity.EntityId;
            msg.Name = group.Name.ToString();
            foreach (var block in group.Blocks)
                m_tmpBlockIdList.Add(block.EntityId);
            msg.Blocks = m_tmpBlockIdList.ToArray();
            m_tmpBlockIdList.Clear();

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnModifyGroupSuccess(MySyncGrid sync, ref ModifyBlockGroupMsg msg, MyNetworkClient sender)
        {
            if (msg.Blocks == null || msg.Blocks.Count() == 0)
                foreach (var group in sync.Entity.BlockGroups)
                {
                    if (group.Name.ToString().Equals(msg.Name))
                    {
                        sync.Entity.RemoveGroup(group);
                        break;
                    }
                }
            else
            {
                MyBlockGroup group = new MyBlockGroup(sync.Entity);
                group.Name.Clear().Append(msg.Name);
                foreach (var blockId in msg.Blocks)
                {
                    MyTerminalBlock block = null;
                    if (MyEntities.TryGetEntityById(blockId, out block))
                        group.Blocks.Add(block);
                }
                sync.Entity.AddGroup(group);
            }
        }

        public void RequestConversionToShip()
        {
            ConvertToShipMsg msg = new ConvertToShipMsg();
            msg.GridEntityId = Entity.EntityId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnConvertedToShipRequest(MySyncGrid sync, ref ConvertToShipMsg msg, MyNetworkClient sender)
        {
            if (!sync.Entity.IsStatic)
            {
                Debug.Assert(false, "Grid was not static!");
                return;
            }

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnConvertedToShipSuccess(MySyncGrid sync, ref ConvertToShipMsg msg, MyNetworkClient sender)
        {
            if (!sync.Entity.IsStatic)
            {
                Debug.Assert(false, "Grid was not static!");
                return;
            }

            sync.Entity.ConvertToDynamic();
        }

        internal void MergeGrid(MyCubeGrid gridToMerge, ref MatrixI transform)
        {
            var msg = new MergeMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.OtherEntityId = gridToMerge.EntityId;
            msg.GridOffset = transform.Translation;
            msg.GridForward = transform.Forward;
            msg.GridUp = transform.Up;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnMergeGridSuccess(MySyncGrid sync, ref MergeMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid grid = null;
            if (MyEntities.TryGetEntityById<MyCubeGrid>(msg.OtherEntityId, out grid))
            {
                Vector3I gridOffset = msg.GridOffset;
                MatrixI transform = new MatrixI(msg.GridOffset, msg.GridForward, msg.GridUp);
                sync.Entity.MergeGridInternal(grid, ref transform);
            }
        }


        internal void ChangeOwnerRequest(MyCubeGrid grid, MyCubeBlock block, long playerId, MyOwnershipShareModeEnum shareMode)
        {
            System.Diagnostics.Debug.Assert(playerId >= 0);
            System.Diagnostics.Debug.Assert((int)shareMode >= 0);

            var msg = new ChangeOwnershipMsg();
            msg.GridEntityId = grid.EntityId;
            msg.BlockId = block.EntityId;
            msg.Owner = playerId;
            msg.RequestingPlayer = playerId;
            msg.ShareMode = shareMode;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public static void ChangeOwnersRequest(MyOwnershipShareModeEnum shareMode, List<MySingleOwnershipRequest> requests)
        {
            System.Diagnostics.Debug.Assert((int)shareMode >= 0);

            var msg = new ChangeOwnershipsMsg();
            msg.RequestingPlayer = MySession.LocalPlayerId; // CH: This is (probably) set only via GUI. If you intend to change this, you'll need playerId
            msg.ShareMode = shareMode;

            msg.Requests = requests;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnChangeOwnerRequest(MySyncGrid sync, ref ChangeOwnershipMsg msg, MyNetworkClient sender)
        {
            MyCubeBlock block = null;
            if (MyEntities.TryGetEntityById<MyCubeBlock>(msg.BlockId, out block))
            {
                if (Sync.IsServer && ((MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle && block.IDModule == null) || (block.IDModule.Owner == 0) || block.IDModule.Owner == msg.RequestingPlayer || (msg.Owner == 0)))
                {
                    OnChangeOwner(sync, ref msg, sender);
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Invalid ownership change request!");
                }
            }
        }

        private static void OnChangeOwnersRequest(ref ChangeOwnershipsMsg msg, MyNetworkClient sender)
        {
            MyCubeBlock block = null;
            int c = 0;

            while (c < msg.Requests.Count)
            {
                var request = msg.Requests[c];
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    if (Sync.IsServer && ((block.IDModule.Owner == 0) || block.IDModule.Owner == msg.RequestingPlayer || (request.Owner == 0)))
                    {
                        c++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Fail("Invalid ownership change request!");
                        msg.Requests.RemoveAtFast(c);
                    }
                }
                else 
                {
                    c++;
                }
            }

            if (msg.Requests.Count > 0)
            {
                OnChangeOwners(ref msg, sender);
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }

        private static void OnChangeOwner(MySyncGrid grid, ref ChangeOwnershipMsg msg, MyNetworkClient sender)
        {
            MyCubeBlock block = null;
            if (MyEntities.TryGetEntityById<MyCubeBlock>(msg.BlockId, out block))
            {
                block.ChangeOwner(msg.Owner, msg.ShareMode);
            }
        }

        private static void OnChangeOwners(ref ChangeOwnershipsMsg msg, MyNetworkClient sender)
        {
            foreach (var request in msg.Requests)
            {
                MyCubeBlock block = null;
                if (MyEntities.TryGetEntityById<MyCubeBlock>(request.BlockId, out block))
                {
                    block.ChangeOwner(request.Owner, msg.ShareMode);
                }
            }
        }

        internal static void ChangeGridOwner(MyCubeGrid grid, long playerId, MyOwnershipShareModeEnum shareMode)
        {
            var msg = new ChangeGridOwnershipMsg();
            msg.GridEntityId = grid.EntityId;
            msg.Owner = playerId;
            msg.ShareMode = shareMode;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnChangeGridOwner(MySyncGrid syncObject, ref ChangeGridOwnershipMsg message, MyNetworkClient sender)
        {
            foreach (var block in syncObject.Entity.GetBlocks())
            {
                if (block.FatBlock != null && block.BlockDefinition.RatioEnoughForOwnership(block.BuildLevelRatio))
                {
                    block.FatBlock.ChangeOwner(message.Owner, message.ShareMode);
                }
            }
        }

        internal void SetHandbrakeRequest(bool v)
        {
            var msg = new SetHandbrakeMsg();

            msg.GridEntityId = Entity.EntityId;
            msg.Handbrake = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnSetHandbrake(MySyncGrid syncObject, ref SetHandbrakeMsg msg, MyNetworkClient sender)
        {
            syncObject.Entity.GridSystems.WheelSystem.HandBrake = msg.Handbrake;
        }

        internal void ChangeDisplayNameRequest(MyCubeGrid grid, String DisplayName)
        {
            var msg = new ChangeDisplayNameMsg();
            msg.GridEntityId = grid.EntityId;
            msg.DisplayName = DisplayName;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnChangeDisplayNameRequest(MySyncGrid sync, ref ChangeDisplayNameMsg msg, MyNetworkClient sender)
        {
            if (Sync.IsServer)
                ChangeDisplayName(ref msg);
            else
                System.Diagnostics.Debug.Fail("Invalid display name change request!");

        }

        static void ChangeDisplayName(ref ChangeDisplayNameMsg msg)
        {
            Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnChangeDisplayName(MySyncGrid grid, ref ChangeDisplayNameMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid m_grid;
            if (MyEntities.TryGetEntityById(msg.GridEntityId, out m_grid))
                m_grid.DisplayName = msg.DisplayName;
        }

        public void AnnounceCreateSplits(List<MySlimBlock> splitBlocks, List<MyDisconnectHelper.Group> groups)
        {
            m_tmpPositionListSend.Clear();

            CreateSplitsMsg msg = new CreateSplitsMsg();
            msg.GridEntityId = this.Entity.EntityId;
            msg.Groups = groups;
            msg.SplitBlocks = m_tmpPositionListSend;
            foreach (var b in splitBlocks)
            {
                msg.SplitBlocks.Add(b.Position);
            }
            Sync.Layer.SendMessageToAll(ref msg);

            m_tmpPositionListSend.Clear();
        }

        private static void OnCreateSplits(MySyncGrid syncGrid, ref CreateSplitsMsg msg, MyNetworkClient sender)
        {
            m_tmpBlockListReceive.Clear();

            var grid = syncGrid.Entity;
            foreach (var b in msg.SplitBlocks)
            {
                var block = grid.GetCubeBlock(b);
                m_tmpBlockListReceive.Add(block); // Add even null, we cannot break the order
            }
            MyCubeGrid.CreateSplits(grid, m_tmpBlockListReceive, msg.Groups, false);

            m_tmpBlockListReceive.Clear();
        }

        public void AnnounceCreateSplit(List<MySlimBlock> blocks, long newEntityId)
        {
            var msg = new CreateSplitMsg();
            msg.GridEntityId = Entity.EntityId;
            msg.NewGridEntityId = newEntityId;

            m_tmpPositionListSend.Clear();
            msg.SplitBlocks = m_tmpPositionListSend;
            foreach (var block in blocks)
            {
                msg.SplitBlocks.Add(block.Position);
            }

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnCreateSplit(MySyncGrid grid, ref CreateSplitMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid m_grid;
            if (MyEntities.TryGetEntityById(msg.GridEntityId, out m_grid))
            {
                m_tmpBlockListReceive.Clear();
                foreach (var position in msg.SplitBlocks)
                {
                    var block = m_grid.GetCubeBlock(position);
                    Debug.Assert(block != null, "Block was null when trying to create a grid split. Desync?");
                    if (block == null)
                    {
                        MySandboxGame.Log.WriteLine("Block was null when trying to create a grid split. Desync?");
                        continue;
                    }

                    m_tmpBlockListReceive.Add(block);
                }

                MyCubeGrid.CreateSplit(m_grid, m_tmpBlockListReceive, sync: false, newEntityId: msg.NewGridEntityId);
                m_tmpBlockListReceive.Clear();
            }
        }

        public void AnnounceRemoveSplit(List<MySlimBlock> blocks)
        {
            var msg = new RemoveSplitMsg();
            msg.GridEntityId = Entity.EntityId;

            m_tmpPositionListSend.Clear();
            msg.RemovedBlocks = m_tmpPositionListSend;
            foreach (var block in blocks)
            {
                msg.RemovedBlocks.Add(block.Position);
            }

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnRemoveSplit(MySyncGrid grid, ref RemoveSplitMsg msg, MyNetworkClient sender)
        {
            MyCubeGrid m_grid;
            if (MyEntities.TryGetEntityById(msg.GridEntityId, out m_grid))
            {
                m_tmpBlockListReceive.Clear();
                foreach (var position in msg.RemovedBlocks)
                {
                    var block = m_grid.GetCubeBlock(position);
                    Debug.Assert(block != null, "Block was null when trying to remove a grid split. Desync?");
                    if (block == null)
                    {
                        MySandboxGame.Log.WriteLine("Block was null when trying to remove a grid split. Desync?");
                        continue;
                    }

                    m_tmpBlockListReceive.Add(block);
                }

                MyCubeGrid.RemoveSplit(m_grid, m_tmpBlockListReceive, 0, m_tmpBlockListReceive.Count, sync: false);
                m_tmpBlockListReceive.Clear();
            }
        }

        internal void SetDestructibleBlocks(bool destructionEnabled)
        {
            var msg = new ChangeDestructibleBlocksMsg();

            msg.GridEntityId = Entity.EntityId;
            msg.DestructionEnabled = destructionEnabled;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnChangeDestructibleBlocks(MySyncGrid syncObject, ref ChangeDestructibleBlocksMsg msg, MyNetworkClient sender)
        {
            syncObject.Entity.DestructibleBlocks = msg.DestructionEnabled;
        }
    }
}

using System;
using System.Text;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using Sandbox.Game.Entities.Blocks;
using System.IO;
using System.IO.Compression;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Multiplayer
{
    internal static class StringCompressor
    {
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
        public static byte[] CompressString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        public static string DecompressString(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

    }

    [PreloadRequired]
    class MySyncProgrammableBlock
    {
        MyProgrammableBlock m_programmableBlock;

        [MessageId(16275, P2PMessageEnum.Reliable)]
        [ProtoBuf.ProtoContract]
        protected struct TerminalRunArgumentMsg : IEntityMessage
        {
            [ProtoBuf.ProtoMember(1)]
            public long EntityId;
            public long GetEntityId() { return EntityId; }
                
            [ProtoBuf.ProtoMember(2)]
            public string TerminalRunArgument;
        }

        [MessageId(16276, P2PMessageEnum.Reliable)]
        protected struct ClearArgumentOnRunMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
                
            public BoolBlit ClearArgumentOnRun;
        }
        
        [MessageIdAttribute(16277, P2PMessageEnum.Reliable)]
        protected struct OpenEditorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public ulong User;
        }

        [MessageIdAttribute(16278, P2PMessageEnum.Reliable)]
        protected struct CloseEditorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        [ProtoBuf.ProtoContract]
        [MessageIdAttribute(16279, P2PMessageEnum.Reliable)]
        protected struct UpdateProgramMsg : IEntityMessage
        { 
            [ProtoBuf.ProtoMember(1)]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            [ProtoBuf.ProtoMember(2)]
            public byte[] Program;
            [ProtoBuf.ProtoMember(3)]
            public byte[] Storage;
        }

        [ProtoBuf.ProtoContract]
        [MessageIdAttribute(16280, P2PMessageEnum.Reliable)]
        protected struct RunProgramMsg : IEntityMessage
        {
            [ProtoBuf.ProtoMember(1)]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            [ProtoBuf.ProtoMember(2)]
            public byte[] Argument;

            [ProtoBuf.ProtoMember(3)]
            public bool ClearTerminalArgument;
        }

        [ProtoBuf.ProtoContract]
        [MessageIdAttribute(16281, P2PMessageEnum.Reliable)]
        protected struct ProgramRepsonseMsg : IEntityMessage
        {
            [ProtoBuf.ProtoMember(1)]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            
            [ProtoBuf.ProtoMember(2)]
            public string Response;

            [ProtoBuf.ProtoMember(3)]
            public bool ClearTerminalArgument;
        }

        static MySyncProgrammableBlock()
        {
            MySyncLayer.RegisterMessage<OpenEditorMsg>(OpenEditorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<OpenEditorMsg>(OpenEditorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<OpenEditorMsg>(OpenEditorFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);

            MySyncLayer.RegisterMessage<CloseEditorMsg>(CloseEditor, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);

            MySyncLayer.RegisterMessage<UpdateProgramMsg>(UpdateProgramRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<UpdateProgramMsg>(UpdateProgramSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<RunProgramMsg>(RunProgramRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);

            MySyncLayer.RegisterMessage<ProgramRepsonseMsg>(ProgramResponeSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ClearArgumentOnRunMsg>(ClearArgumentOnRunRequestCallback, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ClearArgumentOnRunMsg>(ClearArgumentOnRunSuccessCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<TerminalRunArgumentMsg>(TerminalRunArgumentRequestCallback, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<TerminalRunArgumentMsg>(TerminalRunArgumentSuccessCallback, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncProgrammableBlock(MyProgrammableBlock block)
        {
            m_programmableBlock = block;
        }

        public virtual void SendOpenEditorRequest(ulong user)
        {
            if (Sync.IsServer)
            {
                if (m_programmableBlock.ConsoleOpen == false)
                {
                    m_programmableBlock.ConsoleOpen = true;
                    m_programmableBlock.OpenEditor();
                }
                else
                {
                    m_programmableBlock.ShowEditorAllReadyOpen();
                }
            }
            else
            {
                var msg = new OpenEditorMsg();
                msg.User = user;
                m_programmableBlock.ConsoleOpenRequest = true;
                msg.EntityId = m_programmableBlock.EntityId;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        static void OpenEditorRequest(ref OpenEditorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyProgrammableBlock block = entity as MyProgrammableBlock;
            if (block != null && block.ConsoleOpen == false)
            {
                block.UserId = msg.User;
                block.ConsoleOpen = true;
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
            else
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Failure);
            }
        }

        static void OpenEditorSuccess(ref OpenEditorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyProgrammableBlock block =  entity as MyProgrammableBlock;
            if (block != null && block.ConsoleOpenRequest)
            {
                block.ConsoleOpenRequest = false;
                block.OpenEditor();
            }

        }

        static void OpenEditorFailure(ref OpenEditorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyProgrammableBlock block = entity as MyProgrammableBlock;
            if (block != null && block.ConsoleOpenRequest)
            {
                block.ConsoleOpenRequest = false;
                block.ShowEditorAllReadyOpen();
            }
        }

        public virtual void SendCloseEditor()
        {          
            if (Sync.IsServer)
            {
                m_programmableBlock.ConsoleOpen = false;          
            }
            else
            {
                var msg = new CloseEditorMsg();
                msg.EntityId = m_programmableBlock.EntityId;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        static void CloseEditor(ref CloseEditorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity = null;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyProgrammableBlock block = entity as MyProgrammableBlock;
            if (block != null)
            {
                block.ConsoleOpen = false;
            }
        }

        public virtual void SendUpdateProgramRequest(string program,string storage)
        {
            var msg = new UpdateProgramMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.Program = StringCompressor.CompressString(program);
            msg.Storage = StringCompressor.CompressString(storage);
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void UpdateProgramRequest(ref UpdateProgramMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyProgrammableBlock)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void UpdateProgramSuccess(ref UpdateProgramMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyProgrammableBlock)
            {
                (entity as MyProgrammableBlock).UpdateProgram(StringCompressor.DecompressString(msg.Program),StringCompressor.DecompressString(msg.Storage));
            }
        }

        static void RunProgramRequest(ref RunProgramMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyProgrammableBlock)
            {
                (entity as MyProgrammableBlock).Run(StringCompressor.DecompressString(msg.Argument), false);
            }
        }
        public virtual void SendRunProgramRequest(string argument, bool clearTerminalArgument)
        {
            var msg = new RunProgramMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.Argument = StringCompressor.CompressString(argument ?? string.Empty);
            msg.ClearTerminalArgument = clearTerminalArgument;
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ProgramResponeSuccess(ref ProgramRepsonseMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var programmableBlock = entity as MyProgrammableBlock;
            if (programmableBlock != null)
            {
                if (msg.ClearTerminalArgument)
                    programmableBlock.TerminalRunArgument = "";
                programmableBlock.WriteProgramResponse(msg.Response);
            }
        }
        public virtual void SendProgramResponseMessage(string response, bool clearTerminalArgument)
        {
            var msg = new ProgramRepsonseMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.Response = response;
            msg.ClearTerminalArgument = clearTerminalArgument;
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
        public virtual void RequestChangeClearArgumentOnRun(bool clear)
        {
            ClearArgumentOnRunMsg msg = new ClearArgumentOnRunMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.ClearArgumentOnRun = clear;

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                m_programmableBlock.ClearArgumentOnRun = msg.ClearArgumentOnRun;
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        static void ClearArgumentOnRunRequestCallback(ref ClearArgumentOnRunMsg msg, MyNetworkClient sender)
        {
            MyProgrammableBlock programmableBlock;
            MyEntities.TryGetEntityById(msg.EntityId, out programmableBlock);
            if (programmableBlock != null)
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                programmableBlock.ClearArgumentOnRun = msg.ClearArgumentOnRun;
            }   
        }

        static void ClearArgumentOnRunSuccessCallback(ref ClearArgumentOnRunMsg msg, MyNetworkClient sender)
        {
            MyProgrammableBlock programmableBlock;
            MyEntities.TryGetEntityById(msg.EntityId, out programmableBlock);
            if (programmableBlock != null)
            {
                programmableBlock.ClearArgumentOnRun = msg.ClearArgumentOnRun;
            }
        }
    
        public virtual void RequestChangeTerminalRunArgument(string argument)
        {
            TerminalRunArgumentMsg msg = new TerminalRunArgumentMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.TerminalRunArgument = argument;

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                m_programmableBlock.TerminalRunArgument = msg.TerminalRunArgument;
            }
            else
            {
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        static void TerminalRunArgumentRequestCallback(ref TerminalRunArgumentMsg msg, MyNetworkClient sender)
        {
            MyProgrammableBlock programmableBlock;
            MyEntities.TryGetEntityById(msg.EntityId, out programmableBlock);
            if (programmableBlock != null)
            {
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                programmableBlock.TerminalRunArgument = msg.TerminalRunArgument;
            }   
        }

        static void TerminalRunArgumentSuccessCallback(ref TerminalRunArgumentMsg msg, MyNetworkClient sender)
        {
            MyProgrammableBlock programmableBlock;
            MyEntities.TryGetEntityById(msg.EntityId, out programmableBlock);
            if (programmableBlock != null)
            {
                programmableBlock.TerminalRunArgument = msg.TerminalRunArgument;
            }
        }
    }

}

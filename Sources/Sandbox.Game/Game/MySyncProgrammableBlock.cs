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
            [ProtoBuf.ProtoMember]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            [ProtoBuf.ProtoMember]
            public byte[] Program;
            [ProtoBuf.ProtoMember]
            public byte[] Storage;
        }

        [ProtoBuf.ProtoContract]
        [MessageIdAttribute(16280, P2PMessageEnum.Reliable)]
        protected struct RunProgramMsg : IEntityMessage
        {
            [ProtoBuf.ProtoMember]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            [ProtoBuf.ProtoMember]
            public byte[] Argument;
        }

        [ProtoBuf.ProtoContract]
        [MessageIdAttribute(16281, P2PMessageEnum.Reliable)]
        protected struct ProgramRepsonseMsg : IEntityMessage
        {
            [ProtoBuf.ProtoMember]
            public long EntityId;

            public long GetEntityId() { return EntityId; }
            [ProtoBuf.ProtoMember]
            public string Response;
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
                (entity as MyProgrammableBlock).Run(StringCompressor.DecompressString(msg.Argument));
            }
        }
        public virtual void SendRunProgramRequest(string argument)
        {
            var msg = new RunProgramMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.Argument = StringCompressor.CompressString(argument ?? string.Empty);
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ProgramResponeSuccess(ref ProgramRepsonseMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyProgrammableBlock)
            {
                (entity as MyProgrammableBlock).WriteProgramResponse(msg.Response);
            }
        }
        public virtual void SendProgramResponseMessage(string response)
        {
            var msg = new ProgramRepsonseMsg();
            msg.EntityId = m_programmableBlock.EntityId;
            msg.Response = response;
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
    }

}

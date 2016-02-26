#region Using

using System;
using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using VRage.Serialization;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System.Runtime.InteropServices;
using SteamSDK;
using VRageMath.PackedVector;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.ObjectBuilders;
using Sandbox.Engine.Physics;
using VRage.ModAPI;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;
using VRage.Game.Entity;

#endregion

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncFloatingObjects
    {
        static MyFloatingObjects m_floatingObjects;

        [MessageId(10156, P2PMessageEnum.Unreliable)]
        struct MakeUnstableBatchMsg
        {
            public List<long> Entities;
        }

        class MakeUnstableBatchSerializer : ISerializer<MakeUnstableBatchMsg>
        {
            void ISerializer<MakeUnstableBatchMsg>.Serialize(ByteStream destination, ref MakeUnstableBatchMsg data)
            {
                destination.Write7BitEncodedInt(data.Entities.Count);
                for (int i = 0; i < data.Entities.Count; ++i)
                {
                    var l = data.Entities[i];
                    BlitSerializer<long>.Default.Serialize(destination, ref l);
                }
            }

            void ISerializer<MakeUnstableBatchMsg>.Deserialize(ByteStream source, out MakeUnstableBatchMsg data)
            {
                data = new MakeUnstableBatchMsg();
                int length = source.Read7BitEncodedInt();
                data.Entities = new List<long>(length);
                for (int i = 0; i < length; ++i)
                {
                    long id;
                    BlitSerializer<long>.Default.Deserialize(source, out id);
                    data.Entities.Add(id);
                }
            }
        }

        static MySyncFloatingObjects()
        {
            MySyncLayer.RegisterMessage<MakeUnstableBatchMsg>(OnMakeUnstableBatchSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success, new MakeUnstableBatchSerializer());
        }        

        public MySyncFloatingObjects(MyFloatingObjects floatingObjects)
        {
            m_floatingObjects = floatingObjects;
        }
        
        public void SendMakeUnstable(List<long> objects)
        {
            var msg = new MakeUnstableBatchMsg()
            {
                Entities = objects,
            };
            MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        private static void OnMakeUnstableBatchSuccess(ref MakeUnstableBatchMsg msg, MyNetworkClient sender)
        {
            if (m_floatingObjects != null)
                m_floatingObjects.MakeUnstable(msg.Entities);
        }
    }
}

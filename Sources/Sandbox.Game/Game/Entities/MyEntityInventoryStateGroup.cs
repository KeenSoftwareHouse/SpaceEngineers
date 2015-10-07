using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;

namespace Sandbox.Game.Entities
{

    class MyEntityInventoryStateGroup : IMyStateGroup
    {
        struct InventoryPartInfo
        {
            public bool AllItemsSend;
            public int StartItemIndex;
            public int NumItems;
        }

        class InventoryClientData
        {
            public InventoryPartInfo MainSendingInfo;
            public bool Dirty;
            public Dictionary<byte, InventoryPartInfo> SendPackets = new Dictionary<byte, InventoryPartInfo>();
            public List<InventoryPartInfo> FailedIncompletePackets = new List<InventoryPartInfo>();
        }

        MyInventory Inventory { get; set; }

        readonly int m_inventoryIndex;

        Dictionary<ulong, InventoryClientData> m_clientInventoryUpdate;

        List<MyPhysicalInventoryItem> m_items;

        HashSet<int> m_recievedInventoryParts;

        Action<MyInventoryBase> m_inventoryChangedDelegate;

        public StateGroupEnum GroupType { get { return StateGroupEnum.Inventory; } }

        public MyEntityInventoryStateGroup(MyInventory entity, bool attach)
        {
            m_inventoryChangedDelegate = InventoryChanged;
            Inventory = entity;
            if (attach)
            {
                Inventory.ContentsChanged += m_inventoryChangedDelegate;
            }
        }

        void InventoryChanged(MyInventoryBase obj)
        {
            if (m_clientInventoryUpdate == null)
            {
                return;
            }
            foreach (var clientData in m_clientInventoryUpdate)
            {
                m_clientInventoryUpdate[clientData.Key].Dirty = true;
                m_clientInventoryUpdate[clientData.Key].MainSendingInfo.StartItemIndex = 0;
                m_clientInventoryUpdate[clientData.Key].MainSendingInfo.NumItems = 0;
                //when sending inventory over again don't care about old messages
                //inventory will be overwritten;
                m_clientInventoryUpdate[clientData.Key].SendPackets.Clear();
                m_clientInventoryUpdate[clientData.Key].FailedIncompletePackets.Clear();
            }
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            if (m_clientInventoryUpdate == null)
            {
                m_clientInventoryUpdate = new Dictionary<ulong, InventoryClientData>();
            }

            InventoryClientData data;
            if (m_clientInventoryUpdate.TryGetValue(forClient.EndpointId.Value, out data) == false)
            {
                m_clientInventoryUpdate[forClient.EndpointId.Value] = new InventoryClientData();
            }
            m_clientInventoryUpdate[forClient.EndpointId.Value].Dirty = true;
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            if (m_clientInventoryUpdate != null)
            {
                m_clientInventoryUpdate.Remove(forClient.EndpointId.Value);
            }
        }

        public void ClientUpdate()
        {
        }

        public float GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client)
        {
            InventoryClientData clientData = m_clientInventoryUpdate[client.EndpointId.Value];

            MyClientState state = client as MyClientState;
            if (Inventory.Owner is MyCharacter && m_clientInventoryUpdate[client.EndpointId.Value].Dirty)
            {
                MyPlayer player = MyPlayer.GetPlayerFromCharacter(Inventory.Owner as MyCharacter);
                if (player != null && player.Id.SteamId == client.EndpointId.Value)
                {
                    return 1.0f;
                }
            }

            if (m_clientInventoryUpdate[client.EndpointId.Value].Dirty && (state.Context == MyClientState.MyContextKind.Inventory ||
                (state.Context == MyClientState.MyContextKind.Production && Inventory.Owner is MyAssembler)))
            {
                return Inventory.GetPriorityStateGroup(client);
            }
            return 0;
        }

        public void Serialize(BitStream stream, MyClientStateBase forClient, byte packetId, int maxBitPosition)
        {
            if (stream.Writing)
            {
                InventoryClientData clientData = m_clientInventoryUpdate[forClient.EndpointId.Value];
                if (clientData.FailedIncompletePackets.Count > 0)
                {
                    InventoryPartInfo failedPacket = clientData.FailedIncompletePackets[0];
                    clientData.FailedIncompletePackets.RemoveAtFast(0);

                    InventoryPartInfo reSendPacket = WriteInventory(ref failedPacket, stream, packetId, maxBitPosition, true);
                    clientData.SendPackets[packetId] = reSendPacket;
                }
                else
                {
                    clientData.MainSendingInfo = WriteInventory(ref clientData.MainSendingInfo, stream, packetId, maxBitPosition);
                    clientData.SendPackets[packetId] = clientData.MainSendingInfo;

                    List<MyPhysicalInventoryItem> items = Inventory.GetItems();

                    if (clientData.MainSendingInfo.StartItemIndex + clientData.MainSendingInfo.NumItems >= items.Count)
                    {
                        clientData.MainSendingInfo.StartItemIndex = 0;
                        clientData.MainSendingInfo.NumItems = 0;
                        clientData.Dirty = false;
                    }
                }
            }
            else
            {
                ReadInventory(stream);
            }
        }

        private void ReadInventory(BitStream stream)
        {
            if (m_items == null)
            {
                m_items = new List<MyPhysicalInventoryItem>();
            }
            if (m_recievedInventoryParts == null)
            {
                m_recievedInventoryParts = new HashSet<int>();
            }
            m_items.Clear();
            bool newTransfer;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out newTransfer);

            if (newTransfer)
            {
                Inventory.ClearItems();
                m_recievedInventoryParts.Clear();
            }

            int numItems;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out numItems);
            for (int i = 0; i < numItems; ++i)
            {
                MyPhysicalInventoryItem item;
                VRage.Serialization.MySerializer.CreateAndRead(stream, out item, MyObjectBuilderSerializer.Dynamic);
                m_items.Add(item);
            }

            int startItemIndex;
            VRage.Serialization.MySerializer.CreateAndRead(stream, out startItemIndex);
            if (m_recievedInventoryParts.Contains(startItemIndex) == false)
            {
                m_recievedInventoryParts.Add(startItemIndex);
                Inventory.ReplaceItems(startItemIndex, m_items);
                Inventory.OnContentsChanged();
            }
        }

        private InventoryPartInfo WriteInventory(ref InventoryPartInfo packetInfo, BitStream stream, byte packetId, int maxBitPosition, bool resend = false)
        {
            if (m_items == null)
            {
                m_items = new List<MyPhysicalInventoryItem>();
            }

            Console.WriteLine(String.Format("sending: {0}, {1}", packetId, Inventory.Owner.ToString()));
            InventoryPartInfo sendPacketInfo = new InventoryPartInfo();
            sendPacketInfo.AllItemsSend = true;

            List<MyPhysicalInventoryItem> items = Inventory.GetItems();
            int numItems = items.Count;

            int sentItems = packetInfo.StartItemIndex + (resend ? 0 : packetInfo.NumItems);
            sendPacketInfo.StartItemIndex = sentItems;

            bool startNewTransfer = (resend == false) && sentItems == 0;
            VRage.Serialization.MySerializer.Write(stream, ref startNewTransfer);
            int startStreamPosition = stream.BitPosition;
            VRage.Serialization.MySerializer.Write(stream, ref numItems);

            int maxNumItems = (resend ? packetInfo.NumItems : numItems);
            for (; sentItems < numItems; ++sentItems)
            {
                MyPhysicalInventoryItem item = items[sentItems];
                m_items.Add(item);
                VRage.Serialization.MySerializer.Write(stream, ref item, MyObjectBuilderSerializer.Dynamic);
                if (stream.BitPosition > maxBitPosition)
                {
                    m_items.RemoveAt(m_items.Count - 1);
                    break;
                }
                else if (sentItems >= maxNumItems)
                {
                    break;
                }

            }

            if (m_items.Count < numItems)
            {
                sendPacketInfo.AllItemsSend = false;
                stream.SetBitPositionWrite(startStreamPosition);
                numItems = m_items.Count;
                VRage.Serialization.MySerializer.Write(stream, ref numItems);

                for (int i = 0; i < m_items.Count; ++i)
                {
                    MyPhysicalInventoryItem item = m_items[i];
                    VRage.Serialization.MySerializer.Write(stream, ref item, MyObjectBuilderSerializer.Dynamic);
                }
            }

            VRage.Serialization.MySerializer.Write(stream, ref sendPacketInfo.StartItemIndex);
            sendPacketInfo.NumItems = m_items.Count;
            m_items.Clear();
            return sendPacketInfo;
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
            Console.WriteLine(String.Format("delivery: {0}, {1}", packetId, delivered));
            InventoryClientData clientData = m_clientInventoryUpdate[forClient.EndpointId.Value];
            InventoryPartInfo packetInfo;
            if (clientData.SendPackets.TryGetValue(packetId, out packetInfo))
            {
                if (delivered == false)
                {
                    if (packetInfo.AllItemsSend)
                    {
                        clientData.Dirty = true;
                        clientData.MainSendingInfo.StartItemIndex = 0;
                        clientData.MainSendingInfo.NumItems = 0;
                        clientData.SendPackets.Clear();
                        clientData.FailedIncompletePackets.Clear();
                    }
                    else
                    {
                        clientData.FailedIncompletePackets.Add(packetInfo);
                    }
                }

                clientData.SendPackets.Remove(packetId);
            }
        }
    }
}

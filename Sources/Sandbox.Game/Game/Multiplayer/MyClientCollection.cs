using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;


namespace Sandbox.Game.Multiplayer
{
    public class MyClientCollection
    {
        private Dictionary<ulong, MyNetworkClient> m_clients = new Dictionary<ulong, MyNetworkClient>();
        private ulong m_localSteamId;

        public Action<ulong> ClientAdded;
        public Action<ulong> ClientRemoved;

        public int Count { get { return m_clients.Count; } }

        public MyNetworkClient LocalClient
        {
            get
            {
                MyNetworkClient retval = null;
                m_clients.TryGetValue(m_localSteamId, out retval);
                return retval;
            }
        }

        public void SetLocalSteamId(ulong localSteamId, bool createLocalClient = false)
        {
            m_localSteamId = localSteamId;

            if (createLocalClient == true && m_clients.ContainsKey(m_localSteamId) == false)
            {
                AddClient(m_localSteamId);
            }

            Debug.Assert(LocalClient != null, "Missing local client after the local ID was set!");
        }

        public void Clear()
        {
            m_clients.Clear();
        }

        public bool TryGetClient(ulong steamId, out MyNetworkClient client)
        {
            client = null;
            return m_clients.TryGetValue(steamId, out client);
        }

        public bool HasClient(ulong steamId)
        {
            return m_clients.ContainsKey(steamId);
        }

        public MyNetworkClient AddClient(ulong steamId)
        {
            System.Diagnostics.Debug.Assert(!m_clients.ContainsKey(steamId), "Client already present!");
            if (m_clients.ContainsKey(steamId))
            {
                MyLog.Default.WriteLine("ERROR: Added client already present: " + m_clients[steamId].DisplayName);
                return m_clients[steamId];
            }

            var client = new MyNetworkClient(steamId);
            m_clients.Add(steamId, client);

            RaiseClientAdded(steamId);

            return client;
        }

        public void RemoveClient(ulong steamId)
        {
            MyNetworkClient client;
            m_clients.TryGetValue(steamId, out client);            
            //This is ok when there is a lag on server
            //System.Diagnostics.Debug.Assert(client != null, "Client not present!");
            if (client == null)
            {
                MyLog.Default.WriteLine("ERROR: Removed client not present: " + steamId);
                return;
            }

            m_clients.Remove(steamId);

            RaiseClientRemoved(steamId);
        }

        private void RaiseClientAdded(ulong steamId)
        {
            var handler = ClientAdded;
            if (handler != null)
                handler(steamId);
        }

        private void RaiseClientRemoved(ulong steamId)
        {
            var handler = ClientRemoved;
            if (handler != null)
                handler(steamId);
        }

        public Dictionary<ulong, MyNetworkClient>.ValueCollection GetClients()
        {
            return m_clients.Values;
        }
    }
}

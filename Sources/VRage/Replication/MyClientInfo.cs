using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Replication;

namespace VRage.Network
{
    public struct MyClientInfo
    {
        private readonly MyReplicationServer.ClientData m_clientData;

        public MyClientStateBase State { get { return m_clientData.State; } }
        public EndpointId EndpointId { get { return m_clientData.State.EndpointId; } }
        public float PriorityMultiplier { get { return m_clientData.PriorityMultiplier; } }

        internal MyClientInfo(MyReplicationServer.ClientData clientData)
        {
            m_clientData = clientData;
        }

        /// <summary>
        /// Gets priority of different replicable.
        /// E.g. can be used to get priority of grid when calling GetPriority on cube block.
        /// </summary>
        public float GetPriority(IMyReplicable replicable)
        {
            while(replicable.HasToBeChild)
            {
                var parent = replicable.GetParent();
                if (parent == null)
                    break;
                else
                    replicable = parent;
            }

            MyReplicableClientData data;
            return m_clientData.Replicables.TryGetValue(replicable, out data) ? data.Priority : 0;
        }

        public bool HasReplicable(IMyReplicable replicable)
        {
            return m_clientData.Replicables.ContainsKey(replicable);
        }

        public bool IsReplicableReady(IMyReplicable replicable)
        {
            return m_clientData.IsReplicableReady(replicable);
        }
    }
}

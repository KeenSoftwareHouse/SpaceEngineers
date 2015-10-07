#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    class MyRadioBroadcaster : MyDataBroadcaster
    {
        public MySyncRadioBroadcaster SyncObject;
        public Action OnBroadcastRadiusChanged;
        
        float m_broadcastRadius;

        public MyRadioBroadcaster(MyEntity parent, float broadcastRadius = 100)
        {
            SyncObject = new MySyncRadioBroadcaster(this);
            Parent = parent;
            m_broadcastRadius = broadcastRadius;
            parent.OnClose += parent_OnClose;
        }

        void parent_OnClose(MyEntity obj)
        {
            Enabled = false;
        }

        bool m_enabled = false;
        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                {
                    if (value)
                        MyRadioBroadcasters.AddBroadcaster(this);
                    else
                        MyRadioBroadcasters.RemoveBroadcaster(this);

                    m_enabled = value;
                }
            }
        }
        public bool WantsToBeEnabled = true;

        public void MoveBroadcaster()
        {
            MyRadioBroadcasters.MoveBroadcaster(this);
        }

        #region IMyRadioBroadcaster

        public float BroadcastRadius 
        { 
            get { return m_broadcastRadius; }
            set
            {
                if (m_broadcastRadius != value)
                {
                    m_broadcastRadius = value;

                    if (m_enabled)
                    {
                        MyRadioBroadcasters.RemoveBroadcaster(this);
                        MyRadioBroadcasters.AddBroadcaster(this);
                    }

                    var handler = OnBroadcastRadiusChanged;
                    if (handler != null)
                        handler();
                }
            }
        }

        public int m_radioProxyID = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
        public int RadioProxyID
        {
            get { return m_radioProxyID; }
            set { m_radioProxyID = value; }
        }

        public static HashSet<MyDataBroadcaster> GetGridRelayedBroadcasters(MyCubeGrid grid)
        {
            return GetGridRelayedBroadcasters(grid, MySession.LocalPlayerId);
        }

        public static HashSet<MyDataBroadcaster> GetGridRelayedBroadcasters(MyCubeGrid grid, long playerId)
        {
            HashSet<MyDataBroadcaster> output = new HashSet<MyDataBroadcaster>();
            HashSet<MyDataReceiver> playerReceivers = MyDataReceiver.GetGridRadioReceivers(grid, playerId);
            foreach (var receiver in playerReceivers)
            {
                var relayedBroadcasters = receiver.GetRelayedBroadcastersForPlayer(playerId);
                output.UnionWith(relayedBroadcasters);
            }
            return output;
        }
        #endregion
    }
}

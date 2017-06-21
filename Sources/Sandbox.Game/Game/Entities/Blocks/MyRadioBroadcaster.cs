#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage.Game;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    public class MyRadioBroadcaster : MyDataBroadcaster
    {
        public Action OnBroadcastRadiusChanged;
        
        float m_broadcastRadius;

        public MyRadioBroadcaster(float broadcastRadius = 100)
        {
            m_broadcastRadius = broadcastRadius;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
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
                    Debug.Assert(MySandboxGame.Static.UpdateThread == Thread.CurrentThread,"addidng antena from different thread !!!");
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

        public int m_radioProxyID = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
        public int RadioProxyID
        {
            get { return m_radioProxyID; }
            set { m_radioProxyID = value; }
        }

        public static HashSet<MyDataBroadcaster> GetGridRelayedBroadcasters(MyCubeGrid grid)
        {
            return GetGridRelayedBroadcasters(grid, MySession.Static.LocalPlayerId);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;

namespace Sandbox.Game.Multiplayer
{
    // CH: TODO: Synchronize and serialize!
    // CH: TODO: We will ideally move all the stuff from the AllPlayerData in MyPlayerCollection here
    public class MyPerPlayerData
    {
        private Dictionary<PlayerId, Dictionary<MyStringId, object>> m_playerDataByPlayerId;

        public MyPerPlayerData()
        {
            m_playerDataByPlayerId = new Dictionary<PlayerId, Dictionary<MyStringId, object>>(PlayerId.Comparer);
        }

        public void SetPlayerData<T>(PlayerId playerId, MyStringId dataId, T data)
        {
            var playerData = GetOrAllocatePlayerDataDictionary(playerId);
            playerData[dataId] = data;
        }

        public T GetPlayerData<T>(PlayerId playerId, MyStringId dataId, T defaultValue)
        {
            Dictionary<MyStringId, object> playerData = null;
            if (!m_playerDataByPlayerId.TryGetValue(playerId, out playerData))
            {
                return defaultValue;
            }

            object data = null;
            if (!playerData.TryGetValue(dataId, out data))
            {
                return defaultValue;
            }

            return (T)data;
        }

        private Dictionary<MyStringId, object> GetOrAllocatePlayerDataDictionary(PlayerId playerId)
        {
            Dictionary<MyStringId, object> playerData = null;
            if (!m_playerDataByPlayerId.TryGetValue(playerId, out playerData))
            {
                playerData = new Dictionary<MyStringId, object>(MyStringId.Comparer);
                m_playerDataByPlayerId[playerId] = playerData;
            }

            return playerData;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using VRage.Utils;
using Sandbox.Engine.Networking;
using VRage.Network;
using VRage;

namespace Sandbox.Engine.Multiplayer
{
    public class MyMultiplayerBattleData
    {
        private readonly MyMultiplayerBase m_multiplayer;

        private readonly Dictionary<MyStringHash, string> m_mapKeyToValue = new Dictionary<MyStringHash, string>(MyStringHash.Comparer);

        private static readonly MyStringHash BattleRemainingTimeTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleRemainingTimeTag);
        private static readonly MyStringHash BattleCanBeJoinedTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleCanBeJoinedTag);
        private static readonly MyStringHash BattleWorldWorkshopIdTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleWorldWorkshopIdTag);
        private static readonly MyStringHash BattleFaction1MaxBlueprintPointsTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction1MaxBlueprintPointsTag);
        private static readonly MyStringHash BattleFaction2MaxBlueprintPointsTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction2MaxBlueprintPointsTag);
        private static readonly MyStringHash BattleFaction1BlueprintPointsTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction1BlueprintPointsTag);
        private static readonly MyStringHash BattleFaction2BlueprintPointsTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction2BlueprintPointsTag);
        private static readonly MyStringHash BattleMapAttackerSlotsCountTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleMapAttackerSlotsCountTag);
        private static readonly MyStringHash BattleFaction1IdTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction1IdTag);
        private static readonly MyStringHash BattleFaction2IdTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction2IdTag);
        private static readonly MyStringHash BattleFaction1SlotTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction1SlotTag);
        private static readonly MyStringHash BattleFaction2SlotTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction2SlotTag);
        private static readonly MyStringHash BattleFaction1ReadyTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction1ReadyTag);
        private static readonly MyStringHash BattleFaction2ReadyTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleFaction2ReadyTag);
        private static readonly MyStringHash BattleTimeLimitTagHash = MyStringHash.GetOrCompute(MyMultiplayer.BattleTimeLimitTag);

        public float BattleRemainingTime
        {
            get { return GetFloatValue(BattleRemainingTimeTagHash, 0); }
            set { KeyValueChangedRequest(BattleRemainingTimeTagHash, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public bool BattleCanBeJoined
        {
            get { return GetBoolValue(BattleCanBeJoinedTagHash, false); }
            set { KeyValueChangedRequest(BattleCanBeJoinedTagHash, value.ToString()); }
        }

        public ulong BattleWorldWorkshopId
        {
            get { return GetULongValue(BattleWorldWorkshopIdTagHash, 0); }
            set { KeyValueChangedRequest(BattleWorldWorkshopIdTagHash, value.ToString()); }
        }

        public int BattleFaction1MaxBlueprintPoints
        {
            get { return GetIntValue(BattleFaction1MaxBlueprintPointsTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction1MaxBlueprintPointsTagHash, value.ToString()); }
        }

        public int BattleFaction2MaxBlueprintPoints
        {
            get { return GetIntValue(BattleFaction2MaxBlueprintPointsTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction2MaxBlueprintPointsTagHash, value.ToString()); }
        }

        public int BattleFaction1BlueprintPoints
        {
            get { return GetIntValue(BattleFaction1BlueprintPointsTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction1BlueprintPointsTagHash, value.ToString()); }
        }

        public int BattleFaction2BlueprintPoints
        {
            get { return GetIntValue(BattleFaction2BlueprintPointsTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction2BlueprintPointsTagHash, value.ToString()); }
        }

        public int BattleMapAttackerSlotsCount
        {
            get { return GetIntValue(BattleMapAttackerSlotsCountTagHash, 0); }
            set { KeyValueChangedRequest(BattleMapAttackerSlotsCountTagHash, value.ToString()); }
        }

        public long BattleFaction1Id
        {
            get { return GetLongValue(BattleFaction1IdTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction1IdTagHash, value.ToString()); }
        }

        public long BattleFaction2Id
        {
            get { return GetLongValue(BattleFaction2IdTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction2IdTagHash, value.ToString()); }
        }

        public int BattleFaction1Slot
        {
            get { return GetIntValue(BattleFaction1SlotTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction1SlotTagHash, value.ToString()); }
        }

        public int BattleFaction2Slot
        {
            get { return GetIntValue(BattleFaction2SlotTagHash, 0); }
            set { KeyValueChangedRequest(BattleFaction2SlotTagHash, value.ToString()); }
        }

        public bool BattleFaction1Ready
        {
            get { return GetBoolValue(BattleFaction1ReadyTagHash, false); }
            set { KeyValueChangedRequest(BattleFaction1ReadyTagHash, value.ToString()); }
        }

        public bool BattleFaction2Ready
        {
            get { return GetBoolValue(BattleFaction2ReadyTagHash, false); }
            set { KeyValueChangedRequest(BattleFaction2ReadyTagHash, value.ToString()); }
        }

        public int BattleTimeLimit
        {
            get { return GetIntValue(BattleTimeLimitTagHash, 0); }
            set { KeyValueChangedRequest(BattleTimeLimitTagHash, value.ToString()); }
        }

        public MyMultiplayerBattleData(MyMultiplayerBase multiplayer)
        {
            m_multiplayer = multiplayer;
            if (Sync.IsServer == false)
            {
                multiplayer.SyncLayer.TransportLayer.Register(MyMessageId.WORLD_BATTLE_DATA, OnValueChanged);
            }
        }

        private void KeyValueChangedRequest(MyStringHash key, string value)
        {
            var msg = new KeyValueDataMsg();
            msg.Key = key;
            msg.Value = value;

            OnKeyValueChanged(ref msg);
        }

        private void OnKeyValueChanged(ref KeyValueDataMsg msg)
        {
            m_mapKeyToValue[msg.Key] = msg.Value;
        }

        private float GetFloatValue(MyStringHash key, float defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                float val;
                if (float.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    return val;
            }

            return defValue;
        }

        private int GetIntValue(MyStringHash key, int defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                int val;
                if (int.TryParse(strVal, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                    return val;
            }

            return defValue;
        }

        private DateTime GetDateTimeValue(MyStringHash key, DateTime defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                DateTime val;
                if (DateTime.TryParse(strVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out val))
                    return val;
            }

            return defValue;
        }

        private long GetLongValue(MyStringHash key, long defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                long val;
                if (long.TryParse(strVal, out val))
                    return val;
            }

            return defValue;
        }

        private ulong GetULongValue(MyStringHash key, ulong defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                ulong val;
                if (ulong.TryParse(strVal, out val))
                    return val;
            }

            return defValue;
        }

        private bool GetBoolValue(MyStringHash key, bool defValue)
        {
            string strVal;
            if (m_mapKeyToValue.TryGetValue(key, out strVal))
            {
                bool val;
                if (bool.TryParse(strVal, out val))
                    return val;
            }

            return defValue;
        }

        public void LoadData(List<KeyValueDataMsg> keyValueList)
        {
            if (keyValueList == null)
                return;

            foreach (var keyValue in keyValueList)
            {
                m_mapKeyToValue[keyValue.Key] = keyValue.Value;
            }
        }

        public List<KeyValueDataMsg> SaveData()
        {
            List<KeyValueDataMsg> keyValueList = new List<KeyValueDataMsg>();
            foreach (var pair in m_mapKeyToValue)
            {
                KeyValueDataMsg keyValue = new KeyValueDataMsg();
                keyValue.Key = pair.Key;
                keyValue.Value = pair.Value;
                keyValueList.Add(keyValue);
            }

            return keyValueList;
        }

        void OnValueChanged(MyPacket packet)
        {

        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;

using VRageMath;
using Sandbox.Engine.Multiplayer;
using VRage;
using Sandbox.Game.Gui;
using VRage.Utils;
using VRage.FileSystem;
using VRage.Game;

namespace Sandbox.Game.Screens.Helpers
{
    public enum MyBattleMapType
    {
        SAVE,
        OFFICIAL,
        SUBSCRIBED
    }

    public class MyBattleMapInfo
    {
        public MyBattleMapType MapType;

        public string SessionPath;
        // World info for map type SAVE and OFFICIAL
        public MyWorldInfo WorldInfo;

#if !XB1 // XB1_NOWORKSHOP
        // Subscribed info for type SUBSCRIBED.
        public MySteamWorkshop.SubscribedItem SubscribedWorld;
#endif // !XB1
        public ulong BattlePoints;
    }

    public class MyBattleLoadListResult : IMyAsyncResult
    {
        public List<MyBattleMapInfo> AvailableBattleMaps = new List<MyBattleMapInfo>();
        public bool ContainsCorruptedWorlds;

        public bool IsCompleted { get { return this.Task.IsComplete; } }
        public Task Task
        {
            get;
            private set;
        }

        private readonly string m_officialBattlesPath;
        private readonly string m_workshopBattlesPath;


        public MyBattleLoadListResult(string officialBattlesPath, string workshopBattlesPath)
        {
            Debug.Assert(officialBattlesPath != null);

            m_officialBattlesPath = officialBattlesPath;
            m_workshopBattlesPath = workshopBattlesPath;

            Task = Parallel.Start(() => LoadListAsync());
        }

        private void LoadListAsync()
        {
            // Default saves
            List<Tuple<string, MyWorldInfo>> availableSaves = MyLocalCache.GetAvailableWorldInfos();

            // Battle original maps
            var availableBattles = GetAvailableOfficialBattlesInfos();
            availableSaves.AddList(availableBattles);

            ContainsCorruptedWorlds = false;

            StringBuilder corruptedWorlds = new StringBuilder();

            foreach (var pair in availableSaves)
            {
                Debug.Assert(pair != null);
                if (pair.Item2 == null)
                {
                    corruptedWorlds.Append(Path.GetFileNameWithoutExtension(pair.Item1)).Append("\n");
                    ContainsCorruptedWorlds = true;
                }
            }

            if (ContainsCorruptedWorlds)
            {
                availableSaves.RemoveAll(x => x == null || x.Item2 == null);
                if (MyLog.Default != null)
                {
                    MyLog.Default.WriteLine("Corrupted worlds: ");
                    MyLog.Default.WriteLine(corruptedWorlds.ToString());
                }
            }

            // Prepare battle maps
            AddBattleMaps(availableSaves, AvailableBattleMaps, m_officialBattlesPath, m_workshopBattlesPath);
        }

        private List<Tuple<string, MyWorldInfo>> GetAvailableOfficialBattlesInfos()
        {
            MySandboxGame.Log.WriteLine("Loading official castle siege worlds - START");
            var result = new List<Tuple<string, MyWorldInfo>>();
            using (MySandboxGame.Log.IndentUsing(LoggingOptions.ALL))
            {
                MyLocalCache.GetWorldInfoFromDirectory(m_officialBattlesPath, result);
            }
            MySandboxGame.Log.WriteLine("Loading official castle siege worlds - END");
            return result;
        }

        public static void AddBattleMaps(List<Tuple<string, MyWorldInfo>> availableMaps, List<MyBattleMapInfo> outBattleMaps, string officialBattlesPath, string workshopBattlesPath)
        {
            for (int i = 0; i < availableMaps.Count; ++i)
            {
                var save = availableMaps[i];
                ulong dummySizeInBytes;
                var checkpoint = MyLocalCache.LoadCheckpoint(save.Item1, out dummySizeInBytes);
                if (checkpoint == null)
                    continue;

                foreach (var component in checkpoint.SessionComponents)
                {
                    var battleComponent = component as MyObjectBuilder_BattleSystemComponent;
                    if (battleComponent != null && battleComponent.IsCastleSiegeMap)
                    {
                        MyBattleMapInfo battleMapInfo = new MyBattleMapInfo();
                        battleMapInfo.WorldInfo = save.Item2;
                        battleMapInfo.SessionPath = save.Item1;
                        battleMapInfo.BattlePoints = battleComponent.Points;
                        battleMapInfo.MapType = MyBattleMapType.SAVE;

                        if (battleMapInfo.SessionPath.StartsWith(officialBattlesPath))
                            battleMapInfo.MapType = MyBattleMapType.OFFICIAL;
                        else if (workshopBattlesPath != null && battleMapInfo.SessionPath.StartsWith(workshopBattlesPath))
                            battleMapInfo.MapType = MyBattleMapType.SUBSCRIBED;

                        outBattleMaps.Add(battleMapInfo);
                        break;
                    }
                }
            }
        }
    }
}

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
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;

using VRageMath;
using Sandbox.Engine.Multiplayer;
using VRage;
using Sandbox.Game.Gui;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    public class MyLoadListResult : IMyAsyncResult
    {
        public bool IsCompleted { get { return this.Task.IsComplete; } }
        public Task Task
        {
            get;
            private set;
        }

        public List<Tuple<string, MyWorldInfo>> AvailableSaves = new List<Tuple<string, MyWorldInfo>>();
        public bool ContainsCorruptedWorlds;

        public MyLoadListResult(bool missions = false, bool appendBattleMaps = false)
        {
            Task = Parallel.Start(() => LoadListAsync(missions, appendBattleMaps));
        }

        private void LoadListAsync(bool missions, bool appendBattleMaps)
        {
            if (missions)
            {
                AvailableSaves = MyLocalCache.GetAvailableMissionInfos();
            }
            else
            {
                AvailableSaves = MyLocalCache.GetAvailableWorldInfos();
                if (appendBattleMaps) 
                {
                    var availableBattles = MyLocalCache.GetAvailableBattlesInfos();
                    AvailableSaves.AddList(availableBattles);
                }
            }
            ContainsCorruptedWorlds = false;

            StringBuilder corruptedWorlds = new StringBuilder();

            foreach (var pair in AvailableSaves)
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
                AvailableSaves.RemoveAll(x => x == null || x.Item2 == null);
                MyLog.Default.WriteLine("Corrupted worlds: ");
                MyLog.Default.WriteLine(corruptedWorlds.ToString());
            }

            if (AvailableSaves.Count != 0)
                AvailableSaves.Sort((a, b) => b.Item2.LastLoadTime.CompareTo(a.Item2.LastLoadTime));

            VerifyUniqueWorldID(AvailableSaves);
        }

        [Conditional("DEBUG")]
        private void VerifyUniqueWorldID(List<Tuple<string, MyWorldInfo>> availableWorlds)
        {
            if (MyLog.Default == null)
                return;

            HashSet<string> worldIDs = new HashSet<string>();
            foreach (var item in availableWorlds)
            {
                var meta = item.Item2;
                if (worldIDs.Contains(item.Item1))
                {
                    MyLog.Default.WriteLine(string.Format("Non-unique WorldID detected. WorldID = {0}; World Folder Path = '{2}', World Name = '{1}'",
                        item.Item1, meta.SessionName, item.Item1));
                }
                worldIDs.Add(item.Item1);
            }
        }
    }
}

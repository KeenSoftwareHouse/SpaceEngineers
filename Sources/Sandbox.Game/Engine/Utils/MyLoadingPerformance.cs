using System;
using System.Collections.Generic;
using System.Diagnostics;

using Sandbox;

namespace Sandbox.Engine.Utils
{
    class MyLoadingPerformance
    {
        #region Singleton

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static MyLoadingPerformance Instance
        {
            get { return m_instance ?? (m_instance = new MyLoadingPerformance()); }
        }

        static MyLoadingPerformance m_instance;

        #endregion

        public string LoadingName { get; set; }

        private Dictionary<uint, Tuple<int, string>> m_voxelCounts = new Dictionary<uint, Tuple<int, string>>(); 
        private TimeSpan m_loadingTime;

        public bool IsTiming { get; private set; }

        Stopwatch m_stopwatch;

        private void Reset()
        {
            LoadingName = null;
            m_loadingTime = TimeSpan.Zero;
            m_voxelCounts.Clear();
        }

        public void StartTiming()
        {
            if (IsTiming)
            {
                return;
            }

            Reset();

            IsTiming = true;
            m_stopwatch = Stopwatch.StartNew();
        }

        public void AddVoxelHandCount(int count,uint entityID, string name)
        {
            if (IsTiming)
            {
                if(!m_voxelCounts.ContainsKey(entityID)) m_voxelCounts.Add(entityID,new Tuple<int, string>(count,name));
            }
        }

        public void FinishTiming()
        {
            m_stopwatch.Stop();
            IsTiming = false;
            m_loadingTime = m_stopwatch.Elapsed;
            WriteToLog();
        }

        public void WriteToLog()
        {
            MySandboxGame.Log.WriteLine("LOADING REPORT FOR: " + LoadingName);
            MySandboxGame.Log.IncreaseIndent();
            {
                MySandboxGame.Log.WriteLine("Loading time: " + m_loadingTime);
                MySandboxGame.Log.IncreaseIndent();
                {
                    foreach (var voxelCount in m_voxelCounts)
                    {
                        if (voxelCount.Value.Item1 > 0) MySandboxGame.Log.WriteLine("Asteroid: " + voxelCount.Key + " voxel hands: " + voxelCount.Value.Item1+ ". Voxel File: "+voxelCount.Value.Item2);
                    }
                }
                MySandboxGame.Log.DecreaseIndent();
            }
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("END OF LOADING REPORT");
        }
    }
}
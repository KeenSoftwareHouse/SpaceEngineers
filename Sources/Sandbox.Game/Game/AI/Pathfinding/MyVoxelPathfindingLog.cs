using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRage.FileSystem;
using VRage.Game;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelPathfindingLog : IMyPathfindingLog
    {
        private abstract class Operation
        {
            public abstract void Perform();
        }

        private class NavMeshOp : Operation
        {
            string m_navmeshName;
            bool m_isAddition;
            Vector3I m_cellCoord;

            public NavMeshOp(string navmeshName, bool addition, Vector3I cellCoord)
            {
                m_navmeshName = navmeshName;
                m_isAddition = addition;
                m_cellCoord = cellCoord;
            }

            public override void Perform()
            {
                var map = MySession.Static.VoxelMaps.TryGetVoxelMapByNameStart(m_navmeshName.Split('-')[0]);
                Debug.Assert(map != null);
                if (map == null) return;

                var navmesh = MyCestmirPathfindingShorts.Pathfinding.VoxelPathfinding.GetVoxelMapNavmesh(map);
                Debug.Assert(navmesh != null);
                if (navmesh == null) return;

                if (m_isAddition)
                {
                    navmesh.AddCellDebug(m_cellCoord);
                }
                else
                {
                    navmesh.RemoveCellDebug(m_cellCoord);
                }
            }
        }

        private class VoxelWriteOp : Operation
        {
            string m_voxelName;
            string m_data;
            MyStorageDataTypeFlags m_dataType;
            Vector3I m_voxelMin;
            Vector3I m_voxelMax;

            public VoxelWriteOp(string voxelName, string data, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin, Vector3I voxelRangeMax)
            {
                m_voxelName = voxelName;
                m_data = data;
                m_dataType = dataToWrite;
                m_voxelMin = voxelRangeMin;
                m_voxelMax = voxelRangeMax;
            }

            public override void Perform()
            {
                var map = MySession.Static.VoxelMaps.TryGetVoxelMapByNameStart(m_voxelName);
                Debug.Assert(map != null);
                if (map == null) return;

                map.Storage.WriteRange(MyStorageData.FromBase64(m_data), m_dataType, m_voxelMin, m_voxelMax);
            }
        }

        string m_navmeshName = null;
        List<Operation> m_operations = new List<Operation>();
        int m_ctr = 0;
        MyLog m_log = null;

        public int Counter { get { return m_ctr; } }

        public MyVoxelPathfindingLog(string filename)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            string path = Path.Combine(MyFileSystem.UserDataPath, filename);

            if (MyFakes.REPLAY_NAVMESH_GENERATION)
            {
                StreamReader logFile = new StreamReader(path);
                string line = null;
                String nmopRegex = "NMOP: Voxel NavMesh: (\\S+) (ADD|REM) \\[X:(\\d+), Y:(\\d+), Z:(\\d+)\\]";
                String voxopRegex = "VOXOP: (\\S*) \\[X:(\\d+), Y:(\\d+), Z:(\\d+)\\] \\[X:(\\d+), Y:(\\d+), Z:(\\d+)\\] (\\S+) (\\S+)";

                while ((line = logFile.ReadLine()) != null)
                {
                    var parts = line.Split('[');
                    var matches = Regex.Matches(line, nmopRegex);
                    if (matches.Count == 1)
                    {
                        string navMeshName = matches[0].Groups[1].Value;
                        if (m_navmeshName == null)
                        {
                            m_navmeshName = navMeshName;
                        }
                        else
                        {
                            Debug.Assert(m_navmeshName == navMeshName, "Pathfinding log contains data from more than one mesh!");
                        }

                        bool addition = matches[0].Groups[2].Value == "ADD";
                        int xCoord = Int32.Parse(matches[0].Groups[3].Value);
                        int yCoord = Int32.Parse(matches[0].Groups[4].Value);
                        int zCoord = Int32.Parse(matches[0].Groups[5].Value);
                        Vector3I coord = new Vector3I(xCoord, yCoord, zCoord);

                        m_operations.Add(new NavMeshOp(m_navmeshName, addition, coord));
                        continue;
                    }

                    matches = Regex.Matches(line, voxopRegex);
                    if (matches.Count == 1)
                    {
                        string voxelMapName = matches[0].Groups[1].Value;

                        int xCoord = Int32.Parse(matches[0].Groups[2].Value);
                        int yCoord = Int32.Parse(matches[0].Groups[3].Value);
                        int zCoord = Int32.Parse(matches[0].Groups[4].Value);
                        Vector3I minCoord = new Vector3I(xCoord, yCoord, zCoord);

                        xCoord = Int32.Parse(matches[0].Groups[5].Value);
                        yCoord = Int32.Parse(matches[0].Groups[6].Value);
                        zCoord = Int32.Parse(matches[0].Groups[7].Value);
                        Vector3I maxCoord = new Vector3I(xCoord, yCoord, zCoord);

                        var flags = (MyStorageDataTypeFlags)Enum.Parse(typeof(MyStorageDataTypeFlags), matches[0].Groups[8].Value);
                        string data = matches[0].Groups[9].Value;

                        m_operations.Add(new VoxelWriteOp(voxelMapName, data, flags, minCoord, maxCoord));
                        continue;
                    }
                }
                logFile.Close();
            }
            if (MyFakes.LOG_NAVMESH_GENERATION)
            {
                m_log = new MyLog();
                m_log.Init(path, MyFinalBuildConstants.APP_VERSION_STRING);
            }
#endif // !XB1
        }

        public void Close()
        {
            if (m_log != null) m_log.Close();
        }

        public void LogCellAddition(MyVoxelNavigationMesh navMesh, Vector3I cell)
        {
            m_log.WriteLine("NMOP: " + navMesh.ToString() + " ADD " + cell.ToString());
        }

        public void LogCellRemoval(MyVoxelNavigationMesh navMesh, Vector3I cell)
        {
            m_log.WriteLine("NMOP: " + navMesh.ToString() + " REM " + cell.ToString());
        }

        public void LogStorageWrite(MyVoxelBase map, MyStorageData source, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin, Vector3I voxelRangeMax)
        {
            var str = source.ToBase64();
            m_log.WriteLine(String.Format("VOXOP: {0} {1} {2} {3} {4}", map.StorageName, voxelRangeMin, voxelRangeMax, dataToWrite, str));
        }

        public void PerformOneOperation(bool triggerPressed)
        {
            if (!triggerPressed && m_ctr > int.MaxValue) return; // Modify the condition here to stop automatic replay at some operation
            if (m_ctr >= m_operations.Count) return;
            m_operations[m_ctr].Perform();
            m_ctr++;
        }

        public void DebugDraw()
        {
            if (MyFakes.REPLAY_NAVMESH_GENERATION)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(500, 10), String.Format("Next operation: {0}/{1}", m_ctr, m_operations.Count), Color.Red, 1.0f);
            }
        }
    }
}

#region Using

using System;
using VRage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.Localization;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems.Electricity;
using VRageMath;
using Sandbox.Game.Voxels;
using Sandbox.Game.Gui;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities.VoxelMaps;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    class MyOreDetectorBase
    {
        public delegate bool CheckControlDelegate();

        public float DetectionRadius { get; set; }
        public CheckControlDelegate OnCheckControl;

        List<MyEntity> m_entitiesInRange = new List<MyEntity>();
        List<IMyDepositCell> m_depositsInRange = new List<IMyDepositCell>();

        public MyOreDetectorBase()
        {
            DetectionRadius = 50;
        }
        
        public void Update(Vector3 position)
        {
            Clear();

            if (!OnCheckControl())
                return;

            var sphere = new BoundingSphere(position, DetectionRadius);
            MyGamePruningStructure.GetAllEntitiesInSphere(ref sphere, m_entitiesInRange);

            foreach (var entity in m_entitiesInRange)
            {
                MyVoxelMap voxelMap = entity as MyVoxelMap;
                if (voxelMap == null)
                    continue;

                foreach (var oreDeposit in voxelMap.Storage.OreDeposits)
                {
                    Debug.Assert(oreDeposit != null);
                    if (oreDeposit.TotalRareOreContent > 0 &&
                        Vector3.DistanceSquared(oreDeposit.WorldCenter, position) < DetectionRadius * DetectionRadius)
                    {
                        m_depositsInRange.Add(oreDeposit);
                        MyHud.OreMarkers.RegisterMarker(oreDeposit, new MyHudEntityParams());
                    }
                }
            }

            m_entitiesInRange.Clear();
        }

        public void Clear()
        {
            foreach (MyCellStorage.DepositCell oreDeposit in m_depositsInRange)
            {
                MyHud.OreMarkers.UnregisterMarker(oreDeposit);
            }
            m_depositsInRange.Clear();
        }
    }
}

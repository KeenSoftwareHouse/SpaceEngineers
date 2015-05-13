using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using BulletXNA.BulletCollision;
using Havok;
using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Models;
//using System.Threading;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using SysUtils.Utils;
using VRage;
using VRage.Common;
using VRage.Common.Utils;
using VRageMath;
using VRageMath.PackedVector;
using Sandbox.Graphics;
using Sandbox.Definitions;
using VRageRender;
using Sandbox.Game.Entities.VoxelMaps;
using Sandbox.Game.Voxels;
using VRage.Common.Generics;

namespace Sandbox.Game.Entities.VoxelMaps
{
    partial class MyVoxelGeometry
    {
        [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 700)]
        internal class CellPool : MySessionComponentBase
        {
            static MyObjectsPool<CellData> m_pool;

            private static readonly FastResourceLock m_lock = new FastResourceLock();

            public override void LoadData()
            {
                if (m_pool == null)
                {
                    m_pool = new MyObjectsPool<CellData>(MyVoxelConstants.GEOMETRY_CELL_CACHE_SIZE);
                }
            }

            protected override void UnloadData()
            {
                Debug.Assert(m_pool.ActiveCount == 0, "There are voxel geometry cells which were not returned to the pool.");
                Debug.Assert(m_pool.BaseCapacity == m_pool.Capacity, "Voxel geometry cells needed to be allocated for this scene. Consider increasing pool capacity.");
                m_pool.TrimToBaseCapacity();
            }

            public static void Deallocate(CellData cell)
            {
                using (m_lock.AcquireExclusiveUsing())
                {
                    m_pool.Deallocate(cell);
                }
            }

            internal static CellData AllocateOrCreate()
            {
                using (m_lock.AcquireExclusiveUsing())
                {
                    CellData cell;
                    m_pool.AllocateOrCreate(out cell);
                    return cell;
                }
            }
        }
    }
}

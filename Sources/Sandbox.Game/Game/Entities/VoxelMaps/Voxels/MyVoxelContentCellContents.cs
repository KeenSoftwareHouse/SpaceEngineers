using System.Collections.Generic;
using Sandbox.Engine.Utils;
using VRage.Common.Generics;
using SysUtils.Utils;
using Sandbox.Game.World;
using Sandbox.Common;
using Sandbox.Game.Entities;
using System.Diagnostics;

//  This STATIC class is buffer of preallocated voxel contents.
//  It is used only if we are switching cell from type FULL to EMPTY or MIXED.
//  We never release cell content, even if it becomes EMPTY (so basicaly it isn't needed more).

namespace Sandbox.Game.Voxels
{
    class MyVoxelContentCellContents : IMySceneComponent
    {
        //  Preallocated cell contents. This is buffer from which we get new cell content if needed (when changing from FULL to MIXED or EMPTY)
        static MyObjectsPool<MyVoxelContentCellContent> m_pool;

        public void Load()
        {
            Profiler.Begin("MyVoxelContentCellContents.LoadData");
            if (m_pool == null) // Never reallocate
            {
                m_pool = new MyObjectsPool<MyVoxelContentCellContent>(MyVoxelConstants.PREALLOCATED_CELL_CONTENTS_COUNT);
            }
            Debug.Assert(m_pool.ActiveCount == 0);
            Profiler.End();
        }

        public void Unload()
        {
            Debug.Assert(m_pool.ActiveCount == 0, "There are voxel cell contents which were not returned to the pool.");
            m_pool.TrimToBaseCapacity();
        }

        //  Get new preallocated content from the buffer.
        //  We don't check for size, because it must be done by higher game logic (reinicializing too destroyed level).
        public static MyVoxelContentCellContent Allocate()
        {
            MyVoxelContentCellContent content;
            m_pool.AllocateOrCreate(out content);
            return content;
        }

        public static void Deallocate(MyVoxelContentCellContent item)
        {
            m_pool.Deallocate(item);
        }

    }
}

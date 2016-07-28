using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Game.Entities.Cube;
using VRage.Collections;
using System.Diagnostics;

namespace Sandbox.Game.GameSystems
{
    public partial class MyGridTerminalSystem
    {
        // Malware: I would by far preferred to have removed this hashset and only used the dictionary,
        // but I fear it might break mod scripts due to the Blocks property below...?
        readonly HashSet<MyTerminalBlock> m_blocks = new HashSet<MyTerminalBlock>();
        readonly Dictionary<long, MyTerminalBlock> m_blockTable = new Dictionary<long, MyTerminalBlock>();

        readonly List<MyBlockGroup> m_blockGroups = new List<MyBlockGroup>();

        public event Action<MyTerminalBlock> BlockAdded;
        public event Action<MyTerminalBlock> BlockRemoved;

        public event Action<MyBlockGroup> GroupAdded;
        public event Action<MyBlockGroup> GroupRemoved;

        public void Add(MyTerminalBlock block)
        {
            if (block.MarkedForClose || block.IsBeingRemoved)
                return;
            Debug.Assert(!m_blockTable.ContainsKey(block.EntityId), "Block to add is already in terminal");
            m_blockTable.Add(block.EntityId, block);
            m_blocks.Add(block);

            var handler = BlockAdded;
            if (handler != null) handler(block);
        }

        public void Remove(MyTerminalBlock block)
        {
            if (block.MarkedForClose)
                return;
            Debug.Assert(m_blockTable.ContainsKey(block.EntityId) || block.IsBeingRemoved, "Block to remove is not in terminal");
            m_blockTable.Remove(block.EntityId);
            Debug.Assert(m_blocks.Contains(block) || block.IsBeingRemoved, "Block to remove is not in terminal");
            m_blocks.Remove(block);

            for (int i = 0; i < BlockGroups.Count; i++)
            {
                var group = BlockGroups[i];
                group.Blocks.Remove(block);
                if (group.Blocks.Count == 0)
                {
                    RemoveGroup(group);
                    i--;
                }
            }

            var handler = BlockRemoved;
            if (handler != null) handler(block);
        }

        public MyBlockGroup AddUpdateGroup(MyBlockGroup group)
        {
            //Can happen on split
            //Debug.Assert(group.Blocks.Count > 0, "Empty group should not be added to system.");
            if (group.Blocks.Count == 0)
                return null;
            bool modified = false;
            for (int index = 0; index < BlockGroups.Count; index++)
            {
                var g = BlockGroups[index];
                if (g.Name.CompareTo(group.Name) == 0)
                {
                    if (group.CubeGrid != null) //change came from grid i.e. destroyed block that was in group
                    {
                        for (int i = 0; i < g.Blocks.Count; i++)
                        {
                            if (g.Blocks[i].CubeGrid == group.CubeGrid)
                            {
                                g.Blocks.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                    else //change came from gui, we clear group and add all blocks that came in
                    {
                        g.Blocks.Clear();
                    }
                    foreach (var b in group.Blocks)
                    {
                        if (g.Blocks.Contains(b))
                        {
                            continue;
                        }
                        g.Blocks.Add(b);
                    }
                    group = g;
                    modified = true;
                    break;
                }
            }

            if (!modified) //new group
            {
                var g = new MyBlockGroup(null);
                g.Name.Clear().AppendStringBuilder(group.Name);
                g.Blocks.AddList(group.Blocks);
                BlockGroups.Add(g);
                group = g;
            }

            if (GroupAdded != null)
                GroupAdded(group);
            return group;
        }

        public void RemoveGroup(MyBlockGroup group)
        {
            bool removed = false;
            if (!BlockGroups.Contains(group)) // if you delete from terminal group matches and you delete whole group
            {
                for (int index = 0; index < BlockGroups.Count; index++)
                {
                    var g = BlockGroups[index];
                    if (g.Name.CompareTo(group.Name) == 0)
                    {
                        for (int i = 0; i < g.Blocks.Count; i++)
                        {
                            var b = g.Blocks[i];
                            if (b.CubeGrid == group.CubeGrid) //remove only blocks of that grid from group
                            {
                                g.Blocks.Remove(b);
                                i--;
                            }
                        }
                        if (g.Blocks.Count == 0)
                        {
                            group = g; //group to remove
                        }
                        else
                        {
                            removed = true;
                        }
                        break;
                    }
                }
            }

            if (!removed)
                BlockGroups.Remove(group);

            if (GroupRemoved != null && group.CubeGrid == null)
                GroupRemoved(group);
        }

        // Malware: Can this one be changed so the m_blocks can be removed and only the m_blockTable remain?
        public HashSetReader<MyTerminalBlock> Blocks
        {
            get
            {
                return new HashSetReader<MyTerminalBlock>(m_blocks);
            }
        }

        public void CopyBlocksTo(List<MyTerminalBlock> result)
        {
            foreach (var block in m_blocks)
            {
                result.Add(block);
            }
        }

        public List<MyBlockGroup> BlockGroups { get { return m_blockGroups; } }

        public void UpdateGridBlocksOwnership(long ownerID)
        {
            foreach (var block in m_blocks)
            {
                block.IsAccessibleForProgrammableBlock = block.HasPlayerAccess(ownerID);
            }
        }
    }
}

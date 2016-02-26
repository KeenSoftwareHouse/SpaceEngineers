using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    #warning Check the ownership manager and fix it to use the new control system
    internal class MyCubeGridOwnershipManager
    {
        public Dictionary<long, int> PlayerOwnedBlocks;
        public Dictionary<long, int> PlayerOwnedValidBlocks;

        public List<long> BigOwners;
        public List<long> SmallOwners;
        public int MaxBlocks;
        public long gridEntityId;

        public bool NeedRecalculateOwners;

        private bool IsValidBlock(MyCubeBlock block)
        {
            return block.IsFunctional;
        }

        public void Init(MyCubeGrid grid)
        {
            PlayerOwnedBlocks = new Dictionary<long, int>();
            PlayerOwnedValidBlocks = new Dictionary<long, int>();

            BigOwners = new List<long>();
            SmallOwners = new List<long>();

            MaxBlocks = 0;
            gridEntityId = grid.EntityId;

            //Finds max blocks within owners: Slow!
            foreach (var block in grid.GetFatBlocks())
            {
                var blockOwner = block.OwnerId;

                if (blockOwner == 0)
                    continue;

                if (!PlayerOwnedBlocks.ContainsKey(blockOwner))
                    PlayerOwnedBlocks.Add(blockOwner, 0);

                PlayerOwnedBlocks[blockOwner]++;

                if (!IsValidBlock(block))
                    continue;

                if (!PlayerOwnedValidBlocks.ContainsKey(blockOwner))
                    PlayerOwnedValidBlocks.Add(blockOwner, 0);

                if (++PlayerOwnedValidBlocks[block.OwnerId] > MaxBlocks)
                    MaxBlocks = PlayerOwnedValidBlocks[blockOwner];

            }

            NeedRecalculateOwners = true;
        }

        //Fast cause usually there will be only a few owners
        internal void RecalculateOwners()
        {
            MaxBlocks = 0;

            //Recalculates maxblocks
            foreach (var key in PlayerOwnedValidBlocks.Keys)
                if (PlayerOwnedValidBlocks[key] > MaxBlocks)
                    MaxBlocks = PlayerOwnedValidBlocks[key];

            //Populates list of owners with this ammount of blocks
            BigOwners.Clear();
            foreach (var key in PlayerOwnedValidBlocks.Keys)
                if (PlayerOwnedValidBlocks[key] == MaxBlocks)
                    BigOwners.Add(key);

            //Small owners are just the keys (which always should have non-empty value)
            if (SmallOwners.Contains(MySession.Static.LocalPlayerId))
                MySession.Static.LocalHumanPlayer.RemoveGrid(gridEntityId);

            SmallOwners.Clear();
            foreach (var key in PlayerOwnedBlocks.Keys)
            {
                SmallOwners.Add(key);
                if (key == MySession.Static.LocalPlayerId)
                    MySession.Static.LocalHumanPlayer.AddGrid(gridEntityId);
            }
        }

        public void ChangeBlockOwnership(MyCubeBlock block, long oldOwner, long newOwner)
        {
            DecreaseValue(ref PlayerOwnedBlocks, oldOwner);
            IncreaseValue(ref PlayerOwnedBlocks, newOwner);
            if (IsValidBlock(block))
            {
                DecreaseValue(ref PlayerOwnedValidBlocks, oldOwner);
                IncreaseValue(ref PlayerOwnedValidBlocks, newOwner);
            }

            NeedRecalculateOwners = true;
        }

        public void UpdateOnFunctionalChange(long ownerId, bool newFunctionalValue)
        {
            if (!newFunctionalValue)
               DecreaseValue(ref PlayerOwnedValidBlocks, ownerId);
            else
                IncreaseValue(ref PlayerOwnedValidBlocks, ownerId);

            NeedRecalculateOwners = true;
        }


        public void IncreaseValue(ref Dictionary<long, int> dict, long key)
        {
            if (key == 0)
                return;

            if (!dict.ContainsKey(key))
                dict.Add(key, 0);

            dict[key]++;
        }

        public void DecreaseValue(ref Dictionary<long, int> dict, long key)
        {
            if (key == 0)
                return;

            System.Diagnostics.Debug.Assert(dict.ContainsKey(key), "Owner ship counter inconsistency! Check you correctly set ownership through ChangeOwner method.");

            if (dict.ContainsKey(key))
            {
                dict[key]--;
                if (dict[key] == 0)
                    dict.Remove(key);
            }
        }
    }
}

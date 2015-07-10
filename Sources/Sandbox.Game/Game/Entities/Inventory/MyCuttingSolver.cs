using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Generics;

namespace Sandbox.Game.Entities.Inventory
{
    public class MyCuttingSolver
    {
        public struct ComponentChange
        {
            private byte m_operation;
            public bool IsRemoval()
            {
                return m_operation == 0;
            }

            public bool IsAddition()
            {
                return m_operation == 1;
            }

            public bool IsChange()
            {
                return m_operation == 2;
            }

            public MyDefinitionId ToRemove; // Only valid if IsRemoval or IsChange
            public MyDefinitionId ToAdd;    // Only valid if IsAddition or IsChange
            public int Amount;              // How many times to apply this change (i.e. how many instances of the component to change)

            public static ComponentChange CreateRemoval(MyDefinitionId toRemove, int amount)
            {
                return new ComponentChange() { ToRemove = toRemove, Amount = amount, m_operation = 0 };
            }

            public static ComponentChange CreateAddition(MyDefinitionId toAdd, int amount)
            {
                return new ComponentChange() { ToAdd = toAdd, Amount = amount, m_operation = 1 };
            }

            public static ComponentChange CreateChange(MyDefinitionId toRemove, MyDefinitionId toAdd, int amount)
            {
                return new ComponentChange() { ToRemove = toRemove, ToAdd = toAdd, Amount = amount, m_operation = 2 };
            }
        }

        private MyDynamicObjectPool<List<int>> m_listAllocator = new MyDynamicObjectPool<List<int>>(2);
        private Dictionary<MyDefinitionId, List<int>> m_groups = new Dictionary<MyDefinitionId, List<int>>();

        private Dictionary<int, int> m_presentItems = new Dictionary<int, int>();
        private int m_totalItemCounter = 0;
        private int m_solvedItemCounter = 0;

        private List<ComponentChange> m_solution = new List<ComponentChange>();

        public void Clear()
        {
            foreach (var entry in m_groups)
            {
                entry.Value.Clear();
                m_listAllocator.Deallocate(entry.Value);
            }
            m_groups.Clear();

            m_totalItemCounter = 0;
            m_solvedItemCounter = 0;
        }

        public void AddItem(MyDefinitionId groupId, int itemValue, int amount)
        {
            List<int> items = null;

            var group = MyDefinitionManager.Static.GetComponentGroup(groupId);
            if (group == null)
            {
                Debug.Assert(false, "Could not find component group definition for group " + groupId);
                return;
            }

            if (!m_groups.TryGetValue(groupId, out items))
            {
                items = m_listAllocator.Allocate();
                items.Clear();

                // We'll have the zero item allocated (for convenience), but it won't be used
                for (int i = 0; i <= group.GetComponentNumber(); ++i)
                {
                    items.Add(0);
                }
                m_groups.Add(groupId, items);
            }

            items[itemValue] += amount;
            m_totalItemCounter += amount;
        }

        public bool Solve(Dictionary<MyDefinitionId, int> componentNumbers)
        {
            m_solution.Clear();
            m_solvedItemCounter = 0;

            foreach (var entry in m_groups)
            {
                var group = MyDefinitionManager.Static.GetComponentGroup(entry.Key);

                List<int> requiredItems = entry.Value;
                UpdatePresentItems(group, componentNumbers);

                // First try to match the items exactly
                for (int j = 1; j <= group.GetComponentNumber(); j++)
                {
                    int itemCount = requiredItems[j];
                    int removedCount = TryRemovePresentItems(j, itemCount);
                    if (removedCount > 0)
                    {
                        AddRemovalToSolution(group.GetComponentDefinition(j).Id, removedCount);
                        requiredItems[j] = Math.Max(0, itemCount - removedCount);
                    }
                    m_solvedItemCounter += removedCount;
                }

                // Then try splitting bigger items to create smaller ones
                for (int j = group.GetComponentNumber(); j >= 1; j--)
                {
                    int itemCount = requiredItems[j];
                    int itemsCreated = TryCreatingItemsBySplit(group, j, itemCount);
                    requiredItems[j] = itemCount - itemsCreated;
                    m_solvedItemCounter += itemsCreated;
                }

                // Finally, try merging, as a last resort
                for (int j = 1; j <= group.GetComponentNumber(); j++)
                {
                    int itemCount = requiredItems[j];
                    if (itemCount > 0)
                    {
                        int itemsCreated = TryCreatingItemsByMerge(group, j, itemCount);
                        requiredItems[j] = itemCount - itemsCreated;
                        m_solvedItemCounter += itemsCreated;
                    }
                }

                // Now all of the required items are bigger than available items (otherwise, they'd be created in matching or splitting phase)
                // And they also cannot be created by merging, because the merging phase ended already
            }

            return m_totalItemCounter == m_solvedItemCounter;
        }

        public int GetSolvedItemCount()
        {
            return m_solvedItemCounter;
        }

        private int TryCreatingItemsBySplit(MyComponentGroupDefinition group, int itemValue, int itemCount)
        {
            int createdCount = 0;
            // Avoid making too much "mess" by trying to find the best-fitting items first
            for (int k = itemValue + 1; k <= group.GetComponentNumber(); k++)
            {
                int fitNumber = k / itemValue; // How many items fit into k-item

                int wholeCount = itemCount / fitNumber; // How many k-items will be fully split

                int partialFitNumber = itemCount % fitNumber;              // How many items will be created from the last k-item
                int partialCount = partialFitNumber == 0 ? 0 : 1;          // How many k-items will be partially split (either 0 or 1 that is)

                int removedCount = TryRemovePresentItems(k, wholeCount + partialCount);
                if (removedCount > 0)
                {
                    int removedWholeCount = Math.Min(removedCount, wholeCount);
                    if (removedWholeCount != 0)
                    {
                        int created = SplitHelper(group, k, itemValue, removedWholeCount, fitNumber);
                        createdCount += created;
                        itemCount -= created;
                    }

                    if (removedCount - wholeCount > 0)
                    {
                        Debug.Assert(removedCount == wholeCount + partialCount && partialCount == 1, "Calculation problem in cutting solver");
                        int created = SplitHelper(group, k, itemValue, 1, partialFitNumber);
                        createdCount += created;
                        itemCount -= created;
                    }
                }
            }

            return createdCount;
        }

        private int SplitHelper(MyComponentGroupDefinition group, int splitItemValue, int resultItemValue, int numItemsToSplit, int splitCount)
        {
            int remainder = splitItemValue - (splitCount * resultItemValue);
            MyDefinitionId removedComponentId = group.GetComponentDefinition(splitItemValue).Id;
            if (remainder != 0)
            {
                MyDefinitionId addedComponentId = group.GetComponentDefinition(remainder).Id;
                AddPresentItems(remainder, numItemsToSplit);
                AddChangeToSolution(removedComponentId, addedComponentId, numItemsToSplit);
            }
            else
            {
                AddRemovalToSolution(removedComponentId, numItemsToSplit);
            }

            return splitCount * numItemsToSplit;
        }

        private int TryCreatingItemsByMerge(MyComponentGroupDefinition group, int itemValue, int itemCount)
        {
            List<int> removalBuffer = m_listAllocator.Allocate();
            removalBuffer.Clear();
            for (int i = 0; i <= group.GetComponentNumber(); ++i)
            {
                removalBuffer.Add(0);
            }

            int createdCount = 0;

            // Create the items one-by-one
            for (int i = 0; i < itemCount; ++i)
            {
                // What remains to be found to create this item
                int remainder = itemValue;

                // Fill this item with smaller items as long as possible
                for (int k = itemValue - 1; k >= 1; k--)
                {
                    int amount = 0;
                    if (m_presentItems.TryGetValue(k, out amount))
                    {
                        int removeCount = Math.Min(remainder / k, amount);
                        if (removeCount > 0)
                        {
                            remainder = remainder - k * removeCount;
                            amount -= removeCount;
                            removalBuffer[k] += removeCount;
                        }
                    }
                }

                // The remainder was not reduced by the remaining items, which means that we don't have any items left
                if (remainder == itemValue)
                {
                    Debug.Assert(m_presentItems.Count == 0 || itemValue == 1, "There are still items present in the cutting solver, but they were not used in the solution!");
                    break;
                }

                // There are no more smaller items to fill the remainder. Try to split one of the larger ones
                for (int j = remainder + 1; j <= group.GetComponentNumber(); ++j)
                {
                    int present = 0;
                    m_presentItems.TryGetValue(j, out present);
                    if (present > removalBuffer[j])
                    {
                        MyDefinitionId removedComponentId = group.GetComponentDefinition(j).Id;
                        MyDefinitionId addedComponentId = group.GetComponentDefinition(j - remainder).Id;
                        AddChangeToSolution(removedComponentId, addedComponentId, 1);
                        remainder = 0;
                        break;
                    }
                }

                if (remainder == 0)
                {
                    createdCount++;
                    // Add the buffered removals of the smaller items here
                    for (int k = 1; k <= group.GetComponentNumber(); ++k)
                    {
                        if (removalBuffer[k] > 0)
                        {
                            MyDefinitionId removedComponentId = group.GetComponentDefinition(k).Id;
                            int removed = TryRemovePresentItems(k, removalBuffer[k]);
                            Debug.Assert(removed == removalBuffer[k]);
                            AddRemovalToSolution(removedComponentId, removalBuffer[k]);
                            removalBuffer[k] = 0; // We need to clear the buffer for the next item
                        }
                    }
                } // The item could not be created and neither would be the others
                else if (remainder > 0) break;
            }

            m_listAllocator.Deallocate(removalBuffer);

            return createdCount;
        }

        private void AddRemovalToSolution(MyDefinitionId removedComponentId, int removeCount)
        {
            // First search through the changes, whether some of them didn't add the given component. If so, change the change to removal
            for (int i = 0; i < m_solution.Count; ++i)
            {
                ComponentChange change = m_solution[i];
                if ((change.IsChange() || change.IsAddition()) && change.ToAdd == removedComponentId)
                {
                    int difference = change.Amount - removeCount;
                    int toRemove = Math.Min(removeCount, change.Amount);
                    removeCount -= change.Amount;
                    if (difference > 0)
                    {
                        change.Amount = difference;
                        m_solution[i] = change;
                    }
                    else
                    {
                        m_solution.RemoveAtFast(i);
                    }

                    if (change.IsChange())
                    {
                        m_solution.Add(ComponentChange.CreateRemoval(change.ToRemove, toRemove));
                    }

                    if (removeCount <= 0) break;
                }
            }

            if (removeCount > 0)
            {
                m_solution.Add(ComponentChange.CreateRemoval(removedComponentId, removeCount));
            }
        }

        private void AddChangeToSolution(MyDefinitionId removedComponentId, MyDefinitionId addedComponentId, int numChanged)
        {
            for (int i = 0; i < m_solution.Count; ++i)
            {
                ComponentChange change = m_solution[i];
                if ((change.IsChange() || change.IsAddition()) && change.ToAdd == removedComponentId)
                {
                    int difference = change.Amount - numChanged;
                    int toChange = Math.Min(numChanged, change.Amount);
                    numChanged -= change.Amount;
                    if (difference > 0)
                    {
                        change.Amount = difference;
                        m_solution[i] = change;
                    }
                    else
                    {
                        m_solution.RemoveAtFast(i);
                    }

                    if (change.IsChange())
                    {
                        m_solution.Add(ComponentChange.CreateChange(change.ToRemove, addedComponentId, toChange));
                    }
                    else // change.IsAddition()
                    {
                        m_solution.Add(ComponentChange.CreateAddition(addedComponentId, toChange));
                    }

                    if (numChanged <= 0) break;
                }
            }

            if (numChanged > 0)
            {
                m_solution.Add(ComponentChange.CreateChange(removedComponentId, addedComponentId, numChanged));
            }
        }

        public void GetSolution(out List<ComponentChange> changes)
        {
            changes = m_solution;
        }

        private void UpdatePresentItems(MyComponentGroupDefinition group, Dictionary<MyDefinitionId, int> componentNumbers)
        {
            m_presentItems.Clear();
            for (int i = 1; i <= group.GetComponentNumber(); i++)
            {
                var compDef = group.GetComponentDefinition(i);
                int amount = 0;
                componentNumbers.TryGetValue(compDef.Id, out amount);
                m_presentItems[i] = amount;
            }
        }

        private int TryRemovePresentItems(int itemValue, int removeCount)
        {
            int amount = 0;
            m_presentItems.TryGetValue(itemValue, out amount);
            if (amount > removeCount)
            {
                m_presentItems[itemValue] = amount - removeCount;
                return removeCount;
            }
            else // (amount <= removeAmount)
            {
                m_presentItems.Remove(itemValue);
                return amount;
            }
        }

        private void AddPresentItems(int itemValue, int addCount)
        {
            int amount = 0;
            m_presentItems.TryGetValue(itemValue, out amount);
            amount += addCount;
            m_presentItems[itemValue] = amount;
        }
    }
}

using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.SessionComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Generics;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.Inventory
{
    public class MyComponentCombiner
    {
        private MyDynamicObjectPool<List<int>> m_listAllocator = new MyDynamicObjectPool<List<int>>(2);
        private Dictionary<MyDefinitionId, List<int>> m_groups = new Dictionary<MyDefinitionId, List<int>>();

        private Dictionary<int, int> m_presentItems = new Dictionary<int, int>();
        private int m_totalItemCounter = 0;
        private int m_solvedItemCounter = 0;

        private List<MyComponentChange> m_solution = new List<MyComponentChange>();
        private static Dictionary<MyDefinitionId, MyFixedPoint> m_componentCounts = new Dictionary<MyDefinitionId, MyFixedPoint>();

        public MyFixedPoint GetItemAmountCombined(MyInventoryBase inventory, MyDefinitionId contentId)
        {
            if (inventory == null)
                return 0;

            int amount = 0;
            var group = MyDefinitionManager.Static.GetGroupForComponent(contentId, out amount);
            if (group == null)
            {
                //MyComponentSubstitutionDefinition substitutions;
                //if (MyDefinitionManager.Static.TryGetComponentSubstitutionDefinition(contentId, out substitutions))
                //{
                //    foreach (var providingComponent in substitutions.ProvidingComponents)
                //    {
                //        amount += (int)inventory.GetItemAmount(providingComponent.Key) / providingComponent.Value;
                //    }
                //}

                return amount + inventory.GetItemAmount(contentId, substitute: true);
            }
            else
            {
                Clear();
                inventory.CountItems(m_componentCounts);
                AddItem(group.Id, amount, int.MaxValue);
                Solve(m_componentCounts);
                return GetSolvedItemCount();
            }
        }

        public bool CanCombineItems(MyInventoryBase inventory, DictionaryReader<MyDefinitionId, int> items)
        {
            bool result = true;

            Clear();
            inventory.CountItems(m_componentCounts);

            foreach (var item in items)
            {
                int itemValue = 0;
                int neededAmount = item.Value;

                MyComponentGroupDefinition group = null;
                group = MyDefinitionManager.Static.GetGroupForComponent(item.Key, out itemValue);
                if (group == null)
                {
                    MyFixedPoint itemAmount;

                    if (MySessionComponentEquivalency.Static != null && MySessionComponentEquivalency.Static.HasEquivalents(item.Key))
                    {
                        if (!MySessionComponentEquivalency.Static.IsProvided(m_componentCounts, item.Key, item.Value))
                        {
                            result = false;
                            break;
                        }
                    }
                    // Checking if this component is not provided by the group
                    //MyComponentSubstitutionDefinition substitutions;
                    //if (MyDefinitionManager.Static.TryGetComponentSubstitutionDefinition(item.Key, out substitutions))
                    //{
                    //    int providedAmount;
                    //    if (!substitutions.IsProvidedByComponents(m_componentCounts, out providedAmount))
                    //    {
                    //        result = false;
                    //        break;
                    //    }
                    //    else if (providedAmount < neededAmount)
                    //    {
                    //        result = false;
                    //        break;
                    //    }
                    //}
                    else if (!m_componentCounts.TryGetValue(item.Key, out itemAmount))
                    {
                        result = false;
                        break;
                    }
                    else if (itemAmount < neededAmount)
                    {
                        result = false;
                        break;
                    }
                }
                else
                {
                    AddItem(group.Id, itemValue, neededAmount);
                }
            }

            if (result)
            {
                result &= Solve(m_componentCounts);
            }

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                if (result == false)
                    MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Can not build", Color.Red, 1.0f);
                else
                {
                    List<MyComponentChange> solution = null;
                    GetSolution(out solution);

                    float yCoord = 0.0f;
                    foreach (var change in solution)
                    {
                        string text = "";
                        if (change.IsAddition())
                        {
                            text += "+ " + change.Amount.ToString() + "x" + change.ToAdd.ToString();
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, yCoord), text, Color.Green, 1.0f);
                            yCoord += 20.0f;
                        }
                        else if (change.IsRemoval())
                        {
                            text += "- " + change.Amount.ToString() + "x" + change.ToRemove.ToString();
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, yCoord), text, Color.Red, 1.0f);
                            yCoord += 20.0f;
                        }
                        else
                        {
                            text += "- " + change.Amount.ToString() + "x" + change.ToRemove.ToString();
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, yCoord), text, Color.Orange, 1.0f);
                            yCoord += 20.0f;

                            text = "";
                            text += "+ " + change.Amount.ToString() + "x" + change.ToAdd.ToString();
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, yCoord), text, Color.Orange, 1.0f);
                            yCoord += 20.0f;
                        }
                    }
                }
            }

            return result;
        }

        public void RemoveItemsCombined(MyInventoryBase inventory, DictionaryReader<MyDefinitionId, int> toRemove)
        {
            Clear();
            foreach (var material in toRemove) // rename material to component
            {
                int groupAmount = 0;
                MyComponentGroupDefinition group = MyDefinitionManager.Static.GetGroupForComponent(material.Key, out groupAmount);

                // The component does not belong to any component group => we are looking exactly for the given component
                if (group == null)
                {
                    if (MySessionComponentEquivalency.Static != null && MySessionComponentEquivalency.Static.HasEquivalents(material.Key))
                    {
                        var eqGroup = MySessionComponentEquivalency.Static.GetEquivalents(material.Key);
                        if (eqGroup != null)
                        {
                            int amountToRemove = material.Value;
                            foreach (var element in eqGroup)
                            {
                                if (amountToRemove > 0)
                                {
                                    var removed = inventory.RemoveItemsOfType(amountToRemove, element);
                                    amountToRemove -= (int)removed;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        inventory.RemoveItemsOfType(material.Value, material.Key);
                        continue;
                    }


                    //MyComponentSubstitutionDefinition substitutionDefinition = null;
                    //if (MyDefinitionManager.Static.TryGetComponentSubstitutionDefinition(material.Key, out substitutionDefinition))
                    //{
                    //    int amountToRemove = material.Value;
                    //    foreach (var entry in substitutionDefinition.ProvidingComponents)
                    //    {
                    //        if (amountToRemove > 0)
                    //        {
                    //            var removed = inventory.RemoveItemsOfType(amountToRemove * entry.Value, entry.Key);
                    //            amountToRemove -= (int)removed;
                    //        }
                    //        else
                    //        {
                    //            break;
                    //        }
                    //    }

                    //    if (amountToRemove > 0)
                    //    {
                    //        var removed = inventory.RemoveItemsOfType(amountToRemove, material.Key);
                    //        amountToRemove -= (int)removed;
                    //    }
                    //}
                    //else
                    //{
                    //    inventory.RemoveItemsOfType(material.Value, material.Key);
                    //    continue;
                    //}
                }
                else
                {
                    AddItem(group.Id, groupAmount, material.Value);
                }
            }

            inventory.CountItems(m_componentCounts);
            bool success = Solve(m_componentCounts);
            Debug.Assert(success, "Could not combine required items!");

            inventory.ApplyChanges(m_solution);

            /*CheckUpdate();

            m_remainder.Clear();
            foreach (var material in toRemove)
            {
                m_remainder.Add(material.Key, material.Value);
            }

            bool success = true;

            m_cuttingSolver.Clear();
            foreach (var material in m_remainder)
            {
                int groupAmount = 0;
                MyComponentGroupDefinition group = MyDefinitionManager.Static.GetGroupForComponent(material.Key, out groupAmount);

                // The component does not belong to any component group => we are looking exactly for the given component
                if (group == null)
                {
                    success &= RemoveItemsOfTypeInternal(material.Key, material.Value);
                    Debug.Assert(success, "Could not find the required component although we were permitted to build!");
                    continue;
                }
                else
                {
                    m_cuttingSolver.AddItem(group.Id, groupAmount, material.Value);
                }

                m_componentCounts.Clear();
                CollectItems(m_componentCounts);
                success &= m_cuttingSolver.Solve(m_componentCounts);

                List<MyComponentCombiner.ComponentChange> changes = null;
                m_cuttingSolver.GetSolution(out changes);
                foreach (var change in changes)
                {
                    if (change.IsRemoval())
                    {
                        success &= RemoveItemsOfTypeInternal(change.ToRemove, change.Amount);
                        Debug.Assert(success, "Could not remove compnents, although the solver told us it should be possible!");
                    }
                    else if (change.IsChange())
                    {
                        ComponentInfo cInfo = null;
                        m_componentInfos.TryGetValue(change.ToRemove, out cInfo);
                        Debug.Assert(cInfo != null, "Could not find a component in MyAreaInventory!");

                        if (cInfo == null) continue;

                        for (int i = 0; i < change.Amount; ++i)
                        {
                            int dummy = 0;
                            long entityId = cInfo.RemoveComponent(1, out dummy);
                            if (entityId == 0) break;

                            var grid = TryGetComponent(entityId);
                            if (grid == null)
                            {
                                break;
                            }

                            SpawnRemainingData spawnData = new SpawnRemainingData();
                            PrepareSpawnRemainingMaterial(grid, ref spawnData);

                            grid.Physics.Enabled = false;
                            grid.SyncObject.SendCloseRequest();

                            spawnData.DefId = change.ToAdd;
                            SpawnRemainingMaterial(ref spawnData);
                        }
                    }
                }
            }

            return success;*/
        }

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

            m_componentCounts.Clear();
        }


        // Adds the component to be checked if can be provided by some of the components in the inventory
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
            else
            {
                // this group is already 

            }

            items[itemValue] += amount;
            m_totalItemCounter += amount;
        }

        public bool Solve(Dictionary<MyDefinitionId, MyFixedPoint> componentCounts)
        {
            m_solution.Clear();
            m_solvedItemCounter = 0;

            foreach (var entry in m_groups)
            {
                var group = MyDefinitionManager.Static.GetComponentGroup(entry.Key);

                List<int> requiredItems = entry.Value;
                UpdatePresentItems(group, componentCounts);

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

        private int GetSolvedItemCount()
        {
            return m_solvedItemCounter;
        }

        public void GetSolution(out List<MyComponentChange> changes)
        {
            changes = m_solution;
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
            // Removal buffer is here so that the method does not do anything until it's clear that the operation can be successful
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
                if (remainder != 0)
                {
                    for (int j = remainder + 1; j <= group.GetComponentNumber(); ++j)
                    {
                        int present = 0;
                        m_presentItems.TryGetValue(j, out present);
                        // If there is some present item that is not planned to be removed, use it
                        if (present > removalBuffer[j])
                        {
                            MyDefinitionId removedComponentId = group.GetComponentDefinition(j).Id;
                            MyDefinitionId addedComponentId = group.GetComponentDefinition(j - remainder).Id;
                            AddChangeToSolution(removedComponentId, addedComponentId, 1);
                            int removed = TryRemovePresentItems(j, 1);
                            AddPresentItems(j - remainder, 1);
                            Debug.Assert(removed == 1);
                            remainder = 0;
                            break;
                        }
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
                MyComponentChange change = m_solution[i];
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
                        m_solution.Add(MyComponentChange.CreateRemoval(change.ToRemove, toRemove));
                    }

                    if (removeCount <= 0) break;
                }
            }

            if (removeCount > 0)
            {
                m_solution.Add(MyComponentChange.CreateRemoval(removedComponentId, removeCount));
            }
        }

        private void AddChangeToSolution(MyDefinitionId removedComponentId, MyDefinitionId addedComponentId, int numChanged)
        {
            for (int i = 0; i < m_solution.Count; ++i)
            {
                MyComponentChange change = m_solution[i];
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
                        m_solution.Add(MyComponentChange.CreateChange(change.ToRemove, addedComponentId, toChange));
                    }
                    else // change.IsAddition()
                    {
                        m_solution.Add(MyComponentChange.CreateAddition(addedComponentId, toChange));
                    }

                    if (numChanged <= 0) break;
                }
            }

            if (numChanged > 0)
            {
                m_solution.Add(MyComponentChange.CreateChange(removedComponentId, addedComponentId, numChanged));
            }
        }

        private void UpdatePresentItems(MyComponentGroupDefinition group, Dictionary<MyDefinitionId, MyFixedPoint> componentCounts)
        {
            m_presentItems.Clear();
            for (int i = 1; i <= group.GetComponentNumber(); i++)
            {
                var compDef = group.GetComponentDefinition(i);
                MyFixedPoint amount = 0;
                componentCounts.TryGetValue(compDef.Id, out amount);
                m_presentItems[i] = (int)amount;
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.World;

using VRage.Trace;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game.Multiplayer;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Game.Entities
{
    public class MyComponentStack
    {
        public struct GroupInfo
        {
            public int MountedCount;
            public int TotalCount;
			public int AvailableCount;

            /// <summary>
            /// Integrity of group, increases when mounting more components
            /// </summary>
            public float Integrity;
            public float MaxIntegrity;
            public MyComponentDefinition Component;
        }

        /// <summary>
        /// Mount threshold, required because of float inaccuracy.
        /// Component that has integrity beyond this threshold is considered unmounted.
        /// The integrity of the whole stack will never fall beyond this level (unless the stack is fully dismounted)
        /// </summary>
        public const float MOUNT_THRESHOLD = 0.000001f;

        #region Fields
        readonly MyCubeBlockDefinition m_blockDefinition;

        // Data per uniform part
        float m_buildIntegrity; // Integrity that decides the displayed model. Does not change with damage
        float m_integrity; // The real structural integrity of the part. Changes with damage

        UInt16 m_topGroupIndex;
        UInt16 m_topComponentIndex;

        #endregion

        #region Properties

        public bool IsFullIntegrity
        {
            get { return m_integrity >= MaxIntegrity; }
        }

        public bool IsFullyDismounted
        {
            get { return m_integrity < MOUNT_THRESHOLD; }
        }

        public bool IsDestroyed
        {
            get { return m_integrity < MOUNT_THRESHOLD; }
        }

        public float Integrity
        {
            get { return m_integrity; }
            private set
            {
                if (m_integrity != value)
                {
                    bool oldFunctional = IsFunctional;
                    m_integrity = value;
                    CheckFunctionalState(oldFunctional);
                }
            }
        }

        public float IntegrityRatio
        {
            get { return Integrity / MaxIntegrity; }
        }

        public float MaxIntegrity
        {
            get { return m_blockDefinition.MaxIntegrity; }
        }

        public float BuildRatio
        {
            get { return m_buildIntegrity / MaxIntegrity; }
        }

        public float BuildIntegrity
        {
            get { return m_buildIntegrity; }
            private set
            {
                if (m_buildIntegrity != value)
                {
                    bool oldFunctional = IsFunctional;
                    m_buildIntegrity = value;
                    CheckFunctionalState(oldFunctional);
                }
            }
        }

        /// <summary>
        /// Component stack is functional when critical part is not destroyed (integrity > 0).
        /// IMPORTANT: When you change the logic beyond this property, don't forget to call CheckFunctionalState every time the
        ///            functional state could have been changed! (Also, remove calls to CheckFunctionalState where no longer needed)
        /// </summary>
        public bool IsFunctional
        {
            get
            {
                return BuildRatio > m_blockDefinition.FinalModelThreshold() && IntegrityRatio > m_blockDefinition.CriticalIntegrityRatio;
            }
        }

        public event Action IsFunctionalChanged;
        private void CheckFunctionalState(bool oldFunctionalState)
        {
            if (IsFunctional != oldFunctionalState && IsFunctionalChanged != null)
            {
                IsFunctionalChanged();
            }
        }

        #endregion

        public MyComponentStack(MyCubeBlockDefinition BlockDefinition, float integrityPercent, float buildPercent)
        {
            m_blockDefinition = BlockDefinition;

            float maxIntegrity = BlockDefinition.MaxIntegrity;
            BuildIntegrity = maxIntegrity * buildPercent;
            Integrity = maxIntegrity * integrityPercent;

            UpdateIndices();

            // Fix top component integrity in case it was incorrectly loaded
            if (Integrity != 0.0f)
            {
                float topIntegrity = GetTopComponentIntegrity();
                if (topIntegrity < MOUNT_THRESHOLD)
                {
                    Integrity += MOUNT_THRESHOLD - topIntegrity;
                }
                if (topIntegrity > BlockDefinition.Components[m_topGroupIndex].Definition.MaxIntegrity)
                {
                    Integrity -= topIntegrity - BlockDefinition.Components[m_topGroupIndex].Definition.MaxIntegrity;
                }
            }
        }

        private float GetTopComponentIntegrity()
        {
            float remainingIntegrity = Integrity;
            var components = m_blockDefinition.Components;
            for (int i = 0; i < m_topGroupIndex; ++i)
            {
                remainingIntegrity -= components[i].Definition.MaxIntegrity * components[i].Count;
            }
            remainingIntegrity -= components[m_topGroupIndex].Definition.MaxIntegrity * m_topComponentIndex;

            return remainingIntegrity;
        }

        private void SetTopIndex(int newTopGroupIndex, int newTopComponentIndex)
        {
            Debug.Assert(newTopGroupIndex >= UInt16.MinValue, "Underflow of group index");
            Debug.Assert(newTopGroupIndex <= UInt16.MaxValue, "Overflow of group index");
            Debug.Assert(newTopComponentIndex >= UInt16.MinValue, "Underflow of component index");
            Debug.Assert(newTopComponentIndex <= UInt16.MaxValue, "Overflow of component index");
            m_topGroupIndex = (UInt16)newTopGroupIndex;
            m_topComponentIndex = (UInt16)newTopComponentIndex;
        }

        /// <summary>
        /// Updates the top 
        /// </summary>
        private void UpdateIndices()
        {
            float integrity = Integrity;
            MyCubeBlockDefinition blockDef = m_blockDefinition;

            int topGroupIndex = 0;
            int topComponentIndex = 0;

            CalculateIndicesInternal(integrity, blockDef, ref topGroupIndex, ref topComponentIndex);

            SetTopIndex(topGroupIndex, topComponentIndex);
            return;
        }

        private static void CalculateIndicesInternal(float integrity, MyCubeBlockDefinition blockDef, ref int topGroupIndex, ref int topComponentIndex)
        {
            float remainingIntegrity = integrity;
            var components = blockDef.Components;
            int i = 0;
            for (i = 0; i < components.Length; ++i)
            {
                float stackGroupIntegrity = components[i].Definition.MaxIntegrity * components[i].Count;
                if (remainingIntegrity >= stackGroupIntegrity)
                {
                    remainingIntegrity -= stackGroupIntegrity;
                    // The next group is not mounted, return the index of last item from this group
                    if (remainingIntegrity < MOUNT_THRESHOLD / 2)
                    {
                        topGroupIndex = i;
                        topComponentIndex = components[i].Count - 1;
                        break;
                    }
                }
                else
                {
                    int thisIndex = (int)(remainingIntegrity / components[i].Definition.MaxIntegrity);
                    float remainder = remainingIntegrity - (components[i].Definition.MaxIntegrity * thisIndex);
                    // The next component is not mounted, return index of previous item
                    if (remainder < MOUNT_THRESHOLD / 2 && thisIndex != 0)
                    {
                        topGroupIndex = i;
                        topComponentIndex = thisIndex - 1;
                        break;
                    }
                    topGroupIndex = i;
                    topComponentIndex = thisIndex;
                    break;
                }
            }

            Debug.Assert(i < components.Length, "Integrity overflow. This should never happen");
        }

        public void UpdateBuildIntegrityUp()
        {
            if (BuildIntegrity < Integrity)
			{
                BuildIntegrity = Integrity;
			}
        }

        public void UpdateBuildIntegrityDown(float ratio)
        {
            if (BuildIntegrity > Integrity * ratio)
            {
                BuildIntegrity = Integrity * ratio;
            }
        }

        public bool CanContinueBuild(MyInventory inventory, MyConstructionStockpile stockpile)
        {
            if (IsFullIntegrity)
                return false;
            float topIntegrity = GetTopComponentIntegrity();
            if (topIntegrity < m_blockDefinition.Components[m_topGroupIndex].Definition.MaxIntegrity)
                return true;

            int nextCompoGroup = m_topGroupIndex;
            if (m_topComponentIndex == m_blockDefinition.Components[nextCompoGroup].Count - 1)
            {
                nextCompoGroup++;
            }

            var componentDefinition = m_blockDefinition.Components[nextCompoGroup].Definition;
            if (stockpile != null && stockpile.GetItemAmount(componentDefinition.Id) > 0)
            {
                return true;
            }

            if (inventory != null && inventory.GetItemAmount(componentDefinition.Id) > 0)
            {
                return true;
            }
            return false;
        }

        public void GetMissingInfo(out int groupIndex, out int componentCount)
        {
            if (IsFullIntegrity)
            {
                groupIndex = 0;
                componentCount = 0;
                return;
            }

            float topIntegrity = GetTopComponentIntegrity();
            if (topIntegrity < m_blockDefinition.Components[m_topGroupIndex].Definition.MaxIntegrity)
            {
                groupIndex = 0;
                componentCount = 0;
                return;
            }

            int missingCompoIndex = m_topComponentIndex + 1;
            groupIndex = m_topGroupIndex;
            if (missingCompoIndex == m_blockDefinition.Components[groupIndex].Count)
            {
                groupIndex++;
                missingCompoIndex = 0;
            }

            componentCount = m_blockDefinition.Components[groupIndex].Count - missingCompoIndex;
            return;
        }

        // TODO: Until Martin rewrites component saving
        public void DestroyCompletely()
        {
            BuildIntegrity = 0.0f;
            Integrity = 0.0f;
            UpdateIndices();
        }

        private bool CheckOrMountFirstComponent(MyConstructionStockpile stockpile = null)
        {
            if (Integrity > MOUNT_THRESHOLD / 2)
            {
                return true;
            }
            var firstComponent = m_blockDefinition.Components[0].Definition;
            if (stockpile == null || stockpile.RemoveItems(1, firstComponent.Id))
            {
                Integrity = MOUNT_THRESHOLD;
                UpdateBuildIntegrityUp();
                return true;
            }
            return false;
        }

        public void GetMissingComponents(Dictionary<string, int> addToDictionary, MyConstructionStockpile availableItems = null)
        {
            int index = m_topGroupIndex;
            var component = m_blockDefinition.Components[index];
            int mountCount = m_topComponentIndex + 1;
            if (IsFullyDismounted)
                mountCount--;
            if (mountCount < component.Count)
            {
                string subTypeId = component.Definition.Id.SubtypeName;
                if (addToDictionary.ContainsKey(subTypeId))
                    addToDictionary[subTypeId] += component.Count - mountCount;
                else
                    addToDictionary[subTypeId] = component.Count - mountCount;
            }
            index++;
            for (; index < m_blockDefinition.Components.Length; index++)
            {
                component = m_blockDefinition.Components[index];
                string subTypeId = component.Definition.Id.SubtypeName;
                if (addToDictionary.ContainsKey(subTypeId))
                    addToDictionary[subTypeId] += component.Count;
                else
                    addToDictionary[subTypeId] = component.Count;
            }
            if (availableItems != null)
                for (index = 0; index < addToDictionary.Keys.Count; index++ )
                {
                    string key = addToDictionary.Keys.ElementAt(index);
                    addToDictionary[key] -= availableItems.GetItemAmount(new MyDefinitionId(typeof(MyObjectBuilder_Component), key));
                    if (addToDictionary[key] <= 0)
                    {
                        addToDictionary.Remove(key);
                        index--;
                    }
                }
        }

        public static void GetMountedComponents(MyComponentList addToList, MyObjectBuilder_CubeBlock block)
        {
            int topGroup = 0;
            int topComponent = 0;

            MyCubeBlockDefinition blockDef = null;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(block.GetId(), out blockDef);
            Debug.Assert(blockDef != null, "Could not find block definition");
            if (blockDef == null) return;

            Debug.Assert(block!= null, "Getting mounted components of a null block");
            if (block == null) return;

            float integrity = block.IntegrityPercent * blockDef.MaxIntegrity;
            
            CalculateIndicesInternal(integrity, blockDef, ref topGroup, ref topComponent);

            Debug.Assert(topGroup < blockDef.Components.Count(), "Component group overflow in CalculateItemRequirements");
            if (topGroup >= blockDef.Components.Count()) return;

            Debug.Assert(topComponent < blockDef.Components[topGroup].Count, "Component overflow in CalculateItemRequirements");
            if (topComponent >= blockDef.Components[topGroup].Count) return;

            int mountCount = topComponent;
            if (integrity >= MOUNT_THRESHOLD)
                mountCount++;

            MyDefinitionId componentId;
            for (int group = 0; group < topGroup; ++group)
            {
                MyCubeBlockDefinition.Component component = blockDef.Components[group];
                addToList.AddMaterial(component.Definition.Id, component.Count, addToDisplayList: false);
            }
            componentId = blockDef.Components[topGroup].Definition.Id;
            addToList.AddMaterial(componentId, mountCount, addToDisplayList: false);
        }

        public void IncreaseMountLevel(float mountAmount, MyConstructionStockpile stockpile = null)
        {
            bool oldWorkingState = IsFunctional;
            IncreaseMountLevelInternal(mountAmount, stockpile);
        }

        private void IncreaseMountLevelInternal(float mountAmount, MyConstructionStockpile stockpile = null)
        {
            Debug.Assert(BuildIntegrity >= Integrity, "Integrity of component stack is larger than build integrity");

            if (!CheckOrMountFirstComponent(stockpile))
            {
                return;
            }

            float topIntegrity = GetTopComponentIntegrity();
            float maxCompIntegrity = m_blockDefinition.Components[m_topGroupIndex].Definition.MaxIntegrity;
            int groupIndex = (int)m_topGroupIndex;
            int compIndex = (int)m_topComponentIndex;

            while (mountAmount > 0) {
                float toBuild = maxCompIntegrity - topIntegrity;

                if (mountAmount < toBuild)
                {
                    Integrity += mountAmount;
                    UpdateBuildIntegrityUp();
                    break;
                }

                Integrity += toBuild + MOUNT_THRESHOLD;
                mountAmount -= toBuild + MOUNT_THRESHOLD;

                ++compIndex;
                if (compIndex >= m_blockDefinition.Components[m_topGroupIndex].Count)
                {
                    ++groupIndex;
                    compIndex = 0;
                }
                // Fully built
                if (groupIndex == m_blockDefinition.Components.Length)
                {
                    Integrity = MaxIntegrity;
                    UpdateBuildIntegrityUp();
                    break;
                }
                // We don't have the next item
                var nextComponent = m_blockDefinition.Components[groupIndex].Definition;
                if (stockpile != null && !stockpile.RemoveItems(1, nextComponent.Id))
                {
                    Integrity -= MOUNT_THRESHOLD;
                    UpdateBuildIntegrityUp();
                    break;
                }
                else
                {
                    UpdateBuildIntegrityUp();
                }

                SetTopIndex(groupIndex, compIndex);
                topIntegrity = MOUNT_THRESHOLD;
                maxCompIntegrity = m_blockDefinition.Components[groupIndex].Definition.MaxIntegrity;
            }

            return;
        }

        /// <summary>
        /// Dismounts component stack, dismounted items are put into output stockpile
        /// </summary>
        public void DecreaseMountLevel(float unmountAmount, MyConstructionStockpile outputStockpile = null)
        {
            Debug.Assert(!IsFullyDismounted, "Dismounting a fully dismounted block. Either it was not razed or it was not created correctly.");

            float buildIntegrityRatio = BuildIntegrity / Integrity; // Save the original build integrity ratio

            UnmountInternal(unmountAmount, outputStockpile);

            // Following function calls CheckFunctionalState itself
            UpdateBuildIntegrityDown(buildIntegrityRatio);
        }

        /// <summary>
        /// Applies damage to the component stack. The method works almost the same as dismounting, it just leaves the
        /// build level at the original value and also the parts that are put into the outputStockpile are damaged.
        /// </summary>
        public void ApplyDamage(float damage, MyConstructionStockpile outputStockpile = null)
        {
            Debug.Assert(!IsDestroyed, "Applying damage to an already destroyed stack. Block should have been removed.");

            UnmountInternal(damage, outputStockpile, true);
        }

		private float GetDeconstructionEfficiency(int groupIndex, bool damageItems)
		{
			return (damageItems ? 1 : m_blockDefinition.Components[groupIndex].Definition.DeconstructionEfficiency);
		}

        private void UnmountInternal(float unmountAmount, MyConstructionStockpile outputStockpile = null, bool damageItems = false)
        {
            // We don't have to update functional state in this function, because it is done in the caller functions.
            // If you start using this function anywhere else, don't forget to update functional state yourself!

            float topIntegrity = GetTopComponentIntegrity();
            int groupIndex = (int)m_topGroupIndex;
            int compIndex = (int)m_topComponentIndex;

            // Continue removing components, until the to be removed component's health is larger than unmountAmount
            MyObjectBuilder_Component componentBuilder = null;
            var scrapBuilder = MyFloatingObject.ScrapBuilder;
			while (unmountAmount * GetDeconstructionEfficiency(groupIndex, damageItems) >= topIntegrity)
            {
                Integrity -= topIntegrity;
                unmountAmount -= topIntegrity;

                // In creative mode, the outputInventory should normally be null.
                // However, if we load the game from the realistic mode, it would not be necessarilly null.
                if (outputStockpile != null && MySession.Static.SurvivalMode)
                {
                    bool doDamage = damageItems && MyFakes.ENABLE_DAMAGED_COMPONENTS;
                    if (!damageItems || (doDamage && MyRandom.Instance.NextFloat() <= m_blockDefinition.Components[groupIndex].Definition.DropProbability))
                    {
                        componentBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>();
                        componentBuilder.SubtypeName = m_blockDefinition.Components[groupIndex].Definition.Id.SubtypeName;
                        if (doDamage)
                        {
                            componentBuilder.Flags |= MyItemFlags.Damaged;
                        }
                        if (!outputStockpile.AddItems(1, componentBuilder))
                        {
                            // TODO: Throw the items into space (although this branch should probably not happen)
                        }
                    }

                    MyComponentDefinition destroyedComponentDefinition = m_blockDefinition.Components[groupIndex].Definition;
                    if (MyFakes.ENABLE_SCRAP && damageItems && (MyRandom.Instance.NextFloat() < destroyedComponentDefinition.DropProbability))
                    {
                        outputStockpile.AddItems((int)(0.8f * destroyedComponentDefinition.Mass), scrapBuilder);
                    }
                }      
               
                compIndex--;
                if (compIndex < 0)
                {
                    groupIndex--;
                    if (groupIndex < 0)
                    {
                        SetTopIndex(0, 0);
                        Integrity = 0.0f;
                        return;
                    }
                    else
                    {
                        compIndex = m_blockDefinition.Components[groupIndex].Count - 1;
                    }
                }


                topIntegrity = m_blockDefinition.Components[groupIndex].Definition.MaxIntegrity;
                SetTopIndex(groupIndex, compIndex);
            }

            // Damage the remaining component
			Integrity -= unmountAmount * GetDeconstructionEfficiency(groupIndex, damageItems);
			topIntegrity -= unmountAmount * GetDeconstructionEfficiency(groupIndex, damageItems);

            if (topIntegrity < MOUNT_THRESHOLD)
            {
                Integrity += MOUNT_THRESHOLD - topIntegrity;
                topIntegrity = MOUNT_THRESHOLD;
            }

            Debug.Assert(Integrity >= MOUNT_THRESHOLD, "Integrity inconsistent after a dismount of component stack");
        }

        internal void SetIntegrity(float buildIntegrity, float integrity)
        {
            Debug.Assert(buildIntegrity >= integrity);
            Integrity = integrity;
            BuildIntegrity = buildIntegrity;
            UpdateIndices();
        }

        public int GroupCount
        {
            get
            {
                return m_blockDefinition.Components.Length;
            }
        }

        public GroupInfo GetGroupInfo(int index)
        {
            var component = m_blockDefinition.Components[index];
            GroupInfo info = new GroupInfo()
            {
                Component = component.Definition,
                TotalCount = component.Count,
                MountedCount = 0,
				AvailableCount = 0,
                Integrity = 0.0f,
                MaxIntegrity = component.Count * component.Definition.MaxIntegrity,
            };
            if (index < m_topGroupIndex)
            {
                info.MountedCount = component.Count;
                info.Integrity = component.Count * component.Definition.MaxIntegrity;
            }
            else if (index == m_topGroupIndex)
            {
                info.MountedCount = m_topComponentIndex + 1;
                info.Integrity = GetTopComponentIntegrity() + m_topComponentIndex * component.Definition.MaxIntegrity;
            }
            return info;
        }
    }
}

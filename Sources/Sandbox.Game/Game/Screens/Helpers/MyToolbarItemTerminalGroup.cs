using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemTerminalGroup))]
    class MyToolbarItemTerminalGroup : MyToolbarItemActions, IMyToolbarItemEntity
    {
        private static HashSet<Type> tmpBlockTypes = new HashSet<Type>();
        private static List<MyTerminalBlock> m_tmpBlocks = new List<MyTerminalBlock>();
        private static StringBuilder m_tmpStringBuilder = new StringBuilder();

        private StringBuilder m_groupName; // StringBuilder for comparison with group.Name
        private long m_blockEntityId;
        private bool m_wasValid;

        private ListReader<MyTerminalBlock> GetBlocks()
        {
            MyCubeBlock ownerCubeBlock;
            MyEntities.TryGetEntityById<MyCubeBlock>(m_blockEntityId, out ownerCubeBlock);
            if (ownerCubeBlock == null)
                return ListReader<MyTerminalBlock>.Empty;

            var thisCubeGrid = ownerCubeBlock.CubeGrid;
            if (thisCubeGrid == null || thisCubeGrid.GridSystems.TerminalSystem == null)
                return ListReader<MyTerminalBlock>.Empty;

            foreach (var group in thisCubeGrid.GridSystems.TerminalSystem.BlockGroups)
                if (group.Name.Equals(m_groupName))
                    return group.Blocks;

            return ListReader<MyTerminalBlock>.Empty;
        }

        private ListReader<ITerminalAction> GetActions(ListReader<MyTerminalBlock> blocks, out bool genericType)
        {
            try
            {
                bool allFunctional = true;
                foreach (var block in blocks)
                {
                    allFunctional &= block is MyFunctionalBlock;
                    tmpBlockTypes.Add(block.GetType());
                }

                if (tmpBlockTypes.Count == 1)
                {
                    genericType = false;
                    return GetValidActions(blocks.ItemAt(0).GetType(), blocks);
                }
                else if (tmpBlockTypes.Count == 0 || !allFunctional)
                {
                    genericType = true;
                    return ListReader<ITerminalAction>.Empty;
                }
                else
                {
                    genericType = true;
                    var commonType = FindBaseClass(tmpBlockTypes.ToArray<Type>(), typeof(MyFunctionalBlock));
                    return GetValidActions(commonType, blocks);
                }
            }
            finally
            {
                tmpBlockTypes.Clear();
            }
        }

        /// <summary>
        /// Searching for common base class. Used to return more specific group actions than only basic actions of functional blocks (if the blocks are of common origin)
        /// </summary>
        /// <param name="types"></param>
        /// <param name="baseKnownCommonType"></param>
        /// <returns></returns>
        public static Type FindBaseClass(Type[] types, Type baseKnownCommonType)
        {
            var currentType = types[0];
            Dictionary<Type, int> typeCount = new Dictionary<Type, int>();
            typeCount.Add(baseKnownCommonType, types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                 currentType = types[i];
                 while (currentType != baseKnownCommonType)
                 {
                     if (typeCount.ContainsKey(currentType))
                     {
                         typeCount[currentType] += 1;
                     }
                     else
                     {
                         typeCount[currentType] = 1;
                     }
                     currentType = currentType.BaseType;
                 }
            }

            //return the top-most class found that is common for all types
            currentType = types[0];
            while (typeCount[currentType] != types.Length)
            {
                currentType = currentType.BaseType;
            }
            return currentType;
        }

        private ListReader<ITerminalAction> GetValidActions(Type blockType, ListReader<MyTerminalBlock> blocks)
        {
            var allActions = MyTerminalControlFactory.GetActions(blockType);
            var validActions = new List<ITerminalAction>();
            foreach (var action in allActions)
            {
                if (action.IsValidForGroups())
                {
                    bool found = false;
                    foreach (var block in blocks)
                        if (action.IsEnabled(block))
                        {
                            found = true;
                            break;
                        }
                    if (found)
                        validActions.Add(action);
                }
            }
            return validActions;
        }

        private ITerminalAction FindAction(ListReader<ITerminalAction> actions, string name)
        {
            foreach (var item in actions)
            {
                if (item.Id == name)
                    return item;
            }
            return null;
        }

        private MyTerminalBlock FirstFunctional(ListReader<MyTerminalBlock> blocks, MyEntity owner, long playerID)
        {
            foreach (var block in blocks)
            {
                if (block.IsFunctional && (block.HasPlayerAccess(playerID) || block.HasPlayerAccess((owner as MyTerminalBlock).OwnerId)))
                    return block;
            }
            return null;
        }

        public override ListReader<ITerminalAction> AllActions
        {
            get
            {
                bool sameType;
                return GetActions(GetBlocks(), out sameType);
            }
        }

        public override ListReader<ITerminalAction> PossibleActions(MyToolbarType toolbarType)
        {
            return AllActions;
        }

        public override bool Activate()
        {
            bool genericType;
            var blocks = GetBlocks();
            var action = FindAction(GetActions(blocks, out genericType), ActionId);
            if (action == null)
                return false;

            try
            {
                foreach (var item in blocks)
                    m_tmpBlocks.Add(item);

                // E.g. when disconnecting multiple connectors
                foreach (var block in m_tmpBlocks)
                {
                    if (block != null && block.IsFunctional)
                        action.Apply(block);
                }
            }
            finally
            {
                m_tmpBlocks.Clear();
            }

            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type != MyToolbarType.Character && type != MyToolbarType.Spectator);
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            ChangeInfo changed = base.Update(owner, playerID);

            bool genericType;
            var blocks = GetBlocks();
            var action = FindAction(GetActions(blocks, out genericType), ActionId);

            var firstFunctional = FirstFunctional(blocks, owner, playerID);

            changed |= SetEnabled(action != null && firstFunctional != null);
            changed |= SetIcons(genericType ? new string[] { "Textures\\GUI\\Icons\\GroupIcon.dds" } : blocks.ItemAt(0).BlockDefinition.Icons);
            changed |= SetSubIcon(action != null ? action.Icon : null);

            if (action != null && !m_wasValid)
            {
                m_tmpStringBuilder.Clear();
                m_tmpStringBuilder.AppendStringBuilder(this.m_groupName);
                m_tmpStringBuilder.Append(" - ");
                m_tmpStringBuilder.Append(action.Name);
                changed |= SetDisplayName(m_tmpStringBuilder.ToString());
                m_tmpStringBuilder.Clear();

                m_wasValid = true;
            }
            else if (action == null)
            {
                m_wasValid = false;
            }

            if (action != null && blocks.Count > 0)
            {
                // When everything is disabled, write value of first disabled block
                m_tmpStringBuilder.Clear();
                action.WriteValue(firstFunctional ?? blocks.ItemAt(0), m_tmpStringBuilder);
                changed |= SetIconText(m_tmpStringBuilder);
                m_tmpStringBuilder.Clear();
            }
            return changed;
        }

        public bool CompareEntityIds(long id)
        {
            return m_blockEntityId == id;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var other = obj as MyToolbarItemTerminalGroup;
            return other != null && this.m_blockEntityId == other.m_blockEntityId && this.m_groupName.Equals(other.m_groupName) && this.ActionId == other.ActionId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = m_blockEntityId.GetHashCode();
                result = (result * 397) ^ m_groupName.GetHashCode();
                result = (result * 397) ^ ActionId.GetHashCode();
                return result;
            }
        }

        public override bool Init(MyObjectBuilder_ToolbarItem objBuilder)
        {
            Debug.Assert(objBuilder is MyObjectBuilder_ToolbarItemTerminalGroup, "Wrong object builder in toolbar");

            WantsToBeActivated = false;
            WantsToBeSelected = false;
            ActivateOnClick = true;

            var builder = (MyObjectBuilder_ToolbarItemTerminalGroup)objBuilder;
            SetDisplayName(builder.GroupName);
            if (builder.BlockEntityId == 0)
            {
                m_wasValid = false;
                return false;
            }
            this.m_blockEntityId = builder.BlockEntityId;
            this.m_groupName = new StringBuilder(builder.GroupName);
            m_wasValid = true;
            SetAction(builder._Action);
            return true;
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            MyObjectBuilder_ToolbarItemTerminalGroup output = (MyObjectBuilder_ToolbarItemTerminalGroup)MyToolbarItemFactory.CreateObjectBuilder(this);
            output.GroupName = this.m_groupName.ToString();
            output.BlockEntityId = this.m_blockEntityId;
            output._Action = this.ActionId;
            return output;
        }
    }
}

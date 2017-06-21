using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Definitions;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using System.Diagnostics;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemTerminalBlock))]
    class MyToolbarItemTerminalBlock : MyToolbarItemActions, IMyToolbarItemEntity
    {
        private long m_blockEntityId;
        private bool m_wasValid;
        private bool m_nameChanged;

        private MyTerminalBlock m_block;
        private List<TerminalActionParameter> m_parameters = new List<TerminalActionParameter>();

        private static List<ITerminalAction> m_tmpEnabledActions = new List<ITerminalAction>();
        private static ListReader<ITerminalAction> m_tmpEnabledActionsReader = new ListReader<ITerminalAction>(m_tmpEnabledActions);
        private static StringBuilder m_tmpStringBuilder = new StringBuilder();

        private bool TryGetBlock()
        {
            bool success = MyEntities.TryGetEntityById<MyTerminalBlock>(m_blockEntityId, out m_block);
            if (success)
            {
                RegisterEvents();
            }
            return success;
        }

        public override ListReader<ITerminalAction> AllActions
        {
            get 
            {
                return GetActions(null);
            }
        }

        public List<TerminalActionParameter> Parameters
        {
            get { return m_parameters; }
        }

        public override ListReader<ITerminalAction> PossibleActions(MyToolbarType type)
        {
            return GetActions(type);
        }

        private ListReader<ITerminalAction> GetActions(MyToolbarType? type)
        {
            if (m_block == null) return ListReader<ITerminalAction>.Empty;

            m_tmpEnabledActions.Clear();
            foreach (var action in MyTerminalControls.Static.GetActions(m_block))
            {
                if (action.IsEnabled(m_block))
                {
                    if (type == null || action.IsValidForToolbarType(type.Value))
                    {
                        m_tmpEnabledActions.Add(action);
                    }
                }
            }

            return m_tmpEnabledActionsReader;
        }

        public override bool Activate()
        {
            var action = GetCurrentAction();

            if (m_block != null && action != null)
            {
                action.Apply(m_block, this.Parameters);
                return true;
            }
            return false;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type != MyToolbarType.Character && type != MyToolbarType.Spectator);
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            ChangeInfo changed = base.Update(owner, playerID);

            if (m_block == null)
                TryGetBlock();

            var action = GetCurrentAction();

            bool isValid = m_block != null && action != null && MyCubeGridGroups.Static.Physical.HasSameGroup((owner as MyTerminalBlock).CubeGrid, m_block.CubeGrid);
            changed |= SetEnabled(isValid && m_block.IsFunctional && (m_block.HasPlayerAccess(playerID) || m_block.HasPlayerAccess((owner as MyTerminalBlock).OwnerId)));
            if (m_block != null)
            {
                changed |= SetIcons(m_block.BlockDefinition.Icons);
            }
            if (isValid)
            {
                if (!m_wasValid || ActionChanged)
                {
                    changed |= SetIcons(m_block.BlockDefinition.Icons);
                    changed |= SetSubIcon(action.Icon);
                    changed |= UpdateCustomName(action);
                }
                else if (m_nameChanged)
                {
                    changed |= UpdateCustomName(action);
                }

                m_tmpStringBuilder.Clear();
                action.WriteValue(m_block, m_tmpStringBuilder);
                changed |= SetIconText(m_tmpStringBuilder);
                m_tmpStringBuilder.Clear();
            }

            m_wasValid = isValid;
            m_nameChanged = false;
            ActionChanged = false;

            return changed;
        }

        private ChangeInfo UpdateCustomName(ITerminalAction action)
        {
            try
            {
                m_tmpStringBuilder.Clear();
                m_tmpStringBuilder.AppendStringBuilder(m_block.CustomName);
                m_tmpStringBuilder.Append(" - ");
                m_tmpStringBuilder.AppendStringBuilder(action.Name);
                return SetDisplayName(m_tmpStringBuilder.ToString());
            }
            finally
            {
                m_tmpStringBuilder.Clear();
            }
        }

        public bool CompareEntityIds(long id)
        {
            return id == m_blockEntityId;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var other = obj as MyToolbarItemTerminalBlock;
            if (!(other != null && this.m_blockEntityId == other.m_blockEntityId && this.ActionId == other.ActionId))
                return false;

            // Two toolbar items are only considered equal if all parameters are also exactly equal.
            if (m_parameters.Count != other.Parameters.Count)
                return false;
            for (int index = 0; index < this.m_parameters.Count; index++)
            {
                var myItem = m_parameters[index];
                var otherItem = other.Parameters[index];
                if (myItem.TypeCode != otherItem.TypeCode)
                    return false;
                if (!object.Equals(myItem.Value, otherItem.Value))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = m_blockEntityId.GetHashCode();
                result = (result * 397) ^ ActionId.GetHashCode();
                return result;
            }
        }

        public override bool Init(MyObjectBuilder_ToolbarItem objectBuilder)
        {
            Debug.Assert(objectBuilder is MyObjectBuilder_ToolbarItemTerminalBlock, "Wrong definition put to toolbar");

            WantsToBeActivated = false;
            WantsToBeSelected = false;
            ActivateOnClick = true;

            m_block = null;

            var builder = (MyObjectBuilder_ToolbarItemTerminalBlock)objectBuilder;
            m_blockEntityId = builder.BlockEntityId;
            if (m_blockEntityId == 0)
            {
                m_wasValid = false;
                return false;
            }
            TryGetBlock();
            SetAction(builder._Action);

            if (builder.Parameters != null && builder.Parameters.Count > 0)
            {
                m_parameters.Clear();
                foreach (var item in builder.Parameters)
                {
                    m_parameters.Add(TerminalActionParameter.Deserialize(item.Value, item.TypeCode));
                }
            }
            return true;
        }

        private void RegisterEvents()
        {
            Debug.Assert(m_block != null);
            m_block.CustomNameChanged += block_CustomNameChanged;
            m_block.OnClose += block_OnClose;
        }

        private void UnregisterEvents()
        {
            Debug.Assert(m_block != null);
            m_block.CustomNameChanged -= block_CustomNameChanged;
            m_block.OnClose -= block_OnClose;
        }

        private void block_CustomNameChanged(MyTerminalBlock obj)
        {
            m_nameChanged = true;
        }

        private void block_OnClose(MyEntity obj)
        {
            UnregisterEvents();
            m_block = null;
        }

        public override void OnRemovedFromToolbar(MyToolbar toolbar)
        {
            if (m_block != null) UnregisterEvents();

            base.OnRemovedFromToolbar(toolbar);
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            MyObjectBuilder_ToolbarItemTerminalBlock output = (MyObjectBuilder_ToolbarItemTerminalBlock)MyToolbarItemFactory.CreateObjectBuilder(this);
            output.BlockEntityId = this.m_blockEntityId;
            output._Action = this.ActionId;
            
            output.Parameters.Clear();
            foreach (var item in m_parameters)
                output.Parameters.Add(item.GetObjectBuilder());
            
            return output;
        }
    }
}

using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;

namespace Sandbox.Game.Entities.Blocks
{
    [ProtoContract]
    struct ToolbarItem : IEqualityComparer<ToolbarItem>
    {
        // TODO Seems a bit strange to me to use IEqualityComparer<> rather than IEquatable<>...

        public static ToolbarItem FromObject(MyToolbarItem item)
        {
            var tItem = new ToolbarItem();
            tItem.EntityID = 0;
            var terminalItem = item as MyToolbarItemTerminalBlock;
            if (terminalItem != null)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalBlock;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
                if (block.Parameters != null && block.Parameters.Count > 0)
                {
                    // Allocation... I know this shouldn't be done but I can't think of an alternative.
                    tItem.Parameters = new ToolbarItemParameter[block.Parameters.Count];
                    for (var i = 0; i < tItem.Parameters.Length; i++)
                        tItem.Parameters[i] = new ToolbarItemParameter { TypeCode = block.Parameters[i].TypeCode, Value = block.Parameters[i].Value };
                }
            }
            else if (item is MyToolbarItemTerminalGroup)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalGroup;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
                tItem.GroupName = block.GroupName;
            }
            return tItem;
        }

        public static MyToolbarItem ToObject<T>(ToolbarItem msgItem) where T : MyCubeBlock
        {
            MyToolbarItem item = null;
            if (string.IsNullOrEmpty(msgItem.GroupName))
            {
                MyTerminalBlock block;
                if (MyEntities.TryGetEntityById<MyTerminalBlock>(msgItem.EntityID, out block))
                {
                    var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                    builder.Action = msgItem.Action;
                    if (msgItem.Parameters != null && msgItem.Parameters.Length > 0)
                    {
                        // TODO Allocations, allocations, allocations...
                        foreach (var parameter in msgItem.Parameters)
                        {
                            builder.Parameters.Add(new MyObjectBuilder_ToolbarItemActionParameter
                            {
                                TypeCode = parameter.TypeCode,
                                Value = parameter.Value
                            });
                        }
                    }
                    item = MyToolbarItemFactory.CreateToolbarItem(builder);
                }
            }
            else
            {
                T parent;
                if (MyEntities.TryGetEntityById<T>(msgItem.EntityID, out parent))
                {
                    var grid = parent.CubeGrid;
                    var groupName = msgItem.GroupName;
                    var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                    ;
                    if (@group != null)
                    {
                        var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(@group);
                        builder.Action = msgItem.Action;
                        builder.BlockEntityId = msgItem.EntityID;
                        item = MyToolbarItemFactory.CreateToolbarItem(builder);
                    }
                }
            }
            return item;
        }

        [ProtoMember(1)]
        public long EntityID;
        [ProtoMember(2)]
        public string GroupName;
        [ProtoMember(3)]
        public string Action;
        [ProtoMember(4)]
        public ToolbarItemParameter[] Parameters;

        public bool Equals(ToolbarItem x, ToolbarItem y)
        {
            if (x.EntityID != y.EntityID || x.GroupName != y.GroupName || x.Action != y.Action)
                return false;
            var xHasParams = x.Parameters != null && x.Parameters.Length > 0;
            var yHasParams = y.Parameters != null && y.Parameters.Length > 0;
            if (xHasParams != yHasParams)
                return false;
            if (xHasParams)
            {
                for (var i = 0; i < x.Parameters.Length; i++)
                    if (!x.Parameters[i].Equals(y.Parameters[i]))
                        return false;
            }
            return true;
        }

        public int GetHashCode(ToolbarItem obj)
        {
            unchecked
            {
                int result = obj.EntityID.GetHashCode();
                result = (result * 397) ^ obj.GroupName.GetHashCode();
                result = (result * 397) ^ obj.Action.GetHashCode();

                // TODO Is this required or should we fall back to ordinary comparison when parameters are in use?
                if (obj.Parameters != null)
                {
                    for (var i = 0; i < obj.Parameters.Length; i++)
                        result = (result * 397) ^ obj.Parameters[i].GetHashCode();
                }
                return result;
            }
        }
    }
}
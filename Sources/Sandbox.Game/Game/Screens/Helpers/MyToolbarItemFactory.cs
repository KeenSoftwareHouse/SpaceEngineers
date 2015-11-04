using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Plugins;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyToolbarItemDescriptor : MyFactoryTagAttribute
    {
        public MyToolbarItemDescriptor(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    public static class MyToolbarItemFactory
    {
        private static MyObjectFactory<MyToolbarItemDescriptor, MyToolbarItem> m_objectFactory;
        static MyToolbarItemFactory()
        {
            m_objectFactory = new MyObjectFactory<MyToolbarItemDescriptor, MyToolbarItem>();
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyToolbarItem)));
            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static MyToolbarItem CreateToolbarItem(MyObjectBuilder_ToolbarItem data)
        {
            MyToolbarItem item = m_objectFactory.CreateInstance(data.TypeId);
            return item.Init(data) ? item : null;
        }

        public static MyObjectBuilder_ToolbarItem CreateObjectBuilder(MyToolbarItem item)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_ToolbarItem>(item);
        }

        #region "auxiliary functions to create toolbar items"
        public static MyObjectBuilder_ToolbarItem ObjectBuilderFromDefinition(MyDefinitionBase defBase)
        {
            if (defBase is MyConsumableItemDefinition)
            {
                MyObjectBuilder_ToolbarItemConsumable consumableData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemConsumable>();
                consumableData.DefinitionId = defBase.Id;
                return consumableData;
            }
            else if ((defBase is MyPhysicalItemDefinition) && (defBase.Id.TypeId == typeof(MyObjectBuilder_PhysicalGunObject)))
            {
                MyObjectBuilder_ToolbarItemWeapon weaponData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemWeapon>();
                weaponData.DefinitionId = defBase.Id;
                return weaponData;
            }
            else if (defBase is MyCubeBlockDefinition)
            {
                MyCubeBlockDefinition blockDef = defBase as MyCubeBlockDefinition;
                MyObjectBuilder_ToolbarItemCubeBlock cubeData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemCubeBlock>();
                cubeData.DefinitionId = defBase.Id;
                return cubeData;
            }
            else if (defBase is MyAnimationDefinition)
            {
                var animData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemAnimation>();
                animData.DefinitionId = defBase.Id;
                return animData;
            }
            else if (defBase is MyVoxelHandDefinition)
            {
                var vhData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemVoxelHand>();
                vhData.DefinitionId = defBase.Id;
                return vhData;
            }
            else if (defBase is MyPrefabThrowerDefinition)
            {
                var ptData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemPrefabThrower>();
                ptData.DefinitionId = defBase.Id;
                return ptData;
            }
            else if (defBase is MyBotDefinition)
            {
                var bdData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemBot>();
                bdData.DefinitionId = defBase.Id;
                return bdData;
            }
            else if (defBase is MyAiCommandDefinition)
            {
                var acData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemAiCommand>();
                acData.DefinitionId = defBase.Id;
                return acData;
            }
            else if (defBase.Id.TypeId == typeof(MyObjectBuilder_RopeDefinition))
            {
                var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemRope>();
                ob.DefinitionId = defBase.Id;
                return ob;
            }
			else if (defBase is MyAreaMarkerDefinition)
			{
				var acData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemAreaMarker>();
				acData.DefinitionId = defBase.Id;
				return acData;
			}
            return new MyObjectBuilder_ToolbarItemEmpty();
        }

        public static string GetIconForTerminalGroup(MyBlockGroup group)
        {
            string output = "Textures\\GUI\\Icons\\GroupIcon.dds";
            bool genericType = false;
            var blocks = group.Blocks;
            if (blocks == null || blocks.Count == 0)
                return output;

            MyDefinitionBase def = blocks[0].BlockDefinition;
            foreach (var block in blocks)
            {
                if (!block.BlockDefinition.Equals(def))
                {
                    genericType = true;
                    break;
                }
            }
            if (!genericType)
                output = def.Icon;

            return output;
        }

        public static MyObjectBuilder_ToolbarItemTerminalBlock TerminalBlockObjectBuilderFromBlock(MyTerminalBlock block)
        {
            MyObjectBuilder_ToolbarItemTerminalBlock output = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalBlock>();
            output.BlockEntityId = block.EntityId;
            output.Action = null;

            return output;
        }

        public static MyObjectBuilder_ToolbarItemTerminalGroup TerminalGroupObjectBuilderFromGroup(MyBlockGroup group)
        {
            MyObjectBuilder_ToolbarItemTerminalGroup output = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalGroup>();
            output.GroupName = group.Name.ToString();
            output.Action = null;
            return output;
        }

        #endregion
    }
}

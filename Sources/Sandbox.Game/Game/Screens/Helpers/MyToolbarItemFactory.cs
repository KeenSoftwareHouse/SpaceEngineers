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
using VRage.Game;
using VRage.Plugins;
using VRage.ObjectBuilders;
using VRage.Game.Common;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Game.ObjectBuilders;
using VRage.Game.ModAPI.Ingame;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

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
#if XB1 // XB1_ALLINONEASSEMBLY
            m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyToolbarItem)));
            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
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
        public static MyToolbarItem CreateToolbarItemFromInventoryItem(IMyInventoryItem inventoryItem)
        {
            var itemDefinitionId = inventoryItem.GetDefinitionId();
            MyDefinitionBase itemDefinition;
            if (MyDefinitionManager.Static.TryGetDefinition(itemDefinitionId, out itemDefinition))
            {
                if ((itemDefinition is MyPhysicalItemDefinition) || (itemDefinition is MyCubeBlockDefinition))
                {
                    var itemBuilder = MyToolbarItemFactory.ObjectBuilderFromDefinition(itemDefinition);
                    if (itemBuilder is MyObjectBuilder_ToolbarItemMedievalWeapon)
                    {
                        var meWeaponBuilder = itemBuilder as MyObjectBuilder_ToolbarItemMedievalWeapon;
                        meWeaponBuilder.ItemId = inventoryItem.ItemId;
                    }

                    if (itemBuilder != null && !(itemBuilder is MyObjectBuilder_ToolbarItemEmpty))
                    {
                        return MyToolbarItemFactory.CreateToolbarItem(itemBuilder);
                    }
                }
            }

            return null;
        }

        public static MyObjectBuilder_ToolbarItem ObjectBuilderFromDefinition(MyDefinitionBase defBase)
        {
            if (defBase is MyUsableItemDefinition)
            {
                MyObjectBuilder_ToolbarItemUsable usableData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemUsable>();
                usableData.DefinitionId = defBase.Id;
                return usableData;
            }
            else if ((defBase is MyPhysicalItemDefinition) && (defBase.Id.TypeId == typeof(MyObjectBuilder_PhysicalGunObject)))
            {
                MyObjectBuilder_ToolbarItemWeapon weaponData = null;
                // CH: TODO: This is especially ugly, I know. But it's a quick fix. To do it properly, we will need to
                // remove this whole method and construct the inventory items solely based upon factory tags on toolbar item types
                if (MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    weaponData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemMedievalWeapon>();
                }
                else
                {
                    weaponData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemWeapon>();
                }
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
            else if (defBase is MyGridCreateToolDefinition)
            {
                var gctool = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemCreateGrid>();
                gctool.DefinitionId = defBase.Id;
                return gctool;

            }
            return new MyObjectBuilder_ToolbarItemEmpty();
        }

        public static string[] GetIconForTerminalGroup(MyBlockGroup group)
        {
            string[] output = new string[] { "Textures\\GUI\\Icons\\GroupIcon.dds" };
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
                output = def.Icons;

            return output;
        }

        public static MyObjectBuilder_ToolbarItemTerminalBlock TerminalBlockObjectBuilderFromBlock(MyTerminalBlock block)
        {
            MyObjectBuilder_ToolbarItemTerminalBlock output = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalBlock>();
            output.BlockEntityId = block.EntityId;
            output._Action = null;

            return output;
        }

        public static MyObjectBuilder_ToolbarItemTerminalGroup TerminalGroupObjectBuilderFromGroup(MyBlockGroup group)
        {
            MyObjectBuilder_ToolbarItemTerminalGroup output = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalGroup>();
            output.GroupName = group.Name.ToString();
            output._Action = null;
            return output;
        }

        public static MyObjectBuilder_ToolbarItemWeapon WeaponObjectBuilder()
        {
            MyObjectBuilder_ToolbarItemWeapon output = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemWeapon>();
            return output;
        }

        #endregion
    }
}

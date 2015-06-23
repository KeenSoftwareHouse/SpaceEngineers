using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using VRage;
using Sandbox.Game.Gui;
using System.Runtime.CompilerServices;
using VRage.Collections;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.ModAPI.Interfaces;

namespace Sandbox.Game.Gui
{
    struct TerminalControl
    {
        public MyGuiControlBase Control;
        public Action RefreshHandler;
    }

    delegate TerminalControl FactoryDelegate<in T>(T property, MyTerminalBlock[] blocks);

    public static class MyTerminalControlFactory
    {
        class BlockData
        {
            public MyUniqueList<ITerminalControl> Controls = new MyUniqueList<ITerminalControl>();
            public MyUniqueList<ITerminalAction> Actions = new MyUniqueList<ITerminalAction>();
        }

        static Dictionary<Type, BlockData> m_controls = new Dictionary<Type, BlockData>();

        /// <summary>
        /// Base class controls are added automatically
        /// </summary>
        public static void AddBaseClass<TBlock, TBase>()
            where TBlock : TBase
            where TBase : MyTerminalBlock
        {
            AddBaseClass(typeof(TBase), GetList<TBlock>());
        }

        public static void RemoveBaseClass<TBlock, TBase>()
            where TBlock : TBase
            where TBase : MyTerminalBlock
        {
            RemoveBaseClass(typeof(TBase), GetList<TBlock>());
        }

        public static void RemoveAllBaseClass<TBlock>()
            where TBlock : MyTerminalBlock
        {
            var list = GetList<TBlock>();
            var baseClass = typeof(TBlock).BaseType;
            while (baseClass != null)
            {
                RemoveBaseClass(baseClass, list);
                baseClass = baseClass.BaseType;
            }
        }
        
        public static void AddControl<TBlock>(int index, MyTerminalControl<TBlock> control)
            where TBlock : MyTerminalBlock
        {
            GetList<TBlock>().Controls.Insert(index, control);
            AddActions(index, control);
        }

        public static void AddControl<TBlock>(MyTerminalControl<TBlock> control)
            where TBlock : MyTerminalBlock
        {
            GetList<TBlock>().Controls.Add(control);
            AddActions(control);
        }

        public static void AddControl<TBase, TBlock>(MyTerminalControl<TBase> control)
            where TBlock : TBase
            where TBase : MyTerminalBlock
        {
            GetList<TBlock>().Controls.Add(control);
            AddActions(control);
        }


        public static void AddAction<TBlock>(int index, MyTerminalAction<TBlock> Action)
            where TBlock : MyTerminalBlock
        {
            GetList<TBlock>().Actions.Insert(index, Action);
        }

        public static void AddAction<TBlock>(MyTerminalAction<TBlock> Action)
            where TBlock : MyTerminalBlock
        {
            GetList<TBlock>().Actions.Add(Action);
        }

        public static void AddAction<TBase, TBlock>(MyTerminalAction<TBase> Action)
            where TBlock : TBase
            where TBase : MyTerminalBlock
        {
            GetList<TBlock>().Actions.Add(Action);
        }

        static void AddActions<TBlock>(MyTerminalControl<TBlock> block)
           where TBlock : MyTerminalBlock
        {
            if (block.Actions != null)
            {
                foreach (var a in block.Actions)
                {
                    AddAction<TBlock>((MyTerminalAction<TBlock>)a);
                }
            }
        }

        static void AddActions<TBlock>(int index, MyTerminalControl<TBlock> block)
           where TBlock : MyTerminalBlock
        {
            if (block.Actions != null)
            {
                foreach (var a in block.Actions)
                {
                    AddAction<TBlock>(index++, (MyTerminalAction<TBlock>)a);
                }
            }
        }

        public static UniqueListReader<ITerminalControl> GetControls(Type blockType)
        {
            return GetList(blockType).Controls.Items;
        }

        public static UniqueListReader<ITerminalAction> GetActions(Type blockType)
        {
            return GetList(blockType).Actions.Items;
        }

        public static void GetControls(Type blockType, List<ITerminalControl> resultList)
        {
            foreach (var item in GetList(blockType).Controls.Items)
            {
                resultList.Add(item);
            }
        }

        public static void GetValueControls(Type blockType, List<ITerminalProperty> resultList)
        {
            foreach (var item in GetList(blockType).Controls.Items)
            {
                var valueControl = item as ITerminalProperty;
                if(valueControl != null)
                    resultList.Add(valueControl);
            }
        }

        public static void GetActions(Type blockType, List<ITerminalAction> resultList)
        {
            foreach (var item in GetList(blockType).Actions.Items)
            {
                resultList.Add(item);
            }
        }

        public static void GetControls<TBlock>(List<MyTerminalControl<TBlock>> resultList)
            where TBlock : MyTerminalBlock
        {
            foreach (var item in GetList<TBlock>().Controls.Items)
            {
                resultList.Add((MyTerminalControl<TBlock>)item);
            }
        }

        public static void GetValueControls<TBlock>(Type blockType, List<ITerminalProperty> resultList)
            where TBlock : MyTerminalBlock
        {
            foreach (var item in GetList<TBlock>().Controls.Items)
            {
                var valueControl = item as ITerminalProperty;
                if (valueControl != null)
                    resultList.Add(valueControl);
            }
        }

        public static void GetActions<TBlock>(List<MyTerminalAction<TBlock>> resultList)
            where TBlock : MyTerminalBlock
        {
            foreach (var item in GetList<TBlock>().Actions.Items)
            {
                resultList.Add((MyTerminalAction<TBlock>)item);
            }
        }

        private static void RemoveBaseClass(Type baseClass, BlockData resultList)
        {
            BlockData baseList;
            if (m_controls.TryGetValue(baseClass, out baseList))
            {
                foreach (var item in baseList.Controls.Items)
                {
                    resultList.Controls.Remove(item);
                }

                foreach (var item in baseList.Actions.Items)
                {
                    resultList.Actions.Remove(item);
                }
            }
        }

        private static void AddBaseClass(Type baseClass, BlockData resultList)
        {
            RuntimeHelpers.RunClassConstructor(baseClass.TypeHandle);

            BlockData baseList;
            if (m_controls.TryGetValue(baseClass, out baseList))
            {
                foreach (var item in baseList.Controls.Items)
                {
                    resultList.Controls.Add(item);
                }

                foreach (var item in baseList.Actions.Items)
                {
                    resultList.Actions.Add(item);
                }
            }
        }

        private static BlockData GetList<TBlock>()
        {
            Debug.Assert(!typeof(TBlock).IsInterface, "Don't pass interface, use AddControl<TBase, TBlock>()");
            return GetList(typeof(TBlock));
        }

        private static BlockData GetList(Type type)
        {
            BlockData list;
            if (!m_controls.TryGetValue(type, out list))
            {
                list = new BlockData();
                m_controls[type] = list;

                var baseClass = type.BaseType;
                while (baseClass != null)
                {
                    AddBaseClass(baseClass, list);
                    baseClass = baseClass.BaseType;
                }
            }
            return list;
        }
    }
}

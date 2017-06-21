using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Components;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Plugins;

namespace Sandbox.Game.Gui
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyTerminalControls : MySessionComponentBase, IMyTerminalControls
    {
        private static MyTerminalControls m_instance = null;

        public static MyTerminalControls Static
        {
            get
            {
                return m_instance;
            }
        }

        public MyTerminalControls()
        {
            m_instance = this;
        }

        protected override void UnloadData()
        {
            m_customControlGetter = null;
        }

        private event CustomControlGetDelegate m_customControlGetter = null;
        public event CustomControlGetDelegate CustomControlGetter
        {
            remove
            {
                m_customControlGetter -= value;
            }

            add
            {
                m_customControlGetter += value;
            }
        }

        private event CustomActionGetDelegate m_customActionGetter = null;
        public event CustomActionGetDelegate CustomActionGetter
        {
            remove
            {
                m_customActionGetter -= value;
            }

            add
            {
                m_customActionGetter += value;
            }
        }

        public List<ITerminalControl> GetControls(IMyTerminalBlock block)
        {
            if (m_customControlGetter != null)
            {
                // This is a double list copy :(
                var controlList = MyTerminalControlFactory.GetControls(block.GetType()).Cast<IMyTerminalControl>().ToList();
                m_customControlGetter(block, controlList);
                return controlList.Cast<ITerminalControl>().ToList();
            }
            else
            {
                return MyTerminalControlFactory.GetControls(block.GetType()).ToList();
            }
        }

        public void GetControls<TBlock>(out List<IMyTerminalControl> items)
        {
            items = new List<IMyTerminalControl>();
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            var blockList = MyTerminalControlFactory.GetList(producedType);
            foreach (var item in blockList.Controls)
            {
                items.Add((IMyTerminalControl)item);
            }
        }

        public void AddControl<TBlock>(IMyTerminalControl item)
        {
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            MyTerminalControlFactory.AddControl(producedType, (ITerminalControl)item);
            MyTerminalControlFactory.AddActions(producedType, (ITerminalControl)item);
        }

        public void RemoveControl<TBlock>(IMyTerminalControl item)
        {
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            MyTerminalControlFactory.RemoveControl(producedType, item);
        }

        /// <summary>
        /// This will create a control to be added to the terminal screen.  This only really applies to ModAPI, as MyTerminalControlFactory class isn't whitelist
        /// </summary>
        /// <typeparam name="TControl">Interface of control type</typeparam>
        /// <param name="blockType">Block type to add this control to</param>
        /// <param name="id">Identifier of this control</param>
        /// <returns>Interface of created control</returns>
        public TControl CreateControl<TControl, TBlock>(string id)
        {
            if (!IsTypeValid<TBlock>())
                return default(TControl);

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return default(TControl);

            // Sanity check
            if (!typeof(MyTerminalBlock).IsAssignableFrom(producedType))
                return default(TControl);

            if (!typeof(IMyTerminalControl).IsAssignableFrom(typeof(TControl)))
                return default(TControl);

            if(!MyTerminalControlFactory.AreControlsCreated(producedType))
            {
                MyTerminalControlFactory.EnsureControlsAreCreated(producedType);
            }

            Type controlType = typeof(TControl);

            // This can be done better -- IMyTerminalControlXXX matches MyTerminalControlXXX, just see if I can
            // search for the classes in the assembly and map (interface) => (class).  Parameters may be an issue, but
            // I may just be able to pull parameter count and default all except for Id (might not be worth my time, just to save 19 lines of code)

            // Yes this is ugly, but I have to work around the fact that both MyTerminalControls and all block entities are not whitelisted, and the
            // control system expects both of those things.  So I need to wrap them for mods. 
            if (controlType == typeof(IMyTerminalControlTextbox))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlTextbox<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlButton))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlButton<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, null });
            else if (controlType == typeof(IMyTerminalControlCheckbox))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlCheckbox<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlColor))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlColor<>), producedType, new object[] { id, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlCombobox))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlCombobox<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlListbox))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlListbox<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, false, 0 });
            else if (controlType == typeof(IMyTerminalControlOnOffSwitch))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlOnOffSwitch<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlSeparator))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlSeparator<>), producedType, new object[] { });
            else if (controlType == typeof(IMyTerminalControlSlider))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlSlider<>), producedType, new object[] { id, MyStringId.NullOrEmpty, MyStringId.NullOrEmpty });
            else if (controlType == typeof(IMyTerminalControlLabel))
                return CreateGenericControl<TControl>(typeof(MyTerminalControlLabel<>), producedType, new object[] { MyStringId.NullOrEmpty });

            return default(TControl);
        }

        public IMyTerminalControlProperty<TValue> CreateProperty<TValue, TBlock>(string id)
        {
            if (!IsTypeValid<TBlock>())
                return null;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return null;

            Type controlType = typeof(MyTerminalControlProperty<,>);
            Type genericControlType = controlType.MakeGenericType(producedType, typeof(TValue));
            return (IMyTerminalControlProperty<TValue>)Activator.CreateInstance(genericControlType, new object[] { id });
        }

        private Type GetProducedType<TBlock>()
        {
            Type producedType;
            // Originally I checked MyObjectBuilder_XX and mapped them to the class, but now I'm going to do ModAPI interfaces as well.  I left
            // the old method here, but it can be removed
            if (typeof(TBlock).IsInterface)
                producedType = FindTerminalTypeFromInterface<TBlock>();
            else
                producedType = MyCubeBlockFactory.GetProducedType(typeof(TBlock));

            return producedType;
        }

        private Type FindTerminalTypeFromInterface<TBlock>()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            var type = MyAssembly.GetTypes().FirstOrDefault(x => typeof(TBlock).IsAssignableFrom(x) && !x.IsInterface);
            if (type == null)
            {
                System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
            }
            return type;
#else // !XB1
            var type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => typeof(TBlock).IsAssignableFrom(x) && !x.IsInterface);
            if (type == null)
            {
                var gameAssembly = MyPlugins.GameAssembly;
                if (gameAssembly != null)
                    type = gameAssembly.GetTypes().FirstOrDefault(x => typeof(TBlock).IsAssignableFrom(x) && !x.IsInterface);
            }

            return type;
#endif // !XB1
        }

        private bool IsTypeValid<TBlock>()
        {
            if (!typeof(TBlock).IsInterface)
            {
                if (!typeof(MyObjectBuilder_TerminalBlock).IsAssignableFrom(typeof(TBlock)))
                    return true;
            }
            else
            {
                if (typeof(ModAPI.Ingame.IMyTerminalBlock).IsAssignableFrom(typeof(TBlock)))
                    return true;
            }

            return false;
        }

        private TControl CreateGenericControl<TControl>(Type controlType, Type blockType, object[] args)
        {
            Type genericControlType = controlType.MakeGenericType(blockType);
            return (TControl)(IMyTerminalControl)Activator.CreateInstance(genericControlType, args);
        }

        public List<ITerminalAction> GetActions(IMyTerminalBlock block)
        {
            if (m_customActionGetter != null)
            {
                // This is a double list copy :(
                var actionList = MyTerminalControlFactory.GetActions(block.GetType()).Cast<IMyTerminalAction>().ToList();
                m_customActionGetter(block, actionList);
                return actionList.Cast<ITerminalAction>().ToList();
            }
            else
            {
                return MyTerminalControlFactory.GetActions(block.GetType()).ToList();
            }
        }

        public void GetActions<TBlock>(out List<IMyTerminalAction> items)
        {
            items = new List<IMyTerminalAction>();
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            var blockList = MyTerminalControlFactory.GetList(producedType);
            foreach (var item in blockList.Actions)
            {
                items.Add((IMyTerminalAction)item);
            }
        }

        public void AddAction<TBlock>(IMyTerminalAction action)
        {
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            var blockList = MyTerminalControlFactory.GetList(producedType).Actions;
            blockList.Add((ITerminalAction)action);
        }

        public void RemoveAction<TBlock>(IMyTerminalAction action)
        {
            if (!IsTypeValid<TBlock>())
                return;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return;

            var blockList = MyTerminalControlFactory.GetList(producedType).Actions;
            blockList.Remove((ITerminalAction)action);
        }

        public IMyTerminalAction CreateAction<TBlock>(string id)
        {
            if (!IsTypeValid<TBlock>())
                return null;

            Type producedType = GetProducedType<TBlock>();
            if (producedType == null)
                return null;

            Type genericActionType = typeof(MyTerminalAction<>);
            Type actionType = genericActionType.MakeGenericType(producedType);

            return (IMyTerminalAction)Activator.CreateInstance(actionType, new object[] { id, new StringBuilder(""), "" });
        }
    }
}

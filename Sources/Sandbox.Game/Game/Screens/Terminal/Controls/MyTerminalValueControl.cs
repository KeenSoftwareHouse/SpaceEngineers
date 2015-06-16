using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public abstract class MyTerminalValueControl<TBlock, TValue> : MyTerminalControl<TBlock>, ITerminalValueControl<TBlock, TValue>
        where TBlock : MyTerminalBlock
    {
        public MyTerminalValueControl(string id)
            : base(id)
        {
        }

        public abstract TValue GetValue(TBlock block);
        public abstract void SetValue(TBlock block, TValue value);
        public abstract TValue GetDefaultValue(TBlock block);
        public abstract TValue GetMininum(TBlock block);
        public abstract TValue GetMaximum(TBlock block);

        public TValue GetValue(ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetValue(((TBlock)block));
        }

        public void SetValue(ModAPI.Ingame.IMyCubeBlock block, TValue value)
        {
            SetValue(((TBlock)block), value);
        }

        public TValue GetDefaultValue(ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetDefaultValue(((TBlock)block));
        }

        public TValue GetMininum(ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetMininum(((TBlock)block));
        }

        public TValue GetMaximum(ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetMaximum(((TBlock)block));
        }

        string ITerminalProperty.Id
        {
            get { return Id; }
        }

        string ITerminalProperty.TypeName
        {
            get { return typeof(TValue).Name; }
        }
    }
}

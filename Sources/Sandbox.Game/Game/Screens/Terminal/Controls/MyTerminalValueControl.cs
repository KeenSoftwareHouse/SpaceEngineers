using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using System.Linq.Expressions;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public abstract class MyTerminalValueControl<TBlock, TValue> : MyTerminalControl<TBlock>, ITerminalValueControl<TBlock, TValue>
        where TBlock : MyTerminalBlock
    {
        public delegate TValue GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, TValue value);
        public delegate void SerializerDelegate(BitStream stream, ref TValue value);

        /// <summary>
        /// Getter which gets value from block.
        /// Can be set by anyone, but used only by MyTerminalValueControl.
        /// If you need to get the value, use GetValue method.
        /// </summary>
        public GetterDelegate Getter { private get; set; }

        /// <summary>
        /// Setter which sets value to block.
        /// Can be set by anyone, but used only by MyTerminalValueControl.
        /// If you need to set the value, use SetValue method, which does handles notification.
        /// </summary>
        public SetterDelegate Setter { private get; set; }

        /// <summary>
        /// Serializer which (de)serializes the value.
        /// </summary>
        public SerializerDelegate Serializer;

        public Expression<Func<TBlock, TValue>> MemberExpression
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }

        public MyTerminalValueControl(string id)
            : base(id)
        {
        }

        public virtual TValue GetValue(TBlock block)
        {
            return Getter(block);
        }

        public virtual void SetValue(TBlock block, TValue value)
        {
            Setter(block, value);
            block.NotifyTerminalValueChanged(this);
        }
        
        public virtual void Serialize(BitStream stream, TBlock block)
        {
            if (stream.Reading)
            {
                TValue value = default(TValue);
                Serializer(stream, ref value);
                SetValue(block, value);
            }
            else
            {
                TValue value = GetValue(block);
                Serializer(stream, ref value);
            }
        }

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

        public void Serialize(BitStream stream, MyTerminalBlock block)
        {
            Serialize(stream, (TBlock)block);
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

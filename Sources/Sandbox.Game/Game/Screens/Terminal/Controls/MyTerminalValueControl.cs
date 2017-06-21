using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using System.Linq.Expressions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public abstract class MyTerminalValueControl<TBlock, TValue> : MyTerminalControl<TBlock>, ITerminalValueControl<TBlock, TValue>, IMyTerminalValueControl<TValue>
        where TBlock : MyTerminalBlock
    {
        public delegate TValue GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, TValue value);
        public delegate void SerializerDelegate(BitStream stream, ref TValue value);
        public delegate void ExternalSetterDelegate(IMyTerminalBlock block, TValue value);

        /// <summary>
        /// Getter which gets value from block.
        /// Can be set by anyone, but used only by MyTerminalValueControl.
        /// If you need to get the value, use GetValue method.
        /// </summary>
        public GetterDelegate Getter { get; set; }

        /// <summary>
        /// Setter which sets value to block.
        /// Can be set by anyone, but used only by MyTerminalValueControl.
        /// If you need to set the value, use SetValue method, which does handles notification.
        /// </summary>
        public SetterDelegate Setter { get; set; }

        /// <summary>
        /// Serializer which (de)serializes the value.
        /// </summary>
        public SerializerDelegate Serializer;
#if !XB1
        public Expression<Func<TBlock, TValue>> MemberExpression
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }
#endif

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
        [Obsolete("Use GetMinimum instead")]
        public TValue GetMininum(TBlock block)
        {
            return GetMinimum(block);
        }
        public abstract TValue GetMinimum(TBlock block);
        public abstract TValue GetMaximum(TBlock block);

        public TValue GetValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetValue(((TBlock)block));
        }

        public void SetValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, TValue value)
        {
            SetValue(((TBlock)block), value);
        }

        public TValue GetDefaultValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetDefaultValue(((TBlock)block));
        }

        [Obsolete("Use GetMinimum instead")]
        public TValue GetMininum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetMinimum(((TBlock)block));
        }

        public TValue GetMinimum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            return GetMinimum(((TBlock)block));
        }

        public TValue GetMaximum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
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

        Func<IMyTerminalBlock, TValue> IMyTerminalValueControl<TValue>.Getter
        {
            get
            {
                GetterDelegate oldGetter = Getter;
                Func<IMyTerminalBlock, TValue> func = (x) =>
                {
                    return oldGetter((TBlock)x);
                };

                return func;
            }

            set
            {
                Getter = new GetterDelegate(value);
            }
        }

        Action<IMyTerminalBlock, TValue> IMyTerminalValueControl<TValue>.Setter
        {
            get
            {
                SetterDelegate oldSetter = Setter;
                Action<IMyTerminalBlock, TValue> func = (x, y) =>
                {
                    oldSetter((TBlock)x, y);
                };

                return func;
            }

            set
            {
                Setter = new SetterDelegate(value);
            }
        }
    }
}

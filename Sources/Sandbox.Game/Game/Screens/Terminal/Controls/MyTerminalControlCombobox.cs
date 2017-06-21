using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRage.ModAPI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlCombobox<TBlock> : MyTerminalControl<TBlock>, ITerminalControlSync, IMyTerminalControlCombobox
        where TBlock : MyTerminalBlock
    {
        public delegate void SerializerDelegate(BitStream stream, ref long value);

        private static List<MyTerminalControlComboBoxItem> m_handlerItems = new List<MyTerminalControlComboBoxItem>();

        public MyStringId Title;
        public MyStringId Tooltip;

        private MyGuiControlCombobox m_comboBox;

        public delegate void ComboBoxContentDelegate(TBlock block, ICollection<MyTerminalControlComboBoxItem> comboBoxContent);
        public ComboBoxContentDelegate ComboBoxContentWithBlock;
        public Action<List<MyTerminalControlComboBoxItem>> ComboBoxContent;
        public Func<TBlock, long> Getter { private get; set; }
        public Action<TBlock, long> Setter { private get; set; }

        public SerializerDelegate Serializer;

        public MyTerminalControlCombobox(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
            SetSerializerDefault();
        }

        /// <summary>
        /// Sets default serializer which serializes always 8B
        /// </summary>
        public void SetSerializerDefault()
        {
            Serializer = delegate(BitStream stream, ref long value) { stream.Serialize(ref value); };
        }

        /// <summary>
        /// Serializes values as 0 or 1
        /// </summary>
        public void SetSerializerBit()
        {
            Serializer = delegate(BitStream stream, ref long value)
            {
                if (stream.Reading) value = stream.ReadBool() ? 1 : 0;
                else stream.WriteBool(value != 0);
            };
        }

        /// <summary>
        /// Sets optimal serizalizer for range of two values (with uniform probability)
        /// </summary>
        public void SetSerializerRange(int minInclusive, int maxInclusive)
        {
            uint valueCount = (uint)((long)maxInclusive - (long)minInclusive + 1);
            valueCount = MathHelper.GetNearestBiggerPowerOfTwo(valueCount);
            int bitCount = MathHelper.Log2(valueCount);

            Serializer = delegate(BitStream stream, ref long value)
            {
                if (stream.Reading) value = (long)(stream.ReadUInt64()) + minInclusive;
                else stream.WriteUInt64((ulong)(value - minInclusive), bitCount);
            };
        }

        /// <summary>
        /// Sets variant length serializer, smaller number takes less bytes than larger.
        /// </summary>
        public void SetSerializerVariant(bool usesNegativeValues = false)
        {
            if (usesNegativeValues)
                Serializer = delegate(BitStream stream, ref long value) { stream.SerializeVariant(ref value); };
            else
                Serializer = delegate(BitStream stream, ref long value)
                {
                    unchecked
                    {
                        if (stream.Reading) value = stream.ReadInt64();
                        else stream.WriteInt64(value);
                    }
                };
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_comboBox = new MyGuiControlCombobox(toolTip: MyTexts.GetString(Tooltip), size: new VRageMath.Vector2(0.23f, 0.04f));
            m_comboBox.VisualStyle = MyGuiControlComboboxStyleEnum.Terminal;
            m_comboBox.ItemSelected += OnItemSelected;

            return new MyGuiControlBlockProperty(
                MyTexts.GetString(Title),
                MyTexts.GetString(Tooltip),
                m_comboBox,
                MyGuiControlBlockPropertyLayoutEnum.Vertical);
        }

        public long GetValue(TBlock block)
        {
            return Getter(block);
        }

        public void SetValue(TBlock block, long value)
        {
            if (Getter(block) != value)
            {
                Setter(block, value);
                block.NotifyTerminalValueChanged(this);
            }
        }

        void OnItemSelected()
        {
            if (m_comboBox.GetItemsCount() > 0)
            {
                var selected = m_comboBox.GetSelectedKey();
                foreach (var block in TargetBlocks)
                {
                    SetValue(block, selected);
                }
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();

            var first = FirstBlock;
            if (first != null)
            {
                // clear current listed items
                m_comboBox.ClearItems();
                m_handlerItems.Clear();
                // add items
                if (ComboBoxContent != null)
                {
                    ComboBoxContent(m_handlerItems);
                    foreach (var item in m_handlerItems)
                    {
                        m_comboBox.AddItem(item.Key, item.Value);
                    }

                    var value = GetValue(first);
                    if (m_comboBox.GetSelectedKey() != value)
                        m_comboBox.SelectItemByKey(value);
                }
                // add items
                if (ComboBoxContentWithBlock != null)
                {
                    ComboBoxContentWithBlock(first, m_handlerItems);
                    foreach (var item in m_handlerItems)
                    {
                        m_comboBox.AddItem(item.Key, item.Value);
                    }

                    var value = GetValue(first);
                    if (m_comboBox.GetSelectedKey() != value)
                        m_comboBox.SelectItemByKey(value);
                }
            }
        }

        /// <summary>
        /// Implements IMyTerminalControlCombobox for Mods
        /// </summary>
        MyStringId IMyTerminalControlTitleTooltip.Title
        {
            get
            {
                return Title;
            }

            set
            {
                Title = value;
            }
        }

        MyStringId IMyTerminalControlTitleTooltip.Tooltip
        {
            get
            {
                return Tooltip;
            }

            set
            {
                Tooltip = value;
            }
        }

        Action<List<MyTerminalControlComboBoxItem>> IMyTerminalControlCombobox.ComboBoxContent
        {
            get
            {
                Action<List<MyTerminalControlComboBoxItem>> oldComboBoxContent = ComboBoxContent;
                Action<List<MyTerminalControlComboBoxItem>> action = (x) =>
                {
                    oldComboBoxContent(x);
                };

                return action;
            }

            set
            {
                ComboBoxContent = value;
            }
        }

        Action<IMyTerminalBlock, List<MyTerminalControlComboBoxItem>> ComboBoxContentWithBlockAction
        {
            set
            {
                ComboBoxContentWithBlock = new ComboBoxContentDelegate((block, comboBoxContent) =>
                {
                    List<MyTerminalControlComboBoxItem> wrapList = new List<MyTerminalControlComboBoxItem>();
                    value(block, wrapList);
                    foreach (var wrapItem in wrapList)
                    {
                        var item = new MyTerminalControlComboBoxItem() { Key = wrapItem.Key, Value = wrapItem.Value};
                        comboBoxContent.Add(item);
                    }
                });
            }
        }

        Func<IMyTerminalBlock, long> IMyTerminalValueControl<long>.Getter
        {
            get
            {
                Func<TBlock, long> oldGetter = Getter;
                Func<IMyTerminalBlock, long> func = (x) =>
                {
                    return oldGetter((TBlock)x);
                };

                return func;
            }

            set
            {
                Getter = value;
            }
        }

        Action<IMyTerminalBlock, long> IMyTerminalValueControl<long>.Setter
        {
            get
            {
                Action<TBlock, long> oldSetter = Setter;
                Action<IMyTerminalBlock, long> action = (x, y) =>
                {
                    oldSetter((TBlock)x, y);
                };

                return action;
            }

            set
            {
                Setter = value;
            }
        }

        string ITerminalProperty.Id { get { return Id; } }
        string ITerminalProperty.TypeName { get { return typeof(TBlock).Name; } }

        public void Serialize(BitStream stream, MyTerminalBlock block)
        {
            if (stream.Reading)
            {
                long value = 0;
                Serializer(stream, ref value);
                SetValue((TBlock)block, value);
            }
            else
            {
                long value = GetValue((TBlock)block);
                Serializer(stream, ref value);
            }
        }
    }
}

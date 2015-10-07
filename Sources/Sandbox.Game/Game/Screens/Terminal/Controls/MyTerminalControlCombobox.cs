
using Sandbox.Common.ObjectBuilders.Gui;
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

namespace Sandbox.Game.Gui
{
    public struct TerminalComboBoxItem
    {
        public long Key;
        public MyStringId Value;
    }

    class MyTerminalControlCombobox<TBlock> : MyTerminalControl<TBlock>, ITerminalControlSync
        where TBlock : MyTerminalBlock
    {
        public delegate void SerializerDelegate(BitStream stream, ref long value);

        private static List<TerminalComboBoxItem> m_handlerItems = new List<TerminalComboBoxItem>();

        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

        private MyGuiControlCombobox m_comboBox;

        public Action<List<TerminalComboBoxItem>> ComboBoxContent;
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
            Setter(block, value);
            block.NotifyTerminalValueChanged(this);
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
            }
        }

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

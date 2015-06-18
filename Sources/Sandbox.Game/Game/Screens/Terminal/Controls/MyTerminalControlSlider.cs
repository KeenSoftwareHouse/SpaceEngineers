
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Graphics.GUI;
using System;
using System.Linq.Expressions;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlSlider<TBlock> : MyTerminalValueControl<TBlock, float>
        where TBlock : MyTerminalBlock
    {
        public delegate float FloatFunc(TBlock block, float val);
        public delegate float GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, float value);

        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

        private MyGuiControlSlider m_slider;
        private MyGuiControlBlockProperty m_control;
        private Action<float> m_amountConfirmed;

        public bool AmountDialogEnabled = true;

        public GetterDelegate Getter;
        public SetterDelegate Setter;
        public WriterDelegate Writer;
        public WriterDelegate CompactWriter;

        public FloatFunc Normalizer = (b, f) => f;
        public FloatFunc Denormalizer = (b, f) => f;

        public GetterDelegate DefaultValueGetter;

        // This is not supported as method, because label and tooltip has issues
        //public Writer Title;
        //public Writer Tooltip;

        public Expression<Func<TBlock, float>> Member
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }

        public float? DefaultValue
        {
            set
            {
                DefaultValueGetter = value.HasValue ? new GetterDelegate((block) => value.Value) : null;
            }
        }

        public string Formatter
        {
            set
            {
                Writer = value != null ? new WriterDelegate((block, result) => result.AppendFormat(value, Getter(block))) : null;
            }
        }

        private Action<MyGuiControlSlider> m_valueChanged;

        public MyTerminalControlSlider(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;

            CompactWriter = CompactWriterMethod;
            m_amountConfirmed = AmountSetter;
        }

        // TODO: separate slider gui to different class
        protected override MyGuiControlBase CreateGui()
        {
            m_slider = new MyGuiControlSlider(
                width: PREFERRED_CONTROL_WIDTH,
                position: Vector2.Zero,
                minValue: 0,
                maxValue: 1,
                defaultValue: null);

            m_valueChanged = OnValueChange;
            m_slider.ValueChanged = m_valueChanged;
            m_slider.SliderClicked = OnSliderClicked;

            m_control = new MyGuiControlBlockProperty(MyTexts.GetString(Title), MyTexts.GetString(Tooltip), m_slider);
            return m_control;
        }

        public void CompactWriterMethod(TBlock block, StringBuilder appendTo)
        {
            //MyValueFormatter.AppendGenericInBestUnit(Getter(block), 0, appendTo);
            int start = appendTo.Length;
            Writer(block, appendTo);
            int dotIndex = FirstIndexOf(appendTo, start, ".,");
            if (dotIndex >= 0)
            {
                RemoveNumbersFrom(dotIndex, appendTo);
            }
        }

        private int FirstIndexOf(StringBuilder sb, int start, string chars, int count = int.MaxValue)
        {
            int end = Math.Min(start + count, sb.Length);
            for (int i = start; i < end; i++)
            {
                var myC = sb[i];
                for (int c = 0; c < chars.Length; c++)
                {
                    if (myC == chars[c])
                        return i;
                }
            }
            return -1;
        }

        private void RemoveNumbersFrom(int index, StringBuilder sb)
        {
            sb.Remove(index, 1);
            while (index < sb.Length && ((sb[index] >= '0' && sb[index] <= '9') || sb[index] == ' '))
            {
                sb.Remove(index, 1);
            }
        }

        public void SetLimits(float min, float max)
        {
            Normalizer = (block, f) => MathHelper.Clamp((f - min) / (max - min), 0, 1);
            Denormalizer = (block, f) => MathHelper.Clamp(min + f * (max - min), min, max);
        }

        public void SetLogLimits(float min, float max)
        {
            Normalizer = (block, f) => MathHelper.Clamp(MathHelper.InterpLogInv(f, min, max), 0, 1);
            Denormalizer = (block, f) => MathHelper.Clamp(MathHelper.InterpLog(f, min, max), min, max);
        }

        public void SetDualLogLimits(float absMin, float absMax, float centerBand)
        {
            Normalizer = (block, f) => DualLogNormalizer(block, f, absMin, absMax, centerBand);
            Denormalizer = (block, f) => DualLogDenormalizer(block, f, absMin, absMax, centerBand);
        }

        private static float DualLogDenormalizer(TBlock block, float value, float min, float max, float centerBand)
        {
            float shiftedVal = value * 2.0f - 1.0f;
            if (Math.Abs(shiftedVal) < centerBand) return 0.0f;
            float scaledVal = (Math.Abs(shiftedVal) - centerBand) / (1.0f - centerBand);
            return MathHelper.Clamp(MathHelper.InterpLog(scaledVal, min, max), min, max) * Math.Sign(shiftedVal);
        }

        private static float DualLogNormalizer(TBlock block, float value, float min, float max, float centerBand)
        {
            if (Math.Abs(value) < min) return 0.5f;
            float width = 0.5f - centerBand / 2.0f;
            float result = MathHelper.Clamp(MathHelper.InterpLogInv(Math.Abs(value), min, max), 0, 1) * width;
            if (value < 0.0f) result = width - result;
            else result = result + width + centerBand;
            return result;
        }

        public void SetLimits(GetterDelegate minGetter, GetterDelegate maxGetter)
        {
            Normalizer = (block, f) =>
                {
                    float min = minGetter(block);
                    float max = maxGetter(block);
                    return MathHelper.Clamp((f - min) / (max - min), 0, 1);
                };
            Denormalizer = (block, f) =>
                {
                    float min = minGetter(block);
                    float max = maxGetter(block);
                    return MathHelper.Clamp(min + f * (max - min), min, max);
                };
        }

        public void SetLogLimits(GetterDelegate minGetter, GetterDelegate maxGetter)
        {
            Normalizer = (block, f) =>
            {
                float min = minGetter(block);
                float max = maxGetter(block);
                return MathHelper.Clamp(MathHelper.InterpLogInv(f, min, max), 0, 1);
            };
            Denormalizer = (block, f) =>
            {
                float min = minGetter(block);
                float max = maxGetter(block);
                return MathHelper.Clamp(MathHelper.InterpLog(f, min, max), min, max);
            };
        }

        public void SetDualLogLimits(GetterDelegate minGetter, GetterDelegate maxGetter, float centerBand)
        {
            Normalizer = (block, f) =>
            {
                float min = minGetter(block);
                float max = maxGetter(block);
                return DualLogNormalizer(block, f, min, max, centerBand);
            };
            Denormalizer = (block, f) =>
            {
                float min = minGetter(block);
                float max = maxGetter(block);
                return DualLogDenormalizer(block, f, min, max, centerBand);
            };
        }

        protected override void OnUpdateVisual()
        {
            // TODO: we don't support 'undefined' value in GUI, we just show first block values
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                m_slider.ValueChanged = null;
                m_slider.DefaultValue = DefaultValueGetter != null ? Normalizer(first, DefaultValueGetter(first)) : default(float?);
                m_slider.Value = Normalizer(first, Getter(first));
                m_slider.ValueChanged = m_valueChanged;

                m_control.SetDetailedInfo(Writer, first);
            }
        }

        void OnValueChange(MyGuiControlSlider slider)
        {
            SetValue(slider.Value);
            m_control.SetDetailedInfo(Writer, FirstBlock);
        }

        private void SetValue(float value)
        {
            foreach (var item in TargetBlocks)
            {
                Setter(item, Denormalizer(item, value));
            }
        }

        void AmountSetter(float value)
        {
            var first = FirstBlock;
            if (first != null)
            {
                m_slider.Value = Normalizer(first, value);
            }
        }

        bool OnSliderClicked(MyGuiControlSlider arg)
        {
            var first = FirstBlock;
            if (AmountDialogEnabled && MyInput.Static.IsAnyCtrlKeyPressed() && first != null)
            {
                float min = Denormalizer(first, 0);
                float max = Denormalizer(first, 1);
                float val = Denormalizer(first, arg.Value);

                // TODO: allocations, needs GUI redo
                MyGuiScreenDialogAmount dialog = new MyGuiScreenDialogAmount(min, max, defaultAmount: val, caption: MySpaceTexts.DialogAmount_SetValueCaption);
                dialog.OnConfirmed += m_amountConfirmed;
                MyGuiSandbox.AddScreen(dialog);
                return true;
            }
            return false;
        }

        void IncreaseAction(TBlock block, float step)
        {
            float curr = Normalizer(block, Getter(block));
            Setter(block, Denormalizer(block, MathHelper.Clamp(curr + step, 0, 1)));
        }

        void DecreaseAction(TBlock block, float step)
        {
            float curr = Normalizer(block, Getter(block));
            Setter(block, Denormalizer(block, MathHelper.Clamp(curr - step, 0, 1)));
        }

        void ResetAction(TBlock block)
        {
            if (DefaultValueGetter != null)
                Setter(block, DefaultValueGetter(block));
        }

        void ActionWriter(TBlock block, StringBuilder appendTo)
        {
            (CompactWriter ?? Writer)(block, appendTo);
        }

        private void SetActions(params MyTerminalAction<TBlock>[] actions)
        {
            Actions = actions;
        }

        public void EnableActions(string increaseIcon, string decreaseIcon, StringBuilder increaseName, StringBuilder decreaseName, float step, string resetIcon = null, StringBuilder resetName = null)
        {
            var increase = new MyTerminalAction<TBlock>("Increase" + Id, increaseName, (b) => IncreaseAction(b, step), ActionWriter, increaseIcon);
            var decrease = new MyTerminalAction<TBlock>("Decrease" + Id, decreaseName, (b) => DecreaseAction(b, step), ActionWriter, decreaseIcon);
            if (resetIcon != null)
                SetActions(increase, decrease, new MyTerminalAction<TBlock>("Reset" + Id, resetName, ResetAction, ActionWriter, resetIcon));
            else
                SetActions(increase, decrease);
        }

        public override float GetValue(TBlock block)
        {
            return Getter(block);
        }

        public override void SetValue(TBlock block, float value)
        {
            Setter(block, MathHelper.Clamp(value, Denormalizer(block, 0), Denormalizer(block, 1)));
        }

        public override float GetDefaultValue(TBlock block)
        {
            return DefaultValueGetter(block);
        }

        public override float GetMininum(TBlock block)
        {
            return Denormalizer(block, 0);
        }

        public override float GetMaximum(TBlock block)
        {
            return Denormalizer(block, 1);
        }
    }
}

#region Using
using Sandbox.Common;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
#endregion

//  Abstract class (screen) for all debug / developer screens

namespace Sandbox.Game.Gui
{
    public abstract class MyGuiScreenDebugBase : MyGuiScreenBase
    {
        static Vector4 m_defaultColor = Color.Yellow.ToVector4();
        static Vector4 m_defaultTextColor = new Vector4(1f, 1f, 0f, 1f);

        protected Vector2 m_currentPosition;
        protected float m_scale = 1.0f;
        protected float m_buttonXOffset = 0;
        protected float m_sliderDebugScale = 1f;

        float m_maxWidth = 0;

        protected float Spacing = 0;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugBase";
        }

        protected MyGuiScreenDebugBase(Vector4? backgroundColor = null, bool isTopMostScreen = false)
            : this(new Vector2(MyGuiManager.GetMaxMouseCoord().X - 0.16f, 0.5f), new Vector2(0.32f, 1.0f), backgroundColor ?? 0.85f * Color.Black.ToVector4(), isTopMostScreen)
        {
            m_closeOnEsc = true;
            m_drawEvenWithoutFocus = true;
            m_isTopMostScreen = false;
            CanHaveFocus = false;
            m_isTopScreen = true;
        }

        protected MyGuiScreenDebugBase(Vector2 position, Vector2? size, Vector4? backgroundColor, bool isTopMostScreen) :
            base(position, backgroundColor, size, isTopMostScreen, null)
        {
            CanBeHidden = false;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = false;
            m_canShareInput = true;
            m_isTopScreen = true;
        }

        #region MultilineText

        protected MyGuiControlMultilineText AddMultilineText(Vector2? size = null, Vector2? offset = null, float textScale = 1.0f, bool selectable = false)
        {
            Vector2 textboxSize = size ?? this.Size ?? new Vector2(0.5f, 0.5f);

            MyGuiControlMultilineText textbox = new MyGuiControlMultilineText(
                position: m_currentPosition + textboxSize / 2.0f + (offset ?? Vector2.Zero),
                size: textboxSize,
                backgroundColor: m_defaultColor,
                textScale: this.m_scale * textScale,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                selectable: selectable,
                font: MyFontEnum.Debug);

            //textbox.BackgroundTexture = MyGuiConstants.TEXTURE_NEWS_BACKGROUND;
            //textbox.TextSize = new Vector2(0.2f, 0.2f);

            Controls.Add(textbox);

            return textbox;
        }

        #endregion

        #region CheckBox

        private MyGuiControlCheckbox AddCheckBox(String text, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            MyGuiControlLabel label = new MyGuiControlLabel(
                position: m_currentPosition,
                text: text,
                colorMask: color ?? m_defaultTextColor,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiConstants.DEBUG_LABEL_TEXT_SCALE * m_scale,
                font: MyFontEnum.Debug);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            float labelWidth = label.GetTextSize().X + 0.02f;
            m_maxWidth = Math.Max(m_maxWidth, labelWidth);
            label.Enabled = enabled;
            Controls.Add(label);


            Vector2? screenSize = this.GetSize();

            MyGuiControlCheckbox checkBox = new MyGuiControlCheckbox(
                isChecked: false,
                color: (color ?? m_defaultColor),
                visualStyle: MyGuiControlCheckboxStyleEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            checkBox.Position = m_currentPosition + new Vector2(screenSize.Value.X - checkBox.Size.X, 0) + (checkBoxOffset ?? Vector2.Zero);
            checkBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            checkBox.Enabled = enabled;

            Controls.Add(checkBox);

            m_currentPosition.Y += Math.Max(checkBox.Size.Y, label.Size.Y) + Spacing;

            if (controlGroup != null)
            {
                controlGroup.Add(label);
                controlGroup.Add(checkBox);
            }

            return checkBox;
        }

        protected MyGuiControlCheckbox AddCheckBox(String text, MyDebugComponent component, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, true, controlGroup, color, checkBoxOffset);
            checkBox.IsChecked = component.Enabled;

            checkBox.IsCheckedChanged = delegate(MyGuiControlCheckbox sender)
            {
                component.Enabled = sender.IsChecked;
            };

            return checkBox;
        }

        protected MyGuiControlCheckbox AddCheckBox(MyStringId textEnum, bool checkedState, Action<MyGuiControlCheckbox> checkBoxChange, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            return AddCheckBox(MyTexts.GetString(textEnum), checkedState, checkBoxChange, enabled, controlGroup, color, checkBoxOffset);
        }

        protected MyGuiControlCheckbox AddCheckBox(String text, bool checkedState, Action<MyGuiControlCheckbox> checkBoxChange, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, enabled, controlGroup, color, checkBoxOffset);
            checkBox.IsChecked = checkedState;
            if (checkBoxChange != null)
            {
                checkBox.IsCheckedChanged =
                delegate(MyGuiControlCheckbox sender)
                {
                    checkBoxChange(sender);
                    ValueChanged(sender);
                };
            }
            return checkBox;
        }

        protected MyGuiControlCheckbox AddCheckBox(MyStringId textEnum, Func<bool> getter, Action<bool> setter, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            return AddCheckBox(MyTexts.GetString(textEnum), getter, setter, enabled, controlGroup, color, checkBoxOffset);
        }

        protected MyGuiControlCheckbox AddCheckBox(String text, Func<bool> getter, Action<bool> setter, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, enabled, controlGroup, color, checkBoxOffset);

            System.Diagnostics.Debug.Assert(getter != null && setter != null, "Setter or getter was null");

            if (getter != null)
            {
                checkBox.IsChecked = getter();
            }

            if (setter != null)
            {
                checkBox.IsCheckedChanged = delegate(MyGuiControlCheckbox sender)
                {
                    setter(sender.IsChecked);
                    ValueChanged(sender);
                };
            }

            return checkBox;
        }

        protected MyGuiControlCheckbox AddCheckBox(String text, object instance, MemberInfo memberInfo, bool enabled = true, List<MyGuiControlBase> controlGroup = null, Vector4? color = null, Vector2? checkBoxOffset = null)
        {
            MyGuiControlCheckbox checkBox = AddCheckBox(text, enabled, controlGroup, color, checkBoxOffset);

            if (memberInfo is PropertyInfo)
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                checkBox.IsChecked = (bool)property.GetValue(instance, new object[0]);
                checkBox.UserData = new Tuple<object, PropertyInfo>(instance, property);
                checkBox.IsCheckedChanged = delegate(MyGuiControlCheckbox sender)
                {
                    Tuple<object, PropertyInfo> tuple = sender.UserData as Tuple<object, PropertyInfo>;
                    tuple.Item2.SetValue(tuple.Item1, sender.IsChecked, new object[0]);
                    ValueChanged(sender);
                };
            }
            else
                if (memberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)memberInfo;
                    checkBox.IsChecked = (bool)field.GetValue(instance);
                    checkBox.UserData = new Tuple<object, FieldInfo>(instance, field);
                    checkBox.IsCheckedChanged = delegate(MyGuiControlCheckbox sender)
                    {
                        Tuple<object, FieldInfo> tuple = sender.UserData as Tuple<object, FieldInfo>;
                        tuple.Item2.SetValue(tuple.Item1, sender.IsChecked);
                        ValueChanged(sender);
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Unknown type of memberInfo");
                }

            return checkBox;
        }

        protected virtual void ValueChanged(MyGuiControlBase sender)
        {
        }

        #endregion

        #region Slider

        private MyGuiControlSliderBase AddSliderBase(String text, MyGuiSliderProperties props, Vector4? color = null)
        {
            MyGuiControlSliderBase slider = new MyGuiControlSliderBase(
                position: m_currentPosition,
                width: 460f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                props: props,
                labelScale: 0.75f * m_scale,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                labelFont: MyFontEnum.Debug);
            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = color ?? m_defaultColor;

            Controls.Add(slider);

            MyGuiControlLabel label = new MyGuiControlLabel(
                position: m_currentPosition + new Vector2(0.015f, 0f),
                text: text,
                colorMask: color ?? m_defaultTextColor,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f * m_scale,
                font: MyFontEnum.Debug);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            float labelWidth = label.GetTextSize().X + 0.02f;
            m_maxWidth = Math.Max(m_maxWidth, labelWidth);
            Controls.Add(label);

            m_currentPosition.Y += slider.Size.Y + Spacing;

            return slider;
        }

        private MyGuiControlSlider AddSlider(String text, float valueMin, float valueMax, Vector4? color = null)
        {
            MyGuiControlSlider slider = new MyGuiControlSlider(
                position: m_currentPosition,
                width: 460f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                minValue: valueMin,
                maxValue: valueMax,
                labelText: new StringBuilder(" {0}").ToString(),
                labelDecimalPlaces: 3,
                labelScale: 0.75f * m_scale,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                labelFont: MyFontEnum.Debug);
            slider.DebugScale = m_sliderDebugScale;
            slider.ColorMask = color ?? m_defaultColor;

            Controls.Add(slider);

            MyGuiControlLabel label = new MyGuiControlLabel(
                position: m_currentPosition + new Vector2(0.015f, 0f),
                text: text,
                colorMask: color ?? m_defaultTextColor,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f * m_scale,
                font: MyFontEnum.Debug);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            float labelWidth = label.GetTextSize().X + 0.02f;
            m_maxWidth = Math.Max(m_maxWidth, labelWidth);
            Controls.Add(label);

            m_currentPosition.Y += slider.Size.Y + Spacing;

            return slider;
        }

        protected MyGuiControlSlider AddSlider(String text, float value, float valueMin, float valueMax, Action<MyGuiControlSlider> valueChange, Vector4? color = null)
        {
            MyGuiControlSlider slider = AddSlider(text, valueMin, valueMax, color);
            slider.Value = value;
            slider.ValueChanged = valueChange;
            slider.ValueChanged += ValueChanged;
            return slider;
        }

        protected MyGuiControlSlider AddSlider(String text, float valueMin, float valueMax, Func<float> getter, Action<float> setter, Vector4? color = null)
        {
            MyGuiControlSlider slider = AddSlider(text, valueMin, valueMax, color);
            slider.Value = getter();
            slider.UserData = setter;
            slider.ValueChanged = delegate(MyGuiControlSlider sender)
            {
                var ac = (Action<float>)sender.UserData;
                ac(sender.Value);
                ValueChanged(sender);
            };
            return slider;
        }

        protected MyGuiControlSliderBase AddSlider(String text, MyGuiSliderProperties properties, Func<float> getter, Action<float> setter, Vector4? color = null)
        {
            MyGuiControlSliderBase slider = AddSliderBase(text, properties, color);
            slider.Value = getter();
            slider.UserData = setter;
            slider.ValueChanged = delegate(MyGuiControlSliderBase sender)
            {
                var ac = (Action<float>)sender.UserData;
                ac(sender.Value);
                ValueChanged(sender);
            };
            return slider;
        }

        protected MyGuiControlSlider AddSlider(String text, float valueMin, float valueMax, object instance, MemberInfo memberInfo, Vector4? color = null)
        {
            MyGuiControlSlider slider = AddSlider(text, valueMin, valueMax, color);

            if (memberInfo is PropertyInfo)
            {
                PropertyInfo property = (PropertyInfo)memberInfo;

                slider.Value = (float)property.GetValue(instance, new object[0]);
                slider.UserData = new Tuple<object, PropertyInfo>(instance, property);
                slider.ValueChanged = delegate(MyGuiControlSlider sender)
                {
                    Tuple<object, PropertyInfo> tuple = sender.UserData as Tuple<object, PropertyInfo>;
                    tuple.Item2.SetValue(tuple.Item1, sender.Value, new object[0]);
                    ValueChanged(sender);
                };
            }
            else
                if (memberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)memberInfo;

                    slider.Value = (float)field.GetValue(instance);
                    slider.UserData = new Tuple<object, FieldInfo>(instance, field);
                    slider.ValueChanged = delegate(MyGuiControlSlider sender)
                    {
                        Tuple<object, FieldInfo> tuple = sender.UserData as Tuple<object, FieldInfo>;
                        tuple.Item2.SetValue(tuple.Item1, sender.Value);
                        ValueChanged(sender);
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Unknown type of memberInfo");
                }

            return slider;
        }

        #endregion

        protected MyGuiControlTextbox AddTextbox(String value, Action<MyGuiControlTextbox> onTextChanged, Vector4? color = null, float scale = 1.0f, MyGuiControlTextboxType type = MyGuiControlTextboxType.Normal, List<MyGuiControlBase> controlGroup = null, string font = MyFontEnum.Debug, MyGuiDrawAlignEnum originAlign =MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
        {
            var textbox = new MyGuiControlTextbox(m_currentPosition, value, 6, color, scale, type);
            textbox.OriginAlign = originAlign;
            if (onTextChanged != null)
            {
                textbox.TextChanged += onTextChanged;
            }
            Controls.Add(textbox);

            m_currentPosition.Y += textbox.Size.Y + 0.01f + Spacing;

            if (controlGroup != null)
                controlGroup.Add(textbox);

            return textbox;
        }

        #region Label

        protected MyGuiControlLabel AddLabel(String text, Vector4 color, float scale, List<MyGuiControlBase> controlGroup = null, string font = MyFontEnum.Debug)
        {
            MyGuiControlLabel label = new MyGuiControlLabel(
                position: m_currentPosition,
                text: text,
                colorMask: color,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiConstants.DEBUG_LABEL_TEXT_SCALE * scale * m_scale,
                font: font);
            label.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            float labelWidth = label.GetTextSize().X + 0.02f;
            m_maxWidth = Math.Max(m_maxWidth, labelWidth);
            Controls.Add(label);

            m_currentPosition.Y += label.Size.Y + Spacing;

            if (controlGroup != null)
                controlGroup.Add(label);

            return label;
        }

        #endregion

        #region Subcaption

        protected MyGuiControlLabel AddSubcaption(MyStringId textEnum, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            return AddSubcaption(MyTexts.GetString(textEnum), captionTextColor: captionTextColor, captionOffset: captionOffset, captionScale: captionScale);
        }

        protected MyGuiControlLabel AddSubcaption(String text, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            float deltaX = m_size == null ? 0.0f : (m_size.Value.X / 2.0f);

            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y;
            m_currentPosition.X += deltaX;

            var caption = new MyGuiControlLabel(
                position: m_currentPosition + (captionOffset != null ? captionOffset.Value : Vector2.Zero),
                text: text,
                colorMask: captionTextColor ?? m_defaultColor,
                textScale: captionScale,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                font: MyFontEnum.Debug);
            Elements.Add(caption);

            m_currentPosition.Y += MyGuiConstants.SCREEN_CAPTION_DELTA_Y + Spacing;
            m_currentPosition.X -= deltaX;

            return caption;
        }

        #endregion

        #region Color


        private MyGuiControlColor AddColor(String text)
        {
            MyGuiControlColor colorControl = new MyGuiControlColor(
                text: text,
                textScale: m_scale,
                position: m_currentPosition,
                color: Color.White,
                defaultColor: Color.White,
                font: MyFontEnum.Debug,
                dialogAmountCaption: MyCommonTexts.DialogAmount_AddAmountCaption);
            colorControl.ColorMask = Color.Yellow.ToVector4();
            colorControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            Controls.Add(colorControl);
            m_currentPosition.Y += colorControl.Size.Y;
            return colorControl;
        }

        protected MyGuiControlColor AddColor(String text, Func<Color> getter, Action<Color> setter)
        {
            return AddColor(text, getter(), (c) => setter(c.GetColor()));
        }

        protected MyGuiControlColor AddColor(String text, Color value, Action<MyGuiControlColor> setter)
        {
            MyGuiControlColor colorControl = AddColor(text);

            colorControl.SetColor(value);
            colorControl.OnChange += delegate(MyGuiControlColor sender)
            {
                setter(colorControl);
            };

            return colorControl;
        }

        protected MyGuiControlColor AddColor(String text, object instance, MemberInfo memberInfo)
        {
            MyGuiControlColor colorControl = AddColor(text);

            if (memberInfo is PropertyInfo)
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                var val = property.GetValue(instance, new object[0]);
                if (val is Color)
                    colorControl.SetColor((Color)val);
                else if (val is Vector3)
                    colorControl.SetColor((Vector3)val);
                else if (val is Vector4)
                    colorControl.SetColor((Vector4)val);

                colorControl.UserData = new Tuple<object, PropertyInfo>(instance, property);
                colorControl.OnChange += delegate(MyGuiControlColor sender)
                {
                    Tuple<object, PropertyInfo> tuple = sender.UserData as Tuple<object, PropertyInfo>;
                    if (tuple.Item2.MemberType.GetType() == typeof(Color))
                    {
                        tuple.Item2.SetValue(tuple.Item1, sender.GetColor(), new object[0]);
                        ValueChanged(sender);
                    }
                    else
                        if (tuple.Item2.MemberType.GetType() == typeof(Vector3))
                        {
                            tuple.Item2.SetValue(tuple.Item1, sender.GetColor().ToVector3(), new object[0]);
                            ValueChanged(sender);
                        }
                        else
                            if (tuple.Item2.MemberType.GetType() == typeof(Vector4))
                            {
                                tuple.Item2.SetValue(tuple.Item1, sender.GetColor().ToVector4(), new object[0]);
                                ValueChanged(sender);
                            }
                };
            }
            else
                if (memberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)memberInfo;
                    var val = field.GetValue(instance);
                    if (val is Color)
                        colorControl.SetColor((Color)val);
                    else if (val is Vector3)
                        colorControl.SetColor((Vector3)val);
                    else if (val is Vector4)
                        colorControl.SetColor((Vector4)val);

                    colorControl.UserData = new Tuple<object, FieldInfo>(instance, field);
                    colorControl.OnChange += delegate(MyGuiControlColor sender)
                    {
                        Tuple<object, FieldInfo> tuple = sender.UserData as Tuple<object, FieldInfo>;
                        if (tuple.Item2.FieldType == typeof(Color))
                        {
                            tuple.Item2.SetValue(tuple.Item1, sender.GetColor());
                            ValueChanged(sender);
                        }
                        else if (tuple.Item2.FieldType == typeof(Vector3))
                        {
                            tuple.Item2.SetValue(tuple.Item1, sender.GetColor().ToVector3());
                            ValueChanged(sender);
                        }
                        else if (tuple.Item2.FieldType == typeof(Vector4))
                        {
                            tuple.Item2.SetValue(tuple.Item1, sender.GetColor().ToVector4());
                            ValueChanged(sender);
                        }
                    };
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Unknown type of memberInfo");
                }

            return colorControl;
        }

        #endregion

        #region Button

        protected MyGuiControlButton AddButton(string text, Action<MyGuiControlButton> onClick, List<MyGuiControlBase> controlGroup = null, Vector4? textColor = null, Vector2? size = null)
        {
            return AddButton(new StringBuilder(text), onClick, controlGroup, textColor, size);
        }

        protected MyGuiControlButton AddButton(StringBuilder text, Action<MyGuiControlButton> onClick, List<MyGuiControlBase> controlGroup = null, Vector4? textColor = null, Vector2? size = null)
        {
            MyGuiControlButton button = new MyGuiControlButton(
                position: new Vector2(m_buttonXOffset, m_currentPosition.Y),
                colorMask: m_defaultColor,
                text: text,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiConstants.DEBUG_BUTTON_TEXT_SCALE * m_scale,
                onButtonClick: onClick,
                visualStyle: MyGuiControlButtonStyleEnum.Debug);
            button.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;

            Controls.Add(button);

            m_currentPosition.Y += button.Size.Y + 0.01f + Spacing;

            if (controlGroup != null)
                controlGroup.Add(button);

            return button;
        }

        #endregion

        #region Combobox

        protected MyGuiControlCombobox AddCombo(
            List<MyGuiControlBase> controlGroup = null,
            Vector4? textColor = null,
            Vector2? size = null,
            int openAreaItemsCount = 10)
        {
            MyGuiControlCombobox combo = new MyGuiControlCombobox(m_currentPosition, size: size, textColor: textColor, openAreaItemsCount: openAreaItemsCount)
            {
                VisualStyle = MyGuiControlComboboxStyleEnum.Debug,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            };
            Controls.Add(combo);

            m_currentPosition.Y += combo.Size.Y + 0.01f + Spacing;

            if (controlGroup != null)
                controlGroup.Add(combo);

            return combo;
        }

        protected MyGuiControlCombobox AddCombo<TEnum>(
            TEnum selectedItem,
            Action<TEnum> valueChanged,
            bool enabled = true,
            int openAreaItemsCount = 10,
            List<MyGuiControlBase> controlGroup = null, Vector4? color = null)
            where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            Debug.Assert(typeof(TEnum).IsEnum);

            var combobox = AddCombo(controlGroup, color, openAreaItemsCount: openAreaItemsCount);
            foreach (var value in MyEnum<TEnum>.Values)
            {
                combobox.AddItem((int)(object)value, new StringBuilder(value.ToString()));
            }
            combobox.SelectItemByKey((int)(object)selectedItem);
            combobox.ItemSelected += delegate()
            {
                valueChanged(MyEnum<TEnum>.SetValue((ulong)combobox.GetSelectedKey()));
            };

            return combobox;
        }

        protected MyGuiControlCombobox AddCombo<TEnum>(
            object instance,
            MemberInfo memberInfo,
            bool enabled = true,
            int openAreaItemsCount = 10,
            List<MyGuiControlBase> controlGroup = null, Vector4? color = null)
            where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            Debug.Assert(typeof(TEnum).IsEnum);

            var combobox = AddCombo(controlGroup, color, openAreaItemsCount: openAreaItemsCount);
            foreach (var value in MyEnum<TEnum>.Values)
            {
                combobox.AddItem((int)(object)value, new StringBuilder(value.ToString()));
            }

            if (memberInfo is PropertyInfo)
            {
                var property = memberInfo as PropertyInfo;
                combobox.SelectItemByKey((int)property.GetValue(instance, new object[0]));
                combobox.ItemSelected += delegate()
                {
                    property.SetValue(instance, Enum.ToObject(typeof(TEnum), combobox.GetSelectedKey()), new object[0]);
                };
            }
            else if (memberInfo is FieldInfo)
            {
                var field = memberInfo as FieldInfo;
                combobox.SelectItemByKey((int)field.GetValue(instance));
                combobox.ItemSelected += delegate()
                {
                    field.SetValue(instance, Enum.ToObject(typeof(TEnum), combobox.GetSelectedKey()));
                };
            }
            else
            {
                Debug.Fail("Unknown type of member info.");
            }

            return combobox;
        }

        #endregion

        protected void AddShareFocusHint()
        {
            MyGuiControlLabel label = new MyGuiControlLabel(
                position: new Vector2(0.01f, -m_size.Value.Y / 2.0f + 0.07f),
                text: "(press ALT to share focus)",
                colorMask: Color.Yellow.ToVector4(),
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.7f,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                font: MyFontEnum.Debug);
            Controls.Add(label);
        }

        public override bool Draw()
        {
            if (MyGuiSandbox.IsDebugScreenEnabled() == false) return false;
            if (!base.Draw()) return false;
            return true;
        }
    }
}

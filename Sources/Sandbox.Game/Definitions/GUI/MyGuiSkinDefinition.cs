﻿using System;
﻿using ObjectBuilders.Definitions.GUI;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;

namespace Sandbox.Definitions.GUI
{
    [MyDefinitionType(typeof(MyObjectBuilder_GuiSkinDefinition))]
    public class MyGuiSkinDefinition : MyDefinitionBase
    {
        public class IconStyleDefinition
        {
            public string Normal;
            public string Highlight;
            public string Active;
            public string ActiveHighlight;
            public string Disabled;
        }

        public class MyContextMenuStyleDefinition
        {
            public MyGuiCompositeTexture TitleTexture;
            public MyStringId ImageStyle;
            public MyStringId SeparatorStyle;
            public float SeparatorHeight;
            public Vector2 Margin;
        }

        // textures
        public Dictionary<MyStringId, MyGuiCompositeTexture> Textures;

        // icons 
        public Dictionary<MyStringId, IconStyleDefinition> IconStyles;

        // button visual styles
        public Dictionary<MyStringId, MyGuiControlButton.StyleDefinition> ButtonStyles;
        public Dictionary<MyStringId, MyGuiControlImageButton.StyleDefinition> ImageButtonStyles;

        // combobox visual styles
        public Dictionary<MyStringId, MyGuiControlCombobox.StyleDefinition> ComboboxStyles;

        // label visual styles
        public Dictionary<MyStringId, MyGuiControlLabel.StyleDefinition> LabelStyles;

        // checkbox visual styles
        public Dictionary<MyStringId, MyGuiControlCheckbox.StyleDefinition> CheckboxStyles;

        // slider visual styles
        public Dictionary<MyStringId, MyGuiControlSliderBase.StyleDefinition> SliderStyles;

        // listbox visual styles
        public Dictionary<MyStringId, MyGuiControlListbox.StyleDefinition> ListboxStyles;

        // textbox visual styles
        public Dictionary<MyStringId, MyGuiControlTextbox.StyleDefinition> TextboxStyles;

        // image visual styles
        public Dictionary<MyStringId, MyGuiControlImage.StyleDefinition> ImageStyles;

        // context menu visual styles
        public Dictionary<MyStringId, MyContextMenuStyleDefinition> ContextMenuStyles;

        //Button list visual styles
        public Dictionary<MyStringId, MyButtonListStyleDefinition> ButtonListStyles;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            Textures = new Dictionary<MyStringId, MyGuiCompositeTexture>(MyStringId.Comparer);
            IconStyles = new Dictionary<MyStringId, IconStyleDefinition>(MyStringId.Comparer);
            ButtonStyles = new Dictionary<MyStringId, MyGuiControlButton.StyleDefinition>(MyStringId.Comparer);
            ComboboxStyles = new Dictionary<MyStringId, MyGuiControlCombobox.StyleDefinition>(MyStringId.Comparer);
            LabelStyles = new Dictionary<MyStringId, MyGuiControlLabel.StyleDefinition>(MyStringId.Comparer);
            CheckboxStyles = new Dictionary<MyStringId, MyGuiControlCheckbox.StyleDefinition>(MyStringId.Comparer);
            SliderStyles = new Dictionary<MyStringId, MyGuiControlSliderBase.StyleDefinition>(MyStringId.Comparer);
            ImageButtonStyles = new Dictionary<MyStringId, MyGuiControlImageButton.StyleDefinition>(MyStringId.Comparer);
            ListboxStyles = new Dictionary<MyStringId, MyGuiControlListbox.StyleDefinition>(MyStringId.Comparer);
            TextboxStyles = new Dictionary<MyStringId, MyGuiControlTextbox.StyleDefinition>(MyStringId.Comparer);
            ImageStyles = new Dictionary<MyStringId, MyGuiControlImage.StyleDefinition>(MyStringId.Comparer);
            ContextMenuStyles = new Dictionary<MyStringId, MyContextMenuStyleDefinition>(MyStringId.Comparer);
            ButtonListStyles = new Dictionary<MyStringId, MyButtonListStyleDefinition>(MyStringId.Comparer);

            var ob = builder as MyObjectBuilder_GuiSkinDefinition;
            if (ob == null)
                return;

            if (ob.GuiTextures != null)
            {
                foreach (var textureStyleDef in ob.GuiTextures)
                {
                    Textures[MyStringId.GetOrCompute(textureStyleDef.StyleName)] = textureStyleDef.Texture;
                }
            }

            if (ob.GuiIcons != null)
            {
                foreach (var iconStyleDef in ob.GuiIcons)
                {
                    var iconStyle = new IconStyleDefinition()
                    {
                        Normal = iconStyleDef.Normal,
                        Highlight = iconStyleDef.Highlight,
                        Active = iconStyleDef.Active,
                        ActiveHighlight = iconStyleDef.ActiveHighlight,
                        Disabled = iconStyleDef.Disabled
                    };
                    IconStyles[MyStringId.GetOrCompute(iconStyleDef.StyleName)] = iconStyle;
                }
            }

            if (ob.Buttons != null)
            {
                foreach (var buttonStyleDef in ob.Buttons)
                {
                    var buttonStyle = new MyGuiControlButton.StyleDefinition()
                    {
                        BackgroundColor = buttonStyleDef.BackgroundColor,
                        NormalTexture = buttonStyleDef.Normal.Texture,
                        HighlightTexture = buttonStyleDef.Highlight.Texture,
                        NormalFont = buttonStyleDef.Normal.Font,
                        HighlightFont = buttonStyleDef.Highlight.Font,
                        Padding = new MyGuiBorderThickness(
                            buttonStyleDef.Padding.Left / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            buttonStyleDef.Padding.Right / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            buttonStyleDef.Padding.Top / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                            buttonStyleDef.Padding.Bottom / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                    };

                    var imageButtonStyle = new MyGuiControlImageButton.StyleDefinition()
                    {
                        BackgroundColor = buttonStyleDef.BackgroundColor,
                        Padding = new MyGuiBorderThickness(
                            buttonStyleDef.Padding.Left / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            buttonStyleDef.Padding.Right / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            buttonStyleDef.Padding.Top / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                            buttonStyleDef.Padding.Bottom / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                    };

                    MyDebug.AssertDebug(buttonStyleDef.Normal != null, String.Format("Normal state for {0} not defined!", buttonStyleDef.StyleName));
                    imageButtonStyle.Normal = new MyGuiControlImageButton.StateDefinition()
                    {
                        Font = buttonStyleDef.Normal.Font,
                        Texture = buttonStyleDef.Normal.Texture,
                        CornerTextFont = buttonStyleDef.Normal.CornerTextFont,
                        CornerTextSize = buttonStyleDef.Normal.CornerTextSize
                    };

                    if (buttonStyleDef.Disabled != null)
                        imageButtonStyle.Disabled = new MyGuiControlImageButton.StateDefinition()
                        {
                            Font = buttonStyleDef.Disabled.Font,
                            Texture = buttonStyleDef.Disabled.Texture,
                            CornerTextFont = buttonStyleDef.Disabled.CornerTextFont,
                            CornerTextSize = buttonStyleDef.Disabled.CornerTextSize
                        };
                    else
                        imageButtonStyle.Disabled = imageButtonStyle.Normal;

                    if (buttonStyleDef.Active != null)
                        imageButtonStyle.Active = new MyGuiControlImageButton.StateDefinition()
                        {
                            Font = buttonStyleDef.Active.Font,
                            Texture = buttonStyleDef.Active.Texture,
                            CornerTextFont = buttonStyleDef.Active.CornerTextFont,
                            CornerTextSize = buttonStyleDef.Active.CornerTextSize
                        };
                    else
                        imageButtonStyle.Active = imageButtonStyle.Normal;

                    if (buttonStyleDef.Highlight != null)
                        imageButtonStyle.Highlight = new MyGuiControlImageButton.StateDefinition()
                        {
                            Font = buttonStyleDef.Highlight.Font,
                            Texture = buttonStyleDef.Highlight.Texture,
                            CornerTextFont = buttonStyleDef.Highlight.CornerTextFont,
                            CornerTextSize = buttonStyleDef.Highlight.CornerTextSize
                        };
                    else
                        imageButtonStyle.Highlight = imageButtonStyle.Normal;

                    if (buttonStyleDef.ActiveHighlight != null)
                        imageButtonStyle.ActiveHighlight = new MyGuiControlImageButton.StateDefinition()
                        {
                            Font = buttonStyleDef.ActiveHighlight.Font,
                            Texture = buttonStyleDef.ActiveHighlight.Texture,
                            CornerTextFont = buttonStyleDef.ActiveHighlight.CornerTextFont,
                            CornerTextSize = buttonStyleDef.ActiveHighlight.CornerTextSize
                        };
                    else
                        imageButtonStyle.ActiveHighlight = imageButtonStyle.Highlight;

                    ButtonStyles[MyStringId.GetOrCompute(buttonStyleDef.StyleName)] = buttonStyle;
                    ImageButtonStyles[MyStringId.GetOrCompute(buttonStyleDef.StyleName)] = imageButtonStyle;
                }
            }

            if (ob.Labels != null)
            {
                foreach (var labelStyleDef in ob.Labels)
                {
                    var labelStyle = new MyGuiControlLabel.StyleDefinition()
                    {
                        Font = labelStyleDef.Font,
                        ColorMask = labelStyleDef.Color,
                        TextScale = labelStyleDef.TextScale
                    };

                    LabelStyles[MyStringId.GetOrCompute(labelStyleDef.StyleName)] = labelStyle;
                }
            }

            if (ob.Checkboxes != null)
            {
                foreach (var checkboxStyleDef in ob.Checkboxes)
                {
                    var checkbodexStyle = new MyGuiControlCheckbox.StyleDefinition()
                    {
                        NormalCheckedTexture = checkboxStyleDef.NormalChecked.Texture,
                        NormalUncheckedTexture = checkboxStyleDef.NormalUnchecked.Texture,
                        HighlightCheckedTexture = checkboxStyleDef.HighlightChecked.Texture,
                        HighlightUncheckedTexture = checkboxStyleDef.HighlightUnchecked.Texture,
                        CheckedIcon = new MyGuiHighlightTexture()
                        {
                            Highlight = checkboxStyleDef.HighlightChecked.Icon,
                            Normal = checkboxStyleDef.NormalChecked.Icon,
                            SizePx = checkboxStyleDef.IconSize,
                        },
                        UncheckedIcon = new MyGuiHighlightTexture()
                        {
                            Highlight = checkboxStyleDef.HighlightUnchecked.Icon,
                            Normal = checkboxStyleDef.NormalUnchecked.Icon,
                            SizePx = checkboxStyleDef.IconSize,
                        },
                    };

                    CheckboxStyles[MyStringId.GetOrCompute(checkboxStyleDef.StyleName)] = checkbodexStyle;
                }
            }

            if (ob.Sliders != null)
            {
                foreach (var sliderStyleDef in ob.Sliders)
                {
                    MyDebug.AssertDebug(sliderStyleDef.Normal != null, String.Format("Normal state for {0} not defined!", sliderStyleDef.StyleName));

                    var sliderStyle = new MyGuiControlSliderBase.StyleDefinition()
                    {
                        RailTexture = sliderStyleDef.Normal.TrackTexture,
                        RailHighlightTexture = sliderStyleDef.Highlight != null ? sliderStyleDef.Highlight.TrackTexture : sliderStyleDef.Normal.TrackTexture,
                        ThumbTexture = new MyGuiHighlightTexture()
                        {
                            Highlight = sliderStyleDef.Highlight != null ? sliderStyleDef.Highlight.Thumb : sliderStyleDef.Normal.Thumb,
                            Normal = sliderStyleDef.Normal.Thumb,
                            SizePx = sliderStyleDef.ThumbSize,
                        },
                    };

                    SliderStyles[MyStringId.GetOrCompute(sliderStyleDef.StyleName)] = sliderStyle;
                }
            }

            if (ob.Comboboxes != null)
            {
                foreach (var comboboxStyleDef in ob.Comboboxes)
                {
                    MyDebug.AssertDebug(comboboxStyleDef.Normal != null, String.Format("Normal state for {0} not defined!", comboboxStyleDef.StyleName));

                    var comboboxStyle = new MyGuiControlCombobox.StyleDefinition()
                    {
                        TextScale = comboboxStyleDef.TextScale,
                        ComboboxTextureNormal = comboboxStyleDef.Normal.Texture,
                        ComboboxTextureHighlight = comboboxStyleDef.Highlight != null ? comboboxStyleDef.Highlight.Texture : comboboxStyleDef.Normal.Texture,
                        ItemFontNormal = comboboxStyleDef.Normal.ItemFont,
                        ItemFontHighlight = comboboxStyleDef.Highlight != null ? comboboxStyleDef.Highlight.ItemFont : comboboxStyleDef.Normal.ItemFont,
                        ItemTextureHighlight = comboboxStyleDef.ItemTextureHighlight,
                        DropDownHighlightExtraWidth = 0.007f,
                        SelectedItemOffset = new Vector2(0.01f, 0.005f),
                        ScrollbarMargin = new MyGuiBorderThickness()
                        {
                            Left = comboboxStyleDef.ScrollbarMargin.Left / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            Right = comboboxStyleDef.ScrollbarMargin.Right / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            Bottom = comboboxStyleDef.ScrollbarMargin.Bottom / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                            Top = comboboxStyleDef.ScrollbarMargin.Top / MyGuiConstants.GUI_OPTIMAL_SIZE.Y
                        },
                        DropDownTexture = comboboxStyleDef.DropDownTexture
                    };

                    ComboboxStyles[MyStringId.GetOrCompute(comboboxStyleDef.StyleName)] = comboboxStyle;
                }
            }

            if (ob.Listboxes != null)
            {
                foreach (var listboxStyleDef in ob.Listboxes)
                {
                    var listboxStyle = new MyGuiControlListbox.StyleDefinition();

                    listboxStyle.TextScale = listboxStyleDef.TextScale;
                    listboxStyle.ItemFontHighlight = listboxStyleDef.ItemFontHighlight;
                    listboxStyle.ItemFontNormal = listboxStyleDef.ItemFontNormal;
                    listboxStyle.ItemSize = listboxStyleDef.ItemSize;
                    listboxStyle.ItemsOffset = listboxStyleDef.ItemOffset;
                    listboxStyle.ItemTextureHighlight = listboxStyleDef.ItemTextureHighlight;
                    listboxStyle.Texture = listboxStyleDef.Texture;
                    listboxStyle.XSizeVariable = listboxStyleDef.XSizeVariable;
                    listboxStyle.DrawScroll = listboxStyleDef.DrawScrollbar;

                    ListboxStyles[MyStringId.GetOrCompute(listboxStyleDef.StyleName)] = listboxStyle;
                }
            }

            if (ob.Textboxes != null)
            {
                foreach (var textboxStyleDef in ob.Textboxes)
                {
                    var textboxStyle = new MyGuiControlTextbox.StyleDefinition();

                    MyDebug.AssertDebug(textboxStyleDef.Normal != null, String.Format("Normal state for {0} not defined!", textboxStyleDef.StyleName));

                    textboxStyle.NormalFont = textboxStyleDef.Normal.Font;
                    textboxStyle.NormalTexture = textboxStyleDef.Normal.Texture;
                    if (textboxStyleDef.Highlight != null)
                    {
                        textboxStyle.HighlightFont = textboxStyleDef.Highlight.Font;
                        textboxStyle.HighlightTexture = textboxStyleDef.Highlight.Texture;
                    }
                    else
                    {
                        textboxStyle.HighlightFont = textboxStyleDef.Normal.Font;
                        textboxStyle.HighlightTexture = textboxStyleDef.Normal.Texture;
                    }

                    TextboxStyles[MyStringId.GetOrCompute(textboxStyleDef.StyleName)] = textboxStyle;
                }
            }

            if (ob.Images != null)
            {
                foreach (var imageStyleDef in ob.Images)
                {
                    var imageStyle = new MyGuiControlImage.StyleDefinition()
                    {
                        BackgroundTexture = imageStyleDef.Texture,
                        Padding = new MyGuiBorderThickness(
                            imageStyleDef.Padding.Left / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            imageStyleDef.Padding.Right / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                            imageStyleDef.Padding.Top / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                            imageStyleDef.Padding.Bottom / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                    };

                    ImageStyles[MyStringId.GetOrCompute(imageStyleDef.StyleName)] = imageStyle;
                }
            }

            if (ob.ContextMenus != null)
            {
                foreach (var contextMenuStyleDef in ob.ContextMenus)
                {
                    var contextMenuStyle = new MyContextMenuStyleDefinition()
                    {
                        TitleTexture = contextMenuStyleDef.TitleTexture,
                        ImageStyle = MyStringId.GetOrCompute(contextMenuStyleDef.ImageStyle),
                        SeparatorStyle = MyStringId.GetOrCompute(contextMenuStyleDef.SeparatorStyle),
                        SeparatorHeight = contextMenuStyleDef.SeparatorHeight,
                        Margin = contextMenuStyleDef.Margin
                    };

                    ContextMenuStyles[MyStringId.GetOrCompute(contextMenuStyleDef.StyleName)] = contextMenuStyle;
                }
            }

            if (ob.ButtonListStyles != null)
            {
                foreach (var btnListStyleDef in ob.ButtonListStyles)
                {
                    var newBtnListStyle = new MyButtonListStyleDefinition();
                    newBtnListStyle.ButtonMargin = btnListStyleDef.ButtonMargin;
                    newBtnListStyle.ButtonSize = btnListStyleDef.ButtonSize;

                    ButtonListStyles[MyStringId.GetOrCompute(btnListStyleDef.StyleName)] = newBtnListStyle;
                }
            }
        }
    }
}

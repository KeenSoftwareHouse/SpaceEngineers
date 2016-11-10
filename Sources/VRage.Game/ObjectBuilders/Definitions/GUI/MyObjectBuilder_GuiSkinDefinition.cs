﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage;
using VRage.Data;
using VRage.Game;
﻿using VRage.Game.ObjectBuilders.Definitions.GUI;
﻿using VRage.ObjectBuilders;
using VRageMath;

namespace ObjectBuilders.Definitions.GUI
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiSkinDefinition : MyObjectBuilder_DefinitionBase
    {
        public struct ColorDefinition
        {
            [XmlAttribute]
            public byte R;

            [XmlAttribute]
            public byte G;

            [XmlAttribute]
            public byte B;

            [XmlAttribute]
            public byte A;

            public static implicit operator Color(ColorDefinition definition)
            {
                return new Color(definition.R, definition.G, definition.B, definition.A);
            }

            public static implicit operator ColorDefinition(Color color)
            {
                return new ColorDefinition()
                {
                    A = color.A,
                    B = color.B,
                    G = color.G,
                    R = color.R,
                };
            }

            public static implicit operator Vector4(ColorDefinition definition)
            {
                return new Vector4(definition.R / 255f, definition.G / 255f, definition.B / 255f, definition.A / 255f);
            }

            public static implicit operator ColorDefinition(Vector4 vector)
            {
                return new ColorDefinition()
                {
                    A = (byte)(vector.W * 255),
                    B = (byte)(vector.Z * 255),
                    G = (byte)(vector.Y * 255),
                    R = (byte)(vector.X * 255),
                };
            }
        }

        public struct PaddingDefinition
        {
            [XmlAttribute("Left")]
            public float Left;

            [XmlAttribute("Right")]
            public float Right;

            [XmlAttribute("Top")]
            public float Top;

            [XmlAttribute("Bottom")]
            public float Bottom;
        }
        
        public class StyleDefinitionBase
        {
            [XmlAttribute]
            public string StyleName;
        }

        public class TextureStyleDefinition : StyleDefinitionBase
        {
            public SerializableCompositeTexture Texture;
        }

        public class IconStyleDefinition: StyleDefinitionBase
        {
            [ModdableContentFile("dds")]
            public string Normal;
            [ModdableContentFile("dds")]
            public string Highlight;
            [ModdableContentFile("dds")]
            public string Active;
            [ModdableContentFile("dds")]
            public string ActiveHighlight;
            [ModdableContentFile("dds")]
            public string Disabled;
        }

        public class ButtonStateDefinition
        {
            public SerializableCompositeTexture Texture;

            public string Font;

            public string CornerTextFont = MyFontEnum.White;

            public float CornerTextSize = 0.8f;
        }

        public class ButtonStyleDefinition: StyleDefinitionBase
        {
            public ButtonStateDefinition Normal;
            public ButtonStateDefinition Active;
            public ButtonStateDefinition Highlight;
            public ButtonStateDefinition ActiveHighlight;
            public ButtonStateDefinition Disabled;

            public PaddingDefinition Padding;

            public ColorDefinition BackgroundColor;
        }

        public class ListboxStyleDefinition: StyleDefinitionBase
        {
            public float TextScale;

            public string ItemFontHighlight;
            public string ItemFontNormal;

            public SerializableVector2 ItemSize;
            public SerializableVector2 ItemOffset;

            [ModdableContentFile("dds")]
            public string ItemTextureHighlight;

            public SerializableCompositeTexture Texture;

            public bool XSizeVariable;
            public bool DrawScrollbar;
        }

        public class LabelStyleDefinition : StyleDefinitionBase
        {
            public string Font;

            public ColorDefinition Color;
            
            public float TextScale;
        }

        public class CheckboxStateDefinition
        {
            public SerializableCompositeTexture Texture;

            [ModdableContentFile("dds")]
            public string Icon;
        }

        public class CheckboxStyleDefinition : StyleDefinitionBase
        {
            public CheckboxStateDefinition NormalChecked;
            public CheckboxStateDefinition NormalUnchecked;
            public CheckboxStateDefinition HighlightChecked;
            public CheckboxStateDefinition HighlightUnchecked;

            public SerializableVector2 IconSize;
        }

        public class ComboboxStateDefinition
        {
            public string ItemFont;
            public SerializableCompositeTexture Texture;
        }

        public class ComboboxStyleDefinition : StyleDefinitionBase
        {
            public ComboboxStateDefinition Normal;
            public ComboboxStateDefinition Highlight;

            public float TextScale;

            [ModdableContentFile("dds")]
            public string ItemTextureHighlight;

            public SerializableCompositeTexture DropDownTexture;

            public PaddingDefinition ScrollbarMargin;
        }

        public class SliderStateDefinition
        {
            public SerializableCompositeTexture TrackTexture;

            [ModdableContentFile("dds")]
            public string Thumb;
        }

        public class SliderStyleDefinition : StyleDefinitionBase
        {
            public SliderStateDefinition Normal;
            public SliderStateDefinition Highlight;

            public SerializableVector2 ThumbSize;
        }

        public class TextboxStateDefinition
        {
            public SerializableCompositeTexture Texture;
            public string Font;
        }

        public class TextboxStyleDefinition : StyleDefinitionBase
        {
            public TextboxStateDefinition Normal;
            public TextboxStateDefinition Highlight;
        }

        public class ImageStyleDefinition : StyleDefinitionBase
        {
            public SerializableCompositeTexture Texture;

            public PaddingDefinition Padding;
        }

        public class ContextMenuStyleDefinition : StyleDefinitionBase
        {
            public string ImageStyle;

            public string SeparatorStyle;

            public SerializableCompositeTexture TitleTexture;

            public float SeparatorHeight;

            public SerializableVector2 Margin;
        }

        [XmlElement("GuiTexture")] 
        public TextureStyleDefinition[] GuiTextures;

        [XmlElement("GuiIcon")]
        public IconStyleDefinition[] GuiIcons;

        [XmlElement("Button")]
        public ButtonStyleDefinition[] Buttons;

        [XmlElement("Label")] 
        public LabelStyleDefinition[] Labels;

        [XmlElement("Checkbox")] 
        public CheckboxStyleDefinition[] Checkboxes;

        [XmlElement("Combobox")] 
        public ComboboxStyleDefinition[] Comboboxes;

        [XmlElement("Slider")] 
        public SliderStyleDefinition[] Sliders;

        [XmlElement("Listbox")]
        public ListboxStyleDefinition[] Listboxes;

        [XmlElement("Textbox")]
        public TextboxStyleDefinition[] Textboxes;

        [XmlElement("Image")]
        public ImageStyleDefinition[] Images;

        [XmlElement("ContextMenu")]
        public ContextMenuStyleDefinition[] ContextMenus;

        [XmlElement("ButtonList")]
        public MyObjectBuilder_ButtonListStyleDefinition[] ButtonListStyles;
    }
}

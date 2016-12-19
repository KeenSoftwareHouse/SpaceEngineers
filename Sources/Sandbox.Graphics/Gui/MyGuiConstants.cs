using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Graphics.GUI
{

    public static class MyGuiConstants
    {
        public static readonly Vector2 GUI_OPTIMAL_SIZE = new Vector2(1600f, 1200f);
        public const float DOUBLE_CLICK_DELAY = 500;
        public const float CLICK_RELEASE_DELAY = 500;
        public const float DEFAULT_TEXT_SCALE = 1f;
        public const float HUD_TEXT_SCALE = 0.8f;
        public const float HUD_LINE_SPACING = 0.025f;
        public static readonly Vector4 LABEL_TEXT_COLOR = Vector4.One;
        public static readonly Vector2 DEFAULT_LISTBOX_ITEM_SIZE = (new Vector2(648f, 390f) - new Vector2(228f, 348f)) / GUI_OPTIMAL_SIZE;
        public static Vector4 DISABLED_CONTROL_COLOR_MASK_MULTIPLIER = new Vector4(0.7f, 0.7f, 0.7f, 1f);

        /// <summary>
        /// Recommended color of lines for GUI.
        /// </summary>
        public static Color THEMED_GUI_LINE_COLOR = new Color(101, 155, 183);
        public static Color THEMED_GUI_BACKGROUND_COLOR = new Color(47, 87, 114);
        public static readonly Color GUI_NEWS_BACKGROUND_COLOR = new Color(35, 66, 85);

        public static readonly MyGuiSizedTexture TEXTURE_ICON_FAKE = new MyGuiSizedTexture()
        {
            Texture = @"Textures\GUI\Icons\Fake.dds",
            SizePx = new Vector2(81f, 81f),
        };

        public static readonly string TEXTURE_ICON_FILTER_URANIUM = @"Textures\GUI\Icons\filter_uranium.dds";
        public static readonly string TEXTURE_ICON_FILTER_ORE = @"Textures\GUI\Icons\filter_ore.dds";
        public static readonly string TEXTURE_ICON_FILTER_INGOT = @"Textures\GUI\Icons\filter_ingot.dds";
        public static readonly string TEXTURE_ICON_FILTER_MISSILE = @"Textures\GUI\Icons\FilterMissile.dds";
        public static readonly string TEXTURE_ICON_FILTER_AMMO_25MM = @"Textures\GUI\Icons\FilterAmmo25mm.dds";
        public static readonly string TEXTURE_ICON_FILTER_AMMO_5_54MM = @"Textures\GUI\Icons\FilterAmmo5.54mm.dds";
        public static readonly string TEXTURE_ICON_FILTER_COMPONENT = @"Textures\GUI\Icons\FilterComponent.dds";
        public static readonly string TEXTURE_ICON_LARGE_BLOCK = @"Textures\GUI\CubeBuilder\GridModeLargeHighl.PNG";
        public static readonly string TEXTURE_ICON_SMALL_BLOCK = @"Textures\GUI\CubeBuilder\GridModeSmallHighl.PNG";

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_DEFAULT_NORMAL    = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(285f, 65f), Texture = @"Textures\GUI\Controls\button_default.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_DEFAULT_HIGHLIGHT = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(285f, 65f), Texture = @"Textures\GUI\Controls\button_default_highlight.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_RED_NORMAL        = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(281f, 62f), Texture = @"Textures\GUI\Controls\button_red.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_RED_HIGHLIGHT     = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(281f, 62f), Texture = @"Textures\GUI\Controls\button_red_highlight.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_CLOSE_NORMAL      = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(34f, 31f), Texture = @"Textures\GUI\Controls\button_close_symbol.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_CLOSE_HIGHLIGHT   = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(34f, 31f), Texture = @"Textures\GUI\Controls\button_close_symbol_highlight.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_INFO_NORMAL       = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(34f, 31f), Texture = @"Textures\GUI\Controls\button_info_symbol.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_INFO_HIGHLIGHT    = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(34f, 31f), Texture = @"Textures\GUI\Controls\button_info_symbol_highlight.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_KEEN_LOGO                = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(198f, 90f), Texture = @"Textures\Gui\KeenLogo.dds", } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_CHARACTER = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(91f, 69f), Texture = @"Textures\GUI\Controls\button_filter_character.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_CHARACTER_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(91f, 69f), Texture = @"Textures\GUI\Controls\button_filter_character_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_GRID = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(91f, 69f), Texture = @"Textures\GUI\Controls\button_filter_grid.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_GRID_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(91f, 69f), Texture = @"Textures\GUI\Controls\button_filter_grid_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_ALL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_all.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_ALL_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_all_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_ENERGY = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_energy.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_ENERGY_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_energy_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_STORAGE = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_storage.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_STORAGE_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_storage_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_SYSTEM = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_system.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_FILTER_SYSTEM_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(72f, 68f), Texture = @"Textures\GUI\Controls\button_filter_system_highlight.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_NULL = new MyGuiCompositeTexture();
        public static readonly MyGuiCompositeTexture TEXTURE_HIGHLIGHT_DARK = new MyGuiCompositeTexture()
        {
            Center = new MyGuiSizedTexture() { SizePx = Vector2.Zero, Texture = @"Textures\GUI\Controls\item_highlight_dark.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_INCREASE = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(40f, 40f), Texture = @"Textures\GUI\Controls\button_increase.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_DECREASE = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(40f, 40f), Texture = @"Textures\GUI\Controls\button_decrease.dds", } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ARROW_LEFT            = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(30f, 29f), Texture = @"Textures\GUI\Controls\button_arrow_left.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ARROW_LEFT_HIGHLIGHT  = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(30f, 29f), Texture = @"Textures\GUI\Controls\button_arrow_left_highlight.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ARROW_RIGHT           = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(30f, 29f), Texture = @"Textures\GUI\Controls\button_arrow_right.dds", } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ARROW_RIGHT_HIGHLIGHT = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(30f, 29f), Texture = @"Textures\GUI\Controls\button_arrow_right_highlight.dds", } };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ARROW_SINGLE = new MyGuiHighlightTexture() { SizePx = new Vector2(64f, 64f), Normal = @"Textures\GUI\Icons\buttons\ArrowSingle.dds", Highlight = @"Textures\GUI\Icons\buttons\ArrowSingleHighlight.dds", };
        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ARROW_DOUBLE = new MyGuiHighlightTexture() { SizePx = new Vector2(64f, 64f), Normal = @"Textures\GUI\Icons\buttons\ArrowDouble.dds", Highlight = @"Textures\GUI\Icons\buttons\ArrowDoubleHighlight.dds", };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_LIKE_NORMAL = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\Like.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_LIKE_HIGHLIGHT = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\LikeHighlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_BUG_NORMAL = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\Bug.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_BUG_HIGHLIGHT = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\BugHighlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_HELP_NORMAL = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\Help.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_HELP_HIGHLIGHT = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\HelpHighlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ENVELOPE_NORMAL = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\Envelope.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_ENVELOPE_HIGHLIGHT = new MyGuiCompositeTexture { Center = new MyGuiSizedTexture { SizePx = new Vector2(128f, 128f), Texture = @"Textures\GUI\Icons\EnvelopeHighlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_SQUARE_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(64f, 64f), Texture = @"Textures\GUI\Icons\buttons\SquareButtonHighlight.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_SQUARE_NORMAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(64f, 64f), Texture = @"Textures\GUI\Icons\buttons\SquareButton.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SWITCHONOFF_LEFT_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(90f, 65f), Texture = @"Textures\GUI\Controls\switch_on_off_left_highlight.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_SWITCHONOFF_LEFT_NORMAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(90f, 65f), Texture = @"Textures\GUI\Controls\switch_on_off_left.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SWITCHONOFF_RIGHT_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(90f, 65f), Texture = @"Textures\GUI\Controls\switch_on_off_right_highlight.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_SWITCHONOFF_RIGHT_NORMAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(90f, 65f), Texture = @"Textures\GUI\Controls\switch_on_off_right.dds", }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_INVENTORY_TRASH_NORMAL = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_trash.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_INVENTORY_TRASH_HIGHLIGHT = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_trash_highlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_INVENTORY_SWITCH_NORMAL = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_bag.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_INVENTORY_SWITCH_HIGHLIGHT = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_bag_highlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_CRAFTING_SWITCH_NORMAL = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_hammer.dds" } };
        public static readonly MyGuiCompositeTexture TEXTURE_CRAFTING_SWITCH_HIGHLIGHT = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(71f, 164f), Texture = @"Textures\GUI\Controls\screen_inventory_hammer_highlight.dds" } };

        public static readonly MyGuiCompositeTexture TEXTURE_TEXTBOX = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_left.dds", SizePx = new Vector2(8f, 48f) },
            CenterTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_center.dds", SizePx = new Vector2(4f, 48f) },
            RightTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_right.dds", SizePx = new Vector2(8f, 48f) },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_TEXTBOX_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_left_highlight.dds", SizePx = new Vector2(8f, 48f) },
            CenterTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_center_highlight.dds", SizePx = new Vector2(4f, 48f) },
            RightTop = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\textbox_right_highlight.dds", SizePx = new Vector2(8f, 48f) },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLABLE_LIST_TOOLS_BLOCKS = new MyGuiCompositeTexture()
        {
            Center = new MyGuiSizedTexture() { SizePx = new Vector2(512f, 1030f), Texture = @"Textures\GUI\Screens\TabGScreen.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLABLE_LIST = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_left_top.dds", },
            LeftCenter = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_left_center.dds", },
            LeftBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_left_bottom.dds", },
            CenterTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_center_top.dds", },
            Center = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_center.dds", },
            CenterBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_center_bottom.dds", },
            RightTop = new MyGuiSizedTexture() { SizePx = new Vector2(50f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_right_top.dds", },
            RightCenter = new MyGuiSizedTexture() { SizePx = new Vector2(50f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_right_center.dds", },
            RightBottom = new MyGuiSizedTexture() { SizePx = new Vector2(50f, 4f), Texture = @"Textures\GUI\Controls\scrollable_list_right_bottom.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_RECTANGLE_DARK = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_left_top.dds", },
            LeftCenter = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_left_center.dds", },
            LeftBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_left_bottom.dds", },
            CenterTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_center_top.dds", },
            Center = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_center.dds", },
            CenterBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_center_bottom.dds", },
            RightTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_right_top.dds", },
            RightCenter = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_right_center.dds", },
            RightBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_dark_right_bottom.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_NEWS_BACKGROUND = new MyGuiCompositeTexture()
        {
            LeftTop      = new MyGuiSizedTexture() { SizePx = new Vector2(14f, 14f), Texture = @"Textures\GUI\Controls\news_background_left_top.dds", },
            LeftCenter   = new MyGuiSizedTexture() { SizePx = new Vector2(14f, 24f), Texture = @"Textures\GUI\Controls\news_background_left_center.dds", },
            LeftBottom   = new MyGuiSizedTexture() { SizePx = new Vector2(14f,  5f), Texture = @"Textures\GUI\Controls\news_background_left_bottom.dds", },
            CenterTop    = new MyGuiSizedTexture() { SizePx = new Vector2(15f, 14f), Texture = @"Textures\GUI\Controls\news_background_center_top.dds", },
            Center       = new MyGuiSizedTexture() { SizePx = new Vector2(15f, 24f), Texture = @"Textures\GUI\Controls\news_background_center.dds", },
            CenterBottom = new MyGuiSizedTexture() { SizePx = new Vector2(15f,  5f), Texture = @"Textures\GUI\Controls\news_background_center_bottom.dds", },
            RightTop     = new MyGuiSizedTexture() { SizePx = new Vector2( 4f, 14f), Texture = @"Textures\GUI\Controls\news_background_right_top.dds", },
            RightCenter  = new MyGuiSizedTexture() { SizePx = new Vector2( 4f, 24f), Texture = @"Textures\GUI\Controls\news_background_right_center.dds", },
            RightBottom  = new MyGuiSizedTexture() { SizePx = new Vector2( 4f,  5f), Texture = @"Textures\GUI\Controls\news_background_right_bottom.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_NEWS_PAGING_BACKGROUND = new MyGuiCompositeTexture()
        {
            LeftTop      = new MyGuiSizedTexture() { SizePx = new Vector2(14f, 14f), Texture = @"Textures\GUI\Controls\news_background_left_top.dds", },
            LeftCenter   = new MyGuiSizedTexture() { SizePx = new Vector2(14f, 24f), Texture = @"Textures\GUI\Controls\news_background_left_center.dds", },
            CenterTop    = new MyGuiSizedTexture() { SizePx = new Vector2(15f, 14f), Texture = @"Textures\GUI\Controls\news_background_center_top.dds", },
            Center       = new MyGuiSizedTexture() { SizePx = new Vector2(15f, 24f), Texture = @"Textures\GUI\Controls\news_background_center.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_RECTANGLE_NEUTRAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_left_top.dds", },
            LeftCenter = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_left_center.dds", },
            LeftBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_left_bottom.dds", },
            CenterTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_center_top.dds", },
            Center = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_center.dds", },
            CenterBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_center_bottom.dds", },
            RightTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_right_top.dds", },
            RightCenter = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_right_center.dds", },
            RightBottom = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 4f), Texture = @"Textures\GUI\Controls\rectangle_neutral_right_bottom.dds", },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_COMBOBOX_NORMAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(20f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_left.dds" },
            CenterTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_center.dds" },
            RightTop = new MyGuiSizedTexture() { SizePx = new Vector2(51f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_right.dds" },
        };
        public static readonly MyGuiCompositeTexture TEXTURE_COMBOBOX_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(20f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_highlight_left.dds" },
            CenterTop = new MyGuiSizedTexture() { SizePx = new Vector2(4f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_highlight_center.dds" },
            RightTop = new MyGuiSizedTexture() { SizePx = new Vector2(51f, 48f), Texture = @"Textures\GUI\Controls\combobox_default_highlight_right.dds" },
        };

        public static readonly MyGuiHighlightTexture TEXTURE_GRID_ITEM = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Controls\grid_item.dds",
            Highlight = @"Textures\GUI\Controls\grid_item_highlight.dds",
            SizePx = new Vector2(82f, 82f),
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_LARGE_BLOCK = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\large_block.dds",
            Highlight = @"Textures\GUI\Icons\buttons\large_block_highlight.dds",
            SizePx = new Vector2(41f, 41f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_SMALL_BLOCK = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\small_block.dds",
            Highlight = @"Textures\GUI\Icons\buttons\small_block_highlight.dds",
            SizePx = new Vector2(43f, 43f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_TOOL = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\tool.dds",
            Highlight = @"Textures\GUI\Icons\buttons\tool_highlight.dds",
            SizePx = new Vector2(41f, 41f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_COMPONENT = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\component.dds",
            Highlight = @"Textures\GUI\Icons\buttons\component_highlight.dds",
            SizePx = new Vector2(37f, 45f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_DISASSEMBLY = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\disassembly.dds",
            Highlight = @"Textures\GUI\Icons\buttons\disassembly_highlight.dds",
            SizePx = new Vector2(32f, 32f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_REPEAT = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\repeat.dds",
            Highlight = @"Textures\GUI\Icons\buttons\repeat_highlight.dds",
            SizePx = new Vector2(54f, 34f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BUTTON_ICON_SLAVE = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\coopmode.dds",
            Highlight = @"Textures\GUI\Icons\buttons\coopmode.dds",
            SizePx = new Vector2(54f, 34f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_WHITE_FLAG = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\WhiteFlag.dds",
            Highlight = @"Textures\GUI\WhiteFlag.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_SENT_WHITE_FLAG = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\RequestSent.dds",
            Highlight = @"Textures\GUI\RequestSent.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_SENT_JOIN_REQUEST = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\PlayerRequest.dds",
            Highlight = @"Textures\GUI\PlayerRequest.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiPaddedTexture TEXTURE_MESSAGEBOX_BACKGROUND_ERROR = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\message_background_red.dds",
            SizePx = new Vector2(1343f, 321f),
            PaddingSizePx = new Vector2(20f, 25f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_MESSAGEBOX_BACKGROUND_INFO = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\message_background_blue.dds",
            SizePx = new Vector2(1343f, 321f),
            PaddingSizePx = new Vector2(20f, 25f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_BACKGROUND = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\screen_background.dds",
            SizePx = new Vector2(1024f, 1024f),
            PaddingSizePx = new Vector2(24f, 24f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_BACKGROUND_RED = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\screen_background_red.dds",
            SizePx = new Vector2(1024f, 1024f),
            PaddingSizePx = new Vector2(24f, 24f),
        };
        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_TOOLS_BACKGROUND_BLOCKS = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\CenterGScreen.dds",
            SizePx = new Vector2(913f, 820f),
            PaddingSizePx = new Vector2(12f, 10f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_TOOLS_BACKGROUND_CONTROLS = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\screen_tools_background_controls.dds",
            SizePx = new Vector2(397f, 529f),
            PaddingSizePx = new Vector2(24f, 24f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_TOOLS_BACKGROUND_WEAPONS = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\screen_tools_background_weapons.dds",
            SizePx = new Vector2(868f, 110f),
            PaddingSizePx = new Vector2(12f, 9f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_SCREEN_STATS_BACKGROUND = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\screen_stats_background.dss",
            SizePx = new Vector2(256f, 128f),
            PaddingSizePx = new Vector2(12f, 12f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_MODS_LOCAL = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\ModFolderIcon.dds",
            Highlight = @"Textures\GUI\Icons\buttons\ModFolderIcon.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_BLUEPRINTS_LOCAL = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\BluePrintFolderIcon.dds",
            Highlight = @"Textures\GUI\Icons\buttons\BluePrintFolderIcon.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_BLUEPRINTS_ARROW = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\ArrowFolderIcon.dds",
            Highlight = @"Textures\GUI\Icons\buttons\ArrowFolderIcon.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiHighlightTexture TEXTURE_ICON_MODS_WORKSHOP = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Icons\buttons\ModSteamIcon.dds",
            Highlight = @"Textures\GUI\Icons\buttons\ModSteamIcon.dds",
            SizePx = new Vector2(53f, 40f)
        };

        public static readonly MyGuiCompositeTexture TEXTURE_CHECKBOX_DEFAULT_NORMAL_CHECKED = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(57f, 54f), Texture = @"Textures\GUI\Controls\checkbox_checked.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_CHECKBOX_DEFAULT_NORMAL_UNCHECKED = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(57f, 54f), Texture = @"Textures\GUI\Controls\checkbox_unchecked.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_CHECKED = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(57f, 54f), Texture = @"Textures\GUI\Controls\checkbox_checked_highlight.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_UNCHECKED = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(57f, 54f), Texture = @"Textures\GUI\Controls\checkbox_unchecked_highlight.dds", }
        };

        public static MyGuiHighlightTexture TEXTURE_SLIDER_THUMB_DEFAULT = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Controls\slider_thumb.dds",
            Highlight = @"Textures\GUI\Controls\slider_thumb_highlight.dds",
            SizePx = new Vector2(32f, 32f),
        };

        public static MyGuiHighlightTexture TEXTURE_HUE_SLIDER_THUMB_DEFAULT = new MyGuiHighlightTexture()
        {
            Normal = @"Textures\GUI\Controls\hue_slider_thumb.dds",
            Highlight = @"Textures\GUI\Controls\hue_slider_thumb_highlight.dds",
            SizePx = new Vector2(32f, 32f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_HUD_BG_MEDIUM_DEFAULT = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\hud_bg_medium_default.dds",
            SizePx = new Vector2(213f, 183f),
            PaddingSizePx = new Vector2(9f, 15f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_HUD_BG_LARGE_DEFAULT = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\hud_bg_large_default.dds",
            SizePx = new Vector2(300f, 366f),
            PaddingSizePx = new Vector2(9f, 15f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_HUD_BG_MEDIUM_RED = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\hud_bg_medium_red.dds",
            SizePx = new Vector2(213f, 181f),
            PaddingSizePx = new Vector2(9f, 15f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_HUD_BG_MEDIUM_RED2 = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\hud_bg_medium_red2.dds",
            SizePx = new Vector2(3f, 127f),
            PaddingSizePx = new Vector2(0f, 15f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_HUD_BG_PERFORMANCE = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Screens\hud_bg_performance.dds",
            SizePx = new Vector2(441f, 124f),
            PaddingSizePx = new Vector2(0f, 0f),
        };

        public static readonly MyGuiPaddedTexture TEXTURE_VOICE_CHAT = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Icons\VoiceIcon.dds",
            SizePx = new Vector2(128f, 128f),
            PaddingSizePx = new Vector2(5f, 5f),
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SLIDER_RAIL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_left.dds",
                SizePx = new Vector2(23f, 55f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_center.dds",
                SizePx = new Vector2(4f, 55f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_right.dds",
                SizePx = new Vector2(23f, 55f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SLIDER_RAIL_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_left_highlight.dds",
                SizePx = new Vector2(23f, 55f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_center_highlight.dds",
                SizePx = new Vector2(4f, 55f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\slider_rail_right_highlight.dds",
                SizePx = new Vector2(23f, 55f),
            },
        };


        public static readonly MyGuiCompositeTexture TEXTURE_HUE_SLIDER_RAIL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_left.dds",
                SizePx = new Vector2(23f, 55f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_center.dds",
                SizePx = new Vector2(4f, 55f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_right.dds",
                SizePx = new Vector2(23f, 55f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_HUE_SLIDER_RAIL_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_left_highlight.dds",
                SizePx = new Vector2(23f, 55f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_center_highlight.dds",
                SizePx = new Vector2(4f, 55f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\hue_slider_rail_right_highlight.dds",
                SizePx = new Vector2(23f, 55f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_V_THUMB = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_top.dds",
                SizePx = new Vector2(46f, 39f),
            },
            LeftCenter = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_center.dds",
                SizePx = new Vector2(46f, 4f),
            },
            LeftBottom = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_bottom.dds",
                SizePx = new Vector2(46f, 26f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_V_THUMB_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_top_highlight.dds",
                SizePx = new Vector2(46f, 39f),
            },
            LeftCenter = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_center_highlight.dds",
                SizePx = new Vector2(46f, 4f),
            },
            LeftBottom = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_v_thumb_bottom_highlight.dds",
                SizePx = new Vector2(46f, 26f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_V_BACKGROUND = new MyGuiCompositeTexture();

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_H_THUMB = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_left.dds",
                SizePx = new Vector2(39f, 46f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_center.dds",
                SizePx = new Vector2(4f, 46f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_right.dds",
                SizePx = new Vector2(26f, 46f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_H_THUMB_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_left_highlight.dds",
                SizePx = new Vector2(39f, 46f),
            },
            CenterTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_center_highlight.dds",
                SizePx = new Vector2(4f, 46f),
            },
            RightTop = new MyGuiSizedTexture()
            {
                Texture = @"Textures\GUI\Controls\scrollbar_h_thumb_right_highlight.dds",
                SizePx = new Vector2(26f, 46f),
            },
        };

        public static readonly MyGuiCompositeTexture TEXTURE_SCROLLBAR_H_BACKGROUND = new MyGuiCompositeTexture();

        public static readonly MyGuiCompositeTexture TEXTURE_TOOLBAR_TAB = new MyGuiCompositeTexture()
        {
            Center = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\ToolBarTab.dds" }
        };

        public static readonly MyGuiCompositeTexture TEXTURE_TOOLBAR_TAB_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            Center = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Controls\ToolBarTabHighlight.dds" }
        };

        public const string TEXTURE_BACKGROUND_FADE = @"Textures\Gui\Screens\screen_background_fade.dds";

        public const string BUTTON_LOCKED = "Textures\\GUI\\LockedButton.dds";

        public const string BLANK_TEXTURE = "Textures\\GUI\\Blank.dds";

		public static MyGuiPaddedTexture TEXTURE_HUD_GRAVITY_GLOBE = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\GravityHudGlobe.dds",
			SizePx = new Vector2(138, 138),
			PaddingSizePx = new Vector2(0f, 0f),
		};

		public static MyGuiPaddedTexture TEXTURE_HUD_GRAVITY_LINE = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\GravityHudLine.dds",
			SizePx = new Vector2(228, 2),
			PaddingSizePx = new Vector2(0f, 0f),
		};

		public static MyGuiPaddedTexture TEXTURE_HUD_GRAVITY_HORIZON = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\GravityHudHorizon.dds",
			SizePx = new Vector2(512, 512),
			PaddingSizePx = new Vector2(0f, 0f),
		};

        public static readonly MyGuiCompositeTexture TEXTURE_GUI_BLANK = new MyGuiCompositeTexture()
        {
            Center = new MyGuiSizedTexture() { Texture = @"Textures\GUI\Blank.dds" }
        };

		public static MyGuiPaddedTexture TEXTURE_HUD_STATS_BG = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\Screens\screen_stats_background.dds",
			SizePx = new Vector2(256, 128),
			PaddingSizePx = new Vector2(6f, 6f),
		};

		public static MyGuiPaddedTexture TEXTURE_HUD_STAT_EFFECT_ARROW_UP = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\Icons\ArrowUpBrown.dds",
		};

		public static MyGuiPaddedTexture TEXTURE_HUD_STAT_EFFECT_ARROW_DOWN = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\Icons\ArrowDownRed.dds",
		};


		public static MyGuiPaddedTexture TEXTURE_HUD_STAT_BAR_BG = new MyGuiPaddedTexture()
		{
			Texture = @"Textures\GUI\Screens\screen_stats_bar_background.dds",
			SizePx = new Vector2(72, 13),
			PaddingSizePx = new Vector2(1f, 1f),
		};

        public const string CURSOR_ARROW = "Textures\\GUI\\MouseCursor.dds";
        public const string CURSOR_HAND = "Textures\\GUI\\MouseCursorHand.dds";

        public const string PROGRESS_BAR = "Textures\\GUI\\ProgressBar.dds";

        public const string LOADING_TEXTURE = "Textures\\GUI\\screens\\screen_loading_wheel.dds";
        public const string LOADING_TEXTURE_LOADING_SCREEN = "Textures\\GUI\\screens\\screen_loading_wheel_loading_screen.dds";

        // General gui constants
        public const float MOUSE_CURSOR_SPEED_MULTIPLIER = 1.3f;
        public const int VIDEO_OPTIONS_CONFIRMATION_TIMEOUT_IN_MILISECONDS = 60 * 1000;
        public static readonly Vector2 SHADOW_OFFSET = new Vector2(0.000f, 0.000f);
        public static readonly Vector4 CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER = new Vector4(1.2f, 1.2f, 1.2f, 1.0f);
        public static readonly Vector2 CONTROLS_DELTA = new Vector2(0, 0.0525f);
        public static readonly Vector4 ROTATING_WHEEL_COLOR = Vector4.One;
        public const float ROTATING_WHEEL_DEFAULT_SCALE = 0.36f;
        public static readonly int SHOW_CONTROL_TOOLTIP_DELAY = 20; //in milliseconds - after this period, show tooltip on control
        public static readonly float TOOLTIP_DISTANCE_FROM_BORDER = 0.003f; //in normalized coordinates
        public static readonly Vector4 DEFAULT_CONTROL_BACKGROUND_COLOR = new Vector4(1f, 1f, 1f, 1f);
        public static readonly Vector4 DEFAULT_CONTROL_NONACTIVE_COLOR = new Vector4(0.9f, 0.9f, 0.9f, 0.95f);
        public static Color DISABLED_BUTTON_COLOR = new Color(87, 127, 147, 210);
        public static Vector4 DISABLED_BUTTON_COLOR_VECTOR = new Vector4(0.52f, 0.6f, 0.63f, 0.9f);
        public static Vector4 DISABLED_BUTTON_TEXT_COLOR = new Vector4(0.4f, 0.47f, 0.5f, 0.8f);
        public static float LOCKBUTTON_SIZE_MODIFICATION = 0.85f;

        public const float APP_VERSION_TEXT_SCALE = 0.95f;

        // Screen gui constants
        public static readonly Vector4 SCREEN_BACKGROUND_FADE_BLANK_DARK = new Vector4(0.03f, 0.04f, 0.05f, 0.7f);
        public static readonly Vector4 SCREEN_BACKGROUND_FADE_BLANK_DARK_PROGRESS_SCREEN = new Vector4(0.03f, 0.04f, 0.05f, 0.4f);
        public static readonly float SCREEN_CAPTION_DELTA_Y = 0.05f;
        public static readonly Vector4 SCREEN_BACKGROUND_COLOR = Vector4.One;
        //  This is screen height we use as reference, so all fonts, textures, etc are made for it and if this height resolution used, it will be 1.0
        //  If e.g. we use vertical resolution 600, then averything must by scaled by 600 / 1200 = 0.5
        public const float REFERENCE_SCREEN_HEIGHT = 1080;
        public const float SAFE_ASPECT_RATIO = 4.0f / 3.0f;

        public const float LOADING_PLEASE_WAIT_SCALE = 1.1f;
        public static readonly Vector2 LOADING_PLEASE_WAIT_POSITION = new Vector2(0.5f, 0.95f);
        public static readonly Vector4 LOADING_PLEASE_WAIT_COLOR = Vector4.One;

        // Textbox gui constants
        public const int TEXTBOX_MOVEMENT_DELAY = 100;
        //  Delay between we accept same key press (e.g. when user holds left key, or X key for a longer period)
        public const int TEXTBOX_CHANGE_DELAY = 500;
        public const int TEXTBOX_INITIAL_THROTTLE_DELAY = 500;
        public const int TEXTBOX_REPEAT_THROTTLE_DELAY = 50;
        public const string TEXTBOX_FALLBACK_CHARACTER = "#";
        public static readonly Vector2 TEXTBOX_TEXT_OFFSET = new Vector2(0.0075f, 0.005f);

        public static readonly Vector2 TEXTBOX_MEDIUM_SIZE = new Vector2(404f / 1600f, 66f / 1200f);

        // Mouse gui constants
        public static readonly Vector4 MOUSE_CURSOR_COLOR = Vector4.One;
        public const float MOUSE_CURSOR_SCALE = 1;

        // Rotation constants
        public const float MOUSE_ROTATION_INDICATOR_MULTIPLIER = 0.075f;
        public const float ROTATION_INDICATOR_MULTIPLIER = 0.15f;  // empirical value for nice keyboard rotation: mouse/joystick/gamepad sensitivity can be tweaked by the user

        // Button gui constants
        public static readonly Vector4 BUTTON_BACKGROUND_COLOR = DEFAULT_CONTROL_BACKGROUND_COLOR;
        public static readonly Vector2 MENU_BUTTONS_POSITION_DELTA = new Vector2(0, 0.06f);
        public static readonly Vector4 BACK_BUTTON_BACKGROUND_COLOR = BUTTON_BACKGROUND_COLOR;
        public static readonly Vector4 BACK_BUTTON_TEXT_COLOR = DEFAULT_CONTROL_NONACTIVE_COLOR;
        public static readonly Vector2 BACK_BUTTON_SIZE = new Vector2(260f / 1600f, 70f / 1200f);
        public static readonly Vector2 OK_BUTTON_SIZE = new Vector2(0.177f, 0.0765f);
        public static readonly Vector2 GENERIC_BUTTON_SPACING = new Vector2(0.002f, 0.002f);

        public const float MAIN_MENU_BUTTON_TEXT_SCALE = 1.08f;

        // TreeView gui constants
        public static Vector4 TREEVIEW_SELECTED_ITEM_COLOR = new Vector4(0.03f, 0.02f, 0.03f, 0.4f);
        public static Vector4 TREEVIEW_DISABLED_ITEM_COLOR = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
        public static readonly Vector4 TREEVIEW_TEXT_COLOR = DEFAULT_CONTROL_NONACTIVE_COLOR;
        public static readonly Vector4 TREEVIEW_VERTICAL_LINE_COLOR = new Vector4(158 / 255f, 208 / 255f, 1, 1);
        public static readonly Vector2 TREEVIEW_VSCROLLBAR_SIZE = new Vector2(20 * 3, 159 * 4) / 3088;
        public static readonly Vector2 TREEVIEW_HSCROLLBAR_SIZE = new Vector2(159 * 3, 20 * 4) / 3088;

        // Combobox gui constants
        public static readonly Vector2 COMBOBOX_MEDIUM_SIZE = new Vector2(0.3f, 0.03f);
        public static readonly Vector2 COMBOBOX_MEDIUM_ELEMENT_SIZE = new Vector2(0.3f, 0.03f);
        public static readonly Vector2 COMBOBOX_VSCROLLBAR_SIZE = new Vector2(0.02f, 0.0805958545f);

        // Listbox gui constants
        public static readonly Vector4 LISTBOX_BACKGROUND_COLOR = DEFAULT_CONTROL_BACKGROUND_COLOR;
        public static readonly Vector2 LISTBOX_ICON_SIZE = new Vector2(0.0205f, 0.02733f);
        public static readonly Vector2 LISTBOX_ICON_OFFSET = LISTBOX_ICON_SIZE/8;
        public static readonly float   LISTBOX_WIDTH = 0.197f;

        // Drag and drop gui constants
        public static readonly Vector2 DRAG_AND_DROP_TEXT_OFFSET = new Vector2(0.01f, 0);
        public static readonly Vector4 DRAG_AND_DROP_TEXT_COLOR = DEFAULT_CONTROL_NONACTIVE_COLOR;
        public static readonly Vector2 DRAG_AND_DROP_SMALL_SIZE = new Vector2(DRAG_AND_DROP_ICON_SIZE_X, DRAG_AND_DROP_ICON_SIZE_Y);
        public static readonly Vector4 DRAG_AND_DROP_BACKGROUND_COLOR = new Vector4(1f, 1f, 1f, 1f);
        public const float DRAG_AND_DROP_ICON_SIZE_X = 0.07395f;
        public const float DRAG_AND_DROP_ICON_SIZE_Y = DRAG_AND_DROP_ICON_SIZE_X * 4 / 3;

        // Slider gui constants
        public static readonly float SLIDER_INSIDE_OFFSET_X = 0.017f;
        public static readonly int REPEAT_PRESS_DELAY = 100; //delay for repeating arrow press

        // Messagebox gui constants
        public static readonly Vector2 MESSAGE_BOX_BUTTON_SIZE_SMALL = new Vector2(190f / 1600f, 65f / 1200f);

        // Tool tips constants:
        public static Vector2 TOOL_TIP_RELATIVE_DEFAULT_POSITION = new Vector2(0.025f, 0.03f);// new Vector2(0.025f, 0.065f);
        public const float TOOL_TIP_TEXT_SCALE = 0.7f;

        //  How long takes transition of opening and closing of the screen - in miliseconds
        public const int TRANSITION_OPENING_TIME = 200;
        public const int TRANSITION_CLOSING_TIME = 200;

        //  Min and max values for transition alpha, where max is alpha when screen is fully active
        public const float TRANSITION_ALPHA_MIN = 0;
        public const float TRANSITION_ALPHA_MAX = 1;

        //  This is the size of background's texture. I hardcoded it here because these textures are resized to power of two, so getting
        //  height/width from texture will return not usable results. So, if you ever change this texture, change this param too.
        public static readonly Vector2I LOADING_BACKGROUND_TEXTURE_REAL_SIZE = new Vector2I(1920, 1080);

        //  Slow down drawing of "loading..." screens, so background thread who is actualy right now loading content will have more time
        //  Plus these two threads won't fight for graphic device. It's not big difference, just 10% or so.
        public const int LOADING_THREAD_DRAW_SLEEP_IN_MILISECONDS = 10;

        // Colored texts contstants
        public const float COLORED_TEXT_DEFAULT_TEXT_SCALE = 0.75f;
        public static readonly Color COLORED_TEXT_DEFAULT_COLOR = new Color(DEFAULT_CONTROL_NONACTIVE_COLOR);
        public static readonly Color COLORED_TEXT_DEFAULT_HIGHLIGHT_COLOR = new Color(MyGuiConstants.CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER * DEFAULT_CONTROL_NONACTIVE_COLOR);

        // Multiline label constants
        public static readonly Vector2 MULTILINE_LABEL_BORDER = new Vector2(0.01f, 0.0050f);

        public static readonly float DEBUG_LABEL_TEXT_SCALE = 1.0f;
        public static readonly float DEBUG_BUTTON_TEXT_SCALE = 0.8f;
        public static readonly float DEBUG_STATISTICS_TEXT_SCALE = 0.75f;
        public static readonly float DEBUG_STATISTICS_ROW_DISTANCE = 0.020f;

        public const float FONT_SCALE = 28.8f / 37f;  // Ratio between font size and line height has changed: old was 28, new is 37 (28.8 makes it closer to the font size change 18->23)
        public const float FONT_TOP_SIDE_BEARING = 3 * 23f / 18f;  // This is exact: old font size was 18, new font size 23, X padding is 7 and Y padding is 4, so (7-4)*23/18

        public static readonly Vector2 NETGRAPH_INITIAL_POSITION = new Vector2(0.97f, 0.5f);
        public const int NETGRAPH_BAR_ALPHA = 198;
        public static readonly Color NETGRAPH_UNRELIABLE_PACKET_COLOR = new Color(120, 0, 0, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_UNRELIABLE_PACKET_COLOR_TOP = new Color(205, 60, 60, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_RELIABLE_PACKET_COLOR = new Color(50, 120, 50, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_RELIABLE_PACKET_COLOR_TOP = new Color(50, 205, 50, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_SENT_PACKET_COLOR = new Color(63, 63, 120, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_SENT_PACKET_COLOR_TOP = new Color(64, 224, 208, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_PACKET_SCALE_SMALL_INTERVAL_COLOR = new Color(153, 50, 204, NETGRAPH_BAR_ALPHA);
        public static readonly Color NETGRAPH_PACKET_SCALE_INTERVAL_POINT_COLOR = new Color(255, 20, 147, NETGRAPH_BAR_ALPHA);
        public const int NETGRAPH_SMALL_INTERVAL_COUNT = 3;
        public static readonly float NETGRAPH_BG_NORM_SIZE_Y = 0.39f;
        public static readonly MyGuiPaddedTexture NETGRAPH_BG_TEXTURE = new MyGuiPaddedTexture()
        {
            Texture = @"Textures\GUI\Blank.dds",
            SizePx = new Vector2(245, 275),
            PaddingSizePx = new Vector2(15, 15),
        };

        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_SQUARE_SMALL_HIGHLIGHT = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(45f, 45f), Texture = @"Textures\GUI\Icons\buttons\SquareButtonHighlight.dds", }
        };
        public static readonly MyGuiCompositeTexture TEXTURE_BUTTON_SQUARE_SMALL_NORMAL = new MyGuiCompositeTexture()
        {
            LeftTop = new MyGuiSizedTexture() { SizePx = new Vector2(45f, 45f), Texture = @"Textures\GUI\Icons\buttons\SquareButton.dds", }
        };

        #region CubeBuilder

        public const string CB_FREE_MODE_ICON = @"Textures\GUI\CubeBuilder\FreeModIcon.png";
        public const string CB_LCS_GRID_ICON = @"Textures\GUI\CubeBuilder\OnGridIcon.png";
        public const string CB_LARGE_GRID_MODE = @"Textures\GUI\CubeBuilder\GridModeLargeHighl.png";
        public const string CB_SMALL_GRID_MODE = @"Textures\GUI\CubeBuilder\GridModeSmallHighl.png";

        #endregion

        #region Broadcast screen

        public const string BS_ANTENNA_ON = @"Textures\GUI\Icons\BroadcastStatus\AntennaOn.png";
        public const string BS_ANTENNA_OFF = @"Textures\GUI\Icons\BroadcastStatus\AntennaOff.png";
        public const string BS_KEY_ON = @"Textures\GUI\Icons\BroadcastStatus\KeyOn.png";
        public const string BS_KEY_OFF = @"Textures\GUI\Icons\BroadcastStatus\KeyOff.png";
        public const string BS_REMOTE_ON = @"Textures\GUI\Icons\BroadcastStatus\RemoteOn.png";
        public const string BS_REMOTE_OFF = @"Textures\GUI\Icons\BroadcastStatus\RemoteOff.png";

        #endregion

    }

}

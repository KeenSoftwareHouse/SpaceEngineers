#region Using

using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Gui.RichTextLabel;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    public enum MyMissionScreenStyleEnum
    {
        RED,
        BLUE,
    }

    public class MyGuiScreenText : MyGuiScreenBase
    {
        public class Style
        {
            public string BackgroundTextureName;
            public string CaptionFont;
            public string TextFont;
            public MyGuiControlButtonStyleEnum ButtonStyle;
            public bool ShowBackgroundPanel;
        }

        static readonly Style[] m_styles;
        private static Vector2 m_defaultWindowSize = new Vector2(0.6f, 0.7f);
        private static Vector2 m_defaultDescSize = new Vector2(0.5f, 0.44f);

        private Vector2 m_windowSize;
        protected Vector2 m_descSize;
        private  string m_currentObjectivePrefix = "Current objective: ";
        private StringBuilder m_okButtonCaption = null;

        private string m_missionTitle = "Mission Title";
        private string m_currentObjective = "";
        protected string m_description = "";
        protected bool m_enableEdit = false;


        protected MyGuiControlLabel m_titleLabel;
        private MyGuiControlLabel m_currentObjectiveLabel;
        protected MyGuiControlMultilineText m_descriptionBox;
        protected MyGuiControlButton m_okButton;
        protected MyGuiControlCompositePanel m_descriptionBackgroundPanel = null;
        private Action<ResultEnum> m_resultCallback = null;
        private ResultEnum m_screenResult = ResultEnum.CANCEL;
        private Style m_style;

        public MyGuiControlMultilineText Description
        {
            get { return m_descriptionBox; }
        }

        static MyGuiScreenText() 
        {
            m_styles = new Style[MyUtils.GetMaxValueFromEnum<MyMessageBoxStyleEnum>() + 1];
            m_styles[(int)MyMissionScreenStyleEnum.RED] = new Style()
            {
                BackgroundTextureName = MyGuiConstants.TEXTURE_SCREEN_BACKGROUND_RED.Texture,
                CaptionFont       = MyFontEnum.White,
                TextFont          = MyFontEnum.White,
                ButtonStyle       = MyGuiControlButtonStyleEnum.Red,
                ShowBackgroundPanel = false,
            };
            m_styles[(int)MyMissionScreenStyleEnum.BLUE] = new Style()
            {
                BackgroundTextureName = MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture,
                CaptionFont = MyFontEnum.White,
                TextFont = MyFontEnum.Blue,
                ButtonStyle = MyGuiControlButtonStyleEnum.Default,
                ShowBackgroundPanel = true,
            };

        }

        public MyGuiScreenText(
            string missionTitle = null,
            string currentObjectivePrefix =null,
            string currentObjective = null,
            string description = null,

            Action<ResultEnum> resultCallback = null, 
            string okButtonCaption = null,
            Vector2? windowSize = null,
            Vector2? descSize = null,
            bool editEnabled = false,
            bool canHideOthers = true,
            bool enableBackgroundFade = false,
            MyMissionScreenStyleEnum style = MyMissionScreenStyleEnum.BLUE)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, windowSize.HasValue ? windowSize.Value : m_defaultWindowSize, false, null)
        {
            m_style = m_styles[(int)style];
            m_enableEdit = editEnabled;
            m_descSize = descSize.HasValue ? descSize.Value : m_defaultDescSize;
            m_windowSize = windowSize.HasValue ? windowSize.Value : m_defaultWindowSize;

            m_missionTitle = missionTitle ?? m_missionTitle;
            m_currentObjectivePrefix = currentObjectivePrefix ??m_currentObjectivePrefix;
            m_currentObjective = currentObjective ?? m_currentObjective;
            m_description = description ?? m_description;

            //m_description = "supr <b>rich</b> <i>text</i>\nwritten in 2 lines.";
            //offset: descPosition + padding, size: descSize

            m_resultCallback = resultCallback;

            m_okButtonCaption = okButtonCaption != null ? new StringBuilder(okButtonCaption) : MyTexts.Get(MyCommonTexts.Ok);
          
            m_closeOnEsc = true;

            RecreateControls(true);

            m_titleLabel.Font = m_style.CaptionFont;
            m_currentObjectiveLabel.Font = m_style.CaptionFont;
            m_descriptionBox.Font = m_style.TextFont;
            m_backgroundTexture = m_style.BackgroundTextureName;
            m_okButton.VisualStyle = m_style.ButtonStyle;
            m_descriptionBackgroundPanel.Visible = m_style.ShowBackgroundPanel;

            m_isTopScreen = false;
            m_isTopMostScreen = false;
            CanHideOthers = canHideOthers;
            EnabledBackgroundFade = enableBackgroundFade;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMission";
        }

        public override void RecreateControls(bool constructor)
        {
            var titlePos = new Vector2(0f, -0.3f);
            var descSize = m_descSize;
            var descPosition = new Vector2(-descSize.X/2, titlePos.Y + 0.10f);
            var objSize = new Vector2(0.2f, 0.3f);
            var objOffset = new Vector2(0.32f, 0f);
            var padding = new Vector2(0.005f, 0f);

            var curObjPos = new Vector2(0f, titlePos.Y + 0.05f);

            base.RecreateControls(constructor);

            CloseButtonEnabled = true;

            m_okButton = new MyGuiControlButton(position: new Vector2(0f, 0.29f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: m_okButtonCaption , onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(m_okButton);

            m_titleLabel = new MyGuiControlLabel(text: m_missionTitle, position: titlePos, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, textScale: 1.5f);
            Controls.Add(m_titleLabel);
            
            m_currentObjectiveLabel = new MyGuiControlLabel(position: curObjPos, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, textScale: 1f);
            Controls.Add(m_currentObjectiveLabel);
            SetCurrentObjective(m_currentObjective);
 

            m_descriptionBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, descPosition, descSize, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            m_descriptionBox = AddMultilineText(offset: descPosition + padding, size: descSize, selectable: false);
            m_descriptionBox.Text = new StringBuilder(m_description);
        }

        protected MyGuiControlCompositePanel AddCompositePanel(MyGuiCompositeTexture texture, Vector2 position, Vector2 size, MyGuiDrawAlignEnum panelAlign)
        {
            var panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = texture
            };
            panel.Position = position;
            panel.Size = size;
            panel.OriginAlign = panelAlign;
            Controls.Add(panel);

            return panel;
        }

        protected virtual MyGuiControlMultilineText AddMultilineText(Vector2? size = null, Vector2? offset = null, float textScale = 1.0f, bool selectable = false, MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, MyGuiDrawAlignEnum textBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            Vector2 textboxSize = size ?? this.Size ?? new Vector2(1.2f, 0.5f);

            MyGuiControlMultilineText textbox = null;
            if (m_enableEdit)
            {
                textbox = new MyGuiControlMultilineEditableText(
                    position: textboxSize / 2.0f + (offset ?? Vector2.Zero),
                    size: textboxSize,
                    backgroundColor: Color.White.ToVector4(),
                    textAlign: textAlign,
                    textBoxAlign: textBoxAlign,
                    font: MyFontEnum.White);
            }
            else
            {
                textbox = new MyGuiControlMultilineText(
                    position: textboxSize / 2.0f + (offset ?? Vector2.Zero),
                    size: textboxSize,
                    backgroundColor: Color.White.ToVector4(),
                    textAlign: textAlign,
                    textBoxAlign: textBoxAlign,
                    selectable: m_enableEdit,
                    font: MyFontEnum.White);
            }

            Controls.Add(textbox);

            return textbox;
        }

        void OkButtonClicked(MyGuiControlButton button)
        {
            m_screenResult = ResultEnum.OK;
            CloseScreen();
        }

        public void SetTitle(string title)
        {
            m_missionTitle = title;
            m_titleLabel.Text = title;
        }

        public void SetCurrentObjective(string objective)
        {
            m_currentObjective = objective;
            m_currentObjectiveLabel.Text = m_currentObjectivePrefix + m_currentObjective;
        }

        public void SetDescription(string desc)
        {
            m_description = desc;
            m_descriptionBox.Clear();
            m_descriptionBox.Text = new StringBuilder(m_description);
        }

        public void AppendTextToDescription(string text, Vector4 color, string font = MyFontEnum.White, float scale = 1.0f)
        {
            m_description += text;
            m_descriptionBox.AppendText(text, font, scale, color);
        }

        public void AppendTextToDescription(string text, string font = MyFontEnum.White, float scale = 1.0f)
        {
            m_description += text;
            m_descriptionBox.AppendText(text, font, scale, Vector4.One);
        }

        public void SetCurrentObjectivePrefix(string prefix)
        {
            m_currentObjectivePrefix = prefix;
        }
        public void SetOkButtonCaption(string caption)
        {
            m_okButtonCaption = new StringBuilder(caption);
        }

        protected override void Canceling()
        {
            base.Canceling();
            m_screenResult = ResultEnum.CANCEL;
        }
        public override bool CloseScreen()
        {
            CallResultCallback(m_screenResult);
            return base.CloseScreen();
        }

        protected void CallResultCallback(ResultEnum result)
        {
            if (m_resultCallback != null) m_resultCallback(result);
        }
    }
}

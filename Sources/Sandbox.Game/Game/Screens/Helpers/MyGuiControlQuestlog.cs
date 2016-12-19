using Sandbox.Game.Gui;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using System;
using System;
using VRage.Audio;
using VRage.Game.ObjectBuilders.Gui;
using VRage.Utils;
using VRageMath;


namespace Sandbox.Game.Screens
{
    class MyGuiControlQuestlog : MyGuiControlBase
    {
        private static readonly float ANIMATION_PERIOD = 10;
        private static readonly int NUMER_OF_PERIODS = 3;
        private static readonly int CHARACTER_TYPING_FREQUENCY = 2;
        private IMySourceVoice m_currentSoundID;

        public MyHudQuestlog QuestInfo;

        private Vector2 m_position;
        private float m_currentFrame = float.MaxValue;
        private int m_timer = 0;
        private bool m_characterWasAdded = false;

        public MyGuiControlQuestlog(Vector2 position)
        {
            if (MyGuiManager.FullscreenHudEnabled)
                m_position = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(position);
            else
                m_position = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(position);
            Size = MyHud.Questlog.QuestlogSize;
            Position = m_position + this.Size / 2;
            BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_MESSAGEBOX_BACKGROUND_INFO.Texture);
            QuestInfo = MyHud.Questlog;
            VisibleChanged += VisibilityChanged;
            QuestInfo.ValueChanged += QuestInfo_ValueChanged;
        }

        void QuestInfo_ValueChanged()
        {
            Position = m_position + this.Size / 2;
            RecreateControls();
            if (QuestInfo.HighlightChanges)
            {
                m_currentFrame = 0;
            }
            else
            {
                m_currentFrame = float.MaxValue;
            }
        }

        public override void Update()
        {
            base.Update();
            m_timer++;
            if (m_timer % CHARACTER_TYPING_FREQUENCY == 0)
            {
                m_timer = 0;
                if (m_characterWasAdded)
                    RecreateControls();
            }
        }

        private void VisibilityChanged(object sender, bool isVisible)
        {
            if (Visible)
            {
                Position = m_position + this.Size / 2;
                RecreateControls();
                m_currentFrame = 0;
            }
            else
            {
                m_currentFrame = float.MaxValue;
                if (m_currentSoundID != null)
                {
                    m_currentSoundID.Stop();
                    m_currentSoundID = null;
                }

            }
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            if (m_currentFrame < NUMER_OF_PERIODS * ANIMATION_PERIOD && QuestInfo.HighlightChanges)
            {
                float ratio = (float)(2 * Math.PI * (m_currentFrame / ANIMATION_PERIOD));
                float visValue = ((float)Math.Cos(ratio) + 1.5f) * 0.5f;
                backgroundTransitionAlpha = MathHelper.Clamp(visValue, 0, 1);
                m_currentFrame++;
            }
            else if (m_currentFrame == NUMER_OF_PERIODS * ANIMATION_PERIOD &&m_currentSoundID!=null)
            {
                m_currentSoundID.Stop();
                m_currentSoundID = null;
            }
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }

        public void RecreateControls()
        {
            if (QuestInfo == null || Elements == null)
                return;
            Elements.Clear();

            Vector2 topleft = -this.Size / 2;
            Vector2 textOffset = new Vector2(0.015f, 0.015f);

            // Title
            MyGuiControlLabel title = new MyGuiControlLabel();
            title.Text = QuestInfo.QuestTitle;
            title.Position = topleft + textOffset;
            title.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            title.Visible = true;
            Elements.Add(title);

            // Pages
            if (QuestInfo.MaxPages > 1)
            {
                MyGuiControlLabel numbers = new MyGuiControlLabel();
                numbers.Text = QuestInfo.Page + "/" + QuestInfo.MaxPages;
                numbers.Position = topleft + Vector2.UnitX * this.Size - textOffset * new Vector2(1, -1);
                numbers.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
                Elements.Add(numbers);
            }

            // Separator
            MyGuiControlSeparatorList m_separator;
            m_separator = new MyGuiControlSeparatorList();
            m_separator.AddHorizontal(topleft + textOffset + new Vector2(0, 0.03f), this.Size.X - 2 * textOffset.X, 0.003f); // Title separator
            m_separator.Visible = true;
            Elements.Add(m_separator);

            // Details
            m_characterWasAdded = false;
            var rowOffset = new Vector2(0, 0.025f);
            MultilineData[] details = QuestInfo.GetQuestGetails();
            int idx = 0;
            for (int i = 0; i < details.Length; i++)
            {
                if (details[i] == null)
                    continue;

                if(details[i].charactersDisplayed < details[i].data.Length)
                {
                    if (m_characterWasAdded)
                        continue;
                    details[i].charactersDisplayed++;
                    m_characterWasAdded = true;
                }

                MyGuiControlMultilineText textBox = new MyGuiControlMultilineText(
                    size: new Vector2(Size.X * 0.92f, rowOffset.Y * 5),
                    position: topleft + textOffset + new Vector2(0, 0.04f) + rowOffset * idx,
                    textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    font: details[i].completed ? VRage.Game.MyFontEnum.Green : VRage.Game.MyFontEnum.Blue,
                    //Debug purpose
                    //backgroundColor: Vector4.One,
                    //backgroundTexture: BackgroundTexture,
                    drawScrollbar: false
                    );
                textBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                textBox.TextScale = 0.9f;
                textBox.AppendText((details[i].completed ? "• " : "") 
                    + ((details[i].charactersDisplayed < details[i].data.Length) ? details[i].data.Substring(0, details[i].charactersDisplayed) : details[i].data));
                textBox.Visible = true;
                idx += textBox.NumberOfRows;
                Elements.Add(textBox);
            }
        }

    }
}

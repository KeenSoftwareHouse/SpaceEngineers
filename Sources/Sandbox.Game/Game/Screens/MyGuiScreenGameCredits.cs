using Sandbox.Common;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

//  This class is called from gameplay screen a is only for drawing scrolling credits.

//  IMPORTANT: Don't forget that here we are using texts that didn't go through text resources so if some credits text contains
//  character that isn't in our font, it will fail. Therefore, if you use something obscure (not in ASCII), add it to font as a special character - AND TEST IT!!!


namespace Sandbox.Game.Gui
{
    public class MyGuiScreenGameCredits : MyGuiScreenBase
    {
        Color color = new Color(255, 255, 255, 220); //  Red 
        const float NUMBER_OF_SECONDS_TO_SCROLL_THROUGH_WHOLE_SCREEN = 30;
        private float m_movementSpeedMultiplier = 1.0f;
        float m_scrollingPositionY;
        string m_keenswhLogoTexture;
        float m_startTimeInMilliseconds;

        public MyGuiScreenGameCredits()
            : base(Vector2.Zero, null, null)
        {
            m_startTimeInMilliseconds = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            var align = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            var logoPanel = new MyGuiControlPanel(
                position: MyGuiManager.ComputeFullscreenGuiCoordinate(align, 54, 84),
                size: MyGuiConstants.TEXTURE_KEEN_LOGO.MinSizeGui,
                originAlign: align
                );
            logoPanel.BackgroundTexture = MyGuiConstants.TEXTURE_KEEN_LOGO;
            Controls.Add(logoPanel);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenGameCredits";
        }

        public override void LoadContent()
        {
            DrawMouseCursor = false;
            m_closeOnEsc = true;

            m_keenswhLogoTexture = "Textures\\GUI\\GameLogoLarge.dds";

            //  We will start scrolling from the bottom
            ResetScrollingPositionY();






            //  IMPORTANT: Base load content must be called after child's load content
            base.LoadContent();
        }

        void ResetScrollingPositionY(float offset = 0f)
        {
            //  We will start scrolling from the bottom
            m_scrollingPositionY = 0.99f + offset;
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            m_scrollingPositionY -= (1.0f / (NUMBER_OF_SECONDS_TO_SCROLL_THROUGH_WHOLE_SCREEN * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND)) * m_movementSpeedMultiplier;

            return true;
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Down))
                    m_movementSpeedMultiplier = 10.0f;
                else if (MyInput.Static.IsNewKeyReleased(MyKeys.Down))
                    m_movementSpeedMultiplier = 1.0f;

                if (MyInput.Static.IsNewKeyPressed(MyKeys.Up))
                    m_movementSpeedMultiplier = -10.0f;
                else if (MyInput.Static.IsNewKeyReleased(MyKeys.Up))
                    m_movementSpeedMultiplier = 1.0f;

            }
        }

        Color ChangeTextAlpha(Color origColor, float coordY)
        {
            float fadeEnd = 0.25f;
            float fadeStart = 0.3f;
            float alpha = MathHelper.Clamp((coordY - fadeEnd) / (fadeStart - fadeEnd), 0, 1);

            Color newColor = origColor;
            newColor.A = (byte)(origColor.A * alpha);

            return newColor;
        }

        public Vector2 GetScreenLeftTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }
        
        public override bool Draw()
        {
            if (!base.Draw()) return false;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Credits
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            float movingY = m_scrollingPositionY;

            var font = MyFontEnum.GameCredits;

            for (int i = 0; i < MyPerGameSettings.Credits.Departments.Count; i++)
            {
                MyGuiManager.DrawString(font, MyPerGameSettings.Credits.Departments[i].Name,
                                        new Vector2(0.5f, movingY), 0.78f,
                                        ChangeTextAlpha(color, movingY),
                                        MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                movingY += 0.05f;

                for (int j = 0; j < MyPerGameSettings.Credits.Departments[i].Persons.Count; j++)
                {
                    MyGuiManager.DrawString(font, MyPerGameSettings.Credits.Departments[i].Persons[j].Name,
                                            new Vector2(0.5f, movingY), 1.04f,
                                            ChangeTextAlpha(color, movingY),
                                            MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    movingY += 0.05f;
                }

                movingY += 0.04f;
            }

            movingY += 0.05f;

            for (int i = 0; i < MyPerGameSettings.Credits.CreditNotices.Count; i++)
            {
                var notice = MyPerGameSettings.Credits.CreditNotices[i];
                if (notice.LogoTexture != null)
                {
                    if (notice.LogoNormalizedSize.HasValue)
                    {
                        MyGuiManager.DrawSpriteBatch(
                            notice.LogoTexture,
                            new Vector2(0.5f, movingY),
                            notice.LogoNormalizedSize.Value,
                            ChangeTextAlpha(color, movingY),
                            MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    }
                    else if (notice.LogoScale.HasValue)
                    {
                        MyGuiManager.DrawSpriteBatch(
                            notice.LogoTexture,
                            new Vector2(0.5f, movingY),
                            notice.LogoScale.Value,
                            ChangeTextAlpha(color, movingY),
                            MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                            0.0f, Vector2.Zero);
                    }
                    else
                        throw new InvalidBranchException();
                    movingY += notice.LogoOffset;
                }

                for (int j = 0; j < notice.CreditNoticeLines.Count; j++)
                {
                    MyGuiManager.DrawString(font, notice.CreditNoticeLines[j],
                                            new Vector2(0.5f, movingY), 0.78f,
                                            ChangeTextAlpha(color, movingY),
                                            MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    movingY += 0.025f;
                }

                movingY += 0.15f;
            }

            //  This will start scrolling again after last word was scrolled through the top
            if (movingY <= 0) ResetScrollingPositionY();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Company Logo - with shadow (thus drawing two times)
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            MyGuiSandbox.DrawGameLogo(m_transitionAlpha);

            return true;
        }
    }
}
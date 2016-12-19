using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlContentButton : MyGuiControlRadioButton
    {
        #region Locals

        private readonly MyGuiControlLabel m_titleLabel;
        private readonly MyGuiControlImage m_previewImage;


        private readonly MyGuiControlImage m_workshopIconNormal;
        private readonly MyGuiControlImage m_workshopIconHighlight;

        private readonly MyGuiControlImage m_localmodIconNormal;
        private readonly MyGuiControlImage m_localmodIconHighlight;

        private bool m_isWorkshopMod;
        private bool m_isLocalMod;

        #endregion

        #region Accessors

        /// <summary>
        /// Changes titlebar content.
        /// </summary>
        public string Title
        {
            get { return m_titleLabel.Text; }
        }

        /// <summary>
        /// Adds respective icon and disables IsLocalMod.
        /// </summary>
        public bool IsWorkshopMod
        {
            get { return m_isWorkshopMod; }
            set
            {
                // Do nothing if called from constructor
                if (m_workshopIconNormal == null) return;
                if (value)
                {
                    Elements.Add(HasHighlight ? m_workshopIconHighlight : m_workshopIconNormal);
                    if (IsLocalMod)
                        IsLocalMod = false;
                }
                else
                {
                    Elements.Remove(m_workshopIconNormal);
                    Elements.Remove(m_workshopIconHighlight);
                }

                m_isWorkshopMod = value;
            }
        }

        /// <summary>
        /// Adds respective icon and disables IsWorkshopMod.
        /// </summary>
        public bool IsLocalMod
        {
            get { return m_isLocalMod; }
            set
            {
                // Do nothing if called from constructor
                if (m_localmodIconNormal == null) return;
                if (value)
                {
                    Elements.Add(HasHighlight ? m_localmodIconHighlight : m_localmodIconNormal);
                    if (IsWorkshopMod)
                        IsWorkshopMod = false;
                }
                else
                {
                    Elements.Remove(m_localmodIconNormal);
                    Elements.Remove(m_localmodIconHighlight);
                }

                m_isLocalMod = value;
            }
        }

        #endregion

        #region Constructor

        public MyGuiControlContentButton(string title, string imagePath)
        {
            IsWorkshopMod = false;
            IsLocalMod = false;
            VisualStyle = MyGuiControlRadioButtonStyleEnum.ScenarioButton;
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            m_titleLabel = new MyGuiControlLabel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Text = title
            };

            m_previewImage = new MyGuiControlImage(textures: new [] {imagePath})
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            m_workshopIconNormal = new MyGuiControlImage(textures: new[] { MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.Normal })
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                Size = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.SizeGui
            };

            m_workshopIconHighlight = new MyGuiControlImage(textures: new[] { MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.Highlight })
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                Size = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.SizeGui
            };

            m_localmodIconNormal = new MyGuiControlImage(textures: new[] { MyGuiConstants.TEXTURE_ICON_MODS_LOCAL.Normal })
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                Size = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.SizeGui
            };

            m_localmodIconHighlight = new MyGuiControlImage(textures: new[] { MyGuiConstants.TEXTURE_ICON_MODS_LOCAL.Highlight })
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                Size = MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP.SizeGui
            };

            m_previewImage.Size = new Vector2(242f, 128f)/MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_previewImage.BorderEnabled = true;
            m_previewImage.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();

            Size = new Vector2(m_previewImage.Size.X,
                m_titleLabel.Size.Y + m_previewImage.Size.Y);

            Elements.Add(m_titleLabel);
            Elements.Add(m_previewImage);
        }

        #endregion


        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            m_titleLabel.Position = Size * -0.5f;
            m_previewImage.Position = m_titleLabel.Position + new Vector2(0f, m_titleLabel.Size.Y);
            m_workshopIconNormal.Position = Size * 0.5f - new Vector2(0.001f, 0.002f);
            m_workshopIconHighlight.Position = Size * 0.5f - new Vector2(0.001f, 0.002f);
            m_localmodIconNormal.Position = Size * 0.5f - new Vector2(0.001f, 0.002f);
            m_localmodIconHighlight.Position = Size * 0.5f - new Vector2(0.001f, 0.002f);
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            if (HasHighlight)
            {
                m_titleLabel.Font = MyFontEnum.White;
                m_previewImage.BorderColor = Vector4.One;
                if (IsWorkshopMod)
                {
                    Elements.Remove(m_workshopIconNormal);
                    Elements.Add(m_workshopIconHighlight);
                }
                else if (IsLocalMod)
                {
                    Elements.Remove(m_localmodIconNormal);
                    Elements.Add(m_localmodIconHighlight);
                }
            }
            else
            {
                m_titleLabel.Font = MyFontEnum.Blue;
                m_previewImage.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();
                if (IsWorkshopMod)
                {
                    Elements.Remove(m_workshopIconHighlight);
                    Elements.Add(m_workshopIconNormal);
                }
                else if (IsLocalMod)
                {
                    Elements.Remove(m_localmodIconHighlight);
                    Elements.Add(m_localmodIconNormal);
                }
            }
        }
    }
}

using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    // TODO: Heavy refactoring, split to proper controls
    public class MyGuiControlBlockInfo : MyGuiControlBase
    {
        public static bool ShowComponentProgress = true;
        public static bool ShowCriticalComponent = false;
        public static bool ShowCriticalIntegrity = true;
        public static bool ShowOwnershipIntegrity = MyFakes.SHOW_FACTIONS_GUI;

        public static Vector4 CriticalIntegrityColor = Color.Red.ToVector4();
        public static Vector4 CriticalComponentColor = CriticalIntegrityColor * new Vector4(1, 1, 1, 0.7f);
        public static Vector4 OwnershipIntegrityColor = Color.Blue.ToVector4();

		public struct MyControlBlockInfoStyle
		{
            public string BlockNameLabelFont;
			public MyStringId ComponentsLabelText;
            public string ComponentsLabelFont;
			public MyStringId InstalledRequiredLabelText;
            public string InstalledRequiredLabelFont;
            public MyStringId RequiredAvailableLabelText;
			public MyStringId RequiredLabelText;
            public string IntegrityLabelFont;
			public Vector4 IntegrityBackgroundColor;
			public Vector4 IntegrityForegroundColor;
			public Vector4 IntegrityForegroundColorOverCritical;
			public Vector4 LeftColumnBackgroundColor;
			public Vector4 TitleBackgroundColor;
            public string ComponentLineMissingFont;
            public string ComponentLineAllMountedFont;
            public string ComponentLineAllInstalledFont;
            public string ComponentLineDefaultFont;
			public Vector4 ComponentLineDefaultColor;
			public bool EnableBlockTypeLabel;
			public bool ShowAvailableComponents;
			public bool EnableBlockTypePanel;
		}

        class ComponentLineControl : MyGuiControlBase
        {
            //public MyGuiControlPanel IconPanel;
            //public MyGuiControlPanel IconPanelBackground;
            public MyGuiControlImage IconImage;
            public MyGuiControlPanel IconPanelProgress;
            public MyGuiControlLabel NameLabel;
            public MyGuiControlLabel NumbersLabel;

            public ComponentLineControl(Vector2 size, float iconSize)
                : base(size: size, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                var m_iconSize = new Vector2(iconSize) * new Vector2(0.75f, 1);
                var middleLeft = new Vector2(-this.Size.X / 2, 0);
                var middleRight = new Vector2(this.Size.X / 2, 0);
                var iconPos = middleLeft - new Vector2(0, m_iconSize.Y / 2);

                //IconPanel = new MyGuiControlPanel();
                //IconPanelBackground = new MyGuiControlPanel();
                IconImage = new MyGuiControlImage();
                IconPanelProgress = new MyGuiControlPanel();
                NameLabel = new MyGuiControlLabel(text: String.Empty);
                NumbersLabel = new MyGuiControlLabel(text: String.Empty);
                
                //IconPanel.Size = m_iconSize;
                //IconPanel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                //IconPanel.Position = iconPos;

                //IconPanelBackground.Size = m_iconSize;
                //IconPanelBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                //IconPanelBackground.Position = iconPos;
                //IconPanelBackground.BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT.Texture);

                IconImage.Size = m_iconSize;
                IconImage.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                IconImage.Position = iconPos;
                IconImage.BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT.Texture);

                IconPanelProgress.Size = m_iconSize;
                IconPanelProgress.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                IconPanelProgress.Position = iconPos;
                IconPanelProgress.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;
                float gray = 0.1f;
                IconPanelProgress.ColorMask = new Vector4(gray, gray, gray, 0.5f);

                NameLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                NameLabel.Position = middleLeft + new Vector2(m_iconSize.X + 0.01225f, 0);

                NameLabel.AutoEllipsis = true;

                NumbersLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
                NumbersLabel.Position = middleRight + new Vector2(-0.033f, 0); //topRight + new Vector2(-0.02f, 0.004f);

                //Elements.Add(IconPanelBackground);
                //Elements.Add(IconPanel);
                Elements.Add(IconImage);
                Elements.Add(IconPanelProgress);
                Elements.Add(NameLabel);
                Elements.Add(NumbersLabel);
            }

            public void RecalcTextSize()
            {
                float numberLeft = NumbersLabel.Position.X - NumbersLabel.GetTextSize().X;
                float minimumGapSize = 0.01f;

                NameLabel.Size = new Vector2(numberLeft - NameLabel.Position.X - minimumGapSize, NameLabel.Size.Y);
            }

            public void SetProgress(float val)
            {
                //IconPanelProgress.Size = IconPanel.Size * new Vector2(1, 1 - val);
                IconPanelProgress.Size = IconImage.Size * new Vector2(1, 1 - val);
            }
        }

        MyGuiControlLabel m_blockTypeLabel;
        MyGuiControlLabel m_blockNameLabel;
        MyGuiControlLabel m_componentsLabel;
        MyGuiControlLabel m_installedRequiredLabel;
        MyGuiControlLabel m_integrityLabel;
        MyGuiControlLabel m_blockBuiltByLabel;

        //MyGuiControlPanel m_blockIconPanel;
        //MyGuiControlPanel m_blockIconPanelBackground;
        MyGuiControlImage m_blockIconImage;

        MyGuiControlPanel m_blockTypePanel;
        MyGuiControlPanel m_blockTypePanelBackground;

        MyGuiControlPanel m_titleBackground;
        MyGuiControlPanel m_leftColumnBackground;
        MyGuiControlPanel m_integrityBackground;
        MyGuiControlPanel m_integrityForeground;

        MyGuiControlPanel m_integrityCriticalLine;

        MyGuiControlSeparatorList m_separator;

        List<ComponentLineControl> m_componentLines = new List<ComponentLineControl>(15);

        public MyHudBlockInfo BlockInfo;
        private bool m_progressMode;
		private MyControlBlockInfoStyle m_style;

        float m_smallerFontSize = 0.83f;

        private float baseScale { get { return m_progressMode ? 1.0f : 0.83f; } }
        private float itemHeight { get { return 0.037f * baseScale; } }

		public MyGuiControlBlockInfo(MyControlBlockInfoStyle style, bool progressMode = true, bool largeBlockInfo = true)
			: base(backgroundTexture: new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_LARGE_DEFAULT.Texture))
		{
			m_style = style;
			m_progressMode = progressMode;

			m_leftColumnBackground = new MyGuiControlPanel(backgroundColor: Color.Red.ToVector4());
			m_leftColumnBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
			Elements.Add(m_leftColumnBackground);

			m_titleBackground = new MyGuiControlPanel(backgroundColor: Color.Red.ToVector4());
			m_titleBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
			Elements.Add(m_titleBackground);

			if (m_progressMode)
			{
				m_integrityBackground = new MyGuiControlPanel(backgroundColor: Color.Red.ToVector4());
				m_integrityBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
				Elements.Add(m_integrityBackground);

				m_integrityForeground = new MyGuiControlPanel(backgroundColor: Color.Red.ToVector4());
				m_integrityForeground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
				Elements.Add(m_integrityForeground);

				m_integrityCriticalLine = new MyGuiControlPanel(backgroundColor: Color.Red.ToVector4());
				m_integrityCriticalLine.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
				Elements.Add(m_integrityCriticalLine);
			}

            //m_blockIconPanelBackground = new MyGuiControlPanel();
            //m_blockIconPanelBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            //m_blockIconPanelBackground.BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT.Texture);
            //m_blockIconPanelBackground.Size = m_progressMode ? new Vector2(0.088f) : new Vector2(0.04f);
            //m_blockIconPanelBackground.Size *= new Vector2(0.75f, 1);
            //Elements.Add(m_blockIconPanelBackground);

            //m_blockIconPanel = new MyGuiControlPanel();
            //m_blockIconPanel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            //m_blockIconPanel.Size = m_progressMode ? new Vector2(0.088f) : new Vector2(0.04f);
            //m_blockIconPanel.Size *= new Vector2(0.75f, 1);
            //Elements.Add(m_blockIconPanel);

            m_blockIconImage = new MyGuiControlImage();
            m_blockIconImage.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_blockIconImage.BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT.Texture);
            m_blockIconImage.Size = m_progressMode ? new Vector2(0.088f) : new Vector2(0.04f);
            m_blockIconImage.Size *= new Vector2(0.75f, 1);
            Elements.Add(m_blockIconImage);

			m_blockTypePanelBackground = new MyGuiControlPanel();
			m_blockTypePanelBackground.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
			m_blockTypePanelBackground.BackgroundTexture = new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_DEFAULT.Texture);
			m_blockTypePanelBackground.Size = m_progressMode ? new Vector2(0.088f) : new Vector2(0.04f);
			m_blockTypePanelBackground.Size *= new Vector2(0.75f, 1);
			Elements.Add(m_blockTypePanelBackground);

			m_blockTypePanel = new MyGuiControlPanel();
			m_blockTypePanel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
			m_blockTypePanel.Size = m_progressMode ? new Vector2(0.088f) : new Vector2(0.04f);
			m_blockTypePanel.Size *= new Vector2(0.75f, 1);
			m_blockTypePanel.BackgroundTexture = new MyGuiCompositeTexture(largeBlockInfo ? @"Textures\GUI\Icons\Cubes\LargeBlock.dds" : @"Textures\GUI\Icons\Cubes\SmallBlock.dds");
			Elements.Add(m_blockTypePanel);

			m_blockNameLabel = new MyGuiControlLabel(text: String.Empty);
			m_blockNameLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
			m_blockNameLabel.TextScale = 1 * baseScale;
			m_blockNameLabel.Font = m_style.BlockNameLabelFont;
			m_blockNameLabel.AutoEllipsis = true;
			Elements.Add(m_blockNameLabel);

			String blockTypeLabelText = String.Empty;
			if (style.EnableBlockTypeLabel)
				blockTypeLabelText = MyTexts.GetString(largeBlockInfo ? MySpaceTexts.HudBlockInfo_LargeShip_Station : MySpaceTexts.HudBlockInfo_SmallShip);
			m_blockTypeLabel = new MyGuiControlLabel(text: blockTypeLabelText);
			m_blockTypeLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
			m_blockTypeLabel.TextScale = 1 * baseScale;
			m_blockTypeLabel.Font = MyFontEnum.White;
			Elements.Add(m_blockTypeLabel);

			m_componentsLabel = new MyGuiControlLabel(text: MyTexts.GetString(m_style.ComponentsLabelText));
			m_componentsLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
			m_componentsLabel.TextScale = m_smallerFontSize * baseScale;
			m_componentsLabel.Font = m_style.ComponentsLabelFont;
			Elements.Add(m_componentsLabel);

			m_installedRequiredLabel = new MyGuiControlLabel(text: MyTexts.GetString(m_style.InstalledRequiredLabelText));
			m_installedRequiredLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
			m_installedRequiredLabel.TextScale = m_smallerFontSize * baseScale;
			m_installedRequiredLabel.Font = m_style.InstalledRequiredLabelFont;
			Elements.Add(m_installedRequiredLabel);

            m_blockBuiltByLabel = new MyGuiControlLabel(text: String.Empty);
            m_blockBuiltByLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            m_blockBuiltByLabel.TextScale = m_smallerFontSize * baseScale;
            m_blockBuiltByLabel.Font = m_style.InstalledRequiredLabelFont;
            Elements.Add(m_blockBuiltByLabel);

			if (m_progressMode)
			{
				m_integrityLabel = new MyGuiControlLabel(text: String.Empty);
				m_integrityLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;
				m_integrityLabel.Font = m_style.IntegrityLabelFont;
				m_integrityLabel.TextScale = 0.75f * baseScale;
				Elements.Add(m_integrityLabel);
			}

			m_separator = new MyGuiControlSeparatorList();
			Elements.Add(m_separator);

			EnsureLineControls(m_componentLines.Capacity);
			Size = m_progressMode ? new Vector2(0.325f, 0.4f) : new Vector2(0.22f, 0.4f);
		}

        void EnsureLineControls(int count)
        {
            while (m_componentLines.Count < count)
            {
                var itemSize = m_progressMode ? new Vector2(0.3f, 0.05f) : new Vector2(0.235f, 0.05f);

                var item = new ComponentLineControl(itemSize * new Vector2(1, baseScale), 0.035f * baseScale);
                m_componentLines.Add(item);
                Elements.Add(item);
            }
        }

        public void RecalculateSize()
        {
            this.Size = new Vector2(this.Size.X, 0.12f * baseScale + itemHeight * BlockInfo.Components.Count);
        }

        void Reposition()
        {
            RecalculateSize();

            //BackgroundTexture =  @"Textures\GUI\Screens\aa";
            var topleft = -this.Size / 2;
            var topRight = new Vector2(this.Size.X / 2, -this.Size.Y / 2);
            var rightColumn = topleft + (m_progressMode ? new Vector2(0.0815f, 0) : new Vector2(0.036f, 0));

            var titleHeight = 0.072f * baseScale;

            Vector2 borderGap = new Vector2(0.0035f) * new Vector2(0.75f, 1) * baseScale;
            if (!m_progressMode)
                borderGap.Y *= 1.0f;

            if (BlockInfo.BlockIntegrity > 0)
            {
                m_installedRequiredLabel.TextToDraw = MyTexts.Get(m_style.InstalledRequiredLabelText);
            }
            else if (BlockInfo.ShowAvailable)
            {
                m_installedRequiredLabel.TextToDraw = MyTexts.Get(m_style.RequiredAvailableLabelText);
            }
            else
            {
                m_installedRequiredLabel.TextToDraw = MyTexts.Get(m_style.RequiredLabelText);
            }

            m_leftColumnBackground.ColorMask = m_style.LeftColumnBackgroundColor;
            m_leftColumnBackground.Position = topleft + borderGap;
            m_leftColumnBackground.Size = new Vector2(rightColumn.X - topleft.X, this.Size.Y) - new Vector2(borderGap.X, 0.0088f);
            m_leftColumnBackground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

            m_titleBackground.ColorMask = m_style.TitleBackgroundColor;
            if (m_progressMode)
            {
                m_titleBackground.Position = rightColumn + new Vector2(-0.0015f, borderGap.Y);
            }
            else
            {
                m_titleBackground.Position = topleft + borderGap;
            }
            m_titleBackground.Size = new Vector2(topRight.X - m_titleBackground.Position.X - borderGap.X, 0.100f * baseScale);
            m_titleBackground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

            Vector2 separatorPos;
            if (m_progressMode)
            {
                separatorPos = rightColumn + new Vector2(0, titleHeight);
            }
            else
            {
                separatorPos = topleft + new Vector2(borderGap.X, titleHeight);
            }
            m_separator.Clear();
            m_separator.AddHorizontal(separatorPos, this.Size.X + topleft.X - separatorPos.X - 0.002f, 0.003f); // Title separator

            if (m_progressMode)
            {
                if (BlockInfo.BlockIntegrity > 0)
                {
                    float integrityHeight = itemHeight * BlockInfo.Components.Count - 0.002f;
                    float integrityWidth = 0.032f;
                    var integrityPos = topleft + new Vector2(0.01f, 0.11f + integrityHeight);

                    m_integrityBackground.ColorMask = m_style.IntegrityBackgroundColor;
                    m_integrityBackground.Position = integrityPos;
                    m_integrityBackground.Size = new Vector2(integrityWidth, integrityHeight);
                    m_integrityBackground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

                    var color = (BlockInfo.BlockIntegrity > BlockInfo.CriticalIntegrity)
                        ? m_style.IntegrityForegroundColorOverCritical
                        : m_style.IntegrityForegroundColor;

                    m_integrityForeground.ColorMask = color;
                    m_integrityForeground.Position = integrityPos;
                    m_integrityForeground.Size = new Vector2(integrityWidth, integrityHeight * BlockInfo.BlockIntegrity);
                    m_integrityForeground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

                    if (ShowCriticalIntegrity)
                    {
                        float lineWidth = 0;
                        if (Math.Abs(BlockInfo.CriticalIntegrity - BlockInfo.OwnershipIntegrity) < 0.005f)
                            lineWidth = 0.004f; //if lines are overdrawing

                        m_separator.AddHorizontal(integrityPos - new Vector2(0, integrityHeight * BlockInfo.CriticalIntegrity), integrityWidth, width: lineWidth, color: CriticalIntegrityColor);
                    }

                    if (ShowOwnershipIntegrity && BlockInfo.OwnershipIntegrity > 0)
                    {
                        m_separator.AddHorizontal(integrityPos - new Vector2(0, integrityHeight * BlockInfo.OwnershipIntegrity), integrityWidth, color: OwnershipIntegrityColor);
                    }

                    m_integrityLabel.Position = integrityPos + new Vector2(integrityWidth / 2, -0.005f);
                    m_integrityLabel.Font = MyFontEnum.White;
                    m_integrityLabel.TextToDraw.Clear();
                    m_integrityLabel.TextToDraw.AppendInt32((int)Math.Floor(BlockInfo.BlockIntegrity * 100)).Append("%");

                    m_integrityBackground.Visible = true;
                    m_integrityForeground.Visible = true;
                    m_integrityLabel.Visible = true;
                }
                else
                {
                    m_integrityBackground.Visible = false;
                    m_integrityForeground.Visible = false;
                    m_integrityLabel.Visible = false;
                }
            }

            if (m_progressMode)
            {
                m_blockNameLabel.Position = rightColumn + new Vector2(0.006f, 0.026f * baseScale);

				if (m_style.ShowAvailableComponents)
				{
					Vector2 offset = new Vector2(0, -0.006f);
					m_componentsLabel.Position = m_blockNameLabel.Position + new Vector2(0, m_blockNameLabel.Size.Y) + offset;
					m_blockNameLabel.Position = m_blockNameLabel.Position + offset;
				}
				else
					m_componentsLabel.Position = rightColumn + new Vector2(0.006f, 0.076f * baseScale);

                m_blockNameLabel.Size = new Vector2(Size.X / 2 - m_blockNameLabel.Position.X, m_blockNameLabel.Size.Y);
                m_blockTypeLabel.Visible = false;
                m_blockTypePanel.Visible = false;
                m_blockTypePanelBackground.Visible = false;
            }
            else
            {
				m_blockTypePanel.Position = topRight + new Vector2(-0.0085f, 0.012f);
				m_blockTypePanelBackground.Position = topRight + new Vector2(-0.0085f, 0.012f);
				if (m_style.EnableBlockTypePanel)
				{
					m_blockTypePanel.Visible = true;
					m_blockTypePanelBackground.Visible = true;
					
					m_blockNameLabel.Size = new Vector2(m_blockTypePanel.Position.X - m_blockTypePanel.Size.X - m_blockNameLabel.Position.X, m_blockNameLabel.Size.Y);
				}
				else
				{
					m_blockTypePanel.Visible = false;
					m_blockTypePanelBackground.Visible = false;

					m_blockNameLabel.Size = new Vector2(m_blockTypePanel.Position.X - m_blockNameLabel.Position.X, m_blockNameLabel.Size.Y);
				}

                m_blockNameLabel.TextScale = 0.95f * baseScale;
                m_blockNameLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                //m_blockNameLabel.Position = m_blockIconPanel.Position + m_blockIconPanel.Size + new Vector2(0.004f, 0);
                m_blockNameLabel.Position = m_blockIconImage.Position + m_blockIconImage.Size + new Vector2(0.004f, 0);
                if (!m_style.EnableBlockTypeLabel)
                {
                    m_blockNameLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                    m_blockNameLabel.Position -= new Vector2(0f, m_blockIconImage.Size.Y * 0.5f);
                }


                m_blockTypeLabel.Visible = true;
                m_blockTypeLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                m_blockTypeLabel.TextScale = m_smallerFontSize * baseScale;
                //m_blockTypeLabel.Position = m_blockIconPanel.Position + new Vector2(m_blockIconPanel.Size.X, 0) + new Vector2(0.004f, -0.0025f);
                m_blockTypeLabel.Position = m_blockIconImage.Position + new Vector2(m_blockIconImage.Size.X, 0) + new Vector2(0.004f, -0.0025f);

				m_componentsLabel.Position = rightColumn + new Vector2(0.006f, 0.076f * baseScale);
            }

            m_installedRequiredLabel.Position = topRight + new Vector2(-0.011f, 0.076f * baseScale);
            m_blockBuiltByLabel.Position = rightColumn + new Vector2(0.006f, 0.07f * baseScale);
            m_blockBuiltByLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            m_blockBuiltByLabel.TextScale = 0.6f;

            //m_blockIconPanel.Position = topleft + new Vector2(0.0085f, 0.012f);
            //m_blockIconPanelBackground.Position = topleft + new Vector2(0.0085f, 0.012f);
            m_blockIconImage.Position = topleft + new Vector2(0.0085f, 0.012f);

            Vector2 listPos;
            if (m_progressMode)
                listPos = topleft + new Vector2(0.0485f, 0.102f);
            else
                listPos = topleft + new Vector2(0.008f, 0.102f * baseScale);

            for (int i = 0; i < BlockInfo.Components.Count; i++)
            {
                m_componentLines[i].Position = listPos + new Vector2(0, (BlockInfo.Components.Count - i - 1) * itemHeight);
                m_componentLines[i].IconPanelProgress.Visible = ShowComponentProgress;
                //m_componentLines[i].IconPanel.BorderColor = CriticalComponentColor;
                m_componentLines[i].IconImage.BorderColor = CriticalComponentColor;
                m_componentLines[i].NameLabel.TextScale = m_smallerFontSize * baseScale;
                m_componentLines[i].NumbersLabel.TextScale = m_smallerFontSize * baseScale;
            }
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            if (BlockInfo != null)
            {              
                EnsureLineControls(BlockInfo.Components.Count);

                Reposition();

                for (int i = 0; i < m_componentLines.Count; i++)
                {
                    if (i < BlockInfo.Components.Count)
                    {
                        var info = BlockInfo.Components[i];
                        string font;
                        Vector4 color = Vector4.One;

                        if (m_progressMode && BlockInfo.BlockIntegrity > 0)
                        {
                            if (BlockInfo.MissingComponentIndex == i)
                                font = m_style.ComponentLineMissingFont;
                            else if (info.MountedCount == info.TotalCount)
                                font = m_style.ComponentLineAllMountedFont;
                            else if (info.InstalledCount == info.TotalCount)
                                font = m_style.ComponentLineAllInstalledFont;
                            else
                            {
                                font = m_style.ComponentLineDefaultFont;
								color = m_style.ComponentLineDefaultColor;
                            }
                        }
                        else
                        {
							font = m_style.ComponentLineDefaultFont;
                        }

                        if (m_progressMode && BlockInfo.BlockIntegrity > 0)
                            m_componentLines[i].SetProgress(info.MountedCount / (float)info.TotalCount);
                        else
                            m_componentLines[i].SetProgress(1);
                        m_componentLines[i].Visible = true;
                        m_componentLines[i].NameLabel.Font = font;
                        m_componentLines[i].NameLabel.ColorMask = color;
                        m_componentLines[i].NameLabel.TextToDraw.Clear();
                        m_componentLines[i].NameLabel.TextToDraw.Append(info.ComponentName);
                        //m_componentLines[i].IconPanel.BackgroundTexture = new MyGuiCompositeTexture(info.Icons[0]);
                        m_componentLines[i].IconImage.SetTextures(info.Icons);
                        m_componentLines[i].NumbersLabel.Font = font;
                        m_componentLines[i].NumbersLabel.ColorMask = color;
                        m_componentLines[i].NumbersLabel.TextToDraw.Clear();
						if (m_progressMode && BlockInfo.BlockIntegrity > 0)
						{
							m_componentLines[i].NumbersLabel.TextToDraw.AppendInt32(info.InstalledCount).Append(" / ").AppendInt32(info.TotalCount);
							if (m_style.ShowAvailableComponents)
								m_componentLines[i].NumbersLabel.TextToDraw.Append(" / ").AppendInt32(info.AvailableAmount);
						}
                        else if (BlockInfo.ShowAvailable)
						{
                            m_componentLines[i].NumbersLabel.TextToDraw.AppendInt32(info.TotalCount);
                            if (m_style.ShowAvailableComponents)
                                m_componentLines[i].NumbersLabel.TextToDraw.Append(" / ").AppendInt32(info.AvailableAmount);
						}
						else
						{
							m_componentLines[i].NumbersLabel.TextToDraw.AppendInt32(info.TotalCount);
						}
                        m_componentLines[i].NumbersLabel.Size = m_componentLines[i].NumbersLabel.GetTextSize();
                        //m_componentLines[i].IconPanel.BorderEnabled = ShowCriticalComponent && BlockInfo.CriticalComponentIndex == i;
                        m_componentLines[i].IconImage.BorderEnabled = ShowCriticalComponent && BlockInfo.CriticalComponentIndex == i;
                        m_componentLines[i].RecalcTextSize();
                    }
                    else
                    {
                        m_componentLines[i].Visible = false;
                    }
                }

                m_blockNameLabel.TextToDraw.Clear();
                if (BlockInfo.BlockName != null)
                    m_blockNameLabel.TextToDraw.Append(BlockInfo.BlockName);
                m_blockNameLabel.TextToDraw.ToUpper();

                m_blockBuiltByLabel.TextToDraw.Clear();
                var identity = MySession.Static.Players.TryGetIdentity(BlockInfo.BlockBuiltBy);
                if (identity != null)
                {
                    m_blockBuiltByLabel.TextToDraw.Append(MyTexts.GetString(MyCommonTexts.BuiltBy));
                    m_blockBuiltByLabel.TextToDraw.Append(": ");
                    m_blockBuiltByLabel.TextToDraw.Append(identity.DisplayName);
                }

                //m_blockIconPanel.BackgroundTexture = new MyGuiCompositeTexture(BlockInfo.BlockIcons[0]);
                m_blockIconImage.SetTextures(BlockInfo.BlockIcons);

                Reposition();

                if (BlockInfo.Components.Count == 0)
                {
                    m_separator.Visible = false;
                    m_installedRequiredLabel.Visible = false;
                    m_componentsLabel.Visible = false;
                }
                else
                {
                    m_separator.Visible = true;
                    m_installedRequiredLabel.Visible = true;
                    m_componentsLabel.Visible = true;
                }
            }

            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }
    }
}

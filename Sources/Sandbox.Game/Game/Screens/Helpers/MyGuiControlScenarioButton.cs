using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using VRage;
using VRageMath;
using Sandbox.Graphics;
using Sandbox.Definitions;
using Sandbox.Common;
using VRage.Game;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlScenarioButton : MyGuiControlRadioButton
    {
        private MyGuiControlLabel m_titleLabel;
        private MyGuiControlPanel m_previewPanel;

        public string Title
        {
            get { return m_titleLabel.Text.ToString(); }
        }

        public MyScenarioDefinition Scenario
        {
            get;
            private set;
        }

        public MyGuiControlScenarioButton(MyScenarioDefinition scenario):
            base(key: MyDefinitionManager.Static.GetScenarioDefinitions().IndexOf(scenario))
        {
            VisualStyle = MyGuiControlRadioButtonStyleEnum.ScenarioButton;
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            Scenario = scenario;
            m_titleLabel = new MyGuiControlLabel(text: scenario.DisplayNameText, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            m_previewPanel = new MyGuiControlPanel(texture: scenario.Icon, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            MyGuiSizedTexture image = new MyGuiSizedTexture() { SizePx = new Vector2(229f, 128f), };
            m_previewPanel.Size = image.SizeGui;
            m_previewPanel.BorderEnabled = true;
            m_previewPanel.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();
            SetToolTip(scenario.DescriptionText);
            Size = new Vector2(Math.Max(m_titleLabel.Size.X, m_previewPanel.Size.X),
                               m_titleLabel.Size.Y + m_previewPanel.Size.Y);
            Elements.Add(m_titleLabel);
            Elements.Add(m_previewPanel);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            m_titleLabel.Position = Size * -0.5f;
            m_previewPanel.Position = m_titleLabel.Position + new Vector2(0f, m_titleLabel.Size.Y);
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            if (HasHighlight)
            {
                m_titleLabel.Font = MyFontEnum.White;
                m_previewPanel.BorderColor = Vector4.One;
            }
            else
            {
                m_titleLabel.Font = MyFontEnum.Blue;
                m_previewPanel.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();
            }
        }
    }
}

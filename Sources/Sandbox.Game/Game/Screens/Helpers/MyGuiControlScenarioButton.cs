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
        private MyGuiControlImage m_previewImage;

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
            m_previewImage = new MyGuiControlImage(textures: scenario.Icons, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            MyGuiSizedTexture image = new MyGuiSizedTexture() { SizePx = new Vector2(229f, 128f), };
            m_previewImage.Size = image.SizeGui;
            m_previewImage.BorderEnabled = true;
            m_previewImage.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();
            SetToolTip(scenario.DescriptionText);
            Size = new Vector2(Math.Max(m_titleLabel.Size.X, m_previewImage.Size.X),
                               m_titleLabel.Size.Y + m_previewImage.Size.Y);
            Elements.Add(m_titleLabel);
            Elements.Add(m_previewImage);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            m_titleLabel.Position = Size * -0.5f;
            m_previewImage.Position = m_titleLabel.Position + new Vector2(0f, m_titleLabel.Size.Y);
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            if (HasHighlight)
            {
                m_titleLabel.Font = MyFontEnum.White;
                m_previewImage.BorderColor = Vector4.One;
            }
            else
            {
                m_titleLabel.Font = MyFontEnum.Blue;
                m_previewImage.BorderColor = MyGuiConstants.THEMED_GUI_LINE_COLOR.ToVector4();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Gui;
using Sandbox;
using VRage;
using VRageMath;

namespace SpaceEngineers.Game.GUI
{
    class MyGuiScreenPerformanceWarnings : MyGuiScreenBase
    {
        /// <summary>
        /// Each of the performance problems on the screen
        /// </summary>
        internal class WarningLine
        {
            static StringBuilder m_tmpTruncated = new StringBuilder();
            public MySimpleProfiler.PerformanceWarning Warning;
            MyGuiControlLabel m_name;
            MyGuiControlLabel m_description;
            public MyGuiControlParent Parent;
            MyGuiControlSeparatorList m_separator;
            MyGuiControlLabel m_time;

            public WarningLine(MySimpleProfiler.PerformanceWarning warning, MyGuiScreenPerformanceWarnings screen)
            {
                Parent = new MyGuiControlParent();
                m_name = new MyGuiControlLabel(text: Truncate(warning.Block.DisplayName), position: new Vector2(-0.43f, 0), originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, font: VRage.Game.MyFontEnum.Red);
                m_description = new MyGuiControlLabel
                    (
                    text: String.IsNullOrEmpty(warning.Block.Description.String) ? MyTexts.GetString(MyCommonTexts.PerformanceWarningTooManyBlocks) : MyTexts.GetString(warning.Block.Description), 
                    position: new Vector2(-0.24f, 0), 
                    originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                    );
                m_separator = new MyGuiControlSeparatorList();
                Parent.Size = new Vector2(Parent.Size.X, m_description.Size.Y);
                m_separator.AddVertical(new Vector2(-0.25f, -Parent.Size.Y / 2 - 0.006f), Parent.Size.Y + 0.016f);
                m_separator.AddVertical(new Vector2(0.35f, -Parent.Size.Y / 2 - 0.006f), Parent.Size.Y + 0.016f);
                m_time = new MyGuiControlLabel(position: new Vector2(0.43f, 0), originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);

                switch (warning.Block.type)
                {
                    case MySimpleProfiler.MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS:
                        screen.m_areaTitleGraphics.Warnings.Add(this);
                        break;
                    case MySimpleProfiler.MySimpleProfilingBlock.ProfilingBlockType.BLOCK:
                        screen.m_areaTitleBlocks.Warnings.Add(this);
                        break;
                    case MySimpleProfiler.MySimpleProfilingBlock.ProfilingBlockType.OTHER:
                        screen.m_areaTitleOther.Warnings.Add(this);
                        break;
                }
                this.Warning = warning;
            }

            public void Prepare()
            {
                Parent.Position = Vector2.Zero;
                int hours = Warning.Time / 216000;
                int minutes = Warning.Time % 216000 / 3600;
                int seconds = Warning.Time % 3600 / 60;
                m_time.Text = String.Format("{0}:{1}:{2}", hours, minutes < 10 ? "0" + minutes : minutes.ToString(), seconds < 10 ? "0" + seconds : seconds.ToString());
                if (Parent.Controls.Count == 0)
                {
                    Parent.Controls.Add(m_name);
                    Parent.Controls.Add(m_description);
                    Parent.Controls.Add(m_separator);
                    Parent.Controls.Add(m_time);
                }
            }

            string Truncate(string input)
            {
                if (input.Length < 21) 
                    return input;
                else
                {
                    m_tmpTruncated.Clear();
                    m_tmpTruncated.Append(input.Substring(0, 18));
                    m_tmpTruncated.Append("...");
                    return m_tmpTruncated.ToString();
                }
                    
            }
        }

        /// <summary>
        /// Used to contain each of the areas (graphics, blocks, other). Also holds the headings.
        /// </summary>
        internal class WarningArea
        {
            internal List<WarningLine> Warnings;
            MyGuiControlParent m_header;
            MyGuiControlPanel m_titleBackground;
            MyGuiControlLabel m_title;
            MyGuiControlLabel m_lastOccurence;
            MyGuiControlSeparatorList m_separator;
            MyGuiControlButton m_graphicsButton;

            public WarningArea(string name, bool graphicsButton)
            {
                Warnings = new List<WarningLine>();

                m_header = new MyGuiControlParent();
                m_titleBackground = new MyGuiControlPanel(texture: @"Textures\GUI\Controls\item_highlight_dark.dds");
                m_title = new MyGuiControlLabel(text: name);
                m_lastOccurence = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.PerformanceWarningLastOccurrence), originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                m_separator = new MyGuiControlSeparatorList();
                m_separator.AddHorizontal(new Vector2(-0.45f, 0.018f), 0.9f);

                m_title.Position = new Vector2(-0.43f, 0f);
                m_lastOccurence.Position = new Vector2(0.43f, 0f);
                m_titleBackground.Size = new Vector2(m_titleBackground.Size.X, 0.035f);
                m_header.Size = new Vector2(m_header.Size.X, m_titleBackground.Size.Y);
                if (graphicsButton) 
                {
                    m_graphicsButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.ScreenCaptionGraphicsOptions), onButtonClick: (sender) => { MyGuiSandbox.AddScreen(new MyGuiScreenOptionsGraphics()); });
                }
            }

            /// <summary>
            /// Add this area into a list
            /// </summary>
            public void Add(MyGuiControlList list, bool showAll)
            {
                m_header.Position = Vector2.Zero;
                if (m_header.Controls.Count == 0)
                {
                    m_header.Controls.Add(m_titleBackground);
                    m_header.Controls.Add(m_title);
                    m_header.Controls.Add(m_lastOccurence);
                    m_header.Controls.Add(m_separator);
                }

                bool headerAdded = false;
                Warnings.Sort(delegate(WarningLine x, WarningLine y)
                {
                    return x.Warning.Time - y.Warning.Time;
                });
                foreach (var warning in Warnings)
                {
                    if (warning.Warning.Time < 120 || showAll)
                    {
                        if (!headerAdded)
                        {
                            list.Controls.Add(m_header);
                            headerAdded = true;
                        }
                        warning.Prepare();
                        list.Controls.Add(warning.Parent);
                    }
                }
                if (headerAdded && m_graphicsButton != null)
                {
                    list.Controls.Add(m_graphicsButton);
                }
            }
        }

        private MyGuiControlList m_warningsList;
        private MyGuiControlCheckbox m_showWarningsCheckBox;
        private MyGuiControlCheckbox m_showAllCheckBox;
        private MyGuiControlButton m_okButton;
        private Dictionary<MySimpleProfiler.MySimpleProfilingBlock, WarningLine> m_warningLines = new Dictionary<MySimpleProfiler.MySimpleProfilingBlock, WarningLine>();
        internal WarningArea m_areaTitleGraphics = new WarningArea(MyTexts.GetString(MyCommonTexts.PerformanceWarningIssuesGraphics), true);
        internal WarningArea m_areaTitleBlocks = new WarningArea(MyTexts.GetString(MyCommonTexts.PerformanceWarningIssuesBlocks), false);
        internal WarningArea m_areaTitleOther = new WarningArea(MyTexts.GetString(MyCommonTexts.PerformanceWarningIssuesOther), false);

        private static bool m_showAll;

        public MyGuiScreenPerformanceWarnings()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1f, 0.98f))
        {
            EnabledBackgroundFade = true;
            CloseButtonEnabled = true;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenPerformanceWarnings";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(MyTexts.GetString(MyCommonTexts.PerformanceWarningHelpHeader));

            m_warningsList = new MyGuiControlList(position: new Vector2(0f, -0.05f), size: new Vector2(0.92f, 0.7f));
            var m_showWarningsLabel = new MyGuiControlLabel(
                text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_EnablePerformanceWarnings), 
                position: new Vector2(-0.17f, 0.35f),
                originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
                );
            m_showWarningsCheckBox = new MyGuiControlCheckbox(
                toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsEnablePerformanceWarnings), 
                position: new Vector2(-0.15f, 0.35f),
                originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
            m_showWarningsCheckBox.IsChecked = MySandboxGame.Config.EnablePerformanceWarnings;
            m_showWarningsCheckBox.IsCheckedChanged += ShowWarningsChanged;

            var m_showAllLabel = new MyGuiControlLabel(
                text: MyTexts.GetString(MyCommonTexts.PerformanceWarningShowAll),
                position: new Vector2(0.25f, 0.35f),
                originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
                );
            m_showAllCheckBox = new MyGuiControlCheckbox(
                toolTip: MyTexts.GetString(MyCommonTexts.ToolTipPerformanceWarningShowAll),
                position: new Vector2(0.27f, 0.35f),
                originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
                );
            m_showAllCheckBox.IsChecked = m_showAll;
            m_showAllCheckBox.IsCheckedChanged += KeepInListChanged;

            m_okButton = new MyGuiControlButton(position: new Vector2(0, 0.42f), text: MyTexts.Get(MyCommonTexts.Ok));
            m_okButton.ButtonClicked += m_okButton_ButtonClicked;

            Controls.Add(m_warningsList);
            Controls.Add(m_showWarningsLabel);
            Controls.Add(m_showWarningsCheckBox);
            Controls.Add(m_showAllLabel);
            Controls.Add(m_showAllCheckBox);
            Controls.Add(m_okButton);

        }

        protected override void OnClosed()
        {
            base.OnClosed();
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }

        private void Refresh()
        {
            m_warningsList.Controls.Clear();
            foreach (var warning in MySimpleProfiler.CurrentWarnings.Values)
            {
                if (warning.Time < 120 || m_showAll)
                {
                    WarningLine warningLine;
                    if (!m_warningLines.TryGetValue(warning.Block, out warningLine))
                    {
                        warningLine = new WarningLine(warning, this);
                        m_warningLines.Add(warning.Block, warningLine);
                    }
                }
            }
            m_areaTitleGraphics.Add(m_warningsList, m_showAll);
            m_areaTitleBlocks.Add(m_warningsList, m_showAll);
            m_areaTitleOther.Add(m_warningsList, m_showAll);
        }

        private void ShowWarningsChanged(MyGuiControlCheckbox obj)
        {
            MySandboxGame.Config.EnablePerformanceWarnings = obj.IsChecked;
        }

        private void KeepInListChanged(MyGuiControlCheckbox obj)
        {
            m_showAll = obj.IsChecked;
        }

        void m_okButton_ButtonClicked(MyGuiControlButton obj)
        {
            CloseScreen();
        }

        public override bool Update(bool hasFocus)
        {
            Refresh();
            return base.Update(hasFocus);
        }
    }
}

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public abstract class MyGuiScreenScenarioBase : MyGuiScreenBase
    {
        protected enum StateEnum
        {
            ListNeedsReload,
            ListLoading,
            ListLoaded
        }
        protected StateEnum m_state;

        protected MyGuiControlTextbox m_nameTextbox, m_descriptionTextbox;
        protected MyGuiControlButton m_okButton, m_cancelButton;
        protected MyGuiControlTable m_scenarioTable;
        protected MyGuiControlMultilineText m_descriptionBox;

        protected MyLayoutTable m_sideMenuLayout;
        protected MyLayoutTable m_buttonsLayout;

        protected int m_selectedRow;
        
        protected const float MARGIN_TOP = 0.1f;
        protected const float MARGIN_LEFT = 0.42f;
        protected const string WORKSHOP_PATH_TAG = "workshop";

        private List<Tuple<string, MyWorldInfo>> m_availableSaves = new List<Tuple<string, MyWorldInfo>>();

        public MyGuiScreenScenarioBase()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(null/*checkpoint*/))
        {
        }

        private static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)//TODO optimize
        {
            float width = checkpoint == null ? 0.9f : 0.65f;
            float height = checkpoint == null ? 1.24f : 0.97f;
            if (checkpoint != null)
                height -= 0.05f;
            height -= 0.27f;

            return new Vector2(width, height);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();
            SetDefaultValues();
        }

        protected virtual void BuildControls()
        {
            AddCaption(ScreenCaption);

            // side menu
            {
                var nameLabel = MakeLabel(MyCommonTexts.Name);
                var descriptionLabel = MakeLabel(MyCommonTexts.Description);

                m_nameTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_NAME_LENGTH);
                m_nameTextbox.Enabled = false;
                m_descriptionTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_DESCRIPTION_LENGTH);
                m_descriptionTextbox.Enabled = false;

                Vector2 originL;
                Vector2 controlsDelta = new Vector2(0f, 0.052f);
                originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);

                var screenSize = m_size.Value;
                var layoutSize = screenSize / 2 - originL;
                var columnWidthLabel = layoutSize.X * 0.25f;
                var columnWidthControl = layoutSize.X - columnWidthLabel;
                var rowHeight = 0.052f;
                layoutSize.Y = rowHeight * 5;

                m_sideMenuLayout = new MyLayoutTable(this, originL, layoutSize);
                m_sideMenuLayout.SetColumnWidthsNormalized(columnWidthLabel, columnWidthControl);
                m_sideMenuLayout.SetRowHeightsNormalized(rowHeight, rowHeight, rowHeight, rowHeight, rowHeight);
                
                m_sideMenuLayout.Add(nameLabel, MyAlignH.Left, MyAlignV.Top, 0, 0);
                m_sideMenuLayout.Add(m_nameTextbox, MyAlignH.Left, MyAlignV.Top, 0, 1);
                m_sideMenuLayout.Add(descriptionLabel, MyAlignH.Left, MyAlignV.Top, 1, 0);
                m_sideMenuLayout.Add(m_descriptionTextbox, MyAlignH.Left, MyAlignV.Top, 1, 1);
            }

            // briefing
            {
                var briefingPanel = new MyGuiControlPanel()
                {
                    Name = "BriefingPanel",
                    Position = new Vector2(-0.02f, -0.12f),
                    Size = new Vector2(0.43f, 0.422f),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST
                };
                Controls.Add(briefingPanel);

                m_descriptionBox = new MyGuiControlMultilineText(
                    selectable: false,
                    font: MyFontEnum.Blue)
                {
                    Name = "BriefingMultilineText",
                    Position = new Vector2(-0.009f, -0.115f),
                    Size = new Vector2(0.419f, 0.412f),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                };
                Controls.Add(m_descriptionBox);
            }

            // buttons
            {
                int buttonRowCount = 2;
                int buttonColumnCount = 4;
                Vector2 buttonSize = new Vector2(300f / 1600f, 70f / 1200f);
                Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.83f, 0.16f);
                Vector2 buttonOffset = new Vector2(0.01f, 0.01f);
                Vector2 buttonLayoutSize = new Vector2((buttonSize.X + buttonOffset.X) * (buttonColumnCount), (buttonSize.Y + buttonOffset.Y) * (buttonRowCount));
                m_buttonsLayout = new MyLayoutTable(this, buttonsOrigin, buttonLayoutSize);

                float[] columnWidths = Enumerable.Repeat(buttonSize.X + buttonOffset.X, buttonColumnCount).ToArray();
                m_buttonsLayout.SetColumnWidthsNormalized(columnWidths);
                float[] rowHeights = Enumerable.Repeat(buttonSize.Y + buttonOffset.Y, buttonRowCount).ToArray();
                m_buttonsLayout.SetRowHeightsNormalized(rowHeights);

                m_okButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkButtonClick);
                m_cancelButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick);

                m_buttonsLayout.Add(m_okButton, MyAlignH.Left, MyAlignV.Top, 1, 2);
                m_buttonsLayout.Add(m_cancelButton, MyAlignH.Left, MyAlignV.Top, 1, 3);
            }

            // left menu
            {
                m_scenarioTable = CreateScenarioTable();
                Controls.Add(m_scenarioTable);
            }
        }

        protected virtual MyGuiControlTable CreateScenarioTable()
        {
            var scenarioTable = new MyGuiControlTable();
            scenarioTable.Position = new Vector2(-0.42f, -0.5f + MARGIN_TOP);
            scenarioTable.Size = new Vector2(0.38f, 1.8f);
            scenarioTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            scenarioTable.VisibleRowsCount = 20;
            scenarioTable.ColumnsCount = 2;
            scenarioTable.SetCustomColumnWidths(new float[] { 0.085f, 0.905f });
            scenarioTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.Name));
            scenarioTable.ItemSelected += OnTableItemSelected;

            return scenarioTable;
        }

        protected MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        protected abstract MyStringId ScreenCaption { get; }
        protected abstract bool IsOnlineMode { get; }

        protected virtual void SetDefaultValues()
        {
            FillRight();
        }

        protected void OnOkButtonClick(object sender)
        {
            // Validate
            if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH || m_nameTextbox.Text.Length > MySession.MAX_NAME_LENGTH)
            {
                MyStringId errorType;
                if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH) errorType = MyCommonTexts.ErrorNameTooShort;
                else errorType = MyCommonTexts.ErrorNameTooLong;
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(errorType),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }

            if (m_descriptionTextbox.Text.Length > MySession.MAX_DESCRIPTION_LENGTH)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.ErrorDescriptionTooLong),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }

            CloseScreen();
            LoadSandbox(IsOnlineMode);
        }

        private void OnCancelButtonClick(object sender)
        {
            CloseScreen();
        }

        protected virtual void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = eventArgs.RowIndex;
            FillRight();
        }

        public override bool Update(bool hasFocus)
        {
            if (m_state == StateEnum.ListNeedsReload)
                FillList();

            if (m_scenarioTable.SelectedRow != null)
            {
                m_okButton.Enabled = true;
            }
            else
            {
                m_okButton.Enabled = false;
            }

            return base.Update(hasFocus);
        }

        public override bool Draw()
        {
            // Dont draw screen when the list is about to be reloaded,
            // otherwise it will flick just before opening the loading screen
            if (m_state != StateEnum.ListLoaded)
                return false;
            return base.Draw();
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (m_state == StateEnum.ListNeedsReload)
                FillList();
        }

        protected void FillRight()
        {
            if (m_scenarioTable == null || m_scenarioTable.SelectedRow == null)
            {
                m_nameTextbox.SetText(new StringBuilder(""));
                m_descriptionTextbox.SetText(new StringBuilder(""));
            }
            else
            {
                Tuple<string, MyWorldInfo> t = FindSave(m_scenarioTable.SelectedRow);
                m_nameTextbox.SetText(new StringBuilder(MyTexts.GetString(t.Item2.SessionName)));// translation of session name
                m_descriptionTextbox.SetText(new StringBuilder(t.Item2.Description));
                m_descriptionBox.Text = new StringBuilder(MyTexts.GetString(t.Item2.Briefing)); // translation of checkpoint briefing
            }

        }

        protected virtual void FillList()
        {
            m_state = StateEnum.ListLoading;
        }

        protected void AddSave(Tuple<string, MyWorldInfo> save)
        {
            m_availableSaves.Add(save);
        }

        protected void AddSaves(List<Tuple<string, MyWorldInfo>> saves)
        {
            m_availableSaves.AddList(saves);
        }

        protected void ClearSaves()
        {
            m_availableSaves.Clear();
        }

        protected void RefreshGameList()
        {
            int selectedIndex = m_scenarioTable.SelectedRowIndex ?? -1;
            m_scenarioTable.Clear();
            Color? color = null;
            for (int index = 0; index < m_availableSaves.Count; index++)
            {
                var checkpoint = m_availableSaves[index].Item2;
                var name = new StringBuilder(checkpoint.SessionName);
                var row = new MyGuiControlTable.Row(m_availableSaves[index]);
                row.AddCell(new MyGuiControlTable.Cell(text: String.Empty, textColor : color, icon: GetIcon(m_availableSaves[index])));
                row.AddCell(new MyGuiControlTable.Cell(text: name, textColor: color, userData: name));
                m_scenarioTable.Add(row);

                // Select row with same world ID as we had before refresh.
                if (index == selectedIndex)
                {
                    m_selectedRow = index;
                    m_scenarioTable.SelectedRow = row;
                }
            }

            m_scenarioTable.SelectedRowIndex = m_selectedRow;
            m_scenarioTable.ScrollToSelection();
            FillRight();
        }

        protected Tuple<string, MyWorldInfo> FindSave(MyGuiControlTable.Row row)
        {
            return (Tuple<string, MyWorldInfo>)(row.UserData);
        }

        
        protected virtual MyGuiHighlightTexture GetIcon(Tuple<string, MyWorldInfo> save)
        {
            return MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL;
        }

        private void LoadSandbox(bool MP)
        {
            MyLog.Default.WriteLine("LoadSandbox() - Start");
            var row = m_scenarioTable.SelectedRow;
            if (row != null)
            {
                var save = FindSave(row);
                if (save != null)
                {
                    LoadSandboxInternal(save, MP);
                }
            }

            MyLog.Default.WriteLine("LoadSandbox() - End");
        }

        protected virtual void LoadSandboxInternal(Tuple<string, MyWorldInfo> save, bool MP)
        {
        }
    }
}

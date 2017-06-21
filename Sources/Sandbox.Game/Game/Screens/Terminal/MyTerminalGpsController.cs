using Sandbox.ModAPI;
using Sandbox.Common;

using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

using Sandbox.Game.Screens.Helpers;
using VRage;
using Sandbox.Engine.Networking;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;
using System.Threading;
#if !XB
using System.Text.RegularExpressions;
#endif // !XB1
using Sandbox.Game.Localization;
using VRage;

namespace Sandbox.Game.Gui
{
    class MyTerminalGpsController
    {

        private IMyGuiControlsParent m_controlsParent;
        MyGuiControlTextbox m_searchIns;
        MyGuiControlButton m_searchInsClear;
        MyGuiControlTable m_tableIns;

        MyGuiControlLabel m_labelInsName;
        MyGuiControlTextbox m_panelInsName;
        MyGuiControlLabel m_labelInsDesc;
        MyGuiControlTextbox m_panelInsDesc;
        MyGuiControlLabel m_labelInsX;
        MyGuiControlTextbox m_xCoord;
        MyGuiControlLabel m_labelInsY;
        MyGuiControlTextbox m_yCoord;
        MyGuiControlLabel m_labelInsZ;
        MyGuiControlTextbox m_zCoord;

        MyGuiControlLabel m_labelInsShowOnHud;
        MyGuiControlCheckbox m_checkInsShowOnHud;

        MyGuiControlLabel m_labelInsAlwaysVisible;
        MyGuiControlCheckbox m_checkInsAlwaysVisible;

        MyGuiControlButton m_buttonAdd;
        MyGuiControlButton m_buttonAddFromClipboard;
        MyGuiControlButton m_buttonAddCurrent;
        MyGuiControlButton m_buttonDelete;

        MyGuiControlButton m_buttonCopy;

        MyGuiControlLabel m_labelSaveWarning;

        public void Init(IMyGuiControlsParent controlsParent)
        {
            m_controlsParent = controlsParent;
            //left:
            m_searchIns = (MyGuiControlTextbox)m_controlsParent.Controls.GetControlByName("SearchIns");
            m_searchIns.TextChanged += searchIns_TextChanged;
            m_searchInsClear = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("SearchInsClear");
            m_searchInsClear.ButtonClicked += searchInsClear_ButtonClicked;

            m_tableIns = (MyGuiControlTable)controlsParent.Controls.GetControlByName("TableINS");
            m_tableIns.SetColumnComparison(0, TableSortingComparison);
            m_tableIns.ItemSelected += OnTableItemSelected;
            m_tableIns.ItemDoubleClicked += OnTableDoubleclick;
            
            //bottom buttons
            m_buttonAdd = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonAdd");
            m_buttonAddCurrent = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonFromCurrent");
            m_buttonAddFromClipboard = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonFromClipboard");
            m_buttonDelete = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonDelete");
            m_buttonAdd.ButtonClicked += OnButtonPressedNew;
            m_buttonAddFromClipboard.ButtonClicked += OnButtonPressedNewFromClipboard;
            m_buttonAddCurrent.ButtonClicked += OnButtonPressedNewFromCurrent;
            m_buttonDelete.ButtonClicked += OnButtonPressedDelete;

            //right:
            m_labelInsName = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsName");
            m_panelInsName = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("panelInsName");
            m_labelInsDesc = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsDesc");
            m_panelInsDesc = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsDesc");

            m_labelInsX = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsX");
            m_xCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsX");
            m_labelInsY = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsY");
            m_yCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsY");
            m_labelInsZ = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsZ");
            m_zCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsZ");

            m_labelInsShowOnHud = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsShowOnHud");
            m_checkInsShowOnHud = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkInsShowOnHud");
            m_checkInsShowOnHud.IsCheckedChanged += OnShowOnHudChecked;

            m_labelInsAlwaysVisible = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsAlwaysVisible");
            m_checkInsAlwaysVisible = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkInsAlwaysVisible");
            m_checkInsAlwaysVisible.IsCheckedChanged += OnAlwaysVisibleChecked;

            m_buttonCopy = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonToClipboard");
            m_buttonCopy.ButtonClicked += OnButtonPressedCopy;


            m_labelSaveWarning = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("TerminalTab_GPS_SaveWarning");
            m_labelSaveWarning.Visible = false;

            HookSyncEvents();

            MySession.Static.Gpss.GpsChanged += OnInsChanged;
            MySession.Static.Gpss.ListChanged += OnListChanged;

            //int ret = MySession.Static.Inss.ScanText("df INS:nefinalni 3:1:2.3:1:  INS:nefinalni 4:2:2.3:2:");

            MySession.Static.Gpss.DiscardOld();

            PopulateList();
            m_previousHash = null;
            enableEditBoxes(false);
            m_buttonDelete.Enabled = false;
        }

        private int TableSortingComparison(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            if (((MyGps)a.UserData).DiscardAt != null && ((MyGps)b.UserData).DiscardAt != null
                || ((MyGps)a.UserData).DiscardAt == null && ((MyGps)b.UserData).DiscardAt == null)
            {//sort by name
                return a.Text.CompareToIgnoreCase(b.Text);
            }
            else
            {//final first
                if (((MyGps)a.UserData).DiscardAt == null)
                    return -1;
                else
                    return 1;
            }
        }


        public void PopulateList()
        {
            PopulateList(null);
        }

        public void PopulateList(string searchString)
        {
            var selected = m_tableIns.SelectedRow==null?null:m_tableIns.SelectedRow.UserData;
            ClearList();
            if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
                foreach (var item in MySession.Static.Gpss[MySession.Static.LocalPlayerId])
                {
                    if (searchString != null)
                    {
                        String[] tmpSearch = searchString.ToLower().Split(' ');
                        String tmpName = item.Value.Name.ToString().ToLower();
                        bool add = true;
                        foreach (var search in tmpSearch)
                            if (!tmpName.Contains(search.ToLower()))
                            {
                                add = false;
                                break;
                            }
                        if (add)
                            AddToList(item.Value);
                    }
                    else
                    {
                        AddToList(item.Value);
                    }
                }
            m_tableIns.SortByColumn(0, MyGuiControlTable.SortStateEnum.Ascending);
            enableEditBoxes(false);
            if (selected!=null)
                for (int i = 0; i < m_tableIns.RowsCount; i++)
                    if (selected == m_tableIns.GetRow(i).UserData)
                    {
                        m_tableIns.SelectedRowIndex = i;
                        enableEditBoxes(true);
                        break;
                    }
            m_tableIns.ScrollToSelection();
            FillRight();
        }

        public static readonly Color ITEM_SHOWN_COLOR = Color.CornflowerBlue;

        private MyGuiControlTable.Row AddToList(MyGps ins)
        {
            var row = new MyGuiControlTable.Row(ins);
            var name = new StringBuilder(ins.Name);
            row.AddCell(new MyGuiControlTable.Cell(text: name, userData: ins, textColor: (ins.DiscardAt != null ? Color.Gray : (ins.ShowOnHud ? ITEM_SHOWN_COLOR : Color.White))));
            m_tableIns.Add(row);
            return row;
        }


        public void ClearList()
        {
            if (m_tableIns != null)
                m_tableIns.Clear();
        }

        private void searchIns_TextChanged(MyGuiControlTextbox sender)
        {
            PopulateList(sender.Text);
        }

        private void searchInsClear_ButtonClicked(MyGuiControlButton sender)
        {
            m_searchIns.Text = "";
            PopulateList(null);
        }

        private void OnTableItemSelected(MyGuiControlTable sender, Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs args)
        {
            trySync();
            if (sender.SelectedRow != null)
            {
                enableEditBoxes(true);
                m_buttonDelete.Enabled = true;
                FillRight((MyGps)sender.SelectedRow.UserData);
            }
            else
            {
                enableEditBoxes(false);
                m_buttonDelete.Enabled = false;
                ClearRight();
            }
        }

        private void enableEditBoxes(bool enable)
        {
            m_panelInsName.Enabled = enable;
            m_panelInsDesc.Enabled = enable;
            m_xCoord.Enabled = enable;
            m_yCoord.Enabled = enable;
            m_zCoord.Enabled = enable;
            m_checkInsShowOnHud.Enabled = enable;
            m_checkInsAlwaysVisible.Enabled = enable;
            m_buttonCopy.Enabled = enable;
        }

        private void OnTableDoubleclick(MyGuiControlTable sender, Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs args)
        {
            if (sender.SelectedRow != null)
            {
                ((MyGps)sender.SelectedRow.UserData).ShowOnHud ^= true;
                MySession.Static.Gpss.ChangeShowOnHud(MySession.Static.LocalPlayerId, ((MyGps)sender.SelectedRow.UserData).Hash, ((MyGps)sender.SelectedRow.UserData).ShowOnHud);
            }
        }

        #region edit right side values and their sync
        int? m_previousHash;//for sync
        bool m_needsSyncName;
        bool m_needsSyncDesc;
        bool m_needsSyncX;
        bool m_needsSyncY;
        bool m_needsSyncZ;

        void HookSyncEvents()
        {
            m_panelInsName.TextChanged += OnNameChanged;
            m_panelInsDesc.TextChanged += OnDescChanged;
            m_xCoord.TextChanged += OnXChanged;
            m_yCoord.TextChanged += OnYChanged;
            m_zCoord.TextChanged += OnZChanged;
        }
        void UnhookSyncEvents()
        {
            m_panelInsName.TextChanged -= OnNameChanged;
            m_panelInsDesc.TextChanged -= OnDescChanged;
            m_xCoord.TextChanged -= OnXChanged;
            m_yCoord.TextChanged -= OnYChanged;
            m_zCoord.TextChanged -= OnZChanged;
        }

        public void OnNameChanged(MyGuiControlTextbox sender)
        {
            if (m_tableIns.SelectedRow != null)
            {
                m_needsSyncName = true;
                //":" is not valid:
                if (IsNameOk(sender.Text))
                {
                    m_nameOk = true;
                    sender.ColorMask = Vector4.One;
                    //propagate new name into table and re-sort:
                    Sandbox.Graphics.GUI.MyGuiControlTable.Row selected = m_tableIns.SelectedRow;
                    Sandbox.Graphics.GUI.MyGuiControlTable.Cell cell = selected.GetCell(0);
                    if (cell != null)
                        cell.Text.Clear().Append(sender.Text);
                    m_tableIns.SortByColumn(0, MyGuiControlTable.SortStateEnum.Ascending);
                    //select same entry:
                    for (int i = 0; i < m_tableIns.RowsCount; i++)
                        if (selected == m_tableIns.GetRow(i))
                        {
                            m_tableIns.SelectedRowIndex = i;
                            break;
                        }
                    m_tableIns.ScrollToSelection();
                }
                else
                {
                    m_nameOk = false;
                    sender.ColorMask = Color.Red.ToVector4();
                }
                updateWarningLabel();
            }
        }
        public bool IsNameOk(string str)
        {
            if (str.Contains(":"))
                return false;
            return true;
        }

        public void OnDescChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncDesc = true;
        }

        bool IsCoordOk(string str)
        {
            double x;
            return double.TryParse(str,System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out x);
        }

        public void OnXChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncX = true;
            if (IsCoordOk(sender.Text))
            {
                m_xOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_xOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            updateWarningLabel();
        }

        public void OnYChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncY = true;
            if (IsCoordOk(sender.Text))
            {
                m_yOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_yOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            updateWarningLabel();
        }

        public void OnZChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncZ = true;
            if (IsCoordOk(sender.Text))
            {
                m_zOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_zOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            updateWarningLabel();
        }
        private bool m_nameOk, m_xOk, m_yOk, m_zOk;
        private void updateWarningLabel()
        {
            if (m_nameOk && m_xOk && m_yOk && m_zOk)
            {
                m_labelSaveWarning.Visible = false;
                if (m_panelInsName.Enabled)//because copy button may be disabled if there is nothing selected from the list on the left
                    m_buttonCopy.Enabled = true;
            }
            else
            {
                m_labelSaveWarning.Visible = true;
                m_buttonCopy.Enabled = false;
            }
        }

        private MyGps m_syncedGps;
        private bool trySync()
        {//takes current right side values of name, description and coordinates, compares them against record with previous hash and synces if necessary
            if (m_previousHash != null && (m_needsSyncName || m_needsSyncDesc || m_needsSyncX || m_needsSyncY || m_needsSyncZ))
            {
                if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
                {
                    if (IsNameOk(m_panelInsName.Text) && IsCoordOk(m_xCoord.Text) && IsCoordOk(m_yCoord.Text) && IsCoordOk(m_zCoord.Text))
                    {
                        Dictionary<int, MyGps> insList;
                        insList = MySession.Static.Gpss[MySession.Static.LocalPlayerId];
                        MyGps ins;
                        if (insList.TryGetValue((int)m_previousHash, out ins))
                        {
                            if (m_needsSyncName)
                                ins.Name = m_panelInsName.Text;
                            if (m_needsSyncDesc)
                                ins.Description = m_panelInsDesc.Text;
                            StringBuilder str = new StringBuilder();
                            if (m_needsSyncX)
                            {
                                m_xCoord.GetText(str);
                                ins.Coords.X = Math.Round(double.Parse(str.ToString(), System.Globalization.CultureInfo.InvariantCulture),2);
                            }
                            str.Clear();
                            if (m_needsSyncY)
                            {
                                m_yCoord.GetText(str);
                                ins.Coords.Y = Math.Round(double.Parse(str.ToString(), System.Globalization.CultureInfo.InvariantCulture),2);
                            }
                            str.Clear();
                            if (m_needsSyncZ)
                            {
                                m_zCoord.GetText(str);
                                ins.Coords.Z = Math.Round(double.Parse(str.ToString(), System.Globalization.CultureInfo.InvariantCulture), 2);
                            }
                            m_syncedGps = ins;
                            MySession.Static.Gpss.SendModifyGps(MySession.Static.LocalPlayerId, ins);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        private void OnButtonPressedNew(MyGuiControlButton sender)
        {
            trySync();
            MyGps ins = new MyGps();
            ins.Name = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewCoord_Name).ToString();
            ins.Description = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewCoord_Desc).ToString();
            ins.Coords = new Vector3D(0, 0, 0);
            ins.ShowOnHud = true;
            ins.DiscardAt = null;//finalize
            MySession.Static.Gpss.SendAddGps(MySession.Static.LocalPlayerId, ref ins);
            m_searchIns.Text = "";
            enableEditBoxes(false);
        }

        private StringBuilder m_NameBuilder = new StringBuilder();
        private void OnButtonPressedNewFromCurrent(MyGuiControlButton sender)
        {
            trySync();
            MyGps ins = new MyGps();
            MySession.Static.Gpss.GetNameForNewCurrent(m_NameBuilder);
            ins.Name = m_NameBuilder.ToString();
            ins.Description = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromCurrent_Desc).ToString();
            ins.Coords = new Vector3D(MySession.Static.LocalHumanPlayer.GetPosition());
            ins.Coords.X = Math.Round(ins.Coords.X, 2);
            ins.Coords.Y = Math.Round(ins.Coords.Y, 2);
            ins.Coords.Z = Math.Round(ins.Coords.Z, 2);
            ins.ShowOnHud = true;
            ins.DiscardAt = null;//final
            MySession.Static.Gpss.SendAddGps(MySession.Static.LocalPlayerId, ref ins);
            m_searchIns.Text = "";
            enableEditBoxes(false);
        }

        string m_clipboardText;
        void PasteFromClipboard()
        {
#if !XB1            
            m_clipboardText = System.Windows.Forms.Clipboard.GetText();
#else
            System.Diagnostics.Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
        }
        private void OnButtonPressedNewFromClipboard(MyGuiControlButton sender)
        {
            Thread thread = new Thread(() => PasteFromClipboard());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            MySession.Static.Gpss.ScanText(m_clipboardText, MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromClipboard_Desc));
            m_searchIns.Text = "";
        }

        private void OnButtonPressedDelete(MyGuiControlButton sender)
        {
            if (m_tableIns.SelectedRow == null)
                return;
            Delete();
        }
        private void Delete()
        {
            MySession.Static.Gpss.SendDelete(MySession.Static.LocalPlayerId, ((MyGps)m_tableIns.SelectedRow.UserData).GetHashCode());
            PopulateList();
            enableEditBoxes(false);
            m_buttonDelete.Enabled = false;
        }
        public void OnDelKeyPressed()
        {//delete entry from list
            if (m_tableIns.SelectedRow == null || m_tableIns.HasFocus == false)
                return;
            Delete();
        }

        private void OnButtonPressedCopy(MyGuiControlButton sender)
        {
            if (m_tableIns.SelectedRow == null)
                return;
            if (trySync())//copy values directly from right side (they may be still running through network at this time, not yet updated in m_tableIns)
                m_syncedGps.ToClipboard();
            else
                ((MyGps)m_tableIns.SelectedRow.UserData).ToClipboard();

        }

        private void OnInsChanged(long id,int hash)
        {
            //screen refresh is only needed when an GPS applies to this player
            if (id==MySession.Static.LocalPlayerId)
            {
                FillRight();
                //color/name in table:
                int i = 0;
                while (i<m_tableIns.RowsCount)
                {
                    if (((MyGps)m_tableIns.GetRow(i).UserData).GetHashCode() == hash)
                    {
                        Sandbox.Graphics.GUI.MyGuiControlTable.Cell cell = m_tableIns.GetRow(i).GetCell(0);
                        if (cell != null)
                        {
                            MyGps ins=(MyGps)m_tableIns.GetRow(i).UserData;
                            cell.TextColor = (ins.DiscardAt != null ? Color.Gray : (ins.ShowOnHud ? ITEM_SHOWN_COLOR : Color.White));
                            cell.Text.Clear().Append(((MyGps)m_tableIns.GetRow(i).UserData).Name);
                            //FillRight((MyIns)m_tableIns.SelectedRow.UserData);
                        }
                        break;
                    }
                    ++i;
                }
            }
        }
        private void OnListChanged(long id)
        {
            //screen refresh is only needed when an INS applies to this player
            if (id==MySession.Static.LocalPlayerId)
                PopulateList();
        }

        private void OnShowOnHudChecked(MyGuiControlCheckbox sender)
        {
            if (m_tableIns.SelectedRow == null)
                return;
            MyGps gps = m_tableIns.SelectedRow.UserData as MyGps;
            gps.ShowOnHud = sender.IsChecked;//will be updated onSuccess but need to be correct for trySync now
            if (!sender.IsChecked && gps.AlwaysVisible)
            {
                gps.AlwaysVisible = false;

                // Uncheck Always Visible without sending it
                m_checkInsShowOnHud.IsCheckedChanged -= OnShowOnHudChecked;
                m_checkInsShowOnHud.IsChecked = false;
                m_checkInsShowOnHud.IsCheckedChanged += OnShowOnHudChecked;
            }

            if (!trySync())
                MySession.Static.Gpss.ChangeShowOnHud(MySession.Static.LocalPlayerId, gps.Hash, sender.IsChecked);
        }

        private void OnAlwaysVisibleChecked(MyGuiControlCheckbox sender)
        {
            if (m_tableIns.SelectedRow == null)
                return;

            MyGps gps = m_tableIns.SelectedRow.UserData as MyGps;
            gps.AlwaysVisible = sender.IsChecked;//will be updated onSuccess but need to be correct for trySync now
            gps.ShowOnHud = gps.ShowOnHud || gps.AlwaysVisible;

            // Check Show on HUD without sending it
            m_checkInsShowOnHud.IsCheckedChanged -= OnShowOnHudChecked;
            m_checkInsShowOnHud.IsChecked = m_checkInsShowOnHud.IsChecked || sender.IsChecked;
            m_checkInsShowOnHud.IsCheckedChanged += OnShowOnHudChecked;

            if (!trySync())
                MySession.Static.Gpss.ChangeAlwaysVisible(MySession.Static.LocalPlayerId, gps.Hash, sender.IsChecked);
        }

        private void FillRight()
        {
            if (m_tableIns.SelectedRow != null)
                FillRight((MyGps)m_tableIns.SelectedRow.UserData);
            else
                ClearRight();
            m_nameOk = m_xOk = m_yOk = m_zOk = true;
            updateWarningLabel();
        }

        private void FillRight(MyGps ins)
        {
            UnhookSyncEvents();
            m_panelInsName.SetText(new StringBuilder(ins.Name));
            m_panelInsDesc.SetText(new StringBuilder(ins.Description));
            //m_textInsDesc
            m_xCoord.SetText(new StringBuilder(ins.Coords.X.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));
            m_yCoord.SetText(new StringBuilder(ins.Coords.Y.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));
            m_zCoord.SetText(new StringBuilder(ins.Coords.Z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));
            m_checkInsShowOnHud.IsCheckedChanged -= OnShowOnHudChecked;
            m_checkInsShowOnHud.IsChecked = ins.ShowOnHud;
            m_checkInsShowOnHud.IsCheckedChanged += OnShowOnHudChecked;
            m_checkInsAlwaysVisible.IsCheckedChanged -= OnAlwaysVisibleChecked;
            m_checkInsAlwaysVisible.IsChecked = ins.AlwaysVisible;
            m_checkInsAlwaysVisible.IsCheckedChanged += OnAlwaysVisibleChecked;
            m_previousHash = ins.Hash;
            HookSyncEvents();
            m_needsSyncName = false;
            m_needsSyncDesc = false;
            m_needsSyncX = false;
            m_needsSyncY = false;
            m_needsSyncZ = false;

            m_panelInsName.ColorMask = Vector4.One;
            m_xCoord.ColorMask = Vector4.One;
            m_yCoord.ColorMask = Vector4.One;
            m_zCoord.ColorMask = Vector4.One;
            m_nameOk = m_xOk = m_yOk = m_zOk = true;
            updateWarningLabel();
        }

        private void ClearRight()
        {
            UnhookSyncEvents();
            StringBuilder sb = new StringBuilder("");
            m_panelInsName.SetText(sb);
            m_panelInsDesc.SetText(sb);
            //m_textInsDesc
            m_xCoord.SetText(sb);
            m_yCoord.SetText(sb);
            m_zCoord.SetText(sb);
            m_checkInsShowOnHud.IsChecked = false;
            m_checkInsAlwaysVisible.IsChecked = false;
            m_previousHash = null;
            HookSyncEvents();
            m_needsSyncName = false;
            m_needsSyncDesc = false;
            m_needsSyncX = false;
            m_needsSyncY = false;
            m_needsSyncZ = false;
        }

        public void Close()
        {
            trySync();
            if (m_tableIns != null)
            {
                ClearList();
                m_tableIns.ItemSelected -= OnTableItemSelected;
                m_tableIns.ItemDoubleClicked -= OnTableDoubleclick;
            }
            m_syncedGps = null;
            MySession.Static.Gpss.GpsChanged -= OnInsChanged;
            MySession.Static.Gpss.ListChanged -= OnListChanged;

            UnhookSyncEvents();

            m_checkInsShowOnHud.IsCheckedChanged -= OnShowOnHudChecked;
            m_checkInsAlwaysVisible.IsCheckedChanged -= OnAlwaysVisibleChecked;

            m_buttonAdd.ButtonClicked -= OnButtonPressedNew;
            m_buttonAddFromClipboard.ButtonClicked -= OnButtonPressedNewFromClipboard;
            m_buttonAddCurrent.ButtonClicked -= OnButtonPressedNewFromCurrent;
            m_buttonDelete.ButtonClicked -= OnButtonPressedDelete;

            m_buttonCopy.ButtonClicked -= OnButtonPressedCopy;
        }
    

    }
}

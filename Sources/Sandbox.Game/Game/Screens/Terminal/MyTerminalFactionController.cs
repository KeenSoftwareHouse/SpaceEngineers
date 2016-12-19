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

using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Game.ModAPI;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    class MyTerminalFactionController
    {
        internal static readonly Color COLOR_CUSTOM_RED   = new Color(228, 62 , 62 );
        internal static readonly Color COLOR_CUSTOM_GREEN = new Color(101, 178, 91 );
        internal static readonly Color COLOR_CUSTOM_GREY  = new Color(149, 169, 179);

        internal enum MyMemberComparerEnum
        {
            Founder   = 0,
            Leader    = 1,
            Member    = 2,
            Applicant = 3
        }

        private IMyGuiControlsParent m_controlsParent;

        private bool   m_userIsFounder;
        private bool   m_userIsLeader;
        private long   m_selectedUserId;
        private string m_selectedUserName;

        private IMyFaction m_userFaction;
        private IMyFaction m_selectedFaction;

        // left controls
        MyGuiControlTable m_tableFactions;

        MyGuiControlButton m_buttonCreate;
        MyGuiControlButton m_buttonJoin;
        MyGuiControlButton m_buttonCancelJoin;
        MyGuiControlButton m_buttonLeave;
        MyGuiControlButton m_buttonSendPeace;
        MyGuiControlButton m_buttonCancelPeace;
        MyGuiControlButton m_buttonAcceptPeace;
        MyGuiControlButton m_buttonMakeEnemy;

        // right controls
        MyGuiControlLabel m_labelFactionName;
        MyGuiControlLabel m_labelFactionDesc;
        MyGuiControlLabel m_labelFactionPriv;
        MyGuiControlLabel m_labelMembers;
        MyGuiControlLabel m_labelAutoAcceptMember;
        MyGuiControlLabel m_labelAutoAcceptPeace;

        MyGuiControlCheckbox m_checkAutoAcceptMember;
        MyGuiControlCheckbox m_checkAutoAcceptPeace;

        MyGuiControlMultilineText m_textFactionDesc;
        MyGuiControlMultilineText m_textFactionPriv;

        MyGuiControlTable m_tableMembers;

        MyGuiControlButton m_buttonEdit;
        MyGuiControlButton m_buttonKick;
        MyGuiControlButton m_buttonPromote;
        MyGuiControlButton m_buttonDemote;
        MyGuiControlButton m_buttonAcceptJoin;
        MyGuiControlButton m_buttonAddNpc;


        public void Init(IMyGuiControlsParent controlsParent)
        {
            m_controlsParent = controlsParent;
            RefreshUserInfo();

            m_tableFactions = (MyGuiControlTable)controlsParent.Controls.GetControlByName("FactionsTable");
            m_tableFactions.SetColumnComparison(0, (a, b) => ((StringBuilder)a.UserData).CompareToIgnoreCase((StringBuilder)b.UserData));
            m_tableFactions.SetColumnComparison(1, (a, b) => ((StringBuilder)a.UserData).CompareToIgnoreCase((StringBuilder)b.UserData));
            m_tableFactions.ItemSelected += OnFactionsTableItemSelected;
            RefreshTableFactions();
            m_tableFactions.SortByColumn(1);

            m_buttonCreate      = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonCreate");
            m_buttonJoin        = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonJoin");
            m_buttonCancelJoin  = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonCancelJoin");
            m_buttonLeave       = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonLeave");
            m_buttonSendPeace   = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonSendPeace");
            m_buttonCancelPeace = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonCancelPeace");
            m_buttonAcceptPeace = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonAcceptPeace");
            m_buttonMakeEnemy   = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonEnemy");

            m_buttonCreate.ShowTooltipWhenDisabled = true;

            m_buttonCreate.TextEnum      = MySpaceTexts.TerminalTab_Factions_Create;
            m_buttonJoin.TextEnum        = MySpaceTexts.TerminalTab_Factions_Join;
            m_buttonCancelJoin.TextEnum  = MySpaceTexts.TerminalTab_Factions_CancelJoin;
            m_buttonLeave.TextEnum       = MySpaceTexts.TerminalTab_Factions_Leave;
            m_buttonSendPeace.TextEnum   = MySpaceTexts.TerminalTab_Factions_Friend;
            m_buttonCancelPeace.TextEnum = MySpaceTexts.TerminalTab_Factions_CancelPeaceRequest;
            m_buttonAcceptPeace.TextEnum = MySpaceTexts.TerminalTab_Factions_AcceptPeaceRequest;
            m_buttonMakeEnemy.TextEnum   = MySpaceTexts.TerminalTab_Factions_Enemy;


            m_buttonJoin.SetToolTip(MySpaceTexts.TerminalTab_Factions_JoinToolTip);
            m_buttonSendPeace.SetToolTip(MySpaceTexts.TerminalTab_Factions_FriendToolTip);

            m_buttonCreate.ButtonClicked      += OnCreateClicked;
            m_buttonJoin.ButtonClicked        += OnJoinClicked;
            m_buttonCancelJoin.ButtonClicked  += OnCancelJoinClicked;
            m_buttonLeave.ButtonClicked       += OnLeaveClicked;
            m_buttonSendPeace.ButtonClicked   += OnFriendClicked;
            m_buttonCancelPeace.ButtonClicked += OnCancelPeaceRequestClicked;
            m_buttonAcceptPeace.ButtonClicked += OnAcceptFriendClicked;
            m_buttonMakeEnemy.ButtonClicked   += OnEnemyClicked;

            // RIGHT SIDE
            m_labelFactionName      = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionName");
            m_labelFactionDesc      = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionDesc");
            m_labelFactionPriv      = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionPrivate");
            m_labelMembers          = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionMembers");
            m_labelAutoAcceptMember = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionMembersAcceptEveryone");
            m_labelAutoAcceptPeace  = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelFactionMembersAcceptPeace");

            m_labelFactionDesc.Text      = MyTexts.Get(MySpaceTexts.TerminalTab_Factions_CreateFactionDescription).ToString();
            m_labelFactionPriv.Text      = MyTexts.Get(MySpaceTexts.TerminalTab_Factions_Private).ToString();
            m_labelMembers.Text          = MyTexts.Get(MySpaceTexts.TerminalTab_Factions_Members).ToString();
            m_labelAutoAcceptMember.Text = MyTexts.Get(MySpaceTexts.TerminalTab_Factions_AutoAccept).ToString();
            m_labelAutoAcceptPeace.Text  = MyTexts.Get(MySpaceTexts.TerminalTab_Factions_AutoAcceptRequest).ToString();

            m_labelAutoAcceptMember.SetToolTip(MySpaceTexts.TerminalTab_Factions_AutoAcceptToolTip);
            m_labelAutoAcceptPeace.SetToolTip(MySpaceTexts.TerminalTab_Factions_AutoAcceptRequestToolTip);

            m_textFactionDesc = (MyGuiControlMultilineText)controlsParent.Controls.GetControlByName("textFactionDesc");
            m_textFactionPriv = (MyGuiControlMultilineText)controlsParent.Controls.GetControlByName("textFactionPrivate");

            m_textFactionDesc.BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK;
            m_textFactionPriv.BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK;

            m_tableMembers = (MyGuiControlTable)controlsParent.Controls.GetControlByName("tableMembers");
            m_tableMembers.SetColumnComparison(1, (a, b) => ((int)((MyMemberComparerEnum)a.UserData)).CompareTo((int)((MyMemberComparerEnum)b.UserData)));
            m_tableMembers.ItemSelected += OnTableItemSelected;

            m_checkAutoAcceptMember = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkFactionMembersAcceptEveryone");
            m_checkAutoAcceptPeace  = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkFactionMembersAcceptPeace");

            m_checkAutoAcceptMember.SetToolTip(MySpaceTexts.TerminalTab_Factions_AutoAcceptToolTip);
            m_checkAutoAcceptPeace.SetToolTip(MySpaceTexts.TerminalTab_Factions_AutoAcceptRequestToolTip);

            m_checkAutoAcceptMember.IsCheckedChanged += OnAutoAcceptChanged;
            m_checkAutoAcceptPeace.IsCheckedChanged  += OnAutoAcceptChanged;

            m_buttonEdit       = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonEdit");
            m_buttonPromote    = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonPromote");
            m_buttonKick       = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonKick");
            m_buttonAcceptJoin = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonAcceptJoin");
            m_buttonDemote     = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonDemote");
            m_buttonAddNpc = (MyGuiControlButton)controlsParent.Controls.GetControlByName("buttonAddNpc");

            m_buttonEdit.TextEnum       = MyCommonTexts.Edit;
            m_buttonPromote.TextEnum    = MyCommonTexts.Promote;
            m_buttonKick.TextEnum       = MyCommonTexts.Kick;
            m_buttonAcceptJoin.TextEnum = MyCommonTexts.Accept;
            m_buttonDemote.TextEnum     = MyCommonTexts.Demote;
            m_buttonAddNpc.TextEnum = MySpaceTexts.AddNpcToFaction;
            m_buttonAddNpc.SetToolTip(MySpaceTexts.AddNpcToFactionHelp);

            m_buttonEdit.ButtonClicked       += OnCreateClicked;
            m_buttonPromote.ButtonClicked    += OnPromotePlayerClicked;
            m_buttonKick.ButtonClicked       += OnKickPlayerClicked;
            m_buttonAcceptJoin.ButtonClicked += OnAcceptJoinClicked;
            m_buttonDemote.ButtonClicked     += OnDemoteClicked;
            m_buttonAddNpc.ButtonClicked += OnNewNpcClicked;

            MySession.Static.Factions.FactionCreated           += OnFactionCreated;
            MySession.Static.Factions.FactionEdited            += OnFactionEdited;
            MySession.Static.Factions.FactionStateChanged      += OnFactionsStateChanged;
            MySession.Static.Factions.FactionAutoAcceptChanged += OnAutoAcceptChanged;

            Refresh();
        }

        public void Close()
        {
            UnregisterEvents();

            // left controls
            m_selectedFaction = null;

            m_tableFactions     = null;
            m_buttonCreate      = null;
            m_buttonJoin        = null;
            m_buttonCancelJoin  = null;
            m_buttonLeave       = null;
            m_buttonSendPeace   = null;
            m_buttonCancelPeace = null;
            m_buttonAcceptPeace = null;
            m_buttonMakeEnemy   = null;

            // right controls
            m_labelFactionName   = null;
            m_labelFactionDesc   = null;
            m_labelFactionPriv   = null;
            m_labelMembers       = null;
            m_labelAutoAcceptMember = null;
            m_labelAutoAcceptPeace  = null;
            m_checkAutoAcceptMember = null;
            m_checkAutoAcceptPeace  = null;
            m_textFactionDesc    = null;
            m_textFactionPriv    = null;
            m_tableMembers       = null;
            m_buttonKick         = null;
            m_buttonAcceptJoin   = null;

            m_controlsParent = null;
        }

        private void UnregisterEvents()
        {
            if (m_controlsParent == null)
                return;

            MySession.Static.Factions.FactionCreated           -= OnFactionCreated;
            MySession.Static.Factions.FactionEdited            -= OnFactionEdited;
            MySession.Static.Factions.FactionStateChanged      -= OnFactionsStateChanged;
            MySession.Static.Factions.FactionAutoAcceptChanged -= OnAutoAcceptChanged;

            m_tableFactions.ItemSelected -= OnFactionsTableItemSelected;
            m_tableMembers.ItemSelected  -= OnTableItemSelected;

            m_checkAutoAcceptMember.IsCheckedChanged -= OnAutoAcceptChanged;
            m_checkAutoAcceptPeace.IsCheckedChanged  -= OnAutoAcceptChanged;

            m_buttonCreate.ButtonClicked      -= OnCreateClicked;
            m_buttonJoin.ButtonClicked        -= OnJoinClicked;
            m_buttonCancelJoin.ButtonClicked  -= OnCancelJoinClicked;
            m_buttonLeave.ButtonClicked       -= OnLeaveClicked;
            m_buttonSendPeace.ButtonClicked   -= OnFriendClicked;
            m_buttonAcceptPeace.ButtonClicked -= OnAcceptFriendClicked;
            m_buttonMakeEnemy.ButtonClicked   -= OnEnemyClicked;

            m_buttonEdit.ButtonClicked       -= OnCreateClicked;
            m_buttonPromote.ButtonClicked    -= OnPromotePlayerClicked;
            m_buttonKick.ButtonClicked       -= OnKickPlayerClicked;
            m_buttonAcceptJoin.ButtonClicked -= OnAcceptJoinClicked;
            m_buttonDemote.ButtonClicked     -= OnDemoteClicked;
            m_buttonAddNpc.ButtonClicked -= OnNewNpcClicked;
        }

        private void OnFactionsTableItemSelected(MyGuiControlTable sender, Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs args)
        {
            if (sender.SelectedRow != null)
            {
                m_selectedFaction = (MyFaction)sender.SelectedRow.UserData;

                m_labelFactionName.Text = string.Format("{0}.{1}", m_selectedFaction.Tag, m_selectedFaction.Name);
                m_textFactionDesc.Text  = new StringBuilder(m_selectedFaction.Description);
                m_textFactionPriv.Text  = new StringBuilder(m_selectedFaction.PrivateInfo);

                RefreshTableMembers();
            }
            m_tableMembers.Sort(false);

            RefreshJoinButton();
            RefreshDiplomacyButtons();
            RefreshFactionProperties();
        }

        private void OnTableItemSelected(MyGuiControlTable sender, Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs args)
        {
            RefreshRightSideButtons(sender.SelectedRow);
        }

        #region Left Side Buttons

        private void OnCreateClicked(MyGuiControlButton sender)
        {
            //MyGuiSandbox.AddScreen(new MyGuiScreenCreateOrEditFaction(ref m_userFaction));
            var screen = (MyGuiScreenCreateOrEditFaction) MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.CreateFactionScreen);
            screen.Init(ref m_userFaction);
            MyGuiSandbox.AddScreen(screen);

        }

        private void OnJoinClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.SendJoinRequest(m_selectedFaction.FactionId, MySession.Static.LocalPlayerId);
        }

        private void OnCancelJoinClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.CancelJoinRequest(m_selectedFaction.FactionId, MySession.Static.LocalPlayerId);
        }

        private void OnLeaveClicked(MyGuiControlButton sender)
        {
            if (m_selectedFaction.FactionId == m_userFaction.FactionId)
                ShowConfirmBox(
                    new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxConfirmFactionsLeave, m_userFaction.Name),
                    LeaveFaction
                );
        }

        private void LeaveFaction()
        {
            if (m_userFaction == null) // player can be kicked while confirming leave
                return;

            MyFactionCollection.MemberLeaves(m_userFaction.FactionId, MySession.Static.LocalPlayerId);
            m_userFaction = null;
            Refresh();
        }

        private void OnFriendClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.SendPeaceRequest(m_userFaction.FactionId, m_selectedFaction.FactionId);
        }

        private void OnAcceptFriendClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.AcceptPeace(m_userFaction.FactionId, m_selectedFaction.FactionId);
        }

        private void OnCancelPeaceRequestClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.CancelPeaceRequest(m_userFaction.FactionId, m_selectedFaction.FactionId);
        }

        private void OnEnemyClicked(MyGuiControlButton sender)
        {
            MyFactionCollection.DeclareWar(m_userFaction.FactionId, m_selectedFaction.FactionId);
        }

        #endregion

        #region Right Side Buttons

        private void OnAutoAcceptChanged(MyGuiControlCheckbox sender)
        {
            if (m_userFaction != null)
                MySession.Static.Factions.ChangeAutoAccept(m_userFaction.FactionId, MySession.Static.LocalPlayerId, m_checkAutoAcceptMember.IsChecked, m_checkAutoAcceptPeace.IsChecked);
        }

        private void OnAutoAcceptChanged(long factionId, bool autoAcceptMember, bool autoAcceptPeace)
        {
            RefreshFactionProperties();
        }

        private void OnPromotePlayerClicked(MyGuiControlButton sender)
        {
            if (m_tableMembers.SelectedRow != null)
                ShowConfirmBox(new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxConfirmFactionsPromote, m_selectedUserName), PromotePlayer);
        }

        private void PromotePlayer()
        {
            MyFactionCollection.PromoteMember(m_userFaction.FactionId, m_selectedUserId);
        }

        private void OnKickPlayerClicked(MyGuiControlButton sender)
        {
            if (m_tableMembers.SelectedRow != null)
                ShowConfirmBox(new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxConfirmFactionsKickPlayer, m_selectedUserName), KickPlayer);
        }

        private void KickPlayer()
        {
            MyFactionCollection.KickMember(m_userFaction.FactionId, m_selectedUserId);
        }

        private void OnAcceptJoinClicked(MyGuiControlButton sender)
        {
            if (m_tableMembers.SelectedRow != null)
                ShowConfirmBox(new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxConfirmFactionsAcceptJoin, m_selectedUserName), AcceptJoin);
        }

        private void AcceptJoin()
        {
            MyFactionCollection.AcceptJoin(m_userFaction.FactionId, m_selectedUserId);
        }

        private void OnDemoteClicked(MyGuiControlButton sender)
        {
            if (m_tableMembers.SelectedRow != null)
                ShowConfirmBox(new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxConfirmFactionsDemote, m_selectedUserName), Demote);
        }

        private void Demote()
        {
            MyFactionCollection.DemoteMember(m_userFaction.FactionId, m_selectedUserId);
        }

        private void OnNewNpcClicked(MyGuiControlButton sender)
        {
            string npcName = m_userFaction.Tag + " NPC" + MyRandom.Instance.Next(1000, 9999);
            var identity = Sync.Players.CreateNewIdentity(npcName);
            Sync.Players.MarkIdentityAsNPC(identity.IdentityId);

            MyFactionCollection.SendJoinRequest(m_userFaction.FactionId, identity.IdentityId);
        }

        #endregion

        #region Refresh methods

        private void Refresh()
        {
            RefreshUserInfo();
            RefreshCreateButton();
            RefreshJoinButton();
            RefreshDiplomacyButtons();
            RefreshRightSideButtons(null);
            RefreshFactionProperties();
        }

        private void RefreshUserInfo()
        {
            m_userIsFounder = false;
            m_userIsLeader  = false;
            m_userFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);

            if (m_userFaction != null)
            {
                m_userIsFounder = m_userFaction.IsFounder(MySession.Static.LocalPlayerId);
                m_userIsLeader = m_userFaction.IsLeader(MySession.Static.LocalPlayerId);
            }
        }

        private void RefreshCreateButton()
        {
            if(m_buttonCreate == null)
            {
                return;
            }

            if (m_userFaction != null)
            {
                m_buttonCreate.Enabled = false;
                m_buttonCreate.SetToolTip(MySpaceTexts.TerminalTab_Factions_BeforeCreateLeave);
            }
            else
            {
                m_buttonCreate.Enabled = true;
                m_buttonCreate.SetToolTip(MySpaceTexts.TerminalTab_Factions_CreateToolTip);
            }
        }

        private void RefreshJoinButton()
        {
            m_buttonLeave.Visible      = false;
            m_buttonJoin.Visible       = false;
            m_buttonCancelJoin.Visible = false;

            m_buttonLeave.Enabled      = false;
            m_buttonJoin.Enabled       = false;
            m_buttonCancelJoin.Enabled = false;

            if (m_userFaction != null)
            {
                m_buttonLeave.Visible = true;
                m_buttonLeave.Enabled = (m_tableFactions.SelectedRow != null && m_tableFactions.SelectedRow.UserData == m_userFaction) ? true : false;
            }
            else
            {
                if (m_tableFactions.SelectedRow != null)
                {
                    if (m_selectedFaction.JoinRequests.ContainsKey(MySession.Static.LocalPlayerId))
                    {
                        m_buttonCancelJoin.Visible = true;
                        m_buttonCancelJoin.Enabled = true;
                        m_buttonJoin.Visible       = false;
                    }
                    else
                    {

                        if (m_selectedFaction.AcceptHumans)
                        {
                            m_buttonJoin.Visible = true;
                            m_buttonJoin.Enabled = true;
                        }
                        else
                        {
                            m_buttonJoin.Visible = true;
                            m_buttonJoin.Enabled = false;
                        }
                    }
                }
                else
                {
                    m_buttonJoin.Visible = true;
                    m_buttonJoin.Enabled = false;
                }
            }
        }

        private void RefreshDiplomacyButtons()
        {
            m_buttonSendPeace.Enabled   = false;
            m_buttonCancelPeace.Enabled = false;
            m_buttonAcceptPeace.Enabled = false;
            m_buttonMakeEnemy.Enabled   = false;

            m_buttonCancelPeace.Visible = false;
            m_buttonAcceptPeace.Visible = false;

            if (m_userIsLeader && m_selectedFaction != null && m_selectedFaction.FactionId != m_userFaction.FactionId)
            {
                if (MySession.Static.Factions.AreFactionsEnemies(m_userFaction.FactionId, m_selectedFaction.FactionId))
                {
                    if (MySession.Static.Factions.IsPeaceRequestStateSent(m_userFaction.FactionId, m_selectedFaction.FactionId))
                    {
                        m_buttonSendPeace.Visible   = false;
                        m_buttonCancelPeace.Visible = true;
                        m_buttonCancelPeace.Enabled = true;
                    }
                    else if (MySession.Static.Factions.IsPeaceRequestStatePending(m_userFaction.FactionId, m_selectedFaction.FactionId))
                    {
                        m_buttonSendPeace.Visible   = false;
                        m_buttonAcceptPeace.Visible = true;
                        m_buttonAcceptPeace.Enabled = true;
                    }
                    else
                    {
                        m_buttonSendPeace.Visible = true;
                        m_buttonSendPeace.Enabled = true;
                    }
                }
                else
                    m_buttonMakeEnemy.Enabled = true;
            }
        }

        private void RefreshRightSideButtons(MyGuiControlTable.Row selected)
        {
            m_buttonPromote.Enabled    = false;
            m_buttonKick.Enabled       = false;
            m_buttonAcceptJoin.Enabled = false;
            m_buttonDemote.Enabled     = false;

            if (selected != null)
            {
                var data           = (MyFactionMember)selected.UserData;
                m_selectedUserId   = data.PlayerId;
                var identity       = Sync.Players.TryGetIdentity(data.PlayerId);
                m_selectedUserName = identity.DisplayName;

                if (m_selectedUserId != MySession.Static.LocalPlayerId)
                {
                    if (m_userIsFounder && m_userFaction.IsLeader(m_selectedUserId))
                    {
                        m_buttonKick.Enabled   = true;
                        m_buttonDemote.Enabled = true;
                    }
                    else if (m_userIsFounder && m_userFaction.IsMember(m_selectedUserId))
                    {
                        m_buttonKick.Enabled    = true;
                        m_buttonPromote.Enabled = true;
                    }
                    else if (m_userIsLeader
                                &&  m_userFaction.IsMember(m_selectedUserId) 
                                && !m_userFaction.IsLeader(m_selectedUserId)
                                && !m_userFaction.IsFounder(m_selectedUserId))
                    {
                        m_buttonKick.Enabled = true;
                    }
                    else if ((m_userIsLeader || m_userIsFounder) && m_userFaction.JoinRequests.ContainsKey(m_selectedUserId))
                        m_buttonAcceptJoin.Enabled = true;
                }
            }
        }

        private void RefreshFactionProperties()
        {
            m_checkAutoAcceptMember.IsCheckedChanged -= OnAutoAcceptChanged;
            m_checkAutoAcceptPeace.IsCheckedChanged  -= OnAutoAcceptChanged;

            m_labelFactionName.Visible = false;
            m_labelFactionDesc.Visible = false;
            m_labelFactionPriv.Visible = false;
            m_labelMembers.Visible     = false;
            m_labelAutoAcceptMember.Visible = false;
            m_labelAutoAcceptPeace.Visible  = false;
            m_checkAutoAcceptMember.Visible = false;
            m_checkAutoAcceptPeace.Visible  = false;
            m_textFactionDesc.Visible  = false;
            m_textFactionPriv.Visible  = false;
            m_tableMembers.Visible     = false;
            m_buttonEdit.Visible       = false;
            m_buttonKick.Visible       = false;
            m_buttonPromote.Visible    = false;
            m_buttonDemote.Visible     = false;
            m_buttonAcceptJoin.Visible = false;
            m_buttonAddNpc.Visible = false;

            if (m_tableFactions.SelectedRow != null)
            {
                m_selectedFaction = (MyFaction)m_tableFactions.SelectedRow.UserData;

                m_labelFactionName.Text = string.Format("{0}.{1}", m_selectedFaction.Tag, m_selectedFaction.Name);
                m_textFactionDesc.Text  = new StringBuilder(m_selectedFaction.Description);
                m_textFactionPriv.Text  = new StringBuilder(m_selectedFaction.PrivateInfo);

                m_checkAutoAcceptMember.IsChecked = m_selectedFaction.AutoAcceptMember;
                m_checkAutoAcceptPeace.IsChecked  = m_selectedFaction.AutoAcceptPeace;

                m_labelFactionName.Visible = true;
                m_labelFactionDesc.Visible = true;
                m_textFactionDesc.Visible  = true;
                m_labelMembers.Visible     = true;
                m_tableMembers.Visible     = true;

                if (m_userFaction != null && m_userFaction.FactionId == m_selectedFaction.FactionId)
                {
                    m_labelFactionPriv.Visible = true;
                    m_textFactionPriv.Visible  = true;

                    if (m_userIsLeader)
                    {
                        m_labelAutoAcceptMember.Visible = true;
                        m_labelAutoAcceptPeace.Visible  = true;
                        m_checkAutoAcceptMember.Visible = true;
                        m_checkAutoAcceptPeace.Visible  = true;
                        m_buttonKick.Visible       = true;
                        m_buttonAcceptJoin.Visible = true;
                        m_buttonEdit.Visible       = true;
                        m_buttonPromote.Visible    = true;
                        m_buttonDemote.Visible     = true;
                        if (MySession.Static.IsUserSpaceMaster(MySession.Static.LocalHumanPlayer.Client.SteamUserId))
                            m_buttonAddNpc.Visible = true;
                    }
                }
            }
            else
                m_tableMembers.Clear();

            m_checkAutoAcceptMember.IsCheckedChanged += OnAutoAcceptChanged;
            m_checkAutoAcceptPeace.IsCheckedChanged  += OnAutoAcceptChanged;
        }

        private void RefreshTableFactions()
        {
            m_tableFactions.Clear();

            foreach (var entry in MySession.Static.Factions)
            {
                var faction = entry.Value;

                Color? color = null;
                MyGuiHighlightTexture? icon = null;
                String iconToolTip = null;

                if (m_userFaction == null)
                {
                    color = COLOR_CUSTOM_RED;

                    if (faction.JoinRequests.ContainsKey(MySession.Static.LocalPlayerId))
                    {
                        icon = MyGuiConstants.TEXTURE_ICON_SENT_JOIN_REQUEST;
                        iconToolTip = MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_SentJoinToolTip);
                    }
                }
                else
                {
                    if (m_userFaction.FactionId == faction.FactionId)
                        color = COLOR_CUSTOM_GREEN;
                    else if (MySession.Static.Factions.AreFactionsEnemies(m_userFaction.FactionId, faction.FactionId))
                        color = COLOR_CUSTOM_RED;

                    if (MySession.Static.Factions.IsPeaceRequestStateSent(m_userFaction.FactionId, faction.FactionId))
                    {
                        icon = MyGuiConstants.TEXTURE_ICON_SENT_WHITE_FLAG;
                        iconToolTip = MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_SentPeace);
                    }
                    else if (MySession.Static.Factions.IsPeaceRequestStatePending(m_userFaction.FactionId, faction.FactionId))
                    {
                        icon = MyGuiConstants.TEXTURE_ICON_WHITE_FLAG;
                        iconToolTip = MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_PendingPeace);
                    }
                }
                AddFaction(faction, color, icon, iconToolTip);
            }
            m_tableFactions.Sort(false);
        }

        private void RefreshTableMembers()
        {
            m_tableMembers.Clear();

            foreach (var entry in m_selectedFaction.Members)
            {
                var member      = entry.Value;

                var identity = Sync.Players.TryGetIdentity(member.PlayerId);
                //System.Diagnostics.Debug.Assert(identity != null, "Faction member is not known identity!");
                if (identity == null)
                    continue;

                var row         = new MyGuiControlTable.Row(member);
                var compare     = MyMemberComparerEnum.Member;
                var statusEnum  = MyCommonTexts.Member;
                Color? txtColor = null;

                if (m_selectedFaction.IsFounder(member.PlayerId))
                {
                    compare    = MyMemberComparerEnum.Founder;
                    statusEnum = MyCommonTexts.Founder;
                }
                else if (m_selectedFaction.IsLeader(member.PlayerId))
                {
                    compare    = MyMemberComparerEnum.Leader;
                    statusEnum = MyCommonTexts.Leader;
                }
                else if (m_selectedFaction.JoinRequests.ContainsKey(member.PlayerId))
                {
                    txtColor   = COLOR_CUSTOM_GREY;
                    compare    = MyMemberComparerEnum.Applicant;
                    statusEnum = MyCommonTexts.Applicant;
                }

                row.AddCell(new MyGuiControlTable.Cell(text:    new StringBuilder(identity.DisplayName),
                                                       toolTip: identity.DisplayName,
                                                       userData: entry, textColor: txtColor));
                row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.Get(statusEnum), userData: compare, textColor: txtColor));
                m_tableMembers.Add(row);
            }

            foreach (var entry in m_selectedFaction.JoinRequests)
            {
                var request = entry.Value;
                var row = new MyGuiControlTable.Row(request);

                var identity = Sync.Players.TryGetIdentity(request.PlayerId);
                System.Diagnostics.Debug.Assert(identity != null, "Player is not in allplayers list!");
                if (identity != null)
                {
                    row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(identity.DisplayName),
                                                           toolTip: identity.DisplayName,
                                                           userData: entry, textColor: COLOR_CUSTOM_GREY));

                    row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.Get(MyCommonTexts.Applicant),
                                                           userData: MyMemberComparerEnum.Applicant,
                                                           textColor: COLOR_CUSTOM_GREY));
                    m_tableMembers.Add(row);
                }
            }
        }

        #endregion

        private void OnFactionCreated(long insertedId)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(insertedId);
            AddFaction(faction, (faction.IsMember(MySession.Static.LocalPlayerId)) ? COLOR_CUSTOM_GREEN : COLOR_CUSTOM_RED);
            Refresh();            
            RefreshTableFactions();
            m_tableFactions.Sort(false);
            m_tableFactions.SelectedRowIndex = m_tableFactions.FindIndex((row) => ((MyFaction)row.UserData).FactionId == insertedId);
            OnFactionsTableItemSelected(m_tableFactions, new Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs());
        }

        private void OnFactionEdited(long editedId)
        {
            RefreshTableFactions();
            m_tableFactions.SelectedRowIndex = m_tableFactions.FindIndex((row) => ((MyFaction)row.UserData).FactionId == editedId);
            OnFactionsTableItemSelected(m_tableFactions, new Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs());
            Refresh();
        }

        private void OnFactionsStateChanged(MyFactionCollection.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            System.Diagnostics.Debug.Assert(MySession.Static != null);
            if (MySession.Static == null)
                return;

            System.Diagnostics.Debug.Assert(MySession.Static.Factions != null);
            if (MySession.Static.Factions == null)
                return;

            System.Diagnostics.Debug.Assert(m_tableFactions != null);
            if (m_tableFactions == null)
                return;

            var fromFaction = MySession.Static.Factions.TryGetFactionById(fromFactionId);
            var toFaction   = MySession.Static.Factions.TryGetFactionById(toFactionId);

            switch (action)
            {
                case MyFactionCollection.MyFactionStateChange.SendPeaceRequest:
                    {
                        System.Diagnostics.Debug.Assert(m_userFaction != null);
                        if (m_userFaction == null)
                            return;

                        if (m_userFaction.FactionId == fromFactionId)
                        {
                            RemoveFaction(toFactionId);
                            AddFaction(toFaction, COLOR_CUSTOM_RED, MyGuiConstants.TEXTURE_ICON_SENT_WHITE_FLAG, MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_SentPeace));
                        }
                        else if (m_userFaction.FactionId == toFactionId)
                        {
                            RemoveFaction(fromFactionId);
                            AddFaction(fromFaction, COLOR_CUSTOM_RED, MyGuiConstants.TEXTURE_ICON_WHITE_FLAG, MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_PendingPeace));
                        }
                        break;
                    }

                case MyFactionCollection.MyFactionStateChange.AcceptPeace:
                    {
                        System.Diagnostics.Debug.Assert(m_userFaction != null);
                        if (m_userFaction == null)
                            return;

                        if (m_userFaction.FactionId == fromFactionId)
                        {
                            RemoveFaction(toFactionId);
                            AddFaction(toFaction);
                        }
                        else if (m_userFaction.FactionId == toFactionId)
                        {
                            RemoveFaction(fromFactionId);
                            AddFaction(fromFaction);
                        }
                        break;
                    }

                case MyFactionCollection.MyFactionStateChange.CancelPeaceRequest:
                case MyFactionCollection.MyFactionStateChange.DeclareWar:
                    {
                        if (m_userFaction == null)
                            return;

                        if (m_userFaction.FactionId == fromFactionId)
                        {
                            RemoveFaction(toFactionId);
                            AddFaction(toFaction, COLOR_CUSTOM_RED);
                        }
                        else if (m_userFaction.FactionId == toFactionId)
                        {
                            RemoveFaction(fromFactionId);
                            AddFaction(fromFaction, COLOR_CUSTOM_RED);
                        }
                        break;
                    }

                case MyFactionCollection.MyFactionStateChange.RemoveFaction:
                    RemoveFaction(toFactionId);
                    break;

                default:
                    OnMemberStateChanged(action, fromFaction, playerId);
                    break;
            }
            m_tableFactions.Sort(false);
            m_tableFactions.SelectedRowIndex = m_tableFactions.FindIndex((row) => ((MyFaction)row.UserData).FactionId == toFactionId);
            OnFactionsTableItemSelected(m_tableFactions, new Sandbox.Graphics.GUI.MyGuiControlTable.EventArgs());
            Refresh();
        }

        private void RemoveFaction(long factionId)
        {
            System.Diagnostics.Debug.Assert(m_tableFactions != null);
            if (m_tableFactions == null)
                return;

            m_tableFactions.Remove((row) => ((((MyFaction)row.UserData).FactionId) == factionId));
        }

        private void AddFaction(IMyFaction faction, Color? color = null, MyGuiHighlightTexture? icon = null, String iconToolTip = null)
        {
            System.Diagnostics.Debug.Assert(m_tableFactions != null);
            if (m_tableFactions == null)
                return;

            var row  = new MyGuiControlTable.Row(faction);
            var tag  = new StringBuilder(faction.Tag);
            var name = new StringBuilder(faction.Name);
            row.AddCell(new MyGuiControlTable.Cell(text: tag, userData: tag, textColor: color));
            row.AddCell(new MyGuiControlTable.Cell(text: name, userData: name, textColor: color, toolTip: faction.Name));
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(), icon: icon, toolTip: iconToolTip));
            m_tableFactions.Add(row);
        }

        private void OnMemberStateChanged(MyFactionCollection.MyFactionStateChange action, IMyFaction fromFaction, long playerId)
        {
            var identity = Sync.Players.TryGetIdentity(playerId);
            System.Diagnostics.Debug.Assert(identity != null, "Identity does not exist!");
            if (identity == null)
            {
                MyLog.Default.WriteLine("ERROR: Faction " + fromFaction.Name + " member " + playerId + " does not exists! ");
                return;
            }

            RemoveMember(playerId);

            switch (action)
            {
                case MyFactionCollection.MyFactionStateChange.FactionMemberPromote:
                    AddMember(playerId, identity.DisplayName, true,  MyMemberComparerEnum.Leader, MyCommonTexts.Leader);
                    break;

                case MyFactionCollection.MyFactionStateChange.FactionMemberCancelJoin:
                case MyFactionCollection.MyFactionStateChange.FactionMemberLeave:
                case MyFactionCollection.MyFactionStateChange.FactionMemberKick: break;

                case MyFactionCollection.MyFactionStateChange.FactionMemberAcceptJoin:
                case MyFactionCollection.MyFactionStateChange.FactionMemberDemote:
                    AddMember(playerId, identity.DisplayName, false, MyMemberComparerEnum.Member, MyCommonTexts.Member);
                    break;

                case MyFactionCollection.MyFactionStateChange.FactionMemberSendJoin:
                    AddMember(playerId, identity.DisplayName, false, MyMemberComparerEnum.Applicant, MyCommonTexts.Applicant, COLOR_CUSTOM_GREY);
                    break;
            }
            RefreshUserInfo();
            RefreshTableFactions();
            m_tableMembers.Sort(false);
        }

        private void RemoveMember(long playerId)
        {
            m_tableMembers.Remove((row) => ((((MyFactionMember)row.UserData).PlayerId) == playerId));
        }

        private void AddMember(long playerId, string playerName, bool isLeader, MyMemberComparerEnum status, MyStringId textEnum, Color? color = null)
        {
            var row = new MyGuiControlTable.Row(new MyFactionMember(playerId, isLeader));

            row.AddCell(new MyGuiControlTable.Cell(text:    new StringBuilder(playerName),
                                                   toolTip: playerName,
                                                   userData: playerId, textColor: color));
            row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.Get(textEnum), userData: status, textColor: color));
            m_tableMembers.Add(row);
        }

        private void ShowErrorBox(StringBuilder text)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                messageText: text);
            messageBox.SkipTransition      = true;
            messageBox.CloseBeforeCallback = true;
            messageBox.CanHideOthers       = false;
            MyGuiSandbox.AddScreen(messageBox);
        }

        private void ShowConfirmBox(StringBuilder text, Action callback)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                messageText: text,
                focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        callback();
                });
            messageBox.SkipTransition      = true;
            messageBox.CloseBeforeCallback = true;
            messageBox.CanHideOthers       = false;
            MyGuiSandbox.AddScreen(messageBox);
        }
    }
}

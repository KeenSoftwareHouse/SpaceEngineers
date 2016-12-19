using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using VRage;
using VRageMath;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;

using Sandbox.Engine.Utils;
using Sandbox.Common.ObjectBuilders;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlGenericFunctionalBlock : MyGuiControlBase
    {
        private List<ITerminalControl> m_currentControls = new List<ITerminalControl>();

        private MyGuiControlSeparatorList m_separatorList;
        private MyGuiControlList m_terminalControlList;
        private MyGuiControlMultilineText m_blockPropertiesMultilineText;

        private MyTerminalBlock[] m_currentBlocks;
        private Dictionary<ITerminalControl, int> m_tmpControlDictionary = new Dictionary<ITerminalControl, int>(InstanceComparer<ITerminalControl>.Default);

        private MyGuiControlCombobox m_transferToCombobox;
        private MyGuiControlCombobox m_shareModeCombobox;
        private MyGuiControlLabel m_ownershipLabel;
        private MyGuiControlLabel m_ownerLabel;
        private MyGuiControlLabel m_transferToLabel;
        private MyGuiControlButton m_npcButton;

        List<MyCubeGrid.MySingleOwnershipRequest> m_requests = new List<MyCubeGrid.MySingleOwnershipRequest>();

        bool m_askForConfirmation = true;
        bool m_canChangeShareMode = true;

        internal MyGuiControlGenericFunctionalBlock(MyTerminalBlock block)
            : this(new MyTerminalBlock[] { block } )
        {
        }

        internal MyGuiControlGenericFunctionalBlock(MyTerminalBlock[] blocks) :
            base(canHaveFocus: true,
                  allowFocusingElements: true,
                  isActiveControl: false)
        {
            this.m_currentBlocks = blocks;

            m_separatorList = new MyGuiControlSeparatorList();
            Elements.Add(m_separatorList);

            m_terminalControlList = new MyGuiControlList();
            m_terminalControlList.VisualStyle = MyGuiControlListStyleEnum.Simple;
            m_terminalControlList.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            m_terminalControlList.Position = new Vector2(0.1f, 0.1f);
            Elements.Add(m_terminalControlList);

            m_blockPropertiesMultilineText = new MyGuiControlMultilineText(
                position: new Vector2(0.049f, -0.195f),
                size: new Vector2(0.39f, 0.635f),
                font: MyFontEnum.Blue,
                textScale: 0.85f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
                );
            m_blockPropertiesMultilineText.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_blockPropertiesMultilineText.Text = new StringBuilder();
            Elements.Add(m_blockPropertiesMultilineText);

            m_transferToCombobox = new MyGuiControlCombobox(
                Vector2.Zero,
                new Vector2(0.15f, 0.1f),
                null, null);
            m_transferToCombobox.ItemSelected += m_transferToCombobox_ItemSelected;
            Elements.Add(m_transferToCombobox);

            m_shareModeCombobox = new MyGuiControlCombobox(
            Vector2.Zero,
            new Vector2(0.25f, 0.1f),
            null, null);
            m_shareModeCombobox.ItemSelected += m_shareModeCombobox_ItemSelected;
            Elements.Add(m_shareModeCombobox);

            m_ownershipLabel = new MyGuiControlLabel(
                Vector2.Zero, null, MyTexts.GetString(MySpaceTexts.BlockOwner_Owner) + ":");
            Elements.Add(m_ownershipLabel);

            m_ownerLabel = new MyGuiControlLabel(
                Vector2.Zero, null, String.Empty);
            Elements.Add(m_ownerLabel);

            m_transferToLabel = new MyGuiControlLabel(
                Vector2.Zero, null, MyTexts.GetString(MySpaceTexts.BlockOwner_TransferTo));
            Elements.Add(m_transferToLabel);

            if (MySession.Static.CreativeMode)
            {
                var topLeftRelative = Vector2.One * -0.5f;
                Vector2 leftColumnSize = new Vector2(0.3f, 0.55f);
                var position = topLeftRelative + new Vector2(leftColumnSize.X + 0.503f, 0.42f);

                m_npcButton = new MyGuiControlButton(
                    position,
                    MyGuiControlButtonStyleEnum.Tiny,
                    new Vector2(0.1f, 0.1f),
                    null, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, MyTexts.GetString(MyCommonTexts.AddNewNPC), new StringBuilder("+"),
                    MyGuiConstants.DEFAULT_TEXT_SCALE, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE,
                    OnNewNpcClick, GuiSounds.MouseClick, 0.75f);
                Elements.Add(m_npcButton);
            }


            RecreateBlockControls();
            RecreateOwnershipControls();

            if (m_currentBlocks.Length > 0)
            {
                m_currentBlocks[0].PropertiesChanged += block_PropertiesChanged;
            }

            foreach (var block in m_currentBlocks)
            {
                block.OwnershipChanged += block_OwnershipChanged;
                block.VisibilityChanged += block_VisibilityChanged;
            }

            Sync.Players.IdentitiesChanged += Players_IdentitiesChanged;

            UpdateDetailedInfo();

            Size = new Vector2(0.595f, 0.64f);
        }

        void Players_IdentitiesChanged()
        {
            UpdateOwnerGui();
        }

        void block_OwnershipChanged(MyTerminalBlock sender)
        {
            if (m_canChangeShareMode)
            {
                RecreateOwnershipControls();
                UpdateOwnerGui();
            }
        }

        public override void OnRemoving()
        {
            m_currentControls.Clear();

            if (m_currentBlocks.Length > 0)
            {
                m_currentBlocks[0].PropertiesChanged -= block_PropertiesChanged;
            }

            foreach (var block in m_currentBlocks)
            {
                block.OwnershipChanged -= block_OwnershipChanged;
                block.VisibilityChanged -= block_VisibilityChanged;
            }

            Sync.Players.IdentitiesChanged -= Players_IdentitiesChanged;

            base.OnRemoving();
        }

        private void block_VisibilityChanged(MyTerminalBlock obj)
        {
            foreach (var control in m_currentControls)
            {
                if (control.GetGuiControl().Visible != control.IsVisible(obj))
                    control.GetGuiControl().Visible = control.IsVisible(obj);
            }
        }

        private void block_PropertiesChanged(MyTerminalBlock sender)
        {
            if (m_canChangeShareMode == false)
                return;

            ProfilerShort.Begin("MyGuiControlGenericFun....block_Propert...");

            foreach (var control in m_currentControls)
            {
                ProfilerShort.Begin("UpdateVisual");
                control.UpdateVisual();
                ProfilerShort.End();
            }

            UpdateDetailedInfo();

            ProfilerShort.End();
        }

        private void UpdateDetailedInfo()
        {
            ProfilerShort.Begin("UpdateDetailedInfo");
            m_blockPropertiesMultilineText.Text.Clear();
            if (m_currentBlocks.Length == 1)
            {
                var block = m_currentBlocks[0];

                m_blockPropertiesMultilineText.Text.AppendStringBuilder(block.DetailedInfo);
                if(block.CustomInfo.Length > 0)
                {
                    m_blockPropertiesMultilineText.Text.TrimTrailingWhitespace().AppendLine();
                    m_blockPropertiesMultilineText.Text.AppendStringBuilder(block.CustomInfo);
                }

                m_blockPropertiesMultilineText.Text.Autowrap(0.29f, MyFontEnum.Blue, MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiManager.LanguageTextScale);
                m_blockPropertiesMultilineText.RefreshText(false);
            }
            ProfilerShort.End();
        }

        MyScenarioBuildingBlock dummy = new MyScenarioBuildingBlock();
        private void RecreateBlockControls()
        {
            m_currentControls.Clear();
            m_terminalControlList.Controls.Clear();

            try
            {
                foreach (var block in m_currentBlocks)
                {
                    var type = block.GetType();
                    foreach(var control in MyTerminalControls.Static.GetControls(block))
                    {
                        int num;
                        m_tmpControlDictionary.TryGetValue(control, out num);
                        m_tmpControlDictionary[control] = num + (control.IsVisible(block) ? 1 : 0);
                    }
                }

                if (MySession.Static.Settings.ScenarioEditMode && MyFakes.ENABLE_NEW_TRIGGERS)
                {
                    var scenarioType = typeof(MyTerminalBlock);
                    var c = MyTerminalControlFactory.GetControls(scenarioType);
                    foreach (var control in c)
                        m_tmpControlDictionary[control] = m_currentBlocks.Length;
                }

                int blockCount = m_currentBlocks.Length;
                foreach (var item in m_tmpControlDictionary)
                {
                    bool visibleAtLeastOnce = item.Value != 0;

                    if (blockCount > 1 && !item.Key.SupportsMultipleBlocks)
                        continue;

                    if (item.Value == blockCount)
                    {
                        item.Key.GetGuiControl().Visible = visibleAtLeastOnce;

                        m_terminalControlList.Controls.Add(item.Key.GetGuiControl());
                        item.Key.TargetBlocks = m_currentBlocks;
                        item.Key.UpdateVisual();

                        m_currentControls.Add(item.Key);
                    }
                }
            }
            finally
            {
                m_tmpControlDictionary.Clear();
            }
        }

        void RecreateOwnershipControls()
        {
            bool ownershipBlockPresent = false;
            foreach (var block in m_currentBlocks)
            {
                if (block.IDModule != null)
                {
                    ownershipBlockPresent = true;
                }
            }

            if (ownershipBlockPresent && MyFakes.SHOW_FACTIONS_GUI)
            {
                m_ownershipLabel.Visible = true;
                m_ownerLabel.Visible = true;
                m_transferToLabel.Visible = true;
                m_transferToCombobox.Visible = true;
                m_shareModeCombobox.Visible = true;

                if (m_npcButton != null)
                    m_npcButton.Visible = true;
            }
            else
            {
                m_ownershipLabel.Visible = false;
                m_ownerLabel.Visible = false;
                m_transferToLabel.Visible = false;
                m_transferToCombobox.Visible = false;
                m_shareModeCombobox.Visible = false;

                if (m_npcButton != null)
                    m_npcButton.Visible = false;
                return;
            }


            var topLeftRelative = Vector2.One * -0.5f;
            Vector2 leftColumnSize = new Vector2(0.3f, 0.55f);

            m_ownershipLabel.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.22f, 0.38f);

            m_ownerLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            m_ownerLabel.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.46f, 0.38f);

            m_transferToLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_transferToLabel.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.22f, 0.405f);

            m_transferToCombobox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_transferToCombobox.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.32f, 0.4f);
            
            m_shareModeCombobox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_shareModeCombobox.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.22f, 0.45f);

            m_shareModeCombobox.ClearItems();
            m_shareModeCombobox.AddItem((long)MyOwnershipShareModeEnum.None, MyTexts.Get(MySpaceTexts.BlockOwner_ShareNone));
            m_shareModeCombobox.AddItem((long)MyOwnershipShareModeEnum.Faction, MyTexts.Get(MySpaceTexts.BlockOwner_ShareFaction));
            m_shareModeCombobox.AddItem((long)MyOwnershipShareModeEnum.All, MyTexts.Get(MySpaceTexts.BlockOwner_ShareAll));

            UpdateOwnerGui();
        }

        public override MyGuiControlBase HandleInput()
        {
            base.HandleInput();
            return base.HandleInputElements();
        }

        protected override void OnSizeChanged()
        {
            if (m_currentBlocks.Length == 0)
                return;

            var topLeftRelative = Size * -0.5f;
            Vector2 leftColumnSize = new Vector2(0.3f, 0.55f);
            m_separatorList.Clear();
            m_separatorList.AddHorizontal(topLeftRelative, Size.X);
            m_separatorList.AddVertical(topLeftRelative + new Vector2(leftColumnSize.X, 0), Size.Y);
            m_separatorList.AddHorizontal(topLeftRelative + new Vector2(leftColumnSize.X + 0.008f, 0.18f), leftColumnSize.X * 0.96f);//above ownership
            

            m_terminalControlList.Position = topLeftRelative + new Vector2(leftColumnSize.X * 0.5f, 0.01f);
            m_terminalControlList.Size = new Vector2(leftColumnSize.X, 0.625f);

            float propertiesOffset = 0.06f;

            if (MyFakes.SHOW_FACTIONS_GUI)
            {
                foreach (var block in m_currentBlocks)
                {
                    if (block.IDModule != null)
                    {
                        propertiesOffset = 0.22f;
                        m_separatorList.AddHorizontal(topLeftRelative + new Vector2(leftColumnSize.X + 0.008f, propertiesOffset + 0.11f), leftColumnSize.X * 0.96f);
                        break;
                    }
                }
            }

            m_blockPropertiesMultilineText.Position = topLeftRelative + new Vector2(leftColumnSize.X + 0.008f, propertiesOffset + 0.12f);
            m_blockPropertiesMultilineText.Size = 0.5f * Size - m_blockPropertiesMultilineText.Position;

            base.OnSizeChanged();
        }

        void m_shareModeCombobox_ItemSelected()
        {
            if (!m_canChangeShareMode)
                return;

             m_canChangeShareMode = false;

             bool updateVisuals = false;

             MyOwnershipShareModeEnum shareMode = (MyOwnershipShareModeEnum)m_shareModeCombobox.GetSelectedKey();

             if (m_currentBlocks.Length > 0)
             {
                 m_requests.Clear();

                 foreach (var block in m_currentBlocks)
                 {
                     if (block.IDModule != null)
                     {
                         if (shareMode >= 0 && (block.OwnerId == MySession.Static.LocalPlayerId || MySession.Static.IsUserSpaceMaster(MySession.Static.LocalHumanPlayer.Client.SteamUserId)))
                         {
                             m_requests.Add(new MyCubeGrid.MySingleOwnershipRequest()
                             {
                                 BlockId = block.EntityId,
                                 Owner = block.IDModule.Owner
                             });
                             updateVisuals = true;
                         }
                     }
                 }

                 if (m_requests.Count > 0)
                     MyCubeGrid.ChangeOwnersRequest(shareMode, m_requests, MySession.Static.LocalPlayerId);
             }

            m_canChangeShareMode = true;

            if (updateVisuals)
                block_PropertiesChanged(null);
        }

        void m_transferToCombobox_ItemSelected()
        {
            if (m_transferToCombobox.GetSelectedIndex() == -1)
                return;

            if (m_askForConfirmation)
            {
                long ownerKey = m_transferToCombobox.GetSelectedKey();
                int ownerIndex = m_transferToCombobox.GetSelectedIndex();
                var ownerName = m_transferToCombobox.GetItemByIndex(ownerIndex).Value;

                var messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.MessageBoxTextChangeOwner), ownerName.ToString()),
                    focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                    {
                        if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            if (m_currentBlocks.Length > 0)
                            {
                                m_requests.Clear();

                                foreach (var block in m_currentBlocks)
                                {
                                    if (block.IDModule != null)
                                    {
                                        if (block.OwnerId == 0 || block.OwnerId == MySession.Static.LocalPlayerId || MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.UseTerminals))
                                        {
                                            m_requests.Add(new MyCubeGrid.MySingleOwnershipRequest()
                                            {
                                                BlockId = block.EntityId,
                                                Owner = ownerKey
                                            });
                                        }
                                    }
                                }

                                if (m_requests.Count > 0)
                                {
                                    if (MySession.Static.IsUserSpaceMaster(MySession.Static.LocalHumanPlayer.Client.SteamUserId) && Sync.Players.IdentityIsNpc(ownerKey)) 
                                    {
                                        MyCubeGrid.ChangeOwnersRequest(MyOwnershipShareModeEnum.Faction, m_requests, MySession.Static.LocalPlayerId);
                                    }
                                    else if (MySession.Static.LocalPlayerId == ownerKey)
                                    {
                                        // this should not be changed to No share, without approval from a designer, see ticket https://app.asana.com/0/64822442925263/64356719169418
                                        MyCubeGrid.ChangeOwnersRequest(MyOwnershipShareModeEnum.Faction, m_requests, MySession.Static.LocalPlayerId);
                                    }
                                    else
                                    {
                                        MyCubeGrid.ChangeOwnersRequest(MyOwnershipShareModeEnum.None, m_requests, MySession.Static.LocalPlayerId);
                                    }
                                }
                            }

                            RecreateOwnershipControls();
                            UpdateOwnerGui();
                        }
                        else
                        {
                            m_askForConfirmation = false;
                            m_transferToCombobox.SelectItemByIndex(-1);
                            m_askForConfirmation = true;
                        }
                    });
                messageBox.CanHideOthers = false;
                MyGuiSandbox.AddScreen(messageBox);
            }
            else
                UpdateOwnerGui();
        }

        private void UpdateOwnerGui()
        {
            long? owner;
            bool propertyMixed = GetOwnershipStatus(out owner);

            m_transferToCombobox.ClearItems();

            if (!propertyMixed && !owner.HasValue)
                return; //selected block without id module

            if (propertyMixed || owner.Value != 0)
                m_transferToCombobox.AddItem(0, MyTexts.Get(MySpaceTexts.BlockOwner_Nobody));

            if (propertyMixed || owner.Value != MySession.Static.LocalPlayerId)
                m_transferToCombobox.AddItem(MySession.Static.LocalPlayerId, MyTexts.Get(MySpaceTexts.BlockOwner_Me));

            foreach (var playerEntry in Sync.Players.GetOnlinePlayers())
            {
                var identity = playerEntry.Identity;
                if (identity.IdentityId != MySession.Static.LocalPlayerId && !identity.IsDead)
                {
                    var relation = MySession.Static.LocalHumanPlayer.GetRelationTo(identity.IdentityId);
                    if (relation != VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                    {
                        m_transferToCombobox.AddItem(identity.IdentityId, new StringBuilder(identity.DisplayName));
                    }
                }
            }

            foreach (var identityId in Sync.Players.GetNPCIdentities())
            {
                var identity = Sync.Players.TryGetIdentity(identityId);
                Debug.Assert(identity != null, "Couldn't find NPC identity!");
                if (identity == null) continue;

                var relation = MySession.Static.LocalHumanPlayer.GetRelationTo(identity.IdentityId);
                m_transferToCombobox.AddItem(identity.IdentityId, new StringBuilder(identity.DisplayName));
            }

            if (!propertyMixed)
            {
                if (owner.Value == MySession.Static.LocalPlayerId || MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.UseTerminals))
                {
                    m_shareModeCombobox.Enabled = true;
                }
                else
                {
                    m_shareModeCombobox.Enabled = false;
                }

                if (owner.Value == 0)
                {
                    m_transferToCombobox.Enabled = true;
                    m_ownerLabel.TextEnum = MySpaceTexts.BlockOwner_Nobody;
                }
                else
                {
                    m_transferToCombobox.Enabled = owner.Value == MySession.Static.LocalPlayerId || MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.UseTerminals);
                    m_ownerLabel.TextEnum = MySpaceTexts.BlockOwner_Me;
                    if (owner.Value != MySession.Static.LocalPlayerId)
                    {
                        var identity = Sync.Players.TryGetIdentity(owner.Value);
                        if (identity != null)
                        {
                            m_ownerLabel.Text = identity.DisplayName + (identity.IsDead ? " [" + MyTexts.Get(MyCommonTexts.PlayerInfo_Dead).ToString() + "]" : "");
                        }
                        else
                        {
                            m_ownerLabel.TextEnum = MySpaceTexts.BlockOwner_Unknown;
                        }
                    }
                }

                MyOwnershipShareModeEnum? shareMode;
                propertyMixed = GetShareMode(out shareMode);
                m_canChangeShareMode = false;
                if (!propertyMixed && shareMode.HasValue && owner.Value != 0)
                {
                    m_shareModeCombobox.SelectItemByKey((long)shareMode.Value);
                }
                else
                {
                    m_shareModeCombobox.SelectItemByIndex(-1);
                }
                m_canChangeShareMode = true;
            }
            else
            {

                m_shareModeCombobox.Enabled = true;
                m_ownerLabel.Text = "";

                m_canChangeShareMode = false;
                m_shareModeCombobox.SelectItemByIndex(-1);
                m_canChangeShareMode = true;
            }
        }

        private bool GetOwnershipStatus(out long? owner)
        {
            bool propertyMixed = false;
            owner = null;
            foreach (var block in m_currentBlocks)
            {
                if (block.IDModule != null)
                {
                    if (!owner.HasValue)
                        owner = block.IDModule.Owner;
                    else
                    {
                        if (owner.Value != block.IDModule.Owner)
                        {
                            propertyMixed = true;
                            break;
                        }
                    }
                }
            }

            return propertyMixed;
        }

        private bool GetShareMode(out MyOwnershipShareModeEnum? shareMode)
        {
            bool propertyMixed = false;
            shareMode = null;
            foreach (var block in m_currentBlocks)
            {
                if (block.IDModule != null)
                {
                    if (!shareMode.HasValue)
                        shareMode = block.IDModule.ShareMode;
                    else
                    {
                        if (shareMode.Value != block.IDModule.ShareMode)
                        {
                            propertyMixed = true;
                            break;
                        }
                    }
                }
            }

            return propertyMixed;
        }

        void OnNewNpcClick(MyGuiControlButton button)
        {
            Sync.Players.RequestNewNpcIdentity();
        }
    }
}

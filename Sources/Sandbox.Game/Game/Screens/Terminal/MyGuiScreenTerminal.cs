using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using System.Text;
using Sandbox.Engine.Networking;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Gui
{
    public partial class MyGuiScreenTerminal : MyGuiScreenBase
    {
        public static event ScreenHandler ClosedCallback
        {
            add { m_instance.Closed += value; }
            remove { m_instance.Closed -= value; }
        }

        private MyGuiControlTabControl m_terminalTabs;
        private MyGuiControlParent m_propertiesTopMenuParent;
        private MyGuiControlParent m_propertiesTableParent;

        private MyTerminalControlPanel m_controllerControlPanel;
        private MyTerminalInventoryController m_controllerInventory;
        private MyTerminalProductionController m_controllerProduction;
        private MyTerminalInfoController m_controllerInfo;
        private MyTerminalFactionController m_controllerFactions; // under MyFakes.SHOW_FACTIONS_GUI
        private MyTerminalPropertiesController m_controllerProperties;

        private MyTerminalChatController m_controllerChat;
        private MyTerminalGpsController m_controllerGps;
        private MyGridColorHelper m_colorHelper;

        private MyGuiControlLabel m_terminalNotConnected;                

        private MyCharacter m_user;
        private static MyEntity m_interactedEntity, m_openInventoryInteractedEntity;
        private MyTerminalPageEnum m_initialPage;

        private static Action<MyEntity> m_closeHandler;

        public static MyEntity InteractedEntity
        {
            get
            {
                return m_interactedEntity;
            }
            set
            {
                if (m_interactedEntity != null)
                    m_interactedEntity.OnClose -= m_closeHandler;

                if (m_instance.m_controllerControlPanel != null)
                    m_instance.m_controllerControlPanel.ClearBlockList();

                m_interactedEntity = value;
                
                if (m_interactedEntity != null)
                {
                    m_interactedEntity.OnClose += m_closeHandler;
                    if (m_interactedEntity != m_openInventoryInteractedEntity)
                        m_instance.m_initialPage = MyTerminalPageEnum.ControlPanel;
                }

                if (m_screenOpen)
                    m_instance.RecreateTabs();
            }
        }

        private static bool m_screenOpen;
        private bool m_connected = true;

        internal static bool IsOpen { get { return m_screenOpen; } }

        /// <summary>
        /// Do not call directly. Use static Show() method instead.
        /// </summary>
        private MyGuiScreenTerminal() :
            base(position: new Vector2(0.5f, 0.5f),
                 backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                 size: new Vector2(0.99f, 0.9f), backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;
            m_closeHandler = OnInteractedClose;
            m_colorHelper = new MyGridColorHelper();
        }

        void OnInteractedClose(MyEntity entity)
        {
            if(m_interactedEntity != null)
                m_interactedEntity.OnClose -= m_closeHandler;
            Hide();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTerminal";
        }

        public override bool CloseScreen()
        {
            if (base.CloseScreen())
            {
                if (m_interactedEntity != null)
                    m_interactedEntity.OnClose -= m_closeHandler;
                return true;
            }
            return false;
        }

        #region recreate controls on load

        private void CreateFixedTerminalElements()
        {
            m_terminalNotConnected = CreateErrorLabel(MySpaceTexts.ScreenTerminalError_ShipHasBeenDisconnected, "DisconnectedMessage");
            m_terminalNotConnected.Visible = false;

            var captionLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(0f, -0.42f),
                Size = new Vector2(0.06918918f, 0.0266666654f),
                Name = "CaptionLabel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.Terminal
            };

            Controls.Add(m_terminalNotConnected);
            Controls.Add(captionLabel);
            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {

                //Inits of temporary panels
                m_propertiesTopMenuParent = new MyGuiControlParent()
                {
                    Position = new Vector2(-0.864f, -0.487f),
                    Size = new Vector2(0.8f, 0.15f),
                    Name = "PropertiesPanel",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                };

                m_propertiesTableParent = new MyGuiControlParent()
                {
                    Position = new Vector2(-0.02f, -0.67f),
                    Size = new Vector2(0.93f, 0.78f),
                    Name = "PropertiesTable",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                };

                //populate them
                CreatePropertiesPageControls(m_propertiesTopMenuParent, m_propertiesTableParent);

                //pass them onto the properties class
                if (m_controllerProperties == null)
                    m_controllerProperties = new MyTerminalPropertiesController();
                else
                    m_controllerProperties.Close();

                //adds event handlers
                m_controllerProperties.ButtonClicked += PropertiesButtonClicked;
                
                //Add to screen
                Controls.Add(m_propertiesTableParent);
                Controls.Add(m_propertiesTopMenuParent);
            }

        }

        private void CreateTabs()
        {
            m_terminalTabs = new MyGuiControlTabControl()
            {
                Position = new Vector2(-0f, -0.36f),
                Size = new Vector2(0.93f, 0.78f),
                Name = "TerminalTabs",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            if (MyFakes.ENABLE_COMMUNICATION)
            {
                m_terminalTabs.TabButtonScale = 0.875f;
            }

            var inventoryPage = m_terminalTabs.GetTabSubControl(0);
            var controlPanelPage = m_terminalTabs.GetTabSubControl(1);
            var productionPage = m_terminalTabs.GetTabSubControl(2);
            var infoPage = m_terminalTabs.GetTabSubControl(3);
            var factionsPage = m_terminalTabs.GetTabSubControl(4);

            MyGuiControlTabPage communicationsPage = null;
            if (MyFakes.ENABLE_COMMUNICATION)
            {
                communicationsPage = m_terminalTabs.GetTabSubControl(5);
            }

            MyGuiControlTabPage gpsPage = null;
            if (MyFakes.ENABLE_GPS)
            {
                gpsPage = m_terminalTabs.GetTabSubControl(6);
                m_terminalTabs.TabButtonScale = 0.75f;
            }

            CreateInventoryPageControls(inventoryPage);
            CreateControlPanelPageControls(controlPanelPage);
            CreateProductionPageControls(productionPage);
            CreateInfoPageControls(infoPage);
            CreateFactionsPageControls(factionsPage);

            if (MyFakes.ENABLE_GPS)
                CreateGpsPageControls(gpsPage);

            if (MyFakes.ENABLE_COMMUNICATION)
            {
                CreateChatPageControls(communicationsPage);
            }
            MyCubeGrid grid = (InteractedEntity != null) ? InteractedEntity.Parent as MyCubeGrid : null;
            m_colorHelper.Init(grid);

            if (m_controllerInventory == null)
                m_controllerInventory = new MyTerminalInventoryController();
            else
                m_controllerInventory.Close();

            if (m_controllerControlPanel == null)
                m_controllerControlPanel = new MyTerminalControlPanel();
            else
                m_controllerControlPanel.Close();

            if (m_controllerProduction == null)
                m_controllerProduction = new MyTerminalProductionController();
            else
                m_controllerProduction.Close();

            if (m_controllerInfo == null)
                m_controllerInfo = new MyTerminalInfoController();
            else
                m_controllerInfo.Close();

            if (m_controllerFactions == null)
                m_controllerFactions = new MyTerminalFactionController();
            else
                m_controllerFactions.Close();

            if (MyFakes.ENABLE_GPS)
            {
                if (m_controllerGps == null)
                    m_controllerGps = new MyTerminalGpsController();
                else
                    m_controllerGps.Close();
            }
            
            if (MyFakes.ENABLE_COMMUNICATION)
            {
                if (m_controllerChat == null)
                    m_controllerChat = new MyTerminalChatController();
                else
                    m_controllerChat.Close();
            }

            m_controllerInventory.Init(inventoryPage, m_user, InteractedEntity, m_colorHelper);
            m_controllerControlPanel.Init(controlPanelPage, MySession.Static.LocalHumanPlayer, grid, InteractedEntity as MyTerminalBlock, m_colorHelper);
            m_controllerProduction.Init(productionPage, grid);
            m_controllerInfo.Init(infoPage, InteractedEntity != null ? InteractedEntity.Parent as MyCubeGrid : null);
            m_controllerFactions.Init(factionsPage);

            if (MyFakes.ENABLE_GPS)
                m_controllerGps.Init(gpsPage);

            if (MyFakes.ENABLE_COMMUNICATION)
            {
                m_controllerChat.Init(communicationsPage);
            }
            m_terminalTabs.SelectedPage = (int)m_initialPage;

            if (m_terminalTabs.SelectedPage != (int)MyTerminalPageEnum.Properties && !m_terminalTabs.GetTabSubControl(m_terminalTabs.SelectedPage).Enabled)
                m_terminalTabs.SelectedPage = m_terminalTabs.Controls.IndexOf(controlPanelPage);

            CloseButtonEnabled = true;
            CloseButtonOffset = new Vector2(-0.02f, 0.012f);
            //SetDefaultCloseButtonOffset();

            Controls.Add(m_terminalTabs);

            if(MyFakes.ENABLE_TERMINAL_PROPERTIES)
                m_terminalTabs.OnPageChanged += tabs_OnPageChanged;

        }

        private void CreateProperties()
        {
            if (m_controllerProperties == null)
                m_controllerProperties = new MyTerminalPropertiesController();
            else
                m_controllerProperties.Close();

            m_controllerProperties.Init(m_propertiesTopMenuParent, m_propertiesTableParent, InteractedEntity, m_openInventoryInteractedEntity);
            if(m_propertiesTableParent != null)
                m_propertiesTableParent.Visible = m_initialPage == MyTerminalPageEnum.Properties;
        }

        private void RecreateTabs()
        {
            Controls.RemoveControlByName("TerminalTabs");
            CreateTabs();

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
                CreateProperties();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            CreateFixedTerminalElements();
            CreateTabs();
            if(MyFakes.ENABLE_TERMINAL_PROPERTIES)
                CreateProperties();
        }
        #endregion

        #region populate tab pages
        private void CreateInventoryPageControls(MyGuiControlTabPage page)
        {
            page.Name      = "PageInventory";
            page.TextEnum  = MySpaceTexts.Inventory;
            page.TextScale = 0.9f;

            #region Left radio buttons
            var leftRadioCharacter = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.465f, -0.36f),
                Size = new Vector2(0.056875f, 0.0575f),
                Name = "LeftSuitButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterCharacter
            };
            var leftRadioGrid = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.405f, -0.36f),
                Size = new Vector2(0.056875f, 0.0575f),
                Name = "LeftGridButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterGrid
            };
            var leftRadioStorage = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.175f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "LeftFilterStorageButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterStorage
            };
            var leftRadioSystem = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.125f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "LeftFilterSystemButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterSystem
            };
            var leftRadioEnergy = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.075f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "LeftFilterEnergyButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterEnergy
            };
            var leftRadioAll = new MyGuiControlRadioButton()
            {
                Position = new Vector2(-0.025f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "LeftFilterAllButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterAll
            };
            #endregion

            #region Left search filters
            var blockSearchLeft = new MyGuiControlTextbox()
            {
                Position = new Vector2(-0.465f, -0.283f),
                Size = new Vector2(0.288f, 0.052f),
                Name = "BlockSearchLeft",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
            };
            var blockSearchClearLeft = new MyGuiControlButton()
            {
                Position = new Vector2(-0.2f, -0.283f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "BlockSearchClearLeft",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close,
                ActivateOnMouseRelease = true
            };
            var hideEmptyLeft = new MyGuiControlCheckbox()
            {
                Position = new Vector2(-0.025f, -0.283f),
                Name = "CheckboxHideEmptyLeft",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
            };
            var hideEmptyLeftLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(-0.155f, -0.283f),
                Name = "LabelHideEmptyLeft",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.HideEmpty
            };
            #endregion

            var leftList = new MyGuiControlList()
            {
                Position = new Vector2(-0.465f, -0.26f),
                Size = new Vector2(0.44f, 0.616f),
                Name = "LeftInventory",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            page.Controls.Add(leftRadioCharacter);
            page.Controls.Add(leftRadioGrid);
            page.Controls.Add(leftRadioStorage);
            page.Controls.Add(leftRadioSystem);
            page.Controls.Add(leftRadioEnergy);
            page.Controls.Add(leftRadioAll);

            page.Controls.Add(blockSearchLeft);
            page.Controls.Add(blockSearchClearLeft);
            page.Controls.Add(hideEmptyLeft);
            page.Controls.Add(hideEmptyLeftLabel);
            page.Controls.Add(leftList);

            #region Right radio buttons
            var rightRadioCharacter = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.025f, -0.36f),
                Size = new Vector2(0.056875f, 0.0575f),
                Name = "RightSuitButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterCharacter
            };
            var rightRadioGrid = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.085f, -0.36f),
                Size = new Vector2(0.056875f, 0.0575f),
                Name = "RightGridButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterGrid
            };
            var rightRadioStorage = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.315f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "RightFilterStorageButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterStorage
            };
            var rightRadioSystem = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.365f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "RightFilterSystemButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterSystem
            };
            var rightRadioEnergy = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.415f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "RightFilterEnergyButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterEnergy
            };
            var rightRadioAll = new MyGuiControlRadioButton()
            {
                Position = new Vector2(0.465f, -0.36f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "RightFilterAllButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Key = 0,
                VisualStyle = MyGuiControlRadioButtonStyleEnum.FilterAll
            };
            #endregion

            #region Right search filters
            var blockSearchRight = new MyGuiControlTextbox()
            {
                Position = new Vector2(0.025f, -0.283f),
                Size = new Vector2(0.288f, 0.052f),
                Name = "BlockSearchRight",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
            };
            var blockSearchClearRight = new MyGuiControlButton()
            {
                Position = new Vector2(0.29f, -0.283f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "BlockSearchClearRight",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close,
                ActivateOnMouseRelease = true
            };
            var hideEmptyRight = new MyGuiControlCheckbox()
            {
                Position = new Vector2(0.465f, -0.283f),
                Name = "CheckboxHideEmptyRight",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
            };
            var hideEmptyRightLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(0.335f, -0.283f),
                Name = "LabelHideEmptyRight",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.HideEmpty
            };
            #endregion

            var rightList = new MyGuiControlList()
            {
                Position = new Vector2(0.465f, -0.295f),
                Size = new Vector2(0.44f, 0.65f),
                Name = "RightInventory",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
            };

            page.Controls.Add(rightRadioCharacter);
            page.Controls.Add(rightRadioGrid);
            page.Controls.Add(rightRadioStorage);
            page.Controls.Add(rightRadioSystem);
            page.Controls.Add(rightRadioEnergy);
            page.Controls.Add(rightRadioAll);

            page.Controls.Add(blockSearchRight);
            page.Controls.Add(blockSearchClearRight);
            page.Controls.Add(hideEmptyRight);
            page.Controls.Add(hideEmptyRightLabel);
            page.Controls.Add(rightList);

            var trash = new MyGuiControlButton()
            {
                Position = new Vector2(0f, 0.355f),
                Size = new Vector2(0.044375f, 0.13666667f),
                Name = "ThrowOutButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM,
                TextEnum = MySpaceTexts.Afterburner,
                TextScale = 0f,
                TextAlignment = 0f,
                DrawCrossTextureWhenDisabled = true,
                VisualStyle = MyGuiControlButtonStyleEnum.InventoryTrash,
                ActivateOnMouseRelease = true
            };
            page.Controls.Add(trash);
        }

        private void CreateControlPanelPageControls(MyGuiControlTabPage page)
        {
            page.Name      = "PageControlPanel";
            page.TextEnum  = MySpaceTexts.ControlPanel;
            page.TextScale = 0.9f;

            var functionalBlockSearch = new MyGuiControlTextbox()
            {
                Position = new Vector2(-0.4625f, -0.325f),
                Size = new Vector2(0.255f, 0.052f),
                Name = "FunctionalBlockSearch",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
            };

            var functionalBlockSearchClear = new MyGuiControlButton()
            {
                Position = new Vector2(-0.232f, -0.325f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "FunctionalBlockSearchClear",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close,
                ActivateOnMouseRelease = true
            };

            var functionalBlockListbox = new MyGuiControlListbox()
            {
                Position = new Vector2(-0.4625f, 0.0225f),
                Size = new Vector2(0.29f, 0.5f),
                Name = "FunctionalBlockListbox",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisibleRowsCount = 16
            };

            var control = new MyGuiControlCompositePanel()
            {
                Position = new Vector2(-0.1525f, 0f),
                Size = new Vector2(0.615f, 0.7125f),
                Name = "Control",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                InnerHeight = 0.685f
            };

            var selectedBlockNamePanel = new MyGuiControlPanel()
            {
                Position = new Vector2(-0.1425f, -0.32f),
                Size = new Vector2(0.595f, 0.035f),
                Name = "SelectedBlockNamePanel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK
            };

            var blockNameLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(-0.1325f, -0.322f),
                Size = new Vector2(0.0470270254f, 0.0266666654f),
                Name = "BlockNameLabel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.Afterburner
            };

            var groupTitleLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(0.17f, -0.27f),
                Size = new Vector2(0.0470270254f, 0.0266666654f),
                Name = "GroupTitleLabel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.Terminal_GroupTitle
            };

            var groupName = new MyGuiControlTextbox()
            {
                Position = new Vector2(0.165f, -0.23f),
                Size = new Vector2(0.29f, 0.052f),
                Name = "GroupName",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
            };

            var groupSave = new MyGuiControlButton()
            {
                Position = new Vector2(0.2f, -0.17f),
                Name = "GroupSave",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextEnum = MySpaceTexts.TerminalButton_GroupSave
            };

            var groupDelete = new MyGuiControlButton()
            {
                Position = new Vector2(0.4f, -0.17f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "GroupDelete",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisualStyle = MyGuiControlButtonStyleEnum.Close
            };


            var showAll = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall,
                position: new Vector2(-0.205f, -0.345f),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                buttonScale:0.5f)
                {
                    Name = "ShowAll",
                };

                 
            page.Controls.Add(functionalBlockSearch);
            page.Controls.Add(functionalBlockSearchClear);
            page.Controls.Add(functionalBlockListbox);
            page.Controls.Add(control);
            page.Controls.Add(selectedBlockNamePanel);
            page.Controls.Add(blockNameLabel);
            page.Controls.Add(groupTitleLabel);
            page.Controls.Add(groupName);
            page.Controls.Add(groupSave);
            page.Controls.Add(showAll);
            page.Controls.Add(groupDelete);
        }

        private void CreateFactionsPageControls(MyGuiControlTabPage page)
        {
            page.Name = "PageFactions";
            page.TextEnum = MySpaceTexts.TerminalTab_Factions;

            var left = -0.462f;
            var top = -0.34f;
            var spacingH = 0.0045f;
            var spacingV = 0.01f;
            var buttonSize = new Vector2(0.29f, 0.052f);
            var smallerBtn = new Vector2(0.13f, 0.04f);

            var factionsComposite = new MyGuiControlCompositePanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.4f, 0.69f),
                Name = "Factions"
            };
            left += spacingH;
            top += spacingV;

            var factionsPanel = new MyGuiControlPanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(factionsComposite.Size.X - 0.01f, 0.035f),
                BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK
            };

            var factionsLabel = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left + spacingH, top),
                size: factionsPanel.Size - new Vector2(0.01f, 0.01f),
                text: MyTexts.GetString(MySpaceTexts.TerminalTab_Factions)
            );
            top += factionsLabel.Size.Y + spacingV;

            var factionsTable = new MyGuiControlTable()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(factionsPanel.Size.X, 0.15f),
                Name = "FactionsTable",
                ColumnsCount = 3,
                VisibleRowsCount = 14,
            };
            factionsTable.SetCustomColumnWidths(new float[] { 0.16f, 0.75f, 0.09f });
            factionsTable.SetColumnName(0, MyTexts.Get(MyCommonTexts.Tag));
            factionsTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.Name));
            top += factionsTable.Size.Y + spacingV;

            var createBtn      = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left, top)) { Name = "buttonCreate" };
            var joinBtn        = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left, top + buttonSize.Y + spacingV)) { Name = "buttonJoin" };
            var joinCancelBtn  = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left, top + buttonSize.Y + spacingV)) { Name = "buttonCancelJoin" };
            var leaveBtn       = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left, top + buttonSize.Y + spacingV)) { Name = "buttonLeave" };
            var sendPeaceBtn   = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, position: new Vector2(-0.065f, top)) { Name = "buttonSendPeace" };
            var cancelPeaceBtn = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, position: new Vector2(-0.065f, top)) { Name = "buttonCancelPeace" };
            var acceptPeaceBtn = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, position: new Vector2(-0.065f, top)) { Name = "buttonAcceptPeace" };
            var enemyBtn       = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, position: new Vector2(-0.065f, top + buttonSize.Y + spacingV)) { Name = "buttonEnemy" };

            page.Controls.Add(factionsComposite);
            page.Controls.Add(factionsPanel);
            page.Controls.Add(factionsLabel);
            page.Controls.Add(factionsTable);
            page.Controls.Add(createBtn);
            page.Controls.Add(joinBtn);
            page.Controls.Add(joinCancelBtn);
            page.Controls.Add(leaveBtn);
            page.Controls.Add(sendPeaceBtn);
            page.Controls.Add(cancelPeaceBtn);
            page.Controls.Add(acceptPeaceBtn);
            page.Controls.Add(enemyBtn);

            // Do the right side
            // reset left / top
            left = -0.0475f;
            top = -0.34f;

            var factionComposite = new MyGuiControlCompositePanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(-0.05f, top),
                Size = new Vector2(0.5f, 0.69f),
                Name = "compositeFaction"
            };
            left += spacingH;
            top += spacingV;

            var factionNamePanel = new MyGuiControlPanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(factionComposite.Size.X - 0.012f, 0.035f),
                BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK,
                Name = "panelFactionName"
            };

            var factionName = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left + spacingH, top),
                size: factionNamePanel.Size - new Vector2(0.01f, 0.01f)
            ) { Name = "labelFactionName" };
            top += factionsLabel.Size.Y + (2f * spacingV);
            var size = factionNamePanel.Size - new Vector2(0.14f, 0.01f);

            var factionDescLabel = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: factionNamePanel.Size - new Vector2(0.01f, 0.01f)
            ) { Name = "labelFactionDesc" };
            top += factionDescLabel.Size.Y + spacingV;

            var factionDesc = new MyGuiControlMultilineText(
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textScale: MyGuiConstants.TOOL_TIP_TEXT_SCALE,
                position: new Vector2(left, top),
                size: new Vector2(size.X, 0.08f)
            )
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Name = "textFactionDesc",
            };
            top += factionDesc.Size.Y + 2f * spacingV;

            var factionPrivateLabel = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: factionNamePanel.Size - new Vector2(0.01f, 0.01f)
            ) { Name = "labelFactionPrivate" };
            top += factionPrivateLabel.Size.Y + spacingV;

            var factionPrivate = new MyGuiControlMultilineText(
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textScale: MyGuiConstants.TOOL_TIP_TEXT_SCALE,
                position: new Vector2(left, top),
                size: new Vector2(size.X, 0.08f)
            )
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Name = "textFactionPrivate",
            };
            top += factionDesc.Size.Y + 0.0275f;

            var labelFactionMembers = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: factionNamePanel.Size - new Vector2(0.01f, 0.01f)
            ) { Name = "labelFactionMembers" };


            var checkAcceptEveryone = new MyGuiControlCheckbox(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                position: new Vector2(factionNamePanel.Position.X + factionNamePanel.Size.X, top + spacingV)
            ) { Name = "checkFactionMembersAcceptEveryone" };

            var labelAcceptEveryone = new MyGuiControlLabel(
             originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
             position: new Vector2(checkAcceptEveryone.Position.X - checkAcceptEveryone.Size.X - spacingH, top),
             size: labelFactionMembers.Size - new Vector2(0.01f, 0.01f)
         ) { Name = "labelFactionMembersAcceptEveryone" };


            var labelAcceptPeace = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2((17 * spacingH), top),
                size: labelFactionMembers.Size - new Vector2(0.01f, 0.01f)
            ) { Name = "labelFactionMembersAcceptPeace" };

            var checkAcceptPeace = new MyGuiControlCheckbox(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2((47 * spacingH), top + spacingV)
            ) { Name = "checkFactionMembersAcceptPeace" };



            top += factionPrivateLabel.Size.Y + spacingV;

            var membersTable = new MyGuiControlTable()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(size.X, 0.15f),
                Name = "tableMembers",
                ColumnsCount = 2,
                VisibleRowsCount = 8,
                HeaderVisible = false
            };
            membersTable.SetCustomColumnWidths(new float[] { 0.7f, 0.3f });
            membersTable.SetColumnName(0, MyTexts.Get(MyCommonTexts.Name));
            membersTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.Status));

            var btnSpacing = smallerBtn.Y + spacingV;
            var editBtn = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, factionDesc.Position.Y)) { Name = "buttonEdit" };
            var promBtn = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y)) { Name = "buttonPromote" };
            var kickBtn = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y + btnSpacing)) { Name = "buttonKick" };
            var acceptJoin = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y + 2f * btnSpacing)) { Name = "buttonAcceptJoin" };
            var demote = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y + 3f * btnSpacing)) { Name = "buttonDemote" };
            //var acceptPeace = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,    position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y + 2f * btnSpacing)) { Name = "buttonAcceptPeace" };
            var addNpcToFaction = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Rectangular, size: smallerBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, position: new Vector2(left + membersTable.Size.X + spacingV, membersTable.Position.Y + 4f * btnSpacing)) { Name = "buttonAddNpc" };

            page.Controls.Add(factionComposite);
            page.Controls.Add(factionNamePanel);
            page.Controls.Add(factionName);
            page.Controls.Add(factionDescLabel);
            page.Controls.Add(factionDesc);
            page.Controls.Add(factionPrivateLabel);
            page.Controls.Add(factionPrivate);
            page.Controls.Add(labelFactionMembers);
            page.Controls.Add(labelAcceptEveryone);
            page.Controls.Add(labelAcceptPeace);
            page.Controls.Add(checkAcceptEveryone);
            page.Controls.Add(checkAcceptPeace);
            page.Controls.Add(membersTable);

            page.Controls.Add(editBtn);
            page.Controls.Add(promBtn);
            page.Controls.Add(kickBtn);
            page.Controls.Add(demote);
            page.Controls.Add(acceptJoin);
            page.Controls.Add(addNpcToFaction);
        }

        private void CreateChatPageControls(MyGuiControlTabPage chatPage)
        {
            chatPage.Name = "PageComms";
            chatPage.TextEnum = MySpaceTexts.TerminalTab_Chat;

            float left = -0.4625f;
            float right = -left;
            
            float top = -0.34f;

            int rowCount = 11;

            float width = 0.35f;
            //defined based on row count
            float height = 0;

            float margin = 0.02f;

            var playerLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(left, top),
                Name = "PlayerLabel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Text = MyTexts.GetString(MyCommonTexts.ScreenCaptionPlayers)
            };
            chatPage.Controls.Add(playerLabel);

            top += playerLabel.GetTextSize().Y + 0.01f;

            var playerList = new MyGuiControlListbox()
            {
                Position = new Vector2(left, top),
                Size = new Vector2(width, 0f),
                Name = "PlayerListbox",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                VisibleRowsCount = rowCount
            };
            chatPage.Controls.Add(playerList);

            height = playerList.ItemSize.Y * rowCount;
            top += height + margin;
            rowCount = 4;

            var factionLabel = new MyGuiControlLabel()
            {
                Position = new Vector2(left, top),
                Name = "PlayerLabel",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Text = MyTexts.GetString(MyCommonTexts.Factions)
            };
            chatPage.Controls.Add(factionLabel);

            top += playerLabel.GetTextSize().Y + 0.01f;

            var factionsList = new MyGuiControlListbox()
            {
                Position = new Vector2(left, top),
                Size = new Vector2(width, 0f),
                Name = "FactionListbox",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                VisibleRowsCount = rowCount
            };
            chatPage.Controls.Add(factionsList);

            top = -0.34f;
            width = 0.6f;
            height = 0.515f;
            margin = 0.038f;

            var chatboxPanel = new MyGuiControlPanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Position = new Vector2(right, top),
                Size = new Vector2(width, height),
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
            };

            chatPage.Controls.Add(chatboxPanel);

            var chatHistory = new MyGuiControlMultilineText(
                position: new Vector2(right, top + 0.005f),
                size: new Vector2(width - 0.01f, height - 0.01f),
                backgroundColor: null,
                font: MyFontEnum.Blue,
                textScale: 0.95f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                contents: null);
            chatHistory.Name = "ChatHistory";
            chatHistory.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            chatHistory.TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            chatPage.Controls.Add(chatHistory);

            top += height + margin;
            height = 0.05f;
            var chatbox = new MyGuiControlTextbox()
            {
                Position = new Vector2(right, top),
                Size = new Vector2(width, height),
                Name = "Chatbox",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
            };

            chatPage.Controls.Add(chatbox);

            width = 0.75f;
            top += height + margin;
            height = 0.05f;
            var sendButton = new MyGuiControlButton()
            {
                Position = new Vector2(right, top),
                Text = "Send",
                Name = "SendButton",
                Size = new Vector2(width, height),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
            };

            chatPage.Controls.Add(sendButton);
        }

        private void CreateInfoPageControls(MyGuiControlTabPage infoPage)
        {
            infoPage.Name = "PageInfo";
            infoPage.TextEnum = MySpaceTexts.TerminalTab_Info;

            var list = new MyGuiControlList(new Vector2(-0.462f, -0.34f), new Vector2(0.35f, 0.69f));
            //var list = new MyGuiControlMultilineText( new Vector2(-0.462f, -0.34f), new Vector2(0.35f,0.69f), null, MyFontEnum.White, 1, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, new StringBuilder());
            list.Name = "InfoList";
            list.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            infoPage.Controls.Add(list);

            var convertBtn = new MyGuiControlButton();
            convertBtn.Position = new Vector2(0f, 0.06f);
            convertBtn.TextEnum = MySpaceTexts.TerminalTab_Info_ConvertButton;
            convertBtn.SetToolTip(MySpaceTexts.TerminalTab_Info_ConvertButton_TT);
            convertBtn.ShowTooltipWhenDisabled = true;
            convertBtn.Name = "ConvertBtn";
            infoPage.Controls.Add(convertBtn);

            var convertToStationBtn = new MyGuiControlButton();
            convertBtn.Position = new Vector2(0f, -0.06f);
            convertToStationBtn.TextEnum = MySpaceTexts.TerminalTab_Info_ConvertToStationButton;
            convertToStationBtn.SetToolTip(MySpaceTexts.TerminalTab_Info_ConvertToStationButton_TT);
            convertToStationBtn.ShowTooltipWhenDisabled = true;
            convertToStationBtn.Name = "ConvertToStationBtn";
            convertToStationBtn.Visible = MySession.Static.EnableConvertToStation;
            infoPage.Controls.Add(convertToStationBtn);
            

            if (MyFakes.ENABLE_CENTER_OF_MASS)
            {
                var sep = new MyGuiControlSeparatorList();
                sep.AddVertical(new Vector2(0.14f, -0.34f), 0.7f, 0.002f);
                infoPage.Controls.Add(sep);

                var centerBtnLabel = new MyGuiControlLabel(new Vector2(0.15f, -0.32f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_ShowMassCenter));
                centerBtnLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                infoPage.Controls.Add(centerBtnLabel);

                var centerBtn = new MyGuiControlCheckbox(new Vector2(0.45f, centerBtnLabel.Position.Y));
                centerBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
                centerBtn.Name = "CenterBtn";
                infoPage.Controls.Add(centerBtn);

            }

            var showGravityGizmoBtnLabel = new MyGuiControlLabel(new Vector2(0.15f, -0.27f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_ShowGravityGizmo));
            showGravityGizmoBtnLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(showGravityGizmoBtnLabel);

            var showGravityGizmoBtn = new MyGuiControlCheckbox(new Vector2(0.45f, showGravityGizmoBtnLabel.Position.Y));
            showGravityGizmoBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            showGravityGizmoBtn.Name = "ShowGravityGizmo";
            infoPage.Controls.Add(showGravityGizmoBtn);

            var showSenzorGizmoBtnLabel = new MyGuiControlLabel(new Vector2(0.15f, -0.22f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_ShowSenzorGizmo));
            showSenzorGizmoBtnLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(showSenzorGizmoBtnLabel);

            var showSenzorGizmoBtn = new MyGuiControlCheckbox(new Vector2(0.45f, showSenzorGizmoBtnLabel.Position.Y));
            showSenzorGizmoBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            showSenzorGizmoBtn.Name = "ShowSenzorGizmo";
            infoPage.Controls.Add(showSenzorGizmoBtn);

            var showAntenaGizmoBtnLabel = new MyGuiControlLabel(new Vector2(0.15f, -0.17f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_ShowAntenaGizmo));
            showAntenaGizmoBtnLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(showAntenaGizmoBtnLabel);

            var showAntenaGizmoBtn = new MyGuiControlCheckbox(new Vector2(0.45f, showAntenaGizmoBtnLabel.Position.Y));
            showAntenaGizmoBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            showAntenaGizmoBtn.Name = "ShowAntenaGizmo";
            infoPage.Controls.Add(showAntenaGizmoBtn);

            CreateAntennaSlider(infoPage, MyTexts.GetString(MySpaceTexts.TerminalTab_Info_FriendlyAntennaRange),"FriendAntennaRange",-0.13f);
            CreateAntennaSlider(infoPage, MyTexts.GetString(MySpaceTexts.TerminalTab_Info_EnemyAntennaRange), "EnemyAntennaRange", -0.01f);
            CreateAntennaSlider(infoPage, MyTexts.GetString(MySpaceTexts.TerminalTab_Info_OwnedAntennaRange), "OwnedAntennaRange", 0.11f);

            var pivotBtnLabel = new MyGuiControlLabel(new Vector2(0.15f, 0.23f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_PivotBtn));
            pivotBtnLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(pivotBtnLabel);

            var pivotBtn = new MyGuiControlCheckbox(new Vector2(0.45f, pivotBtnLabel.Position.Y));
            pivotBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            pivotBtn.Name = "PivotBtn";
            infoPage.Controls.Add(pivotBtn);

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                var nameLabel = new MyGuiControlLabel()
                {
                    Name = "RenameShipLabel",
                    Text = "Ship Name",
                    Position = new Vector2(0.15f, 0.26f)
                };
                var nameTextBox = new MyGuiControlTextbox()
                {
                    Name = "RenameShipText",
                    Position = new Vector2(0.25f, 0.3f),
                    Size = new Vector2(0.2f, 0.005f)
                };

                var renameButton = new MyGuiControlButton()
                {
                    Name = "RenameShipButton",
                    Position = new Vector2(0.38f, 0.3f),
                    Text = "Ok",
                    VisualStyle = MyGuiControlButtonStyleEnum.Tiny,
                };
                infoPage.Controls.Add(nameLabel);
                infoPage.Controls.Add(nameTextBox);
                infoPage.Controls.Add(renameButton);
            }

            var setDestructibleBlocksLabel = new MyGuiControlLabel(new Vector2(0.15f, 0.28f), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_DestructibleBlocks));
            setDestructibleBlocksLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            setDestructibleBlocksLabel.Visible = MySession.Static.Settings.ScenarioEditMode || MySession.Static.IsScenario;
            infoPage.Controls.Add(setDestructibleBlocksLabel);

            var setDestructibleBlocksBtn = new MyGuiControlCheckbox(new Vector2(0.45f, setDestructibleBlocksLabel.Position.Y), toolTip: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_DestructibleBlocks_Tooltip));
            setDestructibleBlocksBtn.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            setDestructibleBlocksBtn.Name = "SetDestructibleBlocks";
            infoPage.Controls.Add(setDestructibleBlocksBtn);
        }

		private static bool OnAntennaSliderClicked(MyGuiControlSlider arg)
		{
			if (MyInput.Static.IsAnyCtrlKeyPressed())
			{
				float min = MyHudMarkerRender.Denormalize(0);
				float max = MyHudMarkerRender.Denormalize(1);
				float val = MyHudMarkerRender.Denormalize(arg.Value);

				bool parseAsInteger = true;

				if (parseAsInteger && System.Math.Abs(min) < 1.0f)	// This allows the user to enter 0 as input
					min = 0;

				// TODO: allocations, needs GUI redo
                MyGuiScreenDialogAmount dialog = new MyGuiScreenDialogAmount(min, max, parseAsInteger: parseAsInteger, defaultAmount: val, caption: MyCommonTexts.DialogAmount_SetValueCaption);
				dialog.OnConfirmed += (v) => { arg.Value = MyHudMarkerRender.Normalize(v); };
				MyGuiSandbox.AddScreen(dialog);
				return true;
			}
			return false;
		}

        private static void CreateAntennaSlider(MyGuiControlTabPage infoPage,string labelText,string name,float startY)
        {
            var friendAntennaRangeLabel = new MyGuiControlLabel(new Vector2(0.15f, startY), text: labelText);
            friendAntennaRangeLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(friendAntennaRangeLabel);

            var friendAntennaRangeValueLabel = new MyGuiControlLabel(new Vector2(0.15f, startY+0.09f));
            friendAntennaRangeValueLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            infoPage.Controls.Add(friendAntennaRangeValueLabel);

            var friendAntennaRange = new MyGuiControlSlider(new Vector2(0.45f, startY+0.05f));
            friendAntennaRange.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            friendAntennaRange.Name = name;
            friendAntennaRange.MinValue = 0;
            friendAntennaRange.MaxValue = 1;
			friendAntennaRange.DefaultValue = friendAntennaRange.MaxValue;
            friendAntennaRange.ValueChanged += (MyGuiControlSlider s) => { friendAntennaRangeValueLabel.Text = MyValueFormatter.GetFormatedFloat(MyHudMarkerRender.Denormalize(s.Value), 0) + "m"; };
			friendAntennaRange.SliderClicked = OnAntennaSliderClicked;
            infoPage.Controls.Add(friendAntennaRange);
        }

        private void CreateProductionPageControls(MyGuiControlTabPage productionPage)
        {
            productionPage.Name = "PageProduction";
            productionPage.TextEnum = MySpaceTexts.TerminalTab_Production;

            float columnSpacing = 0.03f;
            float controlSpacing = 0.01f;
            float smallBackgroundPanelHeight = 0.05f;
            float largeBackgroundPanelHeight = 0.08f;

            var assemblersCombobox = new MyGuiControlCombobox(
                position: -0.5f * productionPage.Size + new Vector2(0f, controlSpacing))
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    Name = "AssemblersCombobox"
                };

            var blueprintsBackgroundPanel = new MyGuiControlPanel(
                position: assemblersCombobox.Position + new Vector2(0f, assemblersCombobox.Size.Y + controlSpacing),
                size: new Vector2(1f, largeBackgroundPanelHeight),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
                {
                    BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                    Name = "BlueprintsBackgroundPanel",
                };

            var blueprintsLabel = new MyGuiControlLabel(
                position: blueprintsBackgroundPanel.Position + new Vector2(controlSpacing, controlSpacing),
                text: MyTexts.GetString(MySpaceTexts.ScreenTerminalProduction_Blueprints),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
                {
                    Name = "BlueprintsLabel"
                };
            var blueprintsGrid = new MyGuiControlGrid()
            {
                VisualStyle = MyGuiControlGridStyleEnum.Toolbar,
                RowsCount = MyTerminalProductionController.BLUEPRINT_GRID_ROWS,
                ColumnsCount = 4,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            var blueprintsScrollableArea = new MyGuiControlScrollablePanel(
                scrolledControl: blueprintsGrid)
                {
                    Name = "BlueprintsScrollableArea",
                    ScrollbarVEnabled = true,
                    Position = blueprintsBackgroundPanel.Position + new Vector2(0f, blueprintsBackgroundPanel.Size.Y),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                    Size = new Vector2(blueprintsBackgroundPanel.Size.X, 0.5f),
                    ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
                };
            blueprintsScrollableArea.FitSizeToScrolledControl();
            assemblersCombobox.Size = new Vector2(blueprintsScrollableArea.Size.X, assemblersCombobox.Size.Y);
            blueprintsBackgroundPanel.Size = new Vector2(blueprintsScrollableArea.Size.X, largeBackgroundPanelHeight);
            blueprintsGrid.RowsCount = 20;

            productionPage.Controls.Add(assemblersCombobox);
            productionPage.Controls.Add(blueprintsBackgroundPanel);
            productionPage.Controls.Add(blueprintsLabel);
            productionPage.Controls.Add(blueprintsScrollableArea);

            var materialsBackgroundPanel = new MyGuiControlPanel(
                position: blueprintsBackgroundPanel.Position + new Vector2(blueprintsBackgroundPanel.Size.X + columnSpacing, 0f),
                size: new Vector2(blueprintsBackgroundPanel.Size.X, smallBackgroundPanelHeight),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
                {
                    BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK
                };

            var materialsLabel = new MyGuiControlLabel(
                position: materialsBackgroundPanel.Position + new Vector2(controlSpacing, controlSpacing),
                text: MyTexts.GetString(MySpaceTexts.ScreenTerminalProduction_StoredMaterials),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            );

            var materialsList = new MyGuiControlComponentList()
            {
                Position = materialsBackgroundPanel.Position + new Vector2(0f, materialsBackgroundPanel.Size.Y),
                Size = new Vector2(materialsBackgroundPanel.Size.X, blueprintsBackgroundPanel.Size.Y + blueprintsScrollableArea.Size.Y - materialsBackgroundPanel.Size.Y),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                Name = "MaterialsList",
            };

            productionPage.Controls.Add(materialsBackgroundPanel);
            productionPage.Controls.Add(materialsLabel);
            productionPage.Controls.Add(materialsList);

            var assemblingButton = new MyGuiControlRadioButton(
                position: materialsBackgroundPanel.Position + new Vector2(materialsBackgroundPanel.Size.X + columnSpacing, 0f),
                size: new Vector2(200f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    Icon = MyGuiConstants.TEXTURE_BUTTON_ICON_COMPONENT,
                    IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    Text = MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_AssemblingButton),
                    Name = "AssemblingButton",
                };
            assemblingButton.SetToolTip(MySpaceTexts.ToolTipTerminalProduction_AssemblingMode);

            var disassemblingButton = new MyGuiControlRadioButton(
                position: assemblingButton.Position + new Vector2(assemblingButton.Size.X + controlSpacing, 0f),
                size: new Vector2(238f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    Icon = MyGuiConstants.TEXTURE_BUTTON_ICON_DISASSEMBLY,
                    IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    Text = MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_DisassemblingButton),
                    Name = "DisassemblingButton",
                };
            disassemblingButton.SetToolTip(MySpaceTexts.ToolTipTerminalProduction_DisassemblingMode);

            var queueBackgroundPanel = new MyGuiControlCompositePanel()
            {
                Position = assemblingButton.Position + new Vector2(0f, assemblingButton.Size.Y + controlSpacing),
                Size = new Vector2(0.4f, largeBackgroundPanelHeight),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };
            var queueLabel = new MyGuiControlLabel(
                position: queueBackgroundPanel.Position + new Vector2(controlSpacing, controlSpacing),
                text: MyTexts.GetString(MySpaceTexts.ScreenTerminalProduction_ProductionQueue),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            );
            var queueGrid = new MyGuiControlGrid()
            {
                VisualStyle = MyGuiControlGridStyleEnum.Toolbar,
                RowsCount = 2,
                ColumnsCount = 6,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            var queueScrollableArea = new MyGuiControlScrollablePanel(
                scrolledControl: queueGrid)
                {
                    Name = "QueueScrollableArea",
                    ScrollbarVEnabled = true,
                    Position = queueBackgroundPanel.Position + new Vector2(0f, queueBackgroundPanel.Size.Y),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                    ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
                };
            queueScrollableArea.FitSizeToScrolledControl();
            queueGrid.RowsCount = 10;
            queueBackgroundPanel.Size = new Vector2(queueScrollableArea.Size.X, queueBackgroundPanel.Size.Y);

            var repeatCheckbox = new MyGuiControlCheckbox(
                position: queueBackgroundPanel.Position + new Vector2(queueBackgroundPanel.Size.X - controlSpacing, controlSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipTerminalProduction_RepeatMode),
                visualStyle: MyGuiControlCheckboxStyleEnum.Repeat)
                {
                    Name = "RepeatCheckbox",
                };

            var slaveCheckbox = new MyGuiControlCheckbox(
                position: queueBackgroundPanel.Position + new Vector2(queueBackgroundPanel.Size.X - 0.1f - controlSpacing, controlSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipTerminalProduction_SlaveMode),
                visualStyle: MyGuiControlCheckboxStyleEnum.Slave)
            {
                Name = "SlaveCheckbox",
            };

            var inventoryBackgroundPanel = new MyGuiControlCompositePanel()
            {
                Position = queueScrollableArea.Position + new Vector2(0f, queueScrollableArea.Size.Y + controlSpacing),
                Size = new Vector2(0.4f, largeBackgroundPanelHeight),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };
            var inventoryLabel = new MyGuiControlLabel(
                position: inventoryBackgroundPanel.Position + new Vector2(controlSpacing, controlSpacing),
                text: MyTexts.GetString(MySpaceTexts.ScreenTerminalProduction_Inventory),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            );
            var inventoryGrid = new MyGuiControlGrid()
            {
                VisualStyle = MyGuiControlGridStyleEnum.Toolbar,
                RowsCount = 3,
                ColumnsCount = 6,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            var inventoryScrollableArea = new MyGuiControlScrollablePanel(
                scrolledControl: inventoryGrid)
                {
                    Name = "InventoryScrollableArea",
                    ScrollbarVEnabled = true,
                    Position = inventoryBackgroundPanel.Position + new Vector2(0f, inventoryBackgroundPanel.Size.Y),
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                    ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
                };
            inventoryScrollableArea.FitSizeToScrolledControl();
            inventoryGrid.RowsCount = 10;
            inventoryBackgroundPanel.Size = new Vector2(inventoryScrollableArea.Size.X, inventoryBackgroundPanel.Size.Y);
            var disassembleAllButton = new MyGuiControlButton(
                position: inventoryBackgroundPanel.Position + new Vector2(inventoryBackgroundPanel.Size.X - controlSpacing, controlSpacing),
                size: new Vector2(220f, 40f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_DisassembleAllButton),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipTerminalProduction_DisassembleAll))
                {
                    Name = "DisassembleAllButton",
                };

            var inventoryButton = new MyGuiControlButton(
                position: inventoryScrollableArea.Position + new Vector2(0f, inventoryScrollableArea.Size.Y + controlSpacing),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(214f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_InventoryButton))
                {
                    Name = "InventoryButton",
                };

            var controlPanelButton = new MyGuiControlButton(
                position: inventoryButton.Position + new Vector2(inventoryButton.Size.X + controlSpacing, 0f),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: inventoryButton.Size,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_ControlPanelButton))
                {
                    Name = "ControlPanelButton",
                };

            productionPage.Controls.Add(assemblingButton);
            productionPage.Controls.Add(disassemblingButton);
            productionPage.Controls.Add(queueBackgroundPanel);
            productionPage.Controls.Add(queueLabel);
            productionPage.Controls.Add(repeatCheckbox);
            productionPage.Controls.Add(slaveCheckbox);
            productionPage.Controls.Add(queueScrollableArea);
            productionPage.Controls.Add(inventoryBackgroundPanel);
            productionPage.Controls.Add(inventoryLabel);
            productionPage.Controls.Add(disassembleAllButton);
            productionPage.Controls.Add(inventoryScrollableArea);
            productionPage.Controls.Add(inventoryButton);
            productionPage.Controls.Add(controlPanelButton);
        }

        private void CreateGpsPageControls(MyGuiControlTabPage gpsPage)
        {
            gpsPage.Name      = "PageIns";
            gpsPage.TextEnum = MySpaceTexts.TerminalTab_GPS;
            gpsPage.TextScale = 0.9f;
            var spacingH = 0.01f;
            var spacingV = 0.01f;
            var buttonSize = new Vector2(0.29f, 0.052f);
            var smallerBtn = new Vector2(0.13f, 0.04f);
            var left = -0.4625f;
            var top = -0.325f;

            var gpsBlockSearch = new MyGuiControlTextbox()
            {
                Position = new Vector2(left,top),
                Size = new Vector2(0.29f, 0.052f),
                Name = "SearchIns",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };

            var gpsBlockSearchClear = new MyGuiControlButton()
            {
                Position = new Vector2(left+gpsBlockSearch.Size.X, top+0.01f),
                Size = new Vector2(0.045f, 0.05666667f),
                Name = "SearchInsClear",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                VisualStyle = MyGuiControlButtonStyleEnum.Close,
                ActivateOnMouseRelease = true
            };
            top += gpsBlockSearch.Size.Y + spacingV;

            var gpsBlockTable = new MyGuiControlTable()
            {
                Position = new Vector2(left,top),
                Size = new Vector2(0.29f, 0.5f),
                Name = "TableINS",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                ColumnsCount = 1,
                VisibleRowsCount = 14,
                HeaderVisible=false
            };
            gpsBlockTable.SetCustomColumnWidths(new float[1]{1});
            top += gpsBlockTable.Size.Y + spacingV;

            //LEFT SIDE BUTTONS:
            var gpsButtonAdd = new MyGuiControlButton(
                position: new Vector2(left,top),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(140f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Add),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                Name = "buttonAdd"
            };
            var gpsButtonDelete = new MyGuiControlButton(
                position: new Vector2(left,top +gpsButtonAdd.Size.Y+spacingV),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(140f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Delete),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                Name = "buttonDelete"
            };
            var gpsButtonFromCurrent = new MyGuiControlButton(
                position: new Vector2(left+gpsButtonAdd.Size.X+spacingH,top),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(310f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromCurrent),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                Name = "buttonFromCurrent"
            };
            var gpsButtonFromClipboard = new MyGuiControlButton(
                position: new Vector2(left + gpsButtonAdd.Size.X + spacingH, top + gpsButtonAdd.Size.Y + spacingV),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(310f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromClipboard),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                Name = "buttonFromClipboard"
            };


            gpsPage.Controls.Add(gpsBlockSearch);
            gpsPage.Controls.Add(gpsBlockSearchClear);
            gpsPage.Controls.Add(gpsBlockTable);
            gpsPage.Controls.Add(gpsButtonAdd);
            gpsPage.Controls.Add(gpsButtonDelete);
            gpsPage.Controls.Add(gpsButtonFromCurrent);
            gpsPage.Controls.Add(gpsButtonFromClipboard);


            //RIGHT SIDE:
            left = -0.15f;
            top = -0.325f;
            var gpsComposite = new MyGuiControlCompositePanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.6f, 0.39f),
                Name = "compositeIns"
            };
            left += spacingH;
            top += spacingV+0.05f;

            var gpsNameLabel = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top),
                size: new Vector2(0.4f, 0.035f)
            ) { Name = "labelInsName",
                Text = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Name).ToString()
            };
            var gpsNamePanel = new MyGuiControlTextbox(maxLength:32)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Position = new Vector2(left + spacingH + gpsNameLabel.Size.X, top),
                Size = new Vector2(gpsComposite.Size.X - spacingH - gpsNameLabel.Size.X - spacingH -0.01f, 0.035f),
                Name = "panelInsName"
            };

            top += gpsNamePanel.Size.Y + (2f * spacingV);

            var size = gpsNamePanel.Size - new Vector2(0.14f, 0.01f);

            var gpsDescLabel = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top),
                //size: insNamePanel.Size - new Vector2(0.01f, 0.01f)
                size : new Vector2(gpsComposite.Size.X - 0.012f, 0.035f)
            ){
                Name = "labelInsDesc",
                Text = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Description).ToString()
            };
            top += gpsDescLabel.Size.Y + spacingV;

            var gpsDescText = new MyGuiControlTextbox(
                position: new Vector2(left, top),
                maxLength:255
            )
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Name = "textInsDesc",
                Size= new Vector2(gpsComposite.Size.X - 2*spacingH, 0.035f)
            };
            top += gpsDescText.Size.Y + 2f * spacingV;

            //X,Y,Z:
            var gpsLabelX = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top),
                size : new Vector2(0.01f, 0.035f),
                text : MyTexts.Get(MySpaceTexts.TerminalTab_GPS_X).ToString()
            )
            {
                Name = "labelInsX",
            };
            left += gpsLabelX.Size.X+spacingH;
            var gpsXCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Position = new Vector2(left, top),
                Size = new Vector2((gpsComposite.Size.X - spacingH )/ 3 - 2 * spacingH - gpsLabelX.Size.X, 0.035f),
                Name = "textInsX"
            };
            left += gpsXCoord.Size.X + spacingH;

            var gpsLabelY = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top),
                //size: new Vector2(0.01f, 0.035f),
                size : new Vector2(gpsComposite.Size.X - 0.012f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Y).ToString()
                //size: new Vector2(0.4f, 0.035f)
            )
            {
                Name = "labelInsY"
            };
            left += gpsLabelX.Size.X + spacingH;
            var gpsYCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Position = new Vector2(left, top),
                Size = new Vector2((gpsComposite.Size.X - spacingH) / 3 - 2 * spacingH - gpsLabelX.Size.X, 0.035f),
                Name = "textInsY"
            };
            left += gpsYCoord.Size.X + spacingH;

            var gpsLabelZ = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top),
                size: new Vector2(0.01f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Z).ToString()
                //size: new Vector2(0.4f, 0.035f)
            )
            {
                Name = "labelInsZ",
            };
            left += gpsLabelX.Size.X + spacingH;
            var gpsZCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Position = new Vector2(left, top),
                Size = new Vector2((gpsComposite.Size.X - spacingH) / 3 - 2 * spacingH - gpsLabelX.Size.X, 0.035f),
                Name = "textInsZ"
            };
            top += gpsNamePanel.Size.Y + (2f * spacingV);

            //BUTTONS:
            left = spacingH-0.15f;

            //SHOW ON HUD & COPY TO CLIPBOARD:
            var checkGpsShowOnHud = new MyGuiControlCheckbox(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top)
            ) { Name = "checkInsShowOnHud" };

            var labelGpsShowOnHud = new MyGuiControlLabel(
             originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
             position: new Vector2(left+ checkGpsShowOnHud.Size.X + spacingH, top),
             size: checkGpsShowOnHud.Size - new Vector2(0.01f, 0.01f)
            )
            {
                Name = "labelInsShowOnHud",
                Text = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_ShowOnHud).ToString()
            };

            var toClipboardButton = new MyGuiControlButton(
                position: new Vector2(gpsComposite.Position.X+gpsComposite.Size.X-spacingH, top),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                size: new Vector2(300f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_CopyToClipboard),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER)
            {
                Name = "buttonToClipboard"
            };

            top += toClipboardButton.Size.Y * 1.1f;
            var checkGpsAlwaysVisible = new MyGuiControlCheckbox(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                position: new Vector2(left, top)
            )
            {
                Name = "checkInsAlwaysVisible",
            };
            checkGpsAlwaysVisible.SetToolTip(MySpaceTexts.TerminalTab_GPS_AlwaysVisible_Tooltip);

            var labelGpsAlwaysVisible = new MyGuiControlLabel(
             originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
             position: new Vector2(left + checkGpsShowOnHud.Size.X + spacingH, top),
             size: checkGpsShowOnHud.Size - new Vector2(0.01f, 0.01f)
            )
            {
                Name = "labelInsAlwaysVisible",
                Text = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_AlwaysVisible).ToString()
            };
            labelGpsAlwaysVisible.SetToolTip(MySpaceTexts.TerminalTab_GPS_AlwaysVisible_Tooltip);

            top += checkGpsShowOnHud.Size.Y;
            var labelIllegalDataWarning = new MyGuiControlLabel(
             originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
             position: new Vector2(left + spacingH, top),
             size: new Vector2(gpsComposite.Size.X - 0.012f, 0.035f)
            )
            {
                Name = "TerminalTab_GPS_SaveWarning",
                Text = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_SaveWarning).ToString(),
                ColorMask = Color.Red.ToVector4()
            };

            gpsPage.Controls.Add(gpsComposite);
            gpsPage.Controls.Add(gpsNamePanel);
            gpsPage.Controls.Add(gpsNameLabel);
            gpsPage.Controls.Add(gpsDescLabel);
            gpsPage.Controls.Add(gpsDescText);

            gpsPage.Controls.Add(gpsLabelX);
            gpsPage.Controls.Add(gpsXCoord);
            gpsPage.Controls.Add(gpsLabelY);
            gpsPage.Controls.Add(gpsYCoord);
            gpsPage.Controls.Add(gpsLabelZ);
            gpsPage.Controls.Add(gpsZCoord);

            gpsPage.Controls.Add(toClipboardButton);

            gpsPage.Controls.Add(checkGpsShowOnHud);
            gpsPage.Controls.Add(labelGpsShowOnHud);

            gpsPage.Controls.Add(labelIllegalDataWarning);

            gpsPage.Controls.Add(checkGpsAlwaysVisible);
            gpsPage.Controls.Add(labelGpsAlwaysVisible);
        }
        #endregion

        #region populate properties
        private void CreatePropertiesPageControls(MyGuiControlParent menuParent, MyGuiControlParent panelParent)
        {
            m_propertiesTableParent.Name = "PropertiesTable";
            m_propertiesTopMenuParent.Name = "PropertiesTopMenu";
            //Combobox on top of Terminal
            var shipsInRange = new MyGuiControlCombobox()
            {
                Position = new Vector2(0,0f),
                Size = new Vector2(0.25f, 0.10f),
                Name = "ShipsInRange",
                Visible = false,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            };

            var selectShipButton = new MyGuiControlButton()
            {
                Position = new Vector2(0.265f, 0.0f),
                Size = new Vector2(0.2f, 0.05f),
                Name = "SelectShip",
                Text = MyTexts.GetString(MySpaceTexts.Terminal_RemoteControl_Button),
                TextScale = 0.9f,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                VisualStyle = MyGuiControlButtonStyleEnum.Small,
            };
            selectShipButton.SetToolTip(MySpaceTexts.ScreenTerminal_ShipList);

            menuParent.Controls.Add(shipsInRange);
            menuParent.Controls.Add(selectShipButton);

            //The panel itself
            var shipsDataTable = new MyGuiControlTable()
            {
                Position = new Vector2(0.0f, 0.0f),
                Size = new Vector2(0.88f, 0.78f),
                Name = "ShipsData",
                ColumnsCount = 5,
                VisibleRowsCount = 16,
                HeaderVisible = true,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
            };

            shipsDataTable.SetCustomColumnWidths(new float[] { 0.4f, 0.15f, 0.15f, 0.1f, 0.2f });
            shipsDataTable.SetColumnName(0, new StringBuilder("Name"));
            shipsDataTable.SetColumnName(1, new StringBuilder("Control"));
            shipsDataTable.SetColumnName(2, new StringBuilder("Distance"));
            shipsDataTable.SetColumnName(3, new StringBuilder("Status"));
            shipsDataTable.SetColumnName(4, new StringBuilder("Terminal Access"));
            shipsDataTable.SetHeaderColumnAlign(1, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            shipsDataTable.SetHeaderColumnAlign(4, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            shipsDataTable.SetColumnAlign(1, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            shipsDataTable.SetColumnAlign(4, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            shipsDataTable.SetColumnComparison(0, (a, b) => (a.Text).CompareTo(b.Text));
            shipsDataTable.SetColumnComparison(2, (a, b) => ((float)a.UserData).CompareTo((float)b.UserData));
            shipsDataTable.SetColumnComparison(3, (a, b) => ((Int32)a.UserData).CompareTo((Int32)b.UserData));

            panelParent.Controls.Add(shipsDataTable);
            panelParent.Visible = false;
        }
        #endregion

        #region event handlers
        protected override void OnClosed()
        {
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
            m_interactedEntity = null;

            MyAnalyticsHelper.ReportActivityEnd(m_instance.m_user, "show_terminal");

            if (MyFakes.ENABLE_GPS)
                m_controllerGps.Close();
            m_controllerControlPanel.Close();
            m_controllerInventory.Close();
            m_controllerProduction.Close();
            m_controllerInfo.Close();
            Controls.Clear();
            m_terminalTabs = null;
            m_controllerInventory = null;
            
            if (MyFakes.SHOW_FACTIONS_GUI)
            {
                m_controllerFactions.Close();
            }

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                m_controllerProperties.Close();
                m_controllerProperties.ButtonClicked -= PropertiesButtonClicked;
                m_propertiesTableParent = null;
                m_propertiesTopMenuParent = null;
            }

            if (MyFakes.ENABLE_COMMUNICATION)
            {
                m_controllerChat.Close();
            }

            m_instance = null;
            m_screenOpen = false;
            base.OnClosed();
        }

        void InfoButton_OnButtonClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_HELP_TERMINAL_SCREEN, "Steam Guide");
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            // Hack to ensure that player can type keys which are bound to TERMINAL and INVENTORY controls.
            // Unbuffered input can report key press before it arrives through buffered text input, which would
            // interfere with player typing.
            bool textboxHasFocus = FocusedControl is MyGuiControlTextbox;

            if (!textboxHasFocus && (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.TERMINAL) || MyInput.Static.IsNewGameControlPressed(MyControlsSpace.USE)))
            {
                GuiSounds closeEnum = m_closingCueEnum.HasValue ? m_closingCueEnum.Value : GuiSounds.MouseClick;
                MyGuiSoundManager.PlaySound(closeEnum);
                CloseScreen();
            }
            if (!textboxHasFocus && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.INVENTORY))
            {
                if (m_terminalTabs.SelectedPage == (int)MyTerminalPageEnum.Inventory)
                {
                    GuiSounds closeEnum = m_closingCueEnum.HasValue ? m_closingCueEnum.Value : GuiSounds.MouseClick;
                    MyGuiSoundManager.PlaySound(closeEnum);
                    CloseScreen();
                }
                else
                {
					SwitchToInventory();
                }
            }
            if (!textboxHasFocus && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.PAUSE_GAME))
            {
                MySandboxGame.PauseToggle();
            }

            if (!textboxHasFocus && MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsKeyPress(MyKeys.A))
            {
                if (m_instance.m_terminalTabs.SelectedPage == (int)MyTerminalPageEnum.ControlPanel)
                {
                    m_controllerControlPanel.SelectAllBlocks();
                }
            }

            base.HandleUnhandledInput(receivedFocusInThisUpdate);
        }

        public void PropertiesButtonClicked()
        {
            m_terminalTabs.SelectedPage = (int)MyTerminalPageEnum.Properties;
            m_controllerProperties.Refresh();
            m_propertiesTableParent.Visible = true;
        }

        public void Info_ShipRenamed()
        {
            m_controllerProperties.Refresh();
        }

        public void tabs_OnPageChanged()
        {
            if (m_propertiesTableParent.Visible)
                m_propertiesTableParent.Visible = false;
            if (m_instance.m_terminalTabs.SelectedPage == (int)MyTerminalPageEnum.Inventory && m_instance.m_controllerInventory != null)
                m_instance.m_controllerInventory.Refresh();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
            {
                if (m_terminalTabs.SelectedPage == (int) MyTerminalPageEnum.Gps)
                    m_controllerGps.OnDelKeyPressed();
            }
            base.HandleInput(receivedFocusInThisUpdate);
        }


        #endregion

        #region Static
        private static MyGuiScreenTerminal m_instance;

        public static void Show(MyTerminalPageEnum page, MyCharacter user, MyEntity interactedEntity)
        {
            if (!MyPerGameSettings.TerminalEnabled || !MyPerGameSettings.GUI.EnableTerminalScreen)
                return;

            bool showProperties = MyInput.Static.IsAnyShiftKeyPressed();
            Debug.Assert(m_instance == null);

            m_instance = new MyGuiScreenTerminal();
            m_instance.m_user = user;

            m_openInventoryInteractedEntity = interactedEntity;

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                m_instance.m_initialPage = showProperties ? MyTerminalPageEnum.Properties : page;
            }
            else
                m_instance.m_initialPage = page;

            InteractedEntity = interactedEntity;
            m_instance.RecreateControls(true);

            MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = m_instance);
            m_screenOpen = true;

            string target = interactedEntity != null ? interactedEntity.GetType().Name : "";
            MyAnalyticsHelper.ReportActivityStart(user, "show_terminal", target, "gui", string.Empty);
        }

        internal static void Hide()
        {
            if (m_instance != null)
            {
                m_instance.CloseScreen();
            }
        }

        public static void ChangeInteractedEntity(MyEntity interactedEntity)
        {
            InteractedEntity = interactedEntity;
        }


        public static MyGuiControlLabel CreateErrorLabel(MyStringId text, string name)
        {
            var label = new MyGuiControlLabel(
                text: MyTexts.GetString(text),
                font: MyFontEnum.Red,
                textScale: 1.5f * MyGuiConstants.DEFAULT_TEXT_SCALE,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            label.Name = name;
            label.Visible = true;
            return label;
        }

        public static void SwitchToControlPanelBlock(MyTerminalBlock block)
        {
            m_instance.m_terminalTabs.SelectedPage = (int)MyTerminalPageEnum.ControlPanel;
            m_instance.m_controllerControlPanel.SelectBlocks(new MyTerminalBlock[] { block });
        }

        public static void SwitchToInventory()
        {
            m_instance.m_terminalTabs.SelectedPage = (int)MyTerminalPageEnum.Inventory;
        }

        public override bool Update(bool hasFocus)
        {
            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                if (m_connected && m_terminalTabs.SelectedPage != (int)MyTerminalPageEnum.Properties && !m_controllerProperties.TestConnection())
                {
                    m_connected = false;
                    ShowDisconnectScreen();
                }
                else if (!m_connected && m_controllerProperties.TestConnection())
                {
                    m_connected = true;
                    ShowConnectScreen();
                }
                m_controllerProperties.Update();

                if (MyFakes.ENABLE_COMMUNICATION)
                {
                    m_controllerChat.Update();
                }
            }

            MyCubeGrid grid = (InteractedEntity != null && !InteractedEntity.Closed) ? InteractedEntity.Parent as MyCubeGrid : null;
            if (grid != null && grid.GridSystems.TerminalSystem != m_controllerControlPanel.TerminalSystem)
            {
                if (m_controllerControlPanel != null)
                {
                    m_controllerControlPanel.Close();

                    var controlPanelPage = (MyGuiControlTabPage)m_terminalTabs.Controls.GetControlByName("PageControlPanel");
                    m_controllerControlPanel.Init(controlPanelPage, MySession.Static.LocalHumanPlayer, grid, InteractedEntity as MyTerminalBlock, m_colorHelper);
                }

                if (m_controllerProduction != null)
                {
                    m_controllerProduction.Close();

                    var productionPage = m_terminalTabs.GetTabSubControl(2);
                    m_controllerProduction.Init(productionPage, grid);
                }

                if (m_controllerInventory != null)
                {
                    m_controllerInventory.Close();
                    
                    var inventoryPage = (MyGuiControlTabPage)m_terminalTabs.Controls.GetControlByName("PageInventory");
                    m_controllerInventory.Init(inventoryPage, m_user, InteractedEntity, m_colorHelper);
                }
            }

            return base.Update(hasFocus);
        }

        public void ShowDisconnectScreen()
        {
            m_terminalTabs.Visible = false;
            m_propertiesTableParent.Visible = false;
            m_terminalNotConnected.Visible = true;
        }

        public void ShowConnectScreen()
        {
            m_terminalTabs.Visible = true;
            m_propertiesTableParent.Visible = m_terminalTabs.SelectedPage == (int) MyTerminalPageEnum.Properties;
            m_terminalNotConnected.Visible = false;
        }

        public static MyTerminalPageEnum GetCurrentScreen()
        {
            if(IsOpen)
            {
                return (MyTerminalPageEnum)m_instance.m_terminalTabs.SelectedPage;
            }
            return MyTerminalPageEnum.None;
        }

        #endregion

    }
}

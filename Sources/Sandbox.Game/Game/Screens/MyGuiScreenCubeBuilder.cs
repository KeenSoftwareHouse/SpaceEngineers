#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Diagnostics;
using Sandbox.Engine.Utils;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Input;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenCubeBuilder : MyGuiScreenToolbarConfigBase
    {
        MyGuiControlButton m_smallShipButton;
        MyGuiControlButton m_largeShipButton;
        MyGuiControlButton m_stationButton;
        MyGuiControlButton m_goodAiBotButton;
        MyGuiControlBlockInfo m_blockInfoSmall;
        MyGuiControlBlockInfo m_blockInfoLarge;

        public MyGuiScreenCubeBuilder(int scrollOffset = 0, MyCubeBlock owner = null)
            : base(scrollOffset, owner)
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenCubeBuilder.ctor START");

            Static = this;

            m_scrollOffset = scrollOffset / 6.5f;
            m_size = new Vector2(1, 1);
            m_canShareInput = true;
            m_drawEvenWithoutFocus = true;
            EnabledBackgroundFade = true;
            m_screenOwner = owner;
            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenCubeBuilder.ctor END");
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenCubeBuilder";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            ProfilerShort.Begin("MyGuiScreenCubeBuilder.RecreateControls");

            bool showRightControls = !(MySession.Static.ControlledEntity is MyShipController) || (MySession.Static.ControlledEntity is MyShipController && (MySession.Static.ControlledEntity as MyShipController).BuildingMode) || MyToolbarComponent.GlobalBuilding;
            
            //Disable right buttons if current spectator is official spectator
            if (MySession.Static.SurvivalMode)
                showRightControls &= !(MySession.Static.IsCameraUserControlledSpectator() && !MyInput.Static.ENABLE_DEVELOPER_KEYS && MySession.Static.Settings.EnableSpectator);

            m_smallShipButton = (MyGuiControlButton)Controls.GetControlByName("ButtonSmall");
            m_smallShipButton.Visible = showRightControls;
            m_smallShipButton.ButtonClicked += smallShipButton_OnButtonClick;

            m_largeShipButton = (MyGuiControlButton)Controls.GetControlByName("ButtonLarge");
            m_largeShipButton.Visible = showRightControls;
            m_largeShipButton.ButtonClicked += largeShipButton_OnButtonClick;

            m_stationButton = (MyGuiControlButton)Controls.GetControlByName("ButtonStation");
            m_stationButton.Visible = showRightControls;
            m_stationButton.ButtonClicked += stationButton_OnButtonClick;



            if (m_screenCubeGrid != null && !(MySession.Static.ControlledEntity is MyShipController && (MySession.Static.ControlledEntity as MyShipController).BuildingMode))
            {
                m_smallShipButton.Visible = false;
                m_stationButton.Visible = false;
                m_largeShipButton.Visible = false;
                //m_goodAiBotButton.Visible = false;
            }

			var style = new MyGuiControlBlockInfo.MyControlBlockInfoStyle()
			{
				BlockNameLabelFont = MyFontEnum.White,
				EnableBlockTypeLabel = true,
				ComponentsLabelText = MySpaceTexts.HudBlockInfo_Components,
				ComponentsLabelFont = MyFontEnum.Blue,
				InstalledRequiredLabelText = MySpaceTexts.HudBlockInfo_Installed_Required,
				InstalledRequiredLabelFont = MyFontEnum.Blue,
                RequiredLabelText = MyCommonTexts.HudBlockInfo_Required,
				IntegrityLabelFont = MyFontEnum.White,
				IntegrityBackgroundColor = new Vector4(78 / 255.0f, 116 / 255.0f, 137 / 255.0f, 1.0f),
				IntegrityForegroundColor = new Vector4(0.5f, 0.1f, 0.1f, 1),
				IntegrityForegroundColorOverCritical = new Vector4(118 / 255.0f, 166 / 255.0f, 192 / 255.0f, 1.0f),
				LeftColumnBackgroundColor = new Vector4(46 / 255.0f, 76 / 255.0f, 94 / 255.0f, 1.0f),
				TitleBackgroundColor = new Vector4(72 / 255.0f, 109 / 255.0f, 130 / 255.0f, 1.0f),
				ComponentLineMissingFont = MyFontEnum.Red,
				ComponentLineAllMountedFont = MyFontEnum.White,
				ComponentLineAllInstalledFont = MyFontEnum.Blue,
				ComponentLineDefaultFont = MyFontEnum.White,
				ComponentLineDefaultColor = new Vector4(0.6f, 0.6f, 0.6f, 1f),
				ShowAvailableComponents = false,
				EnableBlockTypePanel = true,
			};
            m_blockInfoSmall = new MyGuiControlBlockInfo(style, false, false);
            m_blockInfoSmall.Visible = false;
            m_blockInfoSmall.IsActiveControl = false;
            m_blockInfoSmall.BlockInfo = new MyHudBlockInfo();
            m_blockInfoSmall.Position = new Vector2(0.28f, -0.04f);
            m_blockInfoSmall.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            Controls.Add(m_blockInfoSmall);
            m_blockInfoLarge = new MyGuiControlBlockInfo(style, false, true);
            m_blockInfoLarge.Visible = false;
            m_blockInfoLarge.IsActiveControl = false;
            m_blockInfoLarge.BlockInfo = new MyHudBlockInfo();
            m_blockInfoLarge.Position = new Vector2(0.28f, -0.06f);
            m_blockInfoLarge.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            Controls.Add(m_blockInfoLarge);

            ProfilerShort.End();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);
            if (m_gridBlocks.Visible && m_gridBlocks.MouseOverItem != null && m_gridBlocks.MouseOverItem.UserData is GridItemUserData)
            {
                if ((m_gridBlocks.MouseOverItem.UserData as GridItemUserData).ItemData is MyObjectBuilder_ToolbarItemCubeBlock)
                {
                    var block = (m_gridBlocks.MouseOverItem.UserData as GridItemUserData).ItemData as MyObjectBuilder_ToolbarItemCubeBlock;
                    MyDefinitionBase definition;
                    if (MyDefinitionManager.Static.TryGetDefinition(block.DefinitionId, out definition))
                    {
                        var group = MyDefinitionManager.Static.GetDefinitionGroup((definition as MyCubeBlockDefinition).BlockPairName);

                        if (group.Large != null)
                        {
                            m_blockInfoLarge.BlockInfo.LoadDefinition(group.Large);
                            m_blockInfoLarge.Visible = true;
                        }
                        else
                            m_blockInfoLarge.Visible = false;

                        if (group.Small != null)
                        {
                            m_blockInfoSmall.BlockInfo.LoadDefinition(group.Small);
                            m_blockInfoSmall.Visible = true;
                        }
                        else
                            m_blockInfoSmall.Visible = false;
                    }
                }
                //Case where grid item is used for new stastion/ship creation and not a CubeBlock directly
                else if ((m_gridBlocks.MouseOverItem.UserData as GridItemUserData).ItemData is MyObjectBuilder_ToolbarItemCreateGrid)
                {
                    var block = (m_gridBlocks.MouseOverItem.UserData as GridItemUserData).ItemData as MyObjectBuilder_ToolbarItemCreateGrid;
                    if (block.DefinitionId.SubtypeName.Equals("CreateSmallShip"))
                        CreateToolTipForNewGrid(MyCubeSize.Small, false);
                    else if (block.DefinitionId.SubtypeName.Equals("CreateLargeShip"))
                        CreateToolTipForNewGrid(MyCubeSize.Large, false);
                    else if (block.DefinitionId.SubtypeName.Equals("CreateStation"))
                        CreateToolTipForNewGrid(MyCubeSize.Large, true);
                }
                else
                {
                    m_blockInfoSmall.Visible = false;
                    m_blockInfoLarge.Visible = false;
                }
            }
            //button for new station/ship mouse over cases
            else if ((m_smallShipButton.Visible && m_smallShipButton.IsMouseOver))
                CreateToolTipForNewGrid(MyCubeSize.Small, false);
            else if (m_largeShipButton.Visible && m_largeShipButton.IsMouseOver)
                CreateToolTipForNewGrid(MyCubeSize.Large, false);
            else if (m_stationButton.Visible && m_stationButton.IsMouseOver)
                CreateToolTipForNewGrid(MyCubeSize.Large, true);
            else
            {
                m_blockInfoSmall.Visible = false;
                m_blockInfoLarge.Visible = false;
            }
        }

        /// <summary>
        /// Used in order to get material requirements from prefabs of the new station/ship and draw them to the hud
        /// </summary>
        void CreateToolTipForNewGrid(MyCubeSize size, bool isStatic)
        {
            bool isLarge = (size == MyCubeSize.Large);
            MyGuiControlBlockInfo usedInfo = isLarge ? m_blockInfoLarge : m_blockInfoSmall;
            MyGuiControlBlockInfo unusedInfo = !isLarge ? m_blockInfoLarge : m_blockInfoSmall;
            string prefabName;
            MyDefinitionManager.Static.GetBaseBlockPrefabName(size, isStatic, MySession.Static.CreativeMode, out prefabName);
            if (prefabName == null)
                return;
            var gridBuilders = MyPrefabManager.Static.GetGridPrefab(prefabName);
            Debug.Assert(gridBuilders != null && gridBuilders.Length > 0);
            if (gridBuilders == null || gridBuilders.Length == 0)
                return;

            MyCubeBlockDefinition blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(gridBuilders[0].CubeBlocks[0].GetId());
            if (blockDefinition != null)
            {
                usedInfo.BlockInfo.LoadDefinition(blockDefinition);
                usedInfo.Visible = true;
            }
            else
                usedInfo.Visible = false;
            unusedInfo.Visible = false;
        }

        void smallShipButton_OnButtonClick(MyGuiControlButton sender)
        {
            CreateGrid(MyCubeSize.Small, isStatic: false);
        }

        void largeShipButton_OnButtonClick(MyGuiControlButton sender)
        {
            CreateGrid(MyCubeSize.Large, isStatic: false);
        }

        void stationButton_OnButtonClick(MyGuiControlButton sender)
        {
            CreateGrid(MyCubeSize.Large, isStatic: true);
        }

        void goodAiBotButton_OnButtonClick(MyGuiControlButton sender)
        {
            //CreateGrid(MyCubeSize.Large, isStatic: true);
        }

        void CreateGrid(MyCubeSize cubeSize, bool isStatic)
        {
            if (!MyEntities.MemoryLimitReachedReport && !MySandboxGame.IsPaused)
            {
                MySessionComponentVoxelHand.Static.Enabled = false;
                MyCubeBuilder.Static.StartNewGridPlacement(cubeSize, isStatic);
                var character = MySession.Static.LocalCharacter;

                Debug.Assert(character != null);
                if (character != null)
                {
                    MyDefinitionId weaponDefinition = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));
                    character.SwitchToWeapon(weaponDefinition);
                }
            }
            CloseScreen();
        }

        public override bool AllowToolbarKeys()
        {
            return base.AllowToolbarKeys();
        }
    }
}
#region Using

using System;
using System.Collections.Generic;
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
using System.Text;
using Sandbox.Engine.Utils;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Input;
using VRage.Library.Collections;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenCubeBuilder : MyGuiScreenToolbarConfigBase
    {
        private MyGuiControlRadioButton m_rbGridSizeSmall;
        private MyGuiControlRadioButton m_rbGridSizeLarge;
        private MyGuiControlRadioButtonGroup m_rbGroupGridSize;
        MyGuiControlButton m_goodAiBotButton;
        private MyGuiControlList m_blockInfoList;
        //MyGuiControlBlockInfo m_blockInfoSmall;
        //MyGuiControlBlockInfo m_blockInfoLarge;
        private MyGuiControlBlockInfo.MyControlBlockInfoStyle m_blockInfoStyle;

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

            m_gridBlocks.MouseOverIndexChanged += OnGridMouseOverIndexChanged;
            m_gridBlocks.ItemSelected += OnSelectedItemChanged;

            m_blockInfoStyle = new MyGuiControlBlockInfo.MyControlBlockInfoStyle()
			{
				BlockNameLabelFont = MyFontEnum.White,
				EnableBlockTypeLabel = false,
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
                EnableBlockTypePanel = false,
			};

            m_rbGridSizeSmall = (MyGuiControlRadioButton)Controls.GetControlByName("GridSizeSmall");
            if(m_rbGridSizeSmall == null) 
                Debug.Fail("Someone changed CubeBuilder.gsc file in Content folder? Please check");

            m_rbGridSizeSmall.HighlightType = MyGuiControlHighlightType.NEVER;
            m_rbGridSizeSmall.SelectedChanged += OnGridSizeSmallSelected;

            m_rbGridSizeLarge = (MyGuiControlRadioButton)Controls.GetControlByName("GridSizeLarge");
            if (m_rbGridSizeLarge == null)
                Debug.Fail("Someone changed CubeBuilder.gsc file in Content folder? Please check");

            m_rbGridSizeLarge.HighlightType = MyGuiControlHighlightType.NEVER;
            m_rbGridSizeLarge.SelectedChanged += OnGridSizeLargeSelected;

            m_rbGroupGridSize = new MyGuiControlRadioButtonGroup { m_rbGridSizeSmall, m_rbGridSizeLarge };
            if (MyCubeBuilder.Static != null)
            {
                m_rbGroupGridSize.SelectedIndex = MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Small ? 0 : 1;
            }

            MyGuiControlLabel gridSizeLabel = (MyGuiControlLabel)Controls.GetControlByName("GridSizeHintLabel");
            gridSizeLabel.Text = string.Format(MyTexts.GetString(gridSizeLabel.TextEnum), MyGuiSandbox.GetKeyName(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE));

            m_blockInfoList = (MyGuiControlList)Controls.GetControlByName("BlockInfoPanel");
            if (m_blockInfoList == null)
                Debug.Fail("Someone changed CubeBuilder.gsc file in Content folder? Please check");



            //m_blockInfoSmall = new MyGuiControlBlockInfo(style, false, false);
            //m_blockInfoSmall.Visible = false;
            //m_blockInfoSmall.IsActiveControl = false;
            //m_blockInfoSmall.BlockInfo = new MyHudBlockInfo();
            //m_blockInfoSmall.Position = new Vector2(0.28f, -0.04f);
            //m_blockInfoSmall.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            //Controls.Add(m_blockInfoSmall);
            //m_blockInfoLarge = new MyGuiControlBlockInfo(style, false, true);
            //m_blockInfoLarge.Visible = false;
            //m_blockInfoLarge.IsActiveControl = false;
            //m_blockInfoLarge.BlockInfo = new MyHudBlockInfo();
            //m_blockInfoLarge.Position = new Vector2(0.28f, -0.06f);
            //m_blockInfoLarge.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            //Controls.Add(m_blockInfoLarge);

            ProfilerShort.End();
        }

        private void OnSelectedItemChanged(MyGuiControlGrid arg1, MyGuiControlGrid.EventArgs arg2)
        {
            OnGridMouseOverIndexChanged(arg1, arg2);
        }

        private void OnGridMouseOverIndexChanged(MyGuiControlGrid myGuiControlGrid, MyGuiControlGrid.EventArgs eventArgs)
        {
           
            if (m_gridBlocks.Visible)
            {
                MyGuiControlGrid.Item gridItem = m_gridBlocks.MouseOverItem ?? m_gridBlocks.SelectedItem;

                if (gridItem == null)
                {
                    m_blockInfoList.InitControls(new MyGuiControlBase[] { });
                    return;
                }

                GridItemUserData userData = gridItem.UserData as GridItemUserData;
                if (userData == null)
                    return;

                MyObjectBuilder_ToolbarItemCubeBlock itemData = userData.ItemData as MyObjectBuilder_ToolbarItemCubeBlock;
                if (itemData == null)
                    return;

                MyDefinitionBase definition;
                if (MyDefinitionManager.Static.TryGetDefinition(itemData.DefinitionId, out definition))
                {
                    var group = MyDefinitionManager.Static.GetDefinitionGroup((definition as MyCubeBlockDefinition).BlockPairName);

                    if (MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Small &&
                        MyCubeBuilder.Static.IsCubeSizeAvailable(group.Small))
                    {
                        m_blockInfoList.InitControls(GenerateBlockInfos(group.Small, ref m_blockInfoStyle));
                    }
                    else if (MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Large &&
                        MyCubeBuilder.Static.IsCubeSizeAvailable(group.Large))
                    {
                        m_blockInfoList.InitControls(GenerateBlockInfos(group.Large, ref m_blockInfoStyle));
                    }
                    else
                    {
                        bool blockSizeLarge = MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Large;
                        m_blockInfoList.InitControls(new MyGuiControlBase[]
                        {
                            GenerateSizeInfoLabel(blockSizeLarge),
                            GenerateSizeNotAvailableText(blockSizeLarge)
                        });
                    }
                }
                
            }
            else
            {
                m_blockInfoList.InitControls(new MyGuiControlBase[] { });
            }
        }

        /// <summary>
        /// Generates multiline text control indicating that block is not available.
        /// </summary>
        /// <param name="blockSizeLarge">Is block size large.</param>
        /// <returns>Multiline text control with block not available info.</returns>
        private MyGuiControlMultilineText GenerateSizeNotAvailableText(bool blockSizeLarge)
        {
            MyGuiControlMultilineText textControl = new MyGuiControlMultilineText(size: new Vector2(0.2f, 0.1f), font: MyFontEnum.Red, showTextShadow: true, textAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            string blockTypeLabelText = MyTexts.GetString(!blockSizeLarge ? MySpaceTexts.HudBlockInfo_LargeShip_Station : MySpaceTexts.HudBlockInfo_SmallShip);
            textControl.AppendText(string.Format(MyTexts.GetString(MySpaceTexts.BlockSize_NotAvailable), blockTypeLabelText));
            return textControl;
        }

        /// <summary>
        /// Generates label control containing block size info.
        /// </summary>
        /// <param name="blockSizeLarge">Is block size large</param>
        /// <returns>Label control with block size info</returns>
        private MyGuiControlLabel GenerateSizeInfoLabel(bool blockSizeLarge)
        {
            string blockTypeLabelText = MyTexts.GetString(blockSizeLarge ? MySpaceTexts.HudBlockInfo_LargeShip_Station : MySpaceTexts.HudBlockInfo_SmallShip);
            return new MyGuiControlLabel(text: blockTypeLabelText, font: MyFontEnum.White);
        }

        /// <summary>
        /// Generates list of block info controls from base block and its stages.
        /// </summary>
        /// <param name="blockDefinition">Definition of the block</param>
        /// <param name="blockInfoStyle">Block info style used in Block info control.</param>
        /// <returns>Array of block info controls.</returns>
        private MyGuiControlBase[] GenerateBlockInfos(MyCubeBlockDefinition blockDefinition, ref MyGuiControlBlockInfo.MyControlBlockInfoStyle blockInfoStyle)
        {
            int blockCt = blockDefinition.BlockStages != null ? blockDefinition.BlockStages.Length + 2 : 2;
            bool blockSizeLarge = blockDefinition.CubeSize == MyCubeSize.Large;

            MyGuiControlBase[] blockInfos = new MyGuiControlBase[blockCt];

            blockInfos[0] = GenerateSizeInfoLabel(blockSizeLarge);
            blockInfos[1] = CreateBlockInfoControl(blockDefinition, blockSizeLarge, ref blockInfoStyle);

            // No stages, just return base block info.
            if (blockCt == 1)
                return blockInfos;

            for (int idx = 0; idx < blockCt - 2; idx++)
            {
                MyCubeBlockDefinition blockStageDefinition = null;
                bool result = MyDefinitionManager.Static.TryGetDefinition(blockDefinition.BlockStages[idx], out blockStageDefinition);
                bool isAvailable = result && (blockStageDefinition.AvailableInSurvival || MySession.Static.CreativeMode);
                if (!isAvailable)
                    continue;

                blockInfos[idx + 2] = CreateBlockInfoControl(blockStageDefinition, blockSizeLarge, ref blockInfoStyle);
            }

            return blockInfos;
        }

        private MyGuiControlBlockInfo CreateBlockInfoControl(MyCubeBlockDefinition blockDefinition, bool blockSizeLarge, ref MyGuiControlBlockInfo.MyControlBlockInfoStyle blockInfoStyle)
        {
            MyGuiControlBlockInfo blockInfo = new MyGuiControlBlockInfo(blockInfoStyle, false, blockSizeLarge)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BlockInfo = new MyHudBlockInfo(),
            };
            blockInfo.BlockInfo.LoadDefinition(blockDefinition);
            blockInfo.RecalculateSize();

            return blockInfo;
        } 

        private void OnGridSizeLargeSelected(MyGuiControlRadioButton obj)
        {
            if (MyCubeBuilder.Static == null)
                return;

            if(obj.Selected)
                MyCubeBuilder.Static.CubeBuilderState.SetCubeSize(MyCubeSize.Large);

            this.UpdateGridControl();

        }

        private void OnGridSizeSmallSelected(MyGuiControlRadioButton obj)
        {
            if (MyCubeBuilder.Static == null)
                return;

            if (obj.Selected)
                MyCubeBuilder.Static.CubeBuilderState.SetCubeSize(MyCubeSize.Small);

            this.UpdateGridControl();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyCubeBuilder.Static == null)
                return;

            if (MyCubeBuilder.Static.IsCubeSizeModesAvailable && MyInput.Static.IsGameControlReleased(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE) &&
                !m_searchItemTextBox.HasFocus)
            {
                int selectionIdx = MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Large ? 0 : 1;
                int? selIdx = m_gridBlocks.SelectedIndex;
                float scrollValue = m_gridBlocksPanel.ScrollbarVPosition;
                m_rbGroupGridSize.SelectedIndex = selectionIdx;
                OnGridMouseOverIndexChanged(m_gridBlocks, new MyGuiControlGrid.EventArgs());
                m_gridBlocks.SelectedIndex = selIdx;
                m_gridBlocksPanel.ScrollbarVPosition = scrollValue;
                MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);

                return;
            }
            
        }

        ///// <summary>
        ///// Used in order to get material requirements from prefabs of the new station/ship and draw them to the hud
        ///// </summary>
        //void CreateToolTipForNewGrid(MyCubeSize size, bool isStatic)
        //{
        //    bool isLarge = (size == MyCubeSize.Large);
           // MyGuiControlBlockInfo usedInfo = isLarge ? m_blockInfoLarge : m_blockInfoSmall;
           // MyGuiControlBlockInfo unusedInfo = !isLarge ? m_blockInfoLarge : m_blockInfoSmall;
            //string prefabName;
            //MyDefinitionManager.Static.GetBaseBlockPrefabName(size, isStatic, MySession.Static.CreativeMode, out prefabName);
            //if (prefabName == null)
            //    return;
            //var gridBuilders = MyPrefabManager.Static.GetGridPrefab(prefabName);
            //Debug.Assert(gridBuilders != null && gridBuilders.Length > 0);
            //if (gridBuilders == null || gridBuilders.Length == 0)
            //    return;

            //MyCubeBlockDefinition blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(gridBuilders[0].CubeBlocks[0].GetId());
            //if (blockDefinition != null)
            //{
            //    usedInfo.BlockInfo.LoadDefinition(blockDefinition);
            //    usedInfo.Visible = true;
            //}
            //else
            //    usedInfo.Visible = false;
            //unusedInfo.Visible = false;
        //}
    }
}
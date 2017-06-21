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
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenCubeBuilder : MyGuiScreenToolbarConfigBase
    {
        private MyGuiControlList m_blockInfoList;
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

            m_blockInfoList = (MyGuiControlList)Controls.GetControlByName("BlockInfoPanel");
            if (m_blockInfoList == null)
                Debug.Fail("Someone changed CubeBuilder.gsc file in Content folder? Please check");


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

                    List<MyGuiControlBase> blockInfos = null;

                    if (group.Small != null)
                    {
                        var blockInfosSmall = GenerateBlockInfos(group.Small, ref m_blockInfoStyle);
                        if (blockInfosSmall != null)
                        {
                            if (blockInfos == null)
                                blockInfos = new List<MyGuiControlBase>();
                            blockInfos.AddArray(blockInfosSmall);
                        }
                    }
 
                    if (group.Large != null)
                    {
                        var blockInfosLarge = GenerateBlockInfos(group.Large, ref m_blockInfoStyle);
                        if (blockInfosLarge != null)
                        {
                            if (blockInfos == null)
                                blockInfos = new List<MyGuiControlBase>();
                            blockInfos.AddArray(blockInfosLarge);
                        }
                    }

                    if (blockInfos != null)
                        m_blockInfoList.InitControls(blockInfos);
                    else
                    {
                        m_blockInfoList.InitControls(new MyGuiControlBase[] { });
                    }

                    //else
                    //{
                    //    bool blockSizeLarge = MyCubeBuilder.Static.CubeBuilderState.CubeSizeMode == MyCubeSize.Large;
                    //    m_blockInfoList.InitControls(new MyGuiControlBase[]
                    //    {
                    //        GenerateSizeInfoLabel(blockSizeLarge),
                    //        GenerateSizeNotAvailableText(blockSizeLarge)
                    //    });
                    //}
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
    }
}
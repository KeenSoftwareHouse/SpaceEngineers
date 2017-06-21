using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Entity;
using VRage;
using Sandbox.Game.SessionComponents;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemCubeBlock))]
    public class MyToolbarItemCubeBlock : MyToolbarItemDefinition
    {
        public static readonly string VariantsAvailableSubicon = @"Textures\GUI\Icons\VariantsAvailable.dds";

        private MyFixedPoint m_lastAmount = 0;
        public MyFixedPoint Amount
        {
            get { return m_lastAmount; }
        }

        public override bool Activate()
        {
            var character = MySession.Static.LocalCharacter;

            MyDefinitionId weaponDefinition = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));
            if (character != null)
            {
                if (!(character.CurrentWeapon != null && character.CurrentWeapon.DefinitionId == weaponDefinition))
                {
                    character.SwitchToWeapon(weaponDefinition);
                }

                MyCubeBuilder.Static.Activate(((MyCubeBlockDefinition)Definition).Id);
            }
            else
            if (MyCubeBuilder.SpectatorIsBuilding)
            {
                MyCubeBuilder.Static.Activate(((MyCubeBlockDefinition)Definition).Id);
            }
            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type == MyToolbarType.Character || type == MyToolbarType.Spectator || type == MyToolbarType.BuildCockpit);
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool result = base.Init(data);
            ActivateOnClick = false;
            var cubeDef = Definition as MyCubeBlockDefinition;
            if (result && cubeDef != null && cubeDef.BlockStages != null && cubeDef.BlockStages.Length > 0)
            {
                SetSubIcon(VariantsAvailableSubicon);
            }
            return result;
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            ChangeInfo changed = ChangeInfo.None;
            bool enable = true;

            if (MyCubeBuilder.Static == null)
                return changed;
            var blockDefinition = MyCubeBuilder.Static.IsActivated ? MyCubeBuilder.Static.ToolbarBlockDefinition : null;
            var blockDef = (this.Definition as Sandbox.Definitions.MyCubeBlockDefinition);
            if ((MyCubeBuilder.Static.IsActivated /*|| MyCubeBuilder.Static.MultiBlockCreationIsActivated*/) && blockDefinition != null)
            {
                if (blockDefinition.BlockPairName == blockDef.BlockPairName)
                {
                    WantsToBeSelected = true;
                }
                else if (blockDef.BlockStages != null && blockDef.BlockStages.Contains(blockDefinition.Id))
                {
                    WantsToBeSelected = true;
                }
                else
                {
                    WantsToBeSelected = false;
                }
            }
            else
            {
                WantsToBeSelected = false;
            }

            var character = MySession.Static.LocalCharacter;
            if (MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID)
            {
                if (blockDef.CubeSize == MyCubeSize.Small && character != null)
                {
                    var inventory = character.GetInventory();
                    MyFixedPoint amount = inventory != null ? inventory.GetItemAmount(Definition.Id) : 0;
                    if (m_lastAmount != amount)
                    {
                        m_lastAmount = amount;
                        changed |= ChangeInfo.IconText;
                    }

                    if (MySession.Static.SurvivalMode)
                    {
                        enable &= m_lastAmount > 0;
                    }
                    else
                    {
                        // so that we correctly set icontext when changing from enabled to disabled even when the amount is the same
                        changed |= ChangeInfo.IconText;
                    }
                }
            }

            if (MyPerGameSettings.EnableResearch && MySessionComponentResearch.Static != null && (blockDef.CubeSize == MyCubeSize.Large))
                enable &= MySessionComponentResearch.Static.CanUse(character, Definition.Id);

            if (MyCubeBuilder.Static != null)
                enable &= MyCubeBuilder.Static.IsCubeSizeAvailable(blockDef);

            return changed;
        }

        public override void FillGridItem(MyGuiControlGrid.Item gridItem)
        {
            if (MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID)
                if (m_lastAmount > 0)
                    gridItem.AddText(String.Format("{0}x", m_lastAmount), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                else
                    gridItem.ClearText(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
        }
    }
}

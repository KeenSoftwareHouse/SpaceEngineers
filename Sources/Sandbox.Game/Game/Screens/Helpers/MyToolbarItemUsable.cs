using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.Utils;
using System.Text;
using Sandbox.Graphics.GUI;
using System;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemUsable))]
    public class MyToolbarItemUsable : MyToolbarItemDefinition
    {
        public MyInventory Inventory
        {
            get
            {
                var character = MySession.Static.ControlledEntity as MyCharacter;
                if (character == null)
                {
                    return null;
                }
                return character.GetInventory() as MyInventory;
            }
        }

        private MyFixedPoint m_lastAmount = 0;
        public MyFixedPoint Amount
        {
            get { return m_lastAmount; }
        }

        public override bool Activate()
        {
            var itemAmount = Inventory != null ? Inventory.GetItemAmount(Definition.Id) : 0;
            bool available = itemAmount > 0;
            if (available)
            {
                var character = MySession.Static.ControlledEntity as MyCharacter;
                itemAmount = MyFixedPoint.Min(itemAmount, 1);
                if (character != null && itemAmount > 0)
                {
                    Inventory.ConsumeItem(Definition.Id, itemAmount, character.EntityId);
                }
            }
            return true;
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool result = base.Init(data);
            ActivateOnClick = false;
            WantsToBeActivated = false;
            return result;
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            if (Definition == null)
            {
                return null;
            }

            MyObjectBuilder_ToolbarItemUsable builder = (MyObjectBuilder_ToolbarItemUsable)MyToolbarItemFactory.CreateObjectBuilder(this);
            builder.DefinitionId = Definition.Id;

            return builder;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return type == MyToolbarType.Character;
        }

        public override MyToolbarItem.ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            var character = MySession.Static.LocalCharacter;
            ChangeInfo changed = ChangeInfo.None;

            if (character != null)
            {
                var inventory = character.GetInventory();
                MyFixedPoint amount = inventory != null ? inventory.GetItemAmount(Definition.Id) : 0;
                if (m_lastAmount != amount)
                {
                    m_lastAmount = amount;
                    changed |= ChangeInfo.IconText;
                }
            }
            bool enabled = m_lastAmount > 0;
            return changed | SetEnabled(enabled);
        }

        public override void FillGridItem(MyGuiControlGrid.Item gridItem)
        {
            if (m_lastAmount > 0)
                gridItem.AddText(String.Format("{0}x", m_lastAmount), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            else
                gridItem.ClearText(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
        }
    }
}

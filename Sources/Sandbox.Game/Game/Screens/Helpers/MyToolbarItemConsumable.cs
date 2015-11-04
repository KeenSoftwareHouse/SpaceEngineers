﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemConsumable))]
    public class MyToolbarItemConsumable : MyToolbarItemDefinition
    {
        public MyInventory Inventory
        {
            get
            {
                var character = MySession.ControlledEntity as MyCharacter;
                if (character == null)
                {
                    return null;
                }
                return character.GetInventory();
            }
        }

        public override bool Activate()
        {
            var itemAmount = Inventory != null ? Inventory.GetItemAmount(Definition.Id) : 0;
            bool available = itemAmount > 0;
            if (available)
            {
                var character = MySession.ControlledEntity as MyCharacter;
                itemAmount = MyFixedPoint.Min(itemAmount, 1);
                if (character != null && character.StatComp != null && itemAmount > 0)
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

            MyObjectBuilder_ToolbarItemConsumable builder = (MyObjectBuilder_ToolbarItemConsumable)MyToolbarItemFactory.CreateObjectBuilder(this);
            builder.DefinitionId = Definition.Id;            

            return builder;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return type == MyToolbarType.Character;
        }

        public override MyToolbarItem.ChangeInfo Update(Entities.MyEntity owner, long playerID = 0)
        {            
            bool enabled = Inventory != null ? Inventory.GetItemAmount(Definition.Id) > 0 : false; 
            
            return SetEnabled(enabled);
        }        
    }
}

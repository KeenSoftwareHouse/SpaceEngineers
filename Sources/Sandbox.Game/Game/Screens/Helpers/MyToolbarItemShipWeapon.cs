using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Definitions;
using VRageMath;

/* Class that controls ship blocks and what happens when you put them in your toolbar and activate them (switch on/off the ship part on control panel) */

namespace Sandbox.Game.Screens.Helpers
{
    class MyToolbarItemShipWeapon : MyToolbarItem
    {
        public Vector3I Position { get; set; }

        public override bool Activate()
        {
            var controlledObject = MySession.ControlledObject as IControllableEntity;
            if (controlledObject != null)
                controlledObject.SwitchToWeapon(Definition.Id);

            return true;
        }

        public override void Deactivate()
        {
        }

        internal override void Update(ref MyToolbarAffectingState state)
        {
            base.Update(ref state);
        }

        //two ship blocks are equal iff they have the same definition and are on the same position relative to the spaceship
        public override bool Equals(object obj)
        {
            var otherObj = obj as MyToolbarItemShipBlock;
            if (otherObj == null)
                return false;
            if (!Position.Equals(otherObj.Position))
                return false;

            return base.Equals(obj);
        }
    }
}

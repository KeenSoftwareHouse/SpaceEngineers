#region Using

using System;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Definitions;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.Entity;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AmmoMagazine))]
    public class MyAmmoMagazine : MyBaseInventoryItemEntity
    {
        public MyAmmoMagazine()
        {
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {          
         	base.Init(objectBuilder);
        }  
    }
}

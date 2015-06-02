#region Using

using System;
using System.Text;
using VRageMath;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Definitions;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AmmoMagazine))]
    class MyAmmoMagazine : MyBaseInventoryItemEntity
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

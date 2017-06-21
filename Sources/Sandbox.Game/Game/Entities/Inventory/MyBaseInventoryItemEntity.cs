#region Using

using System;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Graphics;
using Sandbox.Game.Components;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity;

#endregion

namespace Sandbox.Game.Weapons
{
    /*
     Possibly renamed to MyDroppedItemEntity or something.
     * Created when an item is dropped from inventory. Model and physics could probably be created based on definition (?)
     */

    public class MyBaseInventoryItemEntity : MyEntity
    {
        #region Fields

        MyPhysicalItemDefinition m_definition;
        float m_amount;

        public string[] IconTextures { get { return m_definition.Icons; } }

        #endregion

        #region Init

        public MyBaseInventoryItemEntity()
        {
            Render = new Components.MyRenderComponentInventoryItem();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            m_definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(objectBuilder.GetId());

            Init(null, m_definition.Model, null, null, null);

            Render.SkipIfTooSmall = false;
            Render.NeedsDraw = true;

            this.InitSpherePhysics(MyMaterialType.METAL, Model, 1, 1, 1, 0, RigidBodyFlag.RBF_DEFAULT);

            Physics.Enabled = true;
        }

        #endregion
    }
}

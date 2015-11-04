using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemVoxelHand))]
    public class MyToolbarItemVoxelHand : MyToolbarItemDefinition
    {
        public override bool Init(MyObjectBuilder_ToolbarItem objBuilder)
        {
            base.Init(objBuilder);

            WantsToBeSelected = false;
            ActivateOnClick = false;
            return true;
        }

        public override bool Activate()
        {
            if (Definition == null)
                return false;

            if      (Definition.Id.SubtypeName == "Box")       MySessionComponentVoxelHand.Static.CurrentShape = MyBrushBox.Static;
            else if (Definition.Id.SubtypeName == "Capsule")   MySessionComponentVoxelHand.Static.CurrentShape = MyBrushCapsule.Static;
            else if (Definition.Id.SubtypeName == "Ramp")      MySessionComponentVoxelHand.Static.CurrentShape = MyBrushRamp.Static;
            else if (Definition.Id.SubtypeName == "Sphere")    MySessionComponentVoxelHand.Static.CurrentShape = MyBrushSphere.Static;
            else if (Definition.Id.SubtypeName == "AutoLevel") MySessionComponentVoxelHand.Static.CurrentShape = MyBrushAutoLevel.Static;
            else if (Definition.Id.SubtypeName == "Ellipsoid") MySessionComponentVoxelHand.Static.CurrentShape = MyBrushEllipsoid.Static;

            if (MySessionComponentVoxelHand.Static.CurrentShape != null)
            {
                MySessionComponentVoxelHand.Static.Enabled = MySession.Static.CreativeMode;
                MySessionComponentVoxelHand.Static.CurrentDefinition = Definition as MyVoxelHandDefinition;
                var controlledObject = MySession.ControlledEntity as IMyControllableEntity;
                if (controlledObject != null)
                {
                  controlledObject.SwitchToWeapon(null);
                }

                if (MySessionComponentVoxelHand.Static.Enabled)
                {
                    // Some parts of the cubebuilder can be active (clipboards) without cube placer
                    if (MyCubeBuilder.Static.IsActivated)
                        MyCubeBuilder.Static.Deactivate();
                }

                return true;
            }
            return false;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type == MyToolbarType.Character || type == MyToolbarType.Spectator);
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            var blockDefinition = MySessionComponentVoxelHand.Static.Enabled ? MySessionComponentVoxelHand.Static.CurrentDefinition : null;
            WantsToBeSelected   = MySessionComponentVoxelHand.Static.Enabled && blockDefinition != null && blockDefinition.Id.SubtypeId == (this.Definition as MyVoxelHandDefinition).Id.SubtypeId;
            return ChangeInfo.None;
        }
    }
}

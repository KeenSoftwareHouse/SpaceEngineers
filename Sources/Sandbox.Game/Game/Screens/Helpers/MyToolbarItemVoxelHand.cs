using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRageMath;
using VRage.Game.Entity;

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

            bool exists = MySessionComponentVoxelHand.Static.TrySetBrush(Definition.Id.SubtypeName);
            if (!exists)
                return false;

            bool isCreative = MySession.Static.CreativeMode || MySession.Static.CreativeToolsEnabled(Sync.MyId);

            if (isCreative)
                MySession.Static.GameFocusManager.Clear();

            MySessionComponentVoxelHand.Static.Enabled = isCreative;
            if (MySessionComponentVoxelHand.Static.Enabled)
            {
                MySessionComponentVoxelHand.Static.CurrentDefinition = Definition as MyVoxelHandDefinition;
                var controlledObject = MySession.Static.ControlledEntity as IMyControllableEntity;
                if (controlledObject != null)
                {
                    controlledObject.SwitchToWeapon(null);
                }

                //if (MySessionComponentVoxelHand.Static.Enabled)
                //{
                // Some parts of the cubebuilder can be active (clipboards) without cube placer
                //if (MyCubeBuilder.Static.IsActivated)
                //    MyCubeBuilder.Static.Deactivate();
                //}
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
            if (MySessionComponentVoxelHand.Static == null)
                return ChangeInfo.None;

            var blockDefinition = MySessionComponentVoxelHand.Static.Enabled ? MySessionComponentVoxelHand.Static.CurrentDefinition : null;
            WantsToBeSelected   = MySessionComponentVoxelHand.Static.Enabled && blockDefinition != null && blockDefinition.Id.SubtypeId == (this.Definition as MyVoxelHandDefinition).Id.SubtypeId;
            return ChangeInfo.None;
        }
    }
}

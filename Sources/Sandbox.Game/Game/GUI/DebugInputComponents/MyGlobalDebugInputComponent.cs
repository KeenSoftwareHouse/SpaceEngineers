#region Using

using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage;
using VRage.Input;

#endregion

namespace Sandbox.Game.Gui
{
    class MyGlobalInputComponent : MyDebugComponent
    {
        public override string GetName()
        {
            return "Global";
        }

        public MyGlobalInputComponent()
        {
            AddShortcut(MyKeys.Space, true, true, false, false,
               () => "Teleport controlled object to camera position",
               delegate
               {
                   if (MySession.Static.CameraController == MySpectator.Static && MySession.Static.ControlledEntity != null)
                   {
                       MySession.Static.ControlledEntity.Entity.GetTopMostParent().PositionComp.SetPosition(MySpectator.Static.Position);
                   }
                   return true;
               });

            AddShortcut(MyKeys.NumPad2, true, false, false, false,
               () => "Apply backward linear impulse x100",
               delegate
               {
                   var body = MySession.Static.ControlledEntity.Entity.GetTopMostParent().Physics;
                   if (body != null && body.RigidBody != null)
                       body.RigidBody.ApplyLinearImpulse(MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward * body.Mass * -100);
                   return true;
               });

            AddShortcut(MyKeys.NumPad3, true, false, false, false,
               () => "Apply linear impulse x100",
               delegate
               {
                   var body = MySession.Static.ControlledEntity.Entity.GetTopMostParent().Physics;
                   if (body != null && body.RigidBody != null)
                       body.RigidBody.ApplyLinearImpulse(MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward * body.Mass * 100);
                   return true;
               });

            AddShortcut(MyKeys.NumPad6, true, false, false, false,
               () => "Apply linear impulse x20",
               delegate
               {
                   var body = MySession.Static.ControlledEntity.Entity.GetTopMostParent().Physics;
                   if (body != null && body.RigidBody != null)
                       body.RigidBody.ApplyLinearImpulse(MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward * body.Mass * 20);
                   return true;
               });

            AddShortcut(MyKeys.Z, true, true, true, false,
               () => "Save clipboard as prefab",
               delegate
               {
                  MyCubeBuilder.Static.Clipboard.SaveClipboardAsPrefab();
                   return true;
               });
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;

            if (base.HandleInput())
                return true;

            bool handled = false;

            return handled;
        }

    }
}

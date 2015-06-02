#region Using

using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Linq;
using VRage.Game.Entity.UseObject;
using VRage.Input;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    class MyCharacterInputComponent : MyDebugComponent
    {
        private bool m_toggleMovementState = false;

        public override string GetName()
        {
            return "Character";
        }

        public MyCharacterInputComponent()
        {
            AddShortcut(MyKeys.U, true, false, false, false,
               () => "Spawn new character",
               delegate
               {
                   var character = SpawnCharacter();
                   return true;
               });

            AddShortcut(MyKeys.NumPad7, true, true, false, false,
               () => "Use next ship",
               delegate
               {
                   UseNextShip();
                   return true;
               });

            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Toggle character movement status", () => { ShowMovementState(); return true; });
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

        public static MyCharacter SpawnCharacter(string model = null)
        {
            var charObject = MySession.LocalHumanPlayer == null ? null : MySession.LocalHumanPlayer.Identity.Character as MyCharacter;
            Vector3? colorMask = null;

            string name = MySession.LocalHumanPlayer == null ? "" : MySession.LocalHumanPlayer.Identity.DisplayName;
            string currentModel = MySession.LocalHumanPlayer == null ? MyCharacter.DefaultModel : MySession.LocalHumanPlayer.Identity.Model;

            if (charObject != null)
                colorMask = charObject.ColorMask;

            var character = MyCharacter.CreateCharacter(MatrixD.CreateTranslation(MySector.MainCamera.Position), Vector3.Zero, name, model == null ? currentModel : model, colorMask, false);
            return character;
        }


        public static void UseNextShip()
        {
            MyCockpit first = null;
            object previous = null;
            foreach (var g in MyEntities.GetEntities().OfType<MyCubeGrid>())
            {
                //if (g.GridSizeEnum == CommonLib.ObjectBuilders.MyCubeSize.Large)
                {
                    foreach (var cockpit in g.GetBlocks().Select(s => s.FatBlock as MyCockpit).Where(s => (s != null)))
                    {
                        if (first == null && cockpit.Pilot == null)
                            first = cockpit;

                        if (previous == MySession.ControlledEntity)
                        {
                            if (cockpit.Pilot == null)
                            {
                                UseCockpit(cockpit);
                                return;
                            }
                        }
                        else
                        {
                            previous = cockpit;
                        }
                    }
                }
            }

            if (first != null)
            {
                UseCockpit(first);
            }
        }

        private static void UseCockpit(MyCockpit cockpit)
        {
            if (MySession.LocalHumanPlayer == null) return;

            // Leave current cockpit if controlling any
            if (MySession.ControlledEntity is MyCockpit)
            {
                MySession.ControlledEntity.Use();
            }
            cockpit.RequestUse(UseActionEnum.Manipulate, (MyCharacter)MySession.LocalHumanPlayer.Identity.Character);
            cockpit.RemoveOriginalPilotPosition();
        }

        private void ShowMovementState()
        {
            m_toggleMovementState = !m_toggleMovementState;
        }

        public override void Draw()
        {
            base.Draw();

            if (m_toggleMovementState)
            {
                var allCharacters = MyEntities.GetEntities().OfType<MyCharacter>();
                Vector2 initPos = new Vector2(10, 200);
                foreach (var character in allCharacters)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(initPos, character.GetCurrentMovementState().ToString(), Color.Green, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    initPos += new Vector2(0, 20);
                }
            }
        }
    }
}

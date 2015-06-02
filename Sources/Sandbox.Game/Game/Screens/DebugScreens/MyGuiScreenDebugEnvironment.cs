using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Game", "Environment")]
    public class MyGuiScreenDebugEnvironment : MyGuiScreenDebugBase
    {
        public static Action DeleteEnvironmentItems;

        public MyGuiScreenDebugEnvironment()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugEnvironment";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddCaption("Environmnent", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddButton(new StringBuilder("Delete trees"), onClick: DeleteItems);
        }

        private void DeleteItems(MyGuiControlButton sender)
        {
            var treeType = MyObjectBuilderType.Parse("MyObjectBuilder_Trees");
            var items = new System.Collections.Generic.HashSet<Sandbox.Game.Entities.MyEntity>();

            foreach (var entity in Sandbox.Game.Entities.MyEntities.GetEntities())
            {
                if (entity.GetObjectBuilder().TypeId == treeType)
                {
                    items.Add(entity);
                }
            }

            foreach (var item in items)
            {
                item.Close();
            }
        }
    }
}
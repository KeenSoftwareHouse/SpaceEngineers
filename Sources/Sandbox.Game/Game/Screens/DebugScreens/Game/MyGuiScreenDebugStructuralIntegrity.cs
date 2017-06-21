using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;

using Sandbox.Game.World;
using Sandbox.Engine.Models;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using Sandbox.Game.GUI.DebugInputComponents;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.Multiplayer;

namespace Sandbox.Game.Gui
{
#if !XB1

    [MyDebugScreen("Game", "Structural Integrity")]
    class MyGuiScreenDebugStructuralIntegrity : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_animationComboA;
        MyGuiControlCombobox m_animationComboB;
        MyGuiControlSlider m_blendSlider;

        MyGuiControlCombobox m_animationCombo;
        MyGuiControlCheckbox m_loopCheckbox;

        public MyGuiScreenDebugStructuralIntegrity()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Structural integrity", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f * m_scale;

            AddCheckBox("Enabled", null, MemberHelper.GetMember(() => MyStructuralIntegrity.Enabled));
            AddCheckBox("Draw numbers", null, MemberHelper.GetMember(() => MyAdvancedStaticSimulator.DrawText));
            AddSlider("Closest distance threshold", 0, 16, () => MyAdvancedStaticSimulator.ClosestDistanceThreshold, (x) => MyAdvancedStaticSimulator.ClosestDistanceThreshold = x);

            AddButton(new StringBuilder("Delete fractures"), delegate { DeleteFractures(); });

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugStructuralIntegrity";
        }

        void DeleteFractures()
        {
            if (Sync.IsServer)
            {
                foreach (var entity in Sandbox.Game.Entities.MyEntities.GetEntities())
                {
                    if (entity is Sandbox.Game.Entities.MyFracturedPiece)
                    {
                        Sandbox.Game.GameSystems.MyFracturedPiecesManager.Static.RemoveFracturePiece(entity as Sandbox.Game.Entities.MyFracturedPiece, 0);
                    }

                    if (entity is Sandbox.Game.Entities.MyCubeGrid)
                    {
                        var blocks = (entity as Sandbox.Game.Entities.MyCubeGrid).GetBlocks();
                        foreach (var block in blocks)
                        {
                        }
                    }
                }
            }
        }
    }

#endif
}

#region Using
using System.Text;
using VRageMath;

using Sandbox.Engine.Utils;


using Sandbox.Graphics.GUI;
using Sandbox.Common;
using VRage;

using System;
using Sandbox.Graphics;



#endregion

namespace Sandbox.Game.Gui
{
#if !XB1
    [MyDebugScreen("VRage", "System")]
    class MyGuiScreenDebugSystem : MyGuiScreenDebugBase
    {
        private MyGuiControlMultilineText m_havokStatsMultiline;
        private StringBuilder m_buffer = new StringBuilder();

        public MyGuiScreenDebugSystem()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("System debug", Color.Yellow.ToVector4());
            AddShareFocusHint();


            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddLabel("System", Color.Yellow.ToVector4(), 1.2f);

            //System debugging
            AddCheckBox("Simulate slow update", null, MemberHelper.GetMember(() => MyFakes.SIMULATE_SLOW_UPDATE));
            AddButton(new StringBuilder("Force GC"), onClick: OnClick_ForceGC);
            AddCheckBox("Pause physics", null, MemberHelper.GetMember(() => MyFakes.PAUSE_PHYSICS));
            AddButton(new StringBuilder("Step physics"), (button) => MyFakes.STEP_PHYSICS = true);
            AddSlider("Simulation speed", 0.001f, 3f, null, MemberHelper.GetMember(() => MyFakes.SIMULATION_SPEED));

            m_currentPosition.Y += 0.01f;
            m_havokStatsMultiline = AddMultilineText(textScale: 0.8f);
        }

        public override bool Draw()
        {
            Havok.HkBaseSystem.GetMemoryStatistics(m_buffer);
            m_havokStatsMultiline.Clear();
            m_havokStatsMultiline.AppendText(m_buffer);
            m_buffer.Clear();

            return base.Draw();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugGame";
        }

        private void OnClick_ForceGC(MyGuiControlButton button)
        {
            GC.Collect();
        }
    }
#endif
}

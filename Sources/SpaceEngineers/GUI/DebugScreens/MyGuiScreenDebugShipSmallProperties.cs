using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Small Ship properties")]
    class MyGuiScreenDebugShipSmallProperties : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugShipSmallProperties()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("System small ship properties", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddLabel("Front light", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider(new StringBuilder("Small thrust glare size"), 0.01f, 10.0f, null, MemberHelper.GetMember(() => MyThrust.SMALL_BLOCK_GLARE_SIZE_SMALL));
            //AddSlider(new StringBuilder("Large thrust glare size"), 0.01f, 10.0f, null, MemberHelper.GetMember(() => MyThrust.SMALL_BLOCK_GLARE_SIZE_LARGE));

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugShipSmallProperties";
        }
    }
}

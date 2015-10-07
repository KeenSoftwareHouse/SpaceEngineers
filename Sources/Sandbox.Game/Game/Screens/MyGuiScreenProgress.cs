
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game
{
    public class MyGuiScreenProgress : MyGuiScreenProgressBase
    {
        public event Action Tick;

        public MyGuiScreenProgress(StringBuilder text, MyStringId? cancelText = null)
            : base(MySpaceTexts.Blank, cancelText)
        {
            // Copy
            Text = new StringBuilder(text.Length);
            Text.AppendStringBuilder(text);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            m_rotatingWheel.MultipleSpinningWheels = MyPerGameSettings.GUI.MultipleSpinningWheels;
        }

        protected override void ProgressStart()
        {
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenProgress";
        }

        public StringBuilder Text
        {
            get { return m_progressTextLabel.TextToDraw; }
            set { m_progressTextLabel.TextToDraw = value; }
        }

        public override bool Update(bool hasFocus)
        {
            var handler = Tick;
            if (handler != null && !((MyGuiScreenBase)this).Cancelled) handler();

            return base.Update(hasFocus);
        }
    }
}

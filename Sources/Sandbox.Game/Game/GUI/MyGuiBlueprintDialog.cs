#region Using
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using VRage;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GUI;
using System.Drawing;
#endregion

namespace Sandbox.Game.Gui
{


    class MyGuiBlueprintTextDialog : MyGuiBlueprintScreenBase
    {
        private MyGuiControlTextbox m_nameBox;
        private string m_defaultName;
        private string m_caption;
        private int m_maxTextLength;
        private float m_textBoxWidth;

        Action<string> callBack;
        Vector2 WINDOW_SIZE = new Vector2(0.3f, 0.5f);

        public MyGuiBlueprintTextDialog(Vector2 position, Action<string> callBack, string defaultName, string caption = "", int maxLenght = 20, float textBoxWidth = 0.2f) :
            base(position, new Vector2(0.4f, 0.26f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, true)
        {
            m_maxTextLength = maxLenght;
            m_caption = caption;
            m_textBoxWidth = textBoxWidth;
            this.callBack = callBack;
            m_defaultName = defaultName;
            RecreateControls(true);
            OnEnterCallback = ReturnOk;
        }

        void Createbuttons()
        {
            Vector2 buttonPosition = new Vector2(-WINDOW_SIZE.X / 4, 0.05f);
            Vector2 buttonOffset = new Vector2(WINDOW_SIZE.X / 2, 0.045f);

            float usableWidth = WINDOW_SIZE.X / 3f;

            var okButton = CreateButton(usableWidth, new StringBuilder("Ok"), OnOk, textScale: 0.9f);
            okButton.Position = buttonPosition;

            var cancelButton = CreateButton(usableWidth, new StringBuilder("Cancel"), OnCancel, textScale: 0.9f);
            cancelButton.Position = buttonPosition + buttonOffset * new Vector2(1f, 0f);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            var caption = AddCaption(m_caption, VRageMath.Color.White.ToVector4());
            m_nameBox = new MyGuiControlTextbox(new Vector2(0f, 0f), maxLength: m_maxTextLength);
            m_nameBox.Text = m_defaultName;
            m_nameBox.Size = new Vector2(m_textBoxWidth, 0.2f);
            Controls.Add(m_nameBox);

            Createbuttons();
        }

        void CallResultCallback(string val)
        {
            if (val != null)
            {
                callBack(val);
            }
        }

        void ReturnOk()
        {
            if (m_nameBox.Text.Length <= 0)
            {
                return;
            }
            else
            {
                CallResultCallback(m_nameBox.Text);
                CloseScreen();
            }
        }

        void OnOk(MyGuiControlButton button)
        {
            ReturnOk();
        }

        void OnCancel(MyGuiControlButton button)
        {
            CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiRenameDialog";
        }
    }
}

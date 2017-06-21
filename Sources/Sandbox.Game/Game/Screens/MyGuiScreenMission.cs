#region Using

using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Gui.RichTextLabel;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Gui;
using VRage.Game.ModAPI;

#endregion

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenMission : MyGuiScreenText
    {
        //public MyGuiControlMultilineText m_description_RTF;

        public MyGuiScreenMission(
            string missionTitle = null,
            string currentObjectivePrefix =null,
            string currentObjective = null,
            string description = null,

            Action<ResultEnum> resultCallback = null, 
            string okButtonCaption = null,
            Vector2? windowSize = null,
            Vector2? descSize = null,
            bool editEnabled = false,
            bool canHideOthers = true,
            bool enableBackgroundFade = false,
            MyMissionScreenStyleEnum style = MyMissionScreenStyleEnum.BLUE)
            : base(missionTitle, currentObjectivePrefix, currentObjective, description, resultCallback, okButtonCaption, windowSize, descSize, editEnabled, canHideOthers, enableBackgroundFade, style)
        {
            
        }

        public override void RecreateControls(bool constructor)
        {
            // BUG FIX: this was creating an empty (m_description = "") second textwall which was on top of the one from baseclass, and didn't allow for the vertical scroll to work
            /*
            m_description_RTF = new MyGuiControlMultilineText(
                position: new Vector2(0.5f, 0.52f),
                size: m_descSize * 0.98f,
                backgroundColor: Vector4.One,
                font: MyFontEnum.White,
                textScale: 1.0f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                contents: new StringBuilder(m_description),//"<h1>header</h1> <b>bold</b> <i>itallic</i>\n2nd line + <p>paragraph</p>"),
                drawScrollbar: true);
            m_description_RTF.BorderEnabled = false;
            m_description_RTF.CanHaveFocus = true;
            //m_description_RTF.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_description_RTF.TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            //this.AddMultilineText(m_descSize * 0.97f, Vector2.Zero, 1f, false, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            m_description = "";*/
            base.RecreateControls(constructor);
        }

        public override bool Draw()
        {
            bool ret = base.Draw();
            //DrawInternal();
            return ret;
        }
        /*
        private void DrawInternal()
        {
            m_description_RTF.Draw(1, 1);
        }
        */
    }
}

using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenTextPanel : MyGuiScreenMission
    {
        public MyGuiScreenTextPanel(
            string missionTitle = null,
            string currentObjectivePrefix = null,
            string currentObjective = null,
            string description = null,
            Action<ResultEnum> resultCallback = null,
            Action saveCodeCallback = null,
            string okButtonCaption = null,
            bool editable = false,
            MyGuiScreenBase previousScreen = null)
            : base(missionTitle: missionTitle,
                currentObjectivePrefix: currentObjectivePrefix,
                currentObjective: currentObjective,
                description: description,
                resultCallback: resultCallback,
                okButtonCaption: okButtonCaption,
                editEnabled: editable)
        {
            CanHideOthers = editable;
        }

        protected override void OnClosed()
        {
            base.OnClosed();

            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }
    }
}

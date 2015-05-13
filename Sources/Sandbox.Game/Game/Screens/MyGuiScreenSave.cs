using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using Sandbox.Common.Localization;
namespace Sandbox.Game.Gui
{
    class MyGuiScreenServerSave : MyGuiScreenBase
    {
        const int m_timeoutInSeconds = 30000;
        MyGuiScreenMessageBox m_currentServerSaveScreen = null;
        public bool CloseWindow { get; set; }
        bool m_wasDrawn = false;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenSave";
        }

        public MyGuiScreenServerSave()
        {
            m_currentServerSaveScreen = new MyGuiScreenMessageBox(
                                timeoutInMiliseconds: m_timeoutInSeconds,
                                styleEnum: MyMessageBoxStyleEnum.BLUE,
                                buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                                messageText: new StringBuilder(MyTextsWrapper.GetString(MyTextsWrapperEnum.SavingPleaseWait)),
                                callback: (result) =>
                                {
                                    CloseWindow = true;
                                });
            m_currentServerSaveScreen.InstantClose = false;
            MyGuiSandbox.AddScreen(m_currentServerSaveScreen);
            CloseWindow = false;
            SkipTransition = true;
        }

        public override bool Draw()
        {
            m_wasDrawn = true;
            return base.Draw();
        }
        public override bool Update(bool hasFocus)
        {
            if (CloseWindow && m_wasDrawn)
            {
                MyGuiSandbox.RemoveScreen(m_currentServerSaveScreen);
                MyGuiSandbox.RemoveScreen(this);
            }
            return true;
        }
    }
}

using ParallelTasks;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using VRage.Utils;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Common;
using VRage;
using System.Diagnostics;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using Sandbox.Game.Screens.Helpers;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenSaveAs : MyGuiScreenBase
    {
        MyGuiControlTextbox m_nameTextbox;
        MyGuiControlButton m_okButton, m_cancelButton;
        MyWorldInfo m_copyFrom;
        List<string> m_existingSessionNames = null;
        string m_sessionPath;

        bool m_fromMainMenu = false;

        public event Action SaveAsConfirm;

        public MyGuiScreenSaveAs(MyWorldInfo copyFrom, string sessionPath, List<string> existingSessionNames)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.5f, 0.35f))
        {
            EnabledBackgroundFade = true;

            AddCaption(MyCommonTexts.ScreenCaptionSaveAs);

            float textboxPositionY = -0.02f;

            m_nameTextbox = new MyGuiControlTextbox(
                position: new Vector2(0, textboxPositionY),
                defaultText: copyFrom.SessionName,
                maxLength: 75);

            m_okButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Cancel),
                onButtonClick: OnCancelButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            Vector2 buttonOrigin = new Vector2(0f, Size.Value.Y * 0.4f);
            Vector2 buttonOffset = new Vector2(0.01f, 0f);

            m_okButton.Position     = buttonOrigin - buttonOffset;
            m_cancelButton.Position = buttonOrigin + buttonOffset;

            Controls.Add(m_nameTextbox);
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            m_nameTextbox.MoveCarriageToEnd();
            m_copyFrom = copyFrom;
            m_sessionPath = sessionPath;
            m_existingSessionNames = existingSessionNames;

            CloseButtonEnabled = true;
            CloseButtonOffset = new Vector2(-0.005f, 0.0035f);
            OnEnterCallback = OnEnterPressed;
        }


        public MyGuiScreenSaveAs(string sessionName)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.5f, 0.35f))
        {
            m_existingSessionNames = null;
            m_fromMainMenu = true;
            EnabledBackgroundFade = true;

            AddCaption(MyCommonTexts.ScreenCaptionSaveAs);

            float textboxPositionY = -0.02f;

            m_nameTextbox = new MyGuiControlTextbox(
                position: new Vector2(0, textboxPositionY),
                defaultText: sessionName,
                maxLength: 75);

            m_okButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Cancel),
                onButtonClick: OnCancelButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            Vector2 buttonOrigin = new Vector2(0f, Size.Value.Y * 0.4f);
            Vector2 buttonOffset = new Vector2(0.01f, 0f);

            m_okButton.Position = buttonOrigin - buttonOffset;
            m_cancelButton.Position = buttonOrigin + buttonOffset;

            Controls.Add(m_nameTextbox);
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            m_nameTextbox.MoveCarriageToEnd();

            CloseButtonEnabled = true;
            CloseButtonOffset = new Vector2(-0.005f, 0.0035f);
            OnEnterCallback = OnEnterPressed;
        }


        private void OnEnterPressed()
        {
            TrySaveAs();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenSaveAs";
        }

        void OnCancelButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        void OnOkButtonClick(MyGuiControlButton sender)
        {
            TrySaveAs();
        }

        private bool TrySaveAs()
        {         
            MyStringId? errorType = null;
            if (m_nameTextbox.Text.Length < 5) errorType = MyCommonTexts.ErrorNameTooShort;
            else if (m_nameTextbox.Text.Length > 128) errorType = MyCommonTexts.ErrorNameTooLong;


            if (m_existingSessionNames != null)
            {
                foreach (var name in m_existingSessionNames)
                {
                    if (name == m_nameTextbox.Text)
                    {
                        errorType = MyCommonTexts.ErrorNameAlreadyExists;
                    }
                }
            }

            if (errorType != null)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(errorType.Value),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return false;
            }

            if (m_fromMainMenu)
            {
                string name = MyUtils.StripInvalidChars(m_nameTextbox.Text);
                if(string.IsNullOrWhiteSpace(name))
                {
                    name = MyLocalCache.GetSessionSavesPath(name + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
                }
                MyAsyncSaving.Start(customName: name);
                MySession.Static.Name = m_nameTextbox.Text;
                this.CloseScreen();
                return true;
            }

            m_copyFrom.SessionName = m_nameTextbox.Text;
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.SavingPleaseWait, null,
                beginAction: () => new SaveResult(MyUtils.StripInvalidChars(m_nameTextbox.Text), m_sessionPath, m_copyFrom),
                endAction: (result, screen) =>
                {
                    screen.CloseScreen();
                    this.CloseScreen();
                    var handler = SaveAsConfirm;
                    if (handler != null)
                        handler();
                }));
            return true;
        }

        #region Async Saving

        class SaveResult : IMyAsyncResult
        {
            public bool IsCompleted { get { return this.Task.IsComplete; } }
            public Task Task
            {
                get;
                private set;
            }

            public SaveResult(string saveDir, string sessionPath, MyWorldInfo copyFrom)
            {
                Task = Parallel.Start(() => SaveAsync(saveDir, sessionPath, copyFrom));
            }

            void SaveAsync(string newSaveName, string sessionPath, MyWorldInfo copyFrom)
            {
                // Try a simple path, then a random if it already exists
                var newSessionPath = MyLocalCache.GetSessionSavesPath(newSaveName, false, false);
                while (Directory.Exists(newSessionPath))
                {
                    newSessionPath = MyLocalCache.GetSessionSavesPath(newSaveName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
                }
                Directory.CreateDirectory(newSessionPath);
                MyUtils.CopyDirectory(sessionPath, newSessionPath);
                ulong sizeInBytes;
                var checkpoint = MyLocalCache.LoadCheckpoint(newSessionPath, out sizeInBytes);
                Debug.Assert(checkpoint != null);
                checkpoint.SessionName = copyFrom.SessionName;
                checkpoint.WorkshopId = null;
                MyLocalCache.SaveCheckpoint(checkpoint, newSessionPath);
                MyLocalCache.SaveLastLoadedTime(newSessionPath, DateTime.Now);
            }
        }

        #endregion
    }
}

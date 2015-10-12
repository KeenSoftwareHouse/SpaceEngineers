using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Graphics.GUI;
using ParallelTasks;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    public interface IMyAsyncResult
    {
        bool IsCompleted { get; }
        Task Task { get; }
    }

    public class MyGuiScreenProgressAsync: MyGuiScreenProgressBase
    {
        public string FriendlyName { get; set; }

        private Func<IMyAsyncResult> m_beginAction;
        private Action<IMyAsyncResult, MyGuiScreenProgressAsync> m_endAction;
        private IMyAsyncResult m_asyncResult;

        public MyGuiScreenProgressAsync(MyStringId text, MyStringId? cancelText, Func<IMyAsyncResult> beginAction, Action<IMyAsyncResult, MyGuiScreenProgressAsync> endAction)
            : base(text, cancelText)
        {
            FriendlyName = "MyGuiScreenProgressAsync";
            m_beginAction = beginAction;
            m_endAction = endAction;
        }

        public StringBuilder Text
        {
            get { return m_progressTextLabel.TextToDraw; }
            set { m_progressTextLabel.TextToDraw = value; }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            m_rotatingWheel.MultipleSpinningWheels = MyPerGameSettings.GUI.MultipleSpinningWheels;
        }

        protected override void ProgressStart()
        {
            m_asyncResult = m_beginAction();
        }

        public override string GetFriendlyName()
        {
            return FriendlyName;
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            //  Only continue if this screen is really open (not closing or closed)
            if (State != MyGuiScreenState.OPENED) return false;

            if (m_asyncResult.IsCompleted)
            {
                m_endAction(m_asyncResult, this);
            }

            if (m_asyncResult != null && m_asyncResult.Task.Exceptions != null)
            {
                foreach (var e in m_asyncResult.Task.Exceptions)
                {
                    MySandboxGame.Log.WriteLine(e);
                }
            }

            return true;
        }
    }
}

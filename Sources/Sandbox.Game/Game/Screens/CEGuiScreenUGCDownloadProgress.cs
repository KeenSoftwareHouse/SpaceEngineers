using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Graphics.GUI;
using ParallelTasks;
using VRage.Utils;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Game.Localization;
using VRage;

namespace Sandbox.Game.Gui
{
    public enum DownloadUnits
    {
        Discrete,
        Bytes
    }
    /// <summary>
    /// SECE Steam Workshop Mod Download GUI with progress.
    /// </summary>
    public class CEGuiScreenUGCDownloadProgress : MyGuiScreenProgressBase
    {
        public string FriendlyName { get; set; }
        private VRage.Utils.MyStringId CurrentTextEnum;
        private DownloadUnits Units = DownloadUnits.Discrete;

        private Func<Action<ulong, ulong>, IMyAsyncResult> m_beginAction;
        private Action<IMyAsyncResult, CEGuiScreenUGCDownloadProgress> m_endAction;
        private IMyAsyncResult m_asyncResult;

        public void SetText(MyStringId text, DownloadUnits units)
        {
            CurrentTextEnum = text;
            Units = units;
        }

        public void SetText(MyStringId myStringId, DownloadUnits downloadUnits, ulong progress, ulong total)
        {
            SetText(myStringId, downloadUnits);
            BytesDLed = progress;
            BytesToDL = total;
            UpdateText();
        }

        public string Format(ulong rawNumber)
        {
            var sb = new StringBuilder();
            switch (Units)
            {
                case DownloadUnits.Bytes:
                    MyValueFormatter.AppendDataSizeInBestUnit((long)rawNumber, sb);
                    break;
                case DownloadUnits.Discrete:
                    sb.Append(MyValueFormatter.GetFormatedLong((long)rawNumber));
                    break;
            }
            return sb.ToString();
        }

        public CEGuiScreenUGCDownloadProgress(MyStringId text, MyStringId? cancelText, Func<Action<ulong, ulong>, IMyAsyncResult> beginAction, Action<IMyAsyncResult, CEGuiScreenUGCDownloadProgress> endAction)
            : base(text, cancelText)
        {
            CurrentTextEnum = text;
            FriendlyName = "CEGuiScreenUGCDownloadProgress";
            m_beginAction = beginAction;
            m_endAction = endAction;
        }

        public StringBuilder Text
        {
            get { return m_progressTextLabel.TextToDraw; }
            set { m_progressTextLabel.TextToDraw = value; }
        }

        public ulong BytesToDL = 0;
        public ulong BytesDLed = 0;


        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            m_rotatingWheel.MultipleSpinningWheels = MyPerGameSettings.GUI.MultipleSpinningWheels;
        }

        protected override void ProgressStart()
        {
            m_asyncResult = m_beginAction(progressHandler);
        }

        private void progressHandler(ulong downloaded, ulong total)
        {
            if (total == 18 && Units == DownloadUnits.Bytes)
                throw new InvalidOperationException("Bytes should only be called by ProgressTextDownloadingModsFormatted.");
            BytesToDL = total;
            BytesDLed = downloaded;
            UpdateText();
            MyLog.Default.WriteLineAndConsole(string.Format("DL progress: {0}/{1}", BytesDLed, BytesToDL));
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

            UpdateText();

            return true;
        }

        private void UpdateText()
        {
            m_progressTextLabel.TextEnum = CurrentTextEnum;
            m_progressTextLabel.UpdateFormatParams(Format(BytesDLed), Format(BytesToDL));
        }
    }
}

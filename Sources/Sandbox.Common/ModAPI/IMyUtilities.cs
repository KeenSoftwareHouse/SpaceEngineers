using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public delegate void MessageEnteredDel(string messageText, ref bool sendToOthers);
    public enum ResultEnum
    {     
        OK,
        CANCEL,     
    }

    public interface IMyUtilities
    {
        IMyConfigDedicated ConfigDedicated { get; }
        string GetTypeName(Type type);
        void ShowNotification(string message, int disappearTimeMs = 2000, Common.MyFontEnum font = Common.MyFontEnum.White);

        /// <summary>
        /// Create a notification object.
        /// The object needs to have Show() called on it to be shown.
        /// On top of that you can dynamically change the text, font and adjust the time to live.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="disappearTimeMs"></param>
        /// <param name="font"></param>
        /// <returns>The notification object.</returns>
        IMyHudNotification CreateNotification(string message, int disappearTimeMs = 2000, Common.MyFontEnum font = Common.MyFontEnum.White);

        void ShowMessage(string sender, string messageText);
        void SendMessage(string messageText);
        event MessageEnteredDel MessageEntered;

        bool FileExistsInGlobalStorage(string file);
        bool FileExistsInLocalStorage(string file, Type callingType);

        void DeleteFileInLocalStorage(string file, Type callingType);
        void DeleteFileInGlobalStorage(string file);

        System.IO.TextReader ReadFileInGlobalStorage(string file);
        System.IO.TextReader ReadFileInLocalStorage(string file, Type callingType);
        System.IO.TextWriter WriteFileInGlobalStorage(string file);
        System.IO.TextWriter WriteFileInLocalStorage(string file, Type callingType);
        IMyGamePaths GamePaths { get; }
        bool IsDedicated { get; }
        string SerializeToXML<T>(T objToSerialize);
        T SerializeFromXML<T>(string buffer);
        void InvokeOnGameThread(Action action);
        void ShowMissionScreen(string screenTitle = null, string currentObjectivePrefix = null, string currentObjective = null, string screenDescription = null, Action<ResultEnum> callback = null, string okButtonCaption = null);
        IMyHudObjectiveLine GetObjectiveLine();

        System.IO.BinaryReader ReadBinaryFileInGlobalStorage(string file);
        System.IO.BinaryReader ReadBinaryFileInLocalStorage(string file, Type callingType);
        System.IO.BinaryWriter WriteBinaryFileInGlobalStorage(string file);
        System.IO.BinaryWriter WriteBinaryFileInLocalStorage(string file, Type callingType);

        void SetVariable<T>(string name, T value);
        bool GetVariable<T>(string name, out T value);

    }
}

using System;

namespace VRage.Game.ModAPI
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
        void ShowNotification(string message, int disappearTimeMs = 2000, string font = MyFontEnum.White);

        /// <summary>
        /// Create a notification object.
        /// The object needs to have Show() called on it to be shown.
        /// On top of that you can dynamically change the text, font and adjust the time to live.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="disappearTimeMs"></param>
        /// <param name="font"></param>
        /// <returns>The notification object.</returns>
        IMyHudNotification CreateNotification(string message, int disappearTimeMs = 2000, string font = MyFontEnum.White);

        void ShowMessage(string sender, string messageText);
        void SendMessage(string messageText);
        event MessageEnteredDel MessageEntered;

        bool FileExistsInGlobalStorage(string file);
#if !XB1
        bool FileExistsInLocalStorage(string file, Type callingType);
        bool FileExistsInWorldStorage(string file, Type callingType);
#endif // !XB1

        void DeleteFileInGlobalStorage(string file);
#if !XB1
        void DeleteFileInLocalStorage(string file, Type callingType);
        void DeleteFileInWorldStorage(string file, Type callingType);
#endif // !XB1

        System.IO.TextReader ReadFileInGlobalStorage(string file);
#if !XB1
        System.IO.TextReader ReadFileInLocalStorage(string file, Type callingType);
        /// <summary>
        /// Read text file from the current world's Storage directory.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        /// <remarks>This directory is under Saves\&lt;SteamId&gt;\&lt;WorldName&gt;\Storage</remarks>
        System.IO.TextReader ReadFileInWorldStorage(string file, Type callingType);
#endif // !XB1
        System.IO.TextWriter WriteFileInGlobalStorage(string file);
#if !XB1
        System.IO.TextWriter WriteFileInLocalStorage(string file, Type callingType);
        /// <summary>
        /// Write text file to the current world's Storage directory.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        System.IO.TextWriter WriteFileInWorldStorage(string file, Type callingType);
#endif // !XB1

        IMyGamePaths GamePaths { get; }
        bool IsDedicated { get; }
        string SerializeToXML<T>(T objToSerialize);
        T SerializeFromXML<T>(string buffer);
        void InvokeOnGameThread(Action action);
        void ShowMissionScreen(string screenTitle = null, string currentObjectivePrefix = null, string currentObjective = null, string screenDescription = null, Action<ResultEnum> callback = null, string okButtonCaption = null);
        IMyHudObjectiveLine GetObjectiveLine();

        System.IO.BinaryReader ReadBinaryFileInGlobalStorage(string file);
#if !XB1
        System.IO.BinaryReader ReadBinaryFileInLocalStorage(string file, Type callingType);
        /// <summary>
        /// Read file from the current world's Storage directory.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        System.IO.BinaryReader ReadBinaryFileInWorldStorage(string file, Type callingType);
#endif // !XB1
        System.IO.BinaryWriter WriteBinaryFileInGlobalStorage(string file);
#if !XB1
        System.IO.BinaryWriter WriteBinaryFileInLocalStorage(string file, Type callingType);
        /// <summary>
        /// Write file to the current world's Storage directory.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="callingType"></param>
        /// <returns></returns>
        System.IO.BinaryWriter WriteBinaryFileInWorldStorage(string file, Type callingType);
#endif // !XB1

        void SetVariable<T>(string name, T value);
        bool GetVariable<T>(string name, out T value);
        bool RemoveVariable(string name);

    }
}

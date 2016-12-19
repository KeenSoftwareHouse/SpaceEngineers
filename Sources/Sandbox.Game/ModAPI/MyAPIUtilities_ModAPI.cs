
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using VRage.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.Game.Screens;
using VRage.Game;
using VRage.Game.ModAPI;


namespace Sandbox.ModAPI
{
    public class MyAPIUtilities : IMyUtilities, IMyGamePaths
    {
        private const string STORAGE_FOLDER = "Storage";
        public static readonly MyAPIUtilities Static;

        public event MessageEnteredDel MessageEntered;

        static MyAPIUtilities()
        {
            Static = new MyAPIUtilities();
        }

        string IMyUtilities.GetTypeName(Type type)
        {
            return type.Name;
        }

        void IMyUtilities.ShowNotification(string message, int disappearTimeMs, string font)
        {
            var not = new MyHudNotification(MyCommonTexts.CustomText, disappearTimeMs, font);
            not.SetTextFormatArguments( message);
            MyHud.Notifications.Add(not);
        }

        IMyHudNotification IMyUtilities.CreateNotification(string message, int disappearTimeMs, string font)
        {
            var notification = new MyHudNotification(MyCommonTexts.CustomText, disappearTimeMs, font);
            notification.SetTextFormatArguments(message);
            return notification as IMyHudNotification;
        }

        void IMyUtilities.ShowMessage(string sender, string messageText)
        {
            MyHud.Chat.ShowMessage(sender, messageText);
        }

        void IMyUtilities.SendMessage(string messageText)
        {
            if(MyMultiplayer.Static != null)
                MyMultiplayer.Static.SendChatMessage(messageText);
        }

        public void EnterMessage(string messageText, ref bool sendToOthers)
        {
            var handle = MessageEntered;
            if (handle != null) handle(messageText, ref sendToOthers);
        }

        private string StripDllExtIfNecessary(string name)
        {
            string ext = ".dll";
            if( name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                return name.Substring(0, name.Length - ext.Length);
            }
            return name;
        }

        System.IO.TextReader IMyUtilities.ReadFileInGlobalStorage(string file)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new StreamReader(stream);
            }
            throw new FileNotFoundException();
        }

#if !XB1
        System.IO.TextReader IMyUtilities.ReadFileInLocalStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new StreamReader(stream);
            }
            throw new FileNotFoundException();
        }

        System.IO.TextReader IMyUtilities.ReadFileInWorldStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new StreamReader(stream);
            }
            throw new FileNotFoundException();
        }
#endif // !XB1

        TextWriter IMyUtilities.WriteFileInGlobalStorage(string file)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new StreamWriter(stream);
            }
            throw new FileNotFoundException();
        }

#if !XB1
        TextWriter IMyUtilities.WriteFileInLocalStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }

            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new StreamWriter(stream);
            }
            throw new FileNotFoundException();
        }

        TextWriter IMyUtilities.WriteFileInWorldStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }

            var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new StreamWriter(stream);
            }
            throw new FileNotFoundException();
        }
#endif // !XB1

        event MessageEnteredDel IMyUtilities.MessageEntered
        {
            add { MessageEntered += value; }
            remove { MessageEntered -= value; }
        }

        IMyConfigDedicated IMyUtilities.ConfigDedicated
        {
            get { return MySandboxGame.ConfigDedicated; }
        }

        string IMyGamePaths.ContentPath
        {
            get { return MyFileSystem.ContentPath; }
        }

        string IMyGamePaths.ModsPath
        {
            get { return MyFileSystem.ModsPath; }
        }

        string IMyGamePaths.UserDataPath
        {
            get { return MyFileSystem.UserDataPath; }
        }

        string IMyGamePaths.SavesPath
        {
            get { return MyFileSystem.SavesPath; }
        }


        IMyGamePaths IMyUtilities.GamePaths
        {
            get { return this; }
        }

        bool IMyUtilities.IsDedicated 
        {
            get { return MySandboxGame.IsDedicated; }
        }

        string IMyUtilities.SerializeToXML<T>(T objToSerialize)
        {      
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(objToSerialize.GetType());
            StringWriter textWriter = new StringWriter();
            x.Serialize(textWriter, objToSerialize);
            return textWriter.ToString();
        }

        T IMyUtilities.SerializeFromXML<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader textReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(textReader))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }

        }

        void IMyUtilities.InvokeOnGameThread(Action action)
        {
            if (MySandboxGame.Static != null)
            {
                MySandboxGame.Static.Invoke(action);
            }
        }

        bool IMyUtilities.FileExistsInGlobalStorage(string file)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return false;
            }

            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, file);
            return File.Exists(path);
        }

#if !XB1
        bool IMyUtilities.FileExistsInLocalStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return false;
            }

            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);

            return File.Exists(path);
        }

        bool IMyUtilities.FileExistsInWorldStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return false;
            }

            var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);

            return File.Exists(path);
        }

        void IMyUtilities.DeleteFileInLocalStorage(string file, Type callingType)
        {
            if (true == (this as IMyUtilities).FileExistsInLocalStorage(file, callingType))
            {
                var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
                File.Delete(path);
            }
        }

        void IMyUtilities.DeleteFileInWorldStorage(string file, Type callingType)
        {
            if (true == (this as IMyUtilities).FileExistsInLocalStorage(file, callingType))
            {
                var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
                File.Delete(path);
            }
        }
#endif // !XB1

        void IMyUtilities.DeleteFileInGlobalStorage(string file)
        {
            if (true == (this as IMyUtilities).FileExistsInGlobalStorage(file))
            {
                var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER,file);
                File.Delete(path);
            }
        }

        void IMyUtilities.ShowMissionScreen(string screenTitle, string currentObjectivePrefix, string currentObjective, string screenDescription, Action<ResultEnum> callback = null, string okButtonCaption = null)
        {
            var missionScreen = new MyGuiScreenMission(missionTitle: screenTitle, 
                currentObjectivePrefix: currentObjectivePrefix, 
                currentObjective: currentObjective, 
                description:screenDescription,
                resultCallback: callback,
                okButtonCaption: okButtonCaption);
            
            MyScreenManager.AddScreen(missionScreen);
        }


        IMyHudObjectiveLine IMyUtilities.GetObjectiveLine()
        {
            return MyHud.ObjectiveLine;
        }


        BinaryReader IMyUtilities.ReadBinaryFileInGlobalStorage(string file)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new BinaryReader(stream);
            }
            throw new FileNotFoundException();
        }

#if !XB1
        BinaryReader IMyUtilities.ReadBinaryFileInLocalStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new BinaryReader(stream);
            }
            throw new FileNotFoundException();
        }

        BinaryReader IMyUtilities.ReadBinaryFileInWorldStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenRead(path);
            if (stream != null)
            {
                return new BinaryReader(stream);
            }
            throw new FileNotFoundException();
        }
#endif // !XB1

        BinaryWriter IMyUtilities.WriteBinaryFileInGlobalStorage(string file)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new BinaryWriter(stream);
            }
            throw new FileNotFoundException();
        }

#if !XB1
        BinaryWriter IMyUtilities.WriteBinaryFileInLocalStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MyFileSystem.UserDataPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new BinaryWriter(stream);
            }
            throw new FileNotFoundException();
        }

        BinaryWriter IMyUtilities.WriteBinaryFileInWorldStorage(string file, Type callingType)
        {
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new FileNotFoundException();
            }
            var path = Path.Combine(MySession.Static.CurrentPath, STORAGE_FOLDER, StripDllExtIfNecessary(callingType.Assembly.ManifestModule.ScopeName), file);
            var stream = MyFileSystem.OpenWrite(path);
            if (stream != null)
            {
                return new BinaryWriter(stream);
            }
            throw new FileNotFoundException();
        }
#endif // !XB1

        public Dictionary<string, object> Variables = new Dictionary<string, object>();
        void IMyUtilities.SetVariable<T>(string name, T value)
        {
            Variables.Remove(name);
            Variables.Add(name, value);
        }
        bool IMyUtilities.GetVariable<T>(string name, out T value)
        {
            object item;
            value = default(T);
            if (Variables.TryGetValue(name, out item))
                if (item is T)
                {
                    value = (T)item;
                    return true;
                }
            return false;
        }

        bool IMyUtilities.RemoveVariable(string name)
        {
            return Variables.Remove(name);
        }
    }
}

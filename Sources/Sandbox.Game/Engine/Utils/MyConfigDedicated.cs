using System.IO;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders;
using System;
using Sandbox.Definitions;
using VRage.Serialization;
using System.Collections.Generic;
using VRage.Utils;
using System.Runtime.CompilerServices;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ModAPI;


namespace Sandbox.Engine.Utils
{
    public class MyConfigDedicated<T> : IMyConfigDedicated where T : MyObjectBuilder_SessionSettings, new()
    {
        XmlSerializer m_serializer;
        string m_fileName;

        MyConfigDedicatedData<T> m_data;

        public MyConfigDedicated(string fileName) 
        {
            m_fileName = fileName;

            try { m_serializer = new XmlSerializer(typeof(MyConfigDedicatedData<T>)); }
            catch (Exception) { }

            SetDefault();
        }

        void SetDefault()
        {
            m_data = new MyConfigDedicatedData<T>();
        }

        public T SessionSettings
        {
            get { return m_data.SessionSettings; }
            set { m_data.SessionSettings = value; }
        }

        public string LoadWorld
        {
            get { return m_data.LoadWorld; }
            set { m_data.LoadWorld = value; }
        }

        public string IP
        {
            get { return m_data.IP; }
            set { m_data.IP = value; }
        }

        public int SteamPort
        {
            get { return m_data.SteamPort; }
            set { m_data.SteamPort = value; }
        }
        
        public int ServerPort
        {
            get { return m_data.ServerPort; }
            set { m_data.ServerPort = value; }
        }

        public int AsteroidAmount
        {
            get { return m_data.AsteroidAmount; }
            set { m_data.AsteroidAmount = value; }
        }

        public ulong GroupID
        {
            get { return m_data.GroupID; }
            set { m_data.GroupID = value; }
        }

        public List<string> Administrators
        {
            get { return m_data.Administrators; }
            set { m_data.Administrators = value; }
        }

        public List<ulong> Banned
        {
            get { return m_data.Banned; }
            set { m_data.Banned = value; }
        }

        public List<ulong> Mods
        {
            get { return m_data.Mods; }
            set { m_data.Mods = value; }
        }

        public string ServerName
        {
            get { return m_data.ServerName; }
            set { m_data.ServerName = value; }
        }

        public string WorldName
        {
            get { return m_data.WorldName; }
            set { m_data.WorldName = value; }
        }

        public string PremadeCheckpointPath
        {
            get { return m_data.PremadeCheckpointPath; }
            set { m_data.PremadeCheckpointPath = value; }
        }

        public bool PauseGameWhenEmpty
        {
            get { return m_data.PauseGameWhenEmpty; }
            set { m_data.PauseGameWhenEmpty = value; }
        }

        public bool IgnoreLastSession
        {
            get { return m_data.IgnoreLastSession; }
            set { m_data.IgnoreLastSession = value; }
        }

        public void Load(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = GetFilePath();

            if (!File.Exists(path))
            {
                SetDefault();
                return;
            }

            try
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    m_data = (MyConfigDedicatedData<T>)m_serializer.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                if (MyLog.Default != null)
                    MyLog.Default.WriteLine("Exception during DS config load: " + e.ToString());
                SetDefault();
                return;
            }
        }

        public void Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                path = GetFilePath();

            using (FileStream fs = File.Create(path))
            {
                m_serializer.Serialize(fs, m_data);
            }
        }

        public string GetFilePath()
        {
            return Path.Combine(MyFileSystem.UserDataPath, m_fileName);
        }


        List<string> IMyConfigDedicated.Administrators
        {
            get
            {
                return Administrators;
            }
            set
            {
                Administrators = value;
            }
        }

        int IMyConfigDedicated.AsteroidAmount
        {
            get
            {
                return AsteroidAmount;
            }
            set
            {
                AsteroidAmount = value;
            }
        }

        List<ulong> IMyConfigDedicated.Banned
        {
            get
            {
                return Banned;
            }
            set
            {
                Banned = value;
            }
        }

        string IMyConfigDedicated.GetFilePath()
        {
            return GetFilePath();
        }

        ulong IMyConfigDedicated.GroupID
        {
            get
            {
                return GroupID;
            }
            set
            {
                GroupID = value;
            }
        }

        string IMyConfigDedicated.LoadWorld
        {
            get
            {
                return LoadWorld;
            }
        }

        List<ulong> IMyConfigDedicated.Mods
        {
            get
            {
                return Mods;
            }
        }

        bool IMyConfigDedicated.PauseGameWhenEmpty
        {
            get
            {
                return PauseGameWhenEmpty;
            }
            set
            {
                PauseGameWhenEmpty = value;
            }
        }

        void IMyConfigDedicated.Save(string path = null)
        {
            Save(path);
        }

        string IMyConfigDedicated.ServerName
        {
            get
            {
                return ServerName;
            }
            set
            {
                ServerName = value;
            }
        }

        MyObjectBuilder_SessionSettings IMyConfigDedicated.SessionSettings
        {
            get
            {
                return SessionSettings;
            }
            set
            {
                SessionSettings = (T)value;
            }
        }

        string IMyConfigDedicated.WorldName
        {
            get
            {
                return WorldName;
            }
            set
            {
                WorldName = value;
            }
        }
    }
}
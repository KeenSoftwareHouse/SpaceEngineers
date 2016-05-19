using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;

namespace VRage.Game
{
    public class MyModContext : IEquatable<MyModContext>
    {
        private static MyModContext m_baseContext = null;
        public static MyModContext BaseGame
        {
            get
            {
                if (m_baseContext == null) InitBaseModContext();
                return m_baseContext;
            }
        }

        private static MyModContext m_unknownContext = null;
        public static MyModContext UnknownContext
        {
            get
            {
                if (m_unknownContext == null) InitUnknownModContext();
                return m_unknownContext;
            }
        }

        [XmlIgnore]
        public string ModName { get; private set; }

        [XmlIgnore]
        public string ModId { get; private set; }

        [XmlIgnore]
        public string ModPath { get; private set; }
        
        [XmlIgnore]
        public string ModPathData { get; private set; }

        public string CurrentFile;
        
        public void Init(MyObjectBuilder_Checkpoint.ModItem modItem)
        {
            ModName = modItem.FriendlyName;
            ModId = modItem.Name;
            ModPath = Path.Combine(MyFileSystem.ModsPath, modItem.Name);
            ModPathData = Path.Combine(ModPath, "Data");
        }

        public void Init(MyModContext context)
        {
            ModName = context.ModName;
            ModPath = context.ModPath;
            ModId = context.ModId;
            ModPathData = context.ModPathData;
            CurrentFile = context.CurrentFile;
        }

        // Use this constructon only as a last resort. Proper initialization should be from MyObjectBuilder_Checkpoint.ModItem
        public void Init(string modName, string fileName, string modPath = null)
        {
            ModName = modName;
            ModPath = modPath;
            ModPathData = modPath != null ? Path.Combine(modPath, "Data") : null;
            CurrentFile = fileName;
        }

        public bool IsBaseGame
        {
            get {
                return m_baseContext != null
                    && ModName == m_baseContext.ModName
                    && ModPath == m_baseContext.ModPath
                    && ModPathData == m_baseContext.ModPathData;
            }
        }

        private static void InitBaseModContext()
        {
            m_baseContext = new MyModContext();
            m_baseContext.ModName = null;
            m_baseContext.ModPath = MyFileSystem.ContentPath;
            m_baseContext.ModPathData = Path.Combine(m_baseContext.ModPath, "Data");
        }

        private static void InitUnknownModContext()
        {
            m_unknownContext = new MyModContext();
            m_unknownContext.ModName = "Unknown MOD";
            m_unknownContext.ModPath = null;
            m_unknownContext.ModPathData = null;
        }

        public bool Equals(MyModContext other)
        {
            return ModName == other.ModName && ModPath == other.ModPath;
        }

        public override int GetHashCode()
        {
            return ModPath.GetHashCode() ^ (ModName != null ? ModName.GetHashCode() : 0);
        }
    }
}

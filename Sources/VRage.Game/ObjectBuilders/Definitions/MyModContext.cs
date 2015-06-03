using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    public class MyModContext
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

        [XmlIgnore]
        public string ModName { get; private set; }
        [XmlIgnore]
        public string ModPath { get; private set; }
        [XmlIgnore]
        public string ModPathData { get; private set; }

        public string CurrentFile;
        
        public void Init(MyObjectBuilder_Checkpoint.ModItem modItem)
        {
            ModName = modItem.FriendlyName;
            ModPath = Path.Combine(MyFileSystem.ModsPath, modItem.Name);
            ModPathData = Path.Combine(ModPath, "Data");
        }

        public void Init(MyModContext context)
        {
            ModName = context.ModName;
            ModPath = context.ModPath;
            ModPathData = context.ModPathData;
            CurrentFile = context.CurrentFile;
        }

        // Use this constructon only as a last resort. Proper initialization should be from MyObjectBuilder_Checkpoint.ModItem
        public void Init(string modName, string fileName)
        {
            ModName = modName;
            ModPath = null;
            ModPathData = null;
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
    }
}

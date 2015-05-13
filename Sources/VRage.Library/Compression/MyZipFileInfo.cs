using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VRage.Compression
{
    public struct MyZipFileInfo
    {
        internal object m_fileInfo;

        internal MyZipFileInfo(object fileInfo)
        {
            m_fileInfo = fileInfo;
        }

        public bool IsValid
        {
            get { return m_fileInfo != null; } 
        }

        public CompressionMethodEnum CompressionMethod
        {
            get { return (CompressionMethodEnum)MyZipFileInfoReflection.CompressionMethod(m_fileInfo); }
        }

        public DeflateOptionEnum DeflateOption
        {
            get { return (DeflateOptionEnum)MyZipFileInfoReflection.DeflateOption(m_fileInfo); }
        }

        public bool FolderFlag
        {
            get { return MyZipFileInfoReflection.FolderFlag(m_fileInfo); }
        }

        public DateTime LastModFileDateTime
        {
            get { return MyZipFileInfoReflection.LastModFileDateTime(m_fileInfo); }
        }

        public string Name
        {
            get { return MyZipFileInfoReflection.Name(m_fileInfo); }
        }

        public bool VolumeLabelFlag
        {
            get { return MyZipFileInfoReflection.VolumeLabelFlag(m_fileInfo); }
        }

        public Stream GetStream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return MyZipFileInfoReflection.GetStream(m_fileInfo, mode, access);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

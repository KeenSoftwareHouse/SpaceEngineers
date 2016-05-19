using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;
using VRage.Reflection;

namespace VRage.Compression
{
#if XB1
	public class MyZipArchiveReflection
	{
		public static readonly BindingFlags StaticBind = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		public static readonly BindingFlags InstanceBind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		public static readonly Assembly ZipAssembly = null; //typeof(System.IO.Packaging.Package).Assembly;
		public static readonly Type ZipArchiveType = ZipAssembly.GetType("MS.Internal.IO.Zip.ZipArchive");
		public static readonly Type CompressionMethodType = ZipAssembly.GetType("MS.Internal.IO.Zip.CompressionMethodEnum");
		public static readonly Type DeflateOptionType = ZipAssembly.GetType("MS.Internal.IO.Zip.DeflateOptionEnum");

		public static readonly MethodInfo OpenOnFileMethod = ZipArchiveType.GetMethod("OpenOnFile", StaticBind);
		public static readonly MethodInfo OpenOnStreamMethod = ZipArchiveType.GetMethod("OpenOnStream", StaticBind);
		public static readonly MethodInfo GetFilesMethod = ZipArchiveType.GetMethod("GetFiles", InstanceBind);
		public static readonly MethodInfo GetFileMethod = ZipArchiveType.GetMethod("GetFile", InstanceBind);
		public static readonly MethodInfo FileExistsMethod = ZipArchiveType.GetMethod("FileExists", InstanceBind);
		public static readonly MethodInfo AddFileMethod = ZipArchiveType.GetMethod("AddFile", InstanceBind);
		public static readonly MethodInfo DeleteFileMethod = ZipArchiveType.GetMethod("DeleteFile", InstanceBind);

		public static object OpenOnFile(string s, FileMode fm, FileAccess fa, FileShare fs, bool b)
		{
			return OpenOnFileMethod.Invoke(null,new object[]{s,fm,false,fs,b});
		}

		public static object AddFile( object o , string s, ushort us, byte b)
		{
			return AddFileMethod.Invoke(null, new object[] { o, s, us, b });
		}
		public static object OpenOnStream(Stream st, FileMode fm, FileAccess fa, bool b)
		{
			return OpenOnStreamMethod.Invoke(null, new object[]{st,fm,fa,b});
		}
		public static object GetFiles(object o)
		{
			return GetFilesMethod.Invoke(null, new object[]{o});
		}
		public static object GetFile(object o , string s)
		{
			return GetFileMethod.Invoke(null, new object[]{o,s});
		}
		public static bool FileExists(object o, string s)
		{
			return FileExistsMethod.Invoke(null, new object[] { o, s }) != null;
		}
		public static void DeleteFile(object o, string s)
		{
			DeleteFileMethod.Invoke(null, new object[]{o,s});
		}
	}

	public class MyZipFileInfoReflection
	{
		public static readonly BindingFlags Bind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		public static readonly Type ZipFileInfoType = MyZipArchiveReflection.ZipAssembly.GetType("MS.Internal.IO.Zip.ZipFileInfo");

		public static readonly PropertyInfo CompressionMethodProperty = ZipFileInfoType.GetProperty("CompressionMethod", Bind);
		public static readonly PropertyInfo DeflateOptionProperty = ZipFileInfoType.GetProperty("DeflateOption", Bind);
		public static readonly PropertyInfo FolderFlagProperty = ZipFileInfoType.GetProperty("FolderFlag", Bind);
		public static readonly PropertyInfo LastModFileDateTimeProperty = ZipFileInfoType.GetProperty("LastModFileDateTime", Bind);
		public static readonly PropertyInfo NameProperty = ZipFileInfoType.GetProperty("Name", Bind);
		public static readonly PropertyInfo VolumeLabelFlagProperty = ZipFileInfoType.GetProperty("VolumeLabelFlag", Bind);

		public static readonly MethodInfo GetStreamMethod = ZipFileInfoType.GetMethod("GetStream", Bind);

		public static ushort CompressionMethod(object o)
		{ 
			return (ushort)CompressionMethodProperty.GetValue(o,null);
		}
		public static byte DeflateOption(object o)
		{ 
			return (byte)DeflateOptionProperty.GetValue(o,null);
		}
		public static bool FolderFlag(object o)
		{ 
			return (bool)FolderFlagProperty.GetValue(o,null);
		}
		public static DateTime LastModFileDateTime(object o)
		{ 
			return (DateTime)LastModFileDateTimeProperty.GetValue(o,null);
		}
		public static string Name(object o)
		{ 
			return (string)NameProperty.GetValue(o,null);
		}
		public static bool VolumeLabelFlag(object o)
		{ 
			return (bool)VolumeLabelFlagProperty.GetValue(o,null);
		}
		public static Stream GetStream(object o, FileMode fm , FileAccess fa)
		{
			return (Stream)GetStreamMethod.Invoke(null, new object[]{o,fm,fa});
		}
	}
#else
    public class MyZipArchiveReflection
    {
        public static readonly BindingFlags StaticBind = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static readonly BindingFlags InstanceBind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static readonly Assembly ZipAssembly = typeof(System.IO.Packaging.Package).Assembly;
        public static readonly Type ZipArchiveType = ZipAssembly.GetType("MS.Internal.IO.Zip.ZipArchive");
        public static readonly Type CompressionMethodType = ZipAssembly.GetType("MS.Internal.IO.Zip.CompressionMethodEnum");
        public static readonly Type DeflateOptionType = ZipAssembly.GetType("MS.Internal.IO.Zip.DeflateOptionEnum");

        public static readonly MethodInfo OpenOnFileMethod = ZipArchiveType.GetMethod("OpenOnFile", StaticBind);
        public static readonly MethodInfo OpenOnStreamMethod = ZipArchiveType.GetMethod("OpenOnStream", StaticBind);
        public static readonly MethodInfo GetFilesMethod = ZipArchiveType.GetMethod("GetFiles", InstanceBind);
        public static readonly MethodInfo GetFileMethod = ZipArchiveType.GetMethod("GetFile", InstanceBind);
        public static readonly MethodInfo FileExistsMethod = ZipArchiveType.GetMethod("FileExists", InstanceBind);
        public static readonly MethodInfo AddFileMethod = ZipArchiveType.GetMethod("AddFile", InstanceBind);
        public static readonly MethodInfo DeleteFileMethod = ZipArchiveType.GetMethod("DeleteFile", InstanceBind);

        public static readonly Func<string, FileMode, FileAccess, FileShare, bool, object> OpenOnFile = OpenOnFileMethod.StaticCall<Func<string, FileMode, FileAccess, FileShare, bool, object>>();
        public static readonly Func<Stream, FileMode, FileAccess, bool, object> OpenOnStream = OpenOnStreamMethod.StaticCall<Func<Stream, FileMode, FileAccess, bool, object>>();
        public static readonly Func<object, object> GetFiles = GetFilesMethod.InstanceCall<Func<object, object>>();
        public static readonly Func<object, string, object> GetFile = GetFileMethod.InstanceCall<Func<object, string, object>>();
        public static readonly Func<object, string, bool> FileExists = FileExistsMethod.InstanceCall<Func<object, string, bool>>();
        public static readonly Func<object, string, ushort, byte, object> AddFile = AddFileMethod.InstanceCall<Func<object, string, ushort, byte, object>>();
        public static readonly Action<object, string> DeleteFile = DeleteFileMethod.InstanceCall<Action<object, string>>();
    }

    public class MyZipFileInfoReflection
    {
        public static readonly BindingFlags Bind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static readonly Type ZipFileInfoType = MyZipArchiveReflection.ZipAssembly.GetType("MS.Internal.IO.Zip.ZipFileInfo");

        public static readonly PropertyInfo CompressionMethodProperty = ZipFileInfoType.GetProperty("CompressionMethod", Bind);
        public static readonly PropertyInfo DeflateOptionProperty = ZipFileInfoType.GetProperty("DeflateOption", Bind);
        public static readonly PropertyInfo FolderFlagProperty = ZipFileInfoType.GetProperty("FolderFlag", Bind);
        public static readonly PropertyInfo LastModFileDateTimeProperty = ZipFileInfoType.GetProperty("LastModFileDateTime", Bind);
        public static readonly PropertyInfo NameProperty = ZipFileInfoType.GetProperty("Name", Bind);
        public static readonly PropertyInfo VolumeLabelFlagProperty = ZipFileInfoType.GetProperty("VolumeLabelFlag", Bind);

        public static readonly MethodInfo GetStreamMethod = ZipFileInfoType.GetMethod("GetStream", Bind);

        public static readonly Func<object, ushort> CompressionMethod = CompressionMethodProperty.CreateGetter<object, ushort>();
        public static readonly Func<object, byte> DeflateOption = DeflateOptionProperty.CreateGetter<object, byte>();
        public static readonly Func<object, bool> FolderFlag = FolderFlagProperty.CreateGetter<object, bool>();
        public static readonly Func<object, DateTime> LastModFileDateTime = LastModFileDateTimeProperty.CreateGetter<object, DateTime>();
        public static readonly Func<object, string> Name = NameProperty.CreateGetter<object, string>();
        public static readonly Func<object, bool> VolumeLabelFlag = VolumeLabelFlagProperty.CreateGetter<object, bool>();

        public static readonly Func<object, FileMode, FileAccess, Stream> GetStream = GetStreamMethod.InstanceCall<Func<object, FileMode, FileAccess, Stream>>();
    }
#endif
}

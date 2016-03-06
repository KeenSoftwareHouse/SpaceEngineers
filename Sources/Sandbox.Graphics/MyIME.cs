using System;
using System.Runtime.InteropServices;
using Microsoft.International.Converters;

namespace MyIMESystem
{
    public static class MyIME
    {
		static int s;
		static IFELanguage fel;
		public static void IMEStart()
		{
			s = 0;
			fel = Activator.CreateInstance(Type.GetTypeFromProgID("MSIME.Japan")) as IFELanguage;
			fel.Open();
		}
		public static string RToJ(string r)
		{
			s++;
			return RToJ(r, s);
		}
		public static void IMEEnd()
		{
			fel.Close();
		}
		public static string RToJ(string r,int s)
		{
			string h = KanaConverter.RomajiToHiragana(r), j = "";
			//string h = r, j = "";
			fel.GetConversion(h, 1, -1, out j);
			return j;
		}
    }

	[ComImport]
	[Guid("019F7152-E6DB-11d0-83C3-00C04FDDB82E")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IFELanguage
	{
		int Open();
		int Close();
		int GetJMorphResult(uint dwRequest, uint dwCMode, int cwchInput, [MarshalAs(UnmanagedType.LPWStr)] string pwchInput, IntPtr pfCInfo, out object ppResult);
		int GetConversionModeCaps(ref uint pdwCaps);
		int GetPhonetic([MarshalAs(UnmanagedType.BStr)] string @string, int start, int length, [MarshalAs(UnmanagedType.BStr)] out string result);
		int GetConversion([MarshalAs(UnmanagedType.BStr)] string @string, int start, int length, [MarshalAs(UnmanagedType.BStr)] out string result);
	}
}

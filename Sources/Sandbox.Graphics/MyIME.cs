using System;
using System.Runtime.InteropServices;
using Microsoft.International.Converters;
using System.Text;

namespace MyIMESystem
{
    public static class MyIME
    {
		static int s;
		static IFELanguage fel;

		[StructLayout(LayoutKind.Sequential)]
		public class CANDIDATELIST
		{
			public int dwSize;
			public int dwStyle;
			public int dwCount;
			public int dwSelection;
			public int dwPageStart;
			public int dwPageSize;
			public int dwOffset;
		}

		[DllImport("imm32.dll", SetLastError = true)]
		public static extern int ImmGetCandidateList(int hIMC, int deIndex, ref CANDIDATELIST lpCandidateList, int dwBufLen);
		[DllImport("Imm32.dll")]
		private static extern int ImmGetCompositionString(int hIMC, int dwIndex, StringBuilder lpBuf, int dwBufLen);

		public static void IMEStart()
		{
			s = 0;
			fel = Activator.CreateInstance(Type.GetTypeFromProgID("MSIME.Japan")) as IFELanguage;
			fel.Open();
		}
		public static void IMEEnd()
		{
			fel.Close();
		}
		public static string RToH(string r)
		{
			return KanaConverter.RomajiToHiragana(r);
		}

		public static string HToK(string r)
		{
			string ans = "";
			fel.GetConversion(r, 1, -1,out ans);
			return ans;
		}

		public static string HToKK(string r)
		{
			return KanaConverter.HiraganaToKatakana(r);
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

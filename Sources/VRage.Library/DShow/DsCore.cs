/******************************************************
                  DirectShow .NET
		      netmaster@swissonline.ch
*******************************************************/
//					DsCore
// Core streaming interfaces, ported from axcore.idl

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace DShowNET
{
#if !XB1
    [ComVisible(false)]
    public enum PinDirection		// PIN_DIRECTION
    {
        Input,		// PINDIR_INPUT
        Output		// PINDIR_OUTPUT
    }

    [ComVisible(true), ComImport,
    Guid("56a86891-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPin
    {
        [PreserveSig]
        int Connect(
            [In]											IPin pReceivePin,
            [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

        [PreserveSig]
        int ReceiveConnection(
            [In]											IPin pReceivePin,
            [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

        [PreserveSig]
        int Disconnect();

        [PreserveSig]
        int ConnectedTo([Out] out IPin ppPin);

        [PreserveSig]
        int ConnectionMediaType(
            [Out, MarshalAs(UnmanagedType.LPStruct)]		AMMediaType pmt);

        [PreserveSig]
        int QueryPinInfo(IntPtr pInfo);

        [PreserveSig]
        int QueryDirection(out PinDirection pPinDir);

        [PreserveSig]
        int QueryId(
            [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string Id);

        [PreserveSig]
        int QueryAccept(
            [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

        [PreserveSig]
        int EnumMediaTypes(IntPtr ppEnum);

        [PreserveSig]
        int QueryInternalConnections(IntPtr apPin, [In, Out] ref int nPin);

        [PreserveSig]
        int EndOfStream();

        [PreserveSig]
        int BeginFlush();

        [PreserveSig]
        int EndFlush();

        [PreserveSig]
        int NewSegment(long tStart, long tStop, double dRate);
    }

    [ComVisible(true), ComImport,
    Guid("56a8689f-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFilterGraph
    {
        [PreserveSig]
        int AddFilter(
            [In] IBaseFilter pFilter,
            [In, MarshalAs(UnmanagedType.LPWStr)]			string pName);

        [PreserveSig]
        int RemoveFilter([In] IBaseFilter pFilter);

        [PreserveSig]
        int EnumFilters([Out] out IEnumFilters ppEnum);

        [PreserveSig]
        int FindFilterByName(
            [In, MarshalAs(UnmanagedType.LPWStr)]			string pName,
            [Out]										out IBaseFilter ppFilter);

        [PreserveSig]
        int ConnectDirect([In] IPin ppinOut, [In] IPin ppinIn,
           [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

        [PreserveSig]
        int Reconnect([In] IPin ppin);

        [PreserveSig]
        int Disconnect([In] IPin ppin);

        [PreserveSig]
        int SetDefaultSyncSource();

    }

    // ---------------------------------------------------------------------------------------

    [ComVisible(true), ComImport,
    Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IBaseFilter
    {
        #region "IPersist Methods"
        [PreserveSig]
        int GetClassID(
            [Out]									out Guid pClassID);
        #endregion

        #region "IMediaFilter Methods"
        [PreserveSig]
        int Stop();

        [PreserveSig]
        int Pause();

        [PreserveSig]
        int Run(long tStart);

        [PreserveSig]
        int GetState(int dwMilliSecsTimeout, [Out] out int filtState);

        [PreserveSig]
        int SetSyncSource([In] IReferenceClock pClock);

        [PreserveSig]
        int GetSyncSource([Out] out IReferenceClock pClock);
        #endregion

        [PreserveSig]
        int EnumPins(
            [Out]										out IEnumPins ppEnum);

        [PreserveSig]
        int FindPin(
            [In, MarshalAs(UnmanagedType.LPWStr)]			string Id,
            [Out]										out IPin ppPin);

        [PreserveSig]
        int QueryFilterInfo(
            [Out]											FilterInfo pInfo);

        [PreserveSig]
        int JoinFilterGraph(
            [In]											IFilterGraph pGraph,
            [In, MarshalAs(UnmanagedType.LPWStr)]			string pName);

        [PreserveSig]
        int QueryVendorInfo(
            [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string pVendorInfo);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), ComVisible(false)]
    public class FilterInfo		//  FILTER_INFO
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string achName;
        [MarshalAs(UnmanagedType.IUnknown)]
        public object pUnk;
    }

    [ComVisible(true), ComImport,
    Guid("36b73880-c2c8-11cf-8b46-00805f6cef60"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMediaSeeking
    {
        [PreserveSig]
        int GetCapabilities(out SeekingCapabilities pCapabilities);

        [PreserveSig]
        int CheckCapabilities([In, Out] ref SeekingCapabilities pCapabilities);

        [PreserveSig]
        int IsFormatSupported([In] ref Guid pFormat);
        [PreserveSig]
        int QueryPreferredFormat([Out] out Guid pFormat);

        [PreserveSig]
        int GetTimeFormat([Out] out Guid pFormat);
        [PreserveSig]
        int IsUsingTimeFormat([In] ref Guid pFormat);
        [PreserveSig]
        int SetTimeFormat([In] ref Guid pFormat);

        [PreserveSig]
        int GetDuration(out long pDuration);
        [PreserveSig]
        int GetStopPosition(out long pStop);
        [PreserveSig]
        int GetCurrentPosition(out long pCurrent);

        [PreserveSig]
        int ConvertTimeFormat(out long pTarget, [In] ref Guid pTargetFormat,
                                  long Source, [In] ref Guid pSourceFormat);

        [PreserveSig]
        int SetPositions(
            [In, Out, MarshalAs(UnmanagedType.LPStruct)]		DsOptInt64 pCurrent,
            SeekingFlags dwCurrentFlags,
            [In, Out, MarshalAs(UnmanagedType.LPStruct)]		DsOptInt64 pStop,
            SeekingFlags dwStopFlags);

        [PreserveSig]
        int GetPositions(out long pCurrent, out long pStop);

        [PreserveSig]
        int GetAvailable(out long pEarliest, out long pLatest);

        [PreserveSig]
        int SetRate(double dRate);
        [PreserveSig]
        int GetRate(out double pdRate);

        [PreserveSig]
        int GetPreroll(out long pllPreroll);
    }

    [Flags, ComVisible(false)]
    public enum SeekingCapabilities		// AM_SEEKING_SeekingCapabilities AM_SEEKING_SEEKING_CAPABILITIES
    {
        CanSeekAbsolute = 0x001,
        CanSeekForwards = 0x002,
        CanSeekBackwards = 0x004,
        CanGetCurrentPos = 0x008,
        CanGetStopPos = 0x010,
        CanGetDuration = 0x020,
        CanPlayBackwards = 0x040,
        CanDoSegments = 0x080,
        Source = 0x100		// Doesn't pass thru used to count segment ends
    }

    [Flags, ComVisible(false)]
    public enum SeekingFlags		// AM_SEEKING_SeekingFlags AM_SEEKING_SEEKING_FLAGS
    {
        NoPositioning = 0x00,		// No change
        AbsolutePositioning = 0x01,		// Position is supplied and is absolute
        RelativePositioning = 0x02,		// Position is supplied and is relative
        IncrementalPositioning = 0x03,		// (Stop) position relative to current, useful for seeking when paused (use +1)
        SeekToKeyFrame = 0x04,		// Just seek to key frame (performance gain)
        ReturnTime = 0x08,		// Plug the media time equivalents back into the supplied LONGLONGs
        Segment = 0x10,		// At end just do EC_ENDOFSEGMENT, don't do EndOfStream
        NoFlush = 0x20		// Don't flush
    }

    [ComVisible(true), ComImport,
    Guid("56a86897-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IReferenceClock
    {
        [PreserveSig]
        int GetTime(out long pTime);

        [PreserveSig]
        int AdviseTime(long baseTime, long streamTime, IntPtr hEvent, out int pdwAdviseCookie);

        [PreserveSig]
        int AdvisePeriodic(long startTime, long periodTime, IntPtr hSemaphore, out int pdwAdviseCookie);

        [PreserveSig]
        int Unadvise(int dwAdviseCookie);
    }

    [ComVisible(true), ComImport,
    Guid("56a86893-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumFilters
    {
        [PreserveSig]
        int Next(
            [In]															int cFilters,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]	out	IBaseFilter[] ppFilter,
            [Out]														out int pcFetched);

        [PreserveSig]
        int Skip([In] int cFilters);
        void Reset();
        void Clone([Out] out IEnumFilters ppEnum);
    }

    [ComVisible(true), ComImport,
    Guid("56a86892-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumPins
    {
        [PreserveSig]
        int Next(
            [In]															int cPins,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]	out	IPin[] ppPins,
            [Out]														out int pcFetched);

        [PreserveSig]
        int Skip([In] int cPins);
        void Reset();
        void Clone([Out] out IEnumPins ppEnum);
    }

    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    public class AMMediaType		//  AM_MEDIA_TYPE
    {
        public Guid majorType;
        public Guid subType;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fixedSizeSamples;
        [MarshalAs(UnmanagedType.Bool)]
        public bool temporalCompression;
        public int sampleSize;
        public Guid formatType;
        public IntPtr unkPtr;
        public int formatSize;
        public IntPtr formatPtr;
    }

    [ComVisible(true), ComImport,
    Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMediaSample
    {
        [PreserveSig]
        int GetPointer(out IntPtr ppBuffer);
        [PreserveSig]
        int GetSize();

        [PreserveSig]
        int GetTime(out long pTimeStart, out long pTimeEnd);

        [PreserveSig]
        int SetTime(
            [In, MarshalAs(UnmanagedType.LPStruct)]			DsOptInt64 pTimeStart,
            [In, MarshalAs(UnmanagedType.LPStruct)]			DsOptInt64 pTimeEnd);

        [PreserveSig]
        int IsSyncPoint();
        [PreserveSig]
        int SetSyncPoint(
            [In, MarshalAs(UnmanagedType.Bool)]			bool bIsSyncPoint);

        [PreserveSig]
        int IsPreroll();
        [PreserveSig]
        int SetPreroll(
            [In, MarshalAs(UnmanagedType.Bool)]			bool bIsPreroll);

        [PreserveSig]
        int GetActualDataLength();
        [PreserveSig]
        int SetActualDataLength(int len);

        [PreserveSig]
        int GetMediaType(
            [Out, MarshalAs(UnmanagedType.LPStruct)]	out AMMediaType ppMediaType);

        [PreserveSig]
        int SetMediaType(
            [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pMediaType);

        [PreserveSig]
        int IsDiscontinuity();
        [PreserveSig]
        int SetDiscontinuity(
            [In, MarshalAs(UnmanagedType.Bool)]			bool bDiscontinuity);

        [PreserveSig]
        int GetMediaTime(out long pTimeStart, out long pTimeEnd);

        [PreserveSig]
        int SetMediaTime(
            [In, MarshalAs(UnmanagedType.LPStruct)]			DsOptInt64 pTimeStart,
            [In, MarshalAs(UnmanagedType.LPStruct)]			DsOptInt64 pTimeEnd);
    }
#endif
} // namespace DShowNET

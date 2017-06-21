/******************************************************
                  DirectShow .NET
		      netmaster@swissonline.ch
*******************************************************/
//					DsControl
// basic Quartz control interfaces, ported from control.odl

using System;
using System.Runtime.InteropServices;

namespace DShowNET
{
#if XB1
#else
    [ComVisible(true), ComImport,
    Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IMediaControl
    {
        [PreserveSig]
        int Run();

        [PreserveSig]
        int Pause();

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int GetState(int msTimeout, out int pfs);

        [PreserveSig]
        int RenderFile(string strFilename);

        [PreserveSig]
        int AddSourceFilter(
            [In]											string strFilename,
            [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

        [PreserveSig]
        int get_FilterCollection(
            [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

        [PreserveSig]
        int get_RegFilterCollection(
            [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

        [PreserveSig]
        int StopWhenReady();
    }

    [ComVisible(true), ComImport,
    Guid("56a868c0-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IMediaEventEx
    {
        #region "IMediaEvent Methods"
        [PreserveSig]
        int GetEventHandle(out IntPtr hEvent);

        [PreserveSig]
        int GetEvent(out DsEvCode lEventCode, out int lParam1, out int lParam2, int msTimeout);

        [PreserveSig]
        int WaitForCompletion(int msTimeout, [Out] out int pEvCode);

        [PreserveSig]
        int CancelDefaultHandling(int lEvCode);

        [PreserveSig]
        int RestoreDefaultHandling(int lEvCode);

        [PreserveSig]
        int FreeEventParams(DsEvCode lEvCode, int lParam1, int lParam2);
        #endregion


        [PreserveSig]
        int SetNotifyWindow(IntPtr hwnd, int lMsg, IntPtr lInstanceData);

        [PreserveSig]
        int SetNotifyFlags(int lNoNotifyFlags);

        [PreserveSig]
        int GetNotifyFlags(out int lplNoNotifyFlags);
    }

    [ComVisible(true), ComImport,
    Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IVideoWindow
    {
        [PreserveSig]
        int put_Caption(string caption);
        [PreserveSig]
        int get_Caption([Out] out string caption);

        [PreserveSig]
        int put_WindowStyle(int windowStyle);
        [PreserveSig]
        int get_WindowStyle(out int windowStyle);

        [PreserveSig]
        int put_WindowStyleEx(int windowStyleEx);
        [PreserveSig]
        int get_WindowStyleEx(out int windowStyleEx);

        [PreserveSig]
        int put_AutoShow(int autoShow);
        [PreserveSig]
        int get_AutoShow(out int autoShow);

        [PreserveSig]
        int put_WindowState(int windowState);
        [PreserveSig]
        int get_WindowState(out int windowState);

        [PreserveSig]
        int put_BackgroundPalette(int backgroundPalette);
        [PreserveSig]
        int get_BackgroundPalette(out int backgroundPalette);

        [PreserveSig]
        int put_Visible(int visible);
        [PreserveSig]
        int get_Visible(out int visible);

        [PreserveSig]
        int put_Left(int left);
        [PreserveSig]
        int get_Left(out int left);

        [PreserveSig]
        int put_Width(int width);
        [PreserveSig]
        int get_Width(out int width);

        [PreserveSig]
        int put_Top(int top);
        [PreserveSig]
        int get_Top(out int top);

        [PreserveSig]
        int put_Height(int height);
        [PreserveSig]
        int get_Height(out int height);

        [PreserveSig]
        int put_Owner(IntPtr owner);
        [PreserveSig]
        int get_Owner(out IntPtr owner);

        [PreserveSig]
        int put_MessageDrain(IntPtr drain);
        [PreserveSig]
        int get_MessageDrain(out IntPtr drain);

        [PreserveSig]
        int get_BorderColor(out int color);
        [PreserveSig]
        int put_BorderColor(int color);

        [PreserveSig]
        int get_FullScreenMode(out int fullScreenMode);
        [PreserveSig]
        int put_FullScreenMode(int fullScreenMode);

        [PreserveSig]
        int SetWindowForeground(int focus);

        [PreserveSig]
        int NotifyOwnerMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [PreserveSig]
        int SetWindowPosition(int left, int top, int width, int height);

        [PreserveSig]
        int GetWindowPosition(out int left, out int top, out int width, out int height);

        [PreserveSig]
        int GetMinIdealImageSize(out int width, out int height);

        [PreserveSig]
        int GetMaxIdealImageSize(out int width, out int height);

        [PreserveSig]
        int GetRestorePosition(out int left, out int top, out int width, out int height);

        [PreserveSig]
        int HideCursor(int hideCursor);

        [PreserveSig]
        int IsCursorHidden(out int hideCursor);

    }

    [ComVisible(true), ComImport,
    Guid("56a868b2-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IMediaPosition
    {
        [PreserveSig]
        int get_Duration(out double pLength);

        [PreserveSig]
        int put_CurrentPosition(double llTime);
        [PreserveSig]
        int get_CurrentPosition(out double pllTime);

        [PreserveSig]
        int get_StopTime(out double pllTime);
        [PreserveSig]
        int put_StopTime(double llTime);

        [PreserveSig]
        int get_PrerollTime(out double pllTime);
        [PreserveSig]
        int put_PrerollTime(double llTime);

        [PreserveSig]
        int put_Rate(double dRate);
        [PreserveSig]
        int get_Rate(out double pdRate);

        [PreserveSig]
        int CanSeekForward(out int pCanSeekForward);
        [PreserveSig]
        int CanSeekBackward(out int pCanSeekBackward);
    }

    [ComVisible(true), ComImport,
    Guid("56a868b3-0ad4-11ce-b03a-0020af0ba770"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IBasicAudio
    {
        [PreserveSig]
        int put_Volume(int lVolume);
        [PreserveSig]
        int get_Volume(out int plVolume);

        [PreserveSig]
        int put_Balance(int lBalance);
        [PreserveSig]
        int get_Balance(out int plBalance);
    }

    public enum DsEvCode
    {
        None,
        Complete = 0x01,		// EC_COMPLETE
        UserAbort = 0x02,		// EC_USERABORT
        ErrorAbort = 0x03,		// EC_ERRORABORT
        Time = 0x04,		// EC_TIME
        Repaint = 0x05,		// EC_REPAINT
        StErrStopped = 0x06,		// EC_STREAM_ERROR_STOPPED
        StErrStPlaying = 0x07,		// EC_STREAM_ERROR_STILLPLAYING
        ErrorStPlaying = 0x08,		// EC_ERROR_STILLPLAYING
        PaletteChanged = 0x09,		// EC_PALETTE_CHANGED
        VideoSizeChanged = 0x0a,		// EC_VIDEO_SIZE_CHANGED
        QualityChange = 0x0b,		// EC_QUALITY_CHANGE
        ShuttingDown = 0x0c,		// EC_SHUTTING_DOWN
        ClockChanged = 0x0d,		// EC_CLOCK_CHANGED
        Paused = 0x0e,		// EC_PAUSED
        OpeningFile = 0x10,		// EC_OPENING_FILE
        BufferingData = 0x11,		// EC_BUFFERING_DATA
        FullScreenLost = 0x12,		// EC_FULLSCREEN_LOST
        Activate = 0x13,		// EC_ACTIVATE
        NeedRestart = 0x14,		// EC_NEED_RESTART
        WindowDestroyed = 0x15,		// EC_WINDOW_DESTROYED
        DisplayChanged = 0x16,		// EC_DISPLAY_CHANGED
        Starvation = 0x17,		// EC_STARVATION
        OleEvent = 0x18,		// EC_OLE_EVENT
        NotifyWindow = 0x19,		// EC_NOTIFY_WINDOW
        // EC_ ....

        // DVDevCod.h
        DvdDomChange = 0x101,	// EC_DVD_DOMAIN_CHANGE
        DvdTitleChange = 0x102,	// EC_DVD_TITLE_CHANGE
        DvdChaptStart = 0x103,	// EC_DVD_CHAPTER_START
        DvdAudioStChange = 0x104,	// EC_DVD_AUDIO_STREAM_CHANGE

        DvdSubPicStChange = 0x105,	// EC_DVD_SUBPICTURE_STREAM_CHANGE
        DvdAngleChange = 0x106,	// EC_DVD_ANGLE_CHANGE
        DvdButtonChange = 0x107,	// EC_DVD_BUTTON_CHANGE
        DvdValidUopsChange = 0x108,	// EC_DVD_VALID_UOPS_CHANGE
        DvdStillOn = 0x109,	// EC_DVD_STILL_ON
        DvdStillOff = 0x10a,	// EC_DVD_STILL_OFF
        DvdCurrentTime = 0x10b,	// EC_DVD_CURRENT_TIME
        DvdError = 0x10c,	// EC_DVD_ERROR
        DvdWarning = 0x10d,	// EC_DVD_WARNING
        DvdChaptAutoStop = 0x10e,	// EC_DVD_CHAPTER_AUTOSTOP
        DvdNoFpPgc = 0x10f,	// EC_DVD_NO_FP_PGC
        DvdPlaybRateChange = 0x110,	// EC_DVD_PLAYBACK_RATE_CHANGE
        DvdParentalLChange = 0x111,	// EC_DVD_PARENTAL_LEVEL_CHANGE
        DvdPlaybStopped = 0x112,	// EC_DVD_PLAYBACK_STOPPED
        DvdAnglesAvail = 0x113,	// EC_DVD_ANGLES_AVAILABLE
        DvdPeriodAStop = 0x114,	// EC_DVD_PLAYPERIOD_AUTOSTOP
        DvdButtonAActivated = 0x115,	// EC_DVD_BUTTON_AUTO_ACTIVATED
        DvdCmdStart = 0x116,	// EC_DVD_CMD_START
        DvdCmdEnd = 0x117,	// EC_DVD_CMD_END
        DvdDiscEjected = 0x118,	// EC_DVD_DISC_EJECTED
        DvdDiscInserted = 0x119,	// EC_DVD_DISC_INSERTED
        DvdCurrentHmsfTime = 0x11a,	// EC_DVD_CURRENT_HMSF_TIME
        DvdKaraokeMode = 0x11b		// EC_DVD_KARAOKE_MODE
    }
#endif
} // namespace DShowNET

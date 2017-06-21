/******************************************************
 DirectShow .NET
 netmaster@swissonline.ch
*******************************************************/
// UUIDs from uuids.h

using System;
using System.Runtime.InteropServices;

namespace DShowNET
{
#if !XB1
    [ComVisible(false)]
    public class Clsid		// uuids.h  :  CLSID_*
    {
        /// <summary> CLSID_FilterGraph, filter Graph </summary>
        public static readonly Guid FilterGraph = new Guid(0xe436ebb3, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        /// <summary> CLSID_SampleGrabber, Sample Grabber filter </summary>
        public static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);
    }
#endif
} // namespace DShowNET

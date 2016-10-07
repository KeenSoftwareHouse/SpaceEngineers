#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace VRage
{
    public static class DateTimeExtensions
    {
        public static DateTime Now_GarbageFree(this DateTime dateTime)
        {
            return TimeUtil.LocalTime;
        }
    }


    public static class TimeUtil
    {
        [DllImport("kernel32.dll")]
        static extern void GetLocalTime(out SYSTEMTIME time);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;
        }

        public static DateTime LocalTime
        {
            get
            {
                SYSTEMTIME nativeTime;
                GetLocalTime(out nativeTime);

                return new DateTime(nativeTime.Year, nativeTime.Month, nativeTime.Day,
                                    nativeTime.Hour, nativeTime.Minute, nativeTime.Second,
                                    nativeTime.Milliseconds, DateTimeKind.Local);
            }
        }
    }
}
#endif // !XB1

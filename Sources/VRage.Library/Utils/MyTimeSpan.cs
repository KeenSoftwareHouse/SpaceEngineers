using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Utils
{
    /// <summary>
    /// Hi-resolution time span. Beware: the resolution can be different on different systems!
    /// </summary>
    public struct MyTimeSpan
    {
        public static readonly MyTimeSpan Zero = new MyTimeSpan();
        public static readonly MyTimeSpan MaxValue = new MyTimeSpan(long.MaxValue);

        public readonly long Ticks;

        public double Nanoseconds
        {
            get { return Ticks / (MyGameTimer.Frequency / 1.0e9); }
        }

        public double Microseconds
        {
            get { return Ticks / (MyGameTimer.Frequency / 1.0e6); }
        }

        public double Miliseconds
        {
            get { return Ticks / (MyGameTimer.Frequency / 1.0e3); }
        }

        public double Seconds
        {
            get { return Ticks / (double)MyGameTimer.Frequency; }
        }

        /// <summary>
        /// This may not be accurate for large values - double accuracy
        /// </summary>
        public TimeSpan TimeSpan
        {
            get { return TimeSpan.FromTicks((long)Math.Round(Ticks * (TimeSpan.TicksPerSecond / (double)MyGameTimer.Frequency))); }
        }

        public MyTimeSpan(long stopwatchTicks)
        {
            Ticks = stopwatchTicks;
        }

        public override bool Equals(object obj)
        {
            return Ticks == ((MyTimeSpan)obj).Ticks;
        }

        public override int GetHashCode()
        {
            return Ticks.GetHashCode();
        }

        public static MyTimeSpan FromTicks(long ticks)
        {
            return new MyTimeSpan(ticks);
        }

        public static MyTimeSpan FromSeconds(double seconds)
        {
            return FromMiliseconds(seconds * 1000);
        }

        public static MyTimeSpan FromMinutes(double minutes)
        {
            return FromSeconds(minutes * 60);
        }

        public static MyTimeSpan FromMiliseconds(double miliseconds)
        {
            return new MyTimeSpan((long)(miliseconds * 0.001f * MyGameTimer.Frequency));
        }

        public static MyTimeSpan operator +(MyTimeSpan a, MyTimeSpan b)
        {
            return new MyTimeSpan(a.Ticks + b.Ticks);
        }

        public static MyTimeSpan operator -(MyTimeSpan a, MyTimeSpan b)
        {
            return new MyTimeSpan(a.Ticks - b.Ticks);
        }

        public static bool operator !=(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks != b.Ticks;
        }

        public static bool operator ==(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks == b.Ticks;
        }

        public static bool operator >(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks > b.Ticks;
        }

        public static bool operator <(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks < b.Ticks;
        }

        public static bool operator >=(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks >= b.Ticks;
        }

        public static bool operator <=(MyTimeSpan a, MyTimeSpan b)
        {
            return a.Ticks <= b.Ticks;
        }

        public override string ToString()
        {
            return ((int)Math.Round(Miliseconds)).ToString();
        }
    }
}

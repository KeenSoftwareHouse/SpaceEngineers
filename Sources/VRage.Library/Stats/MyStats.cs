using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage.Library.Utils;

namespace VRage.Stats
{
    public class MyStats
    {
        public enum SortEnum
        {
            None,
            Name,
        }

#if UNSHARPER
		public SortEnum Sort = SortEnum.None;
#else
        public volatile SortEnum Sort = SortEnum.None;
#endif

        static Comparer<KeyValuePair<string, MyStat>> m_nameComparer = new MyNameComparer();

        MyGameTimer m_timer = new MyGameTimer();
        NumberFormatInfo m_format = new NumberFormatInfo() { NumberDecimalSeparator = ".", NumberGroupSeparator = " " };
        FastResourceLock m_lock = new FastResourceLock();
        Dictionary<string, MyStat> m_stats = new Dictionary<string, MyStat>(1024);

        List<KeyValuePair<string, MyStat>> m_tmpWriteList = new List<KeyValuePair<string, MyStat>>(1024);

        private MyStat GetStat(string name)
        {
            MyStat result;
            using (m_lock.AcquireSharedUsing())
            {
                if (m_stats.TryGetValue(name, out result))
                    return result;
            }
            using (m_lock.AcquireExclusiveUsing())
            {
                // Racing condition, someone can faster insert this value
                if (m_stats.TryGetValue(name, out result))
                    return result;
                else
                {
                    result = new MyStat();
                    m_stats[name] = result;
                    return result;
                }
            }
        }

        /// <summary>
        /// Clears all stats (doesn't remove them)
        /// </summary>
        public void Clear()
        {
            using (m_lock.AcquireSharedUsing())
            {
                foreach (var stat in m_stats)
                {
                    stat.Value.Clear();
                }
            }
        }

        /// <summary>
        /// Removes all stats
        /// </summary>
        public void RemoveAll()
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_stats.Clear();
            }
        }

        /// <summary>
        /// Remove a stat
        /// </summary>
        public void Remove(string name)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_stats.Remove(name);
            }
        }

        public void Clear(string name)
        {
            GetStat(name).Clear();
        }

        /// <summary>
        /// Increments an internal counter with given name and sets it to refresh after given time has passed.
        /// </summary>
        public void Increment(string name, int refreshMs = 0, int clearRateMs = -1)
        {
            Write(name, 0, MyStatTypeEnum.Counter, refreshMs, 0, clearRateMs);
        }

        public MyStatToken Measure(string name, MyStatTypeEnum type, int refreshMs = 200, int numDecimals = 1, int clearRateMs = -1)
        {
            var stat = GetStat(name);
            if (stat.DrawText == null)
            {
                // One time alloc for new stat
                stat.DrawText = GetMeasureText(name, type);
            }
            stat.ChangeSettings((type | MyStatTypeEnum.FormatFlag) & ~MyStatTypeEnum.LongFlag, refreshMs, numDecimals, clearRateMs);
            return new MyStatToken(m_timer, stat);
        }
        
        public MyStatToken Measure(string name)
        {
            return Measure(name, MyStatTypeEnum.Avg);
        }

        private string GetMeasureText(string name, MyStatTypeEnum type)
        {
            switch (type & ~MyStatTypeEnum.AllFlags)
            {
                case MyStatTypeEnum.Counter:
                    return name + ": {0}x";

                case MyStatTypeEnum.CounterSum:
                    return name + ": {0}x / {1}ms";

                case MyStatTypeEnum.MinMax:
                    return name + ": {0}ms / {1}ms";

                case MyStatTypeEnum.MinMaxAvg:
                    return name + ": {0}ms / {1}ms / {2}ms";

                default:
                    return name + ": {0}ms";
            }
        }

        /// <summary>
        /// Write stat, colon and space is added automatically
        /// </summary>
        public void Write(string name, float value, MyStatTypeEnum type, int refreshMs, int numDecimals, int clearRateMs = -1)
        {
            GetStat(name).Write(value, type, refreshMs, numDecimals, clearRateMs);
        }

        /// <summary>
        /// Write stat, colon and space is added automatically
        /// </summary>
        public void Write(string name, long value, MyStatTypeEnum type, int refreshMs, int numDecimals, int clearRateMs = -1)
        {
            GetStat(name).Write(value, type, refreshMs, numDecimals, clearRateMs);
        }

        /// <summary>
        /// Write stat using format string
        /// Number of arguments in format string:
        /// MinMaxAvg - three
        /// MinMax - two
        /// Other - one
        /// </summary>
        public void WriteFormat(string name, float value, MyStatTypeEnum type, int refreshMs, int numDecimals, int clearRateMs = -1)
        {
            GetStat(name).Write(value, type | MyStatTypeEnum.FormatFlag, refreshMs, numDecimals, clearRateMs);
        }

        /// <summary>
        /// Write stat using format string
        /// Number of arguments in format string:
        /// MinMaxAvg - three
        /// MinMax - two
        /// Other - one
        /// </summary>
        public void WriteFormat(string name, long value, MyStatTypeEnum type, int refreshMs, int numDecimals, int clearRateMs = -1)
        {
            GetStat(name).Write(value, type | MyStatTypeEnum.FormatFlag, refreshMs, numDecimals, clearRateMs);
        }

        public void WriteTo(StringBuilder writeTo)
        {
            lock (m_tmpWriteList) // Only one access to WriteStats
            {
                try
                {
                    using (m_lock.AcquireSharedUsing())
                    {
                        foreach (var stat in m_stats)
                        {
                            m_tmpWriteList.Add(stat);
                        }
                    }

                    if (Sort == SortEnum.Name)
                    {
                        m_tmpWriteList.Sort(m_nameComparer);
                    }

                    foreach (var stat in m_tmpWriteList)
                    {
                        AppendStat(writeTo, stat.Key, stat.Value);
                    }
                }
                finally
                {
                    m_tmpWriteList.Clear();
                }
            }
        }

        private void AppendStatLine<A, B, C>(StringBuilder text, string statName, A arg0, B arg1, C arg2, NumberFormatInfo format, string formatString)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
        {
            if (formatString == null)
            {
                // E.g. "Draw time min/max/avg: {0} / {1} / {2}"
                text.ConcatFormat(statName, arg0, arg1, arg2, format);
            }
            else
            {
                // E.g. "{0}: {1} / {2} / {3}"
                text.ConcatFormat(formatString, statName, arg0, arg1, arg2, format);
            }
            text.AppendLine();
        }

        private MyTimeSpan RequiredInactivity(MyStatTypeEnum type)
        {
            if ((type & MyStatTypeEnum.DontDisappearFlag) == MyStatTypeEnum.DontDisappearFlag)
                return MyTimeSpan.MaxValue;
            else if ((type & MyStatTypeEnum.KeepInactiveLongerFlag) == MyStatTypeEnum.KeepInactiveLongerFlag)
                return MyTimeSpan.FromSeconds(30);
            else
                return MyTimeSpan.FromSeconds(3);
        }

        private void AppendStat(StringBuilder text, string statKey, MyStat stat)
        {
            MyStat.Value sum, min, max, last;
            int count, decimals;
            MyStatTypeEnum type;
            MyTimeSpan inactivity;

            stat.ReadAndClear(m_timer.Elapsed, out sum, out count, out min, out max, out last, out type, out decimals, out inactivity);

            if (inactivity > RequiredInactivity(type))
            {
                Remove(statKey);
                return;
            }

            string drawText = stat.DrawText ?? statKey;

            bool isLong = (type & MyStatTypeEnum.LongFlag) == MyStatTypeEnum.LongFlag;

            float avg = (float)((isLong ? (double)sum.AsLong : (double)sum.AsFloat) / count);

            m_format.NumberDecimalDigits = decimals;
            m_format.NumberGroupSeparator = decimals == 0 ? "," : String.Empty;
            bool isFormatString = (type & MyStatTypeEnum.FormatFlag) == MyStatTypeEnum.FormatFlag;
            switch (type & ~MyStatTypeEnum.AllFlags)
            {
                case MyStatTypeEnum.Avg:
                    AppendStatLine(text, drawText, avg, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.Counter:
                    AppendStatLine(text, drawText, count, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.CurrentValue:
                    if (isLong)
                        AppendStatLine(text, drawText, last.AsLong, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    else
                        AppendStatLine(text, drawText, last.AsFloat, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.Max:
                    if (isLong)
                        AppendStatLine(text, drawText, max.AsLong, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    else
                        AppendStatLine(text, drawText, max.AsFloat, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.Min:
                    if (isLong)
                        AppendStatLine(text, drawText, min.AsLong, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    else
                        AppendStatLine(text, drawText, min.AsFloat, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.MinMax:
                    if (isLong)
                        AppendStatLine(text, drawText, min.AsLong, max.AsLong, 0, m_format, isFormatString ? null : "{0}: {1} / {2}");
                    else
                        AppendStatLine(text, drawText, min.AsFloat, max.AsFloat, 0, m_format, isFormatString ? null : "{0}: {1} / {2}");
                    break;

                case MyStatTypeEnum.MinMaxAvg:
                    if (isLong)
                        AppendStatLine(text, drawText, min.AsLong, max.AsLong, avg, m_format, isFormatString ? null : "{0}: {1} / {2} / {3}");
                    else
                        AppendStatLine(text, drawText, min.AsFloat, max.AsFloat, avg, m_format, isFormatString ? null : "{0}: {1} / {2} / {3}");
                    break;

                case MyStatTypeEnum.Sum:
                    if (isLong)
                        AppendStatLine(text, drawText, sum.AsLong, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    else
                        AppendStatLine(text, drawText, sum.AsFloat, 0, 0, m_format, isFormatString ? null : "{0}: {1}");
                    break;

                case MyStatTypeEnum.CounterSum:
                    if (isLong)
                        AppendStatLine(text, drawText, count, sum.AsLong, 0, m_format, isFormatString ? null : "{0}: {1} / {2}");
                    else
                        AppendStatLine(text, drawText, count, sum.AsFloat, 0, m_format, isFormatString ? null : "{0}: {1} / {2}");
                    break;
            }
        }
    }
}

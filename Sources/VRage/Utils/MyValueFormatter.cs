using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using VRageMath;

namespace VRage.Utils
{
    public class MyValueFormatter
    {
        static private NumberFormatInfo m_numberFormatInfoHelper;
        
        static MyValueFormatter()
        {
            m_numberFormatInfoHelper = new NumberFormatInfo();
            m_numberFormatInfoHelper.NumberDecimalSeparator = ".";
            m_numberFormatInfoHelper.NumberGroupSeparator = " ";
        }


        //  Formats to: "1 234.123"
        public static string GetFormatedFloat(float num, int decimalDigits)
        {
            m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
            return num.ToString("N", m_numberFormatInfoHelper);
        }        

        //  Same as GetFormatedFloat() but allow to specify group separator
        public static string GetFormatedFloat(float num, int decimalDigits, string groupSeparator)
        {
            string originalGroupSeparator = m_numberFormatInfoHelper.NumberGroupSeparator;
            m_numberFormatInfoHelper.NumberGroupSeparator = groupSeparator;
            m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
            string ret = num.ToString("N", m_numberFormatInfoHelper);
            m_numberFormatInfoHelper.NumberGroupSeparator = originalGroupSeparator;
            return ret;
        }

        public static string GetFormatedDouble(double num, int decimalDigits)
        {
            m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
            return num.ToString("N", m_numberFormatInfoHelper);
        }

        public static string GetFormatedQP(decimal num)
        {
            return GetFormatedDecimal(num, 1);
        }

        public static string GetFormatedDecimal(decimal num, int decimalDigits)
        {
            m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
            return num.ToString("N", m_numberFormatInfoHelper);
        }

        public static string GetFormatedGameMoney(decimal num)
        {
            return GetFormatedDecimal(num, 2);
        }

        public static decimal GetDecimalFromString(string number, int decimalDigits)
        {
            try
            {
                m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
                //by Gregory: Added Round cause decimal digits weren't wworking properly
                return Math.Round(System.Convert.ToDecimal(number, m_numberFormatInfoHelper), decimalDigits);
            }
            catch
            {
            }
            return 0;
        }

        public static float? GetFloatFromString(string number, int decimalDigits, string groupSeparator) 
        {
            float? result = null;

            string originalGroupSeparator = m_numberFormatInfoHelper.NumberGroupSeparator;
            m_numberFormatInfoHelper.NumberGroupSeparator = groupSeparator;
            m_numberFormatInfoHelper.NumberDecimalDigits = decimalDigits;
            try
            {
                //by Gregory: Added Round cause decimal digits weren't wworking properly
                result = (float)Math.Round((float)System.Convert.ToDouble(number, m_numberFormatInfoHelper), decimalDigits);
            }
            catch 
            {
            }
            m_numberFormatInfoHelper.NumberGroupSeparator = originalGroupSeparator;

            return result;
        }

        public static string GetFormatedLong(long l)
        {
            //  By Marek Rosa at 28.4.2010: Changed according to implementation int GetFormatedInt()
            return l.ToString("#,0", CultureInfo.InvariantCulture);
        }

        public static String GetFormatedDateTimeOffset(DateTimeOffset dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
        }

        public static String GetFormatedDateTime(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
        }

        public static String GetFormatedDateTimeForFilename(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd-HH-mm-ss-fff", DateTimeFormatInfo.InvariantInfo);
        }

        //  Especially for displaying the price on the web site
        public static string GetFormatedPriceEUR(decimal num)
        {
            return GetFormatedDecimal(num, 2) + " €";
        }
        
        //  Especially for displaying the price on the web site
        public static string GetFormatedPriceUSD(decimal num)
        {
            return "$" + GetFormatedDecimal(num, 2);
        }

        //  Especially for displaying the price on the web site - in USD
        //  Input price is in EUR and we convert it here to USD, rounding to two decimal points
        public static string GetFormatedPriceUSD(decimal priceInEur, decimal exchangeRateEurToUsd)
        {
            return "~" + GetFormatedDecimal(decimal.Round(exchangeRateEurToUsd * priceInEur, 2), 2) + " $";
        }

        public static string GetFormatedInt(int i)
        {
            //  By Marek Rosa at 20.4.2008: This is my last try to have working int formating with group separator ",".
            //  Now it display '0' as '0' and any higher positive/negative number with correct grouping.
            return i.ToString("#,0", CultureInfo.InvariantCulture);
        }

        public static string GetFormatedArray<T>(T[] array)
        {
            string s = string.Empty;
            for (int i = 0; i < array.Length; i++)
            {
                s += array[i].ToString();
                if (i < (array.Length - 1)) s += ", ";
            }
            return s;
        }

        public static void AppendFormattedValueInBestUnit(float value, string[] unitNames, float[] unitMultipliers, int unitDecimalDigits, StringBuilder output)
        {
            Debug.Assert(unitNames.Length == unitMultipliers.Length);

            var absValue = Math.Abs(value);
            int i = 1;
            for (; i < unitMultipliers.Length; ++i)
            {
                if (absValue < unitMultipliers[i])
                    break;
            }

            --i; // move back to the best unit
            value /= unitMultipliers[i];
            output.AppendDecimal(Math.Round(value, unitDecimalDigits), unitDecimalDigits);
            output.Append(' ').Append(unitNames[i]);
        }

        public static void AppendFormattedValueInBestUnit(float value, string[] unitNames, float[] unitMultipliers, int[] unitDecimalDigits, StringBuilder output)
        {
            Debug.Assert(unitNames.Length == unitDecimalDigits.Length);
            if (float.IsInfinity(value))
            {
                output.Append('-');
                return;
            }

            var absValue = Math.Abs(value);
            int i = 1;
            for (; i < unitMultipliers.Length; ++i)
            {
                if (absValue < unitMultipliers[i])
                    break;
            }

            --i; // move back to the best unit
            value /= unitMultipliers[i];
            output.AppendDecimal(Math.Round(value, unitDecimalDigits[i]), unitDecimalDigits[i]);
            output.Append(' ').Append(unitNames[i]);
        }

        private static readonly string[] m_genericUnitNames = new string[] { "", "k", "M", "G", "T" };
        private static readonly float[] m_genericUnitMultipliers = new float[] { 1f, 1000f, 1000000f, 1000000000f, 1000000000000f };
        private static readonly int[] m_genericUnitDigits = new int[] { 1, 1, 1, 1, 1 };
        public static void AppendGenericInBestUnit(float genericInBase, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(genericInBase, m_genericUnitNames, m_genericUnitMultipliers, m_genericUnitDigits, output);
        }

        public static void AppendGenericInBestUnit(float genericInBase, int numDecimals, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(genericInBase, m_genericUnitNames, m_genericUnitMultipliers, numDecimals, output);
        }

        private static readonly string[] m_forceUnitNames = new string[] { "N", "kN", "MN" };
        private static readonly float[] m_forceUnitMultipliers = new float[] { 1f, 1000f, 1000000f };
        private static readonly int[] m_forceUnitDigits = new int[] { 1, 1, 1 };
        public static void AppendForceInBestUnit(float forceInNewtons, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(forceInNewtons, m_forceUnitNames, m_forceUnitMultipliers, m_forceUnitDigits, output);
        }

        private static readonly string[] m_torqueUnitNames      = new string[] { "Nm", "kNm", "MNm" };
        private static readonly float[] m_torqueUnitMultipliers = new float[] { 1f, 1000f, 1000000f };
        private static readonly int[] m_torqueUnitDigits        = new int[] { 0, 1, 1 };
        public static void AppendTorqueInBestUnit(float torqueInNewtonMeters, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(torqueInNewtonMeters, m_torqueUnitNames, m_torqueUnitMultipliers, m_torqueUnitDigits, output);
        }

        private static readonly string[] m_workUnitNames      = new string[] { "W", "kW", "MW", "GW" };
        private static readonly float[] m_workUnitMultipliers = new float[] { 0.000001f, 0.001f, 1f, 1000f };
        private static readonly int[] m_workUnitDigits        = new int[] { 0, 2, 2, 2 };
        public static void AppendWorkInBestUnit(float workInMegaWatts, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(workInMegaWatts, m_workUnitNames, m_workUnitMultipliers, m_workUnitDigits, output);
        }

        private static readonly string[] m_workHoursUnitNames = new string[] { "Wh", "kWh", "MWh", "GWh" };
        private static readonly float[] m_workHoursUnitMultipliers = new float[] { 0.000001f, 0.001f, 1f, 1000f };
        private static readonly int[] m_workHoursUnitDigits = new int[] { 0, 2, 2, 2 };
        public static void AppendWorkHoursInBestUnit(float workInMegaWatts, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(workInMegaWatts, m_workHoursUnitNames, m_workHoursUnitMultipliers, m_workHoursUnitDigits, output);
        }

        private static readonly string[] m_timeUnitNames      = new string[] { "sec", "min", "hours",      "days",          "years" };
        private static readonly float[] m_timeUnitMultipliers = new float[]  {    1f,   60f,  60*60f, 60f*60f*24f, 60f*60f*24f*365f };
        private static readonly int[] m_timeUnitDigits        = new int[] { 0, 0, 0, 0, 0, };
        public static void AppendTimeInBestUnit(float timeInSeconds, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(timeInSeconds, m_timeUnitNames, m_timeUnitMultipliers, m_timeUnitDigits, output);
        }

        private static readonly string[] m_weightUnitNames = new string[] { "g", "kg", "T", "MT" };
        private static readonly float[] m_weightUnitMultipliers = new float[] { 0.001f, 1f, 1000f, 1000000f };
        private static readonly int[] m_weightUnitDigits = new int[] { 0, 2, 2, 2 };
        public static void AppendWeightInBestUnit(float weightInKG, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(weightInKG, m_weightUnitNames, m_weightUnitMultipliers, m_weightUnitDigits, output);
        }

        private static readonly string[] m_volumeUnitNames = new string[] { "mL", "cL", "dL", "L", "hL", "m³" };
        private static readonly float[] m_volumeUnitMultipliers = new float[] { 0.000001f, 0.00001f, 0.0001f, 0.001f, 0.1f, 1f };
        private static readonly int[] m_volumeUnitDigits = new int[] { 0, 0, 0, 0, 2, 1 };
        public static void AppendVolumeInBestUnit(float volumeInCubicMeters, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(volumeInCubicMeters, m_volumeUnitNames, m_volumeUnitMultipliers, m_volumeUnitDigits, output);
        }

        public static void AppendTimeExact(int timeInSeconds, StringBuilder output)
        {
            if (timeInSeconds >= 60 * 60 * 24)
            {
                output.Append(timeInSeconds / (60 * 60 * 24));
                output.Append("d ");
            }

            output.ConcatFormat("{0:00}", timeInSeconds / (60 * 60) % 24);
            output.Append(":");
            output.ConcatFormat("{0:00}", timeInSeconds / 60 % 60);
            output.Append(":");
            output.ConcatFormat("{0:00}", timeInSeconds % 60);
        }

        public static void AppendTimeExactMinSec(int timeInSeconds, StringBuilder output)
        {
            output.ConcatFormat("{0:00}", timeInSeconds / 60 % (60 * 24));
            output.Append(":");
            output.ConcatFormat("{0:00}", timeInSeconds % 60);
        }

        private static readonly string[] m_distanceUnitNames = new string[] { "mm", "cm", "m", "km" };
        private static readonly float[] m_distanceUnitMultipliers = new float[] { 0.001f, 0.01f, 1f, 1000f };
        private static readonly int[] m_distanceUnitDigits = new int[] { 0, 1, 2, 2, };
        public static void AppendDistanceInBestUnit(float distanceInMeters, StringBuilder output)
        {
            AppendFormattedValueInBestUnit(distanceInMeters, m_distanceUnitNames, m_distanceUnitMultipliers, m_distanceUnitDigits, output);
        }

    }
}

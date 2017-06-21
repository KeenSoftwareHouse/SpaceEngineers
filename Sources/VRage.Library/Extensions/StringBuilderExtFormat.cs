/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// File:	StringBuilderExtFormat.cs
// Date:	11th March 2010
// Author:	Gavin Pugh
// Details:	Extension methods for the 'StringBuilder' standard .NET class, to allow garbage-free concatenation of
//			formatted strings with a variable set of arguments.
//
// Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using VRage;

namespace System.Text
{
    public static class StringBuilderExtensions_Format
    {
        public static StringBuilder AppendStringBuilder(this StringBuilder stringBuilder, StringBuilder otherStringBuilder)
        {
            // Prevent multiple reallocations
            stringBuilder.EnsureCapacity(stringBuilder.Length + otherStringBuilder.Length);
            for (int i = 0; i < otherStringBuilder.Length; i++)
            {
                stringBuilder.Append(otherStringBuilder[i]);
            }
            return stringBuilder;
        }


        public static StringBuilder AppendSubstring(this StringBuilder stringBuilder, StringBuilder append, int start, int count)
        {
            // Prevent multiple reallocations
            stringBuilder.EnsureCapacity(stringBuilder.Length + count);
            for (int i = 0; i < count; i++)
            {
                stringBuilder.Append(append[start + i]);
            }
            return stringBuilder;
        }

        //! Concatenate a formatted string with arguments
        public static StringBuilder ConcatFormat<A>(this StringBuilder string_builder, String format_string, A arg1, NumberFormatInfo numberFormat = null)
            where A : IConvertible
        {
            return string_builder.ConcatFormat<A, int, int, int>(format_string, arg1, 0, 0, 0, numberFormat);
        }

        //! Concatenate a formatted string with arguments
        public static StringBuilder ConcatFormat<A, B>(this StringBuilder string_builder, String format_string, A arg1, B arg2, NumberFormatInfo numberFormat = null)
            where A : IConvertible
            where B : IConvertible
        {
            return string_builder.ConcatFormat<A, B, int, int>(format_string, arg1, arg2, 0, 0, numberFormat);
        }

        //! Concatenate a formatted string with arguments
        public static StringBuilder ConcatFormat<A, B, C>(this StringBuilder string_builder, String format_string, A arg1, B arg2, C arg3, NumberFormatInfo numberFormat = null)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
        {
            return string_builder.ConcatFormat<A, B, C, int>(format_string, arg1, arg2, arg3, 0, numberFormat);
        }

        //! Concatenate a formatted string with arguments
        public static StringBuilder ConcatFormat<A, B, C, D>(this StringBuilder string_builder, String format_string, A arg1, B arg2, C arg3, D arg4, NumberFormatInfo numberFormat = null)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
            where D : IConvertible
        {
            int verbatim_range_start = 0;
            numberFormat = numberFormat ?? CultureInfo.InvariantCulture.NumberFormat;
            for (int index = 0; index < format_string.Length; index++)
            {
                if (format_string[index] == '{')
                {
                    // Formatting bit now, so make sure the last block of the string is written out verbatim.
                    if (verbatim_range_start < index)
                    {
                        // Write out unformatted string portion
                        string_builder.Append(format_string, verbatim_range_start, index - verbatim_range_start);
                    }

                    uint base_value = 10;
                    uint padding = 0;
                    uint decimal_places = (uint)numberFormat.NumberDecimalDigits; // Default decimal places in .NET libs
                    bool thousandSeparator = !String.IsNullOrEmpty(numberFormat.NumberGroupSeparator);

                    index++;
                    char format_char = format_string[index];
                    if (format_char == '{')
                    {
                        string_builder.Append('{');
                        index++;
                    }
                    else
                    {
                        index++;

                        if (format_string[index] == ':')
                        {
                            // Extra formatting. This is a crude first pass proof-of-concept. It's not meant to cover
                            // comprehensively what the .NET standard library Format() can do.
                            index++;

                            // Deal with padding
                            while (format_string[index] == '0')
                            {
                                index++;
                                padding++;
                            }

                            if (format_string[index] == 'X')
                            {
                                index++;

                                // Print in hex
                                base_value = 16;

                                // Specify amount of padding ( "{0:X8}" for example pads hex to eight characters
                                if ((format_string[index] >= '0') && (format_string[index] <= '9'))
                                {
                                    padding = (uint)(format_string[index] - '0');
                                    index++;
                                }
                            }
                            else if (format_string[index] == '.')
                            {
                                index++;

                                // Specify number of decimal places
                                decimal_places = 0;

                                while (format_string[index] == '0')
                                {
                                    index++;
                                    decimal_places++;
                                }
                            }
                        }


                        // Scan through to end bracket
                        while (format_string[index] != '}')
                        {
                            index++;
                        }

#if UNSHARPER
			Debug.Assert(false);
#else
                        // Have any extended settings now, so just print out the particular argument they wanted
                        switch (format_char)
                        {
                            case '0': string_builder.ConcatFormatValue<A>(arg1, padding, base_value, decimal_places, thousandSeparator); break;
                            case '1': string_builder.ConcatFormatValue<B>(arg2, padding, base_value, decimal_places, thousandSeparator); break;
                            case '2': string_builder.ConcatFormatValue<C>(arg3, padding, base_value, decimal_places, thousandSeparator); break;
                            case '3': string_builder.ConcatFormatValue<D>(arg4, padding, base_value, decimal_places, thousandSeparator); break;
                            default: Debug.Assert(false, "Invalid parameter index"); break;
                        }
#endif
					}

                    // Update the verbatim range, start of a new section now
                    verbatim_range_start = (index + 1);
                }
            }

            // Anything verbatim to write out?
            if (verbatim_range_start < format_string.Length)
            {
                // Write out unformatted string portion
                string_builder.Append(format_string, verbatim_range_start, format_string.Length - verbatim_range_start);
            }

            return string_builder;
        }

        //! The worker method. This does a garbage-free conversion of a generic type, and uses the garbage-free Concat() to add to the stringbuilder
        private static void ConcatFormatValue<T>(this StringBuilder string_builder, T arg, uint padding, uint base_value, uint decimal_places, bool thousandSeparation) where T : IConvertible
        {
            switch (arg.GetTypeCode())
            {
                case System.TypeCode.Boolean:
                    {
                        if (arg.ToBoolean(CultureInfo.InvariantCulture))
                            string_builder.Append("true");
                        else
                            string_builder.Append("false");
                        break;
                    }

                case System.TypeCode.UInt32:
                    {
                        string_builder.Concat(arg.ToUInt32(NumberFormatInfo.InvariantInfo), padding, '0', base_value, thousandSeparation);
                        break;
                    }

                case System.TypeCode.Int32:
                    {
                        string_builder.Concat(arg.ToInt32(NumberFormatInfo.InvariantInfo), padding, '0', base_value, thousandSeparation);
                        break;
                    }

                case System.TypeCode.Int64:
                    {
                        string_builder.Concat(arg.ToInt64(NumberFormatInfo.InvariantInfo), padding, '0', base_value, thousandSeparation);
                        break;
                    }

                case System.TypeCode.UInt64:
                    {
                        string_builder.Concat(arg.ToInt32(NumberFormatInfo.InvariantInfo), padding, '0', base_value, thousandSeparation);
                        break;
                    }

                case System.TypeCode.Single:
                    {
                        string_builder.Concat(arg.ToSingle(NumberFormatInfo.InvariantInfo), decimal_places, padding, '0', false);
                        break;
                    }

                case System.TypeCode.Double:
                    {
                        string_builder.Concat(arg.ToDouble(NumberFormatInfo.InvariantInfo), decimal_places, padding, '0', false);
                        break;
                    }

                case System.TypeCode.Decimal:
                    {
                        string_builder.Concat(arg.ToSingle(NumberFormatInfo.InvariantInfo), decimal_places, padding, '0', false);
                        break;
                    }

                case System.TypeCode.String:
                    {
                        string_builder.Append(arg.ToString());
                        break;
                    }

                default:
                    {
                        Debug.Assert(false, "Unknown parameter type");
                        break;
                    }
            }
        }

        //! Change all characters to upper case without allocation and return this instance.
        public static StringBuilder ToUpper(this StringBuilder self)
        {
            for (int i = 0; i < self.Length; ++i)
                self[i] = Char.ToUpper(self[i]);
            return self;
        }

        //! Change all characters to lower case without allocation and return this instance.
        public static StringBuilder ToLower(this StringBuilder self)
        {
            for (int i = 0; i < self.Length; ++i)
                self[i] = Char.ToLower(self[i]);
            return self;
        }

        //! Change first characters to upper case without allocation and return this instance.
        public static StringBuilder FirstLetterUpperCase(this StringBuilder self)
        {
            if (self.Length > 0)
                self[0] = Char.ToUpper(self[0]);
            return self;
        }
    }
}

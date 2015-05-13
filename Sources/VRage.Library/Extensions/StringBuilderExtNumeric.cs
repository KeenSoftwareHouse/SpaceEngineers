/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// File:	StringBuilderExtNumeric.cs
// Date:	9th March 2010
// Author:	Gavin Pugh
// Details:	Extension methods for the 'StringBuilder' standard .NET class, to allow garbage-free concatenation of
//			a selection of simple numeric types.  
//
// Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System.Text
{
    using System;
    using System.Globalization;

    public static partial class StringBuilderExtensions
    {
        // These digits are here in a static array to support hex with simple, easily-understandable code. 
        // Since A-Z don't sit next to 0-9 in the ascii table.
        private static readonly char[] ms_digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static readonly uint ms_default_decimal_places = 5; //< Matches standard .NET formatting dp's
        private static readonly char ms_default_pad_char = '0';

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat(this StringBuilder string_builder, ulong uint_val, uint pad_amount, char pad_char, uint base_val, bool thousandSeparation)
        {
            Debug.Assert(pad_amount >= 0);
            Debug.Assert(base_val > 0 && base_val <= 16);

            // Calculate length of integer when written out
            uint length = 0;
            ulong length_calc = uint_val;

            int thousandsCounter = 0;

            do
            {
                thousandsCounter++;
                if (thousandSeparation && thousandsCounter % 4 == 0)
                {
                    length++;
                    continue;
                }

                length_calc /= base_val;
                length++;
            }
            while (length_calc > 0);

            // Pad out space for writing.
            string_builder.Append(pad_char, (int)Math.Max(pad_amount, length));

            int strpos = string_builder.Length;

            thousandsCounter = 0;

            // We're writing backwards, one character at a time.
            while (length > 0)
            {
                strpos--;
                thousandsCounter++;

                if (thousandSeparation && thousandsCounter % 4 == 0)
                {
                    length--;
                    string_builder[strpos] = NumberFormatInfo.InvariantInfo.NumberGroupSeparator[0];
                    continue;
                }

                // Lookup from static char array, to cover hex values too
                string_builder[strpos] = ms_digits[uint_val % base_val];

                uint_val /= base_val;
                length--;
            }

            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val)
        {
            string_builder.Concat(uint_val, 0, ms_default_pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val, uint pad_amount)
        {
            string_builder.Concat(uint_val, pad_amount, ms_default_pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val, uint pad_amount, char pad_char)
        {
            string_builder.Concat(uint_val, pad_amount, pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount, char pad_char, uint base_val, bool thousandSeparation)
        {
            Debug.Assert(pad_amount >= 0);
            Debug.Assert(base_val > 0 && base_val <= 16);

            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Append('-');
                uint uint_val = uint.MaxValue - ((uint)int_val) + 1; //< This is to deal with Int32.MinValue
                string_builder.Concat(uint_val, pad_amount, pad_char, base_val, thousandSeparation);
            }
            else
            {
                string_builder.Concat((uint)int_val, pad_amount, pad_char, base_val, thousandSeparation);
            }

            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat(this StringBuilder string_builder, long long_val, uint pad_amount, char pad_char, uint base_val, bool thousandSeparation)
        {
            Debug.Assert(pad_amount >= 0);
            Debug.Assert(base_val > 0 && base_val <= 16);

            // Deal with negative numbers
            if (long_val < 0)
            {
                string_builder.Append('-');
                ulong ulong_val = ulong.MaxValue - ((ulong)long_val) + 1; //< This is to deal with Int32.MinValue
                string_builder.Concat(ulong_val, pad_amount, pad_char, base_val, thousandSeparation);
            }
            else
            {
                string_builder.Concat((ulong)long_val, pad_amount, pad_char, base_val, thousandSeparation);
            }
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, int int_val)
        {
            string_builder.Concat(int_val, 0, ms_default_pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount)
        {
            string_builder.Concat(int_val, pad_amount, ms_default_pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount, char pad_char)
        {
            string_builder.Concat(int_val, pad_amount, pad_char, 10, true);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder
        public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places, uint pad_amount, char pad_char, bool thousandSeparator)
        {
            Debug.Assert(pad_amount >= 0);

            if (decimal_places == 0)
            {
                // No decimal places, just round up and print it as an int

                // Agh, Math.Floor() just works on doubles/decimals. Don't want to cast! Let's do this the old-fashioned way.
                long int_val;
                if (float_val >= 0.0f)
                {
                    // Round up
                    int_val = (long)(float_val + 0.5f);
                }
                else
                {
                    // Round down for negative numbers
                    int_val = (long)(float_val - 0.5f);
                }
                string_builder.Concat(int_val, pad_amount, pad_char, 10, thousandSeparator);
            }
            else
            {
                float add = 0.5f;
                for (int i = 0; i < decimal_places; i++)
                {
                    add *= 0.1f;
                }
                float_val += float_val >= 0 ? add : -add;

                int int_part = (int)float_val;

                if (int_part == 0 && float_val < 0)
                    string_builder.Append('-');

                // First part is easy, just cast to an integer
                string_builder.Concat(int_part, pad_amount, pad_char, 10, thousandSeparator);

                // Decimal point
                string_builder.Append('.');

                // Work out remainder we need to print after the d.p.
                float remainder = Math.Abs(float_val - int_part);
                uint tmp = decimal_places;

                // Multiply up to become an int that we can print
                do
                {
                    remainder *= 10;
                    tmp--;
                }
                while (tmp > 0);

                // Round up. It's guaranteed to be a positive number, so no extra work required here.
                //remainder += 0.5f;

                // All done, print that as an int!
                string_builder.Concat((uint)remainder, decimal_places, '0', 10, thousandSeparator);
            }
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, float float_val)
        {
            string_builder.Concat(float_val, ms_default_decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places)
        {
            string_builder.Concat(float_val, decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places, uint pad_amount)
        {
            string_builder.Concat(float_val, decimal_places, pad_amount, ms_default_pad_char, false);
            return string_builder;
        }



        //! Convert a given float value to a string and concatenate onto the stringbuilder
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places, uint pad_amount, char pad_char, bool thousandSeparator)
        {
            Debug.Assert(pad_amount >= 0);

            if (decimal_places == 0)
            {
                // No decimal places, just round up and print it as an int

                // Agh, Math.Floor() just works on doubles/decimals. Don't want to cast! Let's do this the old-fashioned way.
                long int_val;
                if (double_val >= 0.0)
                {
                    // Round up
                    int_val = (long)(double_val + 0.5);
                }
                else
                {
                    // Round down for negative numbers
                    int_val = (long)(double_val - 0.5);
                }
                string_builder.Concat(int_val, pad_amount, pad_char, 10, thousandSeparator);
            }
            else
            {
                double add = 0.5;
                for (int i = 0; i < decimal_places; i++)
                {
                    add *= 0.1f;
                }
                double_val += double_val >= 0 ? add : -add;

                int int_part = (int)double_val;

                if (int_part == 0 && double_val < 0)
                    string_builder.Append('-');

                // First part is easy, just cast to an integer
                string_builder.Concat(int_part, pad_amount, pad_char, 10, thousandSeparator);

                // Decimal point
                string_builder.Append('.');

                // Work out remainder we need to print after the d.p.
                double remainder = Math.Abs(double_val - int_part);
                uint tmp = decimal_places;

                // Multiply up to become an int that we can print
                do
                {
                    remainder *= 10;
                    tmp--;
                }
                while (tmp > 0);

                // Round up. It's guaranteed to be a positive number, so no extra work required here.
                //remainder += 0.5f;

                // All done, print that as an int!
                string_builder.Concat((uint)remainder, decimal_places, '0', 10, thousandSeparator);
            }
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val)
        {
            string_builder.Concat(double_val, ms_default_decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places)
        {
            string_builder.Concat(double_val, decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places, uint pad_amount)
        {
            string_builder.Concat(double_val, decimal_places, pad_amount, ms_default_pad_char, false);
            return string_builder;
        }
    }
}

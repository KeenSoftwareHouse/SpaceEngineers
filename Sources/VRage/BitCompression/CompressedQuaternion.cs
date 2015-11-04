using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;

namespace VRage.BitCompression
{
    public static class CompressedQuaternion
    {
        const int bits = 9; // 9 bits per component
        const int bitSize = 2 + bits * 3; // 29 bits

        const int max_value = (1 << bits) - 1;
        const float minimum = -1.0f / 1.414214f; // 1.0f / sqrt(2)
        const float maximum = +1.0f / 1.414214f;
        const float scale = (float)((1 << bits) - 1);
        const float inverse_scale = 1.0f / scale;

        public static void Write(BitStream stream, Quaternion q)
        {
            float x = q.X;
            float y = q.Y;
            float z = q.Z;
            float w = q.W;

            float abs_x = Math.Abs(x);
            float abs_y = Math.Abs(y);
            float abs_z = Math.Abs(z);
            float abs_w = Math.Abs(w);

            uint largest = 0;
            float largest_value = abs_x;

            if (abs_y > largest_value)
            {
                largest = 1;
                largest_value = abs_y;
            }

            if (abs_z > largest_value)
            {
                largest = 2;
                largest_value = abs_z;
            }

            if (abs_w > largest_value)
            {
                largest = 3;
                largest_value = abs_w;
            }

            float a = 0;
            float b = 0;
            float c = 0;

            switch (largest)
            {
                case 0:
                    if (x >= 0)
                    {
                        a = y;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -y;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 1:
                    if (y >= 0)
                    {
                        a = x;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 2:
                    if (z >= 0)
                    {
                        a = x;
                        b = y;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -w;
                    }
                    break;

                case 3:
                    if (w >= 0)
                    {
                        a = x;
                        b = y;
                        c = z;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -z;
                    }
                    break;

                default:
                    Debug.Fail("Error");
                    break;
            }

            float normal_a = (a - minimum) / (maximum - minimum);
            float normal_b = (b - minimum) / (maximum - minimum);
            float normal_c = (c - minimum) / (maximum - minimum);

            stream.WriteUInt32(largest, 2);
            stream.WriteUInt32((uint)Math.Floor(normal_a * scale + 0.5f), bits);
            stream.WriteUInt32((uint)Math.Floor(normal_b * scale + 0.5f), bits);
            stream.WriteUInt32((uint)Math.Floor(normal_c * scale + 0.5f), bits);
        }

        public static Quaternion Read(BitStream stream)
        {
            float x, y, z, w;

            // note: you're going to want to normalize the quaternion returned from this function
            uint largest = stream.ReadUInt32(2);
            float a = stream.ReadUInt32(bits) * inverse_scale * (maximum - minimum) + minimum;
            float b = stream.ReadUInt32(bits) * inverse_scale * (maximum - minimum) + minimum;
            float c = stream.ReadUInt32(bits) * inverse_scale * (maximum - minimum) + minimum;

            switch (largest)
            {
                case 0:
                    x = (float)Math.Sqrt(1 - a * a - b * b - c * c);
                    y = a;
                    z = b;
                    w = c;
                    break;

                case 1:
                    x = a;
                    y = (float)Math.Sqrt(1 - a * a - b * b - c * c);
                    z = b;
                    w = c;
                    break;

                case 2:
                    x = a;
                    y = b;
                    z = (float)Math.Sqrt(1 - a * a - b * b - c * c);
                    w = c;
                    break;

                case 3:
                    x = a;
                    y = b;
                    z = c;
                    w = (float)Math.Sqrt(1 - a * a - b * b - c * c);
                    break;

                default:
                    Debug.Fail("Error");
                    x = 0;
                    y = 0;
                    z = 0;
                    w = 1;
                    break;
            }
            return Quaternion.Normalize(new Quaternion(x, y, z, w));
        }
    }
}
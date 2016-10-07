#if !XB1

using System.Runtime.InteropServices;
using System.Security;

namespace VRage.Input
{
    static class MyWindowsKeyboard
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern unsafe bool GetKeyboardState(byte* data);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int keyCode);


        public static MyKeyboardState GetCurrentState()
        {
            MyKeyboardBuffer buffer = new MyKeyboardBuffer();
            // Because GetKeyboardState cannot read from different than main thread
                /*
            unsafe
            {
                byte* keyData = stackalloc byte[256];
           
                if (!GetKeyboardState(keyData))
                {
                    throw new InvalidOperationException("Could not read keyboard");
                } 


                CopyBuffer(keyData, ref buffer);
            }     */

            for (int i = 0; i < 256; i++)
            {
                if ((((ushort)GetAsyncKeyState(i)) >> 15) != 0)
                {
                    buffer.SetBit((byte)i, true);
                }
            }

            return MyKeyboardState.FromBuffer(buffer);
        }

        static unsafe void CopyBuffer(byte* windowsKeyData, ref MyKeyboardBuffer buffer)
        {
            for (int i = 0; i < 256; i++)
            {
                if ((windowsKeyData[i] & 0x80) != 0)
                {
                    buffer.SetBit((byte)i, true);
                }
            }
        }
    }
}

#endif
#if !XB1

using System.Collections.Generic;

namespace VRage.Input
{
    public struct MyKeyboardState
    {   
        MyKeyboardBuffer m_buffer;

        public void GetPressedKeys(List<MyKeys> keys)
        {
            keys.Clear();

            for (int i = 1; i < 255; i++)
            {
                if (m_buffer.GetBit((byte)i))
                    keys.Add((MyKeys)i);
            }
        }

        public bool IsAnyKeyPressed()
        {
            return m_buffer.AnyBitSet();
        }
        
        public void SetKey(MyKeys key, bool value)
        {
            m_buffer.SetBit((byte)key, value);
        }

        public static MyKeyboardState FromBuffer(MyKeyboardBuffer buffer)
        {
            return new MyKeyboardState() { m_buffer = buffer };
        }

        public bool IsKeyDown(MyKeys key)
        {
            return m_buffer.GetBit((byte)key);
        }

        public bool IsKeyUp(MyKeys key)
        {
            return !IsKeyDown(key);
        }
    }
}

#endif

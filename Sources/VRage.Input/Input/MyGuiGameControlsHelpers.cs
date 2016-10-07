using System.Collections.Generic;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Input
{
    public static class MyGuiGameControlsHelpers
    {
        static Dictionary<MyStringId, MyGuiDescriptor> m_gameControlHelpers = new Dictionary<MyStringId, MyGuiDescriptor>(MyStringId.Comparer);

        static MyGuiGameControlsHelpers()
        {
            MyLog.Default.WriteLine("MyGuiGameControlsHelpers()");
        }

        public static MyGuiDescriptor GetGameControlHelper(MyStringId controlHelper)
        {
            MyGuiDescriptor ret;
            if (m_gameControlHelpers.TryGetValue(controlHelper, out ret))
                return ret;
            else
                return null;
        }

        public static void Add(MyStringId control, MyGuiDescriptor descriptor)
        {
            m_gameControlHelpers.Add(control, descriptor);
        }

        public static void Reset()
        {
            m_gameControlHelpers.Clear();
        }
    }
}

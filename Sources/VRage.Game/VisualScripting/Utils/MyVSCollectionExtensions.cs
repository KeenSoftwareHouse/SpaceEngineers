using System.Collections.Generic;

namespace VRage.Game.VisualScripting.Utils
{
    public static class MyVSCollectionExtensions
    {
        [VisualScriptingMember]
        public static T At<T>(this List<T> list, int index) 
        {
            if (index < list.Count)
                return list[index];

            return default(T);
        }

        [VisualScriptingMember]
        public static int Count<T>(this List<T> list)
        {
            return list.Count;
        }
    }
}

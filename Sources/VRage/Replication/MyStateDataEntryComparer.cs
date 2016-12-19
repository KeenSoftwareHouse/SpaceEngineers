using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MyStateDataEntryComparer : IComparer<MyStateDataEntry>
    {
        public static readonly MyStateDataEntryComparer Instance = new MyStateDataEntryComparer();

        private MyStateDataEntryComparer()
        {
        }

        public int Compare(MyStateDataEntry x, MyStateDataEntry y)
        {
            int group = x.Group.GroupType.CompareTo(y.Group.GroupType);
            if (group == 0)
            {
                return y.Priority.CompareTo(x.Priority);
            }
            return group;
        }
    }
}

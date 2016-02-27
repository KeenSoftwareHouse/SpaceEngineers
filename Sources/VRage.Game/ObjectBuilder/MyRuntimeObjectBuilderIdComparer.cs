using System.Collections.Generic;

namespace VRage.ObjectBuilders
{
    public class MyRuntimeObjectBuilderIdComparer : IComparer<MyRuntimeObjectBuilderId>, IEqualityComparer<MyRuntimeObjectBuilderId>
    {
        public int Compare(MyRuntimeObjectBuilderId x, MyRuntimeObjectBuilderId y)
        {
            return x.Value - y.Value;
        }

        public bool Equals(MyRuntimeObjectBuilderId x, MyRuntimeObjectBuilderId y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode(MyRuntimeObjectBuilderId obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}

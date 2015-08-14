using System;

namespace VRage.Input
{
    public class MyInput
    {
        public static IMyInput Static
        {
            get;
            private set;
        }

        public static void Initialize(IMyInput implementation)
        {
            if (Static != null)
                throw new InvalidOperationException("Input already initialized.");
            Static = implementation;
            //Static = isNull ? new MyNullInput() as IMyInput
            //                : new MyDirectXInput(textInputBuffer, nameLookup, defaultGameControls, enableDevKeys) as IMyInput;
        }

        public static void UnloadData()
        {
            if (Static != null)
            {
                Static.UnloadData();
                Static = null;
            }
        }
    }
}

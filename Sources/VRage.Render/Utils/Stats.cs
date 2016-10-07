using VRage.Stats;

namespace VRageRender.Utils
{
    public static class Stats
    {
        public static readonly MyStats Timing;
        public static readonly MyStats Generic;
        public static readonly MyStats Network;

        static Stats()
        {
            Timing = new MyStats();
            Generic = MyRenderStats.Generic;
            Network = new MyStats();

            MyRenderStats.SetColumn(MyRenderStats.ColumnEnum.Left, Timing, Generic, Network);
        }

        // Todo: Support for counters that are incremented by any value and never reset.
        // Use MyStatTypeEnum.Sum, numDecimals = 0, clearRate = int.MaxValue
        public struct MyPerAppLifetime
        {
            public int MyModelsCount;
            public int MyModelsMeshesCount;
            public int MyModelsVertexesCount;
            public int MyModelsTrianglesCount;
        }
        public static MyPerAppLifetime PerAppLifetime;

    }
}

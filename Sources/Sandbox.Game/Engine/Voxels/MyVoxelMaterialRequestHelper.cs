using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Voxels
{
    class MyVoxelMaterialRequestHelper
    {
        [ThreadStatic]
        public static bool WantsOcclusion;

        [ThreadStatic]
        public static bool IsContouring;

        public struct ContouringFlagsProxy : IDisposable
        {
            private bool oldState;

            public void Dispose()
            {
                WantsOcclusion = false;
                IsContouring = false;
            }
        }

        public static ContouringFlagsProxy StartContouring()
        {
            WantsOcclusion = true;
            IsContouring = true;
            return new ContouringFlagsProxy();
        }
    }
}

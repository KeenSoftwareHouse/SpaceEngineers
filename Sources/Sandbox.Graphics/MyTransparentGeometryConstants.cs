using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox
{
    static class MyTransparentGeometryConstants
    {
        public const int MAX_TRANSPARENT_GEOMETRY_COUNT = 50000;

        public const int MAX_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.05f);
        public const int MAX_NEW_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.7f);
    }

}

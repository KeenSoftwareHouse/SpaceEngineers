using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Generics;

namespace VRageRender
{
    public class MyBillboardBatch<T> where T : MyBillboard, new()
    {
        public readonly List<T> Billboards = new List<T>(16384);
        public readonly MyObjectsPoolSimple<T> Pool = new MyObjectsPoolSimple<T>(MyTransparentGeometryConstants.MAX_TRANSPARENT_GEOMETRY_COUNT / 2);
        public readonly Dictionary<int, MyBillboardViewProjection> Matrices = new Dictionary<int, MyBillboardViewProjection>(10);

        public void Clear()
        {
            Billboards.Clear();
            Pool.ClearAllAllocated();
        }
    }
}

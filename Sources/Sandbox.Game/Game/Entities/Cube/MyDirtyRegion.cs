using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public class MyDirtyRegion
    {
        public HashSet<Vector3I> Cubes = new HashSet<Vector3I>();


        public void AddCube(Vector3I pos)
        {
            Cubes.Add(pos);
        }

        /// <summary>
        /// Adds dirty region, min and max are inclusive
        /// </summary>
        public void AddCubeRegion(Vector3I min, Vector3I max)
        {
            Vector3I pos;
            for (pos.X = min.X; pos.X <= max.X; pos.X++)
            {
                for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
                {
                    for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                    {
                        Cubes.Add(pos);
                    }
                }
            }
        }

        public bool IsDirty
        {
            get { return Cubes.Count > 0; }
        }

        public void Clear()
        {
            Cubes.Clear();
        }
    }
}

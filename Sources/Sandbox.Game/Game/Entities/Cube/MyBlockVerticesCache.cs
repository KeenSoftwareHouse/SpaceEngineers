using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Collections;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    class MyBlockVerticesCache
    {
        const bool ADD_INNER_BONES_TO_CONVEX = true;
        private static List<Vector3>[][] Cache;

        static MyBlockVerticesCache()
        {
            GenerateConvexVertices();
        }

        public static ListReader<Vector3> GetBlockVertices(MyCubeTopology topology, MyBlockOrientation orientation)
        {
            var list = Cache[(int) topology][(int) orientation.Forward*6 + (int) orientation.Up];
            Debug.Assert(list != null, "Unknown topology");
            return new ListReader<Vector3>(list);
        }

        static void GenerateConvexVertices()
        {
            List<Vector3> tmpHelperVerts = new List<Vector3>(27);
            var topologyValues = Enum.GetValues(typeof(MyCubeTopology));
            Cache = new List<Vector3>[topologyValues.Length][];
            foreach (var topologyObj in topologyValues)
            {
                var topology = (MyCubeTopology) topologyObj;
                GetTopologySwitch(topology, tmpHelperVerts);

                Cache[(int)topology] = new List<Vector3>[6*6];
                foreach (var forward in Base6Directions.EnumDirections)
                {
                    foreach (var up in Base6Directions.EnumDirections)
                    {
                        if(forward == up || Base6Directions.GetIntVector(forward) == -Base6Directions.GetIntVector(up))
                            continue;
                        var list = new List<Vector3>(tmpHelperVerts.Count);
                        Cache[(int) topology][(int) forward*6 + (int) up] = list;

                        var orientation = new MyBlockOrientation(forward, up);
                        foreach (var vert in tmpHelperVerts)
                        {
                            list.Add(Vector3.TransformNormal(vert, orientation));
                        }
                    }
                }

                tmpHelperVerts.Clear();
            }
        }

        private static void GetTopologySwitch(MyCubeTopology topology, List<Vector3> verts)
        {
            switch (topology)
            {
                case MyCubeTopology.Slope:
                case MyCubeTopology.RotatedSlope:
                    // Main 6 corners
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, -1, 0));

                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, -1, 0));

                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, -1, 0));
                    }
                    break;
                case MyCubeTopology.RoundSlope:
                    // Main 6 corners
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, -1));

                    //Slope points
                    verts.Add(new Vector3(-1f, 0.414f, 0.414f));
                    verts.Add(new Vector3(1f, 0.414f, 0.414f));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, -1, 0));

                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, -1, 0));

                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, -1, 0));
                    }
                    break;
                case MyCubeTopology.Corner:
                case MyCubeTopology.RotatedCorner:
                    // Main 4 corners
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, -1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, 1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(1, -1, 0));
                        verts.Add(new Vector3(0, -1, -1));

                        // Inner bones (middle)
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(0, 0, -1));
                    }
                    break;
                case MyCubeTopology.RoundCorner:
                    // Main 4 corners
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, -1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, 1));

                    //Slope points
                    verts.Add(new Vector3(-0.414f, 0.414f, -1f));
                    verts.Add(new Vector3(-0.414f, -1f, 0.414f));
                    verts.Add(new Vector3(1f, 0.414f, 0.414f));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(1, -1, 0));
                        verts.Add(new Vector3(0, -1, -1));

                        // Inner bones (middle)
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(0, 0, -1));
                    }
                    break;

                case MyCubeTopology.InvCorner:
                    // Main 7 corners
                    verts.Add(new Vector3(1, 1, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, 1, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 1, 0));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 1, 0));
                        verts.Add(new Vector3(0, 1, 1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 1, 0));
                    }
                    break;
                case MyCubeTopology.RoundInvCorner:
                    // Main 7 corners
                    verts.Add(new Vector3(1, 1, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, 1, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    //Slope points
                    verts.Add(new Vector3(0.414f, -0.414f, -1f));
                    verts.Add(new Vector3(0.414f, -1f, -0.414f));
                    verts.Add(new Vector3(1f, -0.414f, -0.414f));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 1, 0));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 1, 0));
                        verts.Add(new Vector3(0, 1, 1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 1, 0));
                    }
                    break;

                case MyCubeTopology.Box:
                case MyCubeTopology.RoundedSlope:
                    // Main 8 corners
                    verts.Add(new Vector3(1, 1, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(1, -1, -1));
                    verts.Add(new Vector3(-1, 1, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 19 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 1, 0));
                        verts.Add(new Vector3(0, -1, -1));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        //verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 1, 0));
                        verts.Add(new Vector3(0, 1, 1));
                        verts.Add(new Vector3(1, -1, 0));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 1, 0));
                    }
                    break;

                case MyCubeTopology.Slope2Base:
                    // Main 8 corners
                    verts.Add(new Vector3(1, 0, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(1, -1, -1));
                    verts.Add(new Vector3(-1, 0, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 19 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 0.5f, 0));
                        verts.Add(new Vector3(0, -1, -1));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 0.5f, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(1, -1, 0));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 0.5f, 0));
                    }
                    break;

                case MyCubeTopology.Slope2Tip:
                    // Main 6 corners
                    verts.Add(new Vector3(-1, 0, -1));
                    verts.Add(new Vector3(1, 0, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        verts.Add(new Vector3(-1, -0.5f, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, -1, 0));

                        verts.Add(new Vector3(0, -0.5f, 0));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, -1, 0));

                        verts.Add(new Vector3(1, -0.5f, 0));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, -1, 0));
                    }
                    break;
                case MyCubeTopology.Corner2Base:
                    // Main 6 corners
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(1, 0, -1));
                    verts.Add(new Vector3(1, -1, 0));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));
                    verts.Add(new Vector3(1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 9 bones
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, -1, 0));

                        verts.Add(new Vector3(0.5f, -0.5f, 0));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, -1, 0));

                        verts.Add(new Vector3(1, -0.5f, -0.5f));
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, -1, 0));
                    }
                    break;
                case MyCubeTopology.Corner2Tip:
                    // Main 4 corners
                    verts.Add(new Vector3(1, 0, -1));
                    verts.Add(new Vector3(1, -1, -1));
                    verts.Add(new Vector3(0, -1, -1));
                    verts.Add(new Vector3(1, -1, 1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Inner bones (bottom)
                        verts.Add(new Vector3(0.5f, -1, 0));
                        verts.Add(new Vector3(1, -1, 0));
                        verts.Add(new Vector3(0, -1, -1));

                        // Inner bones (middle)
                        verts.Add(new Vector3(1, 0, -1));
                        verts.Add(new Vector3(1, -0.5f, 0));
                        verts.Add(new Vector3(0.5f, -0.5f, -1));
                    }
                    break;
                case MyCubeTopology.InvCorner2Base:
                    verts.Add(new Vector3(1, 1, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(1, 0, -1));
                    verts.Add(new Vector3(0, -1, -1));
                    verts.Add(new Vector3(-1, 1, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 1, 0));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 1, 0));
                        verts.Add(new Vector3(0, 1, 1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 1, 0));
                    }
                    break;

                    break;
                case MyCubeTopology.InvCorner2Tip:
                    verts.Add(new Vector3(1, 1, 1));
                    verts.Add(new Vector3(1, 1, -1));
                    //verts.Add(new Vector3(1, -1, 1));
                    verts.Add(new Vector3(-1, 1, 1));
                    verts.Add(new Vector3(-1, 1, -1));
                    verts.Add(new Vector3(-1, -1, 1));
                    verts.Add(new Vector3(-1, -1, -1));

                    if (ADD_INNER_BONES_TO_CONVEX)
                    {
                        // Other 16 bones
                        verts.Add(new Vector3(-1, -1, 0));
                        verts.Add(new Vector3(-1, 0, -1));
                        verts.Add(new Vector3(-1, 0, 0));
                        verts.Add(new Vector3(-1, 0, 1));
                        verts.Add(new Vector3(-1, 1, 0));
                        verts.Add(new Vector3(0, -1, 0));
                        verts.Add(new Vector3(0, -1, 1));
                        verts.Add(new Vector3(0, 0, -1));
                        verts.Add(new Vector3(0, 0, 0));
                        verts.Add(new Vector3(0, 0, 1));
                        verts.Add(new Vector3(0, 1, -1));
                        verts.Add(new Vector3(0, 1, 0));
                        verts.Add(new Vector3(0, 1, 1));
                        verts.Add(new Vector3(1, 0, 0));
                        verts.Add(new Vector3(1, 0, 1));
                        verts.Add(new Vector3(1, 1, 0));
                    }
                    break;

                    break;
                default:
                    return;
            }
        }
    }
}

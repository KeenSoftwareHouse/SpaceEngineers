using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRageMath;
using VRage;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Runtime.InteropServices;

namespace VRage.Utils
{
    public static partial class MyUtils
    {
        struct Edge : IEquatable<Edge>
        {
            public int I0;
            public int I1;

            public bool Equals(Edge other)
            {
                return Equals(other.GetHashCode(), GetHashCode());
            }

            public override int GetHashCode()
            {
                return I0 < I1 ? (I0.GetHashCode() * 397) ^ I1.GetHashCode() : (I1.GetHashCode() * 397) ^ I0.GetHashCode();
            }
        }

        public static void GetOpenBoundaries(Vector3[] vertices, int[] indices, List<Vector3> openBoundaries)
        {
            System.Diagnostics.Debug.Assert(indices.Length > 0);
            System.Diagnostics.Debug.Assert(indices.Length % 3 == 0);

            Dictionary<int, List<int>> indicesRemap = new Dictionary<int, List<int>>(); //for same vertices
            for (int i = 0; i < vertices.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    if (MyUtils.IsEqual(vertices[j], vertices[i]))
                    {
                        if (!indicesRemap.ContainsKey(j))
                            indicesRemap[j] = new List<int>();

                        indicesRemap[j].Add(i);
                        break;
                    }
                }

            foreach (var pair in indicesRemap)
            {
                foreach (var remapValue in pair.Value)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] == remapValue)
                            indices[i] = pair.Key;
                    }
                }
            }


            Dictionary<Edge, int> edgeCounts = new Dictionary<Edge,int>();

            for (int i = 0; i < indices.Length; i += 3)
            {
                AddEdge(indices[i], indices[i + 1], edgeCounts);
                AddEdge(indices[i + 1], indices[i + 2], edgeCounts);
                AddEdge(indices[i + 2], indices[i], edgeCounts);
            }

            openBoundaries.Clear();
            foreach (var edgeCount in edgeCounts)
            {
                System.Diagnostics.Debug.Assert(edgeCount.Value > 0);

                if (edgeCount.Value == 1)
                {
                    openBoundaries.Add(vertices[edgeCount.Key.I0]);
                    openBoundaries.Add(vertices[edgeCount.Key.I1]);
                }
            }
        }

        static void AddEdge(int i0, int i1, Dictionary<Edge, int> edgeCounts)
        {
            Edge edge = new Edge() { I0 = i0, I1 = i1 };

            System.Diagnostics.Debug.Assert(edge.I0 != edge.I1);

            var hash = edge.GetHashCode();

            if (edgeCounts.ContainsKey(edge))
                edgeCounts[edge] = edgeCounts[edge] + 1;
            else
                edgeCounts[edge] = 1;
        }
    }
}


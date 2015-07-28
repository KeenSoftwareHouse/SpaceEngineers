using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Engine.Voxels
{
    public struct MyCsgShapePlanetMaterialAttributes
    {
        public MyOreProbability[] OreProbabilities;

        public float OreStartDepth;
        public float OreEndDepth;

        public void WriteTo(Stream stream)
        {
            if (OreProbabilities != null)
            {
                stream.WriteNoAlloc(OreProbabilities.Length);
                for (int i = 0; i < OreProbabilities.Length; ++i)
                {
                    stream.WriteNoAlloc(OreProbabilities[i].CummulativeProbability);
                    stream.WriteNoAlloc(OreProbabilities[i].OreName);
                }
            }
            else
            {
                stream.WriteNoAlloc((int)0);
            }

            stream.WriteNoAlloc(OreStartDepth);
            stream.WriteNoAlloc(OreEndDepth);
        }
        public void ReadFrom(Stream stream)
        {         
            int numOreProbabilities = stream.ReadInt32();
            OreProbabilities = new MyOreProbability[numOreProbabilities];
            for (int i = 0; i < numOreProbabilities; ++i)
            {
                OreProbabilities[i] = new MyOreProbability();
                OreProbabilities[i].CummulativeProbability = stream.ReadFloat();
                OreProbabilities[i].OreName = stream.ReadString();
            }

            OreStartDepth = stream.ReadFloat();
            OreEndDepth = stream.ReadFloat();
        }
    }

    public struct MyCsgShapePlanetShapeAttributes
    {
        public int Seed;
        public float Diameter;
        public float Radius;
        public float DeviationScale;
        public float AveragePlanetRadius;

        public void WriteTo(Stream stream)
        {
            stream.WriteNoAlloc(Seed);
            stream.WriteNoAlloc(Radius);
            stream.WriteNoAlloc(DeviationScale);
            stream.WriteNoAlloc(AveragePlanetRadius);
        }
        public void ReadFrom(Stream stream)
        {
            Seed = stream.ReadInt32();
            Radius = stream.ReadFloat();
            DeviationScale = stream.ReadFloat();
            Diameter = Radius * 2.0f;
            AveragePlanetRadius = stream.ReadFloat();
        }
    }
}

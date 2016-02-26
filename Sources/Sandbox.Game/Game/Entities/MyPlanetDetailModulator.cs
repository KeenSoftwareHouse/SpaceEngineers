using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    class MyPlanetDetailModulator : IMyModule
    {
        MyPlanetGeneratorDefinition m_planetDefinition;
        MyPlanetMaterialProvider m_oreDeposit;
        float m_radius;

        //good
        //module = new MyRidgedMultifractalFast(
        //        seed: 13136546,
        //        quality: MyNoiseQuality.Low,
        //        frequency: 2 * 0.09f + 0.11f,
        //        layerCount: 1);
        //module = new MyBillowFast(
        //            seed: 13136546,
        //            quality: MyNoiseQuality.Low,
        //            frequency: 3 * 0.07f + 0.13f,
        //            layerCount: 1);
        //MyModuleFast module = new MyRidgedMultifractalFast(
        //            seed: 13136546,
        //            quality: MyNoiseQuality.Low,
        //            frequency: 75624 * 0.09f + 0.11f,
        //            layerCount: 1);

        struct MyModulatorData
        {
            public float Height;
            public MyModuleFast Modulator;
        }

        Dictionary<byte, MyModulatorData> m_modulators = new Dictionary<byte, MyModulatorData>();

        public MyPlanetDetailModulator(MyPlanetGeneratorDefinition planetDefinition, MyPlanetMaterialProvider oreDeposit, int seed, float radius)
        {
            m_planetDefinition = planetDefinition;
            m_oreDeposit = oreDeposit;
            m_radius = radius;

            foreach (var distortionDefinition in m_planetDefinition.DistortionTable)
            {
                MyModuleFast modulator = null;

                float frequency = distortionDefinition.Frequency;
                frequency *= radius / 6.0f;

                switch (distortionDefinition.Type)
                {
                    case "Billow":
                        {
                            modulator = new MyBillowFast(
                            seed: seed,
                            quality: MyNoiseQuality.High,
                            frequency: frequency,
                            layerCount: distortionDefinition.LayerCount);
                        }
                        break;
                    case "RidgedMultifractal":
                        {
                            modulator = new MyRidgedMultifractalFast(
                            seed: seed,
                            quality: MyNoiseQuality.High,
                            frequency: frequency,
                            layerCount: distortionDefinition.LayerCount);
                        }
                        break;
                    case "Perlin":
                        {
                            modulator = new MyPerlinFast(
                            seed: seed,
                            quality: MyNoiseQuality.High,
                            frequency: frequency,
                            octaveCount: distortionDefinition.LayerCount);
                        }
                        break;
                    case "Simplex":
                        {
                            modulator = new MySimplexFast()
                            {
                                Seed = seed,
                                Frequency = frequency,
                            };
                        }
                        break;

                    default:
                        System.Diagnostics.Debug.Fail("Unknown modulator type!");
                        break;
                }

                if (modulator != null)
                {
                    m_modulators.Add(distortionDefinition.Value, 
                        new MyModulatorData()
                        {
                            Height = distortionDefinition.Height,
                            Modulator = modulator
                        }                        
                        );
                }
            }
        }

        public double GetValue(double x)
        {
            return 0;
        }

        public double GetValue(double x, double y)
        {
            return 0;
        }

        public double GetValue(double x, double y, double z)
        {
            /*Vector3 samplePos = new Vector3(x, y, z);
            MyFormatRGBA8 material = m_oreDeposit.GetMaterialDataForTexcoord(ref samplePos);

            if (material.GetDistortValue() == 0)
                return 0;

            MyModulatorData modulatorData;
            if (m_modulators.TryGetValue(material.GetDistortValue(), out modulatorData))
            {
                return modulatorData.Height * modulatorData.Modulator.GetValue(x, y, z);
            }*/

            return 0;
        }
    }
}

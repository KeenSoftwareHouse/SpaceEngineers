using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace SpaceEngineers.Game.World.Generator
{
    [PreloadRequired]
    class MySpaceWorldGenerator
    {
        public static Dictionary<string, Vector3> m_compatStaticAsteroids = new Dictionary<string, Vector3>(128);

        static MySpaceWorldGenerator()
        {
            m_compatStaticAsteroids.Add("centralAsteroid", new Vector3(-467.79776, -344.905579, -422.073059));
            m_compatStaticAsteroids.Add("centralAsteroidmoon0", new Vector3(-76.98491, -131.79158, -24.2211456));
            m_compatStaticAsteroids.Add("centralAsteroidmoon1", new Vector3(-101.686935, -118.12616, -210.164291));
            m_compatStaticAsteroids.Add("centralAsteroidmoon2", new Vector3(-256.495361, 38.2605057, -183.8953));
            m_compatStaticAsteroids.Add("centralAsteroidmoon3", new Vector3(-56.29669, -140.993454, -76.5553));
            m_compatStaticAsteroids.Add("centralAsteroidmoon4", new Vector3(-246.923538, 87.1476, -112.943687));
            m_compatStaticAsteroids.Add("asteroid0", new Vector3(1510.89124, -465.1121, 260.685));
            m_compatStaticAsteroids.Add("asteroid0moon0", new Vector3(1662.6283, -162.567474, 359.632416));
            m_compatStaticAsteroids.Add("asteroid0moon1", new Vector3(1748.95581, -338.9174, 326.005432));
            m_compatStaticAsteroids.Add("asteroid0moon2", new Vector3(1746.39221, -287.431152, 595.522766));
            m_compatStaticAsteroids.Add("asteroid0moon3", new Vector3(1698.522, -135.195282, 511.5056));
            m_compatStaticAsteroids.Add("asteroid0moon4", new Vector3(1828.02527, -266.84, 370.210022));
            m_compatStaticAsteroids.Add("asteroid1", new Vector3(868.609863, -770.4491, -688.579956));
            m_compatStaticAsteroids.Add("asteroid1moon0", new Vector3(1155.94092, -630.9224, -408.786469));
            m_compatStaticAsteroids.Add("asteroid1moon1", new Vector3(1201.52026, -692.583, -447.062317));
            m_compatStaticAsteroids.Add("asteroid1moon2", new Vector3(1202.91467, -715.0832, -543.2147));
            m_compatStaticAsteroids.Add("asteroid1moon3", new Vector3(1124.04163, -583.4918, -554.5276));
            m_compatStaticAsteroids.Add("asteroid1moon4", new Vector3(1209.46143, -724.96875, -496.102417));
            m_compatStaticAsteroids.Add("asteroid2", new Vector3(-868.6959, -717.699, 709.567261));
            m_compatStaticAsteroids.Add("asteroid2moon0", new Vector3(-740.1664, -766.9268, 976.74));
            m_compatStaticAsteroids.Add("asteroid2moon1", new Vector3(-808.041138, -625.8883, 968.462769));
            m_compatStaticAsteroids.Add("asteroid2moon2", new Vector3(-602.8258, -688.6499, 1027.35132));
            m_compatStaticAsteroids.Add("asteroid2moon3", new Vector3(-629.0064, -643.9914, 759.6818));
            m_compatStaticAsteroids.Add("asteroid2moon4", new Vector3(-629.370056, -791.147339, 864.577942));
            m_compatStaticAsteroids.Add("asteroid3", new Vector3(29.2436523, -1853.01147, 158.969727));
            m_compatStaticAsteroids.Add("asteroid3moon0", new Vector3(215.777008, -1559.10168, 460.8993));
            m_compatStaticAsteroids.Add("asteroid3moon1", new Vector3(350.500336, -1600.10486, 305.328857));
            m_compatStaticAsteroids.Add("asteroid3moon2", new Vector3(88.53842, -1598.97961, 318.699341));
            m_compatStaticAsteroids.Add("asteroid3moon3", new Vector3(287.0002, -1793.83484, 327.860352));
            m_compatStaticAsteroids.Add("asteroid3moon4", new Vector3(334.049744, -1568.85254, 315.171417));
            m_compatStaticAsteroids.Add("asteroid4", new Vector3(-1011.94885, -688.988464, 1573.34753));
            m_compatStaticAsteroids.Add("asteroid4moon0", new Vector3(-948.649658, -471.660126, 1692.58374));
            m_compatStaticAsteroids.Add("asteroid4moon1", new Vector3(-952.0157, -456.203979, 1823.614));
            m_compatStaticAsteroids.Add("asteroid4moon2", new Vector3(-943.316467, -517.9845, 1848.04944));
            m_compatStaticAsteroids.Add("asteroid4moon3", new Vector3(-948.6832, -429.600525, 1802.581));
            m_compatStaticAsteroids.Add("asteroid4moon4", new Vector3(-700.344849, -416.96402, 1723.02759));
            m_compatStaticAsteroids.Add("asteroid5", new Vector3(-408.154449, 1190.29834, 527.698364));
            m_compatStaticAsteroids.Add("asteroid5moon0", new Vector3(-300.982117, 1336.61035, 627.345764));
            m_compatStaticAsteroids.Add("asteroid5moon1", new Vector3(-137.878265, 1134.39661, 675.0172));
            m_compatStaticAsteroids.Add("asteroid5moon2", new Vector3(-252.941559, 1259.60791, 865.0205));
            m_compatStaticAsteroids.Add("asteroid5moon3", new Vector3(-77.54451, 1307.79578, 699.0752));
            m_compatStaticAsteroids.Add("asteroid5moon4", new Vector3(-208.0395, 1260.0542, 869.368042));
            m_compatStaticAsteroids.Add("asteroid6", new Vector3(-619.9507, -1807.48132, 1206.3573));
            m_compatStaticAsteroids.Add("asteroid6moon0", new Vector3(-410.582458, -1490.075, 1317.91125));
            m_compatStaticAsteroids.Add("asteroid6moon1", new Vector3(-359.675537, -1510.88892, 1481.41711));
            m_compatStaticAsteroids.Add("asteroid6moon2", new Vector3(-550.6644, -1589.871, 1480.7312));
            m_compatStaticAsteroids.Add("asteroid6moon3", new Vector3(-567.8957, -1562.03906, 1406.0592));
            m_compatStaticAsteroids.Add("asteroid6moon4", new Vector3(-410.3158, -1758.13623, 1355.47876));
            m_compatStaticAsteroids.Add("asteroid7", new Vector3(-674.2594, -493.379578, -1447.60425));
            m_compatStaticAsteroids.Add("asteroid7moon0", new Vector3(-623.499268, -415.767761, -1304.246));
            m_compatStaticAsteroids.Add("asteroid7moon1", new Vector3(-554.9427, -385.733765, -1379.34668));
            m_compatStaticAsteroids.Add("asteroid7moon2", new Vector3(-568.8472, -307.443756, -1244.019));
            m_compatStaticAsteroids.Add("asteroid7moon3", new Vector3(-373.1848, -331.604126, -1223.31348));
            m_compatStaticAsteroids.Add("asteroid7moon4", new Vector3(-618.0778, -367.864777, -1239.19421));
            m_compatStaticAsteroids.Add("asteroid8", new Vector3(-477.607819, 366.410522, -803.1929));
            m_compatStaticAsteroids.Add("asteroid8moon0", new Vector3(-310.096741, 704.0997, -637.173));
            m_compatStaticAsteroids.Add("asteroid8moon1", new Vector3(-255.818344, 705.065735, -621.4319));
            m_compatStaticAsteroids.Add("asteroid8moon2", new Vector3(-238.096176, 564.4208, -469.0432));
            m_compatStaticAsteroids.Add("asteroid8moon3", new Vector3(-215.801025, 500.157227, -491.888367));
            m_compatStaticAsteroids.Add("asteroid8moon4", new Vector3(-246.462, 686.4795, -678.764648));
            m_compatStaticAsteroids.Add("asteroid9", new Vector3(-263.329651, 511.187561, -735.3834));
            m_compatStaticAsteroids.Add("asteroid9moon0", new Vector3(-210.570984, 588.0855, -597.6558));
            m_compatStaticAsteroids.Add("asteroid9moon1", new Vector3(41.102623, 502.5733, -611.104553));
            m_compatStaticAsteroids.Add("asteroid9moon2", new Vector3(71.4354858, 620.224854, -533.9093));
            m_compatStaticAsteroids.Add("asteroid9moon3", new Vector3(37.0611725, 650.394, -614.7657));
            m_compatStaticAsteroids.Add("asteroid9moon4", new Vector3(-10.148819, 690.253357, -617.6583));
            m_compatStaticAsteroids.Add("asteroid10", new Vector3(1220.54272, -396.55835, -1067.83179));
            m_compatStaticAsteroids.Add("asteroid10moon0", new Vector3(1283.41455, -141.081909, -833.4448));
            m_compatStaticAsteroids.Add("asteroid10moon1", new Vector3(1351.84424, -340.871521, -860.5213));
            m_compatStaticAsteroids.Add("asteroid10moon2", new Vector3(1441.8689, -265.305634, -741.8551));
            m_compatStaticAsteroids.Add("asteroid10moon3", new Vector3(1431.87585, -321.47998, -967.78595));
            m_compatStaticAsteroids.Add("asteroid10moon4", new Vector3(1480.02515, -322.488922, -812.282654));
            m_compatStaticAsteroids.Add("asteroid11", new Vector3(-183.633957, 466.912048, -661.5457));
            m_compatStaticAsteroids.Add("asteroid11moon0", new Vector3(-11.5127068, 753.9039, -555.280457));
            m_compatStaticAsteroids.Add("asteroid11moon1", new Vector3(-183.039, 568.8601, -495.7103));
            m_compatStaticAsteroids.Add("asteroid11moon2", new Vector3(-249.045959, 588.8494, -568.498169));
            m_compatStaticAsteroids.Add("asteroid11moon3", new Vector3(23.8666077, 697.3216, -618.337341));
            m_compatStaticAsteroids.Add("asteroid11moon4", new Vector3(-64.45533, 794.289368, -563.955566));
            m_compatStaticAsteroids.Add("asteroid12", new Vector3(1661.90222, -764.5163, -834.8348));
            m_compatStaticAsteroids.Add("asteroid12moon0", new Vector3(1746.53491, -508.546356, -559.888367));
            m_compatStaticAsteroids.Add("asteroid12moon1", new Vector3(1759.22644, -460.547882, -674.450745));
            m_compatStaticAsteroids.Add("asteroid12moon2", new Vector3(1902.05261, -646.29895, -521.436035));
            m_compatStaticAsteroids.Add("asteroid12moon3", new Vector3(1783.50293, -481.156219, -546.9324));
            m_compatStaticAsteroids.Add("asteroid12moon4", new Vector3(1922.93164, -536.1296, -514.729553));
            m_compatStaticAsteroids.Add("asteroid13", new Vector3(-554.702148, 680.6017, -1425.75684));
            m_compatStaticAsteroids.Add("asteroid13moon0", new Vector3(-247.824249, 658.906, -1189.49268));
            m_compatStaticAsteroids.Add("asteroid13moon1", new Vector3(-223.8683, 798.0059, -1253.07385));
            m_compatStaticAsteroids.Add("asteroid13moon2", new Vector3(-451.320862, 623.977539, -1223.93152));
            m_compatStaticAsteroids.Add("asteroid13moon3", new Vector3(-499.679932, 796.921753, -1202.13879));
            m_compatStaticAsteroids.Add("asteroid13moon4", new Vector3(-388.491455, 878.9669, -1172.26782));
            m_compatStaticAsteroids.Add("asteroid14", new Vector3(339.465454, 540.275452, -1189.011));
            m_compatStaticAsteroids.Add("asteroid14moon0", new Vector3(480.786316, 636.0764, -1039.763));
            m_compatStaticAsteroids.Add("asteroid14moon1", new Vector3(270.7714, 802.2115, -1126.12329));
            m_compatStaticAsteroids.Add("asteroid14moon2", new Vector3(418.871948, 670.8664, -1260.99463));
            m_compatStaticAsteroids.Add("asteroid14moon3", new Vector3(374.3302, 858.692932, -1049.71313));
            m_compatStaticAsteroids.Add("asteroid14moon4", new Vector3(482.535767, 743.1303, -998.006836));

            MyWorldGenerator.OnAfterGenerate += MyWorldGenerator_AfterGenerate;
        }

        static void MyWorldGenerator_AfterGenerate(ref MyWorldGenerator.Args args)
        {
            if (MyPerGameSettings.EnablePregeneratedAsteroidHack)
                LoadPregeneratedAsteroids(ref args);
        }

        // Backward compatibility method only used when the procedurally generated asteroids were not working
        public static void LoadPregeneratedAsteroids(ref MyWorldGenerator.Args args)
        {
            float offset = args.Scenario.AsteroidClustersOffset;
            float scale = Math.Max(offset / 700.0f, 1.0f);

            args.AsteroidAmount -= MySession.Static.VoxelMaps.Instances.Count;
            if (args.Scenario.CentralClusterEnabled)
                AddPregeneratedCluster(ref args, "centralAsteroid", scale);

            List<int> asteroidSelector = new List<int>(15);
            for (int i = 0; i < 15; ++i) asteroidSelector.Add(i);

            while (args.AsteroidAmount > 0 && asteroidSelector.Count > 0)
            {
                int i = MyRandom.Instance.Next(0, asteroidSelector.Count);
                int asteroidIndex = asteroidSelector[i];
                asteroidSelector.RemoveAtFast(i);

                AddPregeneratedCluster(ref args, "asteroid" + asteroidIndex, scale);
            }
        }

        // Backward compatibility method only used when the procedurally generated asteroids were not working
        private static void AddPregeneratedCluster(ref MyWorldGenerator.Args args, string name, float scale)
        {
            AddPregeneratedAsteroid(name, scale);
            args.AsteroidAmount -= 1;

            for (int i = 0; i < 5; ++i)
            {
                AddPregeneratedAsteroid(name + "moon" + i, scale);
            }
        }

        // Backward compatibility method only used when the procedurally generated asteroids were not working
        private static void AddPregeneratedAsteroid(string name, float scale)
        {
            Vector3 position = m_compatStaticAsteroids[name] * scale;
            MyWorldGenerator.AddAsteroidPrefab("Pregen\\" + name, position, name);
        }
    }
}

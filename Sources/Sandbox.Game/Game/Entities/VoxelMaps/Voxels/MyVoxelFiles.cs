using Sandbox;
using VRage.Common.Utils;
using SysUtils.Utils;
using VRageMath;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Voxels;
using System;
using System.IO;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Voxels
{
    class MyVoxelFile
    {
        public MyMwcVoxelFilesEnum VoxelFileEnum;
        public Vector3I SizeInVoxels;
        public string VoxelName;

        private MyVoxelFile() { }
        
        public MyVoxelFile(MyMwcVoxelFilesEnum voxelFileEnum, Vector3I sizeInVoxels, string voxelName)
        {
            VoxelFileEnum = voxelFileEnum;
            SizeInVoxels = sizeInVoxels;
            VoxelName = voxelName;
        }

        public int GetLargestSizeInVoxels()
        {
            int max = SizeInVoxels.X;
            if (SizeInVoxels.Y > max) max = SizeInVoxels.Y;
            if (SizeInVoxels.Z > max) max = SizeInVoxels.Z;
            return max;
        }

        //  Full or relative path to VOX file
        public string GetVoxFilePath()
        {
            return Path.Combine(MyFileSystem.ContentPath, "VoxelMaps", VoxelName + MyVoxelConstants.FILE_EXTENSION);
        }

        public string GetIconFilePath()
        {
            return "Textures\\GUI\\GuiHelpers\\" + VoxelName;
        }
    }

    static class MyVoxelFiles
    {
        public static readonly string ExportFile = "VoxelImporterTest";

        public static MyVoxelFile[] DefaultVoxelFiles = new MyVoxelFile[MyVRageUtils.GetMaxValueFromEnum<MyMwcVoxelFilesEnum>() + 1];

        public static void LoadData()
        {
            Add(MyMwcVoxelFilesEnum.TorusWithManyTunnels_256x128x256, new Vector3I(256, 128, 256), "TorusWithManyTunnels_256x128x256");
            Add(MyMwcVoxelFilesEnum.TorusWithSmallTunnel_256x128x256, new Vector3I(256, 128, 256), "TorusWithSmallTunnel_256x128x256");
            Add(MyMwcVoxelFilesEnum.VerticalIsland_128x128x128, new Vector3I(128, 128, 128), "VerticalIsland_128x128x128");
            Add(MyMwcVoxelFilesEnum.VerticalIsland_128x256x128, new Vector3I(128, 256, 128), "VerticalIsland_128x256x128");
            Add(MyMwcVoxelFilesEnum.VerticalIslandStorySector_128x256x128, new Vector3I(128, 256, 128), "VerticalIslandStorySector_128x256x128");
            Add(MyMwcVoxelFilesEnum.DeformedSphere1_64x64x64, new Vector3I(64, 64, 64), "DeformedSphere1_64x64x64");
            Add(MyMwcVoxelFilesEnum.DeformedSphere2_64x64x64, new Vector3I(64, 64, 64), "DeformedSphere2_64x64x64");
            Add(MyMwcVoxelFilesEnum.DeformedSphereWithCorridor_128x64x64, new Vector3I(128, 64, 64), "DeformedSphereWithCorridor_128x64x64");
            Add(MyMwcVoxelFilesEnum.ScratchedBoulder_128x128x128, new Vector3I(128, 128, 128), "ScratchedBoulder_128x128x128");
            Add(MyMwcVoxelFilesEnum.DeformedSphereWithHoles_64x128x64, new Vector3I(64, 128, 64), "DeformedSphereWithHoles_64x128x64");
            Add(MyMwcVoxelFilesEnum.Mission01_asteroid_mine, new Vector3I(256, 256, 256), "Mission01_asteroid_mine");

            Add(MyMwcVoxelFilesEnum.EacPrisonAsteroid, new Vector3I(512, 512, 512), "EacPrisonAsteroid");
            Add(MyMwcVoxelFilesEnum.Chinese_Mines_FrontRightAsteroid, new Vector3I(128, 128, 128), "Chinese_Mines_FrontRightAsteroid");
            
            Add(MyMwcVoxelFilesEnum.Barths_moon_base, new Vector3I(256, 256, 256), "Barths_moon_base");
            Add(MyMwcVoxelFilesEnum.JunkYardToxic_128x128x128, new Vector3I(128, 128, 128), "JunkYardToxic_128x128x128");

            Add(MyMwcVoxelFilesEnum.Barths_moon_camp, new Vector3I(256, 256, 256), "Barths_moon_camp");
            Add(MyMwcVoxelFilesEnum.rift_base_smaller, new Vector3I(64, 128, 64), "Rift_base_smaller");
            Add(MyMwcVoxelFilesEnum.Junkyard_RaceAsteroid_256x256x256, new Vector3I(256, 256, 256), "Junkyard_RaceAsteroid_256x256x256");
            Add(MyMwcVoxelFilesEnum.ChineseRefinery_Second_128x128x128, new Vector3I(128, 128, 128), "ChineseRefinery_Second_128x128x128");
            Add(MyMwcVoxelFilesEnum.Chinese_Corridor_Tunnel_256x256x256, new Vector3I(256, 256, 256), "Chinese_Corridor_Tunnel_256x256x256");

            Add(MyMwcVoxelFilesEnum.Bioresearch, new Vector3I(256, 256, 256), "Bioresearch");

            Add(MyMwcVoxelFilesEnum.small2_asteroids, new Vector3I(64, 64, 64), "small2_asteroids");
            Add(MyMwcVoxelFilesEnum.small3_asteroids, new Vector3I(64, 64, 64), "small3_asteroids");
            Add(MyMwcVoxelFilesEnum.many_medium_asteroids, new Vector3I(64, 64, 64), "many_medium_asteroids");
            Add(MyMwcVoxelFilesEnum.many_small_asteroids, new Vector3I(128, 128, 128), "many_small_asteroids");
            Add(MyMwcVoxelFilesEnum.many2_small_asteroids, new Vector3I(64, 64, 64), "many2_small_asteroids");

            Add(MyMwcVoxelFilesEnum.reef_ast, new Vector3I(64, 64, 64), "reef_ast");

            Add(MyMwcVoxelFilesEnum.hopebase512, new Vector3I(512, 512, 512), "hopebase512");
            Add(MyMwcVoxelFilesEnum.hopefood128, new Vector3I(128, 128, 128), "hopefood128");
            Add(MyMwcVoxelFilesEnum.Small_Pirate_Base_Asteroid, new Vector3I(128, 128, 128), "Small_Pirate_Base_Asteroid");
            Add(MyMwcVoxelFilesEnum.Small_Pirate_Base_3_1, new Vector3I(64, 64, 64), "Small_Pirate_Base_3_1");
            Add(MyMwcVoxelFilesEnum.Small_Pirate_Base_3_2, new Vector3I(64, 64, 64), "Small_Pirate_Base_3_2");
            Add(MyMwcVoxelFilesEnum.Laika5_128_128_128, new Vector3I(128, 128, 128), "Laika5_128_128_128");
            Add(MyMwcVoxelFilesEnum.Arabian_Border_7, new Vector3I(64, 64, 64), "Arabian_Border_7");
            Add(MyMwcVoxelFilesEnum.Arabian_Border_Arabian, new Vector3I(128, 128, 128), "Arabian_Border_Arabian");
            Add(MyMwcVoxelFilesEnum.Chinese_Mines_Side, new Vector3I(128, 128, 128), "Chinese_Mines_Side");

            Add(MyMwcVoxelFilesEnum.Fortress_Sanc_1, new Vector3I(128, 256, 128), "Fortress_Sanc_1");
            Add(MyMwcVoxelFilesEnum.Russian_Transmitter_2, new Vector3I(256, 128, 256), "Russian_Transmitter_2");
            Add(MyMwcVoxelFilesEnum.Nearby_Station_7, new Vector3I(64, 128, 64), "Nearby_Station_7");

            Add(MyMwcVoxelFilesEnum.RiftStationSmaller, new Vector3I(64, 128, 64), "RiftStationSmaller");

            Add(MyMwcVoxelFilesEnum.PirateBaseStaticAsteroid_A_1000m, new Vector3I(128, 128, 128), "PirateBaseStaticAsteroid_A_1000m");
            Add(MyMwcVoxelFilesEnum.PirateBaseStaticAsteroid_A_5000m_1, new Vector3I(384, 384, 384), "PirateBaseStaticAsteroid_A_5000m_1");
            Add(MyMwcVoxelFilesEnum.PirateBaseStaticAsteroid_A_5000m_2, new Vector3I(384, 384, 384), "PirateBaseStaticAsteroid_A_5000m_2");

            Add(MyMwcVoxelFilesEnum.RedShipCrashedAsteroid, new Vector3I(128, 128, 128), "RedShipCrashedAsteroid");

            //  Assert whether we didn't forget on some voxelfile
            for (int i = 0; i < DefaultVoxelFiles.Length; i++)
            {
                MyDebug.AssertDebug(DefaultVoxelFiles[i] != null);
            }
        }

        static void Add(MyMwcVoxelFilesEnum voxelFileEnum, Vector3I sizeInVoxels, string filename)
        {
            DefaultVoxelFiles[(int)voxelFileEnum] = new MyVoxelFile(voxelFileEnum, sizeInVoxels, filename);
        }

        public static MyVoxelFile Get(MyMwcVoxelFilesEnum voxelFileEnum)
        {
            return DefaultVoxelFiles[(int)voxelFileEnum];
        }
    }
}

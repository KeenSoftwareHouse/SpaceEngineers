using System;
using System.Collections.Generic;
using System.Diagnostics;


using Sandbox.Common;
using VRage.Utils;


namespace Sandbox.Engine.Utils
{
    //  I have made this mapping array because when using obfuscator, enum names change and when converted to string, they no more corresponds to file names
    static partial class MyEnumsToStrings
    {
        public static string[] HudTextures = new string[] 
        {
            "corner.png",
            "crosshair.png",
            "HudOre.png",
            "Target_enemy.png",
            "Target_friend.png",
            "Target_neutral.png",
            "Target_me.png",
            "TargetTurret.png",
            "DirectionIndicator.png",
            "gravity_point_red.png",
            "gravity_point_white.png",
            "gravity_arrow.png",
            "hit_confirmation.png"
        };

        public static string[] Particles = new string[] 
        { 
            "Explosion.dds", 
            "ExplosionSmokeDebrisLine.dds", 
            "Smoke.dds", 
            "Test.dds", 
            "EngineThrustMiddle.dds", 
            "ReflectorCone.dds", 
            "ReflectorGlareAdditive.dds", 
            "ReflectorGlareAlphaBlended.dds", 
            "MuzzleFlashMachineGunFront.dds",
            "MuzzleFlashMachineGunSide.dds", 
            "ProjectileTrailLine.dds", 
            "ContainerBorder.dds",
            "Dust.dds",
            "Crosshair.dds",
            "Sun.dds",
            "LightRay.dds",
            "LightGlare.dds",
            "SolarMapOrbitLine.dds",
            "SolarMapSun.dds",
            "SolarMapAsteroidField.dds",
            "SolarMapFactionMap.dds",
            "SolarMapAsteroid.dds",
            "SolarMapZeroPlaneLine.dds",
            "SolarMapSmallShip.dds",
            "SolarMapLargeShip.dds",
            "SolarMapOutpost.dds",

            "Grid.dds",
            "ContainerBorderSelected.dds",

            // Factions
            "FactionRussia.dds",
            "FactionChina.dds",
            "FactionJapan.dds",
            "FactionUnitedKorea.dds",
            "FactionFreeAsia.dds",
            "FactionSaudi.dds",
            "FactionEAC.dds",
            "FactionCSR.dds",
            "FactionIndia.dds",
            "FactionChurch.dds",
            "FactionOmnicorp.dds",
            "FactionFourthReich.dds",
            "FactionSlavers.dds",

            "Smoke_b.dds",
            "Smoke_c.dds",

            "Sparks_a.dds",
            "Sparks_b.dds",
            "particle_stone.dds",
            "Stardust.dds",
            "particle_trash_a.dds",
            "particle_trash_b.dds",
            "particle_glare.dds",
            "smoke_field.dds",
            "Explosion_pieces.dds",
            "particle_laser.dds",
            "particle_nuclear.dds",
            "Explosion_line.dds",
            "particle_flash_a.dds",
            "particle_flash_b.dds",
            "particle_flash_c.dds",
            "snap_point.dds",

            "SolarMapNavigationMark.dds",

            "Impostor_StaticAsteroid20m_A.dds",
            "Impostor_StaticAsteroid20m_C.dds",
            "Impostor_StaticAsteroid50m_D.dds",
            "Impostor_StaticAsteroid50m_E.dds",

            "GPS.dds",
            "GPSBack.dds",

            "ShotgunParticle.dds",

            "ObjectiveDummyFace.dds",
            "ObjectiveDummyLine.dds",

            "SunDisk.dds",

            "scanner_01.dds",
            "Smoke_square.dds",
            "Smoke_lit.dds",

            "SolarMapSideMission.dds",
            "SolarMapStoryMission.dds",
            "SolarMapTemplateMission.dds",
            "SolarMapPlayer.dds",
            "ReflectorConeCharacter.dds",
        };

        public static string[] HudRadarTextures = new string[] {"Arrow.png", "ImportantObject.tga", "LargeShip.tga",
            "Line.tga", "RadarBackground.tga", "RadarPlane.tga", "SectorBorder.tga",
            "SmallShip.tga", "Sphere.png", "SphereGrid.tga", "Sun.tga" , "OreDeposit_Treasure.png", "OreDeposit_Helium.png",
            "OreDeposit_Ice.png", "OreDeposit_Iron.png", "OreDeposit_Lava.png", "OreDeposit_Gold.png", "OreDeposit_Platinum.png", 
            "OreDeposit_Silver.png", "OreDeposit_Silicon.png", "OreDeposit_Organic.png", "OreDeposit_Nickel.png", "OreDeposit_Magnesium.png", 
            "OreDeposit_Uranite.png", "OreDeposit_Cobalt.png", "OreDeposit_Snow.png" };

        public static string[] Decals = new string[] { "ExplosionSmut", "BulletHoleOnMetal", "BulletHoleOnRock" };

        public static string[] CockpitGlassDecals = new string[] { "DirtOnGlass", "BulletHoleOnGlass", "BulletHoleSmallOnGlass" };

        public static string[] SessionType = new string[] { "NEW_STORY", "LOAD_CHECKPOINT", "JOIN_FRIEND_STORY", "MMO", "SANDBOX_OWN", "SANDBOX_FRIENDS", "JOIN_SANDBOX_FRIEND", "EDITOR_SANDBOX", "EDITOR_STORY", "EDITOR_MMO", "SANDBOX_RANDOM" };

        static MyEnumsToStrings()
        {
            //  We need to check if programmer who changed/added entries in enum, didn't forget to add it also to these string constants
            //  If he forgot, application will fail here on start, so he can find out quickly
            try
            {
                //Validate(typeof(MyGameControlEnums), GameControlEnums);
                //Validate(typeof(MyEditorControlEnums), EditorControlEnums);
                //Validate(typeof(MyCameraDirection), CameraDirection);

                //Validate(typeof(VRageRender.MyDecalTexturesEnum), Decals);
                //Validate(typeof(MyCockpitGlassDecalTexturesEnum), CockpitGlassDecals);
                //Validate(typeof(MySoundCuesEnum), Sounds);
                //Validate(typeof(MyGuiInputDeviceEnum), GuiInputDeviceEnum);
                //Validate(typeof(MyMouseButtonsEnum), MouseButtonsEnum);
                //Validate(typeof(MyJoystickButtonsEnum), JoystickButtonsEnum);
                //Validate(typeof(MyJoystickAxesEnum), JoystickAxesEnum);
                //Validate(typeof(MyGuiControlTypeEnum), ControlTypeEnum);
            }
            catch (Exception e)
            {
                Debug.Fail("Validation threw an exception: " + e.Message);
            }
        }

        static void Validate<T>(Type type, T list) where T : IList<string>
        {
            Array values = Enum.GetValues(type);
            Type underlyingType = Enum.GetUnderlyingType(type);
            if (underlyingType == typeof(System.Byte))
            {
                foreach (byte value in values)
                {
                    MyDebug.AssertRelease(list[value] != null);
                }
            }
            else if (underlyingType == typeof(System.Int16))
            {
                foreach (short value in values)
                {
                    MyDebug.AssertRelease(list[value] != null);
                }
            }
            else if (underlyingType == typeof(System.UInt16))
            {
                foreach (ushort value in values)
                {
                    MyDebug.AssertRelease(list[value] != null);
                }
            }
            else if (underlyingType == typeof(System.Int32))
            {
                foreach (int value in values)
                {
                    MyDebug.AssertRelease(list[value] != null);
                }
            }
            else
            {
                //  Unhandled underlying type - probably "long"
                throw new InvalidBranchException();
            }            
        }

        
    }

}
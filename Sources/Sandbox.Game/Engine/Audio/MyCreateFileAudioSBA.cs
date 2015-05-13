using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.CommonLib.ObjectBuilders;
using Sandbox.CommonLib.ObjectBuilders.Audio;
using System.IO;

namespace Sandbox.Engine.Audio
{
    static class MyCreateFileAudioSBA
    {
        static public string GetFilenameSBA()
        {
            return Path.Combine(GameEnvironment.ContentPath, "Data", "Audio.sba");
        }

        static public void Create()
        {
            MyObjectBuilder_CueDefinitions ob = new MyObjectBuilder_CueDefinitions()
            {
                Cues = new MyObjectBuilder_CueDefinition[]
                {
                    //
                    // MUSIC
                    //
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_KA01",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA01\Mus_calm_KA_1.xwm",
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA01\Mus_calm_KA_1_theme.xwm",
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA01\Mus_calm_KA_3.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_KA02",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA02\Mus_calm_KA_4.xwm",
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA02\Mus_calm_KA_5.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_KA03",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA03\Mus_calm_KA_2.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_KA05",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_KA02\Mus_calm_KA_5.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_MM_b",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_MM01\Mus_calm_b_MM.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_MM01",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_MM01\Mus_calm_b_MM.xwm",
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_MM01\Mus_calm_a_MM.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MusCalmAtmosphere_MM02",
                        Category = "Music",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_MM01\Mus_calm_a_MM.xwm",
                            @"MUS\MusCalmAtmosphere\MusCalmAtmosphere_MM01\Mus_calm_b_MM.xwm"
                        }
                    },

                    //
                    // SOUNDS
                    //
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpBulletHitGlass",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpBulletHitGlass02.xwm",
                            @"IMP\ImpBulletHitGlass03.xwm",
                            @"IMP\ImpBulletHitGlass04.xwm",
                            @"IMP\ImpBulletHitGlass01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpBulletHitMetal",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpBulletHitMetal01.xwm",
                            @"IMP\ImpBulletHitMetal02.xwm",
                            @"IMP\ImpBulletHitMetal03.xwm",
                            @"IMP\ImpBulletHitMetal04.xwm",
                            @"IMP\ImpBulletHitMetal05.xwm",
                            @"IMP\ImpBulletHitMetal06.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpBulletHitRock",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpBulletHitRock06.xwm",
                            @"IMP\ImpBulletHitRock05.xwm",
                            @"IMP\ImpBulletHitRock04.xwm",
                            @"IMP\ImpBulletHitRock01.xwm",
                            @"IMP\ImpBulletHitRock02.xwm",
                            @"IMP\ImpBulletHitRock03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpBulletHitShip",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpBulletHitShip01.xwm",
                            @"IMP\ImpBulletHitShip02.xwm",
                            @"IMP\ImpBulletHitShip03.xwm",
                            @"IMP\ImpBulletHitShip04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpExpHitGlass",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpExpHitGlass01.xwm",
                            @"IMP\ImpExpHitGlass02.xwm",
                            @"IMP\ImpExpHitGlass03.xwm",
                            @"IMP\ImpExpHitGlass04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpExpHitMetal",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpExpHitMetal01.xwm",
                            @"IMP\ImpExpHitMetal02.xwm",
                            @"IMP\ImpExpHitMetal03.xwm",
                            @"IMP\ImpExpHitMetal04.xwm",
                            @"IMP\ImpExpHitMetal05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpExpHitRock",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpExpHitRock01.xwm",
                            @"IMP\ImpExpHitRock02.xwm",
                            @"IMP\ImpExpHitRock03.xwm",
                            @"IMP\ImpExpHitRock04.xwm",
                            @"IMP\ImpExpHitRock05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpExpHitShip",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpExpHitShip05.xwm",
                            @"IMP\ImpExpHitShip01.xwm",
                            @"IMP\ImpExpHitShip02.xwm",
                            @"IMP\ImpExpHitShip03.xwm",
                            @"IMP\ImpExpHitShip04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpPlayerShipCollideMetal",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpPlayerShipCollideMetal01.xwm",
                            @"IMP\ImpPlayerShipCollideMetal02.xwm",
                            @"IMP\ImpPlayerShipCollideMetal03.xwm",
                            @"IMP\ImpPlayerShipCollideMetal04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpPlayerShipCollideRock",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpPlayerShipCollideRock01.xwm",
                            @"IMP\ImpPlayerShipCollideRock02.xwm",
                            @"IMP\ImpPlayerShipCollideRock03.xwm",
                            @"IMP\ImpPlayerShipCollideRock04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpPlayerShipCollideShip",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpPlayerShipCollideShip01.xwm",
                            @"IMP\ImpPlayerShipCollideShip02.xwm",
                            @"IMP\ImpPlayerShipCollideShip03.xwm",
                            @"IMP\ImpPlayerShipCollideShip04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpPlayerShipScrapeShipLoop",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpPlayerShipScrapeShipLoop.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpRockCollideMetal",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpRockCollideMetal01.xwm",
                            @"IMP\ImpRockCollideMetal02.xwm",
                            @"IMP\ImpRockCollideMetal03.xwm",
                            @"IMP\ImpRockCollideMetal04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpRockCollideRock",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpRockCollideRock01.xwm",
                            @"IMP\ImpRockCollideRock02.xwm",
                            @"IMP\ImpRockCollideRock03.xwm",
                            @"IMP\ImpRockCollideRock04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpShipCollideMetal",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 200f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpShipCollideMetal01.xwm",
                            @"IMP\ImpShipCollideMetal02.xwm",
                            @"IMP\ImpShipCollideMetal03.xwm",
                            @"IMP\ImpShipCollideMetal04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ImpShipCollideRock",
                        Category = "Imp",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 200f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"IMP\ImpShipCollideRock01.xwm",
                            @"IMP\ImpShipCollideRock02.xwm",
                            @"IMP\ImpShipCollideRock03.xwm",
                            @"IMP\ImpShipCollideRock04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MovDoorSmallClose",
                        Category = "Door",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MOV\MovDoorSmallClose.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "MovDoorSmallOpen",
                        Category = "Door",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"MOV\MovDoorSmallOpen.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "SfxFlareLoop01",
                        Category = "Sfx",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"SFX\SfxFlareLoop01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "SfxGeigerCounterHeavyLoop",
                        Category = "Sfx",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"SFX\SfxGeigerCounterHeavyLoop.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "VehToolCrusherDrillLoop2d",
                        Category = "Drills",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 250f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"VEH\VehToolCrusherDrillLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "VehToolCrusherDrillLoop3d",
                        Category = "Drills",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 250f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "VehToolCrusherDrillLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"VEH\VehToolCrusherDrillLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "VehToolCrusherDrillRelease2d",
                        Category = "Drills",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"VEH\VehToolCrusherDrillRelease2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "VehToolCrusherDrillRelease3d",
                        Category = "Drills",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 250f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "VehToolCrusherDrillRelease2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"VEH\VehToolCrusherDrillReleaseLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepAutocanonFire2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 0.5397514f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepAutocanon3FireLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepAutocanonFire3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 154.379517f,
                        Volume = 0.5686615f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "WepAutocanonFire2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepAutocanon3FireLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepAutocanonRel2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.590344131f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepAutocanon3Rel2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepAutocanonRel3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 154.379517f,
                        Volume = 0.6337095f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "WepAutocanonRel2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepAutocanon3Rel3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepBombExplosion",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 139.9247f,
                        Volume = 0.8505353f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepBombExplosion04.xwm",
                            @"WEP\WepBombExplosion01.xwm",
                            @"WEP\WepBombExplosion02.xwm",
                            @"WEP\WepBombExplosion03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepLargeShipAutocannonRotate",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepLargeShipAutocannonRotate.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepLargeShipAutocannonRotateRelease",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepLargeShipAutocannonRotateRelease.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunHighFire2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunHighFire2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunHighFire3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "WepMachineGunHighFire2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunHighFireLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunHighRel2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunHighRel2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunHighRel3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "WepMachineGunHighRel2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunHighRel3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunNormFire2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunNormFire2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunNormFire3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "WepMachineGunNormFire2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunNormFire3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunNormRel2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunNormRel2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMachineGunNormRel3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "WepMachineGunNormRel2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMachineGunNormRel3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMissileExplosion",
                        Category = "IMPORTANTS",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMissileExplosion01.xwm",
                            @"WEP\WepMissileExplosion02.xwm",
                            @"WEP\WepMissileExplosion03.xwm",
                            @"WEP\WepMissileExplosion04.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMissileFly",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMissileFlyLoop.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMissileLaunch2d",
                        Category = "wep2d",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 200f,
                        Volume = 0.532524f,
                        VolumeVariation = 0f,
                        PitchVariation = 250f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMissileLaunch2d01.xwm",
                            @"WEP\WepMissileLaunch2d02.xwm",
                            @"WEP\WepMissileLaunch2d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepMissileLaunch3d",
                        Category = "Wep3D",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 183.290009f,
                        Volume = 0.518068731f,
                        VolumeVariation = 0.699623466f,
                        PitchVariation = 57.241993f,
                        Loopable = false,
                        Alternative2D = "WepMissileLaunch2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepMissileLaunch3d01.xwm",
                            @"WEP\WepMissileLaunch3d02.xwm",
                            @"WEP\WepMissileLaunch3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepSniperScopeZoomALoop",
                        Category = "Cockpit",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepSniperScopeZoomAloop.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "WepSniperScopeZoomRel",
                        Category = "Cockpit",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"WEP\WepSniperScopeZoomRel.xwm"
                        }
                    },

                    //
                    // ARCADE
                    //
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayRunMetalLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.7854873f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayRunMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayRunRockLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.8071699f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayRunRockLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayRunMetalLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.8505353f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlayRunMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayRunMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayRunRockLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.814397335f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlayRunRockLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayRunRockLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJump2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 1f,
                        VolumeVariation = 10f,
                        PitchVariation = 243.350739f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJump2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJump3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcPlayJump2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJump3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayFallMetal2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 1f,
                        VolumeVariation = 1.42237329f,
                        PitchVariation = 35.5593338f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayFallMetal2d01.xwm",
                            @"ARC\PLAYER\ArcPlayFallMetal2d02.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayFallRock2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.662619531f,
                        VolumeVariation = 3.084708f,
                        PitchVariation = 62.6625557f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayFallRock2d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayFallMetal3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.555071f,
                        PitchVariation = 55.4350357f,
                        Loopable = false,
                        Alternative2D = "ArcPlayFallMetal2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayFallMetal3d01.xwm",
                            @"ARC\PLAYER\ArcPlayFallMetal3d02.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayFallRock3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.156692579f,
                        VolumeVariation = 2.28967977f,
                        PitchVariation = 55.4350357f,
                        Loopable = false,
                        Alternative2D = "ArcPlayFallRock2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayFallRock3d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetOn2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.844171762f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetOn2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetOff2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 1f,
                        VolumeVariation = 3.879736f,
                        PitchVariation = 241.543884f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetOff2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetIdleLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.8505353f,
                        VolumeVariation = 1.92830074f,
                        PitchVariation = 13.8767748f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetIdleLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetOn3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.193696067f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcPlayJetOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetOff3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 4.602486f,
                        PitchVariation = 234.316269f,
                        Loopable = false,
                        Alternative2D = "ArcPlayJetOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetIdleLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.843307853f,
                        VolumeVariation = 0.41052267f,
                        PitchVariation = 4.8424015f,
                        Loopable = true,
                        Alternative2D = "ArcPlayJetIdleLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetIdleLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 1f,
                        VolumeVariation = 2.72332883f,
                        PitchVariation = 89.76578f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 1.06099844f,
                        PitchVariation = 28.3318138f,
                        Loopable = true,
                        Alternative2D = "ArcPlayJetRunLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunStart2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.193696067f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunStart2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunStart3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.193696067f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcPlayJetRunStart2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunStart3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunRelease2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.193696067f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunRelease2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayJetRunRelease3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0.193696067f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcPlayJetRunRelease2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayJetRunRelease3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudClick",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.5108413f,
                        VolumeVariation = 1.6392f,
                        PitchVariation = 31.9456253f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudClick.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudUse",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.6f,
                        VolumeVariation = 1.20555079f,
                        PitchVariation = 33.75248f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudUse01.xwm",
                            @"ARC\HUD\HudUse02.xwm",
                            @"ARC\HUD\HudUse03.xwm"     
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudRotateBlock",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.532524f,
                        VolumeVariation = 2.795607f,
                        PitchVariation = 131.324142f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudRotateBlock01.xwm",
                            @"ARC\HUD\HudRotateBlock02.xwm",
                            @"ARC\HUD\HudRotateBlock03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudPlaceBlock",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.4963865f,
                        VolumeVariation = 0.6273493f,
                        PitchVariation = 68.08322f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudPlaceBlock01.xwm",
                            @"ARC\HUD\HudPlaceBlock02.xwm",
                            @"ARC\HUD\HudPlaceBlock03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudDeleteBlock",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.4963865f,
                        VolumeVariation = 2.00057888f,
                        PitchVariation = 95.18645f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudDeleteBlock01.xwm",
                            @"ARC\HUD\HudDeleteBlock02.xwm",
                            @"ARC\HUD\HudDeleteBlock03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudColorBlock",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.5036139f,
                        VolumeVariation = 1.6392f,
                        PitchVariation = 44.59381f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudColorBlock01.xwm",
                            @"ARC\HUD\HudColorBlock02.xwm",
                            @"ARC\HUD\HudColorBlock03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudMouseClick",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.409655839f,
                        VolumeVariation = 1.06099844f,
                        PitchVariation = 12.0699205f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudMouseClick.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "HudMouseOver",
                        Category = "HUD",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 0.380745769f,
                        VolumeVariation = 2.57878041f,
                        PitchVariation = 69.8901749f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\HUD\HudMouseOver.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleShotLoop2d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 327.84f,
                        Volume = 0.7132123f,
                        VolumeVariation = 1.35009921f,
                        PitchVariation = 50.014473f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepPlayRifleShotLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleShotLoop3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 197.744843f,
                        Volume = 0.7276671f,
                        VolumeVariation = 1.277825f,
                        PitchVariation = 30.1387711f,
                        Loopable = true,
                        Alternative2D = "ArcWepPlayRifleShotLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepPlayRifleShotLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleShotRel2d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 183.290009f,
                        Volume = 0.301242948f,
                        VolumeVariation = 1.56692576f,
                        PitchVariation = 250f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepPlayRifleShotRel2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleShotRel3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 241.110168f,
                        Volume = 1f,
                        VolumeVariation = 1.20555079f,
                        PitchVariation = 250f,
                        Loopable = false,
                        Alternative2D = "ArcWepPlayRifleShotRel2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepPlayRifleShotRel3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleEnd2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleEnd2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleEnd3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolPlayDrillIdleEnd2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleEnd3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayDrillIdleLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleStart2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleStart2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillIdleStart3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolPlayDrillIdleStart2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillIdleStart3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillMetalLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillMetalLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayDrillMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillRockLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillRockLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayDrillRockLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayDrillRockLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayDrillRockLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingShotLoop2d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipGatlingShotLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingShotLoop3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcWepShipGatlingShotLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipGatlingShotLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingShotRel2d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipGatlingShotRel2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingShotRel3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcWepShipGatlingShotRel2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipGatlingShotRel3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipMissileFlyLoop3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipMissileFlyLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipSmallMissileShot2d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipSmallMissileShot2d01.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot2d02.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot2d03.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot2d04.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot2d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipSmallMissileShot3d",
                        Category = "WEP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcWepShipSmallMissileShot2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\WEP\ArcWepShipSmallMissileShot3d01.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot3d02.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot3d03.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot3d04.xwm",
                            @"ARC\WEP\ArcWepShipSmallMissileShot3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {   
                        Name = "ArcWepPlayRifleImpMetal3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 111.0142f,
                        Volume = 0.7132123f,
                        VolumeVariation = 4.45793772f,
                        PitchVariation = 234.316269f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d01.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d02.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d03.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d04.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d05.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d06.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d07.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d08.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpMetal3d09.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleImpPlay3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 111.0142f,
                        Volume = 0.691529632f,
                        VolumeVariation = 0.988724232f,
                        PitchVariation = 250f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepPlayRifleImpPlay3d01.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpPlay3d02.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpPlay3d03.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpPlay3d04.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpPlay3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepPlayRifleImpRock3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 111.0142f,
                        Volume = 0.7132123f,
                        VolumeVariation = 0f,
                        PitchVariation = 243.350739f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d01.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d02.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d03.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d04.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d05.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d06.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d07.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d08.xwm",
                            @"ARC\IMP\ArcWepPlayRifleImpRock3d09.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingImpMetal3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 154.379517f,
                        Volume = 0.669846952f,
                        VolumeVariation = 2.2174015f,
                        PitchVariation = 62.6625557f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d01.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d02.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d03.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d04.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d05.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d06.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d07.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d08.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpMetal3d09.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingImpPlay3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 154.379517f,
                        Volume = 0.6553917f,
                        VolumeVariation = 2.00057888f,
                        PitchVariation = 78.92455f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepShipGatlingImpPlay3d01.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpPlay3d02.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpPlay3d03.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpPlay3d04.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpPlay3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepShipGatlingImpRock3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 154.379517f,
                        Volume = 0.6843018f,
                        VolumeVariation = 2.50650215f,
                        PitchVariation = 84.3452148f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d01.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d02.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d03.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d04.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d05.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d06.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d07.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d08.xwm",
                            @"ARC\IMP\ArcWepShipGatlingImpRock3d09.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearOff2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.4963865f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearOff2d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearOff3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.467476f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcShipNuclearOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearOff3d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearOn2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.467476f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearOn2d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearOn3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 0.474703848f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcShipNuclearOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearOn3d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearRunLoop2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.481931239f,
                        VolumeVariation = 0f,
                        PitchVariation = 250f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearRunLoop2d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipNuclearRunLoop3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 53.194046f,
                        Volume = 0.481931239f,
                        VolumeVariation = 0f,
                        PitchVariation = 250f,
                        Loopable = true,
                        Alternative2D = "ArcShipNuclearRunLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipNuclearRunLoop3d.xwm",
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchDwn2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 0.5397514f,
                        VolumeVariation = 2.2174015f,
                        PitchVariation = 127.710335f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchDwn2d01.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchDwn2d02.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchDwn2d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchDwn3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 38.739212f,
                        Volume = 0.453021169f,
                        VolumeVariation = 2.00057888f,
                        PitchVariation = 133.131f,
                        Loopable = false,
                        Alternative2D = "ArcPlayCrouchDwn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchDwn3d01.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchDwn3d02.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchDwn3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchUp2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 0.5036139f,
                        VolumeVariation = 1.78375232f,
                        PitchVariation = 125.903481f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchUp2d01.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchUp2d02.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchUp2d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchUp3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 53.194046f,
                        Volume = 0.546978831f,
                        VolumeVariation = 2.28967977f,
                        PitchVariation = 140.358521f,
                        Loopable = false,
                        Alternative2D = "ArcPlayCrouchUp2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchUp3d01.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchUp3d02.xwm",
                            @"ARC\PLAYER\ArcPlayCrouchUp3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchMetalLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.546978831f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchMetalLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 53.194046f,
                        Volume = 0.525296569f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlayCrouchMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchRockLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.5036139f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchRockLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayCrouchRockLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 53.194046f,
                        Volume = 0.481931239f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlayCrouchRockLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayCrouchRockLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlaySprintMetalLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlaySprintMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlaySprintMetalLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlaySprintMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlaySprintMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlaySprintRockLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlaySprintRockLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlaySprintRockLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcPlaySprintRockLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlaySprintRockLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayIronSightOff2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 0f,
                        Volume = 0.8722175f,
                        VolumeVariation = 0.699623466f,
                        PitchVariation = 246.964447f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayIronSightOff2d01.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOff2d02.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOff2d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayIronSightOff3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 38.739212f,
                        Volume = 0.901128f,
                        VolumeVariation = 1.42237329f,
                        PitchVariation = 71.69703f,
                        Loopable = false,
                        Alternative2D = "ArcPlayIronSightOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayIronSightOff3d01.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOff3d02.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOff3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayIronSightOn2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 0.5614341f,
                        VolumeVariation = 0f,
                        PitchVariation = 241.543884f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayIronSightOn2d01.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOn2d02.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOn2d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcPlayIronSightOn3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 53.194046f,
                        Volume = 0.518068731f,
                        VolumeVariation = 0.41052267f,
                        PitchVariation = 196.371811f,
                        Loopable = false,
                        Alternative2D = "ArcPlayIronSightOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\PLAYER\ArcPlayIronSightOn3d01.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOn3d02.xwm",
                            @"ARC\PLAYER\ArcPlayIronSightOn3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 400f,
                        Volume = 0.5614341f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 67.64888f,
                        Volume = 0.467476f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcSmShipLrgJetLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetLoop2d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 200f,
                        Volume = 0.48915866f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetLoop3d",
                        Category = "PLAYER",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 67.64888f,
                        Volume = 0.5036139f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcSmShipSmJetLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetRelease2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetRelease2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetRelease3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcSmShipLrgJetRelease2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetRelease3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetRelease2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetRelease2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetRelease3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcSmShipSmJetRelease2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetRelease3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetStart2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetStart2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipLrgJetStart3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcSmShipLrgJetStart2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipLrgJetStart3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetStart2d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetStart2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcSmShipSmJetStart3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcSmShipSmJetStart2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcSmShipSmJetStart3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipLandGearOff3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipLandGearOff3d01.xwm",
                            @"ARC\SHIP\ArcShipLandGearOff3d02.xwm",
                            @"ARC\SHIP\ArcShipLandGearOff3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcShipLandGearOn3d",
                        Category = "SHIP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 1000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\SHIP\ArcShipLandGearOn3d01.xwm",
                            @"ARC\SHIP\ArcShipLandGearOn3d02.xwm",
                            @"ARC\SHIP\ArcShipLandGearOn3d03.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillIdleLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.619254231f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillIdleLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillIdleLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.6553917f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolShipDrillIdleLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillIdleLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillMetalLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillMetalLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolShipDrillMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillRockLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillRockLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillRockLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolShipDrillRockLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillRockLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillOn2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.6843018f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillOn2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillOn3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.6770744f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolShipDrillOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillOff2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.742122352f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillOff2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolShipDrillOff3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 100f,
                        Volume = 0.6843018f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolShipDrillOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolShipDrillOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExpl2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExpl2d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl2d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl2d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl2d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl2d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExpl3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcWepSmallMissileExpl2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExpl3d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl3d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl3d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl3d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExpl3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExplPlay2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay2d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay2d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay2d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay2d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay2d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExplPlay3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcWepSmallMissileExplPlay2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay3d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay3d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay3d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay3d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplPlay3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExplShip2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExplShip2d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip2d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip2d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip2d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip2d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcWepSmallMissileExplShip3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcWepSmallMissileExplShip2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\EXPL\ArcWepSmallMissileExplShip3d01.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip3d02.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip3d03.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip3d04.xwm",
                            @"ARC\EXPL\ArcWepSmallMissileExplShip3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpMetalMetal3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpMetalMetal3d01.xwm",
                            @"ARC\IMP\ArcImpMetalMetal3d02.xwm",
                            @"ARC\IMP\ArcImpMetalMetal3d03.xwm",
                            @"ARC\IMP\ArcImpMetalMetal3d04.xwm",
                            @"ARC\IMP\ArcImpMetalMetal3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpMetalRock3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpMetalRock3d01.xwm",
                            @"ARC\IMP\ArcImpMetalRock3d02.xwm",
                            @"ARC\IMP\ArcImpMetalRock3d03.xwm",
                            @"ARC\IMP\ArcImpMetalRock3d04.xwm",
                            @"ARC\IMP\ArcImpMetalRock3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpPlayerMetal3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpPlayerMetal3d01.xwm",
                            @"ARC\IMP\ArcImpPlayerMetal3d02.xwm",
                            @"ARC\IMP\ArcImpPlayerMetal3d03.xwm",
                            @"ARC\IMP\ArcImpPlayerMetal3d04.xwm",
                            @"ARC\IMP\ArcImpPlayerMetal3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpPlayerPlayer3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpPlayerPlayer3d01.xwm",
                            @"ARC\IMP\ArcImpPlayerPlayer3d02.xwm",
                            @"ARC\IMP\ArcImpPlayerPlayer3d03.xwm",
                            @"ARC\IMP\ArcImpPlayerPlayer3d04.xwm",
                            @"ARC\IMP\ArcImpPlayerPlayer3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpPlayerRock3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpPlayerRock3d01.xwm",
                            @"ARC\IMP\ArcImpPlayerRock3d02.xwm",
                            @"ARC\IMP\ArcImpPlayerRock3d03.xwm",
                            @"ARC\IMP\ArcImpPlayerRock3d04.xwm",
                            @"ARC\IMP\ArcImpPlayerRock3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcImpRockRock3d",
                        Category = "IMP",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\IMP\ArcImpRockRock3d01.xwm",
                            @"ARC\IMP\ArcImpRockRock3d02.xwm",
                            @"ARC\IMP\ArcImpRockRock3d03.xwm",
                            @"ARC\IMP\ArcImpRockRock3d04.xwm",
                            @"ARC\IMP\ArcImpRockRock3d05.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindIdleLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindIdleLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindIdleLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayGrindIdleLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindIdleLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindMetalLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindMetalLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayGrindMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindOff2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindOff2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindOff3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolPlayGrindOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindOn2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindOn2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayGrindOn3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcToolPlayGrindOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayGrindOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayWeldIdleLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayWeldIdleLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayWeldIdleLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayWeldIdleLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayWeldIdleLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayWeldMetalLoop2d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayWeldMetalLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcToolPlayWeldMetalLoop3d",
                        Category = "TOOL",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcToolPlayWeldMetalLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\TOOL\ArcToolPlayWeldMetalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockGravityGenLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 24.2843819f,
                        Volume = 0.6337095f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockGravityGenLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockGravityGenOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockGravityGenOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockGravityGenOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockGravityGenOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryLoop2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 24.2843819f,
                        Volume = 0.5614341f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcBlockRafineryLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryOff2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryOff2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockRafineryOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryOn2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryOn2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockRafineryOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryProcess2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryProcess2d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRafineryProcess3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockRafineryProcess2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRafineryProcess3d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerLoop2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 24.2843819f,
                        Volume = 0.481931239f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = true,
                        Alternative2D = "ArcBlockAssemblerLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerOff2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerOff2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockAssemblerOff2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerOn2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerOn2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockAssemblerOn2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerProcess2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerProcess2d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockAssemblerProcess3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0f,
                        Loopable = false,
                        Alternative2D = "ArcBlockAssemblerProcess2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockAssemblerProcess3d01.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 24.2843819f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressEnd2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressEnd2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressEnd3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "ArcBlockMedicalProgressEnd2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressEnd3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressStart2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressStart2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressStart3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "ArcBlockMedicalProgressStart2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressStart3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressLoop2d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 2000f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressLoop2d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockMedicalProgressLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 50f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "ArcBlockMedicalProgressLoop2d",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockMedicalProgressLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorLrgLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 25f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorLrgLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorSmLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 9.828723f,
                        Volume = 0.7132123f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorSmLoop3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorLrgOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 25f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorLrgOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorSmOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 9.828723f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorSmOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorLrgOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 25f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorLrgOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockReactorSmOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 9.828723f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockReactorSmOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRotorIOff3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 40f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRotorIOff3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRotorIOn3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 40f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = false,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRotorOn3d.xwm"
                        }
                    },
                    new MyObjectBuilder_CueDefinition()
                    {
                        Name = "ArcBlockRotorLoop3d",
                        Category = "BLOCK",
                        VolumeCurve = MyAudioHelpers.CurveType.Custom_1,
                        MaxDistance = 40f,
                        Volume = 1f,
                        VolumeVariation = 0f,
                        PitchVariation = 0,
                        Loopable = true,
                        Alternative2D = "",
                        UseOcclusion = false,
                        Waves = new string[]
                        {
                            @"ARC\BLOCK\ArcBlockRotorLoop3d.xwm"
                        }
                    },
                }
            };

            MyObjectBuilder_Base.SerializeXML(GetFilenameSBA(), ob);
        }
    }
}

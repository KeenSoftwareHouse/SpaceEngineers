using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;


using Sandbox.Engine.Platform.VideoMode;
using VRage.Utils;


using Sandbox;
using VRageMath;
using Sandbox.Common;
using System.Xml.Serialization;
using VRage;

using VRage.Serialization;
using Sandbox.Graphics;

//  This class encapsulated read/write access to our config file - xxx.cfg - stored in user's local files
//  It assumes that config file may be non existing, or that some values may be missing or in wrong format - this class can handle it
//  and in such case will offer default values -> BUT YOU HAVE TO HELP IT... HOW? -> when writing getter from a new property,
//  you have to return default value in case it's null or empty or invalid!!
//  IMPORTANT: Never call get/set on this class properties from real-time code (during gameplay), e.g. don't do AddCue2D(cueEnum, MyConfig.VolumeMusic)
//  IMPORTANT: Only from loading and initialization methods.
using VRageRender;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using VRage.Audio;
using Sandbox.Game;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using ProtoBuf;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sandbox.Engine.Utils
{
    public class MyConfig : MyConfigBase, IMyConfig
    {
        //  Constants for mapping between our get/set properties and parameters inside the config file
        readonly string DX9_RENDER_QUALITY = "RenderQuality";
        readonly string MODEL_QUALITY = "ModelQuality";
        readonly string VOXEL_QUALITY = "VoxelQuality";
        readonly string FIELD_OF_VIEW = "FieldOfView";
        readonly string ENABLE_DAMAGE_EFFECTS = "EnableDamageEffects";
        readonly string RENDER_INTERPOLATION = "RenderInterpolation";
        readonly string SCREEN_WIDTH = "ScreenWidth";
        readonly string SCREEN_HEIGHT = "ScreenHeight";
        readonly string FULL_SCREEN = "FullScreen";
        readonly string VIDEO_ADAPTER = "VideoAdapter";
        readonly string VERTICAL_SYNC = "VerticalSync";
        readonly string REFRESH_RATE = "RefreshRate";
        readonly string HARDWARE_CURSOR = "HardwareCursor";
        readonly string GAME_VOLUME = "GameVolume";
        readonly string MUSIC_VOLUME = "MusicVolume";
        readonly string VOICE_CHAT_VOLUME = "VoiceChatVolume";
        readonly string LANGUAGE = "Language";
        readonly string SKIN = "Skin";
        readonly string CONTROLS_HINTS = "ControlsHints";
        readonly string ROTATION_HINTS = "RotationHints";
        readonly string ANIMATED_ROTATION = "AnimatedRotation";
        readonly string BUILDING_SIZE_HINT = "BuildingSizeHint";
        readonly string SHOW_CROSSHAIR = "ShowCrosshair";
        readonly string DISABLE_HEADBOB = "DisableHeadbob";
        readonly string CONTROLS_GENERAL = "ControlsGeneral";
        readonly string CONTROLS_BUTTONS = "ControlsButtons";
        readonly string SCREENSHOT_SIZE_MULTIPLIER = "ScreenshotSizeMultiplier";
        readonly string FIRST_TIME_RUN = "FirstTimeRun";
        readonly string SYNC_RENDERING = "SyncRendering";
        readonly string NEED_SHOW_BATTLE_TUTORIAL_QUESTION = "NeedShowBattleTutorialQuestion";
        readonly string DEBUG_INPUT_COMPONENTS = "DebugInputs";
        readonly string DEBUG_INPUT_COMPONENTS_INFO = "DebugComponentsInfo";
        readonly string MINIMAL_HUD = "MinimalHud";
        readonly string MEMORY_LIMITS = "MemoryLimits";
        readonly string CUBE_BUILDER_USE_SYMMETRY = "CubeBuilderUseSymmetry";
        readonly string CUBE_BUILDER_BUILDING_MODE = "CubeBuilderBuildingMode";
        readonly string MULTIPLAYER_SHOWCOMPATIBLE = "MultiplayerShowCompatible";
        readonly string COMPRESS_SAVE_GAMES = "CompressSaveGames";
        readonly string SHOW_PLAYER_NAMES_ON_HUD = "ShowPlayerNamesOnHud";
        readonly string RELEASING_ALT_RESETS_CAMERA = "ReleasingAltResetsCamera";
        readonly string ENABLE_PERFORMANCE_WARNINGS_TEMP = "EnablePerformanceWarningsTemp";
        readonly string LAST_CHECKED_VERSION = "LastCheckedVersion";
        readonly string WINDOW_MODE = "WindowMode";
        readonly string MOUSE_CAPTURE = "CaptureMouse";
        readonly string HUD_WARNINGS = "HudWarnings";
        readonly string DYNAMIC_MUSIC = "EnableDynamicMusic";
        readonly string SHIP_SOUNDS_SPEED = "ShipSoundsAreBasedOnSpeed";
        readonly string ANTIALIASING_MODE = "AntialiasingMode";
        readonly string SHADOW_MAP_RESOLUTION = "ShadowMapResolution";
        readonly string AMBIENT_OCCLUSION_ENABLED = "AmbientOcclusionEnabled";
        readonly string MULTITHREADED_RENDERING = "MultithreadedRendering";
        //readonly string TONEMAPPING = "Tonemapping";
        readonly string TEXTURE_QUALITY = "TextureQuality";
        readonly string ANISOTROPIC_FILTERING = "AnisotropicFiltering";
        readonly string FOLIAGE_DETAILS = "FoliageDetails";
        readonly string GRASS_DENSITY = "GrassDensity";
        readonly string VEGETATION_DISTANCE = "VegetationViewDistance";
        readonly string GRAPHICS_RENDERER = "GraphicsRenderer";
        readonly string ENABLE_VOICE_CHAT = "VoiceChat";
        readonly string ENABLE_MUTE_WHEN_NOT_IN_FOCUS = "EnableMuteWhenNotInFocus";
        readonly string ENABLE_REVERB = "EnableReverb";
        readonly string UI_TRANSPARENCY = "UiTransparency";
        readonly string UI_BK_TRANSPARENCY = "UiBkTransparency";
        readonly string TUTORIALS_FINISHED = "TutorialsFinished";
        readonly string MUTED_PLAYERS = "MutedPlayers";
        readonly string DONT_SEND_VOICE_PLAYERS = "DontSendVoicePlayers";
        readonly string LOW_MEM_SWITCH_TO_LOW = "LowMemSwitchToLow";
        readonly string NEWSLETTER_CURRENT_STATUS = "NewsletterCurrentStatus";

        public enum LowMemSwitch
        {
            ARMED = 0,
            TRIGGERED,
            USER_SAID_NO
        }

        public enum NewsletterStatus
        {
            Unknown = 0,
            NoFeedback,
            NotInterested,
            EmailNotConfirmed,
            EmailConfirmed
        }

        public MyConfig(string fileName)
            : base(fileName)
        {
        }
        
        public bool FirstTimeRun
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(FIRST_TIME_RUN), true);
            }
            set
            {
                SetParameterValue(FIRST_TIME_RUN, value);
            }
        }

        public bool SyncRendering
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(SYNC_RENDERING), false);
            }
            set
            {
                SetParameterValue(SYNC_RENDERING, value);
            }
        }

        public bool NeedShowBattleTutorialQuestion
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(NEED_SHOW_BATTLE_TUTORIAL_QUESTION), true);
            }

            set
            {
                SetParameterValue(NEED_SHOW_BATTLE_TUTORIAL_QUESTION, value);
            }
        }

        public MyRenderQualityEnum? Dx9RenderQuality
        {
            get
            {
                int? retInt = MyUtils.GetIntFromString(GetParameterValue(DX9_RENDER_QUALITY));
                if (retInt.HasValue && Enum.IsDefined(typeof(MyRenderQualityEnum), retInt.Value))
                    return (MyRenderQualityEnum)retInt.Value;
                else
                    return null;
            }

            set
            {
                SetParameterValue(DX9_RENDER_QUALITY, (int)value);
            }
        }

        public MyRenderQualityEnum? ModelQuality
        {
            get { return GetOptionalEnum<MyRenderQualityEnum>(MODEL_QUALITY); }
            set { SetOptionalEnum(MODEL_QUALITY, value); }
        }
        
        public MyRenderQualityEnum? VoxelQuality
        {
            get { return GetOptionalEnum<MyRenderQualityEnum>(VOXEL_QUALITY); }
            set { SetOptionalEnum(VOXEL_QUALITY, value); }
        }

        public bool RenderInterpolation
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(RENDER_INTERPOLATION), false);
            }

            set
            {
                SetParameterValue(RENDER_INTERPOLATION, value);
            }
        }

        public float GrassDensityFactor
        {
            get
            {
                return MyUtils.GetFloatFromString(GetParameterValue(GRASS_DENSITY), 1.0f);
            }

            set
            {
                SetParameterValue(GRASS_DENSITY, value);
            }
        }


        public float VegetationDrawDistance
        {
            get
            {
                return MyUtils.GetFloatFromString(GetParameterValue(VEGETATION_DISTANCE), 100);
            }

            set
            {
                SetParameterValue(VEGETATION_DISTANCE, value);
            }
        }

        public float FieldOfView
        {
            get
            {
                float? ret = MyUtils.GetFloatFromString(GetParameterValue(FIELD_OF_VIEW));
                if (ret.HasValue)
                {
                    // Loading value - load degrees and convert to radians
                    return VRageMath.MathHelper.ToRadians(ret.Value);
                }
                else
                {
                    // In radians
                    return MyConstants.FIELD_OF_VIEW_CONFIG_DEFAULT;
                }
            }
            set
            {
                // Saving value - save as degrees
                SetParameterValue(FIELD_OF_VIEW, VRageMath.MathHelper.ToDegrees(value));
            }
        }

        public MyAntialiasingMode? AntialiasingMode
        {
            get { return GetOptionalEnum<MyAntialiasingMode>(ANTIALIASING_MODE); }
            set { SetOptionalEnum(ANTIALIASING_MODE, value); }
        }

        public MyShadowsQuality? ShadowQuality
        {
            get { return GetOptionalEnum<MyShadowsQuality>(SHADOW_MAP_RESOLUTION); }
            set { SetOptionalEnum(SHADOW_MAP_RESOLUTION, value); }
        }

        public bool? AmbientOcclusionEnabled
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(AMBIENT_OCCLUSION_ENABLED)); }
            set { SetParameterValue(AMBIENT_OCCLUSION_ENABLED, value); }
        }

        public MyTextureQuality? TextureQuality
        {
            get { return GetOptionalEnum<MyTextureQuality>(TEXTURE_QUALITY); }
            set { SetOptionalEnum(TEXTURE_QUALITY, value); }
        }

        public MyTextureAnisoFiltering? AnisotropicFiltering
        {
            get { return GetOptionalEnum<MyTextureAnisoFiltering>(ANISOTROPIC_FILTERING); }
            set { SetOptionalEnum(ANISOTROPIC_FILTERING, value); }
        }

        public MyFoliageDetails? FoliageDetails
        {
            get { return GetOptionalEnum<MyFoliageDetails>(FOLIAGE_DETAILS); }
            set { SetOptionalEnum(FOLIAGE_DETAILS, value); }
        }

        //public bool? Tonemapping
        //{
        //    get { return MyUtils.GetBoolFromString(GetParameterValue(TONEMAPPING)); }
        //    set
        //    {
        //        if (value.HasValue)
        //            SetParameterValue(TONEMAPPING, value.Value);
        //        else
        //            RemoveParameterValue(TONEMAPPING);
        //    }
        //}

        public int? ScreenWidth
        {
            get { return MyUtils.GetInt32FromString(GetParameterValue(SCREEN_WIDTH)); }
            set { SetParameterValue(SCREEN_WIDTH, value); }
        }

        public int? ScreenHeight
        {
            get { return MyUtils.GetInt32FromString(GetParameterValue(SCREEN_HEIGHT)); }
            set { SetParameterValue(SCREEN_HEIGHT, value); }
        }

        public int VideoAdapter
        {
            get { return MyUtils.GetIntFromString(GetParameterValue(VIDEO_ADAPTER), 0); }
            set { SetParameterValue(VIDEO_ADAPTER, value); }
        }

        public MyWindowModeEnum WindowMode
        {
            get
            {
                // Backward compatibility reading. Can be removed when Space Engineers is no longer supported.
                var paramValue = GetParameterValue(WINDOW_MODE);
                byte? retByte = null;
                if (!string.IsNullOrEmpty(paramValue))
                {
                    retByte = MyUtils.GetByteFromString(paramValue);
                }
                else
                {
                    bool? fullscreen = MyUtils.GetBoolFromString(GetParameterValue(FULL_SCREEN));
                    if (fullscreen.HasValue)
                    {
                        RemoveParameterValue(FULL_SCREEN);
                        retByte = (byte)(fullscreen.Value ? MyWindowModeEnum.Fullscreen : MyWindowModeEnum.Window);
                        SetParameterValue(WINDOW_MODE, retByte.Value);
                    }
                }

                if ((retByte.HasValue == false) || (Enum.IsDefined(typeof(MyWindowModeEnum), retByte) == false))
                {
                    return MyWindowModeEnum.Fullscreen;
                }
                else
                {
                    return (MyWindowModeEnum)retByte.Value;
                }
            }

            set
            {
                SetParameterValue(WINDOW_MODE, (byte)value);
            }
        }

        public bool CaptureMouse
        {
            get
            {
                var paramValue = GetParameterValue(MOUSE_CAPTURE);
                if (paramValue.Equals("False")) return false;
                return true;
            }
            set
            {
                SetParameterValue(MOUSE_CAPTURE, value.ToString());
            }
        }

        public bool VerticalSync
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(VERTICAL_SYNC), false); }
            set { SetParameterValue(VERTICAL_SYNC, value); }
        }

        public int RefreshRate
        {
            get { return MyUtils.GetIntFromString(GetParameterValue(REFRESH_RATE), 0); }
            set { SetParameterValue(REFRESH_RATE, value); }
        }

        public bool HardwareCursor
        {
            get
            {
                // Hardware cursor is always disabled for OnLive
                // HW cursor is also disabled on AMD+ATI cards
                return MyFinalBuildConstants.IS_CLOUD_GAMING ? false : MyUtils.GetBoolFromString(GetParameterValue(HARDWARE_CURSOR), true);
            }

            set
            {
                SetParameterValue(HARDWARE_CURSOR, value);
            }
        }

        public bool EnableDamageEffects
        {
            get
            {
                // Hardware cursor is always disabled for OnLive
                // HW cursor is also disabled on AMD+ATI cards
                return MyUtils.GetBoolFromString(GetParameterValue(ENABLE_DAMAGE_EFFECTS), true);
            }

            set
            {
                SetParameterValue(ENABLE_DAMAGE_EFFECTS, value);
            }
        }
        public float GameVolume
        {
            get
            {
                return MyUtils.GetFloatFromString(GetParameterValue(GAME_VOLUME), MyAudioConstants.GAME_MASTER_VOLUME_MAX);
            }

            set
            {
                SetParameterValue(GAME_VOLUME, value);
            }
        }

        public float MusicVolume
        {
            get
            {
                return MyUtils.GetFloatFromString(GetParameterValue(MUSIC_VOLUME), MyAudioConstants.MUSIC_MASTER_VOLUME_MAX);
            }
            set
            {
                SetParameterValue(MUSIC_VOLUME, value);
            }
        }

        public float VoiceChatVolume
        {
            get
            {
                return MyUtils.GetFloatFromString(GetParameterValue(VOICE_CHAT_VOLUME), MyAudioConstants.VOICE_CHAT_VOLUME_MAX);
            }
            set
            {
                SetParameterValue(VOICE_CHAT_VOLUME, value);
            }
        }

        public bool ControlsHints
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(CONTROLS_HINTS), true);
            }
            set
            {
                SetParameterValue(CONTROLS_HINTS, value);
            }
        }

        public bool RotationHints
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(ROTATION_HINTS), true);
            }
            set
            {
                SetParameterValue(ROTATION_HINTS, value);
            }
        }

        public bool AnimatedRotation
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(ANIMATED_ROTATION), true);
            }
            set
            {
                SetParameterValue(ANIMATED_ROTATION, value);
            }
        }

        public bool ShowBuildingSizeHint
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(BUILDING_SIZE_HINT), true);
            }
            set
            {
                SetParameterValue(BUILDING_SIZE_HINT, value);
            }
        }

        public bool ShowCrosshair
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(SHOW_CROSSHAIR), true);
            }
            set
            {
                SetParameterValue(SHOW_CROSSHAIR, value);
            }
        }

        public bool DisableHeadbob
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(DISABLE_HEADBOB), false);
            }
            set
            {
                SetParameterValue(DISABLE_HEADBOB, value);
            }
        }

        public float ScreenshotSizeMultiplier
        {
            get
            {
                if (string.IsNullOrEmpty(GetParameterValue(SCREENSHOT_SIZE_MULTIPLIER)))
                {
                    SetParameterValue(SCREENSHOT_SIZE_MULTIPLIER, 1.0f);
                    Save();
                }

                return MyUtils.GetFloatFromString(GetParameterValue(SCREENSHOT_SIZE_MULTIPLIER), 1.0f);
            }

            set
            {
                SetParameterValue(SCREENSHOT_SIZE_MULTIPLIER, value);
            }
        }

        public MyLanguagesEnum Language
        {
            get
            {
                byte? retByte = MyUtils.GetByteFromString(GetParameterValue(LANGUAGE));

                if ((retByte.HasValue == false) || (Enum.IsDefined(typeof(MyLanguagesEnum), retByte) == false))
                {
                    return MyLanguagesEnum.English;
                }
                else
                {
                    return (MyLanguagesEnum)retByte.Value;
                }
            }

            set
            {
                SetParameterValue(LANGUAGE, (byte)value);
            }
        }

        public string Skin
        {
            get
            {
                if (string.IsNullOrEmpty(GetParameterValue(SKIN)))
                {
                    SetParameterValue(SKIN, "Default");
                    Save();
                }

                return GetParameterValue(SKIN);
            }

            set
            {
                SetParameterValue(SKIN, value);
            }
        }

        public SerializableDictionary<string, object> ControlsGeneral
        {
            get
            {
                if (!m_values.Dictionary.ContainsKey(CONTROLS_GENERAL))
                    m_values.Dictionary.Add(CONTROLS_GENERAL, new SerializableDictionary<string, object>());

                return GetParameterValueDictionary(CONTROLS_GENERAL);
            }

            set
            {
                System.Diagnostics.Debug.Assert(false);
                //SetParameterValue(CONTROLS_GENERAL, value);
            }
        }

        public SerializableDictionary<string, object> ControlsButtons
        {
            get
            {
                if (!m_values.Dictionary.ContainsKey(CONTROLS_BUTTONS))
                    m_values.Dictionary.Add(CONTROLS_BUTTONS, new SerializableDictionary<string, object>());

                return GetParameterValueDictionary(CONTROLS_BUTTONS);
            }

            set
            {
                System.Diagnostics.Debug.Assert(false);
                //SetParameterValue(CONTROLS_BUTTONS, value);
            }
        }

        [ProtoContract]
        public struct MyDebugInputData
        {
            [ProtoMember]
            public bool Enabled;

            [ProtoMember]
            public string SerializedData;

            public object Data
            {
                get { return Decode64AndDeserialize(SerializedData); }
                set { SerializedData = SerialiazeAndEncod64(value); }
            }

            public bool ShouldSerializeData()
            {
                return false;
            }
        }

        private static string SerialiazeAndEncod64(object p)
        {
            if (p == null) return "";
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, p);

            return Convert.ToBase64String(ms.GetBuffer());
        }

        private static object Decode64AndDeserialize(string p)
        {
            if (p == null || p.Length == 0) return null;

            byte[] bytes = Convert.FromBase64String(p);

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(bytes);
            return bf.Deserialize(ms);
        }

        public SerializableDictionary<string, MyDebugInputData> DebugInputComponents
        {
            get
            {
                if (!m_values.Dictionary.ContainsKey(DEBUG_INPUT_COMPONENTS))
                    m_values.Dictionary.Add(DEBUG_INPUT_COMPONENTS, new SerializableDictionary<string, MyDebugInputData>());
                else if (!(m_values.Dictionary[DEBUG_INPUT_COMPONENTS] is SerializableDictionary<string, MyDebugInputData>))
                {
                    m_values.Dictionary[DEBUG_INPUT_COMPONENTS] = new SerializableDictionary<string, MyDebugInputData>();
                }

                return GetParameterValueT<SerializableDictionary<string, MyDebugInputData>>(DEBUG_INPUT_COMPONENTS);
            }

            set
            {
                System.Diagnostics.Debug.Assert(false);
                //SetParameterValue(CONTROLS_GENERAL, value);
            }
        }

        public MyDebugComponent.MyDebugComponentInfoState DebugComponentsInfo
        {
            get
            {
                int? retInt = MyUtils.GetIntFromString(GetParameterValue(DEBUG_INPUT_COMPONENTS_INFO));
                if ((retInt.HasValue == false) || (Enum.IsDefined(typeof(MyDebugComponent.MyDebugComponentInfoState), retInt) == false))
                {
                    return MyDebugComponent.MyDebugComponentInfoState.EnabledInfo;
                }
                else
                {
                    return (MyDebugComponent.MyDebugComponentInfoState)retInt.Value;
                }
            }

            set
            {
                SetParameterValue(DEBUG_INPUT_COMPONENTS_INFO, (int)value);
            }
        }

        public bool MinimalHud
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(MINIMAL_HUD), false);
            }

            set
            {
                SetParameterValue(MINIMAL_HUD, value);
            }
        }

        public bool MemoryLimits
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(MEMORY_LIMITS), true);
            }

            set
            {
                SetParameterValue(MEMORY_LIMITS, value);
            }
        }

        public bool CubeBuilderUseSymmetry
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(CUBE_BUILDER_USE_SYMMETRY), true);
            }

            set
            {
                SetParameterValue(CUBE_BUILDER_USE_SYMMETRY, value);
            }
        }

        public int CubeBuilderBuildingMode
        {
            get
            {
                return MyUtils.GetIntFromString(GetParameterValue(CUBE_BUILDER_BUILDING_MODE), 0);
            }

            set
            {
                SetParameterValue(CUBE_BUILDER_BUILDING_MODE, value);
            }
        }

        public bool MultiplayerShowCompatible
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(MULTIPLAYER_SHOWCOMPATIBLE), true);
            }

            set
            {
                SetParameterValue(MULTIPLAYER_SHOWCOMPATIBLE, value);
            }
        }

        public bool CompressSaveGames
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(COMPRESS_SAVE_GAMES), MyFakes.GAME_SAVES_COMPRESSED_BY_DEFAULT);
            }

            set
            {
                SetParameterValue(COMPRESS_SAVE_GAMES, value);
            }
        }

        public bool ShowPlayerNamesOnHud
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(SHOW_PLAYER_NAMES_ON_HUD), true); }
            set { SetParameterValue(SHOW_PLAYER_NAMES_ON_HUD, value); }
        }

        public bool ReleasingAltResetsCamera
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(RELEASING_ALT_RESETS_CAMERA), true); }
            set { SetParameterValue(RELEASING_ALT_RESETS_CAMERA, value); }
        }

        public bool EnablePerformanceWarnings
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(ENABLE_PERFORMANCE_WARNINGS_TEMP), false); }
            set { SetParameterValue(ENABLE_PERFORMANCE_WARNINGS_TEMP, value); }
        }

        public int LastCheckedVersion
        {
            get
            {
                return MyUtils.GetIntFromString(GetParameterValue(LAST_CHECKED_VERSION), 0);
            }

            set
            {
                SetParameterValue(LAST_CHECKED_VERSION, value);
            }
        }

        public float UIOpacity
        {
            get { return MyUtils.GetFloatFromString(GetParameterValue(UI_TRANSPARENCY), 1.0f); }
            set { SetParameterValue(UI_TRANSPARENCY, value); }
        }

        public float UIBkOpacity
        {
            get { return MyUtils.GetFloatFromString(GetParameterValue(UI_BK_TRANSPARENCY), 1.0f); }
            set { SetParameterValue(UI_BK_TRANSPARENCY, value); }
        }

        public List<string> TutorialsFinished
        {
            get
            {
                if (!m_values.Dictionary.ContainsKey(TUTORIALS_FINISHED))
                    m_values.Dictionary.Add(TUTORIALS_FINISHED, new List<string>());
                return GetParameterValueT <List<string>>(TUTORIALS_FINISHED);
            }
            set
            {
                System.Diagnostics.Debug.Assert(false);
                //SetParameterValue(TUTORIALS_UNLOCKED, value);
            }
        }

        public bool HudWarnings

        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(HUD_WARNINGS), true); }
            set { SetParameterValue(HUD_WARNINGS, value); }
        }

        public bool EnableVoiceChat
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(ENABLE_VOICE_CHAT), true); }
            set { SetParameterValue(ENABLE_VOICE_CHAT, value); }
        }

        public bool EnableMuteWhenNotInFocus
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(ENABLE_MUTE_WHEN_NOT_IN_FOCUS), true); }
            set { SetParameterValue(ENABLE_MUTE_WHEN_NOT_IN_FOCUS, value); }
        }

        public bool EnableDynamicMusic
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(DYNAMIC_MUSIC), true); }
            set { SetParameterValue(DYNAMIC_MUSIC, value); }
        }

        public bool ShipSoundsAreBasedOnSpeed
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(SHIP_SOUNDS_SPEED), true); }
            set { SetParameterValue(SHIP_SOUNDS_SPEED, value); }
        }

        public bool EnableReverb
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(ENABLE_REVERB), true); }
            set { SetParameterValue(ENABLE_REVERB, value); }
        }

        public MyStringId GraphicsRenderer
        {
            get
            {
                var id = MyStringId.TryGet(GetParameterValue(GRAPHICS_RENDERER));
                if (id != MyStringId.NullOrEmpty)
                    return id;
                else
                    return MyPerGameSettings.DefaultGraphicsRenderer;
            }
            set
            {
                SetParameterValue(GRAPHICS_RENDERER, value.ToString());
            }
        }

        HashSet<ulong> GetSeparatedValues(string key, ref HashSet<ulong> cache, ref bool cacheInitedFlag)
        {
            if (cacheInitedFlag)
                // cached value is returned
                return cache;

            // getting of value
            string list = "";
            if (!m_values.Dictionary.ContainsKey(key))
                m_values.Dictionary.Add(key, "");
            else
                list = GetParameterValue(key);

            HashSet<ulong> ret = new HashSet<ulong>();
            // parsing
            string[] values = list.Split(m_numberSeparator);
            foreach (string num in values)
                if (num.Length > 0)
                    ret.Add(Convert.ToUInt64(num));

            cache = ret;   // caching of value
            cacheInitedFlag = true;
            return ret;
        }
        void SetSeparatedValues(string key, HashSet<ulong> value, ref HashSet<ulong> cache, ref bool cacheInitedFlag)
        {
            // caching of actual value
            cache = value;

            // storing of value
            string val = "";
            foreach (ulong id in value)
            {
                val += id.ToString() + m_numberSeparator;
            }
            SetParameterValue(key, val);
        }


        const char m_numberSeparator = ',';                             // separator between player ids
        private HashSet<ulong> m_mutedPlayers = new HashSet<ulong>();   // cached muted players for quicker access
        private bool m_mutedPlayersInited = false;                      // initialization flag
        public HashSet<ulong> MutedPlayers
        {
            get
            {
                return GetSeparatedValues(MUTED_PLAYERS, ref m_mutedPlayers, ref m_mutedPlayersInited);
            }
            set
            {
                SetSeparatedValues(MUTED_PLAYERS, value, ref m_mutedPlayers, ref m_mutedPlayersInited);
            }

            //get 
            //{
            //    if (m_mutedPlayersInited)
            //        // cached value is returned
            //        return m_mutedPlayers;

            //    // getting of value
            //    string playerList = "";
            //    if (!m_values.Dictionary.ContainsKey(MUTED_PLAYERS))
            //        m_values.Dictionary.Add(MUTED_PLAYERS, "");
            //    else
            //        playerList = GetParameterValue(MUTED_PLAYERS);

            //    HashSet<ulong> players = new HashSet<ulong>();
            //    // parsing
            //    string[] values = playerList.Split(m_numberSeparator);
            //    foreach (string num in values)
            //        if ( num.Length > 0 )
            //            players.Add(Convert.ToUInt64(num));

            //    m_mutedPlayers = players;   // caching of value
            //    m_mutedPlayersInited = true;
            //    return players;
            //}
            //set
            //{
            //    // caching of actual value
            //    m_mutedPlayers = value;

            //    // storing of value
            //    string val = "";
            //    foreach (ulong id in value)
            //    {
            //        val += id.ToString() + m_numberSeparator;
            //    }
            //    SetParameterValue(MUTED_PLAYERS, val);
            //}
        }



        private HashSet<ulong> m_dontSendVoicePlayers = new HashSet<ulong>();   // cached players that don't want receive voice from this player 
        private bool m_dontSendVoicePlayersInited = false;                      // initialization flag
        public HashSet<ulong> DontSendVoicePlayers // players that don't want receive voice from this player
        {
            get
            {
                return GetSeparatedValues(DONT_SEND_VOICE_PLAYERS, ref m_dontSendVoicePlayers, ref m_dontSendVoicePlayersInited);
            }
            set
            {
                SetSeparatedValues(DONT_SEND_VOICE_PLAYERS, value, ref m_dontSendVoicePlayers, ref m_dontSendVoicePlayersInited);
            }
        }

        public LowMemSwitch LowMemSwitchToLow
        {
            get
            {
                return (LowMemSwitch)MyUtils.GetIntFromString(GetParameterValue(LOW_MEM_SWITCH_TO_LOW), (int)LowMemSwitch.ARMED);
            }

            set
            {
                SetParameterValue(LOW_MEM_SWITCH_TO_LOW, (int)value);
            }
        }

        public NewsletterStatus NewsletterCurrentStatus
        {
            get
            {
                return (NewsletterStatus)MyUtils.GetIntFromString(GetParameterValue(NEWSLETTER_CURRENT_STATUS), (int)NewsletterStatus.Unknown);
            }
            set
            {
                SetParameterValue(NEWSLETTER_CURRENT_STATUS, (int)value);
            }
        }

        public bool IsSetToLowQuality()
        {
            if (AnisotropicFiltering == MyTextureAnisoFiltering.NONE &&
                AntialiasingMode == MyAntialiasingMode.NONE &&
                FoliageDetails == MyFoliageDetails.DISABLED &&
                ShadowQuality == MyShadowsQuality.LOW &&
                TextureQuality == MyTextureQuality.LOW &&
                Dx9RenderQuality == MyRenderQualityEnum.LOW &&
                ModelQuality == MyRenderQualityEnum.LOW &&
                VoxelQuality == MyRenderQualityEnum.LOW)
                return true;
            return false;
        }
        public void SetToLowQuality()
        {
            AnisotropicFiltering = MyTextureAnisoFiltering.NONE;
            AntialiasingMode = MyAntialiasingMode.NONE;
            FoliageDetails = MyFoliageDetails.DISABLED;
            ShadowQuality = MyShadowsQuality.LOW;
            TextureQuality = MyTextureQuality.LOW;
            Dx9RenderQuality = MyRenderQualityEnum.LOW;
            ModelQuality = MyRenderQualityEnum.LOW;
            VoxelQuality = MyRenderQualityEnum.LOW;
        }

        
        #region ModAPI
        MyTextureAnisoFiltering? IMyConfig.AnisotropicFiltering
        {
            get { return AnisotropicFiltering; }
        }

        MyAntialiasingMode? IMyConfig.AntialiasingMode
        {
            get { return AntialiasingMode; }
        }

        bool IMyConfig.CompressSaveGames
        {
            get { return CompressSaveGames; }
        }

        SerializableDictionary<string, object> IMyConfig.ControlsButtons
        {
            get { return ControlsButtons; }
        }

        SerializableDictionary<string, object> IMyConfig.ControlsGeneral
        {
            get { return ControlsGeneral; }
        }

        bool IMyConfig.ControlsHints
        {
            get { return ControlsHints; }
        }

        int IMyConfig.CubeBuilderBuildingMode
        {
            get { return CubeBuilderBuildingMode; }
        }

        bool IMyConfig.CubeBuilderUseSymmetry
        {
            get { return CubeBuilderUseSymmetry; }
        }

        SerializableDictionary<string, object> IMyConfig.DebugInputComponents
        {
            get { return null; }
        }

        bool IMyConfig.DisableHeadbob
        {
            get { return DisableHeadbob; }
        }

        bool IMyConfig.EnableDamageEffects
        {
            get { return EnableDamageEffects; }
        }

        float IMyConfig.FieldOfView
        {
            get { return FieldOfView; }
        }

        MyFoliageDetails? IMyConfig.FoliageDetails
        {
            get { return FoliageDetails; }
        }

        float IMyConfig.GameVolume
        {
            get { return GameVolume; }
        }

        bool IMyConfig.HardwareCursor
        {
            get { return HardwareCursor; }
        }

        bool IMyConfig.HudWarnings
        {
            get { return HudWarnings; }
        }

        VRage.MyLanguagesEnum IMyConfig.Language
        {
            get { return Language; }
        }

        bool IMyConfig.MemoryLimits
        {
            get { return MemoryLimits; }
        }

        bool IMyConfig.MinimalHud
        {
            get { return MinimalHud; }
        }

        float IMyConfig.MusicVolume
        {
            get { return MusicVolume; }
        }

        int IMyConfig.RefreshRate
        {
            get { return RefreshRate; }
        }

        bool IMyConfig.RenderInterpolation
        {
            get { return RenderInterpolation; }
        }

        MyRenderQualityEnum? IMyConfig.RenderQuality
        {
            get { return Dx9RenderQuality; }
        }

        MyGraphicsRenderer IMyConfig.GraphicsRenderer
        {
            get
            {
                var id = MyStringId.TryGet(GetParameterValue(GRAPHICS_RENDERER));

                if (id == MySandboxGame.DirectX11RendererKey)
                    return MyGraphicsRenderer.DX11;

                return MyGraphicsRenderer.NONE;
            }
        }

        bool IMyConfig.RotationHints
        {
            get { return RotationHints; }
        }

        int? IMyConfig.ScreenHeight
        {
            get { return ScreenHeight; }
        }

        int? IMyConfig.ScreenWidth
        {
            get { return ScreenWidth; }
        }

        MyShadowsQuality? IMyConfig.ShadowQuality
        {
            get { return ShadowQuality; }
        }

        bool IMyConfig.ShowCrosshair
        {
            get { return ShowCrosshair; }
        }

        bool IMyConfig.ShowPlayerNamesOnHud
        {
            get { return ShowPlayerNamesOnHud; }
        }

        MyTextureQuality? IMyConfig.TextureQuality
        {
            get { return TextureQuality; }
        }

        bool IMyConfig.VerticalSync
        {
            get { return VerticalSync; }
        }

        int IMyConfig.VideoAdapter
        {
            get { return VideoAdapter; }
        }

        MyWindowModeEnum IMyConfig.WindowMode
        {
            get { return WindowMode; }
        }

        bool IMyConfig.CaptureMouse
        {
            get { return CaptureMouse; }
        }
        #endregion
    }
}
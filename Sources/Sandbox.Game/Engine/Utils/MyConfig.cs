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
using Sandbox.Graphics.Render;
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
using VRage;
using VRage.Audio;

namespace Sandbox.Engine.Utils
{
    public class MyConfig : MyConfigBase, Sandbox.ModAPI.IMyConfig
    {
        //  Constants for mapping between our get/set properties and parameters inside the config file
        readonly string USERNAME = "Username";
        readonly string PASSWORD = "Password";
        readonly string LAST_LOGIN_WAS_SUCCESSFUL = "LastLoginWasSuccessful";
        readonly string REMEBER_USERNAME_AND_PASSWORD = "RememberUsernameAndPassword";
        readonly string AUTOLOGIN = "Autologin";
        readonly string DX9_RENDER_QUALITY = "RenderQuality";
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
        readonly string LANGUAGE = "Language";
        readonly string CONTROLS_HINTS = "ControlsHints";
        readonly string ROTATION_HINTS = "RotationHints";
        readonly string SHOW_CROSSHAIR = "ShowCrosshair";
        readonly string DISABLE_HEADBOB = "DisableHeadbob";
        readonly string CONTROLS_GENERAL = "ControlsGeneral";
        readonly string CONTROLS_BUTTONS = "ControlsButtons";
        readonly string SCREENSHOT_SIZE_MULTIPLIER = "ScreenshotSizeMultiplier";
        readonly string LAST_FRIEND_NAME = "LastFriendSectorName";
        readonly string LAST_FRIEND_SECTOR_USER_ID = "LastFriendSectorUserId";
        readonly string LAST_FRIEND_SECTOR_POSITION = "LastFriendSectorPosition";
        readonly string LAST_MY_SANDBOX_SECTOR = "LastMySandboxSector";
        readonly string NEED_SHOW_TUTORIAL_QUESTION = "NeedShowTutorialQuestion";
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
        readonly string LAST_CHECKED_VERSION = "LastCheckedVersion";
        readonly string WINDOW_MODE = "WindowMode";
        readonly string HUD_WARNINGS = "HudWarnings";
        readonly string ANTIALIASING_MODE = "AntialiasingMode";
        readonly string SHADOW_MAP_RESOLUTION = "ShadowMapResolution";
        readonly string MULTITHREADED_RENDERING = "MultithreadedRendering";
        readonly string TONEMAPPING = "Tonemapping";
        readonly string TEXTURE_QUALITY = "TextureQuality";
        readonly string ANISOTROPIC_FILTERING = "AnisotropicFiltering";
        readonly string FOLIAGE_DETAILS = "FoliageDetails";
        readonly string GRAPHICS_RENDERER = "GraphicsRenderer";
        readonly string ENABLE_VOICE_CHAT = "VoiceChat";
        readonly string UI_TRANSPARENCY = "UiTransparency";
        readonly string UI_BK_TRANSPARENCY = "UiBkTransparency";

        public MyConfig(string fileName)
            : base(fileName)
        {
        }

        public Vector3I LastSandboxSector
        {
            get
            {
                return GetParameterValueVector3I(LAST_MY_SANDBOX_SECTOR);
            }
            set
            {
                LastFriendSectorUserId = null;
                SetParameterValue(LAST_MY_SANDBOX_SECTOR, value);
            }
        }

        public string LastFriendName
        {
            get
            {
                return GetParameterValue(LAST_FRIEND_NAME);
            }
            set
            {
                SetParameterValue(LAST_FRIEND_NAME, value);
            }
        }

        public Vector3I LastFriendSectorPosition
        {
            get
            {
                return GetParameterValueVector3I(LAST_FRIEND_SECTOR_POSITION);
            }
            set
            {
                SetParameterValue(LAST_FRIEND_SECTOR_POSITION, value);
            }
        }

        public int? LastFriendSectorUserId
        {
            get
            {
                int result;
                if(int.TryParse(GetParameterValue(LAST_FRIEND_SECTOR_USER_ID), out result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SetParameterValue(LAST_FRIEND_SECTOR_USER_ID, value);
            }
        }

        public bool NeedShowTutorialQuestion
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(NEED_SHOW_TUTORIAL_QUESTION), true);
            }

            set
            {
                SetParameterValue(NEED_SHOW_TUTORIAL_QUESTION, value);
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

        public string Username
        {
            get
            {
                return GetParameterValue(USERNAME);
            }

            set
            {
                SetParameterValue(USERNAME, value);
            }
        }

        //  This property accepts password in a decrypted form, but then stores it in memory and file in an encrypted form
        //  It also returns password in a decrypted form.
        public string Password
        {
            get
            {
                return MyEncryptionSymmetricRijndael.DecryptString(GetParameterValue(PASSWORD), MyConfigConstants.SYMMETRIC_PASSWORD);
            }

            set
            {
                SetParameterValue(PASSWORD, MyEncryptionSymmetricRijndael.EncryptString(value, MyConfigConstants.SYMMETRIC_PASSWORD));
            }
        }

        public bool LastLoginWasSuccessful
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(LAST_LOGIN_WAS_SUCCESSFUL), false);
            }

            set
            {
                SetParameterValue(LAST_LOGIN_WAS_SUCCESSFUL, value);
            }
        }

        public bool RememberUsernameAndPassword
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(REMEBER_USERNAME_AND_PASSWORD), true);
            }

            set
            {
                SetParameterValue(REMEBER_USERNAME_AND_PASSWORD, value);
            }
        }

        public bool Autologin
        {
            get
            {
                return MyUtils.GetBoolFromString(GetParameterValue(AUTOLOGIN), true);
            }

            set
            {
                SetParameterValue(AUTOLOGIN, value);
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

        public bool? MultithreadedRendering
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(MULTITHREADED_RENDERING)); }
            set
            {
                if (value.HasValue)
                    SetParameterValue(MULTITHREADED_RENDERING, value.Value);
                else
                    RemoveParameterValue(MULTITHREADED_RENDERING);
            }
        }

        public bool? Tonemapping
        {
            get { return MyUtils.GetBoolFromString(GetParameterValue(TONEMAPPING)); }
            set
            {
                if (value.HasValue)
                    SetParameterValue(TONEMAPPING, value.Value);
                else
                    RemoveParameterValue(TONEMAPPING);
            }
        }

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

        public SerializableDictionary<string, object> DebugInputComponents
        {
            get
            {
                if (!m_values.Dictionary.ContainsKey(DEBUG_INPUT_COMPONENTS))
                    m_values.Dictionary.Add(DEBUG_INPUT_COMPONENTS, new SerializableDictionary<string, object>());

                return GetParameterValueDictionary(DEBUG_INPUT_COMPONENTS);
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

        public float UITransparency
        {
            get { return MyUtils.GetFloatFromString(GetParameterValue(UI_TRANSPARENCY), 1.0f); }
            set { SetParameterValue(UI_TRANSPARENCY, value); }
        }

        public float UIBkTransparency
        {
            get { return MyUtils.GetFloatFromString(GetParameterValue(UI_BK_TRANSPARENCY), 1.0f); }
            set { SetParameterValue(UI_BK_TRANSPARENCY, value); }
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

        public MyStringId? GraphicsRenderer
        {
            get
            {
                var id = MyStringId.TryGet(GetParameterValue(GRAPHICS_RENDERER));
                if (id != MyStringId.NullOrEmpty)
                    return id;
                else
                    return null;
            }
            set
            {
                if (value.HasValue)
                    SetParameterValue(GRAPHICS_RENDERER, value.Value.ToString());
                else
                    RemoveParameterValue(GRAPHICS_RENDERER);
            }
        }

        #region ModAPI
        MyTextureAnisoFiltering? ModAPI.IMyConfig.AnisotropicFiltering
        {
            get { return AnisotropicFiltering; }
        }

        MyAntialiasingMode? ModAPI.IMyConfig.AntialiasingMode
        {
            get { return AntialiasingMode; }
        }

        bool ModAPI.IMyConfig.CompressSaveGames
        {
            get { return CompressSaveGames; }
        }

        SerializableDictionary<string, object> ModAPI.IMyConfig.ControlsButtons
        {
            get { return ControlsButtons; }
        }

        SerializableDictionary<string, object> ModAPI.IMyConfig.ControlsGeneral
        {
            get { return ControlsGeneral; }
        }

        bool ModAPI.IMyConfig.ControlsHints
        {
            get { return ControlsHints; }
        }

        int ModAPI.IMyConfig.CubeBuilderBuildingMode
        {
            get { return CubeBuilderBuildingMode; }
        }

        bool ModAPI.IMyConfig.CubeBuilderUseSymmetry
        {
            get { return CubeBuilderUseSymmetry; }
        }

        SerializableDictionary<string, object> ModAPI.IMyConfig.DebugInputComponents
        {
            get { return DebugInputComponents; }
        }

        bool ModAPI.IMyConfig.DisableHeadbob
        {
            get { return DisableHeadbob; }
        }

        bool ModAPI.IMyConfig.EnableDamageEffects
        {
            get { return EnableDamageEffects; }
        }

        float ModAPI.IMyConfig.FieldOfView
        {
            get { return FieldOfView; }
        }

        MyFoliageDetails? ModAPI.IMyConfig.FoliageDetails
        {
            get { return FoliageDetails; }
        }

        float ModAPI.IMyConfig.GameVolume
        {
            get { return GameVolume; }
        }

        bool ModAPI.IMyConfig.HardwareCursor
        {
            get { return HardwareCursor; }
        }

        bool ModAPI.IMyConfig.HudWarnings
        {
            get { return HudWarnings; }
        }

        VRage.MyLanguagesEnum ModAPI.IMyConfig.Language
        {
            get { return Language; }
        }

        bool ModAPI.IMyConfig.MemoryLimits
        {
            get { return MemoryLimits; }
        }

        bool ModAPI.IMyConfig.MinimalHud
        {
            get { return MinimalHud; }
        }

        bool? ModAPI.IMyConfig.MultithreadedRendering
        {
            get { return MultithreadedRendering; }
        }

        float ModAPI.IMyConfig.MusicVolume
        {
            get { return MusicVolume; }
        }

        bool ModAPI.IMyConfig.NeedShowTutorialQuestion
        {
            get { return NeedShowTutorialQuestion; }
        }

        int ModAPI.IMyConfig.RefreshRate
        {
            get { return RefreshRate; }
        }

        bool ModAPI.IMyConfig.RenderInterpolation
        {
            get { return RenderInterpolation; }
        }

        MyRenderQualityEnum? ModAPI.IMyConfig.RenderQuality
        {
            get { return Dx9RenderQuality; }
        }

        bool ModAPI.IMyConfig.RotationHints
        {
            get { return RotationHints; }
        }

        int? ModAPI.IMyConfig.ScreenHeight
        {
            get { return ScreenHeight; }
        }

        int? ModAPI.IMyConfig.ScreenWidth
        {
            get { return ScreenWidth; }
        }

        MyShadowsQuality? ModAPI.IMyConfig.ShadowQuality
        {
            get { return ShadowQuality; }
        }

        bool ModAPI.IMyConfig.ShowCrosshair
        {
            get { return ShowCrosshair; }
        }

        bool ModAPI.IMyConfig.ShowPlayerNamesOnHud
        {
            get { return ShowPlayerNamesOnHud; }
        }

        MyTextureQuality? ModAPI.IMyConfig.TextureQuality
        {
            get { return TextureQuality; }
        }

        bool ModAPI.IMyConfig.VerticalSync
        {
            get { return VerticalSync; }
        }

        int ModAPI.IMyConfig.VideoAdapter
        {
            get { return VideoAdapter; }
        }

        MyWindowModeEnum ModAPI.IMyConfig.WindowMode
        {
            get { return WindowMode; }
        }
        #endregion
    }
}
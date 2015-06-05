using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum MyRenderMessageEnum
    {
        CreateSprite,
        DrawSprite,
        DrawSpriteNormalized,
        DrawSpriteAtlas,
        RemoveSprite,
        UnloadTexture,
        UnloadModel,

        RenderProfiler,

        CreateRenderEntity,
        CreateRenderEntityAtmosphere,
        AddRuntimeModel,
        PreloadModel,
        PreloadMaterials,
        SetRenderEntityData,
        SetRenderEntityLOD,
        CreateRenderBatch,
        CreateRenderInstanceBuffer,
        UpdateRenderInstanceBuffer,
        UpdateRenderCubeInstanceBuffer,
        SetInstanceBuffer,
        CreateManualCullObject,
        SetParentCullObject,
        SetCameraViewMatrix,
        DrawScene,
        UpdateRenderObject,
        UpdateRenderObjectVisibility,
        UpdateRenderEntity,
        EnableRenderModule,
        RemoveRenderObject,
        UseCustomDrawMatrix,
        UpdateModelProperties,
        UpdateVoxelMaterialsProperties,
        ChangeModel,
        ChangeModelMaterial,
        TextNotDrawnToTexture,//Output
        RenderTextureFreed,//Output
        RenderObjectNotFound,//Output
        ChangeMaterialTexture,
        DrawTextToMaterial,
        ReleaseRenderTexture,


        CreateClipmap,
        RequireClipmapCell, //Output
        CancelClipmapCell, //Output
        UpdateClipmapCell,
        InvalidateClipmapRange,
        ClipmapsReady, // Output

        CreateRenderVoxelMaterials,
        CreateRenderVoxelDebris,

        RebuildCullingStructure,

        CreateRenderLight,
        UpdateRenderLight,
        SetLightShadowIgnore,
        ClearLightShadowIgnore,

        ReloadEffects,
        ReloadModels,
        ReloadTextures,

        UpdatePostprocessSettings,
        UpdateRenderEnvironment,
        UpdateHDRSettings,
        UpdateAntiAliasSettings,
        UpdateSSAOSettings,
        UpdateFogSettings,
        UpdateGodRaysSettings,
        UpdateEnvironmentMap,
        UpdateVignettingSettings,
        UpdateColorMappingSettings,
        UpdateContrastSettings,
        UpdateChromaticAberrationSettings,

        PlayVideo,
        UpdateVideo,
        DrawVideo,
        CloseVideo,
        SetVideoVolume,

        DrawSecondaryCamera,
        DrawSecondaryCameraSprite,

        CreateDecal,
        CreateScreenDecal,
        RemoveDecal,
        RegisterDecalsMaterials,
        HideDecals,

        UpdateCockpitGlass,
        CreateCockpitGlassDecal,

        UpdateBillboardsColorize,
        AddLineBillboardLocal,
        AddPointBillboardLocal,

        UpdateDistantImpostors,

        SetTextureIgnoreQuality,
        UpdateRenderQuality,

        TakeScreenshot,
        ScreenshotTaken, //Output
        ExportToObjComplete, //Output

        CreateRenderCharacter,
        SetCharacterSkeleton,
        //PlayCharacterAnimation,
        //UpdateCharacterAnimation,
        SetCharacterTransforms,

        DebugDrawLine3D,
        DebugDrawLine2D,
        DebugDrawPoint,
        DebugDrawSphere,
        DebugDrawAABB,
        DebugDrawAxis,
        DebugDrawOBB,
        DebugDrawTriangle,
        DebugDrawCapsule,
        DebugDrawText2D,
        DebugDrawText3D,
        DebugDrawModel,
        DebugDrawTriangles,
        DebugCrashRenderThread,
        DebugDrawPlane,
        DebugDrawCylinder,
        DebugDrawCone,

        UnloadData,

        CreateFont,
        DrawString,
        PreloadTextures,
        CollectGarbage,
        SpriteScissorPush,
        SpriteScissorPop,
        RenderColoredTexture,

        CreateLineBasedObject,
        UpdateLineBasedObject,

        VideoAdaptersRequest,
        VideoAdaptersResponse, // Output
        CreatedDeviceSettings, // Output
        SwitchDeviceSettings,
        SwitchRenderSettings,
    }
}

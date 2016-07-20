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
        CreateRenderEntityClouds,
        AddRuntimeModel,
        PreloadModel,
        PreloadMaterials,
        SetRenderEntityData,
        SetRenderEntityLOD,
        CreateRenderBatch,
        CreateRenderInstanceBuffer,
        UpdateRenderInstanceBufferSettings,
        UpdateRenderInstanceBufferRange,
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
        UpdateModelHighlight,
        UpdateColorEmissivity,
        UpdateVoxelMaterialsProperties,
        ChangeModel,
        ChangeModelMaterial,
        TextNotDrawnToTexture,//Output
        RenderTextureFreed,//Output
        RenderObjectNotFound,//Output
        ChangeMaterialTexture,
        DrawTextToMaterial,
        ReleaseRenderTexture,

        UpdateGameplayFrame,

        CreateClipmap,
        RequireClipmapCell, //Output
        CancelClipmapCell, //Output
        UpdateClipmapCell,
        InvalidateClipmapRange,
        ClipmapsReady, // Output

        UpdateMergedVoxelMesh,
        MergeVoxelMeshes,   // Output
        CancelVoxelMeshMerge,   // Output
        ResetMergedVoxels,

        CreateRenderVoxelMaterials,
        UpdateRenderVoxelMaterials,
        CreateRenderVoxelDebris,

        RebuildCullingStructure,

        CreateGPUEmitter,
        UpdateGPUEmitters,
        UpdateGPUEmittersTransform,
        RemoveGPUEmitter,

        CreateRenderLight,
        UpdateRenderLight,
        SetLightShadowIgnore,
        ClearLightShadowIgnore,

        ReloadEffects,
        ReloadModels,
        ReloadTextures,
        ReloadGrass,

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
        UpdateAtmosphereSettings,
        UpdatePlanetBlurSettings,
        EnableAtmosphere,
        UpdateCloudLayerFogFlag,

        PlayVideo,
        UpdateVideo,
        DrawVideo,
        CloseVideo,
        SetVideoVolume,

        DrawSecondaryCamera,
        DrawSecondaryCameraSprite,

        CreateDecal, // Deprecate

        CreateScreenDecal,
        UpdateScreenDecal,
        RemoveDecal,
        SetDecalGlobals,
        RegisterDecalsMaterials,
        ClearDecals,
        HideDecals,

        UpdateCockpitGlass,
        CreateCockpitGlassDecal,

        UpdateBillboardsColorize,
        AddLineBillboardLocal,
        AddPointBillboardLocal,

        UpdateDistantImpostors,

        SetTextureIgnoreQuality,
        UpdateRenderQuality,

        RequestScreenData,
        ScreenDataReady,    // Output

        TakeScreenshot,
        ScreenshotTaken, //Output
        ExportToObjComplete, //Output

        Error, //Output

        CreateRenderCharacter,
        SetCharacterSkeleton,
        //PlayCharacterAnimation,
        //UpdateCharacterAnimation,
        SetCharacterTransforms,

        /* Debug Draw Messages */
        DebugDrawLine3D,
        DebugDrawLine2D,
        DebugDrawPoint,
        DebugDrawSphere,
        DebugDrawAABB,
        DebugDrawAxis,
        DebugDrawOBB,
        DebugDrawFrustrum,
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
        DebugDrawMesh,
        DebugDraw6FaceConvex,
        DebugWaitForPresent,
        DebugClearPersistentMessages,

        UpdateDebugOverrides,

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

        SetMouseCapture
    }
}

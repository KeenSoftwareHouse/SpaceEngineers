namespace VRageRender.Messages
{
    public enum MyRenderMessageEnum
    {
        DrawSprite,
        DrawSpriteNormalized,
        DrawSpriteAtlas,
        UnloadTexture,

        RenderProfiler,

        CreateRenderEntity,
        CreateRenderEntityAtmosphere,
        CreateRenderEntityClouds,
        AddRuntimeModel,
        PreloadModel,
        PreloadMaterials,
        SetRenderEntityData,
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
        RemoveRenderObject,
        UpdateModelProperties,
        UpdateModelHighlight,
        UpdateColorEmissivity,
        ChangeModel,
        ChangeModelMaterial,
        RenderTextureFreed,//Output
        CreateGeneratedTexture,
        ResetGeneratedTexture,
        ChangeMaterialTexture,
        RenderOffscreenTextureToMaterial,

        UpdateGameplayFrame,

        CreateClipmap,
        RequireClipmapCell, //Output
        CancelClipmapCell, //Output
        UpdateClipmapCell,
        InvalidateClipmapRange,
        ClipmapsReady, // Output

        CreateRenderVoxelMaterials,
        UpdateRenderVoxelMaterials,
        CreateRenderVoxelDebris,

        RebuildCullingStructure,

        CreateGPUEmitter,
        UpdateGPUEmitters,
        UpdateGPUEmittersLight,
        UpdateGPUEmittersTransform,
        RemoveGPUEmitter,

        CreateRenderLight,
        UpdateRenderLight,
        SetLightShadowIgnore,
        ClearLightShadowIgnore,
        UpdateShadowSettings,
        UpdateNewLoddingSettings,
        UpdateNewPipelineSettings,
        UpdateMaterialsSettings,

        ReloadEffects,
        ReloadModels,
        ReloadTextures,

        UpdatePostprocessSettings,
        UpdateRenderEnvironment,
        UpdateSSAOSettings,
        UpdateHBAO,
        UpdateFogSettings,
        UpdateEnvironmentMap,
        UpdateAtmosphereSettings,
        EnableAtmosphere,
        UpdateCloudLayerFogFlag,

        PlayVideo,
        UpdateVideo,
        DrawVideo,
        CloseVideo,
        SetVideoVolume,

        CreateScreenDecal,
        UpdateScreenDecal,
        RemoveDecal,
        SetDecalGlobals,
        RegisterDecalsMaterials,
        ClearDecals,

        TakeScreenshot,
        ScreenshotTaken, //Output
        ExportToObjComplete, //Output

        Error, //Output

        CreateRenderCharacter,
        SetCharacterSkeleton,
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
        DebugPrintAllFileTexturesIntoLog,

        UpdateDebugOverrides,

        UnloadData,

        CreateFont,
        DrawString,
        PreloadTextures,

        SetFrameTimeStep,
        ResetRandomness,
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

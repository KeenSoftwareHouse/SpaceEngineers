#region Using

using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using VRage;
using VRage.Generics;
using VRage.Import;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender.Graphics;
using VRageRender.Lights;
using VRageRender.Messages;
using VRageRender.Profiler;
using BoundingBox = VRageMath.BoundingBox;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using Quaternion = VRageMath.Quaternion;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;

#endregion

namespace VRageRender
{
    public static class MyRenderProxy
    {
        public static bool DRAW_RENDER_STATS = false;

        static IMyRender m_render = null;

        #region Fields

        public static bool IS_OFFICIAL = false;

        public static readonly uint RENDER_ID_UNASSIGNED = 0xFFFFFFFF;

        public static MyRenderThread RenderThread { get; private set; }

        public static MyRenderSettings Settings = new MyRenderSettings();

        public static List<MyBillboard> BillboardsRead { get { return m_render.SharedData.Billboards.Read.Billboards; } }
        public static List<MyBillboard> BillboardsWrite { get { return m_render.SharedData.Billboards.Write.Billboards; } }

        public static Dictionary<int, MyBillboardViewProjection> BillboardsViewProjectionRead { get { return m_render.SharedData.Billboards.Read.Matrices; } }
        public static Dictionary<int, MyBillboardViewProjection> BillboardsViewProjectionWrite { get { return m_render.SharedData.Billboards.Write.Matrices; } }

        public static MyObjectsPoolSimple<MyBillboard> BillboardsPoolRead { get { return m_render.SharedData.Billboards.Read.Pool; } }
        public static MyObjectsPoolSimple<MyBillboard> BillboardsPoolWrite { get { return m_render.SharedData.Billboards.Write.Pool; } }

        public static MyObjectsPoolSimple<MyTriangleBillboard> TriangleBillboardsPoolRead { get { return m_render.SharedData.TriangleBillboards.Read.Pool; } }
        public static MyObjectsPoolSimple<MyTriangleBillboard> TriangleBillboardsPoolWrite { get { return m_render.SharedData.TriangleBillboards.Write.Pool; } }

        public static HashSet<uint> VisibleObjectsRead { get { return m_render.SharedData.VisibleObjects.Read; } }
        public static HashSet<uint> VisibleObjectsWrite { get { return m_render.SharedData.VisibleObjects.Write; } }

        public static MyMessagePool MessagePool = new MyMessagePool();

        public static Action WaitForFlushDelegate = null;

        public static bool LimitMaxQueueSize = false;

        public static MyTimeSpan CurrentDrawTime { get { return m_render.CurrentDrawTime; } set { m_render.CurrentDrawTime = value; } }

        public static MyViewport MainViewport { get { return m_render.MainViewport; } }

        public static Vector2I BackBufferResolution { get { return m_render.BackBufferResolution; } }

        #endregion



        #region Device

        public static MyRenderDeviceSettings CreateDevice(MyRenderThread renderThread, IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            Debug.Assert(RenderThread == null, "Render thread already set, it shouldn't be!");
            RenderThread = renderThread;
            return m_render.CreateDevice(windowHandle, settingsToTry);
        }

        public static void DisposeDevice()
        {
            // It seems we call DisposeDevice multiple times so this assert gets triggered.
            // It might be an error, but since device itself is not disposed multiple times, I guess it can be ignored for now.
            //Debug.Assert(RenderThread != null, "Render thread is not set, it should be!");

            m_render.DisposeDevice();
            RenderThread = null;
        }

        public static long GetAvailableTextureMemory()
        {
            return m_render.GetAvailableTextureMemory();
        }

        public static MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            return m_render.TestDeviceCooperativeLevel();
        }

        public static bool ResetDevice()
        {
            return m_render.ResetDevice();
        }

        public static void DrawBegin()
        {
            m_render.DrawBegin();
        }

        public static void DrawEnd()
        {
            m_render.DrawEnd();
        }

        #endregion

        #region Swapchain

        public static bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return m_render.SettingsChanged(settings);
        }

        public static void ApplySettings(MyRenderDeviceSettings settings)
        {
            m_render.ApplySettings(settings);
        }

        public static void Present()
        {
            m_render.Present();
        }

        public static void ClearBackbuffer(ColorBGRA clearColor)
        {
            m_render.ClearBackbuffer(clearColor);
        }

        public static string RendererInterfaceName()
        {
            return m_render.ToString();
        }

        #endregion

        #region Messages management

        [Conditional("DEBUG")]
        public static void AssertRenderThread()
        {
            Debug.Assert(RenderThread.SystemThread == Thread.CurrentThread, "Render accessed from non-render thread");
        }

        private static void EnqueueMessage(IMyRenderMessage message)
        {
            Debug.Assert(message != null);

            m_render.EnqueueMessage(message, LimitMaxQueueSize);
        }

        // TODO: OP! make time mandatory
        public static void BeforeRender(MyTimeSpan? currentDrawTime)
        {
            m_render.SharedData.BeforeRender(m_render.Settings, currentDrawTime);
        }

        public static void AfterRender()
        {
            if (m_render.SharedData != null)
                m_render.SharedData.AfterRender();
        }

        public static void BeforeUpdate()
        {
            if (m_render.SharedData != null)
                m_render.SharedData.BeforeUpdate();
        }

        // TODO: OP! make time mandatory
        public static void AfterUpdate(MyTimeSpan? updateTimestamp)
        {
            if (m_render.SharedData != null)
                m_render.SharedData.AfterUpdate(MyRenderProxy.Settings, updateTimestamp);
        }

        public static void ProcessMessages()
        {
            MyRenderProxy.AssertRenderThread();
            m_render.Draw(false);
        }

        public static void Draw()
        {
            MyRenderProxy.AssertRenderThread();
            m_render.Draw(true);
        }

        public static MyRenderProfiler GetRenderProfiler()
        {
            return m_render.GetRenderProfiler();
        }

        #endregion

        #region Global

        public static void Initialize(IMyRender render)
        {
            m_render = render;
        }

        public static void LoadContent(MyRenderQualityEnum quality)
        {
            GetRenderProfiler().StartProfilingBlock("Load Content");
            m_render.LoadContent(quality);
            GetRenderProfiler().EndProfilingBlock();
        }

        public static void UnloadContent()
        {
            GetRenderProfiler().StartProfilingBlock("Unload Content");
            m_render.UnloadContent();

            ClearLargeMessages();
            GetRenderProfiler().EndProfilingBlock();

        }

        public static void ClearLargeMessages()
        {
            MessagePool.Clear(MyRenderMessageEnum.CreateRenderInstanceBuffer);
            MessagePool.Clear(MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer);
            MessagePool.Clear(MyRenderMessageEnum.UpdateRenderInstanceBuffer);
        }

        public static void UnloadData()
        {
            ClearLargeMessages();

            var message = MessagePool.Get<MyRenderMessageUnloadData>(MyRenderMessageEnum.UnloadData);

            EnqueueMessage(message);
        }


        public static void SetGlobalValues(string rootDirectory, string rootDirectoryEffects, string rootDirectoryDebug)
        {
            m_render.RootDirectory = rootDirectory;
            m_render.RootDirectoryEffects = rootDirectoryEffects;
            m_render.RootDirectoryDebug = rootDirectoryDebug;
        }

        #endregion

        #region Sprites

        public static void DrawSprite(string texture, ref RectangleF destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, Vector2 rightVector, ref Vector2 origin, SpriteEffects effects, float depth)
        {
            Debug.Assert(!string.IsNullOrEmpty(texture) && (texture.EndsWith(".jpg") || texture.EndsWith(".dds") || texture.EndsWith(".png")), "Unsupported sprite texture: " + texture);

            var message = MessagePool.Get<MyRenderMessageDrawSprite>(MyRenderMessageEnum.DrawSprite);

            message.Texture = texture;
            message.DestinationRectangle = destination;
            message.SourceRectangle = sourceRectangle;
            message.Color = color;
            message.Rotation = rotation;
            message.RightVector = rightVector;
            message.Depth = depth;
            message.Effects = effects;
            message.Origin = origin;
            message.ScaleDestination = scaleDestination;

            EnqueueMessage(message);
        }

        // RotSpeed in rad/s
        public static void DrawSprite(string texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, float rotation, Vector2 rightVector, float scale, Vector2? originNormalized, float rotSpeed = 0)
        {
            Debug.Assert(!string.IsNullOrEmpty(texture) && (texture.EndsWith(".jpg") || texture.EndsWith(".dds") || texture.EndsWith(".png")), "Unsupported sprite texture: " + texture);

            var message = MessagePool.Get<MyRenderMessageDrawSpriteNormalized>(MyRenderMessageEnum.DrawSpriteNormalized);

            message.Texture = texture;
            message.NormalizedCoord = normalizedCoord;
            message.NormalizedSize = normalizedSize;
            message.Color = color;
            message.DrawAlign = drawAlign;
            message.Rotation = rotation;
            message.RightVector = rightVector;
            message.Scale = scale;
            message.OriginNormalized = originNormalized;
            message.RotationSpeed = rotSpeed;

            EnqueueMessage(message);
        }

        public static void DrawSpriteAtlas(string texture, Vector2 position, Vector2 textureOffset, Vector2 textureSize, Vector2 rightVector, Vector2 scale, Color color, Vector2 halfSize)
        {
            Debug.Assert(!string.IsNullOrEmpty(texture) && (texture.EndsWith(".dds") || texture.EndsWith(".png")));

            var message = MessagePool.Get<MyRenderMessageDrawSpriteAtlas>(MyRenderMessageEnum.DrawSpriteAtlas);

            message.Texture = texture;
            message.Position = position;
            message.TextureOffset = textureOffset;
            message.TextureSize = textureSize;
            message.RightVector = rightVector;
            message.Scale = scale;
            message.Color = color;
            message.HalfSize = halfSize;

            EnqueueMessage(message);
        }

        public static void CreateFont(int fontId, string fontPath, bool isDebugFont = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(fontPath));

            var message = MessagePool.Get<MyRenderMessageCreateFont>(MyRenderMessageEnum.CreateFont);

            message.FontId = fontId;
            message.FontPath = fontPath;
            message.IsDebugFont = isDebugFont;

            EnqueueMessage(message);
        }

        public static void DrawString(
            int fontIndex,
            Vector2 screenCoord,
            Color colorMask,
            StringBuilder text,
            float screenScale,
            float screenMaxWidth)
        {
            var message = MessagePool.Get<MyRenderMessageDrawString>(MyRenderMessageEnum.DrawString);
            message.Text.Clear().AppendStringBuilder(text);
            message.FontIndex = fontIndex;
            message.ScreenCoord = screenCoord;
            message.ColorMask = colorMask;
            message.ScreenScale = screenScale;
            message.ScreenMaxWidth = screenMaxWidth;

            EnqueueMessage(message);
        }


        #endregion

        #region Textures

        public static void PreloadTextures(string inDirectory, bool recursive)
        {
            var message = MessagePool.Get<MyRenderMessagePreloadTextures>(MyRenderMessageEnum.PreloadTextures);
            message.InDirectory = inDirectory;
            message.Recursive = recursive;
            EnqueueMessage(message);
        }

        public static void UnloadTexture(string textureName)
        {
            var message = MessagePool.Get<MyRenderMessageUnloadTexture>(MyRenderMessageEnum.UnloadTexture);

            message.Texture = textureName;

            EnqueueMessage(message);
        }

        #endregion

        #region Profiler

        public static void RenderProfilerInput(RenderProfilerCommand command, int index)
        {
            var message = MessagePool.Get<MyRenderMessageRenderProfiler>(MyRenderMessageEnum.RenderProfiler);

            message.Command = command;
            message.Index = index;

            EnqueueMessage(message);
        }

        #endregion

        #region Render objects

        public static uint CreateRenderEntityAtmosphere(
          string debugName,
          string model,
          MatrixD worldMatrix,
          MyMeshDrawTechnique technique,
          RenderFlags flags,
          CullingOptions cullingOptions,
          float atmosphereRadius,
          float planetRadius,
          Vector3 atmosphereWavelengths,
          float dithering = 0,
          float maxViewDistance = float.MaxValue
          )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderEntityAtmosphere>(MyRenderMessageEnum.CreateRenderEntityAtmosphere);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.Technique = technique;
            message.Flags = flags;
            message.CullingOptions = cullingOptions;
            message.MaxViewDistance = maxViewDistance;
            message.AtmosphereRadius = atmosphereRadius;
            message.PlanetRadius = planetRadius;
            message.AtmosphereWavelengths = atmosphereWavelengths;

            EnqueueMessage(message);

            UpdateRenderEntity(id, Vector3.Zero, Vector3.Zero, dithering);

            return id;
        }

        public static uint CreateRenderEntity(
            string debugName,
            string model,
            MatrixD worldMatrix,
            MyMeshDrawTechnique technique,
            RenderFlags flags,
            CullingOptions cullingOptions,
            Color diffuseColor,
            Vector3 colorMaskHsv,
            float dithering = 0,
            float maxViewDistance = float.MaxValue
            )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderEntity>(MyRenderMessageEnum.CreateRenderEntity);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.Technique = technique;
            message.Flags = flags;
            message.CullingOptions = cullingOptions;
            message.MaxViewDistance = maxViewDistance;

            EnqueueMessage(message);

            UpdateRenderEntity(id, diffuseColor, colorMaskHsv, dithering);

            return id;
        }

        public static uint CreateLineBasedObject(
            string colorMetalTexture,
            string normalGlossTexture,
            string extensionTexture)
        {
            var message = MessagePool.Get<MyRenderMessageCreateLineBasedObject>(MyRenderMessageEnum.CreateLineBasedObject);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.ColorMetalTexture = colorMetalTexture;
            message.NormalGlossTexture = normalGlossTexture;
            message.ExtensionTexture = extensionTexture;

            EnqueueMessage(message);

            return id;
        }

        public static MyRenderMessageSetRenderEntityData PrepareSetRenderEntityData()
        {
            var msg = MessagePool.Get<MyRenderMessageSetRenderEntityData>(MyRenderMessageEnum.SetRenderEntityData);
            msg.ModelData.Clear();
            return msg;
        }

        public static void SetRenderEntityData(uint renderObjectId, MyRenderMessageSetRenderEntityData message)
        {
            message.ID = renderObjectId;

            EnqueueMessage(message);
        }

        public static MyRenderMessageAddRuntimeModel PrepareAddRuntimeModel()
        {
            var msg = MessagePool.Get<MyRenderMessageAddRuntimeModel>(MyRenderMessageEnum.AddRuntimeModel);
            msg.ModelData.Clear();
            return msg;
        }

        public static void PreloadModel(string name)
        {
            var msg = MessagePool.Get<MyRenderMessagePreloadModel>(MyRenderMessageEnum.PreloadModel);
            msg.Name = name;
            EnqueueMessage(msg);
        }

        public static void PreloadMaterials(string name)
        {
            var msg = MessagePool.Get<MyRenderMessagePreloadMaterials>(MyRenderMessageEnum.PreloadMaterials);
            msg.Name = name;
            EnqueueMessage(msg);
        }

        public static void AddRuntimeModel(string name, MyRenderMessageAddRuntimeModel message)
        {
            message.Name = name;
            EnqueueMessage(message);
        }

        public static uint SetRenderEntityLOD(
          uint id,
          float distance,
          string model
          )
        {
            System.Diagnostics.Debug.Assert(distance > 0 && distance <= 1, "Distance is multiplier of MyRenderConstants.LodTransitionDistanceBackgroundEnd");

            var message = MessagePool.Get<MyRenderMessageSetRenderEntityLOD>(MyRenderMessageEnum.SetRenderEntityLOD);

            message.ID = id;
            message.Distance = distance;
            message.Model = model;

            EnqueueMessage(message);

            return id;
        }

        public static uint CreateRenderBatch(
            string debugName,
            MatrixD worldMatrix,
            RenderFlags flags,
            List<MyRenderBatchPart> batchParts)
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderBatch>(MyRenderMessageEnum.CreateRenderBatch);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.WorldMatrix = worldMatrix;
            message.Flags = flags;
            message.RenderBatchParts.Clear();
            message.RenderBatchParts.AddList(batchParts);

            EnqueueMessage(message);

            return id;
        }

        public static uint CreateRenderInstanceBuffer(string debugName, MyRenderInstanceBufferType type)
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderInstanceBuffer>(MyRenderMessageEnum.CreateRenderInstanceBuffer);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.Type = type;
            EnqueueMessage(message);

            return id;
        }

        public static void UpdateRenderCubeInstanceBuffer(uint id, List<MyCubeInstanceData> instanceData, int capacity)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderCubeInstanceBuffer>(MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer);

            message.ID = id;
            message.InstanceData.Clear();
            message.InstanceData.AddList(instanceData);
            if (message.InstanceData.Count < message.InstanceData.Capacity &&
                message.InstanceData.Capacity > 20000)
                message.InstanceData.TrimExcess();
            message.Capacity = capacity;

            EnqueueMessage(message);
        }

        public static void UpdateRenderInstanceBuffer(uint id, List<MyInstanceData> instanceData, int capacity)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderInstanceBuffer>(MyRenderMessageEnum.UpdateRenderInstanceBuffer);

            message.ID = id;
            message.InstanceData.Clear();
            message.InstanceData.AddList(instanceData);
            if (message.InstanceData.Count < message.InstanceData.Capacity &&
                message.InstanceData.Capacity > 20000)
                message.InstanceData.TrimExcess();
            message.Capacity = capacity;

            EnqueueMessage(message);
        }

        public static void UpdateLineBasedObject(uint id, Vector3D worldPointA, Vector3D worldPointB)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateLineBasedObject>(MyRenderMessageEnum.UpdateLineBasedObject);

            message.ID = id;
            message.WorldPointA = worldPointA;
            message.WorldPointB = worldPointB;

            EnqueueMessage(message);
        }

        public static uint CreateManualCullObject(string debugName, MatrixD worldMatrix)
        {
            var message = MessagePool.Get<MyRenderMessageCreateManualCullObject>(MyRenderMessageEnum.CreateManualCullObject);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.WorldMatrix = worldMatrix;
            EnqueueMessage(message);

            return id;
        }

        public static void SetParentCullObject(uint renderObject, uint parentCullObject, Matrix? childToParent = null)
        {
            var message = MessagePool.Get<MyRenderMessageSetParentCullObject>(MyRenderMessageEnum.SetParentCullObject);

            message.ID = renderObject;
            message.CullObjectID = parentCullObject;
            message.ChildToParent = childToParent;
            EnqueueMessage(message);
        }

        public static void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projectionMatrix, Matrix nearProjectionMatrix, float safenear, float nearFov, float fov,
            float nearPlane, float farPlane, float nearObjectsNearPlane, float nearObjectsFarPlane,
            Vector3D cameraPosition)
        {
            var message = MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);

            cameraPosition.AssertIsValid();

            message.ViewMatrix = viewMatrix;
            message.ProjectionMatrix = projectionMatrix;
            message.NearProjectionMatrix = nearProjectionMatrix;
            message.SafeNear = safenear;
            message.NearFOV = nearFov;
            message.FOV = fov;
            message.NearPlane = nearPlane;
            message.FarPlane = farPlane;
            message.NearObjectsNearPlane = nearObjectsNearPlane;
            message.NearObjectsFarPlane = nearObjectsFarPlane;
            message.CameraPosition = cameraPosition;

            EnqueueMessage(message);
        }

        public static void Draw3DScene()
        {
            var message = MessagePool.Get<MyRenderMessageDrawScene>(MyRenderMessageEnum.DrawScene);

            EnqueueMessage(message);
        }

        public static void UpdateRenderObject(
           uint id,
           ref MatrixD worldMatrix,
           bool sortIntoCulling,
            BoundingBoxD? aabb = null
           )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderObject>(MyRenderMessageEnum.UpdateRenderObject);

            message.ID = id;
            message.WorldMatrix = worldMatrix;
            message.SortIntoCulling = sortIntoCulling;
            message.AABB = aabb;

            EnqueueMessage(message);
        }

        public static void UpdateRenderObjectVisibility(
           uint id,
           bool visible,
           bool near
           )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderObjectVisibility>(MyRenderMessageEnum.UpdateRenderObjectVisibility);

            message.ID = id;
            message.Visible = visible;
            message.NearFlag = near;

            EnqueueMessage(message);
        }


        public static void RemoveRenderObject(
        uint id
        )
        {
            var message = MessagePool.Get<MyRenderMessageRemoveRenderObject>(MyRenderMessageEnum.RemoveRenderObject);

            message.ID = id;

            EnqueueMessage(message);
        }


        public static void UpdateRenderEntity(
            uint id,
            Color? diffuseColor,
            Vector3? colorMaskHsv,
            float dithering = 0
         )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderEntity>(MyRenderMessageEnum.UpdateRenderEntity);

            message.ID = id;
            message.DiffuseColor = diffuseColor;
            message.ColorMaskHSV = colorMaskHsv;
            message.Dithering = dithering;

            EnqueueMessage(message);
        }

        public static void SetInstanceBuffer(uint entityId, uint instanceBufferId, int instanceStart, int instanceCount, BoundingBox entityLocalAabb)
        {
            var message = MessagePool.Get<MyRenderMessageSetInstanceBuffer>(MyRenderMessageEnum.SetInstanceBuffer);

            message.ID = entityId;
            message.InstanceBufferId = instanceBufferId;
            message.InstanceStart = instanceStart;
            message.InstanceCount = instanceCount;
            message.LocalAabb = entityLocalAabb;

            EnqueueMessage(message);
        }

        public static void EnableRenderModule(
        uint id,
        bool enable
        )
        {
            var message = MessagePool.Get<MyRenderMessageEnableRenderModule>(MyRenderMessageEnum.EnableRenderModule);

            message.ID = id;
            message.Enable = enable;

            EnqueueMessage(message);
        }


        public static void UseCustomDrawMatrix(
        uint id,
        MatrixD drawMatrix,
        bool enable
         )
        {
            var message = MessagePool.Get<MyRenderMessageUseCustomDrawMatrix>(MyRenderMessageEnum.UseCustomDrawMatrix);

            message.ID = id;
            message.DrawMatrix = drawMatrix;
            message.Enable = enable;

            EnqueueMessage(message);
        }

        public static uint CreateClipmap(
            MatrixD worldMatrix,
            Vector3I sizeLod0,
            MyClipmapScaleEnum scaleGroup,
            Vector3D position,
            float atmosphereRadius = 0.0f,
            float planetRadius = 0.0f,
            bool hasAtmosphere = false,
            Vector3? atmosphereWaveLenghts = null)
        {
            var message = MessagePool.Get<MyRenderMessageCreateClipmap>(MyRenderMessageEnum.CreateClipmap);

            uint clipmapId = m_render.GlobalMessageCounter++;
            message.ClipmapId = clipmapId;
            message.WorldMatrix = worldMatrix;
            message.SizeLod0 = sizeLod0;
            message.ScaleGroup = scaleGroup;
            message.AtmosphereRadius = atmosphereRadius;
            message.PlanetRadius = planetRadius;
            message.HasAtmosphere = hasAtmosphere;
            message.Position = position;
            message.AtmosphereWaveLenghts = atmosphereWaveLenghts;
            EnqueueMessage(message);

            return clipmapId;
        }

        public static void UpdateClipmapCell(
            uint clipmapId,
            MyCellCoord cell,
            List<MyClipmapCellBatch> batches,
            Vector3D positionOffset,
            Vector3 positionScale,
            BoundingBox meshAabb)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateClipmapCell>(MyRenderMessageEnum.UpdateClipmapCell);

            Debug.Assert(message.Batches.Count == 0, "Message was not properly cleared");

            message.ClipmapId = clipmapId;
            message.Cell = cell;
            message.Batches.AddList(batches);
            message.PositionOffset = positionOffset;
            message.PositionScale = positionScale;
            message.MeshAabb = meshAabb;

            EnqueueMessage(message);
        }

        public static void InvalidateClipmapRange(
            uint clipmapId,
            Vector3I minCellLod0,
            Vector3I maxCellLod0)
        {
            var message = MessagePool.Get<MyRenderMessageInvalidateClipmapRange>(MyRenderMessageEnum.InvalidateClipmapRange);

            message.ClipmapId = clipmapId;
            message.MinCellLod0 = minCellLod0;
            message.MaxCellLod0 = maxCellLod0;

            EnqueueMessage(message);
        }

        public static void RebuildCullingStructure()
        {
            var message = MessagePool.Get<MyRenderMessageRebuildCullingStructure>(MyRenderMessageEnum.RebuildCullingStructure);

            EnqueueMessage(message);
        }



        public static void ReloadEffects()
        {
            var message = MessagePool.Get<MyRenderMessageReloadEffects>(MyRenderMessageEnum.ReloadEffects);

            EnqueueMessage(message);
        }

        public static void ReloadModels()
        {
            var message = MessagePool.Get<MyRenderMessageReloadModels>(MyRenderMessageEnum.ReloadModels);

            EnqueueMessage(message);
        }

        public static void ReloadTextures()
        {
            var message = MessagePool.Get<MyRenderMessageReloadTextures>(MyRenderMessageEnum.ReloadTextures);

            EnqueueMessage(message);
        }

        public static void ReloadContent(MyRenderQualityEnum quality)
        {
            m_render.ReloadContent(quality);
        }

        public static void UnloadModel(string name)
        {
            var message = MessagePool.Get<MyRenderMessageUnloadModel>(MyRenderMessageEnum.UnloadModel);

            message.Name = name;

            EnqueueMessage(message);
        }



        public static void UpdateEnvironmentMap()
        {
            var message = MessagePool.Get<MyRenderMessageUpdateEnvironmentMap>(MyRenderMessageEnum.UpdateEnvironmentMap);

            EnqueueMessage(message);
        }

        public static void CreateRenderVoxelMaterials(
            MyRenderVoxelMaterialData[] materials
            )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderVoxelMaterials>(MyRenderMessageEnum.CreateRenderVoxelMaterials);

            message.Materials = materials;

            EnqueueMessage(message);
        }

        public static uint CreateRenderVoxelDebris(
        string debugName,
        string model,
        MatrixD worldMatrix,
        float textureCoordOffset,
        float textureCoordScale,
        float textureColorMultiplier,
        byte voxelMaterialIndex
        )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderVoxelDebris>(MyRenderMessageEnum.CreateRenderVoxelDebris);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.TextureCoordOffset = textureCoordOffset;
            message.TextureCoordScale = textureCoordScale;
            message.TextureColorMultiplier = textureColorMultiplier;
            message.VoxelMaterialIndex = voxelMaterialIndex;

            EnqueueMessage(message);

            return id;
        }

        public static void UpdateModelProperties(
          uint id,
          int lod,
          string model,
          int meshIndex,
          string materialName,
          bool? enabled,
          Color? diffuseColor,
          float? specularPower,
          float? specularIntensity,
          float? emissivity
          )
        {
            if (id == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                if (string.IsNullOrEmpty(model))
                {
                    System.Diagnostics.Debug.Assert(false);
                    return;
                }
            }

            var message = MessagePool.Get<MyRenderMessageUpdateModelProperties>(MyRenderMessageEnum.UpdateModelProperties);

            message.ID = id;
            message.LOD = lod;
            message.Model = model;
            message.MeshIndex = meshIndex;
            message.MaterialName = materialName;
            message.Enabled = enabled;
            message.DiffuseColor = diffuseColor;
            message.SpecularIntensity = specularIntensity;
            message.SpecularPower = specularPower;
            message.Emissivity = emissivity;

            EnqueueMessage(message);
        }

        /// <summary>
        /// New model should have similar size to previous model because of prunning structure recalculation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="LOD"></param>
        /// <param name="model"></param>
        /// <param name="useForShadow"></param>
        public static void ChangeModel(
          uint id,
          int LOD,
          string model,
          bool useForShadow
          )
        {
            var message = MessagePool.Get<MyRenderMessageChangeModel>(MyRenderMessageEnum.ChangeModel);

            message.ID = id;
            message.Model = model;
            message.LOD = LOD;
            message.UseForShadow = useForShadow;

            EnqueueMessage(message);
        }

        public static void ChangeModelMaterial(
         string model,
         string material
         )
        {
            var message = MessagePool.Get<MyRenderMessageChangeModelMaterial>(MyRenderMessageEnum.ChangeModelMaterial);

            message.Model = model;
            message.Material = material;

            EnqueueMessage(message);
        }


        public static void UpdateVoxelMaterialProperties(
           byte voxelMaterialIndex,
           float specularPower,
           float specularIntensity
           )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateVoxelMaterialsProperties>(MyRenderMessageEnum.UpdateVoxelMaterialsProperties);

            message.MaterialIndex = voxelMaterialIndex;
            message.SpecularIntensity = specularIntensity;
            message.SpecularPower = specularPower;

            EnqueueMessage(message);
        }

        public static void ChangeMaterialTexture(uint id,string materialName,string textureName)
        {
            var message = MessagePool.Get<MyRenderMessageChangeMaterialTexture>(MyRenderMessageEnum.ChangeMaterialTexture);
            if (message.Changes == null)
            {
                message.Changes = new List<MyTextureChange>();
            }
            else
            {
                Debug.Assert(message.Changes.Count == 0, "content should be cleared after consuming in renderer");
            }
            message.Changes.Add(new MyTextureChange { TextureName = textureName });
            message.MaterialName = materialName;
            message.RenderObjectID = id;
            EnqueueMessage(message);
        }

		public static void ChangeMaterialTexture(uint id, string materialName, List<MyTextureChange> textureChanges)
		{
			if (textureChanges == null)
				return;

			var message = MessagePool.Get<MyRenderMessageChangeMaterialTexture>(MyRenderMessageEnum.ChangeMaterialTexture);

			if (message.Changes != null)
				Debug.Assert(message.Changes.Count == 0, "content should be cleared after consuming in renderer");

			message.Changes = textureChanges;
			message.MaterialName = materialName;
			message.RenderObjectID = id;
			EnqueueMessage(message);
		}

        public static void ReleaseRenderTexture(long entityId,uint id)
        {
            var message = MessagePool.Get<MyRenderMessageReleaseRenderTexture>(MyRenderMessageEnum.ReleaseRenderTexture);
            message.RenderObjectID = id;
            message.EntityId = entityId;
            EnqueueMessage(message);
        }
        public static void RenderTextToTexture(uint id, long entityId, string materialName, string text, float scale, Color fontColor, Color backgroundColor, int textureResolution, int textureAspectRatio)
        {
            var message = MessagePool.Get<MyRenderMessageDrawTextToMaterial>(MyRenderMessageEnum.DrawTextToMaterial);

            message.MaterialName = materialName;
            message.Text = text;
            message.TextScale = scale; 
            message.RenderObjectID = id;
            message.TextureResolution = textureResolution;
            message.FontColor = fontColor;
            message.TextureAspectRatio = textureAspectRatio;
            message.BackgroundColor = backgroundColor;
            message.EntityId = entityId;
            EnqueueMessage(message);
        }
        public static void TextNotDrawnToTexture(long entityID)
        {
            var message = MessagePool.Get<MyRenderMessageTextNotDrawnToTexture>(MyRenderMessageEnum.TextNotDrawnToTexture);

            message.EntityId = entityID;

            EnqueueOutputMessage(message);
        }
        public static void RenderTextureFreed(int freeResources)
        {
            var message = MessagePool.Get<MyRenderMessageRenderTextureFreed>(MyRenderMessageEnum.RenderTextureFreed);

            message.FreeResources = freeResources;           
            EnqueueOutputMessage(message);
        }
        #endregion

        #region Output messages

        private static void EnqueueOutputMessage(IMyRenderMessage message)
        {
            //System.Diagnostics.Debug.Assert(Thread.CurrentThread == AllowedThread);

            m_render.EnqueueOutputMessage(message);
        }

        public static MyMessageQueue OutputQueue
        {
            get { return m_render.OutputQueue; }
        }

        public static void RequireClipmapCell(uint clipmapId, MyCellCoord cell, bool highPriority)
        {
            var message = MessagePool.Get<MyRenderMessageRequireClipmapCell>(MyRenderMessageEnum.RequireClipmapCell);

            message.ClipmapId = clipmapId;
            message.Cell = cell;
            message.HighPriority = highPriority;

            EnqueueOutputMessage(message);
        }

        public static void CancelClipmapCell(uint clipmapId, MyCellCoord cell)
        {
            var message = MessagePool.Get<MyRenderMessageCancelClipmapCell>(MyRenderMessageEnum.CancelClipmapCell);

            message.ClipmapId = clipmapId;
            message.Cell = cell;

            EnqueueOutputMessage(message);
        }

        #endregion

        #region Lights

        public static uint CreateRenderLight(
            LightTypeEnum type,
            Vector3D position,
            int renderObjectID,
            float offset,
            Color color,
            Color specularColor,
            float falloff,
            float range,
            float intensity,
            bool lightOn,
            bool useInForwardRender,
            float reflectorIntensity,
            bool reflectorOn,
            Vector3 reflectorDirection,
            Vector3 reflectorUp,
            float reflectorConeMaxAngleCos,
            Color reflectorColor,
            float reflectorRange,
            float reflectorFalloff,
            string reflectorTexture,
            float shadowDistance,
            bool castShadows,
            bool glareOn,
            MyGlareTypeEnum glareType,
            float glareSize,
            float glareQuerySize,
            float glareIntensity,
            string glareMaterial,
            float glareMaxDistance
            )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderLight>(MyRenderMessageEnum.CreateRenderLight);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;

            EnqueueMessage(message);

            UpdateRenderLight(
                id,
                type,
                position,
                renderObjectID,
                offset,
                color,
                specularColor,
                falloff,
                range,
                intensity,
                lightOn,
                useInForwardRender,
                reflectorIntensity,
                reflectorOn,
                reflectorDirection,
                reflectorUp,
                reflectorConeMaxAngleCos,
                reflectorColor,
                reflectorRange,
                reflectorFalloff,
                reflectorTexture,
                shadowDistance,
                castShadows,
                glareOn,
                glareType,
                glareSize,
                glareQuerySize,
                glareIntensity,
                glareMaterial,
                glareMaxDistance
                );

            return id;
        }

        public static void UpdateRenderLight(
          uint id,
          LightTypeEnum type,
          Vector3D position,
          int renderObjectID,
          float offset,
          Color color,
          Color specularColor,
          float falloff,
          float range,
          float intensity,
          bool lightOn,
          bool useInForwardRender,
          float reflectorIntensity,
          bool reflectorOn,
          Vector3 reflectorDirection,
          Vector3 reflectorUp,
          float reflectorConeMaxAngleCos,
          Color reflectorColor,
          float reflectorRange,
          float reflectorFalloff,
          string reflectorTexture,
          float shadowDistance,
          bool castShadows,
          bool glareOn,
          MyGlareTypeEnum glareType,
          float glareSize,
          float glareQuerySize,
          float glareIntensity,
          string glareMaterial,
          float glareMaxDistance
          )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderLight>(MyRenderMessageEnum.UpdateRenderLight);

            message.ID = id;
            message.Type = type;
            message.Position = position;
            message.ParentID = renderObjectID;
            message.Offset = offset;
            message.Color = color;
            message.SpecularColor = specularColor;
            message.Falloff = falloff;
            message.Range = range;
            message.Intensity = intensity;
            message.LightOn = lightOn;
            message.UseInForwardRender = useInForwardRender;
            message.ReflectorIntensity = reflectorIntensity;
            message.ReflectorOn = reflectorOn;
            message.ReflectorDirection = reflectorDirection;
            message.ReflectorUp = reflectorUp;
            message.ReflectorConeMaxAngleCos = reflectorConeMaxAngleCos;
            message.ReflectorColor = reflectorColor;
            message.ReflectorRange = reflectorRange;
            message.ReflectorFalloff = reflectorFalloff;
            message.ReflectorTexture = reflectorTexture;
            message.ShadowDistance = shadowDistance;
            message.CastShadows = castShadows;
            message.GlareOn = glareOn;
            message.GlareType = glareType;
            message.GlareSize = glareSize;
            message.GlareQuerySize = glareQuerySize;
            message.GlareIntensity = glareIntensity;
            message.GlareMaterial = glareMaterial;
            message.GlareMaxDistance = glareMaxDistance;

            EnqueueMessage(message);
        }

        public static void SetLightShadowIgnore(
          uint id,
            uint ignoreId)
        {
            var message = MessagePool.Get<MyRenderMessageSetLightShadowIgnore>(MyRenderMessageEnum.SetLightShadowIgnore);

            message.ID = id;
            message.ID2 = ignoreId;

            EnqueueMessage(message);
        }

        public static void ClearLightShadowIgnore(
          uint id)
        {
            var message = MessagePool.Get<MyRenderMessageClearLightShadowIgnore>(MyRenderMessageEnum.ClearLightShadowIgnore);

            message.ID = id;

            EnqueueMessage(message);
        }


        public static void UpdateRenderEnvironment(
            Vector3 sunDirection,
            Color sunColor,
            Color sunBackColor,
            Color sunSpecularColor,
            float sunIntensity,
            float sunBackIntensity,
            bool sunLightOn,
            Color ambientColor,
            float ambientMultiplier,
            float envAmbientIntensity,
            Color backgroundColor,
            string backgroundTexture,
            Quaternion backgroundOrientation,
            float sunSizeMultiplier,
            float distanceToSun,
            string sunMaterial,
            float dayTime,
            bool resetEyeAdaptation = false,
            bool enableSunBillboard = false
)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderEnvironment>(MyRenderMessageEnum.UpdateRenderEnvironment);

            message.SunDirection = sunDirection;
            message.SunColor = sunColor;
            message.SunBackColor = sunBackColor;
            message.SunSpecularColor = sunSpecularColor;
            message.SunIntensity = sunIntensity;
            message.SunBackIntensity = sunBackIntensity;
            message.SunLightOn = sunLightOn;
            message.AmbientColor = ambientColor;
            message.AmbientMultiplier = ambientMultiplier;
            message.EnvAmbientIntensity = envAmbientIntensity;
            message.BackgroundColor = backgroundColor;
            message.BackgroundTexture = backgroundTexture;
            message.BackgroundOrientation = backgroundOrientation;
            message.SunSizeMultiplier = sunSizeMultiplier;
            message.DistanceToSun = distanceToSun;
            message.SunMaterial = sunMaterial;
            message.DayTime = dayTime;
            message.ResetEyeAdaptation = resetEyeAdaptation;
            message.SunBillboardEnabled = enableSunBillboard;

            EnqueueMessage(message);
        }

        public static void ResetEnvironmentProbes()
        {
            m_render.ResetEnvironmentProbes();
        }

        #endregion

        #region Post processes


        public static void UpdateHDRSettings(
            bool enabled,
            float exposure,
            float threshold,
            float bloomIntensity,
            float bloomIntensityBackground,
            float verticalBlurAmount,
            float horizontalBlurAmount,
            int numberOfBlurPasses)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateHDRSettings>(MyRenderMessageEnum.UpdateHDRSettings);

            message.Enabled = enabled;
            message.Exposure = exposure;
            message.Threshold = threshold;
            message.BloomIntensity = bloomIntensity;
            message.BloomIntensityBackground = bloomIntensityBackground;
            message.VerticalBlurAmount = verticalBlurAmount;
            message.HorizontalBlurAmount = horizontalBlurAmount;
            message.NumberOfBlurPasses = numberOfBlurPasses;

            EnqueueMessage(message);
        }

        public static void UpdateAntiAliasSettings(
            bool enabled
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateAntiAliasSettings>(MyRenderMessageEnum.UpdateAntiAliasSettings);

            message.Enabled = enabled;

            EnqueueMessage(message);
        }

        public static void UpdateVignettingSettings(
            bool enabled,
            float vignettingPower
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateVignettingSettings>(MyRenderMessageEnum.UpdateVignettingSettings);

            message.Enabled = enabled;
            message.VignettingPower = vignettingPower;

            EnqueueMessage(message);
        }

        public static void UpdateColorMappingSettings(
            bool enabled
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateColorMappingSettings>(MyRenderMessageEnum.UpdateColorMappingSettings);
            message.Enabled = enabled;
            EnqueueMessage(message);
        }

        public static void UpdateChromaticAberrationSettings(
            bool enabled,
            float distortionLens,
            float distortionCubic,
            Vector3 distortionWeights
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateChromaticAberrationSettings>(MyRenderMessageEnum.UpdateChromaticAberrationSettings);
            message.Enabled = enabled;
            message.DistortionLens = distortionLens;
            message.DistortionCubic = distortionCubic;
            message.DistortionWeights = distortionWeights;
            EnqueueMessage(message);
        }

        public static void UpdateContrastSettings(
            bool enabled,
            float contrast,
            float hue,
            float saturation
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateContrastSettings>(MyRenderMessageEnum.UpdateContrastSettings);

            message.Enabled = enabled;
            message.Contrast = contrast;
            message.Hue = hue;
            message.Saturation = saturation;

            EnqueueMessage(message);
        }

        public static void UpdateSSAOSettings(
        bool enabled,
        bool showOnlySSAO,
        bool useBlur,
        float minRadius,
        float maxRadius,
        float radiusGrowZScale,
        float cameraZFar,
        float bias,
        float falloff,
        float normValue,
        float contrast
        )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateSSAOSettings>(MyRenderMessageEnum.UpdateSSAOSettings);

            message.Enabled = enabled;
            message.ShowOnlySSAO = showOnlySSAO;
            message.UseBlur = useBlur;

            message.MinRadius = minRadius;
            message.MaxRadius = maxRadius;
            message.RadiusGrowZScale = radiusGrowZScale;
            message.CameraZFar = cameraZFar;

            message.Bias = bias;
            message.Falloff = falloff;
            message.NormValue = normValue;
            message.Contrast = contrast;

            EnqueueMessage(message);
        }

        [Obsolete("Please use function that takes structure")]
        public static void UpdateFogSettings(
             bool enable,
             float fogNear,
             float fogFar,
             float fogMultiplier,
             float fogBacklightMultiplier,
             Color fogColor
       )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateFogSettings>(MyRenderMessageEnum.UpdateFogSettings);

            message.Settings.Enabled = enable;
            message.Settings.FogNear = fogNear;
            message.Settings.FogFar = fogFar;
            message.Settings.FogMultiplier = fogMultiplier;
            message.Settings.FogBacklightMultiplier = fogBacklightMultiplier;
            message.Settings.FogColor = fogColor;
            message.Settings.FogDensity = 0;

            EnqueueMessage(message);
        }

        public static void UpdateFogSettings(
             ref MyRenderFogSettings settings
       )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateFogSettings>(MyRenderMessageEnum.UpdateFogSettings);

            message.Settings = settings;

            EnqueueMessage(message);
        }

        public static void UpdateGodRaysSettings(
            bool enable,
            float density,
            float weight,
            float decay,
            float exposition,
            bool applyBlur
         )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateGodRaysSettings>(MyRenderMessageEnum.UpdateGodRaysSettings);

            message.Enabled = enable;
            message.Density = density;
            message.Weight = weight;
            message.Decay = decay;
            message.Exposition = exposition;
            message.ApplyBlur = applyBlur;

            EnqueueMessage(message);
        }

        #endregion

        #region Video

        public static uint PlayVideo(string videoFile, float volume)
        {
            var message = MessagePool.Get<MyRenderMessagePlayVideo>(MyRenderMessageEnum.PlayVideo);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.VideoFile = videoFile;
            message.Volume = volume;

            EnqueueMessage(message);

            return id;
        }

        public static void CloseVideo(uint id)
        {
            var message = MessagePool.Get<MyRenderMessageCloseVideo>(MyRenderMessageEnum.CloseVideo);

            message.ID = id;

            EnqueueMessage(message);
        }

        public static void DrawVideo(uint id, Rectangle rect, Color color, MyVideoRectangleFitMode fitMode = MyVideoRectangleFitMode.None)
        {
            var message = MessagePool.Get<MyRenderMessageDrawVideo>(MyRenderMessageEnum.DrawVideo);

            message.ID = id;
            message.Rectangle = rect;
            message.Color = color;
            message.FitMode = fitMode;

            EnqueueMessage(message);
        }

        public static void UpdateVideo(uint id)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateVideo>(MyRenderMessageEnum.UpdateVideo);

            message.ID = id;

            EnqueueMessage(message);
        }

        public static void SetVideoVolume(uint id, float volume)
        {
            var message = MessagePool.Get<MyRenderMessageSetVideoVolume>(MyRenderMessageEnum.SetVideoVolume);

            message.ID = id;
            message.Volume = volume;

            EnqueueMessage(message);
        }

        public static bool IsVideoValid(uint id)
        {
            return m_render.IsVideoValid(id);
        }

        public static VideoState GetVideoState(uint id)
        {
            return m_render.GetVideoState(id);
        }

        #endregion

        #region Secondary camera

        public static void DrawSecondaryCamera(Matrix viewMatrix)
        {
            var message = MessagePool.Get<MyRenderMessageDrawSecondaryCamera>(MyRenderMessageEnum.DrawSecondaryCamera);

            message.ViewMatrix = viewMatrix;

            EnqueueMessage(message);
        }

        #endregion

        #region Decals

        //public static void CreateDecal(uint id, VRageRender.MyDecalTriangle_Data triangle,
        //    int trianglesToAdd, MyDecalTexturesEnum texture,
        //    Vector3 position, float lightSize, float emissivity)
        //{

        //    var message = MessagePool.Get<MyRenderMessageCreateDecal>(MyRenderMessageEnum.CreateDecal);

        //    message.ID = id;
        //    message.Triangle = triangle;
        //    message.TrianglesToAdd = trianglesToAdd;
        //    message.Texture = texture;
        //    message.Position = position;
        //    message.LightSize = lightSize;
        //    message.Emissivity = emissivity;

        //    EnqueueMessage(message);
        //}

        public static void HideDecals(uint id, Vector3 center, float radius)
        {
            var message = MessagePool.Get<MyRenderMessageHideDecals>(MyRenderMessageEnum.HideDecals);

            message.ID = id;
            message.Center = center;
            message.Radius = radius;

            EnqueueMessage(message);
        }

        #endregion

        #region Cockpit

        public static void UpdateCockpitGlass(
            bool visible,
            string model,
            MatrixD worldMatrix,
            float dirtAlpha)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateCockpitGlass>(MyRenderMessageEnum.UpdateCockpitGlass);

            message.Visible = visible;
            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.DirtAlpha = dirtAlpha;

            EnqueueMessage(message);
        }

        #endregion

        #region Billboards

        public static void AddBillboard(MyBillboard billboard)
        {
            System.Diagnostics.Debug.Assert(billboard != null);

            billboard.Position0.AssertIsValid();
            billboard.Position1.AssertIsValid();
            billboard.Position2.AssertIsValid();
            billboard.Position3.AssertIsValid();

            BillboardsWrite.Add(billboard);
        }

        public static void AddBillboards(List<MyBillboard> billboards)
        {
            System.Diagnostics.Debug.Assert(!billboards.Contains(null));
            BillboardsWrite.AddList(billboards);
        }

        public static void UpdateBillboardsColorize(
         bool enable,
         Color color,
         float distance,
         Vector3 normal
         )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateBillboardsColorize>(MyRenderMessageEnum.UpdateBillboardsColorize);

            message.Enable = enable;
            message.Color = color;
            message.Distance = distance;
            message.Normal = normal;

            EnqueueMessage(message);
        }

        public static void AddLineBillboardLocal(uint renderObjectID, string material,
            Color color, Vector3 localPos, Vector3 localDir, float length, float thickness, int priority = 0, bool near = false)
        {
            var message = MessagePool.Get<MyRenderMessageAddLineBillboardLocal>(MyRenderMessageEnum.AddLineBillboardLocal);

            message.RenderObjectID = renderObjectID;
            message.Material = material;
            message.Color = color;
            message.LocalPos = localPos;
            message.LocalDir = localDir;
            message.Length = length;
            message.Thickness = thickness;
            message.Priority = priority;
            message.Near = near;

            EnqueueMessage(message);
        }

        public static void AddPointBillboardLocal(uint renderObjectID, string material,
    Color color, Vector3 localPos, float radius, float angle, int priority = 0, bool colorize = false, bool near = false, bool lowres = false)
        {
            var message = MessagePool.Get<MyRenderMessageAddPointBillboardLocal>(MyRenderMessageEnum.AddPointBillboardLocal);

            message.RenderObjectID = renderObjectID;
            message.Material = material;
            message.Color = color;
            message.LocalPos = localPos;
            message.Radius = radius;
            message.Angle = angle;
            message.Priority = priority;
            message.Colorize = colorize;
            message.Near = near;
            message.Lowres = lowres;

            EnqueueMessage(message);
        }


        public static void AddBillboardViewProjection(int id, MyBillboardViewProjection billboardViewProjection)
        {
            MyBillboardViewProjection existingBillboardViewProjection;
            if (!BillboardsViewProjectionWrite.TryGetValue(id, out existingBillboardViewProjection))
            {
                BillboardsViewProjectionWrite.Add(id, billboardViewProjection);
            }
            else
            {
                BillboardsViewProjectionWrite[id] = billboardViewProjection;
            }
        }

        public static void RemoveBillboardViewProjection(int id)
        {
            BillboardsViewProjectionWrite.Remove(id);
        }


        #endregion

        #region Screenshot and quality

        public static void SetTextureIgnoreQuality(string path)
        {
            var message = MessagePool.Get<MyRenderMessageSetTextureIgnoreQuality>(MyRenderMessageEnum.SetTextureIgnoreQuality);

            message.Path = path;

            EnqueueMessage(message);
        }

        public static void UpdateRenderQuality(
            MyRenderQualityEnum renderQuality,
            float lodTransitionDistanceNear,
            float lodTransitionDistanceFar,
            float lodTransitionDistanceBackgroundStart,
            float lodTransitionDistanceBackgroundEnd,
            float environmentLodTransitionDistance,
            float environmentLodTransitionDistanceBackground,
            bool enableCascadeBlending)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderQuality>(MyRenderMessageEnum.UpdateRenderQuality);

            message.RenderQuality = renderQuality;
            message.LodTransitionDistanceNear = lodTransitionDistanceNear;
            message.LodTransitionDistanceFar = lodTransitionDistanceFar;
            message.LodTransitionDistanceBackgroundStart = lodTransitionDistanceBackgroundStart;
            message.LodTransitionDistanceBackgroundEnd = lodTransitionDistanceBackgroundEnd;
            message.EnvironmentLodTransitionDistance = environmentLodTransitionDistance;
            message.EnvironmentLodTransitionDistanceBackground = environmentLodTransitionDistanceBackground;
            message.EnableCascadeBlending = enableCascadeBlending;

            EnqueueMessage(message);
        }

        public static void TakeScreenshot(VRageMath.Vector2 sizeMultiplier, string pathToSave, bool debug, bool ignoreSprites, bool showNotification)
        {
            if (debug && pathToSave != null)
                throw new ArgumentException("When taking debug screenshot, path to save must be null, becase debug takes a lot of screenshots");

            var message = MessagePool.Get<MyRenderMessageTakeScreenshot>(MyRenderMessageEnum.TakeScreenshot);

            message.IgnoreSprites = ignoreSprites;
            message.SizeMultiplier = sizeMultiplier;
            message.PathToSave = pathToSave;
            message.Debug = debug;
            message.ShowNotification = showNotification;

            EnqueueMessage(message);
        }
        public static void RenderColoredTextures(List<renderColoredTextureProperties> texturesToRender)
        {
            var message = MessagePool.Get<MyRenderMessageRenderColoredTexture>(MyRenderMessageEnum.RenderColoredTexture);
            message.texturesToRender = texturesToRender;
            EnqueueMessage(message);
        }

        public static void ScreenshotTaken(bool success, string filename, bool showNotification)
        {
            var message = MessagePool.Get<MyRenderMessageScreenshotTaken>(MyRenderMessageEnum.ScreenshotTaken);

            message.Success = success;
            message.Filename = filename;
            message.ShowNotification = showNotification;

            EnqueueOutputMessage(message);
        }

        public static void ExportToObjComplete(bool success, string filename)
        {
            var message = MessagePool.Get<MyRenderMessageExportToObjComplete>(MyRenderMessageEnum.ExportToObjComplete);

            message.Success = success;
            message.Filename = filename;

            EnqueueOutputMessage(message);
        }
        #endregion

        #region Background

        public static void UpdateDistantImpostors(
     MyImpostorProperties[] impostorProperties
     )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateDistantImpostors>(MyRenderMessageEnum.UpdateDistantImpostors);

            message.ImpostorProperties = impostorProperties;

            EnqueueMessage(message);
        }

        #endregion

        #region Characters

        public static uint CreateRenderCharacter(
         string debugName,
         string lod0,
         MatrixD worldMatrix,
         Color? diffuseColor,
         Vector3? colorMaskHSV,
         RenderFlags flags
         )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderCharacter>(MyRenderMessageEnum.CreateRenderCharacter);

            uint id = m_render.GlobalMessageCounter++;
            message.ID = id;
            message.DebugName = debugName;
            message.Model = lod0;
            message.WorldMatrix = worldMatrix;
            message.DiffuseColor = diffuseColor;
            message.ColorMaskHSV = colorMaskHSV;
            message.Flags = flags;

            EnqueueMessage(message);

            UpdateRenderEntity(id, diffuseColor, colorMaskHSV);

            return id;
        }

        public static void SetCharacterSkeleton(
            uint characterID,
            MySkeletonBoneDescription[] skeletonBones,
            int[] skeletonIndices
            )
        {
            var message = MessagePool.Get<MyRenderMessageSetCharacterSkeleton>(MyRenderMessageEnum.SetCharacterSkeleton);

            message.CharacterID = characterID;
            message.SkeletonBones = skeletonBones;
            message.SkeletonIndices = skeletonIndices;

            EnqueueMessage(message);
        }

        public static bool SetCharacterTransforms(
             uint characterID,
            Matrix[] boneTransforms
        )
        {
            var message = MessagePool.Get<MyRenderMessageSetCharacterTransforms>(MyRenderMessageEnum.SetCharacterTransforms);

            message.CharacterID = characterID;

            if (message.RelativeBoneTransforms == null || message.RelativeBoneTransforms.Length < boneTransforms.Length)
            {
                message.RelativeBoneTransforms = (Matrix[])boneTransforms.Clone();
            }
            else
            {
                for (int i = 0; i < boneTransforms.Length; i++)
                {
                    message.RelativeBoneTransforms[i] = boneTransforms[i];
                }
            }

            EnqueueMessage(message);

            return false;
        }

        #endregion

        #region Debug draw

        //public static void DebugDrawLine3D(Vector3 pointFrom, Vector3 pointTo, Color colorFrom, Color colorTo, bool depthRead)
        //{
        //    DebugDrawLine3D((Vector3D)pointFrom, (Vector3D)pointTo, colorFrom, colorTo, depthRead);
        //}

        public static void DebugDrawLine3D(Vector3D pointFrom, Vector3D pointTo, Color colorFrom, Color colorTo, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawLine3D>(MyRenderMessageEnum.DebugDrawLine3D);

            message.PointFrom = pointFrom;
            message.PointTo = pointTo;
            message.ColorFrom = colorFrom;
            message.ColorTo = colorTo;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }

        public static void DebugDrawLine2D(Vector2 pointFrom, Vector2 pointTo, Color colorFrom, Color colorTo, Matrix? projection = null)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawLine2D>(MyRenderMessageEnum.DebugDrawLine2D);

            message.PointFrom = pointFrom;
            message.PointTo = pointTo;
            message.ColorFrom = colorFrom;
            message.ColorTo = colorTo;
            message.Projection = projection;

            EnqueueMessage(message);
        }

        public static void DebugDrawPoint(Vector3 position, Color color, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawPoint>(MyRenderMessageEnum.DebugDrawPoint);

            message.Position = position;
            message.Color = color;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }

        public static void DebugDrawText2D(Vector2 screenCoord, string text, Color color, float scale, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawText2D>(MyRenderMessageEnum.DebugDrawText2D);

            message.Coord = screenCoord;
            message.Text = text;
            message.Color = color;
            message.Scale = scale;
            message.Align = align;

            EnqueueMessage(message);
        }

        public static void DebugDrawText3D(Vector3D worldCoord, string text, Color color, float scale, bool depthRead, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, int customViewProjection = -1)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawText3D>(MyRenderMessageEnum.DebugDrawText3D);

            message.Coord = worldCoord;
            message.Text = text;
            message.Color = color;
            message.Scale = scale;
            message.DepthRead = depthRead;
            message.Align = align;
            message.CustomViewProjection = customViewProjection;

            EnqueueMessage(message);
        }

        public static void DebugDrawSphere(Vector3D position, float radius, Color color, float alpha, bool depthRead, bool smooth = false, bool cull = true)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawSphere>(MyRenderMessageEnum.DebugDrawSphere);

            message.Position = position;
            message.Radius = radius;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Cull = cull;

            EnqueueMessage(message);
        }

        public static MyDebugDrawBatchAABB DebugDrawBatchAABB(MatrixD worldMatrix, Color color, bool depthRead, bool shaded)
        {
            return new MyDebugDrawBatchAABB(PrepareDebugDrawTriangles(), ref worldMatrix, ref color, depthRead, shaded);
        }

        public static void DebugDrawAABB(BoundingBoxD aabb, Color color, float alpha, float scale, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawAABB>(MyRenderMessageEnum.DebugDrawAABB);

            message.AABB = aabb;
            message.Color = color;
            message.Alpha = alpha;
            message.Scale = scale;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }

        public static void DebugDrawAxis(MatrixD matrix, float axisLength, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawAxis>(MyRenderMessageEnum.DebugDrawAxis);

            message.Matrix = matrix;
            message.AxisLength = axisLength;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }


        public static void DebugDrawOBB(MyOrientedBoundingBoxD obb, Color color, float alpha, bool depthRead, bool smooth)
        {
            MatrixD obbMatrix = MatrixD.CreateFromQuaternion(obb.Orientation);
            obbMatrix.Right *= obb.HalfExtent.X * 2;
            obbMatrix.Up *= obb.HalfExtent.Y * 2;
            obbMatrix.Forward *= obb.HalfExtent.Z * 2;
            obbMatrix.Translation = obb.Center;

            VRageRender.MyRenderProxy.DebugDrawOBB(obbMatrix, color, alpha, depthRead, smooth);
        }

        public static void DebugDrawCone(
                Vector3 translation,
                Vector3 directionVec,
                Vector3 baseVec,
                Color color,
                bool depthRead
            )
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCone>(MyRenderMessageEnum.DebugDrawCone);

            message.Translation = translation;
            message.DirectionVector = directionVec;
            message.BaseVector = baseVec;
            message.DepthRead = depthRead;
            message.Color = color;

            EnqueueMessage(message);
        }


        public static void DebugDrawOBB(MatrixD matrix, Color color, float alpha, bool depthRead, bool smooth, bool cull = true)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawOBB>(MyRenderMessageEnum.DebugDrawOBB);

            message.Matrix = matrix;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Cull = cull;

            EnqueueMessage(message);
        }

        public static void DebugDrawCylinder(MatrixD worldMatrix, Vector3D vertexA, Vector3D vertexB, float radius, Color color, float alpha, bool depthRead, bool smooth)
        {
            Vector3 offset = (vertexB - vertexA);
            float height = offset.Length();
            float diameter = 2f * radius;

            Matrix m = Matrix.Identity;

            m.Up = offset / height;
            m.Right = Vector3.CalculatePerpendicularVector(m.Up);
            m.Forward = Vector3.Cross(m.Up, m.Right);

            m = Matrix.CreateScale(diameter, height, diameter) * m;
            m.Translation = (vertexA + vertexB) * 0.5f;
            m *= worldMatrix;

            DebugDrawCylinder(m, color, alpha, depthRead, smooth);
        }

        public static void DebugDrawCylinder(Vector3D position, Quaternion orientation, float radius, float height, Color color, float alpha, bool depthRead, bool smooth)
        {
            MatrixD m = MatrixD.CreateFromQuaternion(orientation);
            m.Right *= 2f * radius;
            m.Forward *= 2f * radius;
            m.Up *= height;
            m.Translation = position;

            DebugDrawCylinder(m, color, alpha, depthRead, smooth);
        }

        public static void DebugDrawCylinder(MatrixD matrix, Color color, float alpha, bool depthRead, bool smooth)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCylinder>(MyRenderMessageEnum.DebugDrawCylinder);

            message.Matrix = matrix;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;

            EnqueueMessage(message);
        }

        public static void DebugDrawTriangle(Vector3D vertex0, Vector3D vertex1, Vector3D vertex2, Color color, bool smooth, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawTriangle>(MyRenderMessageEnum.DebugDrawTriangle);

            message.Vertex0 = vertex0;
            message.Vertex1 = vertex1;
            message.Vertex2 = vertex2;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Smooth = smooth;

            EnqueueMessage(message);
        }

        public static void DebugDrawPlane(Vector3D position, Vector3 normal, Color color, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawPlane>(MyRenderMessageEnum.DebugDrawPlane);

            message.Position = position;
            message.Normal = normal;
            message.Color = color;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }

        public static IDrawTrianglesMessage PrepareDebugDrawTriangles()
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawTriangles>(MyRenderMessageEnum.DebugDrawTriangles);

            message.Indices.Clear();
            message.Vertices.Clear();

            return message;
        }

        public static void DebugDrawTriangles(IDrawTrianglesMessage msgInterface, MatrixD worldMatrix, Color color, bool depthRead, bool shaded)
        {
            var message = (MyRenderMessageDebugDrawTriangles)msgInterface;

            message.Color = color;
            message.WorldMatrix = worldMatrix;
            message.DepthRead = depthRead;
            message.Shaded = shaded;

            EnqueueMessage(message);
        }

        public static void DebugDrawCapsule(Vector3D p0, Vector3D p1, float radius, Color color, bool depthRead, bool shaded = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCapsule>(MyRenderMessageEnum.DebugDrawCapsule);

            message.P0 = p0;
            message.P1 = p1;
            message.Radius = radius;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Shaded = shaded;

            EnqueueMessage(message);
        }

        public static void DebugDrawModel(string model, MatrixD worldMatrix, Color color, bool depthRead)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawModel>(MyRenderMessageEnum.DebugDrawModel);

            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.Color = color;
            message.DepthRead = depthRead;

            EnqueueMessage(message);
        }

        public static void DebugCrashRenderThread()
        {
            var message = MessagePool.Get<MyRenderMessageDebugCrashRenderThread>(MyRenderMessageEnum.DebugCrashRenderThread);
            EnqueueMessage(message);
        }

        #endregion

        public static void CollectGarbage()
        {
            var message = MessagePool.Get<MyRenderMessageCollectGarbage>(MyRenderMessageEnum.CollectGarbage);
            EnqueueMessage(message);
        }

        public static void SpriteScissorPop()
        {
            var message = MessagePool.Get<MyRenderMessageSpriteScissorPop>(MyRenderMessageEnum.SpriteScissorPop);
            EnqueueMessage(message);
        }

        public static void SpriteScissorPush(Rectangle screenRectangle)
        {
            var message = MessagePool.Get<MyRenderMessageSpriteScissorPush>(MyRenderMessageEnum.SpriteScissorPush);
            message.ScreenRectangle = screenRectangle;
            EnqueueMessage(message);
        }

        public static void RequestVideoAdapters()
        {
            var message = MessagePool.Get<MyRenderMessageVideoAdaptersRequest>(MyRenderMessageEnum.VideoAdaptersRequest);
            EnqueueMessage(message);
        }

        public static void SendVideoAdapters(MyAdapterInfo[] adapters)
        {
            var message = MessagePool.Get<MyRenderMessageVideoAdaptersResponse>(MyRenderMessageEnum.VideoAdaptersResponse);
            message.Adapters = adapters;
            EnqueueOutputMessage(message);
        }

        public static void SendCreatedDeviceSettings(MyRenderDeviceSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageCreatedDeviceSettings>(MyRenderMessageEnum.CreatedDeviceSettings);
            message.Settings = settings;
            EnqueueOutputMessage(message);
        }

        public static void SwitchDeviceSettings(MyRenderDeviceSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageSwitchDeviceSettings>(MyRenderMessageEnum.SwitchDeviceSettings);
            message.Settings = settings;
            EnqueueMessage(message);
        }

        public static void SwitchRenderSettings(MyRenderSettings1 settings)
        {
            var message = MessagePool.Get<MyRenderMessageSwitchRenderSettings>(MyRenderMessageEnum.SwitchRenderSettings);
            message.Settings = settings;
            EnqueueMessage(message);
        }

        public static void SwitchProsprocessSettings(MyPostprocessSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdatePostprocessSettings>(MyRenderMessageEnum.UpdatePostprocessSettings);
            message.Settings = settings;
            EnqueueMessage(message);
        }

        public static void SendClipmapsReady()
        {
            EnqueueOutputMessage(MessagePool.Get<MyRenderMessageClipmapsReady>(MyRenderMessageEnum.ClipmapsReady));
        }

        public static uint CreateDecal(int parentId, Matrix localOBB, string material = "")
        {
            var message = MessagePool.Get<MyRenderMessageCreateScreenDecal>(MyRenderMessageEnum.CreateScreenDecal);
            message.ID = m_render.GlobalMessageCounter++;
            message.ParentID = (uint)parentId;
            message.LocalOBB = localOBB;
            message.DecalMaterial = material;

            EnqueueMessage(message);

            return message.ID;
        }

        public static void RemoveDecal(uint decalId)
        {
            var message = MessagePool.Get<MyRenderMessageRemoveDecal>(MyRenderMessageEnum.RemoveDecal);
            message.ID = decalId;

            EnqueueMessage(message);
        }

        public static void RegisterDecals(List<string> names, List<MyDecalMaterialDesc> descriptions)
        {
            var message = MessagePool.Get<MyRenderMessageRegisterScreenDecalsMaterials>(MyRenderMessageEnum.RegisterDecalsMaterials);
            message.MaterialsNames = names;
            message.MaterialsDescriptions = descriptions;

            EnqueueMessage(message);
        }

        public static void HandleFocusMessage(MyWindowFocusMessage msg)
        {
            m_render.HandleFocusMessage(msg);
        }
    }

    public enum MyWindowFocusMessage
    {
        Activate,
        SetFocus
    }

    public struct MyDebugDrawBatchAABB : IDisposable
    {
        IDrawTrianglesMessage m_msg;
        MatrixD m_worldMatrix;
        Color m_color;
        bool m_depthRead;
        bool m_shaded;

        internal MyDebugDrawBatchAABB(IDrawTrianglesMessage msg, ref MatrixD worldMatrix, ref Color color, bool depthRead, bool shaded)
        {
            m_msg = msg;
            m_worldMatrix = worldMatrix;
            m_color = color;
            m_depthRead = depthRead;
            m_shaded = shaded;
        }

        public void Add(ref BoundingBoxD aabb)
        {
            int baseVertex = m_msg.VertexCount;

            m_msg.AddVertex(new Vector3D(aabb.Min.X, aabb.Min.Y, aabb.Min.Z));
            m_msg.AddVertex(new Vector3D(aabb.Max.X, aabb.Min.Y, aabb.Min.Z));
            m_msg.AddVertex(new Vector3D(aabb.Min.X, aabb.Min.Y, aabb.Max.Z));
            m_msg.AddVertex(new Vector3D(aabb.Max.X, aabb.Min.Y, aabb.Max.Z));
            m_msg.AddVertex(new Vector3D(aabb.Min.X, aabb.Max.Y, aabb.Min.Z));
            m_msg.AddVertex(new Vector3D(aabb.Max.X, aabb.Max.Y, aabb.Min.Z));
            m_msg.AddVertex(new Vector3D(aabb.Min.X, aabb.Max.Y, aabb.Max.Z));
            m_msg.AddVertex(new Vector3D(aabb.Max.X, aabb.Max.Y, aabb.Max.Z));

            // bottom
            m_msg.AddIndex(baseVertex + 1); m_msg.AddIndex(baseVertex + 0); m_msg.AddIndex(baseVertex + 2);
            m_msg.AddIndex(baseVertex + 1); m_msg.AddIndex(baseVertex + 2); m_msg.AddIndex(baseVertex + 3);

            // top
            m_msg.AddIndex(baseVertex + 4); m_msg.AddIndex(baseVertex + 5); m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 6); m_msg.AddIndex(baseVertex + 5); m_msg.AddIndex(baseVertex + 7);

            // front
            m_msg.AddIndex(baseVertex + 0); m_msg.AddIndex(baseVertex + 1); m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 4); m_msg.AddIndex(baseVertex + 1); m_msg.AddIndex(baseVertex + 5);

            // back
            m_msg.AddIndex(baseVertex + 3); m_msg.AddIndex(baseVertex + 2); m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 3); m_msg.AddIndex(baseVertex + 6); m_msg.AddIndex(baseVertex + 7);

            // right
            m_msg.AddIndex(baseVertex + 1); m_msg.AddIndex(baseVertex + 3); m_msg.AddIndex(baseVertex + 5);
            m_msg.AddIndex(baseVertex + 5); m_msg.AddIndex(baseVertex + 3); m_msg.AddIndex(baseVertex + 7);

            // left
            m_msg.AddIndex(baseVertex + 4); m_msg.AddIndex(baseVertex + 2); m_msg.AddIndex(baseVertex + 0);
            m_msg.AddIndex(baseVertex + 4); m_msg.AddIndex(baseVertex + 6); m_msg.AddIndex(baseVertex + 2);
        }

        public void Dispose()
        {
            MyRenderProxy.DebugDrawTriangles(m_msg, m_worldMatrix, m_color, m_depthRead, m_shaded);
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}

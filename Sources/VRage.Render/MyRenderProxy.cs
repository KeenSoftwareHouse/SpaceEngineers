#region Using

using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using VRage;
using VRage.Generics;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender.ExternalApp;
using VRageRender.Import;
using VRageRender.Messages;
using VRageRender.Voxels;
using BoundingBox = VRageMath.BoundingBox;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using Quaternion = VRageMath.Quaternion;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using BoundingFrustrum = VRageMath.BoundingFrustum;

#endregion

namespace VRageRender
{
    [Unsharper.UnsharperStaticInitializersPriority(1)]
    public static class MyRenderProxy
    {
        public enum MyStatsState
        {
            NoDraw,
            // Stats signalize to debug screen that they turned the last page and should be removed next
            // (on next MoveNext -- no stats are drawn but the debug screen is still there)
            ShouldFinish,
            Draw,
            MoveNext,
        }

        #region Fields

        private static readonly char[] m_invalidGeneratedTextureChars = new char[] { '(', ')','<', '>', '|', '\\', '/', '\0', '\t', ' ', '.', ',' };

        public static MyStatsState DrawRenderStats = MyStatsState.NoDraw;

        public static List<Vector3D> PointsForVoxelPrecache = new List<Vector3D>();

        private static bool m_settingsDirty = true;

        static IMyRender m_render = null;

        public const uint RENDER_ID_UNASSIGNED = 0xFFFFFFFF;

        public static MyRenderThread RenderThread { get; private set; }

        public static MyRenderSettings Settings = MyRenderSettings.Default;

        public static MyRenderDebugOverrides DebugOverrides = new MyRenderDebugOverrides();

        public static MyMessagePool MessagePool = new MyMessagePool();

        public static Action WaitForFlushDelegate = null;

        public static bool LimitMaxQueueSize = false;

        public static bool EnableAppEventsCall = true;

        #endregion

        #region Properties

        public static List<MyBillboard> BillboardsRead
        {
            get { return m_render.SharedData.Billboards.Read.Billboards; }
        }

        public static List<MyBillboard> BillboardsWrite
        {
            get { return m_render.SharedData.Billboards.Write.Billboards; }
        }

        public static Dictionary<int, MyBillboardViewProjection> BillboardsViewProjectionRead
        {
            get { return m_render.SharedData.Billboards.Read.Matrices; }
        }

        public static Dictionary<int, MyBillboardViewProjection> BillboardsViewProjectionWrite
        {
            get { return m_render.SharedData.Billboards.Write.Matrices; }
        }

        public static MyObjectsPoolSimple<MyBillboard> BillboardsPoolRead
        {
            get { return m_render.SharedData.Billboards.Read.Pool; }
        }

        public static MyObjectsPoolSimple<MyBillboard> BillboardsPoolWrite
        {
            get { return m_render.SharedData.Billboards.Write.Pool; }
        }

        public static MyObjectsPoolSimple<MyTriangleBillboard> TriangleBillboardsPoolRead
        {
            get { return m_render.SharedData.TriangleBillboards.Read.Pool; }
        }

        public static MyObjectsPoolSimple<MyTriangleBillboard> TriangleBillboardsPoolWrite
        {
            get { return m_render.SharedData.TriangleBillboards.Write.Pool; }
        }

        public static HashSet<uint> VisibleObjectsRead
        {
            get { return m_render.SharedData != null ? m_render.SharedData.VisibleObjects.Read : null; }
        }

        public static HashSet<uint> VisibleObjectsWrite
        {
            get { return m_render.SharedData != null ? m_render.SharedData.VisibleObjects.Write : null; }
        }

        public static MyTimeSpan CurrentDrawTime
        {
            get { return m_render.CurrentDrawTime; }
            set { m_render.CurrentDrawTime = value; }
        }

        public static MyViewport MainViewport
        {
            get { return m_render.MainViewport; }
        }

        public static Vector2I BackBufferResolution
        {
            get { return m_render.BackBufferResolution; }
        }

        public static MyLog Log
        {
            get { return m_render.Log; }
        }

        public static bool IsInstantiated
        {
            get { return m_render != null; }
        }

        public static bool SettingsDirty
        {
            get { return m_settingsDirty; }
        }

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

            if (m_render != null)
                m_render.DisposeDevice();
            RenderThread = null;
        }

        public static long GetAvailableTextureMemory()
        {
            AssertRenderThread();
            return m_render.GetAvailableTextureMemory();
        }

        public static MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            AssertRenderThread();
            return m_render.TestDeviceCooperativeLevel();
        }

        public static bool ResetDevice()
        {
            AssertRenderThread();
            return m_render.ResetDevice();
        }

        public static void DrawBegin()
        {
            AssertRenderThread();
            m_render.DrawBegin();
        }

        public static void DrawEnd()
        {
            AssertRenderThread();
            m_render.DrawEnd();
        }

        #endregion

        #region Swapchain

        public static bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            AssertRenderThread();
            return m_render.SettingsChanged(settings);
        }

        public static void ApplySettings(MyRenderDeviceSettings settings)
        {
            AssertRenderThread();
            m_render.ApplySettings(settings);
        }

        public static void Present()
        {
            AssertRenderThread();
            m_render.Present();
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

        private static void EnqueueMessage(MyRenderMessageBase message)
        {
            Debug.Assert(message != null);

            m_render.EnqueueMessage(message, LimitMaxQueueSize);
        }

        // TODO: OP! make time mandatory
        public static void BeforeRender(MyTimeSpan? currentDrawTime)
        {
            AssertRenderThread();
            m_render.SharedData.BeforeRender(currentDrawTime);
        }

        public static void AfterRender()
        {
            AssertRenderThread();
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
                m_render.SharedData.AfterUpdate(updateTimestamp);
        }

        public static void ProcessMessages()
        {
            AssertRenderThread();
            m_render.Draw(false);
        }

        public static void Draw()
        {
            AssertRenderThread();
            m_render.Draw(true);
        }

        public static MyRenderProfiler GetRenderProfiler()
        {
            return m_render.GetRenderProfiler();
        }

        private static readonly SpinLockRef m_messageIdLock = new SpinLockRef();

        public enum ObjectType
        {
            Entity,
            InstanceBuffer,
            Clipmap,
            Light,
            Video,
            DebugDrawMesh,
            ScreenDecal,
            Cloud,
            Atmosphere,
            GPUEmitter,

            Max
        };
        private static readonly bool[] m_trackObjectType = new bool[(int) ObjectType.Max];

        private static readonly Dictionary<uint, ObjectType> m_objectTypes = new Dictionary<uint, ObjectType>();
        private static readonly HashSet<uint> m_objectsToRemove = new HashSet<uint>();

        public static Dictionary<uint, ObjectType> ObjectTypes
        {
            get { return m_objectTypes; }
        }


        private static void TrackNewMessageId(ObjectType type)
        {
            if (m_trackObjectType[(int)type])
                m_objectTypes.Add(m_render.GlobalMessageCounter, type);
            if (MyCompilationSymbols.LogRenderGIDs)
                Debug.WriteLine(m_render.GlobalMessageCounter + ": " + type + " added @ " + Environment.StackTrace);
        }

        private static uint GetMessageId(ObjectType type, bool track = true)
        {
            using (m_messageIdLock.Acquire())
            {
                if (track)
                    TrackNewMessageId(type);
#if XB1
                uint v = m_render.GlobalMessageCounter;
                m_render.GlobalMessageCounter = m_render.GlobalMessageCounter + 1;
                return v;
#else
                return m_render.GlobalMessageCounter++;
#endif
            }
        }

        public static void SetSettingsDirty()
        {
            m_settingsDirty = true;
        }

        public static void RemoveMessageId(uint GID, ObjectType type, bool now = true)
        {
            if (!m_trackObjectType[(int) type])
                return;

            using (m_messageIdLock.Acquire())
            {
                ObjectType objectType;
                if (ObjectTypes.TryGetValue(GID, out objectType))
                {
                    if (MyCompilationSymbols.LogRenderGIDs)
                        Debug.WriteLine(GID + ": " + type + (now ? " removed @ " : " will be removed @ ") + Environment.StackTrace);

                    Assert(m_objectTypes[GID] == type);
                    if (now)
                    {
                        m_objectTypes.Remove(GID);
                        m_objectsToRemove.Remove(GID);
                    }
                    else m_objectsToRemove.Add(GID);
                }
                else Assert(false);
            }
        }

        public static void CheckMessageId(uint GID, ObjectType[] allowedTypes = null)
        {
            ObjectType objectType;
            Debug.Assert(GID != RENDER_ID_UNASSIGNED, "Render object ID is not assigned");
            if (ObjectTypes.TryGetValue(GID, out objectType))
            {
                if (allowedTypes != null)
                {
                    bool found = false;
                    foreach (var item in allowedTypes)
                        if (item == objectType)
                        {
                            found = true;
                            break;
                        }
                    Assert(found);
                }
                Assert(!m_objectsToRemove.Contains(GID));
                //Debug.WriteLine(GID + ": " + objectType + " checked @ " + Environment.StackTrace);
            }
            else Assert(false);
        }

        public static void CheckMessageId(uint GID, ObjectType allowedType)
        {
            CheckMessageId(GID, new ObjectType[] {allowedType});
        }

        #endregion

        #region Global

        public static void Initialize(IMyRender render)
        {
            for (int i = 0; i < (int)ObjectType.Max; i++)
                m_trackObjectType[i] = true;
            m_trackObjectType[(int)ObjectType.ScreenDecal] = false;

            m_render = render;
            UpdateDebugOverrides();
            ProfilerShort.SetProfiler(render.GetRenderProfiler());
        }

        public static void LoadContent(MyRenderQualityEnum quality)
        {
            AssertRenderThread();
            GetRenderProfiler().StartProfilingBlock("Load Content");
            m_render.LoadContent(quality);
            GetRenderProfiler().EndProfilingBlock();
        }

        public static void UnloadContent()
        {
            AssertRenderThread();
            GetRenderProfiler().StartProfilingBlock("Unload Content");
            m_render.UnloadContent();

            ClearLargeMessages();
            GetRenderProfiler().EndProfilingBlock();
        }

        public static void ClearLargeMessages()
        {
            MessagePool.Clear(MyRenderMessageEnum.CreateRenderInstanceBuffer);
            MessagePool.Clear(MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer);
            MessagePool.Clear(MyRenderMessageEnum.UpdateRenderInstanceBufferSettings);
        }

        public static void UnloadData()
        {
            ClearLargeMessages();

            var message = MessagePool.Get<MyRenderMessageUnloadData>(MyRenderMessageEnum.UnloadData);

            MyRenderProxy.CheckRenderObjectIds();

            EnqueueMessage(message);
        }

        public static void CheckRenderObjectIds()
        {
            using (m_messageIdLock.Acquire())
            {
#if DEBUG
                if (m_objectTypes.Count != m_objectsToRemove.Count)
                {
                    foreach (var item in m_objectTypes)
                    {
                        if (!m_objectsToRemove.Contains(item.Key))
                        {
                            Debug.WriteLine("renderId #" + item.Key + " typeof " + item.Value);
                        }
                    }
                }
#endif
                Debug.Assert(m_objectTypes.Count == m_objectsToRemove.Count, "Some render objects weren't properly disposed! Enable debug and see debug output for the list of objects");
            }
        }


        public static void SetGlobalValues(string rootDirectory, string rootDirectoryEffects, string rootDirectoryDebug)
        {
            m_render.RootDirectory = rootDirectory;
            m_render.RootDirectoryEffects = rootDirectoryEffects;
            m_render.RootDirectoryDebug = rootDirectoryDebug;
        }

        public static void GenerateShaderCache(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
            m_render.GenerateShaderCache(clean, onShaderCacheProgress);
        }

        #endregion

        #region Sprites

        public static void DrawSprite(string texture, ref RectangleF destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, Vector2 rightVector, ref Vector2 origin, SpriteEffects effects, float depth, bool waitTillLoaded = true, string targetTexture = null)
        {
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
            message.WaitTillLoaded = waitTillLoaded;
            message.TargetTexture = targetTexture;

            EnqueueMessage(message);
        }

        // RotSpeed in rad/s
        public static void DrawSprite(string texture, Vector2 normalizedCoord, Vector2 normalizedSize, Color color, MyGuiDrawAlignEnum drawAlign, float rotation, Vector2 rightVector, float scale, Vector2? originNormalized, float rotSpeed = 0, bool waitTillLoaded = true, string targetTexture = null)
        {
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
            message.WaitTillLoaded = waitTillLoaded;
            message.TargetTexture = targetTexture;

            EnqueueMessage(message);
        }

        public static void DrawSpriteAtlas(string texture, Vector2 position, Vector2 textureOffset, Vector2 textureSize, Vector2 rightVector, Vector2 scale, Color color, Vector2 halfSize, string targetTexture = null)
        {
            var message = MessagePool.Get<MyRenderMessageDrawSpriteAtlas>(MyRenderMessageEnum.DrawSpriteAtlas);

            message.Texture = texture;
            message.Position = position;
            message.TextureOffset = textureOffset;
            message.TextureSize = textureSize;
            message.RightVector = rightVector;
            message.Scale = scale;
            message.Color = color;
            message.HalfSize = halfSize;
            message.TargetTexture = targetTexture;

            EnqueueMessage(message);
        }

        public static void SpriteScissorPop(string targetTexture = null)
        {
            var message = MessagePool.Get<MyRenderMessageSpriteScissorPop>(MyRenderMessageEnum.SpriteScissorPop);
            EnqueueMessage(message);
            message.TargetTexture = targetTexture;
        }

        public static void SpriteScissorPush(Rectangle screenRectangle, string targetTexture = null)
        {
            var message = MessagePool.Get<MyRenderMessageSpriteScissorPush>(MyRenderMessageEnum.SpriteScissorPush);
            message.ScreenRectangle = screenRectangle;
            message.TargetTexture = targetTexture;
            EnqueueMessage(message);
        }

        public static void CreateFont(int fontId, string fontPath, bool isDebugFont = false, string targetTexture = null)
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
            string text,
            float screenScale,
            float screenMaxWidth,
            string targetTexture = null)
        {
            var message = MessagePool.Get<MyRenderMessageDrawString>(MyRenderMessageEnum.DrawString);
            message.Text = text;
            message.FontIndex = fontIndex;
            message.ScreenCoord = screenCoord;
            message.ColorMask = colorMask;
            message.ScreenScale = screenScale;
            message.ScreenMaxWidth = screenMaxWidth;
            message.TargetTexture = targetTexture;

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

        /// <returns>True if the texture name is valid and doesn't contant reserved characters</returns>
        public static bool IsValidGeneratedTextureName(string name)
        {
            return name.IndexOfAny(m_invalidGeneratedTextureChars) < 0;
        }

        public static void CheckValidGeneratedTextureName(string name)
        {
            if (!IsValidGeneratedTextureName(name))
                throw new Exception("Generated texture must not contain any of the following characters: '(', ')','<', '>', '|', '\\', '/', '\0', '\t', ' ', '.', ','");
        }

        /// <returns>Qualified texture</returns>
        public static void CreateGeneratedTexture(string textureName, int width, int height, MyGeneratedTextureType type = MyGeneratedTextureType.RGBA, int numMiplevels = 1)
        {
            CheckValidGeneratedTextureName(textureName);

            if (numMiplevels <= 0) throw new ArgumentOutOfRangeException("numMiplevels");

            var message = MessagePool.Get<MyRenderMessageCreateGeneratedTexture>(MyRenderMessageEnum.CreateGeneratedTexture);

            message.TextureName = textureName;
            message.Width = width;
            message.Height = height;
            message.Type = type;
            message.NumMipLevels = numMiplevels;

            EnqueueMessage(message);
        }

        public static void ResetGeneratedTexture(string textureName, byte[] data)
        {
            var message = MessagePool.Get<MyRenderMessageResetGeneratedTexture>(MyRenderMessageEnum.ResetGeneratedTexture);

            message.TextureName = textureName;
            message.Data = data;

            EnqueueMessage(message);
        }

        public static void PrintAllFileTexturesIntoLog()
        {
            var message = MessagePool.Get<MyRenderMessageDebugPrintAllFileTexturesIntoLog>(MyRenderMessageEnum.DebugPrintAllFileTexturesIntoLog);
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

        public static uint CreateRenderEntityCloudLayer(
            string debugName,
            string model,
            List<string> textures,
            Vector3D centerPoint,
            double altitude,
            double minScaledAltitude,
            bool scalingEnabled,
            double fadeOutRelativeAltitudeStart,
            double fadeOutRelativeAltitudeEnd,
            float applyFogRelativeDistance,
            double maxPlanetHillRadius,
            MyMeshDrawTechnique technique,
            RenderFlags flags,
            CullingOptions cullingOptions,
            Vector3D rotationAxis,
            float angularVelocity,
            float initialRotation
            )
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderEntityClouds>(MyRenderMessageEnum.CreateRenderEntityClouds);

            uint id = GetMessageId(ObjectType.Cloud);
            message.ID = id;
            message.Model = model;
            message.Textures = textures;
            message.CenterPoint = centerPoint;
            message.Altitude = altitude;
            message.MinScaledAltitude = minScaledAltitude;
            message.ScalingEnabled = scalingEnabled;
            message.DebugName = debugName;
            message.Technique = technique;
            message.RotationAxis = rotationAxis;
            message.AngularVelocity = angularVelocity;
            message.InitialRotation = initialRotation;
            message.MaxPlanetHillRadius = maxPlanetHillRadius;
            message.FadeOutRelativeAltitudeStart = fadeOutRelativeAltitudeStart;
            message.FadeOutRelativeAltitudeEnd = fadeOutRelativeAltitudeEnd;
            message.ApplyFogRelativeDistance = applyFogRelativeDistance;

            EnqueueMessage(message);

            //UpdateRenderEntity(id, null, null);

            return id;
        }

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

            uint id = GetMessageId(ObjectType.Atmosphere);
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

            //Color zeroColor = Vector3.Zero;
            //UpdateRenderEntity(id, zeroColor, Vector3.Zero, dithering);

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
            float maxViewDistance = float.MaxValue,
            byte depthBias = 0,
            float rescale = 1.0f
            )
        {
            Debug.Assert(worldMatrix.IsValid() && !worldMatrix.Forward.Equals(Vector3D.Zero, 0.01f));
            var message = MessagePool.Get<MyRenderMessageCreateRenderEntity>(MyRenderMessageEnum.CreateRenderEntity);

            uint id = GetMessageId(ObjectType.Entity);
            message.ID = id;
            message.DebugName = debugName;
            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.Technique = technique;
            message.Flags = flags;
            message.CullingOptions = cullingOptions;
            message.MaxViewDistance = maxViewDistance;
            message.Rescale = rescale;
            message.DepthBias = depthBias;

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

            uint id = GetMessageId(ObjectType.Entity);
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

        /// <param name="parentId">Parent of the instance. Currently used for debugging. May be left unassingned</param>
        public static uint CreateRenderInstanceBuffer(string debugName, MyRenderInstanceBufferType type, int parentId = -1)
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderInstanceBuffer>(MyRenderMessageEnum.CreateRenderInstanceBuffer);

            uint id = GetMessageId(ObjectType.InstanceBuffer);
            message.ID = id;
            message.ParentID = parentId == -1 ? MyRenderProxy.RENDER_ID_UNASSIGNED : (uint) parentId;
            message.DebugName = debugName;
            message.Type = type;
            EnqueueMessage(message);

            return id;
        }

        public static void UpdateRenderCubeInstanceBuffer(uint id, List<MyCubeInstanceData> instanceData, int capacity, List<MyCubeInstanceDecalData> decalData = null)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderCubeInstanceBuffer>(MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer);

            message.ID = id;
            message.InstanceData.Clear();
            message.InstanceData.AddList(instanceData);
            if (message.InstanceData.Count < message.InstanceData.Capacity &&
                message.InstanceData.Capacity > 20000)
                message.InstanceData.TrimExcess();
            message.Capacity = capacity;
            if (decalData != null)
                message.DecalsData.AddRange(decalData);

            EnqueueMessage(message);
        }


        public static void UpdateRenderInstanceBufferSettings(uint id, bool enablePerInstanceLod = false)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderInstanceBufferSettings>(MyRenderMessageEnum.UpdateRenderInstanceBufferSettings);

            message.ID = id;
            message.SetPerInstanceLod = enablePerInstanceLod;

            EnqueueMessage(message);
        }

        public static void UpdateRenderInstanceBufferRange(uint id, MyInstanceData[] instanceData, int offset = 0, bool trimEnd = false)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderInstanceBufferRange>(MyRenderMessageEnum.UpdateRenderInstanceBufferRange);

            message.ID = id;
            message.InstanceData = instanceData;
            message.StartOffset = offset;
            message.Trim = trimEnd;
            

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

            uint id = GetMessageId(ObjectType.Entity);
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

        public static void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projectionMatrix, float safenear, float fov, float nearPlane, float farPlane, float nearObjectsNearPlane, float nearObjectsFarPlane, Vector3D cameraPosition, int lastMomentUpdateIndex = 1)
        {
            var message = MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);

            cameraPosition.AssertIsValid();

            message.ViewMatrix = viewMatrix;
            message.ProjectionMatrix = projectionMatrix;
            message.SafeNear = safenear;
            message.FOV = fov;
            message.NearPlane = nearPlane;
            message.FarPlane = farPlane;
            message.NearObjectsNearPlane = nearObjectsNearPlane;
            message.NearObjectsFarPlane = nearObjectsFarPlane;
            message.CameraPosition = cameraPosition;
            message.LastMomentUpdateIndex = lastMomentUpdateIndex;

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
            BoundingBoxD? aabb = null,
            int lastMomentUpdateIndex = -1
            )
        {
            Debug.Assert(worldMatrix.IsValid() && !worldMatrix.Forward.Equals(Vector3D.Zero, 0.01f));

            CheckMessageId(id, new ObjectType[] { ObjectType.Entity, ObjectType.Clipmap });
            var message = MessagePool.Get<MyRenderMessageUpdateRenderObject>(MyRenderMessageEnum.UpdateRenderObject);

            message.ID = id;
            message.WorldMatrix = worldMatrix;
            message.SortIntoCulling = sortIntoCulling;
            message.AABB = aabb;
            message.LastMomentUpdateIndex = lastMomentUpdateIndex;

            EnqueueMessage(message);
        }

        public static void UpdateRenderObjectVisibility(
            uint id,
            bool visible,
            bool near
            )
        {
            CheckMessageId(id, ObjectType.Entity);
            var message = MessagePool.Get<MyRenderMessageUpdateRenderObjectVisibility>(MyRenderMessageEnum.UpdateRenderObjectVisibility);

            message.ID = id;
            message.Visible = visible;
            message.NearFlag = near;

            EnqueueMessage(message);
        }


        public static void RemoveRenderObject(uint id)
        {
            CheckMessageId(id);
            System.Diagnostics.Debug.Assert(id != RENDER_ID_UNASSIGNED, "Removing unassigned id");

            RemoveMessageId(id, ObjectTypes[id], false);
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
            CheckMessageId(id, ObjectType.Entity);
            Debug.Assert(dithering >= -1 && dithering <= 1, "Dithering is out of the range. The range -1..0 is used for holograph, the range 0..1 is used for general dithering. Values out of those ranges are not supported. If they should be, plaese contact the render team.");
            var message = MessagePool.Get<MyRenderMessageUpdateRenderEntity>(MyRenderMessageEnum.UpdateRenderEntity);

            message.ID = id;
            message.DiffuseColor = diffuseColor;
            message.ColorMaskHSV = colorMaskHsv;
            message.Dithering = dithering;

            EnqueueMessage(message);
        }

        public static void SetInstanceBuffer(uint entityId, uint instanceBufferId, int instanceStart, int instanceCount, BoundingBox entityLocalAabb, MyInstanceData [] instanceData = null)
        {
            CheckMessageId(entityId, ObjectType.Entity);
            var message = MessagePool.Get<MyRenderMessageSetInstanceBuffer>(MyRenderMessageEnum.SetInstanceBuffer);

            message.ID = entityId;
            message.InstanceBufferId = instanceBufferId;
            message.InstanceStart = instanceStart;
            message.InstanceCount = instanceCount;
            message.LocalAabb = entityLocalAabb;
            message.InstanceData = instanceData;

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
            Vector3? atmosphereWaveLenghts = null,
            bool spherizeWithDistance = true,
            RenderFlags additionalFlags = 0,
            MyClipmap.PruningFunc prunningFunc = null)
        {
            var message = MessagePool.Get<MyRenderMessageCreateClipmap>(MyRenderMessageEnum.CreateClipmap);

            uint clipmapId = GetMessageId(ObjectType.Clipmap);
            message.ClipmapId = clipmapId;
            message.WorldMatrix = worldMatrix;
            message.SizeLod0 = sizeLod0;
            message.ScaleGroup = scaleGroup;
            message.AtmosphereRadius = atmosphereRadius;
            message.PlanetRadius = planetRadius;
            message.HasAtmosphere = hasAtmosphere;
            message.Position = position;
            message.AtmosphereWaveLenghts = atmosphereWaveLenghts;
            message.SpherizeWithDistance = spherizeWithDistance;
            message.AdditionalRenderFlags = additionalFlags;
            message.PrunningFunc = prunningFunc;
            EnqueueMessage(message);

            return clipmapId;
        }

        public static void UpdateClipmapCell(
            uint clipmapId,
            ref MyClipmapCellMeshMetadata metadata,
            ref List<MyClipmapCellBatch> batches)
        {
            CheckMessageId(clipmapId, ObjectType.Clipmap);
            var message = MessagePool.Get<MyRenderMessageUpdateClipmapCell>(MyRenderMessageEnum.UpdateClipmapCell);

            Debug.Assert(message.Batches.Count == 0, "Message was not properly cleared");

            message.ClipmapId = clipmapId;
            message.Metadata = metadata;
            MyUtils.Swap(ref message.Batches, ref batches);

            EnqueueMessage(message);
        }

        public static void InvalidateClipmapRange(
            uint clipmapId,
            Vector3I minCellLod0,
            Vector3I maxCellLod0)
        {
            CheckMessageId(clipmapId, ObjectType.Clipmap);
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

        public static void UpdateRenderVoxelMaterials(
            MyRenderVoxelMaterialData[] materials
            )
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderVoxelMaterials>(MyRenderMessageEnum.UpdateRenderVoxelMaterials);

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

            uint id = GetMessageId(ObjectType.Entity);
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
            int meshIndex,
            string materialName,
            bool? enabled,
            Color? diffuseColor,
            float? emissivity
            )
        {
            System.Diagnostics.Debug.Assert(id != MyRenderProxy.RENDER_ID_UNASSIGNED);

            var message = MessagePool.Get<MyRenderMessageUpdateModelProperties>(MyRenderMessageEnum.UpdateModelProperties);

            message.ID = id;
            message.LOD = lod;
            message.MeshIndex = meshIndex;
            message.MaterialName = materialName;
            message.Enabled = enabled;
            message.DiffuseColor = diffuseColor;
            message.Emissivity = emissivity;

            EnqueueMessage(message);
        }

        /// <param name="thickness">Zero or negative to remove highlight</param>
        public static void UpdateModelHighlight(
            uint id,
            string[] sectionNames,
            uint[] subpartIndices,
            Color? outlineColor,
            float thickness = -1,
            float pulseTimeInSeconds = 0,
            int instanceIndex = -1
            )
        {
            Debug.Assert(id != RENDER_ID_UNASSIGNED, "Dont highlight invalid entity!");

            var message = MessagePool.Get<MyRenderMessageUpdateModelHighlight>(MyRenderMessageEnum.UpdateModelHighlight);

            message.ID = id;
            message.SectionNames = sectionNames;
            message.SubpartIndices = subpartIndices;
            message.OutlineColor = outlineColor;
            message.Thickness = thickness;
            message.PulseTimeInSeconds = pulseTimeInSeconds;
            message.InstanceIndex = instanceIndex;

            EnqueueMessage(message);
        }

        public static void UpdateColorEmissivity(
            uint id,
            int lod,
            string materialName,
            Color diffuseColor,
            float emissivity
            )
        {
            if (id == RENDER_ID_UNASSIGNED)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            var message = MessagePool.Get<MyRenderMessageUpdateColorEmissivity>(MyRenderMessageEnum.UpdateColorEmissivity);

            message.ID = id;
            message.LOD = lod;
            message.MaterialName = materialName;
            message.DiffuseColor = diffuseColor;
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
            string model,
            float scale = 1.0f)
        {
            var message = MessagePool.Get<MyRenderMessageChangeModel>(MyRenderMessageEnum.ChangeModel);

            message.ID = id;
            message.Model = model;
            message.Scale = scale;

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

        public static void UpdateGameplayFrame(int frame)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateGameplayFrame>(MyRenderMessageEnum.UpdateGameplayFrame);

            message.GameplayFrame = frame;

            EnqueueMessage(message);
        }


        public static void ChangeMaterialTexture(uint id, string materialName, string textureName, MyTextureType textureType = MyTextureType.ColorMetal)
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
            message.Changes.Add(new MyTextureChange { TextureName = textureName, TextureType = textureType });
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

        /// <param name="backgroundColor">null means no background</param>
        /// <param name="blendAlphaChannel">Blend alpha channel</param>
        public static void RenderOffscreenTextureToMaterial(uint renderObjectId, string materialName, string offscreenTexture,
            Color? backgroundColor = null, MyTextureType textureType = MyTextureType.ColorMetal, bool blendAlphaChannel = false)
        {
            CheckValidGeneratedTextureName(offscreenTexture);

            var message = MessagePool.Get<MyRenderMessageRenderOffscreenTextureToMaterial>(MyRenderMessageEnum.RenderOffscreenTextureToMaterial);

            message.OffscreenTexture = offscreenTexture;
            message.MaterialName = materialName;
            message.TextureType = textureType;
            message.BackgroundColor = backgroundColor;
            message.RenderObjectID = renderObjectId;
            message.BlendAlphaChannel = blendAlphaChannel;
            EnqueueMessage(message);
        }

        public static void RenderTextureFreed(int freeResources)
        {
            var message = MessagePool.Get<MyRenderMessageRenderTextureFreed>(MyRenderMessageEnum.RenderTextureFreed);

            message.FreeResources = freeResources;
            EnqueueOutputMessage(message);
        }

        #endregion

        #region Output messages

        private static void EnqueueOutputMessage(MyRenderMessageBase message)
        {
            //System.Diagnostics.Debug.Assert(Thread.CurrentThread == AllowedThread);

            m_render.EnqueueOutputMessage(message);
        }

        public static MyMessageQueue OutputQueue
        {
            get { return m_render.OutputQueue; }
        }

        public static void RequireClipmapCell(uint clipmapId, MyCellCoord cell, Func<int> priority)
        {
            var message = MessagePool.Get<MyRenderMessageRequireClipmapCell>(MyRenderMessageEnum.RequireClipmapCell);

            message.ClipmapId = clipmapId;
            message.Cell = cell;
            message.Priority = priority;

            EnqueueOutputMessage(message);
        }

        #endregion

        #region Lights

        public static uint CreateRenderLight()
        {
            var message = MessagePool.Get<MyRenderMessageCreateRenderLight>(MyRenderMessageEnum.CreateRenderLight);

            uint id = GetMessageId(ObjectType.Light);
            message.ID = id;

            EnqueueMessage(message);

            return id;
        }

        public static void UpdateRenderLight(ref UpdateRenderLightData data)
        {
            Debug.Assert(!data.SpotLightOn || data.SpotLight.Direction.IsValid() && !data.SpotLight.Up.Equals(Vector3.Zero, 0.01f));
            Debug.Assert(!data.SpotLightOn || data.SpotLight.Up.IsValid() && !data.SpotLight.Up.Equals(Vector3.Zero, 0.01f));
            var message = MessagePool.Get<MyRenderMessageUpdateRenderLight>(MyRenderMessageEnum.UpdateRenderLight);
            message.Data = data;
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

        public static void UpdateShadowsSettings(MyShadowsSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateShadowSettings>(MyRenderMessageEnum.UpdateShadowSettings);

            message.Settings.CopyFrom(settings);
            EnqueueMessage(message);
        }

        public static void UpdateNewPipelineSettings(MyNewPipelineSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateNewPipelineSettings>(MyRenderMessageEnum.UpdateNewPipelineSettings);

            message.Settings.CopyFrom(settings);
            EnqueueMessage(message);
        }

        public static void UpdateNewLoddingSettings(MyNewLoddingSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateNewLoddingSettings>(MyRenderMessageEnum.UpdateNewLoddingSettings);

            message.Settings.CopyFrom(settings);
            EnqueueMessage(message);
        }
        
        public static void UpdateMaterialsSettings(MyMaterialsSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateMaterialsSettings>(MyRenderMessageEnum.UpdateMaterialsSettings);

            message.Settings.CopyFrom(settings);
            EnqueueMessage(message);
        }

        public static void UpdateRenderEnvironment(ref MyEnvironmentData data, bool resetEyeAdaptation)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateRenderEnvironment>(MyRenderMessageEnum.UpdateRenderEnvironment);
            message.Data = data;
            message.ResetEyeAdaptation = resetEyeAdaptation;
            EnqueueMessage(message);
        }

        public static void ResetEnvironmentProbes()
        {
            m_render.ResetEnvironmentProbes();
        }

        #endregion

        #region Post processes

        public static void UpdateSSAOSettings(ref MySSAOSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateSSAOSettings>(MyRenderMessageEnum.UpdateSSAOSettings);

            message.Settings = settings;
            EnqueueMessage(message);
        }

        public static void UpdateHBAOSettings(ref MyHBAOData settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateHBAO>(MyRenderMessageEnum.UpdateHBAO);

            message.Settings = settings;

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

        public static void UpdateCloudLayerFogFlag(bool shouldDrawFog)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateCloudLayerFogFlag>(MyRenderMessageEnum.UpdateCloudLayerFogFlag);

            message.ShouldDrawFog = shouldDrawFog;

            EnqueueMessage(message);
        }

        public static void UpdateAtmosphereSettings(uint id, MyAtmosphereSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateAtmosphereSettings>(MyRenderMessageEnum.UpdateAtmosphereSettings);

            message.ID = id;
            message.Settings = settings;

            EnqueueMessage(message);
        }

        public static void EnableAtmosphere(bool enabled)
        {
            var message = MessagePool.Get<MyRenderMessageEnableAtmosphere>(MyRenderMessageEnum.EnableAtmosphere);

            message.Enabled = enabled;

            EnqueueMessage(message);
        }

        #endregion

        #region Video

        public static uint PlayVideo(string videoFile, float volume)
        {
            var message = MessagePool.Get<MyRenderMessagePlayVideo>(MyRenderMessageEnum.PlayVideo);

            uint id = GetMessageId(ObjectType.Video);
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

        public static void DrawVideo(uint id, Rectangle rect, Color color)
        {
            DrawVideo(id, rect, color, MyVideoRectangleFitMode.None);
        }

        public static void DrawVideo(uint id, Rectangle rect, Color color, MyVideoRectangleFitMode fitMode)
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

        #region Cockpit

        #endregion

        #region Billboards

        public static void AddBillboard(MyBillboard billboard)
        {
            if (!DebugOverrides.BillboardsStatic)
                return;
            System.Diagnostics.Debug.Assert(billboard != null);

            billboard.Position0.AssertIsValid();
            billboard.Position1.AssertIsValid();
            billboard.Position2.AssertIsValid();
            billboard.Position3.AssertIsValid();

            BillboardsWrite.Add(billboard);
        }

        public static void AddBillboards(List<MyBillboard> billboards)
        {
            if (!DebugOverrides.BillboardsStatic)
                return;
            System.Diagnostics.Debug.Assert(!billboards.Contains(null));
            BillboardsWrite.AddList(billboards);
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

        public static uint CreateGPUEmitter()
        {
            var message = MessagePool.Get<MyRenderMessageCreateGPUEmitter>(MyRenderMessageEnum.CreateGPUEmitter);

            uint id = GetMessageId(ObjectType.GPUEmitter);
            message.ID = id;

            EnqueueMessage(message);

            return id;
        }

        public static void UpdateGPUEmitters(MyGPUEmitter[] emitters)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateGPUEmitters>(MyRenderMessageEnum.UpdateGPUEmitters);
            message.Emitters = emitters;
            EnqueueMessage(message);
        }

        public static void UpdateGPUEmittersTransform(MyGPUEmitterTransformUpdate[] emitters)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateGPUEmittersTransform>(MyRenderMessageEnum.UpdateGPUEmittersTransform);
            message.Emitters = emitters;
            EnqueueMessage(message);
        }

        public static void UpdateGPUEmittersLight(MyGPUEmitterLight[] emitters)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateGPUEmittersLight>(MyRenderMessageEnum.UpdateGPUEmittersLight);
            message.Emitters = emitters;
            EnqueueMessage(message);
        }

        public static void RemoveGPUEmitter(uint GID, bool instant)
        {
            var message = MessagePool.Get<MyRenderMessageRemoveGPUEmitter>(MyRenderMessageEnum.RemoveGPUEmitter);
            message.GID = GID;
            message.Instant = instant;
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

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string messageText = null)
        {
            if (!condition)
            {
                Debug.Fail(messageText);
                Error(messageText, 1);
            }
        }

        [Conditional("DEBUG")]
        public static void Fail(string messageText)
        {
            Debug.Fail(messageText);
            Error(messageText, 1);
        }

        public static void Error(string messageText, int skipStack = 0, bool shouldTerminate = false)
        {
            var message = MessagePool.Get<MyRenderMessageError>(MyRenderMessageEnum.Error);

            var stack = new System.Diagnostics.StackTrace(1 + skipStack, true);

            message.Callstack = stack.ToString();
            message.Message = messageText;
            message.ShouldTerminate = shouldTerminate;

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

            uint id = GetMessageId(ObjectType.Entity);
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

        public static bool SetCharacterTransforms(uint characterID, Matrix[] boneTransforms, IReadOnlyList<MyBoneDecalUpdate> boneDecalUpdates)
        {
            var message = MessagePool.Get<MyRenderMessageSetCharacterTransforms>(MyRenderMessageEnum.SetCharacterTransforms);

            message.CharacterID = characterID;

            if (message.BoneAbsoluteTransforms == null || message.BoneAbsoluteTransforms.Length < boneTransforms.Length)
                message.BoneAbsoluteTransforms = new Matrix[boneTransforms.Length];

            Array.Copy(boneTransforms, message.BoneAbsoluteTransforms, boneTransforms.Length);
            message.BoneDecalUpdates.AddRange(boneDecalUpdates);

            EnqueueMessage(message);

            return false;
        }

        #endregion

        #region Debug draw

        //public static void DebugDrawLine3D(Vector3 pointFrom, Vector3 pointTo, Color colorFrom, Color colorTo, bool depthRead)
        //{
        //    DebugDrawLine3D((Vector3D)pointFrom, (Vector3D)pointTo, colorFrom, colorTo, depthRead);
        //}

        public static void DebugDrawArrow3D(Vector3D pointFrom, Vector3D pointTo, Color colorFrom, Color colorTo, bool depthRead, double tipScale = 0.1, string text = null, float textSize = 0.5f)
        {
            Vector3D dir = pointTo - pointFrom;
            double l = dir.Length();
            if (l > 0.0001f)
            {
                tipScale *= l;
                dir /= l;
                Vector3D right, up;
                right = Vector3D.CalculatePerpendicularVector(dir);
                up = Vector3D.Cross(right, dir);

                dir *= tipScale;
                up *= tipScale;
                right *= tipScale;

                DebugDrawLine3D(pointTo, pointTo + right - dir, colorTo, colorTo, depthRead);
                DebugDrawLine3D(pointTo, pointTo - right - dir, colorTo, colorTo, depthRead);
                DebugDrawLine3D(pointTo, pointTo + up - dir, colorTo, colorTo, depthRead);
                DebugDrawLine3D(pointTo, pointTo - up - dir, colorTo, colorTo, depthRead);
            }

            DebugDrawLine3D(pointFrom, pointTo, colorFrom, colorTo, depthRead);

            if (text != null && l > 0.0001f)
            {
                DebugDrawText3D(pointTo + dir, text, colorTo, textSize, depthRead, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }

        public static void DebugDrawFrustrum(BoundingFrustrum frustrum, Color color, float alpha, bool depthRead, bool smooth = false, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawFrustrum>(MyRenderMessageEnum.DebugDrawFrustrum);

            message.Frustrum = frustrum;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawLine3D(Vector3D pointFrom, Vector3D pointTo, Color colorFrom, Color colorTo, bool depthRead, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawLine3D>(MyRenderMessageEnum.DebugDrawLine3D);

            message.PointFrom = pointFrom;
            message.PointTo = pointTo;
            message.ColorFrom = colorFrom;
            message.ColorTo = colorTo;
            message.DepthRead = depthRead;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawLine2D(Vector2 pointFrom, Vector2 pointTo, Color colorFrom, Color colorTo, Matrix? projection = null, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawLine2D>(MyRenderMessageEnum.DebugDrawLine2D);

            message.PointFrom = pointFrom;
            message.PointTo = pointTo;
            message.ColorFrom = colorFrom;
            message.ColorTo = colorTo;
            message.Projection = projection;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawPoint(Vector3 position, Color color, bool depthRead, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawPoint>(MyRenderMessageEnum.DebugDrawPoint);

            message.Position = position;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawText2D(Vector2 screenCoord, string text, Color color, float scale,
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawText2D>(MyRenderMessageEnum.DebugDrawText2D);

            message.Coord = screenCoord;
            message.Text = text;
            message.Color = color;
            message.Scale = scale;
            message.Align = align;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawText3D(Vector3D worldCoord, string text, Color color, float scale, bool depthRead,
            MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, int customViewProjection = -1, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawText3D>(MyRenderMessageEnum.DebugDrawText3D);

            message.Coord = worldCoord;
            message.Text = text;
            message.Color = color;
            message.Scale = scale;
            message.DepthRead = depthRead;
            message.Align = align;
            message.CustomViewProjection = customViewProjection;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawSphere(Vector3D position, float radius, Color color, float alpha, bool depthRead, bool smooth = false, bool cull = true, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawSphere>(MyRenderMessageEnum.DebugDrawSphere);

            message.Position = position;
            message.Radius = radius;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Cull = cull;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static MyDebugDrawBatchAABB DebugDrawBatchAABB(MatrixD worldMatrix, Color color, bool depthRead, bool shaded)
        {
            return new MyDebugDrawBatchAABB(PrepareDebugDrawTriangles(), ref worldMatrix, ref color, depthRead, shaded);
        }

        public static void DebugDrawAABB(BoundingBoxD aabb, Color color, float alpha = 1.0f, float scale = 1.0f, bool depthRead = true, bool shaded = false, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawAABB>(MyRenderMessageEnum.DebugDrawAABB);

            message.AABB = aabb;
            message.Color = color;
            message.Alpha = alpha;
            message.Scale = scale;
            message.DepthRead = depthRead;
            message.Shaded = shaded;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawAxis(MatrixD matrix, float axisLength, bool depthRead, bool skipScale = false, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawAxis>(MyRenderMessageEnum.DebugDrawAxis);

            message.Matrix = matrix;
            message.AxisLength = axisLength;
            message.DepthRead = depthRead;
            message.SkipScale = skipScale;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }


        public static void DebugDrawOBB(MyOrientedBoundingBoxD obb, Color color, float alpha, bool depthRead, bool smooth, bool persistent = false)
        {
            MatrixD obbMatrix = MatrixD.CreateFromQuaternion(obb.Orientation);
            obbMatrix.Right *= obb.HalfExtent.X*2;
            obbMatrix.Up *= obb.HalfExtent.Y*2;
            obbMatrix.Forward *= obb.HalfExtent.Z*2;
            obbMatrix.Translation = obb.Center;

            DebugDrawOBB(obbMatrix, color, alpha, depthRead, smooth, persistent: persistent);
        }

        public static void DebugDraw6FaceConvex(Vector3D[] vertices, Color color, float alpha, bool depthRead, bool fill, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDraw6FaceConvex>(MyRenderMessageEnum.DebugDraw6FaceConvex);

            message.Vertices = (Vector3D[]) vertices.Clone();
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Fill = fill;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawCone(
            Vector3D translation,
            Vector3D directionVec,
            Vector3D baseVec,
            Color color,
            bool depthRead,
            bool persistent = false
            )
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCone>(MyRenderMessageEnum.DebugDrawCone);

            message.Translation = translation;
            message.DirectionVector = directionVec;
            message.BaseVector = baseVec;
            message.DepthRead = depthRead;
            message.Color = color;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }


        public static void DebugDrawOBB(MatrixD matrix, Color color, float alpha, bool depthRead, bool smooth, bool cull = true, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawOBB>(MyRenderMessageEnum.DebugDrawOBB);

            message.Matrix = matrix;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Cull = cull;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawCylinder(MatrixD worldMatrix, Vector3D vertexA, Vector3D vertexB, float radius, Color color, float alpha, bool depthRead, bool smooth, bool persistent = false)
        {
            Vector3 offset = (vertexB - vertexA);
            float height = offset.Length();
            float diameter = 2f*radius;

            Matrix m = Matrix.Identity;

            m.Up = offset/height;
            m.Right = Vector3.CalculatePerpendicularVector(m.Up);
            m.Forward = Vector3.Cross(m.Up, m.Right);

            m = Matrix.CreateScale(diameter, height, diameter)*m;
            m.Translation = (vertexA + vertexB)*0.5f;
            m = m*worldMatrix;

            DebugDrawCylinder(m, color, alpha, depthRead, smooth, persistent);
        }

        public static void DebugDrawCylinder(Vector3D position, Quaternion orientation, float radius, float height, Color color, float alpha, bool depthRead, bool smooth, bool persistent = false)
        {
            MatrixD m = MatrixD.CreateFromQuaternion(orientation);
            m.Right *= 2f*radius;
            m.Forward *= 2f*radius;
            m.Up *= height;
            m.Translation = position;

            DebugDrawCylinder(m, color, alpha, depthRead, smooth, persistent);
        }

        public static void DebugDrawCylinder(Vector3D position, QuaternionD orientation, double radius, double height, Color color, float alpha, bool depthRead, bool smooth, bool persistent = false)
        {
            MatrixD m = MatrixD.CreateFromQuaternion(orientation);
            m.Right *= 2 * radius;
            m.Forward *= 2 * radius;
            m.Up *= height;
            m.Translation = position;

            DebugDrawCylinder(m, color, alpha, depthRead, smooth, persistent);
        }

        public static void DebugDrawCylinder(MatrixD matrix, Color color, float alpha, bool depthRead, bool smooth, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCylinder>(MyRenderMessageEnum.DebugDrawCylinder);

            message.Matrix = matrix;
            message.Color = color;
            message.Alpha = alpha;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawTriangle(Vector3D vertex0, Vector3D vertex1, Vector3D vertex2, Color color, bool smooth, bool depthRead, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawTriangle>(MyRenderMessageEnum.DebugDrawTriangle);

            message.Vertex0 = vertex0;
            message.Vertex1 = vertex1;
            message.Vertex2 = vertex2;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Smooth = smooth;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawPlane(Vector3D position, Vector3 normal, Color color, bool depthRead, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawPlane>(MyRenderMessageEnum.DebugDrawPlane);

            message.Position = position;
            message.Normal = normal;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static MyRenderMessageDebugDrawMesh PrepareDebugDrawMesh()
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawMesh>(MyRenderMessageEnum.DebugDrawMesh);

            message.Vertices.Clear();

            return message;
        }

        public static uint DebugDrawMesh(MyRenderMessageDebugDrawMesh message, MatrixD worldMatrix, Color color, bool depthRead, bool shaded)
        {
            message.ID = GetMessageId(ObjectType.DebugDrawMesh);

            message.WorldMatrix = worldMatrix;
            message.DepthRead = depthRead;
            message.Shaded = shaded;
            message.Color = color;

            EnqueueMessage(message);

            return message.ID;
        }

        public static void DebugDrawUpdateMesh(uint ID, MyRenderMessageDebugDrawMesh message, MatrixD worldMatrix, Color color, bool depthRead, bool shaded)
        {
            message.ID = ID;

            message.WorldMatrix = worldMatrix;
            message.DepthRead = depthRead;
            message.Shaded = shaded;
            message.Color = color;

            EnqueueMessage(message);
        }

        public static IDrawTrianglesMessage PrepareDebugDrawTriangles()
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawTriangles>(MyRenderMessageEnum.DebugDrawTriangles);

            message.Indices.Clear();
            message.Vertices.Clear();

            return message;
        }

        public static void DebugDrawTriangles(IDrawTrianglesMessage msgInterface, MatrixD worldMatrix, Color color, bool depthRead, bool shaded, bool persistent = false)
        {
            var message = (MyRenderMessageDebugDrawTriangles) msgInterface;

            message.Color = color;
            message.WorldMatrix = worldMatrix;
            message.DepthRead = depthRead;
            message.Shaded = shaded;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawCapsule(Vector3D p0, Vector3D p1, float radius, Color color, bool depthRead, bool shaded = false, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawCapsule>(MyRenderMessageEnum.DebugDrawCapsule);

            message.P0 = p0;
            message.P1 = p1;
            message.Radius = radius;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Shaded = shaded;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugDrawModel(string model, MatrixD worldMatrix, Color color, bool depthRead, bool persistent = false)
        {
            var message = MessagePool.Get<MyRenderMessageDebugDrawModel>(MyRenderMessageEnum.DebugDrawModel);

            message.Model = model;
            message.WorldMatrix = worldMatrix;
            message.Color = color;
            message.DepthRead = depthRead;
            message.Persistent = persistent;

            EnqueueMessage(message);
        }

        public static void DebugClearPersistentMessages()
        {
            var message = MessagePool.Get<MyRenderMessageDebugClearPersistentMessages>(MyRenderMessageEnum.DebugClearPersistentMessages);
            EnqueueMessage(message);
        }

        // Wait until current frame is drawn on screen. For debug purposes only, can be useful to force drawing on screen.
        [Conditional("DEBUG")]
        public static void DebugWaitForFrameFinish()
        {
            if (m_render != null && m_render.SharedData != null)
            {
                var waitHandle = new AutoResetEvent(false);
                var message =
                    MessagePool.Get<MyRenderMessageDebugWaitForPresent>(MyRenderMessageEnum.DebugWaitForPresent);
                message.WaitHandle = waitHandle;
                EnqueueMessage(message);
                Draw3DScene();
                m_render.SharedData.CommitBasicRenderMessages();
                // now wait until the frame is presented
                waitHandle.WaitOne();
            }
        }

        public static void DebugCrashRenderThread()
        {
            var message = MessagePool.Get<MyRenderMessageDebugCrashRenderThread>(MyRenderMessageEnum.DebugCrashRenderThread);
            EnqueueMessage(message);
        }

        #endregion

        /// <param name="timestep">Greter than zero: fixed timestep. Zero: timestep is computed</param>
        public static void SetFrameTimeStep(uint timestep = 0)
        {
            var message = MessagePool.Get<MyRenderMessageSetFrameTimeStep>(MyRenderMessageEnum.SetFrameTimeStep);
            message.TimeStep = timestep;
            EnqueueMessage(message);
        }

        public static void ResetRandomness(int? seed = null)
        {
            var message = MessagePool.Get<MyRenderMessageResetRandomness>(MyRenderMessageEnum.ResetRandomness);
            message.Seed = seed;
            EnqueueMessage(message);
        }

        public static void CollectGarbage()
        {
            var message = MessagePool.Get<MyRenderMessageCollectGarbage>(MyRenderMessageEnum.CollectGarbage);
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

        public static void SwitchRenderSettings(MyRenderSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageSwitchRenderSettings>(MyRenderMessageEnum.SwitchRenderSettings);
            message.Settings = settings;
            m_settingsDirty = false;
            EnqueueMessage(message);
        }

        public static void SwitchRenderSettings(MyRenderSettings1 settings)
        {
            Settings.User = settings;
            if (settings.GrassDensityFactor == 0.0f) 
                Settings.User.FoliageDetails = MyFoliageDetails.DISABLED;
            SwitchRenderSettings(Settings);
        }

        public static void SwitchPostprocessSettings(ref MyPostprocessSettings settings)
        {
            var message = MessagePool.Get<MyRenderMessageUpdatePostprocessSettings>(MyRenderMessageEnum.UpdatePostprocessSettings);
            message.Settings = settings;
            EnqueueMessage(message);
        }

        public static void SendClipmapsReady()
        {
            EnqueueOutputMessage(MessagePool.Get<MyRenderMessageClipmapsReady>(MyRenderMessageEnum.ClipmapsReady));
        }

        public static uint CreateDecal(int parentId, ref MyDecalTopoData data, MyDecalFlags flags, string sourceTarget, string material, int matIndex)
        {
            var message = MessagePool.Get<MyRenderMessageCreateScreenDecal>(MyRenderMessageEnum.CreateScreenDecal);
            message.ID = GetMessageId(ObjectType.ScreenDecal, false);
            message.ParentID = (uint) parentId;
            message.TopoData = data;
            message.SourceTarget = sourceTarget;
            message.Flags = flags;
            message.Material = material;
            message.MaterialIndex = matIndex;

            EnqueueMessage(message);

            return message.ID;
        }

        public static void UpdateDecals(List<MyDecalPositionUpdate> decals)
        {
            var message = MessagePool.Get<MyRenderMessageUpdateScreenDecal>(MyRenderMessageEnum.UpdateScreenDecal);
            message.Decals.AddRange(decals);

            EnqueueMessage(message);
        }

        public static void RemoveDecal(uint decalId)
        {
            var message = MessagePool.Get<MyRenderMessageRemoveDecal>(MyRenderMessageEnum.RemoveDecal);
            message.ID = decalId;

            EnqueueMessage(message);
        }

        public static void SetDecalGlobals(MyDecalGlobals globals)
        {
            var message = MessagePool.Get<MyRenderMessageSetDecalGlobals>(MyRenderMessageEnum.SetDecalGlobals);
            message.Globals = globals;

            EnqueueMessage(message);
        }

        public static void RegisterDecals(Dictionary<string, List<MyDecalMaterialDesc>> descriptions)
        {
            var message = MessagePool.Get<MyRenderMessageRegisterScreenDecalsMaterials>(MyRenderMessageEnum.RegisterDecalsMaterials);
            message.MaterialDescriptions = descriptions;

            EnqueueMessage(message);
        }

        public static void ClearDecals()
        {
            var message = MessagePool.Get<MyRenderMessageClearScreenDecals>(MyRenderMessageEnum.ClearDecals);
            EnqueueMessage(message);
        }

        public static void HandleFocusMessage(MyWindowFocusMessage msg)
        {
            m_render.HandleFocusMessage(msg);
        }

        public static void UpdateMouseCapture(bool capture)
        {
            var message = MessagePool.Get<MyRenderMessageSetMouseCapture>(MyRenderMessageEnum.SetMouseCapture);

            message.Capture = capture;

            EnqueueMessage(message);
        }

        public static void UpdateDebugOverrides()
        {
            var message = MessagePool.Get<MyRenderMessageUpdateDebugOverrides>(MyRenderMessageEnum.UpdateDebugOverrides);
            message.Overrides = DebugOverrides.Clone();
            EnqueueMessage(message);
        }
    }

    public enum MyWindowFocusMessage
    {
        Activate,
        SetFocus
    }

    public struct MyDebugDrawBatchAABB : IDisposable
    {
        readonly IDrawTrianglesMessage m_msg;
        readonly MatrixD m_worldMatrix;
        readonly Color m_color;
        readonly bool m_depthRead;
        readonly bool m_shaded;

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
            m_msg.AddIndex(baseVertex + 1);
            m_msg.AddIndex(baseVertex + 0);
            m_msg.AddIndex(baseVertex + 2);
            m_msg.AddIndex(baseVertex + 1);
            m_msg.AddIndex(baseVertex + 2);
            m_msg.AddIndex(baseVertex + 3);

            // top
            m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 5);
            m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 5);
            m_msg.AddIndex(baseVertex + 7);

            // front
            m_msg.AddIndex(baseVertex + 0);
            m_msg.AddIndex(baseVertex + 1);
            m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 1);
            m_msg.AddIndex(baseVertex + 5);

            // back
            m_msg.AddIndex(baseVertex + 3);
            m_msg.AddIndex(baseVertex + 2);
            m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 3);
            m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 7);

            // right
            m_msg.AddIndex(baseVertex + 1);
            m_msg.AddIndex(baseVertex + 3);
            m_msg.AddIndex(baseVertex + 5);
            m_msg.AddIndex(baseVertex + 5);
            m_msg.AddIndex(baseVertex + 3);
            m_msg.AddIndex(baseVertex + 7);

            // left
            m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 2);
            m_msg.AddIndex(baseVertex + 0);
            m_msg.AddIndex(baseVertex + 4);
            m_msg.AddIndex(baseVertex + 6);
            m_msg.AddIndex(baseVertex + 2);
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
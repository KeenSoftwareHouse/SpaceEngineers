using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenDebugStatistics : MyGuiScreenDebugBase
    {
        static StringBuilder m_frameDebugText = new StringBuilder(1024);
        static StringBuilder m_frameDebugTextRA = new StringBuilder(2048);
        static List<StringBuilder> m_texts = new List<StringBuilder>(32);
        static List<StringBuilder> m_rightAlignedtexts = new List<StringBuilder>(32);

        List<MyKeys> m_pressedKeys = new List<MyKeys>(10);

        public MyGuiScreenDebugStatistics()
            : base(new Vector2(0.5f, 0.5f), new Vector2(), null, true)
        {
            m_isTopMostScreen = true;
            m_drawEvenWithoutFocus = true;
            CanHaveFocus = false;
            m_canShareInput = false;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugStatistics";
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void AddToFrameDebugText(string s)
        {
            m_frameDebugText.AppendLine(s);
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void AddToFrameDebugText(StringBuilder s)
        {
            m_frameDebugText.AppendStringBuilder(s);
            m_frameDebugText.AppendLine();
        }

        //  Frame Debug Text Right Aligned - is cleared at the begining of Update and rendered at the end of Draw
        public void AddDebugTextRA(string s)
        {
            m_frameDebugTextRA.Append(s);
            m_frameDebugTextRA.AppendLine();
        }
        //  Frame Debug Text Right Aligned - is cleared at the begining of Update and rendered at the end of Draw
        public void AddDebugTextRA(StringBuilder s)
        {
            m_frameDebugTextRA.AppendStringBuilder(s);
            m_frameDebugTextRA.AppendLine();
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void ClearFrameDebugText()
        {
            m_frameDebugText.Clear();
            m_frameDebugTextRA.Clear();
            //    MyAudio.Static.WriteDebugInfo(m_frameDebugTextRA);
        }

        public Vector2 GetScreenLeftTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }

        public Vector2 GetScreenRightTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(fullscreenRectangle.Width - deltaPixels, deltaPixels));
        }

        static List<StringBuilder> m_statsStrings = new List<StringBuilder>();
        static int m_stringIndex = 0;

        public static StringBuilder StringBuilderCache
        {
            get
            {
                if (m_stringIndex >= m_statsStrings.Count)
                    m_statsStrings.Add(new StringBuilder(1024));

                StringBuilder sb = m_statsStrings[m_stringIndex++];
                return sb.Clear();
            }
        }

        public override bool Draw()
        {
            if (!base.Draw()) return false;

            float rowDistance = MyGuiConstants.DEBUG_STATISTICS_ROW_DISTANCE;
            float textScale = MyGuiConstants.DEBUG_STATISTICS_TEXT_SCALE;

            m_stringIndex = 0;
            m_texts.Clear();
            m_rightAlignedtexts.Clear();
           
            
            m_texts.Add(StringBuilderCache.GetFormatedFloat("FPS: ", MyFpsManager.GetFps()));
            m_texts.Add(new StringBuilder("Renderer: ").Append(MyRenderProxy.RendererInterfaceName()));

            m_texts.Add(MyScreenManager.GetGuiScreensForDebug());
            m_texts.Add(StringBuilderCache.GetFormatedBool("Paused: ", MySandboxGame.IsPaused));
            m_texts.Add(StringBuilderCache.GetFormatedDateTimeOffset("System Time: ", TimeUtil.LocalTime));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total GAME-PLAY Time: ", TimeSpan.FromMilliseconds(MySandboxGame.TotalGamePlayTimeInMilliseconds)));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Session Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.ElapsedPlayTime));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Foot Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnFoot));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Jetpack Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnJetpack));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Small Ship Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnSmallShip));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Big Ship Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnBigShip));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Time: ", TimeSpan.FromMilliseconds(MySandboxGame.TotalTimeInMilliseconds)));
            
            m_texts.Add(StringBuilderCache.GetFormatedLong("GC.GetTotalMemory: ", GC.GetTotalMemory(false), " bytes"));

            if (MyFakes.DETECT_LEAKS)
            {
                var o = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
                m_texts.Add(StringBuilderCache.GetFormatedInt("SharpDX Active object count: ", o.Count));
            }

            //TODO: I am unable to show this without allocations
            m_texts.Add(StringBuilderCache.GetFormatedLong("Environment.WorkingSet: ", WinApi.WorkingSet, " bytes"));

            // TODO: OP! Get available texture memory
            //m_texts.Add(StringBuilderCache.GetFormatedFloat("Available videomemory: ", MySandboxGame.Static.GraphicsDevice.AvailableTextureMemory / (1024.0f * 1024.0f), " MB"));
            m_texts.Add(StringBuilderCache.GetFormatedFloat("Allocated videomemory: ", 0, " MB"));

#if PHYSICS_SENSORS_PROFILING
            if (MyPhysics.physicsSystem != null)
            {
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - new allocated interactions: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetNewAllocatedInteractionsCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - interactions in use: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetInteractionsInUseCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - interactions in use MAX: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetInteractionsInUseCountMax()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - all sensors: ", MyPhysics.physicsSystem.GetSensorModule().SensorsCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - active sensors: ", MyPhysics.physicsSystem.GetSensorModule().ActiveSensors.Count));
            }
#endif

            m_texts.Add(StringBuilderCache.GetFormatedInt("Sound Instances Total 2D: ", MyAudio.Static.GetSoundInstancesTotal2D()));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Sound Instances Total 3D: ", MyAudio.Static.GetSoundInstancesTotal3D()));

            var tmp = StringBuilderCache;
            MyAudio.Static.WriteDebugInfo(tmp);
            m_texts.Add(tmp);
            for (int i = 0; i < 8; i++ )
                m_texts.Add(StringBuilderCache.Clear());

            m_texts.Add(StringBuilderCache.GetFormatedInt("Updating 3D sounds count: ", MyAudio.Static.GetUpdating3DSoundsCount()));
            m_texts.Add(StringBuilderCache.Clear());
            m_texts.Add(StringBuilderCache.GetFormatedInt("Textures 2D Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.Textures2DCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Textures 2D Size In Pixels: ", VRageRender.MyPerformanceCounter.PerAppLifetime.Textures2DSizeInPixels));
            m_texts.Add(StringBuilderCache.AppendFormatedDecimal("Textures 2D Size In Mb: ", (float)VRageRender.MyPerformanceCounter.PerAppLifetime.Textures2DSizeInMb, 3));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Dxt Compressed Textures Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.DxtCompressedTexturesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Non Dxt Compressed Textures Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.NonDxtCompressedTexturesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Non Mip Mapped Textures Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.NonMipMappedTexturesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Texture Cubes Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.TextureCubesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Texture Cubes Size In Pixels: ", VRageRender.MyPerformanceCounter.PerAppLifetime.TextureCubesSizeInPixels));
            m_texts.Add(StringBuilderCache.AppendFormatedDecimal("Texture Cubes Size In Mb: ", (float)VRageRender.MyPerformanceCounter.PerAppLifetime.TextureCubesSizeInMb, 3));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Non MyModels Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.ModelsCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("MyModels Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.MyModelsCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("MyModels Meshes Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.MyModelsMeshesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("MyModels Vertexes Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.MyModelsVertexesCount));
            m_texts.Add(StringBuilderCache.GetFormatedInt("MyModels Triangles Count: ", VRageRender.MyPerformanceCounter.PerAppLifetime.MyModelsTrianglesCount));
            m_texts.Add(StringBuilderCache.GetFormatedFloat("Size of Model Vertex Buffers in Mb: ", (float)VRageRender.MyPerformanceCounter.PerAppLifetime.ModelVertexBuffersSize / (1024 * 1024)));
            m_texts.Add(StringBuilderCache.GetFormatedFloat("Size of Model Index Buffers in Mb: ", (float)VRageRender.MyPerformanceCounter.PerAppLifetime.ModelIndexBuffersSize / (1024 * 1024)));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Size of Voxel Vertex Buffers in Mb: ", VRageRender.MyPerformanceCounter.PerAppLifetime.VoxelVertexBuffersSize / 1024 / 1024));
            m_texts.Add(StringBuilderCache.GetFormatedInt("Size of Voxel Index Buffers in Mb: ", VRageRender.MyPerformanceCounter.PerAppLifetime.VoxelIndexBuffersSize / 1024 / 1024));
            m_texts.Add(StringBuilderCache.GetFormatedFloat("Size of loaded Model files in Mb: ", (float)VRageRender.MyPerformanceCounter.PerAppLifetime.MyModelsFilesSize / (1024 * 1024)));
            m_texts.Add(StringBuilderCache.GetFormatedBool("Paused: ", MySandboxGame.IsPaused));
            if (MySector.MainCamera != null)
            {
                m_texts.Add(GetFormatedVector3(StringBuilderCache, "Camera pos: ", MySector.MainCamera.Position));
            }

            MyInput.Static.GetPressedKeys(m_pressedKeys);
            AddPressedKeys("Current keys              : ", m_pressedKeys);

            m_texts.Add(StringBuilderCache.Clear());
            m_texts.Add(m_frameDebugText);

            m_rightAlignedtexts.Add(m_frameDebugTextRA);
              
            Vector2 origin = GetScreenLeftTopPosition();
            Vector2 rightAlignedOrigin = GetScreenRightTopPosition();

            for (int i = 0; i < m_texts.Count; i++)
            {
                MyGuiManager.DrawString(MyFontEnum.White, m_texts[i], origin + new Vector2(0, i * rowDistance), textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }
            for (int i = 0; i < m_rightAlignedtexts.Count; i++)
            {
                MyGuiManager.DrawString(MyFontEnum.White, m_rightAlignedtexts[i], rightAlignedOrigin + new Vector2(-0.3f, i * rowDistance), textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }

            ClearFrameDebugText();
            AddPerformanceCountersToFrameDebugText();

            return true;
        }

        private static StringBuilder GetFormatedVector3(StringBuilder sb, string before, Vector3D value, string after = "")
        {
            sb.Clear();
            sb.Append(before);
            sb.Append("{");
            sb.ConcatFormat("{0: #,000} ", value.X);
            sb.ConcatFormat("{0: #,000} ", value.Y);
            sb.ConcatFormat("{0: #,000} ", value.Z);
            sb.Append("}");
            sb.Append(after);
            return sb;
        }

        private void AddPressedKeys(string groupName, List<MyKeys> keys)
        {
            var text = StringBuilderCache;
            text.Append(groupName);
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(MyInput.Static.GetKeyName((MyKeys)keys[i]));
            }
            m_texts.Add(text);
        }

        //  Show only draw/rendering statistics, because these counts are reseted in a Draw call
        internal void AddPerformanceCountersToFrameDebugText()
        {
            AddDebugTextRA("MyPerformanceCounter");
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   RenderCellsInFrustum_LOD0: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.RenderCellsInFrustum_LOD0));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   RenderCellsInFrustum_LOD1: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.RenderCellsInFrustum_LOD1));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   VoxelTrianglesInFrustum_LOD0: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.VoxelTrianglesInFrustum_LOD0));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   VoxelTrianglesInFrustum_LOD1: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.VoxelTrianglesInFrustum_LOD1));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   Entities rendered: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.EntitiesRendered));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   Entities occluded: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.EntitiesOccluded));
            AddDebugTextRA(StringBuilderCache.GetFormatedInt("   Drawcalls: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.TotalDrawCalls));
            //debugScreen.AddDebugTextRA(StringBuilderCache.GetFormatedInt("   Shadow entities occluded: ", ShadowEntitiesOccluded));
            //debugScreen.AddDebugTextRA(GetShadowText("  Shadow entities occluded:", ShadowEntitiesOccluded));

            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   Queries count: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.QueriesCount));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   ModelTrianglesInFrustum_LOD0: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.ModelTrianglesInFrustum_LOD0));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   ModelTrianglesInFrustum_LOD1: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.ModelTrianglesInFrustum_LOD1));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   DecalsForVoxelsInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.DecalsForVoxelsInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   DecalsForEntitiesInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.DecalsForEntitiesInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   DecalsForCockipGlassInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.DecalsForCockipGlassInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   BillboardsInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.BillboardsInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   BillboardsDrawCalls: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.BillboardsDrawCalls));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   BillboardsSorted: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.BillboardsSorted));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   OldParticlesInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.OldParticlesInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   ParticleEffects total: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.ParticleEffectsTotal));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   ParticleEffects drawn: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.ParticleEffectsDrawn));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   NewParticles count: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.NewParticlesCount));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   Lights count: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.LightsCount));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   RenderElementsInFrustum: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.RenderElementsInFrustum));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   RenderElementsIBChanges: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.RenderElementsIBChanges));
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   RenderElementsInShadows: ", VRageRender.MyPerformanceCounter.PerCameraDrawRead.RenderElementsInShadows));

            for (int i = 0; i < VRageRender.MyPerformanceCounter.PerCameraDrawRead.ShadowDrawCalls.Length - 1; i++)
            {
                AddDebugTextRA(GetShadowText("   ShadowDrawCalls", i, VRageRender.MyPerformanceCounter.PerCameraDrawRead.ShadowDrawCalls[i]));
            }
            AddDebugTextRA(MyGuiScreenDebugStatistics.StringBuilderCache.GetFormatedInt("   ShadowDrawCalls (other):", VRageRender.MyPerformanceCounter.PerCameraDrawRead.ShadowDrawCalls[VRageRender.MyPerformanceCounter.PerCameraDrawRead.ShadowDrawCalls.Length - 1]));

            AddDebugTextRA("Render states");
            for (int i = 0; i < VRageRender.MyPerformanceCounter.PerCameraDrawRead.MaterialChanges.Length; i++)
            {
                int lodNum;
                var lod = (MyLodTypeEnum)i;
                if (lod == MyLodTypeEnum.LOD0)
                    lodNum = 0;
                else if (lod == MyLodTypeEnum.LOD1)
                    lodNum = 1;
                else
                    continue;

                AddDebugTextRA(GetLodText("   MaterialChanges", lodNum, VRageRender.MyPerformanceCounter.PerCameraDrawRead.MaterialChanges[i]));
                AddDebugTextRA(GetLodText("   TechniqueChanges", lodNum, VRageRender.MyPerformanceCounter.PerCameraDrawRead.TechniqueChanges[i]));
                AddDebugTextRA(GetLodText("   VertexBufferChanges", lodNum, VRageRender.MyPerformanceCounter.PerCameraDrawRead.VertexBufferChanges[i]));
                AddDebugTextRA(GetLodText("   EntityChanges", lodNum, VRageRender.MyPerformanceCounter.PerCameraDrawRead.EntityChanges[i]));
            }
        }

        private StringBuilder GetShadowText(string text, int cascade, int value)
        {
            var sb = StringBuilderCache;
            sb.Clear();
            sb.ConcatFormat("{0} (c {1}): ", text, cascade);
            sb.Concat(value);
            return sb;
        }

        private StringBuilder GetLodText(string text, int lod, int value)
        {
            var sb = StringBuilderCache;
            sb.Clear();
            sb.ConcatFormat("{0}_LOD{1}: ", text, lod);
            sb.Concat(value);
            return sb;
        }
    }
}

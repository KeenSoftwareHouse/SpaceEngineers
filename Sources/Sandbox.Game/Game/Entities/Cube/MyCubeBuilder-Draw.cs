#region Using

using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using ModelId = System.Int32;
using Sandbox.Engine.Physics;
using Havok;
using Sandbox.Game.GameSystems.CoordinateSystem;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage.Game.Models;
using VRage.Game;
using VRage.Input;
using VRage.Profiler;
using VRageRender.Models;
using VRageRender.Utils;

#endregion

namespace Sandbox.Game.Entities
{
    public partial class MyCubeBuilder : MyBlockBuilderBase
    {
        /// <summary>
        /// Used for rescaling aabb in the Draw semi transparent method.
        /// </summary>
        private static float SEMI_TRANSPARENT_BOX_MODIFIER = 1.04f;

        private const float DEBUG_SCALE = 0.5f;

        public static void DrawSemiTransparentBox(MyCubeGrid grid, MySlimBlock block, Color color, bool onlyWireframe = false, string lineMaterial = null, Vector4? lineColor = null)
        {
            DrawSemiTransparentBox(block.Min, block.Max, grid, color, onlyWireframe: onlyWireframe, lineMaterial: lineMaterial, lineColor: lineColor);
        }

        public static void DrawSemiTransparentBox(Vector3I minPosition, Vector3I maxPosition, MyCubeGrid grid, Color color, bool onlyWireframe = false, string lineMaterial = null,
            Vector4? lineColor = null)
        {
            var gridSize = grid.GridSize;
            var min = (minPosition * gridSize) - new Vector3((gridSize / 2.0f) * SEMI_TRANSPARENT_BOX_MODIFIER);
            var max = (maxPosition * gridSize) + new Vector3((gridSize / 2.0f) * SEMI_TRANSPARENT_BOX_MODIFIER);
            BoundingBoxD boxr = new BoundingBoxD(min, max);
            MatrixD gridMatrix = grid.WorldMatrix;
            var lColor = Color.White;
            if (lineColor.HasValue)
            {
                lColor = lineColor.Value;
            }

            MySimpleObjectDraw.DrawTransparentBox(ref gridMatrix, ref boxr, ref lColor, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, null, lineMaterial, false);
            if (!onlyWireframe)
            {
                Color faceColor = new Color(color * 0.2f, 0.3f);
                MySimpleObjectDraw.DrawTransparentBox(ref gridMatrix, ref boxr, ref faceColor, MySimpleObjectRasterizer.Solid, 0, 0.04f, "Square", null, true);
            }
        }

        protected void ClearRenderData()
        {
            m_renderData.ClearInstanceData();
            m_renderData.UpdateRenderInstanceData();
            m_renderData.UpdateRenderEntitiesData(CurrentGrid != null ? CurrentGrid.WorldMatrix : MatrixD.Identity, UseTransparency);
        }

        public override void Draw()
        {
            ProfilerShort.Begin("base.Draw()");
            base.Draw();

            ProfilerShort.BeginNextBlock("DebugDraw");
            DebugDraw();

            if (BlockCreationIsActivated)
            {
                MyHud.Crosshair.Recenter();
            }

            if (!IsActivated || CurrentBlockDefinition == null)
            {
                this.ClearRenderData();
                ProfilerShort.End();
                return;
            }


            this.DrawGuiIndicators();

            if (!BuildInputValid)
            {
                this.ClearRenderData();
                ProfilerShort.End();
                return;
            }


            ProfilerShort.BeginNextBlock("DrawBuildingStepsCount");
            DrawBuildingStepsCount(m_gizmo.SpaceDefault.m_startBuild, m_gizmo.SpaceDefault.m_startRemove, m_gizmo.SpaceDefault.m_continueBuild, ref m_gizmo.SpaceDefault.m_localMatrixAdd);
            ProfilerShort.End();

            bool addPos = m_gizmo.SpaceDefault.m_startBuild.HasValue;
            bool removePos = false;

            float gridSize = 0;
            if (CurrentBlockDefinition != null)
                gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);

            if (DynamicMode)
            {
                PlaneD cameraPlane = new PlaneD(MySector.MainCamera.Position, MySector.MainCamera.UpVector);
                Vector3D projectedPoint = IntersectionStart;
                projectedPoint = cameraPlane.ProjectPoint(ref projectedPoint);
                Vector3D freePlacementIntersectionPoint = projectedPoint + IntersectionDistance * IntersectionDirection;
                
                if (m_hitInfo != null)
                    freePlacementIntersectionPoint = m_hitInfo.Value.Position;

                addPos = this.CaluclateDynamicModePos(freePlacementIntersectionPoint, IsDynamicOverride());
                MyCoordinateSystem.Static.Visible = false;
            }
            else
            {
                if (m_gizmo.SpaceDefault.m_startBuild == null && m_gizmo.SpaceDefault.m_startRemove == null)
                {
                    if (!FreezeGizmo)
                    {
                        if (CurrentGrid != null)
                        {
                            MyCoordinateSystem.Static.Visible = false;
                            ProfilerShort.Begin("MyCubeBuilder.Draw() - Calculate for grid");
                            addPos = GetAddAndRemovePositions(gridSize, PlacingSmallGridOnLargeStatic,
                                out m_gizmo.SpaceDefault.m_addPos, out m_gizmo.SpaceDefault.m_addPosSmallOnLarge,
                                out m_gizmo.SpaceDefault.m_addDir,
                                out m_gizmo.SpaceDefault.m_removePos, out m_gizmo.SpaceDefault.m_removeBlock,
                                out m_gizmo.SpaceDefault.m_blockIdInCompound,
                                m_gizmo.SpaceDefault.m_removeBlocksInMultiBlock);

                            if (addPos)
                            {
                                if (PlacingSmallGridOnLargeStatic)
                                    m_gizmo.SpaceDefault.m_localMatrixAdd.Translation = m_gizmo.SpaceDefault.m_addPosSmallOnLarge.Value;
                                else
                                    m_gizmo.SpaceDefault.m_localMatrixAdd.Translation = m_gizmo.SpaceDefault.m_addPos;

                                m_gizmo.SpaceDefault.m_worldMatrixAdd = m_gizmo.SpaceDefault.m_localMatrixAdd * CurrentGrid.WorldMatrix;

                                var normal = GetSingleMountPointNormal();
                                // Gizmo add dir can be zero in some cases
                                if (normal.HasValue && GridAndBlockValid && m_gizmo.SpaceDefault.m_addDir != Vector3I.Zero)
                                {
                                    m_gizmo.SetupLocalAddMatrix(m_gizmo.SpaceDefault, normal.Value);
                                }
                            }
                            ProfilerShort.End();
                        }
                        else
                        {
                            MyCoordinateSystem.Static.Visible = true;
                            ProfilerShort.Begin("MyCubeBuilder.Draw() - Calculate for voxel");
                            Vector3D localSnappedPos = m_lastLocalCoordSysData.LocalSnappedPos;
                            if (!CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter)
                                localSnappedPos -= new Vector3D(0.5 * gridSize, 0.5 * gridSize, -0.5 * gridSize);

                            Vector3I gridCoord = Vector3I.Round(localSnappedPos / gridSize);

                            m_gizmo.SpaceDefault.m_addPos = gridCoord;
                            m_gizmo.SpaceDefault.m_localMatrixAdd.Translation = m_lastLocalCoordSysData.LocalSnappedPos;
                            m_gizmo.SpaceDefault.m_worldMatrixAdd = m_lastLocalCoordSysData.Origin.TransformMatrix;

                            addPos = true;
                            ProfilerShort.End();
                        }

                    }

                    Debug.Assert(!m_gizmo.SpaceDefault.m_worldMatrixAdd.IsNan(), "Invalid gizmo matrix");

                    if (m_gizmo.SpaceDefault.m_removeBlock != null)
                        removePos = true;

                }
            }

            ProfilerShort.Begin("buildingDisabledByCockpit");
            bool buildingDisabledByCockpit = MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity is MyCockpit && !SpectatorIsBuilding;
            
            if (!buildingDisabledByCockpit)
            {
                if (IsInSymmetrySettingMode)
                {
                    m_gizmo.SpaceDefault.m_continueBuild = null;
                    addPos = false;
                    removePos = false;

                    if (m_gizmo.SpaceDefault.m_removeBlock != null)
                    {
                        var min = (m_gizmo.SpaceDefault.m_removeBlock.Min * CurrentGrid.GridSize);
                        var max = (m_gizmo.SpaceDefault.m_removeBlock.Max * CurrentGrid.GridSize);

                        Vector3 center = (min + max) * 0.5f;

                        Color color = DrawSymmetryPlane(m_symmetrySettingMode, CurrentGrid, center);

                        DrawSemiTransparentBox(CurrentGrid, m_gizmo.SpaceDefault.m_removeBlock, color.ToVector4());
                    }
                }

                if (CurrentGrid != null && (UseSymmetry || IsInSymmetrySettingMode))
                {
                    if (CurrentGrid.XSymmetryPlane != null)
                    {
                        Vector3 center = CurrentGrid.XSymmetryPlane.Value * CurrentGrid.GridSize;
                        DrawSymmetryPlane(CurrentGrid.XSymmetryOdd ? MySymmetrySettingModeEnum.XPlaneOdd : MySymmetrySettingModeEnum.XPlane, CurrentGrid, center);
                    }

                    if (CurrentGrid.YSymmetryPlane != null)
                    {
                        Vector3 center = CurrentGrid.YSymmetryPlane.Value * CurrentGrid.GridSize;
                        DrawSymmetryPlane(CurrentGrid.YSymmetryOdd ? MySymmetrySettingModeEnum.YPlaneOdd : MySymmetrySettingModeEnum.YPlane, CurrentGrid, center);
                    }

                    if (CurrentGrid.ZSymmetryPlane != null)
                    {
                        Vector3 center = CurrentGrid.ZSymmetryPlane.Value * CurrentGrid.GridSize;
                        DrawSymmetryPlane(CurrentGrid.ZSymmetryOdd ? MySymmetrySettingModeEnum.ZPlaneOdd : MySymmetrySettingModeEnum.ZPlane, CurrentGrid, center);
                    }
                }
            }

            ProfilerShort.BeginNextBlock("UpdateGizmos");
            UpdateGizmos(addPos, removePos, true);
            
            ProfilerShort.BeginNextBlock("UpdateRenderInstanceData");
            m_renderData.UpdateRenderInstanceData();

            ProfilerShort.BeginNextBlock("CurrentVoxelBase");
            if (CurrentGrid == null || (DynamicMode && CurrentGrid != null))
            {
                MatrixD drawMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                Vector3D rotatedModelOffset;
                Vector3D.TransformNormal(ref CurrentBlockDefinition.ModelOffset, ref drawMatrix, out rotatedModelOffset);
                drawMatrix.Translation = drawMatrix.Translation + rotatedModelOffset;

                m_renderData.UpdateRenderEntitiesData(drawMatrix, UseTransparency, CurrentBlockScale);
            }
            else
            {
                m_renderData.UpdateRenderEntitiesData(CurrentGrid.WorldMatrix, UseTransparency);
            }

            ProfilerShort.BeginNextBlock("UpdateBlockInfoHud");
            UpdateBlockInfoHud();
            ProfilerShort.End();

        }

        protected void DrawGuiIndicators()
        {
            if (MyHud.MinimalHud || MyHud.CutsceneHud) return;

            if (!MySandboxGame.Config.ShowBuildingSizeHint) return;

            if (MyGuiScreenGamePlay.ActiveGameplayScreen != null)
                return;

            //if (IsCubeSizeModesAvailable)
            //{
            //    StringBuilder sb = new StringBuilder();
            //    sb.AppendFormat(MyTexts.GetString(MySpaceTexts.CubeBuilder_CubeSizeModeChange), MyGuiSandbox.GetKeyName(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE));
            //    Vector2 coords2D = new Vector2(0.5f, 0.13f);
            //    MyGuiManager.DrawString(MyFontEnum.White, sb, coords2D, 1.0f, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            //    coords2D = new Vector2(0.52f, 0.2f);
            //    float alphaValue = CubeBuilderState.CubeSizeMode != MyCubeSize.Small ? 0.8f : 1.0f;
            //    Color premultipliedColor = new Color(alphaValue, alphaValue, alphaValue, alphaValue);
            //    MyRenderProxy.DrawSprite(MyGuiConstants.CB_SMALL_GRID_MODE, coords2D, Vector2.One, premultipliedColor, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, 0, Vector2.UnitX, 1.0f, null);

            //    coords2D = new Vector2(0.48f, 0.2f);
            //    alphaValue = CubeBuilderState.CubeSizeMode != MyCubeSize.Large ? 0.8f : 1.0f;
            //    premultipliedColor = new Color(alphaValue, alphaValue, alphaValue, alphaValue);
            //    MyRenderProxy.DrawSprite(MyGuiConstants.CB_LARGE_GRID_MODE, coords2D, Vector2.One, premultipliedColor, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, 0, Vector2.UnitX, 1.0f, null);
            //}

            //if (BuildInputValid)
            //{
            //    Vector2 screenPos = new Vector2(0.5f, 0.1f);
            //    string texture = DynamicMode ? MyGuiConstants.CB_FREE_MODE_ICON : MyGuiConstants.CB_LCS_GRID_ICON;

            //    MyRenderProxy.DrawSprite(texture, screenPos, Vector2.One, Color.White, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, 0, Vector2.UnitX, 1.0f, null);
            //}

        }

        /// <summary>
        /// Calculates final position of the block in through gizmo.
        /// </summary>
        /// <param name="defaultPos">Proposed position.</param>
        /// <returns>If True than success.</returns>
        protected virtual bool CaluclateDynamicModePos(Vector3D defaultPos, bool isDynamicOverride = false)
        {
            ProfilerShort.Begin("DynamicMode");

            m_gizmo.SpaceDefault.m_worldMatrixAdd.Translation = defaultPos;

            bool addPos = true;
            if (isDynamicOverride)
            {
                defaultPos = GetFreeSpacePlacementPosition(out addPos);
                m_gizmo.SpaceDefault.m_worldMatrixAdd.Translation = defaultPos;
            }

            ProfilerShort.End();
            return addPos;
        }

        protected void DrawBuildingStepsCount(Vector3I? startBuild, Vector3I? startRemove, Vector3I? continueBuild, ref Matrix localMatrixAdd )
        {
            var startPosition = startBuild ?? startRemove;
            if (startPosition != null && continueBuild != null)
            {
                Vector3I rotatedSize;
                Vector3I.TransformNormal(ref CurrentBlockDefinition.Size, ref localMatrixAdd, out rotatedSize);
                rotatedSize = Vector3I.Abs(rotatedSize);

                int stepCount;
                Vector3I stepDelta;
                Vector3I counter;

                ComputeSteps(startPosition.Value, continueBuild.Value, startBuild.HasValue ? rotatedSize : Vector3I.One, out stepDelta, out counter, out stepCount);
                m_cubeCountStringBuilder.Clear();
                m_cubeCountStringBuilder.Append("  ");
                m_cubeCountStringBuilder.AppendInt32(stepCount);

                MyGuiManager.DrawString(MyFontEnum.White, m_cubeCountStringBuilder, new Vector2(0.5f, 0.5f), 1.5f);
            }
        }

        void DebugDraw()
        {
            /*
            int totalVariants = CurrentVariants != null ? CurrentVariants.Count : 0;
            int currentVariant = m_variantIndex + 1;

            int totalDefinitions = MyDefinitionManager.Static.GetUniqueCubeSetsCount(CurrentGrid.GridSizeEnum);
            int currentDefinition = m_definitionIndex + 1;

            VRageRender.MyRenderProxy.DebugDrawText2D(
                new Vector2(0, MySandboxGame.ScreenViewport.Height - 40),
                MyDefinitionManager.Static.GetUniqueCubeBlockDefinition(CurrentGrid.GridSizeEnum, m_definitionIndex, m_variantIndex).DisplayName + " (" + currentDefinition + "/" + totalDefinitions + ")" + " Variant (" + currentVariant + "/" + totalVariants + ")",
                Color.White,
                1); */

            if (MyPerGameSettings.EnableAi && MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES != MyWEMDebugDrawMode.NONE)
            {
                var def = CurrentBlockDefinition;
                if (def != null && CurrentGrid != null)
                {
                    Vector3 translation = Vector3.Transform(m_gizmo.SpaceDefault.m_addPos * 2.5f, CurrentGrid.PositionComp.WorldMatrix);
                    Matrix drawMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                    drawMatrix.Translation = translation;
                    drawMatrix = Matrix.Rescale(drawMatrix, CurrentGrid.GridSize);
                    /*drawMatrix.Translation *= CurrentGrid.GridSize;*/
                    if (def.NavigationDefinition != null && def.NavigationDefinition.Mesh != null)
                        def.NavigationDefinition.Mesh.DebugDraw(ref drawMatrix);
                }
            }

            if (MyFakes.ENABLE_DEBUG_DRAW_TEXTURE_NAMES)
                DebugDrawModelTextures();

            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_NAMES)
                DebugDrawVertexNames();

            if (MyFakes.ENABLE_DEBUG_DRAW_GENERATING_BLOCK)
                DebugDrawGeneratingBlock();
        }

        private void DebugDrawGeneratingBlock()
        {
            LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * 200);
            VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersection = MyEntities.GetIntersectionWithLine(ref line, MySession.Static.LocalCharacter, null);

            if (intersection.HasValue && intersection.Value.Entity is MyCubeGrid)
            {
                MyCubeGrid grid = intersection.Value.Entity as MyCubeGrid;
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? t = null;
                MySlimBlock block = null;
                if (grid.GetIntersectionWithLine(ref line, out t, out block) && t.HasValue && block != null)
                {
                    if (block.BlockDefinition.IsGeneratedBlock)
                        DebugDrawGeneratingBlock(block);
                }
            }
        }

        private void DebugDrawGeneratingBlock(MySlimBlock generatedBlock)
        {
            var generatingBlock = generatedBlock.CubeGrid.GetGeneratingBlock(generatedBlock);
            if (generatingBlock != null)
            {
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0, 0), "Generated SubTypeId: " + generatedBlock.BlockDefinition.Id.SubtypeName + " " + generatedBlock.Min.ToString() + " " + generatedBlock.Orientation.ToString(), Color.Yellow, DEBUG_SCALE);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0, 14), "Generating SubTypeId: " + generatingBlock.BlockDefinition.Id.SubtypeName + " " + generatingBlock.Min.ToString() + " " + generatingBlock.Orientation.ToString(), Color.Yellow, DEBUG_SCALE);

                Vector4 blue = new Vector4(Color.Blue.ToVector3() * 0.8f, 1);
                MyCubeBuilder.DrawSemiTransparentBox(generatingBlock.CubeGrid, generatingBlock, Color.Blue, lineColor: blue);
            }
        }

        private void DebugDrawModelTextures() 
        {
            LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * 200);
            VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersection = MyEntities.GetIntersectionWithLine(ref line, MySession.Static.LocalCharacter, null);

            if (intersection.HasValue)
            {
                float yPos = 0;

                if (intersection.Value.Entity is MyCubeGrid)
                {
                    MyCubeGrid grid = intersection.Value.Entity as MyCubeGrid;
                    VRage.Game.Models.MyIntersectionResultLineTriangleEx? t = null;
                    MySlimBlock block = null;
                    if (grid.GetIntersectionWithLine(ref line, out t, out block) && t.HasValue && block != null) 
                    {
                        DebugDrawModelTextures(block.FatBlock, ref yPos);
                    }
                }
            }
        }

        private void DebugDrawVertexNames()
        {
            //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 0), "Voxel names searching", Color.Yellow, 0.5f);
            LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection*500);
            MyIntersectionResultLineTriangleEx? intersection =
                MyEntities.GetIntersectionWithLine(ref line, MySession.Static.LocalCharacter, null, false, true, true,
                    VRage.Game.Components.IntersectionFlags.ALL_TRIANGLES, 0, false);

            float yPos = 20;
            if (intersection.HasValue)
            {
                if (intersection.Value.Entity is MyVoxelBase)
                {
                    MyVoxelBase voxels = (MyVoxelBase) intersection.Value.Entity;
                    Vector3D point = intersection.Value.IntersectionPointInWorldSpace;
                    if (intersection.Value.Entity is MyPlanet)
                    {
                        MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Type: planet/moon", Color.Yellow,
                            DEBUG_SCALE); yPos += 10;
                        MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos),
                            "Terrain: " + voxels.GetMaterialAt(ref point), Color.Yellow, DEBUG_SCALE); yPos += 10;
                    }
                    else
                    {
                        MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Type: asteroid", Color.Yellow,
                            DEBUG_SCALE); yPos += 10;
                        MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos),
                            "Terrain: " + voxels.GetMaterialAt(ref point), Color.Yellow, DEBUG_SCALE); yPos += 10;
                    }
                    MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos),
                        "Object size: " + voxels.SizeInMetres, Color.Yellow, DEBUG_SCALE); yPos += 10;

                    //location
                    /*
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(20, 50), "Location:", Color.Yellow, 0.5f);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(30, 60), "x " + Math.Round(point.X, 3).ToString(), Color.Yellow, 0.5f);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(30, 70), "y " + Math.Round(point.Y, 3).ToString(), Color.Yellow, 0.5f);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(30, 80), "z " + Math.Round(point.Z, 3).ToString(), Color.Yellow, 0.5f);*/
                }
                else if (intersection.Value.Entity is MyCubeGrid)
                {
                    MyCubeGrid grid = (MyCubeGrid) intersection.Value.Entity;
                    MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Detected grid object", Color.Yellow, DEBUG_SCALE); yPos += 10;
                    MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), String.Format("Grid name: {0}", grid.DisplayName), Color.Yellow,
                        DEBUG_SCALE); yPos += 10;

                    MyIntersectionResultLineTriangleEx? t;
                    MySlimBlock block;
                    if (grid.GetIntersectionWithLine(ref line, out t, out block) && t.HasValue && block != null)
                    {
                        if (block.FatBlock != null)
                        {
                            DebugDrawModelTextures(block.FatBlock, ref yPos);
                        }
                        else
                        {
                            DebugDrawBareBlockInfo(block, ref yPos);
                        }
                    }

                }
                else
                {
                    MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Unknown object detected @ distance " + intersection.Value.Triangle.Distance + "m", Color.Yellow,
                        DEBUG_SCALE); yPos += 10;
                }
                MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Distance " + intersection.Value.Triangle.Distance + "m", Color.Yellow,
                    DEBUG_SCALE);
            }
            else
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(20, 20), "Nothing detected nearby", Color.Yellow, DEBUG_SCALE);
            }
        }

        private static void DebugDrawTexturesInfo(MyModel model, ref float yPos)
        {
            HashSet<string> textures = new HashSet<string>();
            foreach (MyMesh mesh in model.GetMeshList())
            {
                Debug.Assert(mesh.Material.Textures != null);
                if (mesh.Material.Textures == null) continue;
                foreach (string texture in mesh.Material.Textures.Values)
                    if (!string.IsNullOrWhiteSpace(texture)) textures.Add(texture);
            }

            foreach (string texture in textures.OrderBy(s => s, StringComparer.InvariantCultureIgnoreCase))
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), texture, Color.White, DEBUG_SCALE);
                yPos += 10;
            }
        }

        private static void DebugDrawBareBlockInfo(MySlimBlock block, ref float yPos)
        {
            yPos += 20;
            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos),
                String.Format("Display Name: {0}", block.BlockDefinition.DisplayNameText), Color.Yellow, DEBUG_SCALE); yPos += 10;
            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos),
                String.Format("Cube type: {0}", block.BlockDefinition.CubeDefinition.CubeTopology), Color.Yellow, DEBUG_SCALE); yPos += 10;
            foreach (string modelName in block.BlockDefinition.CubeDefinition.Model.Distinct().OrderBy(s => s, StringComparer.InvariantCultureIgnoreCase))
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), String.Format("Asset: {0}", modelName), Color.Yellow, DEBUG_SCALE); yPos += 10;
                MyModel model = MyModels.GetModel(modelName);
                DebugDrawTexturesInfo(model, ref yPos);
            }
        }

        private void DebugDrawModelTextures(MyCubeBlock block, ref float yPos)
        {
            MyModel model = null;
            if (block != null)
            {
                model = block.Model;
            }
            
            if (model == null) return;

            yPos += 20;

            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "SubTypeId: " + block.BlockDefinition.Id.SubtypeName, Color.Yellow, DEBUG_SCALE); yPos += 10;
            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Display name: " + block.BlockDefinition.DisplayNameText, Color.Yellow, DEBUG_SCALE); yPos += 10;
            if (block.SlimBlock.IsMultiBlockPart)
            {
                var multiblockInfo = block.CubeGrid.GetMultiBlockInfo(block.SlimBlock.MultiBlockId);
                if (multiblockInfo != null)
                {
                    MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Multiblock: " + multiblockInfo.MultiBlockDefinition.Id.SubtypeName + " (Id:"
                                                                                                + block.SlimBlock.MultiBlockId + ")", Color.Yellow, DEBUG_SCALE); yPos += 10;
                }
            }

            if (block.BlockDefinition.IsGeneratedBlock)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Generated block: " + block.BlockDefinition.GeneratedBlockType, Color.Yellow, DEBUG_SCALE); yPos += 10;
            }

            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "Asset: " + model.AssetName, Color.Yellow, DEBUG_SCALE); yPos += 10;

            MyRenderProxy.DebugDrawText2D(new Vector2(20, yPos), "BlockID: " + block.EntityId, Color.Yellow, DEBUG_SCALE); yPos += 10;

            // Enables to copy asset name to windows clipboard through * key in MyTomasInputComponent
            var lastIndex = model.AssetName.LastIndexOf("\\") + 1;
            if (lastIndex != -1 && lastIndex < model.AssetName.Length)
            {
                MyTomasInputComponent.ClipboardText = model.AssetName.Substring(lastIndex);
            }
            else
            {
                MyTomasInputComponent.ClipboardText = model.AssetName;
            }

            DebugDrawTexturesInfo(model, ref yPos);
        }

        Color DrawSymmetryPlane(MySymmetrySettingModeEnum plane, MyCubeGrid localGrid, Vector3 center)
        {
            var localGridBB = localGrid.PositionComp.LocalAABB;

            float sizeMultiplier = 1.0f;
            float sizeOffset = localGrid.GridSize;

            Vector3 l0 = Vector3.Zero;
            Vector3 l1 = Vector3.Zero;
            Vector3 l2 = Vector3.Zero;
            Vector3 l3 = Vector3.Zero;

            float alpha = 0.1f;
            Color color = Color.Gray;

            switch (plane)
            {
                case MySymmetrySettingModeEnum.XPlane:
                case MySymmetrySettingModeEnum.XPlaneOdd:
                    {
                        color = new Color(1.0f, 0, 0, alpha);

                        center.X -= localGridBB.Center.X + (plane == MySymmetrySettingModeEnum.XPlaneOdd ? localGrid.GridSize * 0.50025f : 0);
                        center.Y = 0;
                        center.Z = 0;

                        l0 = new Vector3(0, localGridBB.HalfExtents.Y * sizeMultiplier + sizeOffset, localGridBB.HalfExtents.Z * sizeMultiplier + sizeOffset) + localGridBB.Center + center;
                        l1 = new Vector3(0, -localGridBB.HalfExtents.Y * sizeMultiplier - sizeOffset, localGridBB.HalfExtents.Z * sizeMultiplier + sizeOffset) + localGridBB.Center + center;
                        l2 = new Vector3(0, localGridBB.HalfExtents.Y * sizeMultiplier + sizeOffset, -localGridBB.HalfExtents.Z * sizeMultiplier - sizeOffset) + localGridBB.Center + center;
                        l3 = new Vector3(0, -localGridBB.HalfExtents.Y * sizeMultiplier - sizeOffset, -localGridBB.HalfExtents.Z * sizeMultiplier - sizeOffset) + localGridBB.Center + center;
                    }
                    break;
                case MySymmetrySettingModeEnum.YPlane:
                case MySymmetrySettingModeEnum.YPlaneOdd:
                    {
                        color = new Color(0.0f, 1.0f, 0, alpha);

                        center.X = 0;
                        center.Y -= localGridBB.Center.Y + (plane == MySymmetrySettingModeEnum.YPlaneOdd ? localGrid.GridSize * 0.50025f : 0);
                        center.Z = 0;

                        l0 = new Vector3(localGridBB.HalfExtents.X * sizeMultiplier + sizeOffset, 0, localGridBB.HalfExtents.Z * sizeMultiplier + sizeOffset) + localGridBB.Center + center;
                        l1 = new Vector3(-localGridBB.HalfExtents.X * sizeMultiplier - sizeOffset, 0, localGridBB.HalfExtents.Z * sizeMultiplier + sizeOffset) + localGridBB.Center + center;
                        l2 = new Vector3(localGridBB.HalfExtents.X * sizeMultiplier + sizeOffset, 0, -localGridBB.HalfExtents.Z * sizeMultiplier - sizeOffset) + localGridBB.Center + center;
                        l3 = new Vector3(-localGridBB.HalfExtents.X * sizeMultiplier - sizeOffset, 0, -localGridBB.HalfExtents.Z * sizeMultiplier - sizeOffset) + localGridBB.Center + center;
                    }
                    break;
                case MySymmetrySettingModeEnum.ZPlane:
                case MySymmetrySettingModeEnum.ZPlaneOdd:
                    {
                        color = new Color(0.0f, 0, 1.0f, alpha);

                        center.X = 0;
                        center.Y = 0;
                        center.Z -= localGridBB.Center.Z - (plane == MySymmetrySettingModeEnum.ZPlaneOdd ? localGrid.GridSize * 0.50025f : 0);

                        l0 = new Vector3(localGridBB.HalfExtents.X * sizeMultiplier + sizeOffset, localGridBB.HalfExtents.Y * sizeMultiplier + sizeOffset, 0) + localGridBB.Center + center;
                        l1 = new Vector3(-localGridBB.HalfExtents.X * sizeMultiplier - sizeOffset, localGridBB.HalfExtents.Y * sizeMultiplier + sizeOffset, 0) + localGridBB.Center + center;
                        l2 = new Vector3(localGridBB.HalfExtents.X * sizeMultiplier + sizeOffset, -localGridBB.HalfExtents.Y * sizeMultiplier - sizeOffset, 0) + localGridBB.Center + center;
                        l3 = new Vector3(-localGridBB.HalfExtents.X * sizeMultiplier - sizeOffset, -localGridBB.HalfExtents.Y * sizeMultiplier - sizeOffset, 0) + localGridBB.Center + center;
                    }
                    break;
            }

            Vector3 p0 = Vector3.Transform(l0, (Matrix)CurrentGrid.WorldMatrix);
            Vector3 p1 = Vector3.Transform(l1, (Matrix)CurrentGrid.WorldMatrix);
            Vector3 p2 = Vector3.Transform(l2, (Matrix)CurrentGrid.WorldMatrix);
            Vector3 p3 = Vector3.Transform(l3, (Matrix)CurrentGrid.WorldMatrix);

            VRageRender.MyRenderProxy.DebugDrawTriangle(p0, p1, p2, color, true, true);
            VRageRender.MyRenderProxy.DebugDrawTriangle(p2, p1, p3, color, true, true);

            return color;
        }

        private static string[] m_mountPointSideNames = { "Front", "Back", "Left", "Right", "Top", "Bottom" };

        public static void DrawMountPoints(float cubeSize, MyCubeBlockDefinition def, ref MatrixD drawMatrix)
        {
            var mountPoints = def.GetBuildProgressModelMountPoints(1.0f);
            if (mountPoints == null)
                return;

            if (!MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AUTOGENERATE)
                DrawMountPoints(cubeSize, def, drawMatrix, mountPoints);
            else
            {   //Generate mount points from model collisions and draw them
                if (def.Model != null)
                {
                    int index = 0;
                    MyModel model = VRage.Game.Models.MyModels.GetModel(def.Model);

                    foreach (var shape in model.HavokCollisionShapes)
                    {
                        MyPhysicsDebugDraw.DrawCollisionShape(shape, drawMatrix, 0.2f, ref index);
                    }

                    var newMountPoints = AutogenerateMountpoints(model, cubeSize);

                    DrawMountPoints(cubeSize, def, drawMatrix, newMountPoints.ToArray());
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS_HELPERS)
                DrawMountPointsAxisHelpers(def, ref drawMatrix, cubeSize);

        }

        public static List<MyCubeBlockDefinition.MountPoint> AutogenerateMountpoints(MyModel model, float gridSize)
        {
            var shapes = model.HavokCollisionShapes;

            if (shapes == null)
            {
                if (model.HavokBreakableShapes == null)
                    return new List<MyCubeBlockDefinition.MountPoint>();
                shapes = new HkShape[] { model.HavokBreakableShapes[0].GetShape() };
            }

            return AutogenerateMountpoints(shapes, gridSize);
        }
        public static List<MyCubeBlockDefinition.MountPoint> AutogenerateMountpoints(HkShape[] shapes, float gridSize)
        {
            ProfilerShort.Begin("AutogenerateMountpoints");
            HkShapeCutterUtil cutter = new HkShapeCutterUtil();
            List<BoundingBox>[] aabbsPerDirection = new List<BoundingBox>[Base6Directions.EnumDirections.Length];

            var newMountPoints = new List<MyCubeBlockDefinition.MountPoint>();

            foreach (var directionEnum in Base6Directions.EnumDirections)
            {
                int dirEnum = (int)directionEnum;
                //int dirEnum = 2;
                Vector3 direction = Base6Directions.Directions[dirEnum];

                foreach (var shape in shapes)
                {
                    if (shape.ShapeType == HkShapeType.List)
                    {
                        var listShape = (HkListShape)shape;
                        var iterator = listShape.GetIterator();
                        while (iterator.IsValid)
                        {
                            HkShape childShape = iterator.CurrentValue;

                            if (childShape.ShapeType == HkShapeType.ConvexTransform)
                            {
                                HkConvexTransformShape transformShape = (HkConvexTransformShape)childShape;

                                FindMountPoint(cutter, transformShape.Base, direction, gridSize, newMountPoints);
                            }
                            else
                                if (childShape.ShapeType == HkShapeType.ConvexTranslate)
                                {
                                    HkConvexTranslateShape translateShape = (HkConvexTranslateShape)childShape;

                                    FindMountPoint(cutter, translateShape.Base, direction, gridSize, newMountPoints);
                                }
                                else
                                {
                                    FindMountPoint(cutter, childShape, direction, gridSize, newMountPoints);
                                }

                            iterator.Next();
                        }
                        break;
                    }
                    else
                        if (shape.ShapeType == HkShapeType.Mopp)
                        {
                            var compoundShape = (HkMoppBvTreeShape)shape;
                            for (int s = 0; s < compoundShape.ShapeCollection.ShapeCount; s++)
                            {
                                HkShape childShape = compoundShape.ShapeCollection.GetShape((uint)s, null);

                                if (childShape.ShapeType == HkShapeType.ConvexTranslate)
                                {
                                    HkConvexTranslateShape translateShape = (HkConvexTranslateShape)childShape;

                                    FindMountPoint(cutter, translateShape.Base, direction, gridSize, newMountPoints);
                                }
                            }
                            break;
                        }
                        else
                        {
                            FindMountPoint(cutter, shape, direction, gridSize, newMountPoints);
                        }
                }
            }
            ProfilerShort.End();
            return newMountPoints;
        }

        static bool FindMountPoint(HkShapeCutterUtil cutter, HkShape shape, Vector3 direction, float gridSize, List<Sandbox.Definitions.MyCubeBlockDefinition.MountPoint> mountPoints)
        {
            //VRageRender.MyRenderProxy.DebugDrawLine3D(drawMatrix.Translation, Vector3D.Transform(direction, drawMatrix), Color.Green, Color.Green, false);
            //float offset = (gridSize * 0.9f) / 2.0f;
            float offset = (gridSize * 0.75f) / 2.0f; //because fracture pieces can be bit inside the cube
            Plane plane = new Plane(-direction, offset);
            float minimumSize = 0.2f;

            Vector3 min, max;
            if (cutter.Cut(shape, new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D), out min, out max))
            {
                var aabb = new BoundingBox(min, max);
                aabb.InflateToMinimum(new Vector3(minimumSize));
                float centerOffset = gridSize * 0.5f;
                //    VRageRender.MyRenderProxy.DebugDrawOBB(boxC, Color.Red, 0.02f, true, false);

                MyCubeBlockDefinition.MountPoint mountPoint = new MyCubeBlockDefinition.MountPoint();
                mountPoint.Normal = new Vector3I(direction);
                mountPoint.Start = (aabb.Min + new Vector3(centerOffset)) / gridSize;
                mountPoint.End = (aabb.Max + new Vector3(centerOffset)) / gridSize;
				mountPoint.Enabled = true;
                //because it didnt work if shape wasnt realy near the edge
                var zExt = Vector3.Abs(direction) * mountPoint.Start;
                bool add = zExt.AbsMax() > 0.5f;
                mountPoint.Start -= zExt;
                mountPoint.Start -= direction * 0.04f;
                mountPoint.End -= Vector3.Abs(direction) * mountPoint.End;
                mountPoint.End += direction * 0.04f;
                if (add)
                {
                    mountPoint.Start += Vector3.Abs(direction);
                    mountPoint.End += Vector3.Abs(direction);
                }
                mountPoints.Add(mountPoint);

                return true;
            }

            return false;
        }

        public static void DrawMountPoints(float cubeSize, MyCubeBlockDefinition def, MatrixD drawMatrix, MyCubeBlockDefinition.MountPoint[] mountPoints)
        {
            Color color = Color.Yellow;
            Color defaultColor = Color.Blue;
            Vector3I centerGrid = def.Center;
            Vector3 centerOffset = def.Size * 0.5f;
            Matrix drawTransf = MatrixD.CreateTranslation((centerGrid - centerOffset) * cubeSize) * drawMatrix;

            // Visualize existing mount points as yellow cuboids
            for (int i = 0; i < mountPoints.Length; ++i)
            {
                if (mountPoints[i].Normal == Base6Directions.IntDirections[0] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS0)
                    continue;
                if (mountPoints[i].Normal == Base6Directions.IntDirections[1] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS1)
                    continue;
                if (mountPoints[i].Normal == Base6Directions.IntDirections[2] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS2)
                    continue;
                if (mountPoints[i].Normal == Base6Directions.IntDirections[3] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS3)
                    continue;
                if (mountPoints[i].Normal == Base6Directions.IntDirections[4] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS4)
                    continue;
                if (mountPoints[i].Normal == Base6Directions.IntDirections[5] && !MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS5)
                    continue;

                var start = mountPoints[i].Start - centerGrid;
                var end = mountPoints[i].End - centerGrid;
                var box = new BoundingBoxD(Vector3.Min(start, end) * cubeSize, Vector3.Max(start, end) * cubeSize);
                //MySimpleObjectDraw.DrawTransparentBox(ref drawTransf, ref box, ref color, MySimpleObjectRasterizer.SolidAndWireframe, 1, 0.1f);

                MyOrientedBoundingBoxD boxD = new MyOrientedBoundingBoxD(box, drawTransf);

                VRageRender.MyRenderProxy.DebugDrawOBB(boxD, mountPoints[i].Default ? defaultColor : color, 0.2f, true, false);
            }
        }

        private static void DrawMountPointsAxisHelpers(MyCubeBlockDefinition def, ref MatrixD drawMatrix, float cubeSize)
        {
            Vector3I centerGrid = def.Center;
            Vector3 centerOffset = def.Size * 0.5f;

            MatrixD drawTransf = MatrixD.CreateTranslation(centerGrid - centerOffset) * MatrixD.CreateScale(cubeSize) * drawMatrix;

            // Draw axis helpers for the six mount point walls
            for (int i = 0; i < 6; ++i)
            {
                Base6Directions.Direction dir = (Base6Directions.Direction)i;

                Vector3D position = Vector3D.Zero;
                position.Z = -0.2f;

                Vector3D normal = Vector3.Forward;
                Vector3D right = Vector3.Right;
                Vector3D up = Vector3.Up;

                position = def.MountPointLocalToBlockLocal(position, dir);
                position = Vector3D.Transform(position, drawTransf);

                normal = def.MountPointLocalNormalToBlockLocal(normal, dir);
                normal = Vector3D.TransformNormal(normal, drawTransf);

                up = def.MountPointLocalNormalToBlockLocal(up, dir);
                up = Vector3D.TransformNormal(up, drawTransf);

                right = def.MountPointLocalNormalToBlockLocal(right, dir);
                right = Vector3D.TransformNormal(right, drawTransf);

                MatrixD rightMat = MatrixD.CreateWorld(position + right * 0.25f, normal, right);
                MatrixD upMat = MatrixD.CreateWorld(position + up * 0.25f, normal, up);
                Vector4 rc = Color.Red.ToVector4();
                Vector4 uc = Color.Green.ToVector4();

                MyRenderProxy.DebugDrawSphere(position, 0.03f * cubeSize, Color.Red.ToVector3(), 1.0f, true);
                MySimpleObjectDraw.DrawTransparentCylinder(ref rightMat, 0.0f, 0.03f * cubeSize, 0.5f * cubeSize, ref rc, false, 16, 0.01f * cubeSize);
                MySimpleObjectDraw.DrawTransparentCylinder(ref upMat, 0.0f, 0.03f * cubeSize, 0.5f * cubeSize, ref uc, false, 16, 0.01f * cubeSize);
                MyRenderProxy.DebugDrawLine3D(position, position - normal * 0.2f, Color.Red, Color.Red, true);

                float textSizeX = 0.5f * cubeSize;
                float textSizeY = 0.5f * cubeSize;
                float textSizeDesc = 0.5f * cubeSize;
                if (MySector.MainCamera != null)
                {
                    float distX = (float)(position + right * 0.55f - MySector.MainCamera.Position).Length();
                    float distY = (float)(position + up * 0.55f - MySector.MainCamera.Position).Length();
                    float distDesc = (float)(position + normal * 0.1f - MySector.MainCamera.Position).Length();
                    textSizeX = textSizeX * 6 / distX;
                    textSizeY = textSizeY * 6 / distY;
                    textSizeDesc = textSizeDesc * 6 / distDesc;
                }

                MyRenderProxy.DebugDrawText3D(position + right * 0.55f, "X", Color.Red, textSizeX, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                MyRenderProxy.DebugDrawText3D(position + up * 0.55f, "Y", Color.Green, textSizeY, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                MyRenderProxy.DebugDrawText3D(position + normal * 0.1f, m_mountPointSideNames[i], Color.White, textSizeDesc, true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }

            // If close enough, draw a black grid spaced by tenths of a mount point unit
            float dist = (float)(drawTransf.Translation - MySector.MainCamera.Position).Length();
            BoundingBoxD bb = new BoundingBoxD(-def.Size * cubeSize * 0.5f, def.Size * cubeSize * 0.5f);
            dist -= (float)bb.Size.Max() * 0.866f; // sqrt(3) * 0.5 - half of the solid diagonal of a cube
            Color black = Color.Black;
            if (dist < cubeSize * 3.0f)
                MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref bb, ref black, ref black, MySimpleObjectRasterizer.Wireframe, def.Size * 10, 0.005f / (float)bb.Size.Max() * cubeSize, onlyFrontFaces: true);
        }

        protected static void DrawRemovingCubes(Vector3I? startRemove, Vector3I? continueBuild, MySlimBlock removeBlock)
        {
            if (startRemove == null || continueBuild == null || removeBlock == null)
                return;

            Color white = Color.White;

            Vector3I stepDelta;
            Vector3I counter;
            int stepCount;
            ComputeSteps(startRemove.Value, continueBuild.Value, Vector3I.One, out stepDelta, out counter, out stepCount);

            var matrix = removeBlock.CubeGrid.WorldMatrix;
            BoundingBoxD aabb = BoundingBoxD.CreateInvalid();
            aabb.Include((startRemove.Value * removeBlock.CubeGrid.GridSize));
            aabb.Include((continueBuild.Value * removeBlock.CubeGrid.GridSize));
            aabb.Min -= new Vector3(removeBlock.CubeGrid.GridSize / 2.0f + 0.02f);
            aabb.Max += new Vector3(removeBlock.CubeGrid.GridSize / 2.0f + 0.02f);

            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref aabb, ref white, ref white, MySimpleObjectRasterizer.Wireframe, counter, 0.04f, null, "GizmoDrawLineRed", true);
            Color faceColor = new Color(Color.Red * 0.2f, 0.3f);
            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref aabb, ref faceColor, MySimpleObjectRasterizer.Solid, 0, 0.04f, "Square", null, true);
        }

    }
}

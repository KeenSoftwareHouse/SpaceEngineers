#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;
using ModelId = System.Int32;
using Sandbox.Game.GUI;
using Sandbox.Engine.Physics;
using Havok;
using VRage.Game;
using VRage.Game.Models;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    #region Enums

    [Flags]
    public enum MySymmetrySettingModeEnum
    {
        Disabled = 0,
        NoPlane = 1,
        XPlane = 2,
        XPlaneOdd = 4,
        YPlane = 8,
        YPlaneOdd = 16,
        ZPlane = 32,
        ZPlaneOdd = 64
    }

    public enum MyGizmoSpaceEnum
    {
        Default = 0,
        SymmetryX = 1,
        SymmetryY = 2,
        SymmetryZ = 3,
        SymmetryXY = 4,
        SymmetryYZ = 5,
        SymmetryXZ = 6,
        SymmetryXYZ = 7,
    }

    #endregion

    public class MyCubeBuilderGizmo
    {
        public class MyGizmoSpaceProperties
        {
            public bool Enabled = false;

            public MyGizmoSpaceEnum SourceSpace;
            public MySymmetrySettingModeEnum SymmetryPlane;
            public Vector3I SymmetryPlanePos;
            public bool SymmetryIsOdd;

            public MatrixD m_worldMatrixAdd = Matrix.Identity;
            public Matrix m_localMatrixAdd = Matrix.Identity;
            public Vector3I m_addDir = Vector3I.Up;
            public Vector3I m_addPos;
            public Vector3I m_min;
            public Vector3I m_max;
            public Vector3I m_centerPos;
            public Vector3I m_removePos;
            public MySlimBlock m_removeBlock;
            public ushort? m_blockIdInCompound;
            public Vector3I? m_startBuild;
            public Vector3I? m_continueBuild;
            public Vector3I? m_startRemove;
            public List<Vector3I> m_positions = new List<Vector3I>();
            public List<Vector3> m_cubeNormals = new List<Vector3>();
            public List<Vector2> m_patternOffsets = new List<Vector2>();

            // Small cube in large grid prperties (in large grid uniform coordinates)
            public Vector3? m_addPosSmallOnLarge;
            public Vector3 m_minSmallOnLarge;
            public Vector3 m_maxSmallOnLarge;
            public Vector3 m_centerPosSmallOnLarge;
            public List<Vector3> m_positionsSmallOnLarge = new List<Vector3>();

            public List<string> m_cubeModels = new List<string>();
            public List<MatrixD> m_cubeMatrices = new List<MatrixD>();
            public List<string> m_cubeModelsTemp = new List<string>();
            public List<MatrixD> m_cubeMatricesTemp = new List<MatrixD>();
            public bool m_buildAllowed;
            public bool m_showGizmoCube;
            public Quaternion m_rotation;
            public Vector3I m_mirroringOffset;
            public MyCubeBlockDefinition m_blockDefinition;

            public bool m_dynamicBuildAllowed;

            public HashSet<Tuple<MySlimBlock, ushort?>> m_removeBlocksInMultiBlock = new HashSet<Tuple<MySlimBlock, ushort?>>();

            public MatrixD m_animationLastMatrix = MatrixD.Identity;
            public Vector3D m_animationLastPosition = Vector3D.Zero;
            public float m_animationProgress = 1;

            public Quaternion LocalOrientation
            {
                get { return Quaternion.CreateFromRotationMatrix(m_localMatrixAdd); }
            }

            public void Clear()
            {
                m_startBuild = null;
                m_startRemove = null;
                m_removeBlock = null;
                m_blockIdInCompound = null;
                m_positions.Clear();
                m_cubeNormals.Clear();
                m_patternOffsets.Clear();
                m_cubeModels.Clear();
                m_cubeMatrices.Clear();
                m_cubeModelsTemp.Clear();
                m_cubeMatricesTemp.Clear();
                m_mirroringOffset = Vector3I.Zero;
                m_addPosSmallOnLarge = null;
                m_positionsSmallOnLarge.Clear();
                m_dynamicBuildAllowed = false;
                m_removeBlocksInMultiBlock.Clear();
            }
        }

        private MyGizmoSpaceProperties[] m_spaces = new MyGizmoSpaceProperties[8]; //8 octants

        public MyGizmoSpaceProperties SpaceDefault { get { return m_spaces[(int)MyGizmoSpaceEnum.Default]; } }
        public MyGizmoSpaceProperties[] Spaces { get { return m_spaces; } }

        public MyRotationOptionsEnum RotationOptions;


        public MyCubeBuilderGizmo()
        {
            for (int i = 0; i < 8; i++)
            {
                m_spaces[i] = new MyGizmoSpaceProperties();
            }

            m_spaces[(int)MyGizmoSpaceEnum.Default].Enabled = true;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryX].SourceSpace = MyGizmoSpaceEnum.Default;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryX].SymmetryPlane = MySymmetrySettingModeEnum.NoPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryX].SourceSpace = MyGizmoSpaceEnum.Default;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryX].SymmetryPlane = MySymmetrySettingModeEnum.XPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryY].SourceSpace = MyGizmoSpaceEnum.Default;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryY].SymmetryPlane = MySymmetrySettingModeEnum.YPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryZ].SourceSpace = MyGizmoSpaceEnum.Default;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryZ].SymmetryPlane = MySymmetrySettingModeEnum.ZPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXY].SourceSpace = MyGizmoSpaceEnum.SymmetryX;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXY].SymmetryPlane = MySymmetrySettingModeEnum.YPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryYZ].SourceSpace = MyGizmoSpaceEnum.SymmetryY;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryYZ].SymmetryPlane = MySymmetrySettingModeEnum.ZPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXZ].SourceSpace = MyGizmoSpaceEnum.SymmetryX;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXZ].SymmetryPlane = MySymmetrySettingModeEnum.ZPlane;

            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXYZ].SourceSpace = MyGizmoSpaceEnum.SymmetryXZ;
            m_spaces[(int)MyGizmoSpaceEnum.SymmetryXYZ].SymmetryPlane = MySymmetrySettingModeEnum.YPlane;
        }


        public void Clear()
        {
            foreach (var space in m_spaces)
            {
                space.Clear();
            }
        }

        public void RotateAxis(ref MatrixD rotatedMatrix)
        {
            SpaceDefault.m_localMatrixAdd = rotatedMatrix;
            SpaceDefault.m_localMatrixAdd.Forward = Vector3I.Round(SpaceDefault.m_localMatrixAdd.Forward);
            SpaceDefault.m_localMatrixAdd.Up = Vector3I.Round(SpaceDefault.m_localMatrixAdd.Up);
            SpaceDefault.m_localMatrixAdd.Right = Vector3I.Round(SpaceDefault.m_localMatrixAdd.Right);

            Debug.Assert(!SpaceDefault.m_localMatrixAdd.IsNan(), "Invalid gizmo matrix");

        }

        public void SetupLocalAddMatrix(MyGizmoSpaceProperties gizmoSpace, Vector3I normal)
        {
            var norm = -normal; // Make it towards the cube

            // Rotation from identity to normal
            Matrix rotToNormal = Matrix.CreateWorld(Vector3.Zero, norm, Vector3I.Shift(norm));

            // Rotation from normal to identity
            Matrix rotFromNormal = Matrix.Invert(rotToNormal);
            var dir = Vector3I.Round((rotToNormal * gizmoSpace.m_localMatrixAdd).Up);

            // When incoming rotation is invalid for current direction (e.g. changing face)
            if (dir == gizmoSpace.m_addDir || dir == -gizmoSpace.m_addDir)
                dir = Vector3I.Shift(dir);

            // Rotation from identity to target (gizmo add direction)
            Matrix rotToTarget = Matrix.CreateWorld(Vector3.Zero, gizmoSpace.m_addDir, dir);
            Debug.Assert(!rotToTarget.IsNan(), "Invalid gizmo matrix");

            // First rotate from normal direction to identity, than to target
            gizmoSpace.m_localMatrixAdd = rotFromNormal * rotToTarget;
            Debug.Assert(!gizmoSpace.m_localMatrixAdd.IsNan(), "Invalid gizmo matrix");
        }


        #region Render gizmo

        public void UpdateGizmoCubeParts(MyGizmoSpaceProperties gizmoSpace, MyBlockBuilderRenderData renderData, ref MatrixD invGridWorldMatrix, MyCubeBlockDefinition definition = null)
        {
            RemoveGizmoCubeParts(gizmoSpace);
            AddGizmoCubeParts(gizmoSpace, renderData, ref invGridWorldMatrix, definition);
        }

        private void AddGizmoCubeParts(MyGizmoSpaceProperties gizmoSpace, MyBlockBuilderRenderData renderData, ref MatrixD invGridWorldMatrix, MyCubeBlockDefinition definition)
        {
            Vector3UByte[] bones = null;
            MyTileDefinition[] tiles = null;
            MatrixD invGridWorldMatrixOrientation = invGridWorldMatrix.GetOrientation();
            float gridSize = 1f;
            if (definition != null && definition.Skeleton != null)
            {
                tiles = MyCubeGridDefinitions.GetCubeTiles(definition);
                gridSize = MyDefinitionManager.Static.GetCubeSize(definition.CubeSize);
            }


            for (int faceIndex = 0; faceIndex < gizmoSpace.m_cubeModelsTemp.Count; faceIndex++)
            {
                string cubePartModel = gizmoSpace.m_cubeModelsTemp[faceIndex];

                gizmoSpace.m_cubeModels.Add(cubePartModel);
                gizmoSpace.m_cubeMatrices.Add(gizmoSpace.m_cubeMatricesTemp[faceIndex]);

                if (tiles != null)
                {
                    int tileIndex = faceIndex % tiles.Length;

                    var invertedTile = Matrix.Transpose(tiles[tileIndex].LocalMatrix);
                    var onlyOrientation = invertedTile * gizmoSpace.m_cubeMatricesTemp[faceIndex].GetOrientation();
                    var boneMatrix = onlyOrientation * invGridWorldMatrixOrientation;

                    bones = new Vector3UByte[9];
                    for (int i = 0; i < 9; i++)
                    {
                        bones[i] = new Vector3UByte(128, 128, 128);
                    }

                    var model = VRage.Game.Models.MyModels.GetModel(cubePartModel);

                    for (int index = 0; index < Math.Min(model.BoneMapping.Length, 9); index++)
                    {
                        var boneOffset = model.BoneMapping[index];
                        Vector3 centered = boneOffset - Vector3.One;

                        Vector3I transformedOffset = Vector3I.Round(Vector3.Transform(centered, tiles[tileIndex].LocalMatrix) + Vector3.One);

                        for (int skeletonIndex = 0; skeletonIndex < definition.Skeleton.Count; skeletonIndex++)
                        {
                            BoneInfo skeletonBone = definition.Skeleton[skeletonIndex];
                            if (skeletonBone.BonePosition == (SerializableVector3I)transformedOffset)
                            {
                                Vector3 bone = Vector3UByte.Denormalize(skeletonBone.BoneOffset, gridSize);
                                Vector3 transformedBone = Vector3.Transform(bone, boneMatrix);
                                bones[index] = Vector3UByte.Normalize(transformedBone, gridSize);
                                break;
                            }
                        }
                    }
                }

                renderData.AddInstance(MyModel.GetId(cubePartModel), gizmoSpace.m_cubeMatricesTemp[faceIndex], ref invGridWorldMatrix, bones: bones, gridSize: gridSize);
            }
        }

        public void RemoveGizmoCubeParts()
        {
            foreach (var gizmoSpace in m_spaces)
            {
                RemoveGizmoCubeParts(gizmoSpace);
            }
        }

        private void RemoveGizmoCubeParts(MyGizmoSpaceProperties gizmoSpace)
        {
            gizmoSpace.m_cubeMatrices.Clear();
            gizmoSpace.m_cubeModels.Clear();
        }

        public void AddFastBuildParts(MyGizmoSpaceProperties gizmoSpace, MyCubeBlockDefinition cubeBlockDefinition, MyCubeGrid grid)
        {
            if (cubeBlockDefinition != null && gizmoSpace.m_startBuild != null && gizmoSpace.m_continueBuild != null)
            {
                var start = Vector3I.Min(gizmoSpace.m_startBuild.Value, gizmoSpace.m_continueBuild.Value);
                var end = Vector3I.Max(gizmoSpace.m_startBuild.Value, gizmoSpace.m_continueBuild.Value);

                Vector3I temp = new Vector3I();

                int baseCount = gizmoSpace.m_cubeMatricesTemp.Count;

                for (temp.X = start.X; temp.X <= end.X; temp.X += cubeBlockDefinition.Size.X)
                    for (temp.Y = start.Y; temp.Y <= end.Y; temp.Y += cubeBlockDefinition.Size.Y)
                        for (temp.Z = start.Z; temp.Z <= end.Z; temp.Z += cubeBlockDefinition.Size.Z)
                        {
                            var offset = temp - gizmoSpace.m_startBuild.Value;
                            if (offset == Vector3.Zero)
                                continue;

                            Vector3D tempWorldPos = grid != null ? (Vector3D.Transform(temp * grid.GridSize, grid.WorldMatrix)) : ((Vector3D)temp * MyDefinitionManager.Static.GetCubeSize(cubeBlockDefinition.CubeSize));

                            for (int i = 0; i < baseCount; i++)
                            {
                                gizmoSpace.m_cubeModelsTemp.Add(gizmoSpace.m_cubeModelsTemp[i]);
                                MatrixD m = gizmoSpace.m_cubeMatricesTemp[i];
                                m.Translation = tempWorldPos;
                                gizmoSpace.m_cubeMatricesTemp.Add(m);
                            }
                        }
            }
        }

        #endregion

        #region GizmoTests

        public static bool DefaultGizmoCloseEnough(ref MatrixD invGridWorldMatrix, BoundingBoxD gizmoBox, float gridSize, float intersectionDistance)
        {
            //MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Intersection distance = " + intersectionDistance, Color.Red, 1.0f);

            var m = invGridWorldMatrix;

            MyCharacter character = MySession.Static.LocalCharacter;
            if (character == null)
                return false;

            // Character head for measuring distance to intesection.
            Vector3D originHead = character.GetHeadMatrix(true).Translation;
            // Camera position adn direction. Used for ray cast to cube block box.
            Vector3D originCamera = MySector.MainCamera.Position;
            Vector3 direction = MySector.MainCamera.ForwardVector;

            double cameraHeadDist = (originHead - MySector.MainCamera.Position).Length();

            Vector3 localHead = Vector3D.Transform(originHead, m);
            Vector3 localStart = Vector3D.Transform(originCamera, m);
            Vector3 localEnd = Vector3D.Transform(originCamera + direction * (intersectionDistance + (float)cameraHeadDist), m);
            LineD line = new LineD(localStart, localEnd);

            // AABB of added block
            float inflate = 0.025f * gridSize;
            gizmoBox.Inflate(inflate);

            //{
            //    Color blue = Color.Blue;
            //    MatrixD mtx = MatrixD.Invert(invGridWorldMatrix);
            //    MySimpleObjectDraw.DrawTransparentBox(ref mtx, ref gizmoBox, ref blue, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);



            //    MyRenderProxy.DebugDrawLine3D(originCamera, originCamera + direction * (intersectionDistance + (float)cameraHeadDist), Color.Red, Color.Red, false);
            //}

            double distance = double.MaxValue;
            if (gizmoBox.Intersects(ref line, out distance))
            {
                // Distance from the player's head to the gizmo box.
                double distanceToPlayer = gizmoBox.Distance(localHead);
                if (MySession.Static.ControlledEntity is MyShipController)
                {
                    if (MyCubeBuilder.Static.CubeBuilderState.CurrentBlockDefinition.CubeSize == MyCubeSize.Large)
                        return distanceToPlayer <= MyCubeBuilder.CubeBuilderDefinition.BuildingDistLargeSurvivalShip;
                    else
                        return distanceToPlayer <= MyCubeBuilder.CubeBuilderDefinition.BuildingDistSmallSurvivalShip;
                }
                else
                {
                    if (MyCubeBuilder.Static.CubeBuilderState.CurrentBlockDefinition.CubeSize == MyCubeSize.Large)
                        return distanceToPlayer <= MyCubeBuilder.CubeBuilderDefinition.BuildingDistLargeSurvivalCharacter;
                    else
                        return distanceToPlayer <= MyCubeBuilder.CubeBuilderDefinition.BuildingDistSmallSurvivalCharacter;
                }
            }
            return false;
        }

        private void GetGizmoPointTestVariables(ref MatrixD invGridWorldMatrix, float gridSize, out BoundingBoxD bb, out MatrixD m, MyGizmoSpaceEnum gizmo, float inflate = 0.0f, bool onVoxel = false, bool dynamicMode = false)
        {
            m = invGridWorldMatrix * MatrixD.CreateScale(1.0f / gridSize);
            var gizmoSpace = m_spaces[(int)gizmo];

            if (dynamicMode)
            {
                m = invGridWorldMatrix;
                bb = new BoundingBoxD(-gizmoSpace.m_blockDefinition.Size * gridSize * 0.5f, gizmoSpace.m_blockDefinition.Size * gridSize * 0.5f);
            }
            else if (onVoxel)
            {
                m = invGridWorldMatrix;
                Vector3D worldMin = MyCubeGrid.StaticGlobalGrid_UGToWorld(gizmoSpace.m_min, gridSize, MyCubeBuilder.CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter) - Vector3D.Half * gridSize;
                Vector3D worldMax = MyCubeGrid.StaticGlobalGrid_UGToWorld(gizmoSpace.m_max, gridSize, MyCubeBuilder.CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter) + Vector3D.Half * gridSize;
                bb = new BoundingBoxD(worldMin - new Vector3D(inflate * gridSize), worldMax + new Vector3D(inflate * gridSize));
            }
            else if (MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && gizmoSpace.m_addPosSmallOnLarge != null) 
            {
                float smallToLarge = MyDefinitionManager.Static.GetCubeSize(gizmoSpace.m_blockDefinition.CubeSize) / gridSize;
                Debug.Assert(smallToLarge < 0.5f);
                Vector3 localMin = gizmoSpace.m_minSmallOnLarge - new Vector3(0.5f * smallToLarge);
                Vector3 localMax = gizmoSpace.m_maxSmallOnLarge + new Vector3(0.5f * smallToLarge);
                bb = new BoundingBoxD(localMin - new Vector3(inflate), localMax + new Vector3(inflate));
            }
            else 
            {
                Vector3 localMin = gizmoSpace.m_min - new Vector3(0.5f);
                Vector3 localMax = gizmoSpace.m_max + new Vector3(0.5f);
                bb = new BoundingBoxD(localMin - new Vector3(inflate), localMax + new Vector3(inflate));
            }
        }

        public bool PointsAABBIntersectsGizmo(List<Vector3D> points, MyGizmoSpaceEnum gizmo, ref MatrixD invGridWorldMatrix, float gridSize, float inflate = 0.0f, bool onVoxel = false, bool dynamicMode = false)
        {
            MatrixD m = new MatrixD();
            BoundingBoxD gizmoBox = new BoundingBoxD();
            GetGizmoPointTestVariables(ref invGridWorldMatrix, gridSize, out gizmoBox, out m, gizmo, inflate: inflate, onVoxel: onVoxel, dynamicMode: dynamicMode);

            BoundingBoxD pointsBox = BoundingBoxD.CreateInvalid();
            foreach (var point in points)
            {
                Vector3D localPoint = Vector3D.Transform(point, m);

                if (gizmoBox.Contains(localPoint) == ContainmentType.Contains)
                    return true;

                pointsBox.Include(localPoint);
            }

            return pointsBox.Intersects(ref gizmoBox);
        }

        public bool PointInsideGizmo(Vector3D point, MyGizmoSpaceEnum gizmo, ref MatrixD invGridWorldMatrix, float gridSize, float inflate = 0.0f, bool onVoxel = false, bool dynamicMode = false)
        {
            MatrixD m = new MatrixD();
            BoundingBoxD gizmoBox = new BoundingBoxD();
            GetGizmoPointTestVariables(ref invGridWorldMatrix, gridSize, out gizmoBox, out m, gizmo, inflate: inflate, onVoxel: onVoxel, dynamicMode: dynamicMode);

            Vector3D localPoint = Vector3D.Transform(point, m);

            return gizmoBox.Contains(localPoint) == ContainmentType.Contains;
        }

        #endregion

        private void EnableGizmoSpace(MyGizmoSpaceEnum gizmoSpaceEnum, bool enable, Vector3I? planePos, bool isOdd, MyCubeBlockDefinition cubeBlockDefinition, MyCubeGrid cubeGrid)
        {
            var gizmoSpace = m_spaces[(int)gizmoSpaceEnum];
            gizmoSpace.Enabled = enable;

            if (enable)
            {
                if (planePos.HasValue)
                    gizmoSpace.SymmetryPlanePos = planePos.Value;
                gizmoSpace.SymmetryIsOdd = isOdd;
                gizmoSpace.m_buildAllowed = false;

                if (cubeBlockDefinition != null)
                {
                    Quaternion orientationQuat = gizmoSpace.LocalOrientation;
                    MyBlockOrientation blockOrientation = new MyBlockOrientation(ref orientationQuat);

                    Vector3I rotatedBlockSize;
                    MyCubeGridDefinitions.GetRotatedBlockSize(cubeBlockDefinition, ref gizmoSpace.m_localMatrixAdd, out rotatedBlockSize);

                    //integer local center of the cube
                    Vector3I center = cubeBlockDefinition.Center;

                    //integer rotated/world center of the cube
                    Vector3I rotatedCenter;
                    Vector3I.TransformNormal(ref center, ref gizmoSpace.m_localMatrixAdd, out rotatedCenter);

                    //offset to the cube to align exactly on intersected cube
                    Vector3I worldDir = new Vector3I(
                        Math.Sign(rotatedBlockSize.X) == Math.Sign(gizmoSpace.m_addDir.X) ? rotatedCenter.X : Math.Sign(gizmoSpace.m_addDir.X) * ((Math.Abs(rotatedBlockSize.X) - Math.Abs(rotatedCenter.X) - 1)),
                        Math.Sign(rotatedBlockSize.Y) == Math.Sign(gizmoSpace.m_addDir.Y) ? rotatedCenter.Y : Math.Sign(gizmoSpace.m_addDir.Y) * ((Math.Abs(rotatedBlockSize.Y) - Math.Abs(rotatedCenter.Y) - 1)),
                        Math.Sign(rotatedBlockSize.Z) == Math.Sign(gizmoSpace.m_addDir.Z) ? rotatedCenter.Z : Math.Sign(gizmoSpace.m_addDir.Z) * ((Math.Abs(rotatedBlockSize.Z) - Math.Abs(rotatedCenter.Z) - 1)));

                    gizmoSpace.m_positions.Clear();
                    gizmoSpace.m_positionsSmallOnLarge.Clear();

                    if (MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && gizmoSpace.m_addPosSmallOnLarge != null) 
                    {
                        float smallToLarge = MyDefinitionManager.Static.GetCubeSize(cubeBlockDefinition.CubeSize) / cubeGrid.GridSize;

                        gizmoSpace.m_minSmallOnLarge = Vector3.MaxValue;
                        gizmoSpace.m_maxSmallOnLarge = Vector3.MinValue;
                        gizmoSpace.m_centerPosSmallOnLarge = gizmoSpace.m_addPosSmallOnLarge.Value + smallToLarge * worldDir;
                        gizmoSpace.m_buildAllowed = true;

                        Vector3I temp = new Vector3I();

                        for (temp.X = 0; temp.X < cubeBlockDefinition.Size.X; temp.X++)
                            for (temp.Y = 0; temp.Y < cubeBlockDefinition.Size.Y; temp.Y++)
                                for (temp.Z = 0; temp.Z < cubeBlockDefinition.Size.Z; temp.Z++) {
                                    Vector3I rotatedTemp;
                                    Vector3I centeredTemp = temp - center;
                                    Vector3I.TransformNormal(ref centeredTemp, ref gizmoSpace.m_localMatrixAdd, out rotatedTemp);

                                    Vector3 tempIntPos = gizmoSpace.m_addPosSmallOnLarge.Value + smallToLarge * (rotatedTemp + worldDir);
                                    gizmoSpace.m_minSmallOnLarge = Vector3.Min(tempIntPos, gizmoSpace.m_minSmallOnLarge);
                                    gizmoSpace.m_maxSmallOnLarge = Vector3.Max(tempIntPos, gizmoSpace.m_maxSmallOnLarge);

                                    // Commented out - small block can be placed in occupied large block areas 
                                    //if (!cubeGrid.CanAddCube(Vector3I.Round(tempIntPos), blockOrientation, null))
                                    //    gizmoSpace.m_buildAllowed = false;

                                    gizmoSpace.m_positionsSmallOnLarge.Add(tempIntPos);
                                }

                    }
                    else 
                    {                       
                            gizmoSpace.m_min = Vector3I.MaxValue;
                            gizmoSpace.m_max = Vector3I.MinValue;
                            gizmoSpace.m_centerPos = gizmoSpace.m_addPos + worldDir;
                            gizmoSpace.m_buildAllowed = true;

                            Vector3I temp = new Vector3I();

                            for (temp.X = 0; temp.X < cubeBlockDefinition.Size.X; temp.X++)
                                for (temp.Y = 0; temp.Y < cubeBlockDefinition.Size.Y; temp.Y++)
                                    for (temp.Z = 0; temp.Z < cubeBlockDefinition.Size.Z; temp.Z++)
                                    {
                                        Vector3I rotatedTemp;
                                        Vector3I centeredTemp = temp - center;
                                        Vector3I.TransformNormal(ref centeredTemp, ref gizmoSpace.m_localMatrixAdd, out rotatedTemp);

                                        Vector3I tempIntPos = gizmoSpace.m_addPos + rotatedTemp + worldDir;
                                        gizmoSpace.m_min = Vector3I.Min(tempIntPos, gizmoSpace.m_min);
                                        gizmoSpace.m_max = Vector3I.Max(tempIntPos, gizmoSpace.m_max);

                                        if (cubeGrid != null)
                                        {
                                            if (cubeBlockDefinition.CubeSize == cubeGrid.GridSizeEnum)
                                            {
                                                if (!cubeGrid.CanAddCube(tempIntPos, blockOrientation, cubeBlockDefinition))
                                                    gizmoSpace.m_buildAllowed = false;
                                            }
                                        }

                                        gizmoSpace.m_positions.Add(tempIntPos);
                                    }
                    }
                }

                if (gizmoSpace.SymmetryPlane != MySymmetrySettingModeEnum.Disabled)
                    MirrorGizmoSpace(gizmoSpace, m_spaces[(int)gizmoSpace.SourceSpace], gizmoSpace.SymmetryPlane, planePos.Value, isOdd, cubeBlockDefinition, cubeGrid);
            }
        }

        public void EnableGizmoSpaces(MyCubeBlockDefinition cubeBlockDefinition, MyCubeGrid cubeGrid, bool useSymmetry)
        {
            EnableGizmoSpace(MyGizmoSpaceEnum.Default, true, null, false, cubeBlockDefinition, cubeGrid);
            if (cubeGrid != null)
            {
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryX, useSymmetry && cubeGrid.XSymmetryPlane.HasValue, cubeGrid.XSymmetryPlane, cubeGrid.XSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryY, useSymmetry && cubeGrid.YSymmetryPlane.HasValue, cubeGrid.YSymmetryPlane, cubeGrid.YSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryZ, useSymmetry && cubeGrid.ZSymmetryPlane.HasValue, cubeGrid.ZSymmetryPlane, cubeGrid.ZSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXY, useSymmetry && cubeGrid.XSymmetryPlane.HasValue && cubeGrid.YSymmetryPlane.HasValue, cubeGrid.YSymmetryPlane, cubeGrid.YSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryYZ, useSymmetry && cubeGrid.YSymmetryPlane.HasValue && cubeGrid.ZSymmetryPlane.HasValue, cubeGrid.ZSymmetryPlane, cubeGrid.ZSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXZ, useSymmetry && cubeGrid.XSymmetryPlane.HasValue && cubeGrid.ZSymmetryPlane.HasValue, cubeGrid.ZSymmetryPlane, cubeGrid.ZSymmetryOdd, cubeBlockDefinition, cubeGrid);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXYZ, useSymmetry && cubeGrid.XSymmetryPlane.HasValue && cubeGrid.YSymmetryPlane.HasValue && cubeGrid.ZSymmetryPlane.HasValue, cubeGrid.YSymmetryPlane, cubeGrid.YSymmetryOdd, cubeBlockDefinition, cubeGrid);
            }
            else
            {
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryX, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryY, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryZ, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXY, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryYZ, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXZ, false, null, false, cubeBlockDefinition, null);
                EnableGizmoSpace(MyGizmoSpaceEnum.SymmetryXYZ, false, null, false, cubeBlockDefinition, null);
            }
        }

        private static Vector3I MirrorBlockByPlane(MySymmetrySettingModeEnum mirror, Vector3I mirrorPosition, bool isOdd, Vector3I sourcePosition)
        {
            Vector3I mirroredPosition = sourcePosition;

            if (mirror == MySymmetrySettingModeEnum.XPlane)
            {
                mirroredPosition = new Vector3I(mirrorPosition.X - (sourcePosition.X - mirrorPosition.X), sourcePosition.Y, sourcePosition.Z);
                if (isOdd)
                    mirroredPosition.X -= 1;
            }

            if (mirror == MySymmetrySettingModeEnum.YPlane)
            {
                mirroredPosition = new Vector3I(sourcePosition.X, mirrorPosition.Y - (sourcePosition.Y - mirrorPosition.Y), sourcePosition.Z);
                if (isOdd)
                    mirroredPosition.Y -= 1;
            }

            if (mirror == MySymmetrySettingModeEnum.ZPlane)
            {
                mirroredPosition = new Vector3I(sourcePosition.X, sourcePosition.Y, mirrorPosition.Z - (sourcePosition.Z - mirrorPosition.Z));
                if (isOdd)
                    mirroredPosition.Z += 1;
            }

            return mirroredPosition;
        }

        private static Vector3I MirrorDirByPlane(MySymmetrySettingModeEnum mirror, Vector3I mirrorDir, bool isOdd, Vector3I sourceDir)
        {
            Vector3I mirroredDir = sourceDir;

            if (mirror == MySymmetrySettingModeEnum.XPlane)
            {
                mirroredDir = new Vector3I(-sourceDir.X, sourceDir.Y, sourceDir.Z);
            }

            if (mirror == MySymmetrySettingModeEnum.YPlane)
            {
                mirroredDir = new Vector3I(sourceDir.X, -sourceDir.Y, sourceDir.Z);
            }

            if (mirror == MySymmetrySettingModeEnum.ZPlane)
            {
                mirroredDir = new Vector3I(sourceDir.X, sourceDir.Y, -sourceDir.Z);
            }

            return mirroredDir;
        }


        private void MirrorGizmoSpace(MyGizmoSpaceProperties targetSpace, MyGizmoSpaceProperties sourceSpace, MySymmetrySettingModeEnum mirrorPlane, Vector3I mirrorPosition, bool isOdd,
            MyCubeBlockDefinition cubeBlockDefinition, MyCubeGrid cubeGrid)
        {
            targetSpace.m_addPos = MirrorBlockByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_addPos);

            targetSpace.m_localMatrixAdd.Translation = targetSpace.m_addPos;

            targetSpace.m_addDir = MirrorDirByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_addDir);

            targetSpace.m_removePos = MirrorBlockByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_removePos);
            targetSpace.m_removeBlock = cubeGrid.GetCubeBlock(targetSpace.m_removePos);

            if (sourceSpace.m_startBuild.HasValue)
                targetSpace.m_startBuild = MirrorBlockByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_startBuild.Value);
            else
                targetSpace.m_startBuild = null;

            if (sourceSpace.m_continueBuild.HasValue)
                targetSpace.m_continueBuild = MirrorBlockByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_continueBuild.Value);
            else
                targetSpace.m_continueBuild = null;

            if (sourceSpace.m_startRemove.HasValue)
                targetSpace.m_startRemove = MirrorBlockByPlane(mirrorPlane, mirrorPosition, isOdd, sourceSpace.m_startRemove.Value);
            else
                targetSpace.m_startRemove = null;

            //Find block axis ortogonal to mirror plane normal
            Vector3 mirrorNormal = Vector3.Zero;
            switch (mirrorPlane)
            {
                case MySymmetrySettingModeEnum.XPlane:
                    mirrorNormal = Vector3.Right;
                    break;
                case MySymmetrySettingModeEnum.YPlane:
                    mirrorNormal = Vector3.Up;
                    break;
                case MySymmetrySettingModeEnum.ZPlane:
                    mirrorNormal = Vector3.Forward;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            var blockMirrorAxis = MySymmetryAxisEnum.None;
            if (MyUtils.IsZero(Math.Abs(Vector3.Dot(sourceSpace.m_localMatrixAdd.Right, mirrorNormal)) - 1.0f))
            {
                blockMirrorAxis = MySymmetryAxisEnum.X;
            }
            else
                if (MyUtils.IsZero(Math.Abs(Vector3.Dot(sourceSpace.m_localMatrixAdd.Up, mirrorNormal)) - 1.0f))
                {
                    blockMirrorAxis = MySymmetryAxisEnum.Y;
                }
                else
                    if (MyUtils.IsZero(Math.Abs(Vector3.Dot(sourceSpace.m_localMatrixAdd.Forward, mirrorNormal)) - 1.0f))
                    {
                        blockMirrorAxis = MySymmetryAxisEnum.Z;
                    }

            var blockMirrorOption = MySymmetryAxisEnum.None;
            switch (blockMirrorAxis)
            {
                case MySymmetryAxisEnum.X:
                    blockMirrorOption = cubeBlockDefinition.SymmetryX;
                    break;
                case MySymmetryAxisEnum.Y:
                    blockMirrorOption = cubeBlockDefinition.SymmetryY;
                    break;
                case MySymmetryAxisEnum.Z:
                    blockMirrorOption = cubeBlockDefinition.SymmetryZ;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            switch (blockMirrorOption)
            {
                case MySymmetryAxisEnum.X:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.Y:
                case MySymmetryAxisEnum.YThenOffsetX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.Z:
                case MySymmetryAxisEnum.ZThenOffsetX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.HalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(-MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.HalfY:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(-MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.HalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(-MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.XHalfY:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.YHalfY:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.ZHalfY:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.XHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.YHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.ZHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.XHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.YHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.ZHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.XMinusHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.YMinusHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.ZMinusHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.XMinusHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.YMinusHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.ZMinusHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.Pi) * sourceSpace.m_localMatrixAdd;
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.PiOver2) * targetSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.MinusHalfX:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationX(MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.MinusHalfY:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationY(MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;
                case MySymmetryAxisEnum.MinusHalfZ:
                    targetSpace.m_localMatrixAdd = Matrix.CreateRotationZ(MathHelper.PiOver2) * sourceSpace.m_localMatrixAdd;
                    break;

                default:
                    targetSpace.m_localMatrixAdd = sourceSpace.m_localMatrixAdd;
                    break;
            }

            if (!string.IsNullOrEmpty(sourceSpace.m_blockDefinition.MirroringBlock))
            {
                targetSpace.m_blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(sourceSpace.m_blockDefinition.Id.TypeId, sourceSpace.m_blockDefinition.MirroringBlock));
            }
            else
                targetSpace.m_blockDefinition = sourceSpace.m_blockDefinition;

            // Correct mirroring of objects with center offset
            // if (blockMirrorOption == Common.ObjectBuilders.Definitions.MySymmetryAxisEnum.None)
            if (cubeBlockDefinition.SymmetryX == MySymmetryAxisEnum.None && cubeBlockDefinition.SymmetryY == MySymmetryAxisEnum.None && cubeBlockDefinition.SymmetryZ == MySymmetryAxisEnum.None)
            {
                Vector3 min = sourceSpace.m_min * cubeGrid.GridSize - new Vector3(cubeGrid.GridSize / 2);
                Vector3 max = sourceSpace.m_max * cubeGrid.GridSize + new Vector3(cubeGrid.GridSize / 2);

                BoundingBox box = new BoundingBox(min, max);

                //Mirroring algorithm
                // 1. Find vector from closest source box side to mirror (vector A)
                // 2. Find vector from source box pos to opposite side (vector B)
                // 3. Correct mirrored position is source box pos + A - B

                if (box.Size.X > 1 * cubeGrid.GridSize || box.Size.Y > 1 * cubeGrid.GridSize || box.Size.Z > 1 * cubeGrid.GridSize)
                {
                    Vector3 sourceCenterFloatLocal = sourceSpace.m_addPos * cubeGrid.GridSize;
                    Vector3 sourceCenterWorld = Vector3.Transform(sourceCenterFloatLocal, cubeGrid.WorldMatrix);
                    //VRageRender.MyRenderProxy.DebugDrawSphere(sourceCenterWorld, 0.18f, Vector3.One, 1, false, false);

                    Vector3I localToMirror = mirrorPosition - sourceSpace.m_addPos;
                    Vector3 floatLocalToMirror = localToMirror * cubeGrid.GridSize;
                    if (isOdd)
                    {
                        floatLocalToMirror.X -= cubeGrid.GridSize / 2;
                        floatLocalToMirror.Y -= cubeGrid.GridSize / 2;
                        floatLocalToMirror.Z += cubeGrid.GridSize / 2;
                    }

                    Vector3 fullFloatLocalToMirror = floatLocalToMirror;
                    Vector3 alignedFloatLocalToMirror = Vector3.Clamp(sourceCenterFloatLocal + floatLocalToMirror, box.Min, box.Max) - sourceCenterFloatLocal;
                    Vector3 alignedFloatLocalToBoxEnd = Vector3.Clamp(sourceCenterFloatLocal + floatLocalToMirror * 100, box.Min, box.Max) - sourceCenterFloatLocal;
                    Vector3 oppositeFromMirror = Vector3.Clamp(sourceCenterFloatLocal - floatLocalToMirror * 100, box.Min, box.Max) - sourceCenterFloatLocal;

                    if (mirrorPlane == MySymmetrySettingModeEnum.XPlane || mirrorPlane == MySymmetrySettingModeEnum.XPlaneOdd)
                    {
                        oppositeFromMirror.Y = 0;
                        oppositeFromMirror.Z = 0;
                        alignedFloatLocalToMirror.Y = 0;
                        alignedFloatLocalToMirror.Z = 0;
                        fullFloatLocalToMirror.Y = 0;
                        fullFloatLocalToMirror.Z = 0;
                        alignedFloatLocalToBoxEnd.Y = 0;
                        alignedFloatLocalToBoxEnd.Z = 0;
                    }
                    else
                        if (mirrorPlane == MySymmetrySettingModeEnum.YPlane || mirrorPlane == MySymmetrySettingModeEnum.YPlaneOdd)
                        {
                            oppositeFromMirror.X = 0;
                            oppositeFromMirror.Z = 0;
                            alignedFloatLocalToMirror.X = 0;
                            alignedFloatLocalToMirror.Z = 0;
                            fullFloatLocalToMirror.X = 0;
                            fullFloatLocalToMirror.Z = 0;
                            alignedFloatLocalToBoxEnd.X = 0;
                            alignedFloatLocalToBoxEnd.Z = 0;
                        }
                        else
                            if (mirrorPlane == MySymmetrySettingModeEnum.ZPlane || mirrorPlane == MySymmetrySettingModeEnum.ZPlaneOdd)
                            {
                                oppositeFromMirror.Y = 0;
                                oppositeFromMirror.X = 0;
                                alignedFloatLocalToMirror.Y = 0;
                                alignedFloatLocalToMirror.X = 0;
                                fullFloatLocalToMirror.Y = 0;
                                fullFloatLocalToMirror.X = 0;
                                alignedFloatLocalToBoxEnd.Y = 0;
                                alignedFloatLocalToBoxEnd.X = 0;
                            }

                    Vector3 sideLocalToMirror = fullFloatLocalToMirror - alignedFloatLocalToMirror;

                    Vector3 alignedWorldToMirror = Vector3.TransformNormal(alignedFloatLocalToMirror, cubeGrid.WorldMatrix);
                    Vector3 fullWorldToMirror = Vector3.TransformNormal(fullFloatLocalToMirror, cubeGrid.WorldMatrix);
                    Vector3 oppositeWorldToMirror = Vector3.TransformNormal(oppositeFromMirror, cubeGrid.WorldMatrix);
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(sourceCenterWorld, sourceCenterWorld + alignedWorldToMirror, Color.Red, Color.Red, false);
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(sourceCenterWorld + alignedWorldToMirror, sourceCenterWorld + fullWorldToMirror, Color.Yellow, Color.Yellow, false);
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(sourceCenterWorld, sourceCenterWorld + oppositeWorldToMirror, Color.Blue, Color.Blue, false);

                    bool isInsideMirror = false;
                    if (fullFloatLocalToMirror.LengthSquared() < alignedFloatLocalToBoxEnd.LengthSquared())
                    {
                        isInsideMirror = true;
                    }

                    Vector3 newOffsetFromMirror = sideLocalToMirror;
                    Vector3 newOffsetFromBox = -oppositeFromMirror;
                    Vector3 newOffsetFromMirrorWorld = Vector3.TransformNormal(newOffsetFromMirror, cubeGrid.WorldMatrix);
                    Vector3 newOffsetFromBoxWorld = Vector3.TransformNormal(newOffsetFromBox, cubeGrid.WorldMatrix);
                    Vector3 mirrorPositionWorld = sourceCenterWorld + fullWorldToMirror;
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(mirrorPositionWorld, mirrorPositionWorld + newOffsetFromMirrorWorld, Color.Yellow, Color.Yellow, false);
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(mirrorPositionWorld + newOffsetFromMirrorWorld, mirrorPositionWorld + newOffsetFromMirrorWorld + newOffsetFromBoxWorld, Color.Blue, Color.Blue, false);

                    Vector3 newLocalFromMirror = newOffsetFromMirror + newOffsetFromBox;
                    Vector3 newWorldFromMirror = Vector3.TransformNormal(newLocalFromMirror, cubeGrid.WorldMatrix);
                    //VRageRender.MyRenderProxy.DebugDrawLine3D(mirrorPositionWorld, mirrorPositionWorld + newWorldFromMirror, Color.Green, Color.Green, false);


                    Vector3 fromMirrorFloat = sourceSpace.m_addPos + (fullFloatLocalToMirror + newLocalFromMirror) / cubeGrid.GridSize;


                    if (!isInsideMirror)
                    {
                        Vector3 worldFromMirror = Vector3.TransformNormal(fromMirrorFloat, cubeGrid.WorldMatrix);

                        //VRageRender.MyRenderProxy.DebugDrawLine3D(sourceCenterWorld, sourceCenterWorld + worldFromMirror, Color.White, Color.Black, false);

                        Vector3 newPos = fromMirrorFloat;
                        //VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(targetSpace.m_addPos * cubeGrid.GridSize, cubeGrid.WorldMatrix), 0.1f, Color.Aqua, 1, false);
                        targetSpace.m_mirroringOffset = new Vector3I(newPos) - targetSpace.m_addPos;
                        targetSpace.m_addPos += targetSpace.m_mirroringOffset;
                        targetSpace.m_removePos += targetSpace.m_mirroringOffset;
                        targetSpace.m_removeBlock = cubeGrid.GetCubeBlock(targetSpace.m_removePos);
                        targetSpace.m_addDir = sourceSpace.m_addDir;


                        targetSpace.m_localMatrixAdd.Translation = targetSpace.m_addPos;
                        if (targetSpace.m_startBuild != null)
                            targetSpace.m_startBuild = targetSpace.m_startBuild + targetSpace.m_mirroringOffset;
                    }
                    else
                    {
                        targetSpace.m_mirroringOffset = Vector3I.Zero;
                        targetSpace.m_addPos = sourceSpace.m_addPos;
                        targetSpace.m_removePos = sourceSpace.m_removePos;
                        targetSpace.m_removeBlock = cubeGrid.GetCubeBlock(sourceSpace.m_removePos);
                    }
                }
            }

            Vector3I offset = Vector3I.Zero;

            if (blockMirrorOption == MySymmetryAxisEnum.ZThenOffsetX)
                offset = new Vector3I(targetSpace.m_localMatrixAdd.Left);
            if (blockMirrorOption == MySymmetryAxisEnum.YThenOffsetX)
                offset = new Vector3I(targetSpace.m_localMatrixAdd.Left);


            if ((blockMirrorOption == MySymmetryAxisEnum.ZThenOffsetX)
                ||
                (blockMirrorOption == MySymmetryAxisEnum.YThenOffsetX))
            {
                targetSpace.m_mirroringOffset = offset;
                targetSpace.m_addPos += targetSpace.m_mirroringOffset;
                targetSpace.m_removePos += targetSpace.m_mirroringOffset;
                targetSpace.m_removeBlock = cubeGrid.GetCubeBlock(targetSpace.m_removePos);
                targetSpace.m_localMatrixAdd.Translation += offset;
            }



            targetSpace.m_worldMatrixAdd = targetSpace.m_localMatrixAdd * cubeGrid.WorldMatrix;

            Debug.Assert(!targetSpace.m_worldMatrixAdd.IsNan(), "Invalid gizmo matrix");
        }


    }
}

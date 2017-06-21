#region Using

using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;
using VRage.Game;

#endregion Using

namespace Sandbox.Definitions
{
    #region Nested struct and classes

    public enum MyCubeEdgeType : byte
    {
        Vertical = 0,
        Vertical_Diagonal = 1,
        Horizontal = 2,
        Horizontal_Diagonal = 3,
        Hidden = 4,
    }

    public class CubeMaterialSet
    {
        public string[][,] Models;
    }

    public struct MyEdgeDefinition
    {
        public Vector3 Point0;
        public Vector3 Point1;
        public int Side0;
        public int Side1;
    }

    public struct MyTileDefinition
    {
        public Matrix LocalMatrix;
        public Vector3 Normal;
        public bool FullQuad;
        public bool IsEmpty; // True when has no real coverage (triangle slope or triangle face)
        public bool IsRounded; // Don't hide tile when next to a similar facing tile (round slope next to regular slope)
        public bool DontOffsetTexture; // Don't use texture offsetting for this face
        public Vector3 Up; // Up vector for triangle, from triangle center to right angle (for full and empty it's none)
    }

    public class MyEdgeOrientationInfo
    {
        public readonly Matrix Orientation;
        public readonly MyCubeEdgeType EdgeType;

        public MyEdgeOrientationInfo(Matrix localMatrix, MyCubeEdgeType edgeType)
        {
            Orientation = localMatrix;
            EdgeType = edgeType;
        }
    }

    public enum MyRotationOptionsEnum
    {
        None,
        Vertical,
        Horizontal,
        Both
    }

    #endregion Nested struct and classes

    [PreloadRequired]
    public static class MyCubeGridDefinitions
    {
        #region Enums

        public class TableEntry
        {
            public MyRotationOptionsEnum RotationOptions;
            public MyTileDefinition[] Tiles;
            public MyEdgeDefinition[] Edges;
        }

        #endregion Enums

        //public static readonly string[][] CubeEdgeModels = new string[][]
        //{
        //    new string[]
        //    {
        //        @"Models\Cubes\large\armor\EdgeStraight.mwm", // Vertical
        //        @"Models\Cubes\large\armor\EdgeDiagonal.mwm", // Vertical diagonal
        //        @"Models\Cubes\large\armor\EdgeStraight.mwm", // Horizontal
        //        @"Models\Cubes\large\armor\EdgeDiagonal.mwm", // Horizontal diagonal
        //    },
        //    new string[]
        //    {
        //        @"Models\Cubes\small\armor\EdgeStraight.mwm", // Vertical
        //        @"Models\Cubes\small\armor\EdgeDiagonal.mwm", // Vertical diagonal
        //        @"Models\Cubes\small\armor\EdgeStraight.mwm", // Horizontal
        //        @"Models\Cubes\small\armor\EdgeDiagonal.mwm", // Horizontal diagonal
        //    },
        //};

        //public static readonly string[][] CubeHeavyEdgeModels = new string[][]
        //{
        //    new string[]
        //    {
        //        @"Models\Cubes\large\armor\EdgeHeavyStraight.mwm", // Vertical
        //        @"Models\Cubes\large\armor\EdgeHeavyDiagonal.mwm", // Vertical diagonal
        //        @"Models\Cubes\large\armor\EdgeHeavyStraight.mwm", // Horizontal
        //        @"Models\Cubes\large\armor\EdgeHeavyDiagonal.mwm", // Horizontal diagonal
        //    },
        //    new string[]
        //    {
        //        @"Models\Cubes\small\armor\EdgeHeavyStraight.mwm", // Vertical
        //        @"Models\Cubes\small\armor\EdgeHeavyDiagonal.mwm", // Vertical diagonal
        //        @"Models\Cubes\small\armor\EdgeHeavyStraight.mwm", // Horizontal
        //        @"Models\Cubes\small\armor\EdgeHeavyDiagonal.mwm", // Horizontal diagonal
        //    },
        //};

        public static readonly Dictionary<Vector3I, MyEdgeOrientationInfo> EdgeOrientations = new Dictionary<Vector3I, MyEdgeOrientationInfo>(new Vector3INormalEqualityComparer())
        {
            { new Vector3I(0,0,1), new MyEdgeOrientationInfo(Matrix.Identity, MyCubeEdgeType.Horizontal) },
            { new Vector3I(0,0,-1), new MyEdgeOrientationInfo(Matrix.Identity, MyCubeEdgeType.Horizontal) },
            { new Vector3I(1,0,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(MathHelper.PiOver2), MyCubeEdgeType.Horizontal) },
            { new Vector3I(-1,0,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(MathHelper.PiOver2), MyCubeEdgeType.Horizontal) },

            { new Vector3I(0,1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationX(MathHelper.PiOver2), MyCubeEdgeType.Vertical) },
            { new Vector3I(0,-1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationX(MathHelper.PiOver2), MyCubeEdgeType.Vertical) },

            { new Vector3I(-1,0,-1), new MyEdgeOrientationInfo(Matrix.CreateRotationZ(MathHelper.PiOver2), MyCubeEdgeType.Horizontal_Diagonal) },
            { new Vector3I(1,0,1), new MyEdgeOrientationInfo(Matrix.CreateRotationZ(MathHelper.PiOver2), MyCubeEdgeType.Horizontal_Diagonal) },
            { new Vector3I(-1,0,1), new MyEdgeOrientationInfo(Matrix.CreateRotationZ(-MathHelper.PiOver2), MyCubeEdgeType.Horizontal_Diagonal) },
            { new Vector3I(1,0,-1), new MyEdgeOrientationInfo(Matrix.CreateRotationZ(-MathHelper.PiOver2), MyCubeEdgeType.Horizontal_Diagonal) },

            { new Vector3I(0,1,-1), new MyEdgeOrientationInfo(Matrix.Identity, MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(0,-1,1), new MyEdgeOrientationInfo(Matrix.Identity, MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(-1,-1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(-MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(0,-1,-1), new MyEdgeOrientationInfo(Matrix.CreateRotationX(MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(1,-1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(-1,1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(1,1,0), new MyEdgeOrientationInfo(Matrix.CreateRotationY(-MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
            { new Vector3I(0,1,1), new MyEdgeOrientationInfo(Matrix.CreateRotationX(MathHelper.PiOver2), MyCubeEdgeType.Vertical_Diagonal) },
        };

        private static TableEntry[] m_tileTable = new TableEntry[]
        {
            //box
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.None,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Right, Vector3.Up), Normal = Vector3.Up, FullQuad = true},  //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true}, //front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Backward), Normal = Vector3.Backward, FullQuad = true}, //back
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Left, Vector3.Down), Normal = Vector3.Down, FullQuad = true}, //down
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Backward, Vector3.Right), Normal = Vector3.Right, FullQuad = true }, //right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Left), Normal = Vector3.Left, FullQuad = true } //left
                },
                Edges = new MyEdgeDefinition[]
                {
                    //top square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,1,1), Side0 = 0, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 2},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 3, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 2},

                    //vertical edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1, 1), Side0 = 4, Side1 = 2},
                },
            },
            //slope
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(0, 1, 1)), IsEmpty = true}, //slope quad
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Left, Up = new Vector3(0,-1,-1)}, //triangle left
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0,-1,-1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.Pi), Normal = Vector3.Down, FullQuad = true }, //bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true},//front
                },
                Edges = new MyEdgeDefinition[]
                {
                    //edge
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 1, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},

                    //front edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 2, Side1 = 4},
                },
            },
            //corner
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(1, -1, 0)}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0, -1, -1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(-1,1,1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Down, Up = new Vector3(1, 0, -1)},//triangle bottom
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 2},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 1, Side1 = 3},

                    // edge
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},
                },
            },
            //invcorner
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(1, -1, -1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Right, Up = new Vector3(0,1,1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi) * Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(-1,1,0)}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Down, Up = new Vector3(-1,0,1)},  //triangle bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Up, FullQuad = true}, //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Left, FullQuad = true}, //left square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.PiOver2), Normal = Vector3.Backward, FullQuad = true }, //back square
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 2, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 0},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1,1), Side0 = 6, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},

                    //bottom triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 6, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(-1,-1,-1), Side0 = 5, Side1 = 3},

                    // back
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 6},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,1,-1), Side0 = 5, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 6},
                },
            },
            //standalone box
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Right), Normal = Vector3.Right, FullQuad = true }, //right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Right, Vector3.Up), Normal = Vector3.Up, FullQuad = true},  //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true}, //front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Left), Normal = Vector3.Left, FullQuad = true }, //left
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Left, Vector3.Down), Normal = Vector3.Down, FullQuad = true}, //down
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Backward), Normal = Vector3.Backward, FullQuad = true}, //back
                },
                Edges = new MyEdgeDefinition[]
                {
                    //top square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,1,1), Side0 = 0, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 2},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 3, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 2},

                    //vertical edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1, 1), Side0 = 4, Side1 = 2},
                },
            },
            //rounded box
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Right, Vector3.Up), Normal = Vector3.Up, FullQuad = true},  //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true}, //front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Backward), Normal = Vector3.Backward, FullQuad = true}, //back
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Left, Vector3.Down), Normal = Vector3.Down, FullQuad = true}, //down
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Backward, Vector3.Right), Normal = Vector3.Right, FullQuad = true }, //right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Left), Normal = Vector3.Left, FullQuad = true } //left
                },
                Edges = new MyEdgeDefinition[]
                {
                    //top square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,1,1), Side0 = 0, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 0, Side1 = 2},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 3, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 2},

                    //vertical edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1, 1), Side0 = 4, Side1 = 2},
                },
            },
            //round slope
            //same as slope, but with some edges removed
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(0, 1, 1)), IsEmpty = true}, //slope quad
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Left, Up = new Vector3(0,-1,-1), IsRounded = true}, //triangle left
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0,-1,-1), IsRounded = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.Pi), Normal = Vector3.Down, FullQuad = true }, //bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true},//front
                },
                Edges = new MyEdgeDefinition[]
                {
                    //slope
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 1, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},
                    
                    //front edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 2, Side1 = 4},
                },
            },
            //round corner
            //same as slope, but with some edges removed
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(1, -1, 0), IsRounded = true}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0, -1, -1), IsRounded = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(-1,1,1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Down, Up = new Vector3(1, 0, -1), IsRounded = true},//triangle bottom
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 3},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 1, Side1 = 3},
                },
            },
            //round inv corner
            //same as inv corner, but with some edges removed
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(1, -1, -1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Right, Up = new Vector3(0,1,1), IsRounded = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi) * Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(-1,1,0), IsRounded = true}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Down, Up = new Vector3(-1,0,1), IsRounded = true},  //triangle bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Up, FullQuad = true}, //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Left, FullQuad = true}, //left square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.PiOver2), Normal = Vector3.Backward, FullQuad = true }, //back square
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 2, Side1 = 4},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 3, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1,1), Side0 = 5, Side1 = 1},

                    //bottom triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 0},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(-1,-1,-1), Side0 = 4, Side1 = 0},

                    // back
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 4, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,1,-1), Side0 = 4, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 3, Side1 = 5},
                },
            },
            //rotated slope
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(0, 1, 1)), IsEmpty = true}, //slope quad
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Left, Up = new Vector3(0,-1,-1)}, //triangle left
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0,-1,-1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.Pi), Normal = Vector3.Down, FullQuad = true }, //bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true},//front
                },
                Edges = new MyEdgeDefinition[]
                {
                    //edge
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 1, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},

                    //front edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 2, Side1 = 4},
                },
            },
            //rotated corner
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(1, -1, 0)}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0, -1, -1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(-1,1,1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Down, Up = new Vector3(1, 0, -1)},//triangle bottom
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 2},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 1, Side1 = 3},

                    // edge
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},
                },
            },
            //Slope2Base (from box)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up), Normal = Vector3.Normalize(new Vector3(0, 2, 1)), IsEmpty = true},  //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, FullQuad = true}, //front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Backward), Normal = Vector3.Backward}, //back
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Left, Vector3.Down), Normal = Vector3.Down, FullQuad = true}, //down
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Backward, Vector3.Right), Normal = Vector3.Right, Up = new Vector3(0,-2,-1), DontOffsetTexture = true}, //right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Left), Normal = Vector3.Left, Up = new Vector3(0,-2,-1), DontOffsetTexture = true} //left
                },
                Edges = new MyEdgeDefinition[]
                {
                    //top square
                    new MyEdgeDefinition() { Point0 = new Vector3(-1,1,-1), Point1 = new Vector3(1,1,-1), Side0 = 0, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3(-1,1,-1), Point1 = new Vector3(-1,0,1), Side0 = 0, Side1 = 5},
                    //new MyEdgeDefinition() { Point0 = new Vector3(1,1,-1), Point1 = new Vector3(1,0,1), Side0 = 0, Side1 = 4},
                    //new MyEdgeDefinition() { Point0 = new Vector3(-1,0,1), Point1 = new Vector3(1,0,1), Side0 = 0, Side1 = 2},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 3, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 3, Side1 = 2},

                    //vertical edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 5},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 1, Side1 = 4},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,0,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 2},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,0,1), Point1 = new Vector3I(1,-1, 1), Side0 = 4, Side1 = 2},
                },
            },
            //Slope2Tip (from slope)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(0, 2, 1)), IsEmpty = true}, //slope quad
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Left, Up = new Vector3(0,-2,-1), IsEmpty = true}, //triangle left
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0,-2,-1), IsEmpty = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.Pi), Normal = Vector3.Down, FullQuad = true }, //bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward},//front
                },
                Edges = new MyEdgeDefinition[]
                {
                    //edge
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,0,-1), Point1 = new Vector3I(1,0,-1), Side0 = 0, Side1 = 4},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,0,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 0, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,0,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 2},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 1, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},

                    //front edges
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,0,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 4},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,0,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 2, Side1 = 4},
                },
            },
            //Corner2Base (from slope)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(2, 1, 1)), IsEmpty = true, DontOffsetTexture = true}, //slope quad
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Left, Up = new Vector3(0,-1,1), DontOffsetTexture = true}, //triangle left
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0,-1,1), IsEmpty = true, DontOffsetTexture = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.Pi), Normal = Vector3.Down, Up = new Vector3(-2,0,1), DontOffsetTexture = true}, //bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Down, Vector3.Forward), Normal = Vector3.Forward, Up = new Vector3(-2,1,0), DontOffsetTexture = true},//front
                },
                Edges = new MyEdgeDefinition[]
                {
                    //edge
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 0, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 2},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},

                    //bottom square
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 3, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(-1,-1,1), Side0 = 1, Side1 = 3},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},

                    //front edges
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 1, Side1 = 4},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 2, Side1 = 4},
                },
            },
            //Corner2Tip (from corner)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                //NOTE(AF): They are all emtpy because there is no other way (currently) to have detect correct faces based only on Normal and Up vectors
                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(1, -2, 0), IsEmpty = true}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Right, Up = new Vector3(0, -2, -1), IsEmpty = true}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(-1,2,1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Down, Up = new Vector3(1, 0, -1), IsEmpty = true},//triangle bottom
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,-1), Side0 = 0, Side1 = 3},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 2},

                    //right triangle
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 1, Side1 = 3},

                    // edge
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 2, Side1 = 3},
                },
            },
            //InvCorner2Base (from box)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(2, -2, -1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Right, Up = new Vector3(0,-1,2)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi) * Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(2,0,-1)}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Down, Up = new Vector3(1,0,2)},  //triangle bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Up, FullQuad = true}, //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Left, FullQuad = true}, //left square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.PiOver2), Normal = Vector3.Backward, FullQuad = true }, //back square
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 2, Side1 = 5},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 0},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 1},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1,1), Side0 = 6, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},

                    //bottom triangle
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 6, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(-1,-1,-1), Side0 = 5, Side1 = 3},

                    // back
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 6},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,1,-1), Side0 = 5, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 6},
                },
            },
            //InvCorner2Tip (from box)
            new TableEntry
            {
                RotationOptions = MyRotationOptionsEnum.Both,

                Tiles = new MyTileDefinition[]
                {
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Normalize(new Vector3(2, -2, -1)), IsEmpty = true}, //slope triangle
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Right, Up = new Vector3(0,1,1)}, //triangle right
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.Pi) * Matrix.CreateRotationY(-MathHelper.PiOver2), Normal = Vector3.Forward, Up = new Vector3(-2,1,0)}, //triangle front
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * Matrix.CreateRotationX(MathHelper.Pi), Normal = Vector3.Down, Up = new Vector3(2,0,-1)},  //triangle bottom
                    new MyTileDefinition() { LocalMatrix = Matrix.Identity, Normal = Vector3.Up, FullQuad = true}, //top square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2), Normal = Vector3.Left, FullQuad = true}, //left square
                    new MyTileDefinition() { LocalMatrix = Matrix.CreateRotationX(MathHelper.PiOver2), Normal = Vector3.Backward}, //back square
                },
                Edges = new MyEdgeDefinition[]
                {
                    //front triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,-1), Point1 = new Vector3I(-1,-1,-1), Side0 = 2, Side1 = 5},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,1,-1), Side0 = 2, Side1 = 0},

                    //right triangle
                    new MyEdgeDefinition() { Point0 = new Vector3I(1,1,-1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,1,1), Point1 = new Vector3I(1,-1,1), Side0 = 6, Side1 = 1},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(1,1,-1), Side0 = 0, Side1 = 1},

                    //bottom triangle
                    //new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,-1), Point1 = new Vector3I(1,-1,1), Side0 = 0, Side1 = 3},
                    //new MyEdgeDefinition() { Point0 = new Vector3I(1,-1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 6, Side1 = 3},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,-1,1), Point1 = new Vector3I(-1,-1,-1), Side0 = 5, Side1 = 3},

                    // back
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,-1,1), Side0 = 5, Side1 = 6},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(-1,1,-1), Side0 = 5, Side1 = 4},
                    new MyEdgeDefinition() { Point0 = new Vector3I(-1,1,1), Point1 = new Vector3I(1,1,1), Side0 = 4, Side1 = 6},
                },
            },
        };

        private static MatrixI[] m_allPossible90rotations;
        private static MatrixI[][] m_uniqueTopologyRotationTable;

        static MyCubeGridDefinitions()
        {
            InitTopologyUniqueRotationsMatrices();
        }

        public static MyTileDefinition[] GetCubeTiles(MyCubeBlockDefinition block)
        {
            if (block.CubeDefinition == null)
                return null;

            var tableEntry = m_tileTable[(int)block.CubeDefinition.CubeTopology];

            return tableEntry.Tiles;
        }

        public static TableEntry GetTopologyInfo(MyCubeTopology topology)
        {
            return m_tileTable[(int)topology];
        }

        public static MyRotationOptionsEnum GetCubeRotationOptions(MyCubeBlockDefinition block)
        {
            return (block.CubeDefinition != null)
                ? m_tileTable[(int)block.CubeDefinition.CubeTopology].RotationOptions
                : MyRotationOptionsEnum.Both;
        }

        public static void GetRotatedBlockSize(MyCubeBlockDefinition block, ref Matrix rotation, out Vector3I size)
        {
            Vector3I.TransformNormal(ref block.Size, ref rotation, out size);
        }

        private static void InitTopologyUniqueRotationsMatrices()
        {
            //http://www.euclideanspace.com/maths/algebra/matrix/transforms/examples/index.htm
            m_allPossible90rotations = new MatrixI[]
            {
                new MatrixI(Base6Directions.Direction.Forward,  Base6Directions.Direction.Up),
                new MatrixI(Base6Directions.Direction.Down,     Base6Directions.Direction.Forward),
                new MatrixI(Base6Directions.Direction.Backward, Base6Directions.Direction.Down),
                new MatrixI(Base6Directions.Direction.Up,       Base6Directions.Direction.Backward),

                new MatrixI(Base6Directions.Direction.Forward,  Base6Directions.Direction.Right),
                new MatrixI(Base6Directions.Direction.Down,     Base6Directions.Direction.Right),
                new MatrixI(Base6Directions.Direction.Backward, Base6Directions.Direction.Right),
                new MatrixI(Base6Directions.Direction.Up,       Base6Directions.Direction.Right),
                
                new MatrixI(Base6Directions.Direction.Forward,  Base6Directions.Direction.Down),
                new MatrixI(Base6Directions.Direction.Up,       Base6Directions.Direction.Forward),
                new MatrixI(Base6Directions.Direction.Backward, Base6Directions.Direction.Up),
                new MatrixI(Base6Directions.Direction.Down,     Base6Directions.Direction.Backward),
                
                new MatrixI(Base6Directions.Direction.Forward,  Base6Directions.Direction.Left),
                new MatrixI(Base6Directions.Direction.Up,       Base6Directions.Direction.Left),
                new MatrixI(Base6Directions.Direction.Backward, Base6Directions.Direction.Left),
                new MatrixI(Base6Directions.Direction.Down,     Base6Directions.Direction.Left),
                
                new MatrixI(Base6Directions.Direction.Left,     Base6Directions.Direction.Up),
                new MatrixI(Base6Directions.Direction.Left,     Base6Directions.Direction.Backward),
                new MatrixI(Base6Directions.Direction.Left,     Base6Directions.Direction.Down),
                new MatrixI(Base6Directions.Direction.Left,     Base6Directions.Direction.Forward),
                
                new MatrixI(Base6Directions.Direction.Right,    Base6Directions.Direction.Down),
                new MatrixI(Base6Directions.Direction.Right,    Base6Directions.Direction.Backward),
                new MatrixI(Base6Directions.Direction.Right,    Base6Directions.Direction.Up),
                new MatrixI(Base6Directions.Direction.Right,    Base6Directions.Direction.Forward),
            };

            m_uniqueTopologyRotationTable = new MatrixI[Enum.GetValues(typeof(MyCubeTopology)).Length][];

            m_uniqueTopologyRotationTable[(int)MyCubeTopology.Box] = null; //empty, always returns Identity (any rotation of cube leads to cube)

            FillRotationsForTopology(MyCubeTopology.Slope, 0);
            FillRotationsForTopology(MyCubeTopology.Corner, 2);
            FillRotationsForTopology(MyCubeTopology.InvCorner, 0);
            FillRotationsForTopology(MyCubeTopology.StandaloneBox, -1);
            FillRotationsForTopology(MyCubeTopology.RoundedSlope, -1);
            FillRotationsForTopology(MyCubeTopology.RoundSlope, 0);
            FillRotationsForTopology(MyCubeTopology.RoundCorner, 2);
            FillRotationsForTopology(MyCubeTopology.RoundInvCorner, -1);
            FillRotationsForTopology(MyCubeTopology.RotatedSlope, -1);
            FillRotationsForTopology(MyCubeTopology.RotatedCorner, -1);

            //Slopes
            FillRotationsForTopology(MyCubeTopology.Slope2Base, -1);
            FillRotationsForTopology(MyCubeTopology.Slope2Tip, -1);
            FillRotationsForTopology(MyCubeTopology.Corner2Base, -1);
            FillRotationsForTopology(MyCubeTopology.Corner2Tip, -1);
            FillRotationsForTopology(MyCubeTopology.InvCorner2Base, -1);
            FillRotationsForTopology(MyCubeTopology.InvCorner2Tip, -1);
        }

        /// <summary>
        /// Fills rotation table for topology. Any arbitrary 90deg. rotation can then be converted to one unique rotation
        /// </summary>
        /// <param name="topology"></param>
        /// <param name="male">Tile which normal is tested to find unique rotations. If -1, all rotations are allowed</param>
        private static void FillRotationsForTopology(MyCubeTopology topology, int mainTile)
        {
            Vector3[] normals = new Vector3[m_allPossible90rotations.Length];

            m_uniqueTopologyRotationTable[(int)topology] = new MatrixI[m_allPossible90rotations.Length];

            for (int i = 0; i < m_allPossible90rotations.Length; i++)
            {
                int normalFound = -1;
                if (mainTile != -1)
                {
                    Vector3 transformedNormal;
                    Vector3.TransformNormal(ref m_tileTable[(int)topology].Tiles[mainTile].Normal, ref m_allPossible90rotations[i], out transformedNormal);
                    normals[i] = transformedNormal;
                    for (int j = 0; j < i; j++)
                    {
                        if (Vector3.Dot(normals[j], transformedNormal) > 0.98f)
                        {
                            normalFound = j;
                            break;
                        }
                    }
                }

                if (normalFound != -1)
                {
                    m_uniqueTopologyRotationTable[(int)topology][i] = m_uniqueTopologyRotationTable[(int)topology][normalFound];
                }
                else
                {
                    m_uniqueTopologyRotationTable[(int)topology][i] = m_allPossible90rotations[i];
                }
            }
        }


        /// <summary>
        /// From 90degrees rotations combinations returns one unique topology orientation, which can differ
        /// from input, but the resulted shape of topology is same
        /// </summary>
        /// <param name="myCubeTopology">cube topology</param>
        /// <param name="rotation">input rotation</param>
        /// <returns></returns>
        public static MyBlockOrientation GetTopologyUniqueOrientation(MyCubeTopology myCubeTopology, MyBlockOrientation orientation)
        {
            if (m_uniqueTopologyRotationTable[(int)myCubeTopology] == null)
                return MyBlockOrientation.Identity;

            for (int i = 0; i < m_allPossible90rotations.Length; i++)
            {
                MatrixI m1 = m_allPossible90rotations[i];
                if (m1.Forward == orientation.Forward && m1.Up == orientation.Up)
                    return m_uniqueTopologyRotationTable[(int)myCubeTopology][i].GetBlockOrientation();
            }

            System.Diagnostics.Debug.Assert(false, "There is rotation which not belongs to one of 24 possible 90 degrees rotations");
            return MyBlockOrientation.Identity;
        }

        public static MatrixI[] AllPossible90rotations
        {
            get { return m_allPossible90rotations; }
        }
    }
}
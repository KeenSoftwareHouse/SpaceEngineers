using System;
using System.Diagnostics;

using VRage;
using VRage.Utils;
using VRageMath;
using SharpDX.Toolkit.Graphics;
using VRage.Profiler;

namespace Sandbox.Engine.Voxels
{
    #region Cubemap Base

    public interface IMyWrappedCubemapFace
    {
        void CopyRange(Vector2I start, Vector2I end, IMyWrappedCubemapFace other, Vector2I oStart, Vector2I oEnd);
        void FinishFace(string name);

        int Resolution { get; }
        int ResolutionMinusOne { get; }
    }

    public class MyWrappedCubemap<FaceFormat> where FaceFormat : IMyWrappedCubemapFace
    {
        protected FaceFormat[] m_faces;

        protected int m_resolution;

        public string Name;

        public FaceFormat Left
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.XPositive]; }
        }

        public FaceFormat Right
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.XNegative]; }
        }

        public FaceFormat Top
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.YPositive]; }
        }

        public FaceFormat Bottom
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.YNegative]; }
        }

        public FaceFormat Front
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.ZNegative]; }
        }

        public FaceFormat Back
        {
            get { return m_faces[(int)MyCubemapHelpers.Faces.ZPositive]; }
        }

        public int Resolution { get { return m_resolution; } }

        public FaceFormat this[int i]
        {
            get { return m_faces[i]; }
        }

        public MyWrappedCubemap()
        {
        }

        // assign values to the extra borders of the heightmap.
        public void PrepareSides()
        {
            int end = m_resolution - 1;

            ProfilerShort.Begin("Copy Range");
            Front.CopyRange(new Vector2I(0, -1), new Vector2I(end, -1), Top, new Vector2I(0, end), new Vector2I(end, end));
            Front.CopyRange(new Vector2I(0, m_resolution), new Vector2I(end, m_resolution), Bottom, new Vector2I(end, end), new Vector2I(0, end));
            Front.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Left, new Vector2I(end, 0), new Vector2I(end, end));
            Front.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Right, new Vector2I(0, 0), new Vector2I(0, end));

            Back.CopyRange(new Vector2I(end, -1), new Vector2I(0, -1), Top, new Vector2I(0, 0), new Vector2I(end, 0));
            Back.CopyRange(new Vector2I(end, m_resolution), new Vector2I(0, m_resolution), Bottom, new Vector2I(end, 0), new Vector2I(0, 0));
            Back.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Right, new Vector2I(end, 0), new Vector2I(end, end));
            Back.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Left, new Vector2I(0, 0), new Vector2I(0, end));

            Left.CopyRange(new Vector2I(end, -1), new Vector2I(0, -1), Top, new Vector2I(0, end), new Vector2I(0, 0));
            Left.CopyRange(new Vector2I(end, m_resolution), new Vector2I(0, m_resolution), Bottom, new Vector2I(end, end), new Vector2I(end, 0));
            Left.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Front, new Vector2I(0, 0), new Vector2I(0, end));
            Left.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Back, new Vector2I(end, 0), new Vector2I(end, end));

            Right.CopyRange(new Vector2I(end, -1), new Vector2I(0, -1), Top, new Vector2I(end, 0), new Vector2I(end, end));
            Right.CopyRange(new Vector2I(end, m_resolution), new Vector2I(0, m_resolution), Bottom, new Vector2I(0, 0), new Vector2I(0, end));
            Right.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Back, new Vector2I(0, 0), new Vector2I(0, end));
            Right.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Front, new Vector2I(end, 0), new Vector2I(end, end));

            Top.CopyRange(new Vector2I(0, m_resolution), new Vector2I(end, m_resolution), Front, new Vector2I(0, 0), new Vector2I(end, 0));
            Top.CopyRange(new Vector2I(0, -1), new Vector2I(end, -1), Back, new Vector2I(end, 0), new Vector2I(0, 0));
            Top.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Right, new Vector2I(end, 0), new Vector2I(0, 0));
            Top.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Left, new Vector2I(0, 0), new Vector2I(end, 0));

            Bottom.CopyRange(new Vector2I(0, m_resolution), new Vector2I(end, m_resolution), Front, new Vector2I(end, end), new Vector2I(0, end));
            Bottom.CopyRange(new Vector2I(0, -1), new Vector2I(end, -1), Back, new Vector2I(0, end), new Vector2I(end, end));
            Bottom.CopyRange(new Vector2I(-1, 0), new Vector2I(-1, end), Right, new Vector2I(end, end), new Vector2I(0, end));
            Bottom.CopyRange(new Vector2I(m_resolution, 0), new Vector2I(m_resolution, end), Left, new Vector2I(0, end), new Vector2I(end, end));

            ProfilerShort.BeginNextBlock("Assign Borders");
            for (int i = 0; i < 6; ++i)
                Faces[i].FinishFace(string.Format("{0}_{1}", Name, MyCubemapHelpers.GetNameForFace(i)));
            ProfilerShort.End();
        }

        public FaceFormat[] Faces
        {
            get { return m_faces; }
        }
    }

    #endregion

    #region Biome Map

    public enum MyPlanetMapType
    {
        Materail,
        Ore,
        Biome,
        Occlusion
    }

    public class MyCubemapData<T> : IMyWrappedCubemapFace where T : struct, IEquatable<T>
    {
        private int m_real_resolution;

        public T[] Data;

        public void SetMaterial(int x, int y, T value)
        {
            Data[(y + 1) * m_real_resolution + (x + 1)] = value;
        }

        public void SetValue(int x, int y, ref T value)
        {
            int offset = (y + 1) * m_real_resolution + (x + 1);
            Data[offset] = value;
        }

        public void GetValue(int x, int y, out T value)
        {
            value = Data[(y + 1) * m_real_resolution + (x + 1)];
        }

        public T GetValue(float x, float y)
        {
            int xi = (int)(Resolution * x);
            int yi = (int)(Resolution * y);

            return Data[(yi + 1) * m_real_resolution + (xi + 1)];
        }

        public MyCubemapData(int resolution)
        {
            m_real_resolution = resolution + 2;
            Resolution = resolution;
            ResolutionMinusOne = resolution - 1;

            Data = new T[m_real_resolution * m_real_resolution];
        }

        // Get the most frequent byte
        [Obsolete("Obsolete until TODO's are fixed")]
        private static T GetMostFrequentValue(T[] bytes)
        {
            // TODO: Equals allocates
            switch (bytes.Length)
            {
                case 2:
                    return bytes[0];
                    break;
                case 3:
                    if (Equals(bytes[0], bytes[1]) || Equals(bytes[0], bytes[2]))
                        return bytes[0];
                    else if (Equals(bytes[1], bytes[2]))
                        return bytes[1];
                    else
                        return bytes[2];
                    break;
                case 4:
                    if (Equals(bytes[0], bytes[1]) || Equals(bytes[0], bytes[2]) || Equals(bytes[0], bytes[3])) return bytes[0];
                    if (Equals(bytes[1], bytes[2]) || Equals(bytes[1], bytes[3])) return bytes[1];
                    if (Equals(bytes[2], bytes[3])) return bytes[2];
                    return bytes[3];
                default:
                    return bytes[0];
            }
        }

        public void CopyRange(Vector2I start, Vector2I end, MyCubemapData<T> other, Vector2I oStart, Vector2I oEnd)
        {
            Vector2I myStep = MyCubemapHelpers.GetStep(ref start, ref end);
            Vector2I oStep = MyCubemapHelpers.GetStep(ref oStart, ref oEnd);
            T val;

            for (; start != end; start += myStep, oStart += oStep)
            {
                other.GetValue(oStart.X, oStart.Y, out val);
                SetValue(start.X, start.Y, ref val);
            }

            other.GetValue(oStart.X, oStart.Y, out val);
            SetValue(start.X, start.Y, ref val);
        }

        public void CopyRange(Vector2I start, Vector2I end, IMyWrappedCubemapFace other, Vector2I oStart, Vector2I oEnd)
        {
            var o = other as MyCubemapData<T>;
            if (o == null)
            {
                Debug.Fail("Cannot copy range between heightmap faces that are not the same type.");
                return;
            }

            CopyRange(start, end, o, oStart, oEnd);
        }

        public void FinishFace(string name)
        {
            T fmt = default(T);

            //
            // Smoothe corners
            //

            // BL
            SetPixel(-1, -1, ref fmt);

            // BR
            SetPixel(Resolution, -1, ref fmt);

            // TL
            SetPixel(-1, Resolution, ref fmt);

            // TR
            SetPixel(Resolution, Resolution, ref fmt);
        }

        public int Resolution { get; set; }

        public int ResolutionMinusOne { get; set; }

        internal void SetPixel(int y, int x, ref T pixel)
        {
            Data[(y + 1) * m_real_resolution + (x + 1)] = pixel;
        }
    }

    public class MyCubemap : MyWrappedCubemap<MyCubemapData<byte>>
    {
        public MyCubemap(params MyCubemapData<byte>[] faces)
        {
            if (faces.Length != 6)
            {
                MyDebug.FailRelease("When loading cubemap exactly 6 faces are expected.");
            }

            m_faces = faces;
            m_resolution = faces[0].Resolution;
            PrepareSides();
        }
    }

    #endregion

    public class MyHeightDetailTexture
    {
        public uint Resolution;
        public byte[] Data;

        public MyHeightDetailTexture(byte[] data, uint resolution)
        {
            Debug.Assert(resolution * resolution == data.Length, "Expect data array size to match resolution.");

            Resolution = resolution;

            Data = data;
        }

        public float GetValue(float x, float y)
        {
            Debug.Assert(x >= 0f && x < 1f && y >= 0f && y < 1f, "The requested coordinates are outside of range.");

            return Data[(int)(y * Resolution) * Resolution + (int)(x * Resolution)] * MyCubemapHelpers.BYTE_RECIP;
        }

        public byte GetValue(int x, int y)
        {
            Debug.Assert(x >= 0 && x < Resolution && y >= 0 && y < Resolution, "The requested coordinates are outside of range.");

            return Data[y * Resolution + x];
        }
    }

    /**
     * Textures used for tilesets (tiled texture joints maps).
     * 
     * Each texture contains a set of tiles or cells, one for each combination of same textured
     * cells in the corners of a square.
     * 
     * This system allows for fast blending of textures on grid vertices.
     */
    public class MyTileTexture<Pixel> where Pixel : struct
    {
        private Pixel[] m_data;
        private int m_stride;
        private Vector2I m_cellSize;

        /**
         * Cell coordinates indexed by cell corner values
         * 
         * Corners are assigned to bits which index into the array
         *  bits:
         *   tl tr bl br
         * 
         */
        private static readonly Vector2B[] s_baseCellCoords = new Vector2B[] {
            new Vector2B(0, 0), // 0 0 0 0
            new Vector2B(1, 0), // 0 0 0 1
            new Vector2B(2, 0), // 0 0 1 0
            new Vector2B(3, 0), // 0 0 1 1
            new Vector2B(0, 1), // 0 1 0 0
            new Vector2B(1, 1), // 0 1 0 1
            new Vector2B(2, 1), // 0 1 1 0
            new Vector2B(3, 1), // 0 1 1 1
            new Vector2B(0, 2), // 1 0 0 0
            new Vector2B(1, 2), // 1 0 0 1
            new Vector2B(2, 2), // 1 0 1 0
            new Vector2B(3, 2), // 1 0 1 1
            new Vector2B(0, 3), // 1 1 0 0
            new Vector2B(1, 3), // 1 1 0 1
            new Vector2B(2, 3), // 1 1 1 0
            new Vector2B(3, 3), // 1 1 1 1
        };

        // Pre-offset cell coordinates for light speed access.
        private Vector2I[] m_cellCoords = new Vector2I[16];

        public static readonly MyTileTexture<Pixel> Default = new MyTileTexture<Pixel>();

        public MyTileTexture(PixelBuffer image, int cellSize)
        {
            Debug.Assert(4 * cellSize >= image.Width && 4 * cellSize >= image.Height, "Image does not fit cells of the provided cell size");
            m_stride = image.RowStride;
            m_cellSize = new Vector2I(cellSize);
            m_data = image.GetPixels<Pixel>();

            PrepareCellCoords();
        }

        public MyTileTexture()
        {
            m_stride = 4;
            m_cellSize = new Vector2I(1);
            m_data = new Pixel[16];

            PrepareCellCoords();
        }

        private void PrepareCellCoords()
        {
            for (int i = 0; i < 16; i++)
            {
                m_cellCoords[i] = s_baseCellCoords[i] * m_cellSize.X;
            }
        }

        /**
         * Get the value at a given position for a given configuration.
         */
        public void GetValue(int corners, Vector2I coords, out Pixel value)
        {
            if (corners > 15)
            {
                Debug.Fail("Requested a invalid corner configuration.");
                value = default(Pixel);
            }

            Debug.Assert(coords.Between(ref Vector2I.Zero, ref m_cellSize), "The requested coordinates were outside of the cell range");

            coords += m_cellCoords[corners];

            value = m_data[coords.X + coords.Y * m_stride];
        }

        /**
         * Get the value at a given position for a given configuration.
         */
        public void GetValue(int corners, Vector2 coords, out Pixel value)
        {
            if (corners > 15)
            {
                Debug.Fail("Requested a invalid corner configuration.");
                value = default(Pixel);
            }

            Debug.Assert(coords.Between(ref Vector2.Zero, ref Vector2.One), "The requested coordinates were outside of the cell range");

            Vector2I icoords = new Vector2I(coords * m_cellSize.X);

            icoords += m_cellCoords[corners];

            value = m_data[icoords.X + icoords.Y * m_stride];
        }
    }
}

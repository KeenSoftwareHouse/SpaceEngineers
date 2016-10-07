using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Engine.Voxels.Planet;
using SharpDX;
using SharpDX.DXGI;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Utils;
using VRageMath;
using Quaternion = VRageMath.Quaternion;
using VRage.Game.Components;
using SharpDX.Toolkit.Graphics;
using SharpDXImage = SharpDX.Toolkit.Graphics.Image;
using VRage.Game.Definitions;
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyHeightMapLoadingSystem : MySessionComponentBase
    {
        Dictionary<string, MyHeightmapFace> m_heightMaps;
        Dictionary<string, MyCubemap[]> m_planetMaps;
        Dictionary<string, MyTileTexture<byte>> m_ditherTilesets;
        Dictionary<string, MyHeightDetailTexture> m_detailTextures;

        public static MyHeightMapLoadingSystem Static;

        string m_planetDataFolder;

        bool m_first = true;

        public override void LoadData()
        {
            base.LoadData();

            Static = this;
            m_heightMaps = new Dictionary<string, MyHeightmapFace>();
            m_planetMaps = new Dictionary<string, MyCubemap[]>();
            m_ditherTilesets = new Dictionary<string, MyTileTexture<byte>>();
            m_detailTextures = new Dictionary<string, MyHeightDetailTexture>();
            m_planetDataFolder = Path.Combine(MyFileSystem.ContentPath, "Data", "PlanetDataFiles");
        }

        private static void PreloadCrashingData()
        {
            /*MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Collision_Meteor, out effect))
            {
                effect.WorldMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, Vector3D.Zero, Vector3D.One);
            }*/

            ListReader<MyDebrisDefinition> debrisDefinitions = MyDefinitionManager.Static.GetDebrisDefinitions();
            foreach (var definition in debrisDefinitions)
            {
                VRage.Game.Models.MyModels.GetModelOnlyData(definition.Model);
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            m_heightMaps.Clear();
            m_heightMaps = null;
            m_planetMaps.Clear();
            m_planetMaps = null;
            m_ditherTilesets.Clear();
            m_ditherTilesets = null;
            m_detailTextures.Clear();
            m_detailTextures = null;
            m_first = true;
        }

        private static Image LoadTexture(string path)
        {
            if (!MyFileSystem.FileExists(path))
            {
                return null;
            }

            using (Stream textureStream = MyFileSystem.OpenRead(path))
            {
                return textureStream != null ? SharpDXImage.Load(textureStream) : null;
            }
        }

        #region Heightmap

        public MyHeightmapFace GetHeightMap(string folderName, string faceName, MyModContext context)
        {
            ProfilerShort.Begin("MyHeightmapLoadingSystem::GetHeightMap()");
            if (m_first)
            {
                PreloadCrashingData();
                m_first = false;
            }
            string fullPath = null;
            bool found = false;


            // Look for modded textures
            if (!context.IsBaseGame)
            {
                fullPath = Path.Combine(Path.Combine(context.ModPathData,"PlanetDataFiles"), folderName, faceName);
                if (MyFileSystem.FileExists(fullPath + ".png")) {
                    found = true;
                    fullPath += ".png";
                } else if (MyFileSystem.FileExists(fullPath + ".dds"))
                {
                    found = true;
                    fullPath += ".dds";
                }
            }

            // Use default ones
            if (!found)
            {
                fullPath = Path.Combine(m_planetDataFolder, folderName, faceName);
                if (MyFileSystem.FileExists(fullPath + ".png"))
                {
                    found = true;
                    fullPath += ".png";
                }
                else if (MyFileSystem.FileExists(fullPath + ".dds"))
                {
                    fullPath += ".dds";
                }


            }

            MyHeightmapFace value;
            if (m_heightMaps.TryGetValue(fullPath, out value))
            {
                ProfilerShort.End();
                return value;
            }
            try
            {
                using (SharpDXImage image = LoadTexture(fullPath))
                {
                    if (image == null)
                    {
                        MyLog.Default.WriteLine("Could not load texture {0}, no suitable format found. " + fullPath);
                    }
                    else
                    {
                        PixelBuffer buffer = image.GetPixelBuffer(0, 0, 0);

                        value = new MyHeightmapFace(buffer.Height);

                        if (buffer.Format == Format.R16_UNorm)
                        {
                            PrepareHeightMap(value, buffer);
                        }
                        else if (buffer.Format == Format.R8_UNorm)
                        {
                            PrepareHeightMap8Bit(value, buffer);
                        }
                        else
                        {
                            MyDebug.FailRelease(String.Format("Heighmap texture {0}: Invalid format {1} (expecting R16_UNorm or R8_UNorm).", fullPath, buffer.Format));
                        }
                        buffer = null;
                        image.Dispose();
                    }
                }
                m_heightMaps[fullPath] = value;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e.Message);
            }
            ProfilerShort.End();

            return value;
        }

        private static void PrepareHeightMap(MyHeightmapFace map, PixelBuffer imageData)
        {
            IntPtr mapData = imageData.DataPointer;
            var rowStride = imageData.RowStride;

            for (int y = 0; y < map.Resolution; y++)
            {
                Utilities.Read(mapData, map.Data, map.GetRowStart(y), imageData.Width);
                //imageData.GetPixels<ushort>(map.Data, y, map.GetRowStart(y), imageData.Width); this has a bug, see sharpdx source and be amused
                mapData += rowStride;
            }
        }

        public MyHeightDetailTexture GetDetailMap(string path)
        {
            if (m_first)
            {
                PreloadCrashingData();
                m_first = false;
            }

            MyHeightDetailTexture value;
            if (m_detailTextures.TryGetValue(path, out value))
            {
                return value;
            }
            string fullPath = Path.Combine(MyFileSystem.ContentPath, path);

            if (MyFileSystem.FileExists(fullPath + ".png"))
            {
                fullPath += ".png";
            }
            else
            {
                fullPath += ".dds";
            }


            using (var image = LoadTexture(fullPath))
            {
                if (image == null)
                {
                    value = new MyHeightDetailTexture(new byte[1], 1);
                }
                else
                {
                    PixelBuffer buffer = image.GetPixelBuffer(0, 0, 0);

                    if (buffer.Format != Format.R8_UNorm)
                    {
                        string err = String.Format("Detail map '{0}' could not be loaded, expected format R8_UNorm, got {1} instead.", fullPath, buffer.Format);
                        Debug.Fail(err);
                        MyLog.Default.WriteLine(err);
                        return null;
                    }
                    value = new MyHeightDetailTexture(buffer.GetPixels<byte>(), (uint)buffer.Height);
                    image.Dispose();
                }
            }
            m_detailTextures[path] = value;

            return value;
        }

        private static void PrepareHeightMap8Bit(MyHeightmapFace map, PixelBuffer imageData)
        {
            for (int y = 0; y < map.Resolution; y++)
            {
                for (int x = 0; x < map.Resolution; x++)
                {
                    map.SetValue(x, y, (ushort)(imageData.GetPixel<byte>(x, y) * 256));
                }
            }
        }

        #endregion

        #region Map texture loading

        private void ClearMatValues(MyCubemapData<byte>[] maps)
        {
            for (int i = 0; i < 6; ++i)
            {
                maps[i * 4] = null;
                maps[i * 4 + 1] = null;
                maps[i * 4 + 2] = null;
            }
        }

        private void ClearAddValues(MyCubemapData<byte>[] maps)
        {
            for (int i = 0; i < 6; ++i)
            {
                maps[i * 4 + 3] = null;
            }
        }

        public void GetPlanetMaps(string folder, MyModContext context, MyPlanetMaps mapsToUse, out MyCubemap[] maps)
        {
            if (m_planetMaps.ContainsKey(folder))
            {
                maps = m_planetMaps[folder];
                return;
            }

            maps = new MyCubemap[4];
            
            MyCubemapData<byte>[] tmpMaps = new MyCubemapData<byte>[4 * 6];

            byte[][] streams = new byte[4][];

            string fullPath;

            ProfilerShort.Begin("MyHeightmapLoadingSystem::GetPlanetMaps()");

            ProfilerShort.Begin("Load _mat");
            // Round one: material, ore, biome
            if (mapsToUse.Material || mapsToUse.Biome || mapsToUse.Ores)
                for (int i = 0; i < 6; ++i)
                {
                    string name = Path.Combine(folder, MyCubemapHelpers.GetNameForFace(i));

                    using (var texture = TryGetPlanetTexture(name, context, "_mat", out fullPath))
                    {
                        if (texture == null)
                        {
                            ClearMatValues(tmpMaps);
                            break;
                        }

                        PixelBuffer buffer = texture.GetPixelBuffer(0, 0, 0);

                        if (buffer.Format != Format.B8G8R8A8_UNorm &&
                            buffer.Format != Format.R8G8B8A8_UNorm)
                        {
                            MyDebug.FailRelease("While loading maps from {1}: Unsupported planet map format: {0}.", buffer.Format, fullPath);
                            break;
                        }

                        if (buffer.Width != buffer.Height)
                        {
                            MyDebug.FailRelease("While loading maps from {0}: Width and height must be the same.", fullPath);
                            break;
                        }

                        if (mapsToUse.Material)
                        {
                            tmpMaps[i * 4] = new MyCubemapData<byte>(buffer.Width);
                            streams[0] = tmpMaps[i * 4].Data;
                        }

                        if (mapsToUse.Biome)
                        {
                            tmpMaps[i * 4 + 1] = new MyCubemapData<byte>(buffer.Width);
                            streams[1] = tmpMaps[i * 4 + 1].Data;
                        }

                        if (mapsToUse.Ores)
                        {
                            tmpMaps[i * 4 + 2] = new MyCubemapData<byte>(buffer.Width);
                            streams[2] = tmpMaps[i * 4 + 2].Data;
                        }

                        // Invert channels for BGRA
                        if (buffer.Format == Format.B8G8R8A8_UNorm)
                        {
                            var tmp = streams[2];
                            streams[2] = streams[0];
                            streams[0] = tmp;
                        }
                        ReadChannelsFromImage(streams, buffer);
                        texture.Dispose();
                    }
                }

            ProfilerShort.BeginNextBlock("Load _add");
            // round two: add map
            if (mapsToUse.Occlusion)
                for (int i = 0; i < 6; ++i)
                {
                    string name = Path.Combine(folder, MyCubemapHelpers.GetNameForFace(i));

                    using (var texture = TryGetPlanetTexture(name, context,"_add", out fullPath))
                    {
                        if (texture == null)
                        {
                            ClearAddValues(tmpMaps);
                            break;
                        }

                        PixelBuffer buffer = texture.GetPixelBuffer(0, 0, 0);

                        if (buffer.Format != Format.B8G8R8A8_UNorm &&
                            buffer.Format != Format.R8G8B8A8_UNorm)
                        {
                            MyDebug.FailRelease("While loading maps from {1}: Unsupported planet map format: {0}.", buffer.Format, fullPath);
                            break;
                        }

                        if (buffer.Width != buffer.Height)
                        {
                            MyDebug.FailRelease("While loading maps from {0}: Width and height must be the same.", fullPath);
                            break;
                        }

                        if (mapsToUse.Occlusion)
                        {
                            tmpMaps[i * 4 + 3] = new MyCubemapData<byte>(buffer.Width);
                            streams[0] = tmpMaps[i * 4 + 3].Data;
                        }

                        streams[1] = streams[2] = null;

                        // Invert channels for BGRA
                        if (buffer.Format == Format.B8G8R8A8_UNorm)
                        {
                            var tmp = streams[2];
                            streams[2] = streams[0];
                            streams[0] = tmp;
                        }

                        ReadChannelsFromImage(streams, buffer);
                        texture.Dispose();
                    }
                }

            ProfilerShort.BeginNextBlock("Finish");

            for (int i = 0; i < 4; ++i)
            {
                if (tmpMaps[i] != null)
                {
                    var cmaps = new MyCubemapData<byte>[6];
                    for (int j = 0; j < 6; j++)
                    {
                        cmaps[j] = tmpMaps[i + j * 4];
                    }
                    maps[i] = new MyCubemap(cmaps);
                }
            }

            m_planetMaps[folder] = maps;

            ProfilerShort.End();
            ProfilerShort.End();
        }

        private Image TryGetPlanetTexture(string name, MyModContext context, string p, out string fullPath)
        {
            bool found = false;
            name += p;
            fullPath = Path.Combine(context.ModPathData, "PlanetDataFiles", name) + ".png";

            // Check for modded textures
            if (!context.IsBaseGame)
            {
                if (!MyFileSystem.FileExists(fullPath))
                {
                    fullPath = Path.Combine(context.ModPathData, "PlanetDataFiles", name) + ".dds";
                    if (MyFileSystem.FileExists(fullPath))
                        found = true;
                }
                else
                {
                    found = true;
                }
            }

            // Check for default textures
            if (!found)
            {
                fullPath = Path.Combine(m_planetDataFolder, name) + ".png";

                if (!MyFileSystem.FileExists(fullPath))
                {
                    fullPath = Path.Combine(m_planetDataFolder, name) + ".dds";
                    if (!MyFileSystem.FileExists(fullPath))
                    {
                        return null;
                    }
                }
                    
            }

            if (fullPath.Contains(".sbm"))
            {
                string archivePath = fullPath.Substring(0, fullPath.IndexOf(".sbm") + 4);
                string fileRelativeArchivePath = fullPath.Replace(archivePath + "\\", "");
                using (var sbm = VRage.Compression.MyZipArchive.OpenOnFile(archivePath))
                {
                    try
                    {
                        return SharpDXImage.Load(sbm.GetFile(fileRelativeArchivePath).GetStream());
                    }
                    catch (Exception ex)
                    {
                        MyDebug.FailRelease("Failed to load existing " + p + " file from .sbm archive. " + fullPath);
                        return null;
                    }
                }
            }
            
            return SharpDXImage.Load(fullPath);
        }

        private unsafe void ReadChannelsFromImage(byte[][] streams, PixelBuffer buffer)
        {
            byte* data = (byte*)buffer.DataPointer.ToPointer();

            int dim = buffer.Width;

            for (int i = 0; i < 4; ++i)
            {
                if (streams[i] != null)
                {
                    int j = 0, k = dim + 3;
                    for (int y = 0; y < dim; y++)
                    {
                        for (int x = 0; x < dim; x++)
                        {
                            streams[i][k] = data[j * 4 + i];
                            ++j;
                            ++k;
                        }
                        k += 2;
                    }
                }
            }
        }
        #endregion

        public MyTileTexture<byte> GetTerrainBlendTexture(MyPlanetMaterialBlendSettings settings)
        {
            MyTileTexture<byte> tex;

            string path = settings.Texture;
            int cellSize = settings.CellSize;

            if (!m_ditherTilesets.TryGetValue(path, out tex))
            {
                string fullPath = Path.Combine(MyFileSystem.ContentPath, path) + ".png";
                if (!File.Exists(fullPath))
                    fullPath = Path.Combine(MyFileSystem.ContentPath, path) + ".dds";

                SharpDXImage image = null;
                try
                {
                    image = SharpDXImage.Load(fullPath);
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e.Message);
                }

                if (image == null) return MyTileTexture<byte>.Default;

                PixelBuffer buffer = image.GetPixelBuffer(0, 0, 0);

                Debug.Assert(buffer.Format == Format.R8_UNorm);

                if (buffer.Format != Format.R8_UNorm) return MyTileTexture<byte>.Default;

                tex = new MyTileTexture<byte>(buffer, cellSize);
                image.Dispose();
            }

            return tex;
        }
    }
}


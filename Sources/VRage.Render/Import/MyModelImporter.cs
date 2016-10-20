#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BulletXNA.BulletCollision;
using VRage.FileSystem;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Animations;
using VRageRender.Fractures;

#endregion

namespace VRageRender.Import
{
    public class MyModelImporter
    {
        #region TagReader

        interface ITagReader
        {
            object Read(BinaryReader reader, int version);
        }
        struct TagReader<T> : ITagReader
        {
            Func<BinaryReader, int, T> m_tagReader;
            public TagReader(Func<BinaryReader, T> tagReader)
            {
                m_tagReader = (x, y) => tagReader(x);
            }

            public TagReader(Func<BinaryReader, int, T> tagReader)
            {
                m_tagReader = tagReader;
            }
#if !XB1
            T ReadTag(BinaryReader reader, int version)
            {
                return m_tagReader(reader, version);
            }
#endif
            public object Read(BinaryReader reader, int version)
            {
#if XB1
				return m_tagReader(reader, version);
#else
				return ReadTag(reader, version);
#endif
			}
        }


        static Dictionary<string, ITagReader> TagReaders = new Dictionary<string, ITagReader>()
        {
           { MyImporterConstants.TAG_VERTICES, new TagReader<HalfVector4[]>(ReadArrayOfHalfVector4) },
           { MyImporterConstants.TAG_NORMALS, new TagReader<Byte4[]>(ReadArrayOfByte4) },
           { MyImporterConstants.TAG_TEXCOORDS0, new TagReader<HalfVector2[] >(ReadArrayOfHalfVector2) },
           { MyImporterConstants.TAG_BINORMALS, new TagReader<Byte4[] >(ReadArrayOfByte4) },
           { MyImporterConstants.TAG_TANGENTS, new TagReader<Byte4[] >(ReadArrayOfByte4) },
           { MyImporterConstants.TAG_TEXCOORDS1, new TagReader<HalfVector2[] >(ReadArrayOfHalfVector2) },
           { MyImporterConstants.TAG_USE_CHANNEL_TEXTURES, new TagReader<bool>(x => x.ReadBoolean()) },
           { MyImporterConstants.TAG_BOUNDING_BOX, new TagReader<BoundingBox>(ReadBoundingBox) },
           { MyImporterConstants.TAG_BOUNDING_SPHERE, new TagReader<BoundingSphere>(ReadBoundingSphere) },
           { MyImporterConstants.TAG_RESCALE_FACTOR, new TagReader<float>(x => x.ReadSingle()) },
           { MyImporterConstants.TAG_SWAP_WINDING_ORDER, new TagReader<bool>(x => x.ReadBoolean()) },
           { MyImporterConstants.TAG_DUMMIES, new TagReader<Dictionary<string, MyModelDummy>>(ReadDummies) },
           { MyImporterConstants.TAG_MESH_PARTS, new TagReader<List<MyMeshPartInfo>>(ReadMeshParts) },
           { MyImporterConstants.TAG_MESH_SECTIONS, new TagReader<List<MyMeshSectionInfo>>(ReadMeshSections) },
           { MyImporterConstants.TAG_MODEL_BVH, new TagReader<GImpactQuantizedBvh>(delegate(BinaryReader reader) 
               {
                   GImpactQuantizedBvh bvh = new GImpactQuantizedBvh(); 
                   bvh.Load(ReadArrayOfBytes(reader)); 
                   return bvh; 
               }  ) },
           { MyImporterConstants.TAG_MODEL_INFO, new TagReader<MyModelInfo>(delegate(BinaryReader reader)
               {
                    int tri, vert;
                    Vector3 bb;
                    tri = reader.ReadInt32();
                    vert = reader.ReadInt32();
                    bb = ImportVector3(reader);
                    return new MyModelInfo(tri, vert, bb);
            } ) },
            { MyImporterConstants.TAG_BLENDINDICES, new TagReader<Vector4I[]>(ReadArrayOfVector4Int) },
            { MyImporterConstants.TAG_BLENDWEIGHTS, new TagReader<Vector4[]>(ReadArrayOfVector4) },
            { MyImporterConstants.TAG_ANIMATIONS, new TagReader<ModelAnimations>(ReadAnimations) },
            { MyImporterConstants.TAG_BONES, new TagReader<MyModelBone[]>(ReadBones) },
            { MyImporterConstants.TAG_BONE_MAPPING, new TagReader<Vector3I[]>(ReadArrayOfVector3Int) },
            { MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY, new TagReader<byte[]>(ReadArrayOfBytes) },
            { MyImporterConstants.TAG_PATTERN_SCALE, new TagReader<float>(x => x.ReadSingle()) },
            { MyImporterConstants.TAG_LODS, new TagReader<MyLODDescriptor[]>(ReadLODs) },
            { MyImporterConstants.TAG_HAVOK_DESTRUCTION_GEOMETRY, new TagReader<byte[]>(ReadArrayOfBytes) },
            { MyImporterConstants.TAG_HAVOK_DESTRUCTION, new TagReader<byte[]>(ReadArrayOfBytes) },
            { MyImporterConstants.TAG_FBXHASHSTRING, new TagReader<VRage.Security.Md5.Hash>(ReadHash) },
            { MyImporterConstants.TAG_HKTHASHSTRING, new TagReader<VRage.Security.Md5.Hash>(ReadHash) },
            { MyImporterConstants.TAG_XMLHASHSTRING, new TagReader<VRage.Security.Md5.Hash>(ReadHash) },
            { MyImporterConstants.TAG_MODEL_FRACTURES, new TagReader<MyModelFractures>(ReadModelFractures) },
        };

        #endregion

        #region Fields

        private Dictionary<string, object> m_retTagData = new Dictionary<string, object>();

        int m_version = 0;
        static string m_debugAssetName;

        #endregion

        #region Properties

        public int DataVersion
        {
            get { return m_version; }
        }

        public Dictionary<string, object> GetTagData() { return m_retTagData; }

        #endregion

        #region Reading

        /// <summary>
        /// Read Vector34
        /// </summary>
        static Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 vct = new Vector3();
            vct.X = reader.ReadSingle();
            vct.Y = reader.ReadSingle();
            vct.Z = reader.ReadSingle();
            return vct;
        }


        /// <summary>
        /// Read HalfVector4
        /// </summary>
        static HalfVector4 ReadHalfVector4(BinaryReader reader)
        {
            HalfVector4 vct = new HalfVector4();
            vct.PackedValue = reader.ReadUInt64();
            return vct;
        }

        /// <summary>
        /// Read HalfVector2
        /// </summary>
        static HalfVector2 ReadHalfVector2(BinaryReader reader)
        {
            HalfVector2 vct = new HalfVector2();
            vct.PackedValue = reader.ReadUInt32();
            return vct;
        }

        /// <summary>
        /// Read Byte4
        /// </summary>
        static Byte4 ReadByte4(BinaryReader reader)
        {
            Byte4 vct = new Byte4();
            vct.PackedValue = reader.ReadUInt32();
            return vct;
        }

        /// <summary>
        /// ImportVector3
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Vector3 ImportVector3(BinaryReader reader)
        {
            Vector3 vct;
            vct.X = reader.ReadSingle();
            vct.Y = reader.ReadSingle();
            vct.Z = reader.ReadSingle();
            return vct;
        }

        /// <summary>
        /// ImportVector4
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Vector4 ImportVector4(BinaryReader reader)
        {
            Vector4 vct;
            vct.X = reader.ReadSingle();
            vct.Y = reader.ReadSingle();
            vct.Z = reader.ReadSingle();
            vct.W = reader.ReadSingle();
            return vct;
        }

        /// <summary>
        /// ImportQuaternion
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Quaternion ImportQuaternion(BinaryReader reader)
        {
            Quaternion vct;
            vct.X = reader.ReadSingle();
            vct.Y = reader.ReadSingle();
            vct.Z = reader.ReadSingle();
            vct.W = reader.ReadSingle();
            return vct;
        }

        /// <summary>
        /// ImportVector4Int
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Vector4I ImportVector4Int(BinaryReader reader)
        {
            Vector4I vct;
            vct.X = reader.ReadInt32();
            vct.Y = reader.ReadInt32();
            vct.Z = reader.ReadInt32();
            vct.W = reader.ReadInt32();
            return vct;
        }

        /// <summary>
        /// ImportVector3Int
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Vector3I ImportVector3Int(BinaryReader reader)
        {
            Vector3I vct;
            vct.X = reader.ReadInt32();
            vct.Y = reader.ReadInt32();
            vct.Z = reader.ReadInt32();
            return vct;
        }

        /// <summary>
        /// ImportVector2
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Vector2 ImportVector2(BinaryReader reader)
        {
            Vector2 vct;
            vct.X = reader.ReadSingle();
            vct.Y = reader.ReadSingle();
            return vct;
        }

        /// <summary>
        /// Read array of HalfVector4
        /// </summary>
        static HalfVector4[] ReadArrayOfHalfVector4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            HalfVector4[] vctArr = new HalfVector4[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ReadHalfVector4(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// Read array of Byte4
        /// </summary>
        static Byte4[] ReadArrayOfByte4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Byte4[] vctArr = new Byte4[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ReadByte4(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// Read array of HalfVector2
        /// </summary>
        static HalfVector2[] ReadArrayOfHalfVector2(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            HalfVector2[] vctArr = new HalfVector2[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ReadHalfVector2(reader);
            }

            return vctArr;
        }



        /// <summary>
        /// ReadArrayOfVector3
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Vector3[] ReadArrayOfVector3(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector3[] vctArr = new Vector3[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ImportVector3(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// ReadArrayOfVector4
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Vector4[] ReadArrayOfVector4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector4[] vctArr = new Vector4[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ImportVector4(reader);
            }

            return vctArr;
        }


        /// <summary>
        /// ReadArrayOfVector4
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Vector4I[] ReadArrayOfVector4Int(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector4I[] vctArr = new Vector4I[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ImportVector4Int(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// ReadArrayOfVector3I
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Vector3I[] ReadArrayOfVector3Int(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector3I[] vctArr = new Vector3I[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ImportVector3Int(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// ReadArrayOfVector2
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Vector2[] ReadArrayOfVector2(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector2[] vctArr = new Vector2[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vctArr[i] = ImportVector2(reader);
            }

            return vctArr;
        }

        /// <summary>
        /// ReadArrayOfString
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static string[] ReadArrayOfString(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            string[] strArr = new string[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                strArr[i] = reader.ReadString();
            }

            return strArr;
        }


        /// <summary>
        /// ReadBoundingBox
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static BoundingBox ReadBoundingBox(BinaryReader reader)
        {
            BoundingBox bbox;
            bbox.Min = ImportVector3(reader);
            bbox.Max = ImportVector3(reader);
            return bbox;
        }


        /// <summary>
        /// ReadBoundingSphere
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static BoundingSphere ReadBoundingSphere(BinaryReader reader)
        {
            BoundingSphere bsphere;
            bsphere.Center = ImportVector3(reader);
            bsphere.Radius = reader.ReadSingle();
            return bsphere;
        }


        /// <summary>
        /// ReadMatrix
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Matrix ReadMatrix(BinaryReader reader)
        {
            Matrix mat;
            mat.M11 = reader.ReadSingle();
            mat.M12 = reader.ReadSingle();
            mat.M13 = reader.ReadSingle();
            mat.M14 = reader.ReadSingle();

            mat.M21 = reader.ReadSingle();
            mat.M22 = reader.ReadSingle();
            mat.M23 = reader.ReadSingle();
            mat.M24 = reader.ReadSingle();

            mat.M31 = reader.ReadSingle();
            mat.M32 = reader.ReadSingle();
            mat.M33 = reader.ReadSingle();
            mat.M34 = reader.ReadSingle();

            mat.M41 = reader.ReadSingle();
            mat.M42 = reader.ReadSingle();
            mat.M43 = reader.ReadSingle();
            mat.M44 = reader.ReadSingle();

            return mat;
        }


        /// <summary>
        /// ReadMeshParts
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static List<MyMeshPartInfo> ReadMeshParts(BinaryReader reader, int version)
        {
            List<MyMeshPartInfo> list = new List<MyMeshPartInfo>();
            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                MyMeshPartInfo meshPart = new MyMeshPartInfo();
                meshPart.Import(reader, version);
                list.Add(meshPart);
            }

            return list;
        }

        static List<MyMeshSectionInfo> ReadMeshSections(BinaryReader reader, int version)
        {
            List<MyMeshSectionInfo> list = new List<MyMeshSectionInfo>();
            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                MyMeshSectionInfo meshPart = new MyMeshSectionInfo();
                meshPart.Import(reader, version);
                list.Add(meshPart);
            }

            return list;
        }

        /// <summary>
        /// ReadDummies
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static Dictionary<string, MyModelDummy> ReadDummies(BinaryReader reader)
        {
            Dictionary<string, MyModelDummy> dummies = new Dictionary<string, MyModelDummy>();

            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                string str = reader.ReadString();
                Matrix mat = ReadMatrix(reader);

                MyModelDummy dummy = new MyModelDummy();
                dummy.Name = str;
                dummy.Matrix = mat;
                dummy.CustomData = new Dictionary<string, object>();

                int customDataCount = reader.ReadInt32();
                for (int j = 0; j < customDataCount; ++j)
                {
                    string name = reader.ReadString();
                    string value = reader.ReadString();
                    dummy.CustomData.Add(name, value);
                }

                dummies.Add(str, dummy);
            }

            return dummies;
        }


        /// <summary>
        /// ReadArrayOfInt
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static int[] ReadArrayOfInt(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            int[] intArr = new int[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                intArr[i] = reader.ReadInt32();
            }

            return intArr;
        }


        static byte[] ReadArrayOfBytes(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            byte[] data = reader.ReadBytes(nCount);
            return data;
        }

        static VRage.Security.Md5.Hash ReadHash(BinaryReader reader)
        {
            VRage.Security.Md5.Hash hash = new VRage.Security.Md5.Hash();
            hash.A = reader.ReadUInt32();
            hash.B = reader.ReadUInt32();
            hash.C = reader.ReadUInt32();
            hash.D = reader.ReadUInt32();
            return hash;
        }

        public static bool USE_LINEAR_KEYFRAME_REDUCTION = true;
        public static bool LINEAR_KEYFRAME_REDUCTION_STATS = false;
        public struct ReductionInfo
        {
            public string BoneName;
            public int OriginalKeys;
            public int OptimizedKeys;
        }
        public static Dictionary<string, List<ReductionInfo>> ReductionStats = new Dictionary<string, List<ReductionInfo>>();

        private const float TinyLength = 1e-8f;
        private const float TinyCosAngle = 0.9999999f;


        static MyAnimationClip ReadClip(BinaryReader reader)
        {
            MyAnimationClip clip = new MyAnimationClip();

            clip.Name = reader.ReadString();
            clip.Duration = reader.ReadDouble();

            int bonesCount = reader.ReadInt32();
            while (bonesCount-- > 0)
            {
                MyAnimationClip.Bone bone = new MyAnimationClip.Bone();
                bone.Name = reader.ReadString();

                int keyframesCount = reader.ReadInt32();
                while (keyframesCount-- > 0)
                {
                    MyAnimationClip.Keyframe keyframe = new MyAnimationClip.Keyframe();
                    keyframe.Time = reader.ReadDouble();
                    keyframe.Rotation = ImportQuaternion(reader);
                    keyframe.Translation = ImportVector3(reader);
                    bone.Keyframes.Add(keyframe);
                }

                clip.Bones.Add(bone);

                int originalCount = bone.Keyframes.Count;
                int newCount = 0;
                if (originalCount > 3)
                {
                    if (USE_LINEAR_KEYFRAME_REDUCTION)
                    {
                        LinkedList<MyAnimationClip.Keyframe> linkedList = new LinkedList<MyAnimationClip.Keyframe>();
                        foreach (var kf in bone.Keyframes)
                        {
                            linkedList.AddLast(kf);
                        }
                        //LinearKeyframeReduction(linkedList, 0.000001f, 0.985f);
                        //PercentageKeyframeReduction(linkedList, 0.9f);
                        LinearKeyframeReduction(linkedList, TinyLength, TinyCosAngle);
                        bone.Keyframes.Clear();
                        bone.Keyframes.AddArray(linkedList.ToArray());
                        newCount = bone.Keyframes.Count;
                    }
                    if (LINEAR_KEYFRAME_REDUCTION_STATS)
                    {
                        ReductionInfo ri = new ReductionInfo()
                        {
                            BoneName = bone.Name,
                            OriginalKeys = originalCount,
                            OptimizedKeys = newCount
                        };

                        List<ReductionInfo> riList;
                        if (!ReductionStats.TryGetValue(m_debugAssetName, out riList))
                        {
                            riList = new List<ReductionInfo>();
                            ReductionStats.Add(m_debugAssetName, riList);
                        }

                        riList.Add(ri);
                    }
                }

                CalculateKeyframeDeltas(bone.Keyframes);

            }

            return clip;
        }


        static void PercentageKeyframeReduction(LinkedList<MyAnimationClip.Keyframe> keyframes, float ratio)
        {
            if (keyframes.Count < 3)
                return;

            float i = 0;
            int toRemove = (int)(keyframes.Count * ratio);

            if (toRemove == 0)
                return;

            float d = (float)toRemove / keyframes.Count;

            for (LinkedListNode<MyAnimationClip.Keyframe> node = keyframes.First.Next; ; )
            {
                LinkedListNode<MyAnimationClip.Keyframe> next = node.Next;
                if (next == null)
                    break;

                if (i >= 1)
                {
                    while (i >= 1)
                    {
                        keyframes.Remove(node);
                        node = next;
                        next = node.Next;
                        i--;
                    }
                }
                else
                    node = next;

                i += d;
            }
        }


        /// <summary>
        /// This function filters out keyframes that can be approximated well with 
        /// linear interpolation.
        /// </summary>
        /// <param name="keyframes"></param>
        static void LinearKeyframeReduction(LinkedList<MyAnimationClip.Keyframe> keyframes, float translationThreshold, float rotationThreshold)
        {
            if (keyframes.Count < 3)
                return;

            for (LinkedListNode<MyAnimationClip.Keyframe> node = keyframes.First.Next; ; )
            {
                LinkedListNode<MyAnimationClip.Keyframe> next = node.Next;
                if (next == null)
                    break;

                // Determine nodes before and after the current node.
                MyAnimationClip.Keyframe a = node.Previous.Value;
                MyAnimationClip.Keyframe b = node.Value;
                MyAnimationClip.Keyframe c = next.Value;

                float t = (float)((node.Value.Time - node.Previous.Value.Time) /
                                   (next.Value.Time - node.Previous.Value.Time));

                Vector3 translation = Vector3.Lerp(a.Translation, c.Translation, t);
                var rotation = VRageMath.Quaternion.Slerp(a.Rotation, c.Rotation, t);

                if ((translation - b.Translation).LengthSquared() < translationThreshold &&
                   VRageMath.Quaternion.Dot(rotation, b.Rotation) > rotationThreshold)
                {
                    keyframes.Remove(node);
                }

                node = next;
            }
        }

        static void CalculateKeyframeDeltas(List<MyAnimationClip.Keyframe> keyframes)
        {
            // rest of frames
            for (int i = 1; i < keyframes.Count; i++)
            {
                var previousKey = keyframes[i - 1];
                var currentKey = keyframes[i];

                System.Diagnostics.Debug.Assert(previousKey.Time < currentKey.Time, "Incorrect keyframes timing!");

                currentKey.InvTimeDiff = 1.0f / (currentKey.Time - previousKey.Time);
            }
        }


        static ModelAnimations ReadAnimations(BinaryReader reader)
        {
            int clipCount = reader.ReadInt32();

            ModelAnimations animations = new ModelAnimations();

            while (clipCount-- > 0)
            {
                var clip = ReadClip(reader);
                animations.Clips.Add(clip);
            }

            int boneCount = reader.ReadInt32();

            while (boneCount-- > 0)
            {
                int bone = reader.ReadInt32();
                animations.Skeleton.Add(bone);
            }

            return animations;
        }


        static MyModelBone[] ReadBones(BinaryReader reader)
        {
            int boneCount = reader.ReadInt32();
            MyModelBone[] bones = new MyModelBone[boneCount];

            int i = 0;
            while (boneCount-- > 0)
            {
                MyModelBone bone = new MyModelBone();
                bones[i] = bone;

                bone.Name = reader.ReadString();
                bone.Index = i++;
                bone.Parent = reader.ReadInt32();
                bone.Transform = ReadMatrix(reader);
            }

            return bones;
        }

        static MyLODDescriptor[] ReadLODs(BinaryReader reader, int version)
        {
            int lodCount = reader.ReadInt32();
            var lods = new MyLODDescriptor[lodCount];

            int i = 0;
            while (lodCount-- > 0)
            {
                var lod = new MyLODDescriptor();
                lods[i++] = lod;
                lod.Read(reader); 
            }

            return lods;
        }

        static MyModelFractures ReadModelFractures(BinaryReader reader)
        {
            MyModelFractures modelFractures = new MyModelFractures();
            modelFractures.Version = reader.ReadInt32();

            var fracturesCount = reader.ReadInt32();
            for (int i = 0; i < fracturesCount; i++)
            {
                string fractureName = reader.ReadString();

                if (fractureName == "RandomSplit")
                {
                    var settings = new RandomSplitFractureSettings();

                    settings.NumObjectsOnLevel1 = reader.ReadInt32();
                    settings.NumObjectsOnLevel2 = reader.ReadInt32();
                    settings.RandomRange = reader.ReadInt32();
                    settings.RandomSeed1 = reader.ReadInt32();
                    settings.RandomSeed2 = reader.ReadInt32();
                    settings.SplitPlane = reader.ReadString();

                    modelFractures.Fractures = new MyFractureSettings[] { settings };
                }
                else if (fractureName == "Voronoi")
                {
                    var settings = new VoronoiFractureSettings();
                    settings.Seed = reader.ReadInt32();
                    settings.NumSitesToGenerate = reader.ReadInt32();
                    settings.NumIterations = reader.ReadInt32();
                    settings.SplitPlane = reader.ReadString();

                    modelFractures.Fractures = new MyFractureSettings[] { settings };
                }
                else if (fractureName == "WoodFracture")
                {                   
                    var settings = new WoodFractureSettings();
                    settings.BoardCustomSplittingPlaneAxis = reader.ReadBoolean();
                    settings.BoardFractureLineShearingRange = reader.ReadSingle();
                    settings.BoardFractureNormalShearingRange = reader.ReadSingle();
                    settings.BoardNumSubparts = reader.ReadInt32();
                    settings.BoardRotateSplitGeom = (WoodFractureSettings.Rotation)reader.ReadInt32();
                    settings.BoardScale = ReadVector3(reader);
                    settings.BoardScaleRange = ReadVector3(reader);
                    settings.BoardSplitGeomShiftRangeY = reader.ReadSingle();
                    settings.BoardSplitGeomShiftRangeZ = reader.ReadSingle();
                    settings.BoardSplittingAxis = ReadVector3(reader);
                    settings.BoardSplittingPlane = reader.ReadString();
                    settings.BoardSurfaceNormalShearingRange =reader.ReadSingle();
                    settings.BoardWidthRange = reader.ReadSingle();
                    settings.SplinterCustomSplittingPlaneAxis = reader.ReadBoolean();
                    settings.SplinterFractureLineShearingRange = reader.ReadSingle();
                    settings.SplinterFractureNormalShearingRange = reader.ReadSingle();
                    settings.SplinterNumSubparts = reader.ReadInt32();
                    settings.SplinterRotateSplitGeom = (WoodFractureSettings.Rotation)reader.ReadInt32();
                    settings.SplinterScale = ReadVector3(reader);
                    settings.SplinterScaleRange = ReadVector3(reader);
                    settings.SplinterSplitGeomShiftRangeY = reader.ReadSingle();
                    settings.SplinterSplitGeomShiftRangeZ = reader.ReadSingle();
                    settings.SplinterSplittingAxis = ReadVector3(reader);
                    settings.SplinterSplittingPlane = reader.ReadString();
                    settings.SplinterSurfaceNormalShearingRange = reader.ReadSingle();
                    settings.SplinterWidthRange = reader.ReadSingle();

                    modelFractures.Fractures = new MyFractureSettings[] { settings };
                }
            }

            return modelFractures;
        }

        #endregion

        #region Import

        public void ImportData(string assetFileName, string[] tags = null)
        {
            Clear();
            m_debugAssetName = assetFileName;
            var path = Path.IsPathRooted(assetFileName) ? assetFileName : Path.Combine(MyFileSystem.ContentPath, assetFileName);

            using (var fs = MyFileSystem.OpenRead(path))
            {
                if (fs != null)
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        LoadTagData(reader, tags);
                    }
                    fs.Close(); // OM: Although this shouldn't be needed, we experience problems with opening files with autorefresh, is this isn't called explicitely..
                }
            }
        }

        public void Clear()
        {
            m_retTagData.Clear();
            m_version = 0;
        }

        private void LoadTagData(BinaryReader reader, string[] tags)
        {
            //m_retTagData
            string tagName = reader.ReadString();
            string[] strArr = ReadArrayOfString(reader);
            m_retTagData.Add(tagName, strArr);

            string versionTag = "Version:";
            if (strArr.Length > 0 && strArr[0].Contains(versionTag))
            {
                string version = strArr[0].Replace(versionTag, "");
                m_version = Convert.ToInt32(version);
            }

            if (m_version >= 01066002)
            {
                var dict = ReadIndexDictionary(reader);

                if (tags == null)
                    tags = dict.Keys.ToArray();

                foreach (var tag in tags)
                {
                    if (dict.ContainsKey(tag))
                    {
                        int index = dict[tag];
                        reader.BaseStream.Seek(index, SeekOrigin.Begin);

                        string readTag = reader.ReadString();
                        System.Diagnostics.Debug.Assert(tag == readTag, "Wrong model data (version mismatch?)");

                        if (TagReaders.ContainsKey(tag))
                        {
                            m_retTagData.Add(tag, TagReaders[tag].Read(reader, m_version));
                        }
                    }
                }
            }
            else
                LoadOldVersion(reader);
        }

        Dictionary<string, int> ReadIndexDictionary(BinaryReader reader)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            int itemsCount = reader.ReadInt32();

            string tagName;
            for (int i = 0; i < itemsCount; i++)
            {
                tagName = reader.ReadString();
                int index = reader.ReadInt32();
                dict.Add(tagName, index);
            }

            return dict;
        }
      
        #endregion

        #region Old version


        void LoadOldVersion(BinaryReader reader)
        {
            //@ TAG_DUMMIES
            string tagName = reader.ReadString();
            Dictionary<string, MyModelDummy> dummies = ReadDummies(reader);
            m_retTagData.Add(tagName, dummies);

            //@ verticies
            tagName = reader.ReadString();

            HalfVector4[] VctArr = ReadArrayOfHalfVector4(reader);

            m_retTagData.Add(tagName, VctArr);
            //@ normals
            tagName = reader.ReadString();

            Byte4[] VctArr2 = ReadArrayOfByte4(reader);
            m_retTagData.Add(tagName, VctArr2);

            //@ texCoords0
            tagName = reader.ReadString();
            HalfVector2[] vct2Arr = ReadArrayOfHalfVector2(reader);
            m_retTagData.Add(tagName, vct2Arr);

            //TODO: binormals are no longer needed, they are computed in shaders
            //@ binormals 
            tagName = reader.ReadString();
            VctArr2 = ReadArrayOfByte4(reader);
            m_retTagData.Add(tagName, VctArr2);

            //@ tangents
            tagName = reader.ReadString();

            VctArr2 = ReadArrayOfByte4(reader);
            m_retTagData.Add(tagName, VctArr2);

            //@ texcoords1
            tagName = reader.ReadString();

            vct2Arr = ReadArrayOfHalfVector2(reader);
            m_retTagData.Add(tagName, vct2Arr);

            //////////////////////////////////////////////////////////////////////////

            //@ TAG_RESCALE_TO_LENGTH_IN_METERS
            tagName = reader.ReadString();
            bool bVal = reader.ReadBoolean();
            m_retTagData.Add(tagName, bVal);
            //@ TAG_LENGTH_IN_METERS
            tagName = reader.ReadString();
            float fVal = reader.ReadSingle();
            m_retTagData.Add(tagName, fVal);
            //@ TAG_RESCALE_FACTOR
            tagName = reader.ReadString();
            fVal = reader.ReadSingle();
            m_retTagData.Add(tagName, fVal);
            //@ TAG_CENTERED
            tagName = reader.ReadString();
            bVal = reader.ReadBoolean();
            m_retTagData.Add(tagName, bVal);
            //@ TAG_USE_MASK
            tagName = reader.ReadString();
            bVal = reader.ReadBoolean();
            m_retTagData.Add(tagName, bVal);
            //@ TAG_SPECULAR_SHININESS
            tagName = reader.ReadString();
            fVal = reader.ReadSingle();
            m_retTagData.Add(tagName, fVal);
            //@ TAG_SPECULAR_POWER
            tagName = reader.ReadString();
            fVal = reader.ReadSingle();
            m_retTagData.Add(tagName, fVal);
            //@ TAG_BOUNDING_BOX
            tagName = reader.ReadString();
            BoundingBox bbox = ReadBoundingBox(reader);
            m_retTagData.Add(tagName, bbox);
            //@ TAG_BOUNDING_SPHERE
            tagName = reader.ReadString();
            BoundingSphere bSphere = ReadBoundingSphere(reader);
            m_retTagData.Add(tagName, bSphere);
            //@ TAG_SWAP_WINDING_ORDER
            tagName = reader.ReadString();
            bVal = reader.ReadBoolean();
            m_retTagData.Add(tagName, bVal);



            //@ TAG_MESH_PARTS
            tagName = reader.ReadString();
            List<MyMeshPartInfo> meshes = ReadMeshParts(reader, m_version);
            m_retTagData.Add(tagName, meshes);

            //@ TAG_MODEL_BVH
            tagName = reader.ReadString();
            GImpactQuantizedBvh bvh = new GImpactQuantizedBvh();
            bvh.Load(ReadArrayOfBytes(reader));
            m_retTagData.Add(tagName, bvh);


            //@ TAG_MODEL_INFO
            tagName = reader.ReadString();
            int tri, vert;
            Vector3 bb;
            tri = reader.ReadInt32();
            vert = reader.ReadInt32();
            bb = ImportVector3(reader);
            m_retTagData.Add(tagName, new MyModelInfo(tri, vert, bb));

            //TAG_BLENDINDICES
            tagName = reader.ReadString();
            var blendIndices = ReadArrayOfVector4Int(reader);
            m_retTagData.Add(tagName, blendIndices);

            //TAG_BLENDWEIGHTS
            tagName = reader.ReadString();
            var blendWeights = ReadArrayOfVector4(reader);
            m_retTagData.Add(tagName, blendWeights);

            //TAG_ANIMATIONS
            tagName = reader.ReadString();
            var animations = ReadAnimations(reader);
            m_retTagData.Add(tagName, animations);

            //TAG_BONES
            tagName = reader.ReadString();
            var bones = ReadBones(reader);
            m_retTagData.Add(tagName, bones);

            //TAG_BONE_MAPPING
            tagName = reader.ReadString();
            var boneMapping = ReadArrayOfVector3Int(reader);
            m_retTagData.Add(tagName, boneMapping);

            // Compatibility
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                //TAG_HAVOK_COLLISION_GEOMETRY
                tagName = reader.ReadString();
                var havokCollision = ReadArrayOfBytes(reader);
                m_retTagData.Add(tagName, havokCollision);

            }

            // Compatibility
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                //TAG_PATTERN_SCALE
                tagName = reader.ReadString();
                m_retTagData.Add(tagName, reader.ReadSingle());
            }

            // Compatibility
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                //TAG_LODS
                tagName = reader.ReadString();
                m_retTagData.Add(tagName, ReadLODs(reader, 01066002));
            }

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                //TAG_HAVOK_DESTRUCTION_GEOMETRY
                tagName = reader.ReadString();
                var havokCollision = ReadArrayOfBytes(reader);
                m_retTagData.Add(tagName, havokCollision);
            }

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                //TAG_HAVOK_DESTRUCTION
                tagName = reader.ReadString();
                var havokCollision = ReadArrayOfBytes(reader);
                m_retTagData.Add(tagName, havokCollision);
            }
        }

        #endregion
    }
}

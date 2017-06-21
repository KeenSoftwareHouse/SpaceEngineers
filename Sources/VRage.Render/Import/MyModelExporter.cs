using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Animations;
using VRageRender.Fractures;

namespace VRageRender.Import
{
    public class Bone
    {
        public string Name { get; set; }
        // local matrix transform
        public VRageMath.Matrix LocalTransform { get; set; }
        // parent bone reference
        public Bone Parent { get; set; }
        // child bone references
        public List<Bone> Children { get; private set; }
        public Bone()
        {
            Children = new List<Bone>();
        }

        public override string ToString()
        {
            return Name + ": " + base.ToString();
        }
    }

    public class Mesh
    {
        public Matrix AbsoluteMatrix = Matrix.Identity;
        public int MeshIndex;

        /// <summary>
        /// Offset on the vertex buffer
        /// </summary>
        public int VertexOffset = -1;

        public int VertexCount = -1;

        /// <summary>
        /// Offset on the indices buffer
        /// </summary>
        public int StartIndex = -1;

        public int IndexCount = -1;
    }

    public class NodeDesc
    {
        public string Name;
        public string ParentName;
        public NodeDesc Parent;
    }

    public class MyModelExporter : IDisposable
    {
        private BinaryWriter m_writer = null;
        private BinaryWriter m_originalWriter = null;
        private MemoryStream m_cacheStream;

        /// <summary>
        /// c-tor
        /// </summary>
        /// <param name="filePath"></param>
        public MyModelExporter(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            m_writer = new BinaryWriter(fileStream);

            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// c-tor
        /// </summary>
        /// <param name="filePath"></param>
        public MyModelExporter()
        {
        }

        /// <summary>
        /// Close
        /// </summary>
        public void Dispose()
        {
            if (m_writer != null)
            {
                m_writer.Close();
                m_writer = null;
            }

            if (m_originalWriter != null)
            {
                m_originalWriter.Close();
                m_originalWriter = null;
            }
        }

        public void StartCacheWrite()
        {
            System.Diagnostics.Debug.Assert(m_originalWriter == null, "Start already called");
            System.Diagnostics.Debug.Assert(m_writer != null, "Disposed");

            m_originalWriter = m_writer;

            m_cacheStream = new MemoryStream();
            m_writer = new BinaryWriter(m_cacheStream);
        }

        public void StopCacheWrite()
        {
            System.Diagnostics.Debug.Assert(m_originalWriter != null, "Start wasnt called");

            m_writer.Close();
            m_writer = m_originalWriter;
        }

        public int GetCachePosition()
        {
            return (int)m_writer.BaseStream.Position;
        }

        public void FlushCache()
        {
            m_writer.Write(m_cacheStream.GetBuffer());
        }

        int CalculateIndexSize(Dictionary<string, int> dict)
        {
            int size = 4; //items count size
            foreach (var item in dict)
            {
                size += System.Text.ASCIIEncoding.ASCII.GetByteCount(item.Key) + 1;
                size += 4;
            }
            return size;
        }

        public void WriteIndexDictionary(Dictionary<string, int> dict)
        {
            int currentPos = (int)m_writer.BaseStream.Position;
            int dictOffset = CalculateIndexSize(dict);
            m_writer.Write(dict.Count);
            foreach (var item in dict)
            {
                m_writer.Write(item.Key);
                m_writer.Write(item.Value + dictOffset + currentPos);
            }
        }

        /// <summary>
        /// WriteTag
        /// </summary>
        /// <param name="tagName"></param>
        private void WriteTag(string tagName)
        {
            m_writer.Write(tagName);
        }

        /// <summary>
        /// WriteVector2
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector2 vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
        }


        /// <summary>
        /// WriteVector3
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector3 vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
            m_writer.Write(vct.Z);
        }

        /// <summary>
        /// WriteVector4
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector4 vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
            m_writer.Write(vct.Z);
            m_writer.Write(vct.W);
        }

        /// <summary>
        /// WriteMatrix
        /// </summary>
        /// <param name="matrix"></param>
        private void WriteMatrix(Matrix matrix)
        {
            m_writer.Write(matrix.M11);
            m_writer.Write(matrix.M12);
            m_writer.Write(matrix.M13);
            m_writer.Write(matrix.M14);

            m_writer.Write(matrix.M21);
            m_writer.Write(matrix.M22);
            m_writer.Write(matrix.M23);
            m_writer.Write(matrix.M24);

            m_writer.Write(matrix.M31);
            m_writer.Write(matrix.M32);
            m_writer.Write(matrix.M33);
            m_writer.Write(matrix.M34);

            m_writer.Write(matrix.M41);
            m_writer.Write(matrix.M42);
            m_writer.Write(matrix.M43);
            m_writer.Write(matrix.M44);
        }

        /// <summary>
        /// WriteVector2
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector2I vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
        }


        /// <summary>
        /// WriteVector3
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector3I vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
            m_writer.Write(vct.Z);
        }

        /// <summary>
        /// WriteVector4
        /// </summary>
        /// <param name="vct"></param>
        private void WriteVector(Vector4I vct)
        {
            m_writer.Write(vct.X);
            m_writer.Write(vct.Y);
            m_writer.Write(vct.Z);
            m_writer.Write(vct.W);
        }

        private void WriteVector(HalfVector4 val)
        {
            m_writer.Write(val.PackedValue);
        }

        private void WriteVector(HalfVector2 val)
        {
            m_writer.Write(val.PackedValue);
        }

        /// <summary>
        /// Write Byte4
        /// </summary>
        private void WriteByte4(Byte4 val)
        {
            m_writer.Write(val.PackedValue);
        }

        public bool ExportDataPackedAsHV4(string tagName, Vector3[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Vector3 vctVal in vctArr)
            {
                Vector3 v = vctVal;
                WriteVector(VF_Packer.PackPosition(ref v));
            }

            return true;
        }

        public bool ExportData(string tagName, HalfVector4[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (HalfVector4 vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        public bool ExportData(string tagName, Byte4[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Byte4 vctVal in vctArr)
            {
                WriteByte4(vctVal);
            }

            return true;
        }

        public bool ExportDataPackedAsB4(string tagName, Vector3[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Vector3 vctVal in vctArr)
            {
                Vector3 v = vctVal;
                Byte4 vct = new Byte4();
                vct.PackedValue = VF_Packer.PackNormal(ref v);
                WriteByte4(vct);
            }

            return true;
        }

        public bool ExportDataPackedAsHV2(string tagName, Vector2[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Vector2 vctVal in vctArr)
            {
                HalfVector2 vct = new HalfVector2(vctVal);
                WriteVector(vct);
            }

            return true;
        }

        public bool ExportData(string tagName, HalfVector2[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (HalfVector2 vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="vctArray"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Vector3[] vctArr)
        {
            if (vctArr == null)
                return true;

            WriteTag(tagName);
            m_writer.Write(vctArr.Length);
            foreach (Vector3 vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        public bool ExportData(string tagName, Vector3I[] vctArr)
        {
            if (vctArr == null)
                return true;

            WriteTag(tagName);
            m_writer.Write(vctArr.Length);
            foreach (var vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        public bool ExportData(string tagName, Vector4I[] vctArr)
        {
            if (vctArr == null)
                return true;

            WriteTag(tagName);
            m_writer.Write(vctArr.Length);
            foreach (var vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="vctArray"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Matrix[] matArr)
        {
            if (matArr == null)
                return true;

            WriteTag(tagName);
            m_writer.Write(matArr.Length);
            foreach (Matrix matVal in matArr)
            {
                WriteMatrix(matVal);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="vctArray"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Vector2[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Vector2 vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Vector4[] vctArr)
        {
            WriteTag(tagName);

            if (vctArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(vctArr.Length);
            foreach (Vector4 vctVal in vctArr)
            {
                WriteVector(vctVal);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, string[] strArr)
        {
            WriteTag(tagName);

            if (strArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(strArr.Length);
            foreach (string sVal in strArr)
                m_writer.Write(sVal);

            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, int[] intArr)
        {
            WriteTag(tagName);

            if (intArr == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(intArr.Length);
            foreach (int iVal in intArr)
                m_writer.Write(iVal);

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, byte[] byteArray)
        {
            WriteTag(tagName);

            if (byteArray == null)
            {
                m_writer.Write(0);
                return true;
            }

            m_writer.Write(byteArray.Length);
            m_writer.Write(byteArray);
            return true;
        }

 
        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, MyModelInfo modelInfo)
        {
            WriteTag(tagName);

            m_writer.Write(modelInfo.TrianglesCount);
            m_writer.Write(modelInfo.VerticesCount);
            WriteVector(modelInfo.BoundingBoxSize);
            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, BoundingBox boundingBox)
        {
            WriteTag(tagName);
            WriteVector(boundingBox.Min);
            WriteVector(boundingBox.Max);
            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="strArr"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, BoundingSphere boundingSphere)
        {
            WriteTag(tagName);
            WriteVector(boundingSphere.Center);
            m_writer.Write(boundingSphere.Radius);
            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Dictionary<string, Matrix> dict)
        {
            WriteTag(tagName);
            m_writer.Write(dict.Count);
            foreach (KeyValuePair<string, Matrix> pair in dict)
            {
                m_writer.Write(pair.Key);
                WriteMatrix(pair.Value);
            }
            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, List<MyMeshPartInfo> list)
        {
            WriteTag(tagName);
            m_writer.Write(list.Count);
            foreach (MyMeshPartInfo meshInfo in list)
            {
                meshInfo.Export(m_writer);
            }


            return true;
        }

        public bool ExportData(string tagName, List<MyMeshSectionInfo> list)
        {
            WriteTag(tagName);
            m_writer.Write(list.Count);
            foreach (MyMeshSectionInfo section in list)
            {
                section.Export(m_writer);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool ExportData(string tagName, Dictionary<string, MyModelDummy> dict)
        {
            WriteTag(tagName);
            m_writer.Write(dict.Count);
            foreach (KeyValuePair<string, MyModelDummy> pair in dict)
            {
                m_writer.Write(pair.Key);
                WriteMatrix(pair.Value.Matrix);

                m_writer.Write(pair.Value.CustomData.Count);
                foreach (KeyValuePair<string, object> customDataPair in pair.Value.CustomData)
                {
                    m_writer.Write(customDataPair.Key);
                    m_writer.Write(customDataPair.Value.ToString());
                }
            }
            return true;
        }


        /// <summary>
        /// ExportFloat
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ExportFloat(string tagName, float value)
        {
            WriteTag(tagName);
            m_writer.Write(value);
            return true;
        }


        /// <summary>
        /// ExportFloat
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ExportBool(string tagName, bool value)
        {
            WriteTag(tagName);
            m_writer.Write(value);
            return true;
        }


        protected void Write(MyAnimationClip clip)
        {
            m_writer.Write(clip.Name);
            m_writer.Write(clip.Duration);
            m_writer.Write(clip.Bones.Count);
            foreach (MyAnimationClip.Bone bone in clip.Bones)
            {
                m_writer.Write(bone.Name);
                m_writer.Write(bone.Keyframes.Count);
                foreach (MyAnimationClip.Keyframe keyframe in bone.Keyframes)
                {
                    m_writer.Write(keyframe.Time);
                    WriteQuaternion(keyframe.Rotation);
                    WriteVector(keyframe.Translation);
                }
            }
        }


        /// <summary>
        /// WriteQuaternion
        /// </summary>
        /// <param name="vct"></param>
        private void WriteQuaternion(Quaternion q)
        {
            m_writer.Write(q.X);
            m_writer.Write(q.Y);
            m_writer.Write(q.Z);
            m_writer.Write(q.W);
        }

        public bool ExportData(string tagName, ModelAnimations modelAnimations)
        {
            WriteTag(tagName);

            m_writer.Write(modelAnimations.Clips.Count);
            foreach (var clip in modelAnimations.Clips)
            {
                Write(clip);
            }

            m_writer.Write(modelAnimations.Skeleton.Count);
            foreach (var bone in modelAnimations.Skeleton)
            {
                m_writer.Write(bone);
            }

            return true;
        }      

        public bool ExportData(string tagName, MyModelBone[] bones)
        {
            WriteTag(tagName);

            m_writer.Write(bones.Length);

            foreach (var node in bones)
            {
                m_writer.Write(node.Name);
                m_writer.Write(node.Parent);
                WriteMatrix(node.Transform);
            }

            return true;
        }

        public bool ExportData(string tagName, MyLODDescriptor[] lodDescriptions)
        {
            WriteTag(tagName);

            m_writer.Write(lodDescriptions.Length);
            foreach (var desc in lodDescriptions)
            {
                desc.Write(m_writer);
            }

            return true;
        }


        public void ExportData(string tagName, VRage.Security.Md5.Hash hash)
        {
            WriteTag(tagName);

            m_writer.Write(hash.A);
            m_writer.Write(hash.B);
            m_writer.Write(hash.C);
            m_writer.Write(hash.D);
        }

        public void ExportData(string tagName, MyModelFractures modelFractures)
        {
            WriteTag(tagName);

            m_writer.Write(modelFractures.Version);

            m_writer.Write(modelFractures.Fractures != null ? modelFractures.Fractures.Length : 0);

            foreach (var modelFracture in modelFractures.Fractures)
            {
                if (modelFracture is RandomSplitFractureSettings)
                {
                    var settings = (RandomSplitFractureSettings)modelFracture;
                    m_writer.Write("RandomSplit");
                    m_writer.Write(settings.NumObjectsOnLevel1);
                    m_writer.Write(settings.NumObjectsOnLevel2);
                    m_writer.Write(settings.RandomRange);
                    m_writer.Write(settings.RandomSeed1);
                    m_writer.Write(settings.RandomSeed2);
                    m_writer.Write(settings.SplitPlane);
                }
                else
                    if (modelFracture is VoronoiFractureSettings)
                    {
                        var settings = (VoronoiFractureSettings)modelFracture;
                        m_writer.Write("Voronoi");
                        m_writer.Write(settings.Seed);
                        m_writer.Write(settings.NumSitesToGenerate);
                        m_writer.Write(settings.NumIterations);
                        m_writer.Write(settings.SplitPlane);
                    }
                    else
                        if (modelFracture is WoodFractureSettings)
                        {                            
                            var settings = (WoodFractureSettings)modelFracture;
                            m_writer.Write("WoodFracture");                            
                            m_writer.Write(settings.BoardCustomSplittingPlaneAxis);
                            m_writer.Write(settings.BoardFractureLineShearingRange);
                            m_writer.Write(settings.BoardFractureNormalShearingRange);
                            m_writer.Write(settings.BoardNumSubparts);
                            m_writer.Write((int)settings.BoardRotateSplitGeom);
                            WriteVector(settings.BoardScale);
                            WriteVector(settings.BoardScaleRange);
                            m_writer.Write(settings.BoardSplitGeomShiftRangeY);
                            m_writer.Write(settings.BoardSplitGeomShiftRangeZ);
                            WriteVector(settings.BoardSplittingAxis);
                            m_writer.Write(settings.BoardSplittingPlane);
                            m_writer.Write(settings.BoardSurfaceNormalShearingRange);
                            m_writer.Write(settings.BoardWidthRange);
                            m_writer.Write(settings.SplinterCustomSplittingPlaneAxis);
                            m_writer.Write(settings.SplinterFractureLineShearingRange);
                            m_writer.Write(settings.SplinterFractureNormalShearingRange);
                            m_writer.Write(settings.SplinterNumSubparts);
                            m_writer.Write((int)settings.SplinterRotateSplitGeom);
                            WriteVector(settings.SplinterScale);
                            WriteVector(settings.SplinterScaleRange);
                            m_writer.Write(settings.SplinterSplitGeomShiftRangeY);
                            m_writer.Write(settings.SplinterSplitGeomShiftRangeZ);
                            WriteVector(settings.SplinterSplittingAxis);
                            m_writer.Write(settings.SplinterSplittingPlane);
                            m_writer.Write(settings.SplinterSurfaceNormalShearingRange);
                            m_writer.Write(settings.SplinterWidthRange);
                        }
            }
        }

        public static void ExportModelData(string filename, Dictionary<string, object> tagData)
        {
            using (var modelExporter = new MyModelExporter(filename))
            {
                Dictionary<string, int> exportDictionary = new Dictionary<string, int>();

                var debugArray = (string[])tagData[MyImporterConstants.TAG_DEBUG];
                var debug = new List<string>(debugArray);
                debug.RemoveAll(x => x.Contains("Version:"));
                debug.Add("Version:01157001");

                // Debug info
                modelExporter.ExportData(MyImporterConstants.TAG_DEBUG, (string[])debug.ToArray());

                modelExporter.StartCacheWrite();

                // Dummy helper data
                exportDictionary.Add(MyImporterConstants.TAG_DUMMIES, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_DUMMIES, (Dictionary<string, MyModelDummy>)tagData[MyImporterConstants.TAG_DUMMIES]);

                exportDictionary.Add(MyImporterConstants.TAG_VERTICES, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_VERTICES, (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES]);

                exportDictionary.Add(MyImporterConstants.TAG_NORMALS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_NORMALS, (Byte4[])tagData[MyImporterConstants.TAG_NORMALS]);

                //  Depends on whether model contain texture channel 0
                exportDictionary.Add(MyImporterConstants.TAG_TEXCOORDS0, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_TEXCOORDS0, (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0]);

                exportDictionary.Add(MyImporterConstants.TAG_BINORMALS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BINORMALS, (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS]);
                exportDictionary.Add(MyImporterConstants.TAG_TANGENTS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_TANGENTS, (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS]);

                //  Depends on whether model contain texture channel 1
                exportDictionary.Add(MyImporterConstants.TAG_TEXCOORDS1, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_TEXCOORDS1, (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS1]);

                //  Properties
                exportDictionary.Add(MyImporterConstants.TAG_RESCALE_FACTOR, modelExporter.GetCachePosition());
                modelExporter.ExportFloat(MyImporterConstants.TAG_RESCALE_FACTOR, (float)tagData[MyImporterConstants.TAG_RESCALE_FACTOR]);
                exportDictionary.Add(MyImporterConstants.TAG_USE_CHANNEL_TEXTURES, modelExporter.GetCachePosition());
                modelExporter.ExportBool(MyImporterConstants.TAG_USE_CHANNEL_TEXTURES, (bool)tagData[MyImporterConstants.TAG_USE_CHANNEL_TEXTURES]);
                exportDictionary.Add(MyImporterConstants.TAG_BOUNDING_BOX, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BOUNDING_BOX, (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX]);
                exportDictionary.Add(MyImporterConstants.TAG_BOUNDING_SPHERE, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BOUNDING_SPHERE, (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE]);
                exportDictionary.Add(MyImporterConstants.TAG_SWAP_WINDING_ORDER, modelExporter.GetCachePosition());
                modelExporter.ExportBool(MyImporterConstants.TAG_SWAP_WINDING_ORDER, (bool)tagData[MyImporterConstants.TAG_SWAP_WINDING_ORDER]);

                exportDictionary.Add(MyImporterConstants.TAG_MESH_PARTS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_MESH_PARTS, (List<MyMeshPartInfo>)tagData[MyImporterConstants.TAG_MESH_PARTS]);

                exportDictionary.Add(MyImporterConstants.TAG_MESH_SECTIONS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_MESH_SECTIONS, (List<MyMeshSectionInfo>)tagData[MyImporterConstants.TAG_MESH_SECTIONS]);

                exportDictionary.Add(MyImporterConstants.TAG_MODEL_BVH, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_MODEL_BVH, ((BulletXNA.BulletCollision.GImpactQuantizedBvh)tagData[MyImporterConstants.TAG_MODEL_BVH]).Save());

                exportDictionary.Add(MyImporterConstants.TAG_MODEL_INFO, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_MODEL_INFO, (MyModelInfo)tagData[MyImporterConstants.TAG_MODEL_INFO]);

                exportDictionary.Add(MyImporterConstants.TAG_BLENDINDICES, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BLENDINDICES, (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES]);
                exportDictionary.Add(MyImporterConstants.TAG_BLENDWEIGHTS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BLENDWEIGHTS, (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS]);

                exportDictionary.Add(MyImporterConstants.TAG_ANIMATIONS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_ANIMATIONS, (ModelAnimations)tagData[MyImporterConstants.TAG_ANIMATIONS]);
                exportDictionary.Add(MyImporterConstants.TAG_BONES, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BONES, (MyModelBone[])tagData[MyImporterConstants.TAG_BONES]);
                exportDictionary.Add(MyImporterConstants.TAG_BONE_MAPPING, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_BONE_MAPPING, (Vector3I[])tagData[MyImporterConstants.TAG_BONE_MAPPING]);

                exportDictionary.Add(MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY, (byte[])tagData[MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY]);

                exportDictionary.Add(MyImporterConstants.TAG_PATTERN_SCALE, modelExporter.GetCachePosition());
                modelExporter.ExportFloat(MyImporterConstants.TAG_PATTERN_SCALE, (float)tagData[MyImporterConstants.TAG_PATTERN_SCALE]);

                exportDictionary.Add(MyImporterConstants.TAG_LODS, modelExporter.GetCachePosition());
                modelExporter.ExportData(MyImporterConstants.TAG_LODS, (MyLODDescriptor[])tagData[MyImporterConstants.TAG_LODS]);

                // AutoRebuild Tags Hashes
                if (tagData.ContainsKey(MyImporterConstants.TAG_FBXHASHSTRING))
                {
                    exportDictionary.Add(MyImporterConstants.TAG_FBXHASHSTRING, modelExporter.GetCachePosition());
                    modelExporter.ExportData(MyImporterConstants.TAG_FBXHASHSTRING, (VRage.Security.Md5.Hash)tagData[MyImporterConstants.TAG_FBXHASHSTRING]);
                }

                if (tagData.ContainsKey(MyImporterConstants.TAG_HKTHASHSTRING))
                {
                    exportDictionary.Add(MyImporterConstants.TAG_HKTHASHSTRING, modelExporter.GetCachePosition());
                    modelExporter.ExportData(MyImporterConstants.TAG_HKTHASHSTRING, (VRage.Security.Md5.Hash)tagData[MyImporterConstants.TAG_HKTHASHSTRING]);
                }

                if (tagData.ContainsKey(MyImporterConstants.TAG_XMLHASHSTRING))
                {
                    exportDictionary.Add(MyImporterConstants.TAG_XMLHASHSTRING, modelExporter.GetCachePosition());
                    modelExporter.ExportData(MyImporterConstants.TAG_XMLHASHSTRING, (VRage.Security.Md5.Hash)tagData[MyImporterConstants.TAG_XMLHASHSTRING]);
                }

                if (tagData.ContainsKey(MyImporterConstants.TAG_MODEL_FRACTURES))
                {
                    exportDictionary.Add(MyImporterConstants.TAG_MODEL_FRACTURES, modelExporter.GetCachePosition());
                    modelExporter.ExportData(MyImporterConstants.TAG_MODEL_FRACTURES, (MyModelFractures)tagData[MyImporterConstants.TAG_MODEL_FRACTURES]);
                }


                modelExporter.StopCacheWrite();

                modelExporter.WriteIndexDictionary(exportDictionary);

                modelExporter.FlushCache();
            }
        }
    }
}

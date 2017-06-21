using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage.Utils;
using VRageMath;
using VRageRender.Animations;

namespace VRage.Game
{
    public enum MyParticleEmitterType
    {
        Point,
        Line,
        Box,
        Sphere,
        Hemisphere,
        Circle,
    }

    public class MyParticleEmitter
    {
        static string[] MyParticleEmitterTypeStrings =
        {
            "Point",
            "Line",
            "Box",
            "Sphere",
            "Hemisphere",
            "Circle",
        };

        static List<string> s_emitterTypeStrings = MyParticleEmitterTypeStrings.ToList<string>();

        //Version of the emitter for serialization
        static readonly int Version = 5;

        private enum MyEmitterPropertiesEnum
        {
            Type,
            Offset,
            Rotation,
            AxisScale,
            Size,
            RadiusMin,
            RadiusMax,
            DirToCamera,
            LimitAngle
        }

        [ThreadStatic]
        IMyConstProperty[] m_propertiesInternal;
        IMyConstProperty[] m_properties
        {
            get
            {
                if (m_propertiesInternal == null)
                    m_propertiesInternal = new IMyConstProperty[Enum.GetValues(typeof(MyEmitterPropertiesEnum)).Length];
                return m_propertiesInternal;
            }
        }

        public MyParticleEmitterType Type
        {
            get { return (MyParticleEmitterType)(int)(m_properties[(int)MyEmitterPropertiesEnum.Type] as MyConstPropertyEnum); }
            private set { m_properties[(int)MyEmitterPropertiesEnum.Type].SetValue((int)value); }
        }

        public MyAnimatedPropertyVector3 Offset
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.Offset] as MyAnimatedPropertyVector3; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.Offset] = value; }
        }

        public MyAnimatedPropertyVector3 Rotation
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.Rotation] as MyAnimatedPropertyVector3; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.Rotation] = value; }
        }


        public MyConstPropertyVector3 AxisScale
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.AxisScale] as MyConstPropertyVector3; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.AxisScale] = value; }
        }

        public MyAnimatedPropertyFloat Size
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.Size] as MyAnimatedPropertyFloat; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.Size] = value; }
        }

        public MyConstPropertyFloat RadiusMin
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.RadiusMin] as MyConstPropertyFloat; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.RadiusMin] = value; }
        }

        public MyConstPropertyFloat RadiusMax
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.RadiusMax] as MyConstPropertyFloat; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.RadiusMax] = value; }
        }

        public MyConstPropertyBool DirToCamera
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.DirToCamera] as MyConstPropertyBool; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.DirToCamera] = value; }
        }

        public MyAnimatedPropertyFloat LimitAngle
        {
            get { return m_properties[(int)MyEmitterPropertiesEnum.LimitAngle] as MyAnimatedPropertyFloat; }
            private set { m_properties[(int)MyEmitterPropertiesEnum.LimitAngle] = value; }
        }


        public MyParticleEmitter(MyParticleEmitterType type)
        {
        }

        public void Init()
        {
            AddProperty(MyEmitterPropertiesEnum.Type, new MyConstPropertyEnum("Type", typeof(MyParticleEmitterType), s_emitterTypeStrings));
            AddProperty(MyEmitterPropertiesEnum.Offset, new MyAnimatedPropertyVector3("Offset"));
            AddProperty(MyEmitterPropertiesEnum.Rotation, new MyAnimatedPropertyVector3("Rotation", true, null));
            AddProperty(MyEmitterPropertiesEnum.AxisScale, new MyConstPropertyVector3("AxisScale"));
            AddProperty(MyEmitterPropertiesEnum.Size, new MyAnimatedPropertyFloat("Size"));
            AddProperty(MyEmitterPropertiesEnum.RadiusMin, new MyConstPropertyFloat("RadiusMin"));
            AddProperty(MyEmitterPropertiesEnum.RadiusMax, new MyConstPropertyFloat("RadiusMax"));
            AddProperty(MyEmitterPropertiesEnum.DirToCamera, new MyConstPropertyBool("DirToCamera"));
            AddProperty(MyEmitterPropertiesEnum.LimitAngle, new MyAnimatedPropertyFloat("LimitAngle"));

            Offset.AddKey(0, new Vector3(0, 0, 0));
            Rotation.AddKey(0, new Vector3(0, 0, 0));
            AxisScale.SetValue(Vector3.One);
            Size.AddKey(0, 1.0f);
            RadiusMin.SetValue(1.0f);
            RadiusMax.SetValue(1.0f);
            DirToCamera.SetValue(false);
        }

        public void Done()
        {
            for (int i = 0; i < GetProperties().Length; i++)
            {
                if (m_properties[i] is IMyAnimatedProperty)
                    (m_properties[i] as IMyAnimatedProperty).ClearKeys();
            }

            Close();
        }

        public void Start()
        {
            System.Diagnostics.Debug.Assert(Offset == null);
        }

        public void Close()
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = null;
            }
        }

        T AddProperty<T>(MyEmitterPropertiesEnum e, T property) where T : IMyConstProperty
        {
            m_properties[(int)e] = property;
            return property;
        }

        public void CalculateStartPosition(float elapsedTime, MatrixD worldMatrix, Vector3 userAxisScale, float userScale, out Vector3D startOffset, out Vector3D startPosition)
        {
            Vector3 currentOffsetUntransformed;
            Offset.GetInterpolatedValue<Vector3>(elapsedTime, out currentOffsetUntransformed);

            Vector3 currentRotation;
            Rotation.GetInterpolatedValue<Vector3>(elapsedTime, out currentRotation);

            float currentSize;
            Size.GetInterpolatedValue<float>(elapsedTime, out currentSize);
            currentSize *= MyUtils.GetRandomFloat(RadiusMin, RadiusMax) * userScale;

            Vector3 currentAxisScale = userAxisScale * AxisScale;

            Vector3 localPos = Vector3.Zero;
            Vector3D worldOffset;
            Vector3D.Transform(ref currentOffsetUntransformed, ref worldMatrix, out worldOffset);

            switch (Type)
            {
                case MyParticleEmitterType.Point:
                    localPos = Vector3.Zero;
                    break;

                case MyParticleEmitterType.Line:
                    localPos = Vector3.Forward * MyUtils.GetRandomFloat(0.0f, currentSize) * currentAxisScale;
                    break;

                case MyParticleEmitterType.Sphere:
                    if (LimitAngle.GetKeysCount() > 0)
                    {
                        float angle;
                        LimitAngle.GetInterpolatedValue<float>(elapsedTime, out angle);
                        angle = MathHelper.ToRadians(angle);
                        localPos = MyUtils.GetRandomVector3MaxAngle(angle) * currentSize * currentAxisScale;
                    }
                    else
                    {
                        localPos = MyUtils.GetRandomVector3Normalized() * currentSize * currentAxisScale;
                    }
                    break;

                case MyParticleEmitterType.Box:
                    float currentSizeHalf = currentSize * 0.5f;
                    localPos =
                        new Vector3(
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf),
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf),
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf)
                            ) * currentAxisScale;
                    break;

                case MyParticleEmitterType.Hemisphere:
                    localPos = MyUtils.GetRandomVector3HemisphereNormalized(Vector3.Forward) * currentSize * currentAxisScale;
                    break;

                case MyParticleEmitterType.Circle:
                    localPos = MyUtils.GetRandomVector3CircleNormalized() * currentSize * currentAxisScale;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            //if ((LimitAngle < 90 && (Type == MyParticleEmitterType.Hemisphere)) || 
            //    (LimitAngle < 180 && ((Type == MyParticleEmitterType.Sphere) || (Type == MyParticleEmitterType.Box))))
            //{
            //    var angleScaleFactor = Type == MyParticleEmitterType.Hemisphere ? LimitAngle / 90.0f : LimitAngle / 180.0f;
            //    var normalizedPos = Vector3.Normalize(localPos);
            //    var dotProduct = Vector3.Dot(Vector3.Forward, normalizedPos);
            //    var currentAngle = (float)Math.Acos(dotProduct);
            //    var finalAngle = currentAngle * angleScaleFactor;
            //    var rotationAxis = Vector3.Cross(Vector3.Forward,localPos);
            //    var rotationMatrix = Matrix.CreateFromAxisAngle(rotationAxis, finalAngle - currentAngle);
            //    Vector3.TransformNormal(ref localPos, ref rotationMatrix, out localPos);
            //}

            if (currentRotation.LengthSquared() > 0)
            {
                Matrix rotationMatrix = Matrix.CreateRotationX(MathHelper.ToRadians(currentRotation.X));
                rotationMatrix = rotationMatrix * Matrix.CreateRotationY(MathHelper.ToRadians(currentRotation.Y));
                rotationMatrix = rotationMatrix * Matrix.CreateRotationZ(MathHelper.ToRadians(currentRotation.Z));

                Vector3.TransformNormal(ref localPos, ref rotationMatrix, out localPos);
            }


            Vector3D worldPos;

            if (DirToCamera)
            {
                if (MyUtils.IsZero(MyTransparentGeometry.Camera.Forward))
                {
                    startPosition = Vector3.Zero;
                    startOffset = Vector3.Zero;
                    return;
                }
                MatrixD WorldView = worldMatrix * MyTransparentGeometry.CameraView;
                WorldView.Translation += currentOffsetUntransformed;
                MatrixD newWorld = WorldView * MatrixD.Invert(MyTransparentGeometry.CameraView);

                Vector3D dir = MyTransparentGeometry.Camera.Translation - newWorld.Translation;
                dir.Normalize();

                MatrixD matrix = MatrixD.CreateFromDir(dir);
                matrix.Translation = newWorld.Translation;

                Vector3D.Transform(ref localPos, ref matrix, out worldPos);

                startOffset = newWorld.Translation;
                startPosition = worldPos;
            }
            else
            {
                Vector3D.TransformNormal(ref localPos, ref worldMatrix, out worldPos);

                startOffset = worldOffset;
                startPosition = worldOffset + worldPos;
            }
        }

        public void CreateInstance(MyParticleEmitter emitter)
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                m_properties[i] = emitter.m_properties[i];
            }
        }

        public IMyConstProperty[] GetProperties()
        {
            return m_properties;
        }

        public void Duplicate(MyParticleEmitter targetEmitter)
        {
            for (int i = 0; i < m_properties.Length; i++)
            {
                targetEmitter.m_properties[i] = m_properties[i].Duplicate();
            }
        }

        #region Serialization

        public void Serialize(XmlWriter writer)
        {
            writer.WriteElementString("Version", Version.ToString(CultureInfo.InvariantCulture));
            writer.WriteStartElement("Properties");

            foreach (IMyConstProperty property in m_properties)
            {
                writer.WriteStartElement("Property");

                writer.WriteAttributeString("Name", property.Name);

                writer.WriteAttributeString("Type", property.BaseValueType);

                PropertyAnimationType animType = PropertyAnimationType.Const;
                if (property.Animated)
                    animType = property.Is2D ? PropertyAnimationType.Animated2D : PropertyAnimationType.Animated;
                writer.WriteAttributeString("AnimationType", animType.ToString());

                property.Serialize(writer);

                writer.WriteEndElement();//property
            }

            writer.WriteEndElement(); //Properties
        }

        public void Deserialize(XmlReader reader)
        {
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);
            if (version == 0)
            {
                DeserializeV0(reader);
                return;
            }
            else if (version == 1)
            {
                DeserializeV1(reader);
                return;
            }
            else if (version == 2)
            {
                DeserializeV2(reader);
                return;
            }
            else if (version == 3)
            {
                DeserializeV2(reader);
                return;
            }
            else if (version == 4)
            {
                DeserializeV4(reader);
                return;
            }

            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleEmitter
        }

        public void DeserializeFromObjectBuilder(ParticleEmitter emitter)
        {
            foreach (GenerationProperty property in emitter.Properties)
            {
                for (int i = 0; i < m_properties.Length; i++)
                {
                    if (m_properties[i].Name.Equals(property.Name))
                    {
                        m_properties[i].DeserializeFromObjectBuilder(property);
                    }
                }
            }
        }

        void DeserializeV0(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
                if (property.Name == "Rotation" || property.Name == "AxisScale")
                    continue;

                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleEmitter
        }

        void DeserializeV1(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
                if (property.Name == "AxisScale" || property.Name == "LimitAngle")
                    continue;

                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleEmitter
        }

        void DeserializeV2(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
                if (property.Name == "LimitAngle")
                    continue;

                property.Deserialize(reader);
            }

            if (reader.AttributeCount > 0 && reader.GetAttribute(0) == "LimitAngle")
            {
                reader.Skip();
            }

            reader.ReadEndElement(); //ParticleEmitter
        }

        void DeserializeV4(XmlReader reader)
        {
            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
                if (property.Name == "LimitAngle")
                    continue;

                property.Deserialize(reader);
            }

            reader.ReadEndElement(); //ParticleEmitter
        }

        #endregion

        #region DebugDraw

        public void DebugDraw(float elapsedTime, Matrix worldMatrix)
        {
            // TODO: Par
            //Vector3 currentOffsetUntransformed, currentOffset;
            //Offset.GetInterpolatedValue<Vector3>(elapsedTime, out currentOffsetUntransformed);
            //Vector3.Transform(ref currentOffsetUntransformed, ref worldMatrix, out currentOffset);
            //float currentSize;
            //Size.GetInterpolatedValue<float>(elapsedTime, out currentSize);

            //switch (Type)
            //{
            //    case MyParticleEmitterType.Point:
            //        {
            //            MyDebugDraw.DrawSphereWireframe(currentOffset, 0.1f, new Vector3(1, 1, 0), 1.0f);
            //        }
            //        break;

            //    case MyParticleEmitterType.Line:
            //        {
            //            if (DirToCamera)
            //            {
            //                Vector3 dir = MyCamera.Position - currentOffset;
            //                dir.Normalize();
            //                Matrix matrix = Matrix.CreateScale(currentSize) * MyMath.MatrixFromDir(dir);
            //                Vector3 currentOffsetScaled = Vector3.TransformNormal(Vector3.Forward, matrix);
            //                MyDebugDraw.DrawLine3D(worldMatrix.Translation, worldMatrix.Translation + currentOffsetScaled, Color.Yellow, Color.Yellow);
            //            }
            //            else
            //            {
            //                Vector3 currentOffsetScaled = Vector3.Transform(Vector3.Up * currentSize, worldMatrix);
            //                MyDebugDraw.DrawLine3D(worldMatrix.Translation, currentOffsetScaled, Color.Yellow, Color.Yellow);
            //            }
            //        }
            //        break;

            //    case MyParticleEmitterType.Sphere:
            //        {
            //            MyDebugDraw.DrawSphereWireframe(currentOffset, currentSize, new Vector3(1, 1, 0), 1.0f);
            //        }
            //        break;

            //    case MyParticleEmitterType.Box:
            //        {
            //            Matrix matrix = Matrix.CreateScale(currentSize) * Matrix.CreateTranslation(currentOffsetUntransformed) * worldMatrix;
            //            MyDebugDraw.DrawLowresBoxWireframe(matrix, new Vector3(1, 1, 0), 1.0f);
            //        }
            //        break;

            //    case MyParticleEmitterType.Hemisphere:
            //        {
            //            Vector3 worldPos = currentOffset;

            //            Matrix matrix;

            //            if (DirToCamera)
            //            {
            //                Matrix WorldView = worldMatrix * MyCamera.ViewMatrix;
            //                WorldView.Translation += currentOffsetUntransformed;
            //                Matrix newWorld = WorldView * Matrix.Invert(MyCamera.ViewMatrix);

            //                Vector3 dir = MyCamera.Position - newWorld.Translation;
            //                dir.Normalize();

            //                matrix = Matrix.CreateScale(currentSize) * Matrix.CreateRotationX(MathHelper.PiOver2) * MyMath.MatrixFromDir(dir);
            //                matrix.Translation = newWorld.Translation;
            //            }
            //            else
            //            {
            //                matrix = Matrix.CreateScale(currentSize) * Matrix.CreateTranslation(currentOffsetUntransformed) * worldMatrix;
            //            }

            //            MyDebugDraw.DrawHemisphereWireframe(matrix, new Vector3(1, 1, 0), 1.0f);
            //        }
            //        break;

            //    case MyParticleEmitterType.Circle:
            //        {
            //            //No debug draw
            //        }
            //        break;

            //    default:
            //        System.Diagnostics.Debug.Assert(false);
            //        break;
            //}

        }

        #endregion

    }

}

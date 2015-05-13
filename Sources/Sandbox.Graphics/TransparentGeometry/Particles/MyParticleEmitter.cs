using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VRage.Animations;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.TransparentGeometry.Particles
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
        static readonly int Version = 0;

        private enum MyEmitterPropertiesEnum
        {
            Type,
            Offset,
            Size,
            RadiusMin,
            RadiusMax,
            DirToCamera
        }

        IMyConstProperty[] m_properties = new IMyConstProperty[Enum.GetValues(typeof(MyEmitterPropertiesEnum)).Length];

        /// <summary>
        /// Public members to easy access
        /// </summary>
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


        public MyParticleEmitter(MyParticleEmitterType type)
        {
        }

        public void Init()
        {
            AddProperty(MyEmitterPropertiesEnum.Type, new MyConstPropertyEnum("Type", typeof(MyParticleEmitterType), s_emitterTypeStrings));
            AddProperty(MyEmitterPropertiesEnum.Offset, new MyAnimatedPropertyVector3("Offset"));
            AddProperty(MyEmitterPropertiesEnum.Size, new MyAnimatedPropertyFloat("Size"));
            AddProperty(MyEmitterPropertiesEnum.RadiusMin, new MyConstPropertyFloat("RadiusMin"));
            AddProperty(MyEmitterPropertiesEnum.RadiusMax, new MyConstPropertyFloat("RadiusMax"));
            AddProperty(MyEmitterPropertiesEnum.DirToCamera, new MyConstPropertyBool("DirToCamera"));

            Offset.AddKey(0, new Vector3(0, 0, 0));
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

        public void CalculateStartPosition(float elapsedTime, MatrixD worldMatrix, float userScale, out Vector3D startOffset, out Vector3D startPosition)
        {
            Vector3 currentOffsetUntransformed;
            Offset.GetInterpolatedValue<Vector3>(elapsedTime, out currentOffsetUntransformed);

            float currentSize;
            Size.GetInterpolatedValue<float>(elapsedTime, out currentSize);
            currentSize *= MyUtils.GetRandomFloat(RadiusMin, RadiusMax) * userScale;

            Vector3 localPos = Vector3.Zero;
            Vector3D worldOffset;
            Vector3D.Transform(ref currentOffsetUntransformed, ref worldMatrix, out worldOffset);

            switch (Type)
            {
                case MyParticleEmitterType.Point:
                    localPos = Vector3.Zero;
                    break;

                case MyParticleEmitterType.Line:
                    localPos = Vector3.Forward * MyUtils.GetRandomFloat(0.0f, currentSize);
                    break;

                case MyParticleEmitterType.Sphere:
                    localPos = MyUtils.GetRandomVector3Normalized() * currentSize;
                    break;

                case MyParticleEmitterType.Box:
                    float currentSizeHalf = currentSize * 0.5f;
                    localPos =  
                        new Vector3(
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf),
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf),
                            MyUtils.GetRandomFloat(-currentSizeHalf, currentSizeHalf)
                            );
                    break;

                case MyParticleEmitterType.Hemisphere:
                    localPos = MyUtils.GetRandomVector3HemisphereNormalized(Vector3.Forward) * currentSize;
                    break;

                case MyParticleEmitterType.Circle:
                    localPos = MyUtils.GetRandomVector3CircleNormalized() * currentSize;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
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
            writer.WriteStartElement("ParticleEmitter");
            writer.WriteAttributeString("version", Version.ToString(CultureInfo.InvariantCulture));

            foreach (IMyConstProperty property in m_properties)
            {
                property.Serialize(writer);
            }

            writer.WriteEndElement(); //ParticleEmitter
        }

        public void Deserialize(XmlReader reader)
        {
            int version = Convert.ToInt32(reader.GetAttribute("version"), CultureInfo.InvariantCulture);
            reader.ReadStartElement(); //ParticleEmitter

            foreach (IMyConstProperty property in m_properties)
            {
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

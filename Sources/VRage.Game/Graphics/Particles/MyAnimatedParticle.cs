using System;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Animations;
using VRageRender.Utils;


namespace VRage.Game
{
    public enum MyParticleTypeEnum : byte
    {
        Point = 0,
        Line = 1, 
        Trail = 2,
    }
    
    public class MyAnimatedParticle
    {
        float m_elapsedTime;  //secs

        MyParticleGeneration m_generation;

        public MyParticleTypeEnum Type;
        public MyBillboard.BlenType BlendType;

        public MyQuadD Quad = new MyQuadD();

        //Start values
        public Vector3D StartPosition;
        Vector3 m_velocity;
        public Vector3 Velocity
        {
            get { return m_velocity; }
            set
            {
                m_velocity = value;
            }
        }

        public float Life;  //secs
        public Vector3 Angle; //degrees
        public MyAnimatedPropertyVector3 RotationSpeed; //degrees
        public float Thickness;
        public float ColorIntensity;
        public float SoftParticleDistanceScale;

        public MyAnimatedPropertyVector3 Pivot = null; 
        public MyAnimatedPropertyVector3 PivotRotation = null; //degrees
        private Vector3 m_actualPivot = Vector3.Zero;
        Vector3 m_actualPivotRotation;

        public MyAnimatedPropertyFloat AlphaCutout = null;
        public MyAnimatedPropertyInt ArrayIndex = null;
        int m_arrayIndex = -1;

        public MyAnimatedPropertyVector3 Acceleration = null;
        private Vector3 m_actualAcceleration = Vector3.Zero;


        //Per life values
        public MyAnimatedPropertyFloat Radius = new MyAnimatedPropertyFloat();
        public MyAnimatedPropertyVector4 Color = new MyAnimatedPropertyVector4();
        public MyAnimatedPropertyTransparentMaterial Material = new MyAnimatedPropertyTransparentMaterial();

        Vector3D m_actualPosition;
        Vector3D m_previousPosition;
        Vector3 m_actualAngle; //radians

        float m_elapsedTimeDivider;
        float m_normalizedTime;

        public float NormalizedTime
        {
            get { return m_normalizedTime; }
        }

        //  Parameter-less constructor - because particles are stored in object pool
        public MyAnimatedParticle()
        {
            //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
            //  So don't initialize members here, do it in Start()
        }

        public void Start(MyParticleGeneration generation)
        {
            System.Diagnostics.Debug.Assert(Life > 0);

            m_elapsedTime = 0;
            m_normalizedTime = 0.0f;
            m_elapsedTimeDivider = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / Life;
            m_generation = generation;

            MyUtils.AssertIsValid(StartPosition);
            MyUtils.AssertIsValid(Angle);
            MyUtils.AssertIsValid(Velocity);
            
            m_actualPosition = StartPosition;
            m_previousPosition = m_actualPosition;
            m_actualAngle = new Vector3(MathHelper.ToRadians(Angle.X), MathHelper.ToRadians(Angle.Y), MathHelper.ToRadians(Angle.Z));

            if (Pivot != null)
            {
                Pivot.GetInterpolatedValue<Vector3>(0, out m_actualPivot);
            }

            if (PivotRotation != null)
            {
                PivotRotation.GetInterpolatedValue<Vector3>(0, out m_actualPivotRotation);
            }

            m_arrayIndex = -1;
            if (ArrayIndex != null)
            {
                ArrayIndex.GetInterpolatedValue<int>(m_normalizedTime, out m_arrayIndex);

                int arrayOffset = m_generation.ArrayOffset;
                Vector3 arraySize = m_generation.ArraySize;

                if (arraySize.X > 0 && arraySize.Y > 0)
                {
                    int arrayModulo = m_generation.ArrayModulo == 0 ? (int)arraySize.X * (int)arraySize.Y : m_generation.ArrayModulo;
                    m_arrayIndex = arrayOffset + m_arrayIndex % arrayModulo;
                }
            }
        }

        public bool Update()
        {
            m_elapsedTime += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (m_elapsedTime >= Life)
                return false;

            m_normalizedTime += m_elapsedTimeDivider;

            m_velocity += m_generation.GetEffect().Gravity * m_generation.Gravity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_previousPosition = m_actualPosition;
            m_actualPosition.X += Velocity.X * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_actualPosition.Y += Velocity.Y * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_actualPosition.Z += Velocity.Z * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (Pivot != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("Pivot calculation");
               
                Pivot.GetInterpolatedValue<Vector3>(m_normalizedTime, out m_actualPivot);

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            if (Acceleration != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("Acceleration calculation");

                Acceleration.GetInterpolatedValue<Vector3>(m_normalizedTime, out m_actualAcceleration);

                Matrix transform = Matrix.Identity;

                if (m_generation.AccelerationReference == MyAccelerationReference.Camera)
                {
                    transform = MyTransparentGeometry.Camera;
                }
                else if (m_generation.AccelerationReference == MyAccelerationReference.Local)
                {
                    
                }
                else if ((m_generation.AccelerationReference == MyAccelerationReference.Velocity))
                {
                    Vector3 actualVelocity = (Vector3)(m_actualPosition - m_previousPosition);

                    if (actualVelocity.LengthSquared() < 0.00001f)
                        m_actualAcceleration = Vector3.Zero;
                    else
                        transform = Matrix.CreateFromDir(Vector3.Normalize(actualVelocity));
                }
                else if ((m_generation.AccelerationReference == MyAccelerationReference.Gravity))
                {
                    if (m_generation.GetEffect().Gravity.LengthSquared() < 0.00001f)
                        m_actualAcceleration = Vector3.Zero;
                    else
                        transform = Matrix.CreateFromDir(Vector3.Normalize(m_generation.GetEffect().Gravity));
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Unknown RotationReference enum");
                }

                m_actualAcceleration = Vector3.TransformNormal(m_actualAcceleration, transform);

                Velocity += m_actualAcceleration * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            if (RotationSpeed != null)
            {
                Vector3 rotationSpeed;
                RotationSpeed.GetInterpolatedValue<Vector3>(m_normalizedTime, out rotationSpeed);
                m_actualAngle += new Vector3(MathHelper.ToRadians(rotationSpeed.X), MathHelper.ToRadians(rotationSpeed.Y), MathHelper.ToRadians(rotationSpeed.Z)) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            }

            if (PivotRotation != null)
            {
                Vector3 pivotRotation;
                PivotRotation.GetInterpolatedValue<Vector3>(m_normalizedTime, out pivotRotation);
                m_actualPivotRotation += pivotRotation;
            }

            if (ArrayIndex != null)
            {
                ArrayIndex.GetInterpolatedValue<int>(m_normalizedTime, out m_arrayIndex);
            }

            MyUtils.AssertIsValid(m_actualPosition);
            MyUtils.AssertIsValid(m_actualAngle);

            return true;
        }



        //  Update position, check collisions, etc. and draw if particle still lives.
        //  Return false if particle dies/timeouts in this tick.
        public bool Draw(VRageRender.MyBillboard billboard)
        {
            if (Pivot != null && !MyParticlesManager.Paused)
            {
                if (PivotRotation != null)
                {
                    Matrix pivotRotationTransform =
                      Matrix.CreateRotationX(MathHelper.ToRadians(m_actualPivotRotation.X) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) *
                      Matrix.CreateRotationY(MathHelper.ToRadians(m_actualPivotRotation.Y) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) *
                      Matrix.CreateRotationZ(MathHelper.ToRadians(m_actualPivotRotation.Z) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

                    m_actualPivot = Vector3.TransformNormal(m_actualPivot, pivotRotationTransform);
                }

                m_actualPivot = Vector3D.TransformNormal(m_actualPivot, m_generation.GetEffect().WorldMatrix);
            }

            var actualPosition = m_actualPosition + m_actualPivot;
            
            MyTransparentGeometry.StartParticleProfilingBlock("Distance calculation");
            //  This time is scaled according to planned lifespan of the particle

            // Distance for sorting
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyTransparentGeometry.Camera.Translation, actualPosition);

            MyTransparentGeometry.EndParticleProfilingBlock();

            // If distance to camera is really small don't draw it.
            if (billboard.DistanceSquared <= 0.1f)
            {
                return false;
            }

            MyTransparentGeometry.StartParticleProfilingBlock("Quad calculation");

            MyTransparentGeometry.StartParticleProfilingBlock("actualRadius");
            float actualRadius = 1;
            Radius.GetInterpolatedValue<float>(m_normalizedTime, out actualRadius);
            MyTransparentGeometry.EndParticleProfilingBlock();

            float actualAlphaCutout = 0;
            if (AlphaCutout != null)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("AlphaCutout calculation");

                AlphaCutout.GetInterpolatedValue<float>(m_normalizedTime, out actualAlphaCutout);

                MyTransparentGeometry.EndParticleProfilingBlock();
            }

            billboard.CustomViewProjection = -1;
            billboard.ParentID = -1;
            billboard.AlphaCutout = actualAlphaCutout;
            billboard.UVOffset = Vector2.Zero;
            billboard.UVSize = Vector2.One;

            billboard.BlendType = BlendType;


            float alpha = 1;


            Matrix transform = Matrix.Identity;
            Vector3 normal = Vector3.Forward;

            Vector3 actualVelocity = (Vector3)(m_actualPosition - m_previousPosition);

            float radiusBySpeed = m_generation.RadiusBySpeed;
            if (radiusBySpeed > 0)
            {
                float actualSpeed = actualVelocity.Length();
                actualRadius = Math.Max(actualRadius, actualRadius * m_generation.RadiusBySpeed * actualSpeed);
            }


            if (Type == MyParticleTypeEnum.Point)
            {
                MyTransparentGeometry.StartParticleProfilingBlock("GetBillboardQuadRotated");
               
                Vector2 actualRadiusV2 = new Vector2(actualRadius, actualRadius);

                if (Thickness > 0)
                {
                    actualRadiusV2.Y = Thickness;
                }

                if (m_generation.RotationReference == MyRotationReference.Camera)
                {
                    transform =
                       Matrix.CreateFromAxisAngle(MyTransparentGeometry.Camera.Right, m_actualAngle.X) *
                       Matrix.CreateFromAxisAngle(MyTransparentGeometry.Camera.Up, m_actualAngle.Y) *
                       Matrix.CreateFromAxisAngle(MyTransparentGeometry.Camera.Forward, m_actualAngle.Z);

                    GetBillboardQuadRotated(billboard, ref actualPosition, actualRadiusV2, ref transform, MyTransparentGeometry.Camera.Left, MyTransparentGeometry.Camera.Up);
                }
                else if (m_generation.RotationReference == MyRotationReference.Local)
                {
                    transform = Matrix.CreateFromAxisAngle(m_generation.GetEffect().WorldMatrix.Right, m_actualAngle.X) *
                    Matrix.CreateFromAxisAngle(m_generation.GetEffect().WorldMatrix.Up, m_actualAngle.Y) *
                    Matrix.CreateFromAxisAngle(m_generation.GetEffect().WorldMatrix.Forward, m_actualAngle.Z);

                    GetBillboardQuadRotated(billboard, ref actualPosition, actualRadiusV2, ref transform, m_generation.GetEffect().WorldMatrix.Left, m_generation.GetEffect().WorldMatrix.Up);
                }
                else if (m_generation.RotationReference == MyRotationReference.Velocity)
                {
                    if (actualVelocity.LengthSquared() < 0.00001f)
                        return false;

                    Matrix velocityRef = Matrix.CreateFromDir(Vector3.Normalize(actualVelocity));

                    transform = Matrix.CreateFromAxisAngle(velocityRef.Right, m_actualAngle.X) *
                    Matrix.CreateFromAxisAngle(velocityRef.Up, m_actualAngle.Y) *
                    Matrix.CreateFromAxisAngle(velocityRef.Forward, m_actualAngle.Z);

                    GetBillboardQuadRotated(billboard, ref actualPosition, actualRadiusV2, ref transform, velocityRef.Left, velocityRef.Up);
                }
                else if (m_generation.RotationReference == MyRotationReference.VelocityAndCamera)
                {
                    if (actualVelocity.LengthSquared() < 0.0001f)
                        return false;

                    Vector3 cameraToPoint = Vector3.Normalize(m_actualPosition - MyTransparentGeometry.Camera.Translation);
                    Vector3 velocityDir = Vector3.Normalize(actualVelocity);

                    Vector3 sideVector = Vector3.Cross(cameraToPoint, velocityDir);
                    Vector3 upVector = Vector3.Cross(sideVector, velocityDir);

                    Matrix velocityRef = Matrix.CreateWorld(m_actualPosition, velocityDir, upVector);

                    transform = Matrix.CreateFromAxisAngle(velocityRef.Right, m_actualAngle.X) *
                    Matrix.CreateFromAxisAngle(velocityRef.Up, m_actualAngle.Y) *
                    Matrix.CreateFromAxisAngle(velocityRef.Forward, m_actualAngle.Z);

                    GetBillboardQuadRotated(billboard, ref actualPosition, actualRadiusV2, ref transform, velocityRef.Left, velocityRef.Up);
                }
                else if (m_generation.RotationReference == MyRotationReference.LocalAndCamera)
                {
                    Vector3 cameraToPoint = Vector3.Normalize(m_actualPosition - MyTransparentGeometry.Camera.Translation);
                    Vector3 localDir = m_generation.GetEffect().WorldMatrix.Forward;
                    float dot = cameraToPoint.Dot(localDir);
                    Matrix velocityRef;
                    if (dot >= 0.9999f)
                    {
                        // TODO Petr: probably not correct, at least it does not produce NaN positions
                        velocityRef = Matrix.CreateTranslation(m_actualPosition);
                    }
                    else
                    {
                        Vector3 sideVector = Vector3.Cross(cameraToPoint, localDir);
                        Vector3 upVector = Vector3.Cross(sideVector, localDir);

                        velocityRef = Matrix.CreateWorld(m_actualPosition, localDir, upVector);
                    }

                    transform = Matrix.CreateFromAxisAngle(velocityRef.Right, m_actualAngle.X) *
                    Matrix.CreateFromAxisAngle(velocityRef.Up, m_actualAngle.Y) *
                    Matrix.CreateFromAxisAngle(velocityRef.Forward, m_actualAngle.Z);

                    GetBillboardQuadRotated(billboard, ref actualPosition, actualRadiusV2, ref transform, velocityRef.Left, velocityRef.Up);
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Unknown RotationReference enum");
                }

                MyTransparentGeometry.EndParticleProfilingBlock();
            }
            else if (Type == MyParticleTypeEnum.Line)
            {
                if (MyUtils.IsZero(Velocity.LengthSquared()))
                    Velocity = MyUtils.GetRandomVector3Normalized();

                MyQuadD quad = new MyQuadD();

                MyPolyLineD polyLine = new MyPolyLineD();
                //polyLine.LineDirectionNormalized = MyUtils.Normalize(Velocity);
                if (actualVelocity.LengthSquared() > 0)
                    polyLine.LineDirectionNormalized = MyUtils.Normalize(actualVelocity);
                else
                    polyLine.LineDirectionNormalized = MyUtils.Normalize(Velocity);

                if (m_actualAngle.Z != 0)
                {
                    polyLine.LineDirectionNormalized = Vector3.TransformNormal(polyLine.LineDirectionNormalized, Matrix.CreateRotationY(m_actualAngle.Z));
                }

                polyLine.Point0 = actualPosition;
                polyLine.Point1.X = actualPosition.X - polyLine.LineDirectionNormalized.X * actualRadius;
                polyLine.Point1.Y = actualPosition.Y - polyLine.LineDirectionNormalized.Y * actualRadius;
                polyLine.Point1.Z = actualPosition.Z - polyLine.LineDirectionNormalized.Z * actualRadius;

                if (m_actualAngle.LengthSquared() > 0)
                { //centerize
                    polyLine.Point0.X = polyLine.Point0.X - polyLine.LineDirectionNormalized.X * actualRadius * 0.5f;
                    polyLine.Point0.Y = polyLine.Point0.Y - polyLine.LineDirectionNormalized.Y * actualRadius * 0.5f;
                    polyLine.Point0.Z = polyLine.Point0.Z - polyLine.LineDirectionNormalized.Z * actualRadius * 0.5f;
                    polyLine.Point1.X = polyLine.Point1.X - polyLine.LineDirectionNormalized.X * actualRadius * 0.5f;
                    polyLine.Point1.Y = polyLine.Point1.Y - polyLine.LineDirectionNormalized.Y * actualRadius * 0.5f;
                    polyLine.Point1.Z = polyLine.Point1.Z - polyLine.LineDirectionNormalized.Z * actualRadius * 0.5f;
                }

                polyLine.Thickness = Thickness;
                var camPos = MyTransparentGeometry.Camera.Translation;
                MyUtils.GetPolyLineQuad(out quad, ref polyLine, camPos);

                transform.Forward = polyLine.LineDirectionNormalized;

                billboard.Position0 = quad.Point0;
                billboard.Position1 = quad.Point1;
                billboard.Position2 = quad.Point2;
                billboard.Position3 = quad.Point3;
            }
            else if (Type == MyParticleTypeEnum.Trail)
            {
                if (Quad.Point0 == Quad.Point2) //not moving particle
                    return false;
                if (Quad.Point1 == Quad.Point3) //not moving particle was previous one
                    return false;
                if (Quad.Point0 == Quad.Point3) //not moving particle was previous one
                    return false;

                billboard.Position0 = Quad.Point0;
                billboard.Position1 = Quad.Point1;
                billboard.Position2 = Quad.Point2;
                billboard.Position3 = Quad.Point3;
            }
            else
            {
                throw new NotSupportedException(Type + " is not supported particle type");
            }

            if (this.m_generation.AlphaAnisotropic)
            {
                normal = Vector3.Normalize(Vector3.Cross(billboard.Position0 - billboard.Position1, billboard.Position0 - billboard.Position2));

                Vector3 forward = (billboard.Position0 + billboard.Position1 + billboard.Position2 + billboard.Position3) / 4 - MyTransparentGeometry.Camera.Translation;

                //Vector3 forward = MyTransparentGeometry.Camera.Forward;

                float angle = Math.Abs(Vector3.Dot(MyUtils.Normalize(forward), normal));


                float alphaCone = 1 - (float)Math.Pow(1 - angle, 4);
                alpha = alphaCone;
            }


            MyTransparentGeometry.EndParticleProfilingBlock();

            MyTransparentGeometry.StartParticleProfilingBlock("Material calculation");

            Vector4 color = Vector4.One;
            if (Color.GetKeysCount() > 0)
            {
                Color.GetInterpolatedValue<Vector4>(m_normalizedTime, out color);
            }

            if (m_arrayIndex != -1)
            {
                Vector3 arraySize = m_generation.ArraySize;
                if (arraySize.X > 0 && arraySize.Y > 0)
                {
                    int arrayOffset = m_generation.ArrayOffset;
                    int arrayModulo = m_generation.ArrayModulo == 0 ? (int)arraySize.X * (int)arraySize.Y : m_generation.ArrayModulo;

                    m_arrayIndex = m_arrayIndex % arrayModulo + arrayOffset;

                    float xDiv = 1.0f / arraySize.X;
                    float yDiv = 1.0f / arraySize.Y;
                    int xIndex = m_arrayIndex % (int)arraySize.X;
                    int yIndex = m_arrayIndex / (int)arraySize.X;

                    billboard.UVOffset = new Vector2(xDiv * xIndex, yDiv * yIndex);
                    billboard.UVSize = new Vector2(xDiv, yDiv);
                }
            }

            var material1 = MyTransparentMaterials.GetMaterial("ErrorMaterial");
            Material.GetInterpolatedValue(m_normalizedTime, out material1);

            MyTransparentGeometry.EndParticleProfilingBlock();
                     
            //This gets 0.44ms for 2000 particles
            MyTransparentGeometry.StartParticleProfilingBlock("billboard.Start");

            if (material1 != null)
                billboard.Material = material1.Name;

            billboard.Color = color * alpha * m_generation.GetEffect().UserColorMultiplier;
            billboard.ColorIntensity = ColorIntensity;
            billboard.SoftParticleDistanceScale = SoftParticleDistanceScale;

            MyTransparentGeometry.EndParticleProfilingBlock();

            return true;
        }

        public void AddMotionInheritance(ref float motionInheritance, ref MatrixD deltaMatrix)
        {
            Vector3D newPosition = Vector3D.Transform(m_actualPosition, deltaMatrix);

            m_actualPosition = m_actualPosition + (newPosition - m_actualPosition) * motionInheritance;

            Velocity = Vector3.TransformNormal(Velocity, deltaMatrix);
        }

        public Vector3D ActualPosition
        {
            get { return m_actualPosition; }
        }

        /// <summary>
        /// Return quad whos face is always looking to the camera. 
        /// IMPORTANT: This bilboard looks same as point vertexes (point sprites) - horizontal and vertical axes of billboard are always parallel to screen
        /// That means, if billboard is in the left-up corner of screen, it won't be distorted by perspective. If will look as 2D quad on screen. As I said, it's same as GPU points.
        /// </summary>
        private static void GetBillboardQuadRotated(VRageRender.MyBillboard billboard, ref Vector3D position, float radius, float angle)
        {
            float angleCos = radius * (float)Math.Cos(angle);
            float angleSin = radius * (float)Math.Sin(angle);

            //	Two main vectors of a billboard rotated around the view axis/vector
            Vector3D billboardAxisX = new Vector3D();
            billboardAxisX.X = angleCos * MyTransparentGeometry.Camera.Left.X + angleSin * MyTransparentGeometry.Camera.Up.X;
            billboardAxisX.Y = angleCos * MyTransparentGeometry.Camera.Left.Y + angleSin * MyTransparentGeometry.Camera.Up.Y;
            billboardAxisX.Z = angleCos * MyTransparentGeometry.Camera.Left.Z + angleSin * MyTransparentGeometry.Camera.Up.Z;

            Vector3D billboardAxisY = new Vector3D();
            billboardAxisY.X = -angleSin * MyTransparentGeometry.Camera.Left.X + angleCos * MyTransparentGeometry.Camera.Up.X;
            billboardAxisY.Y = -angleSin * MyTransparentGeometry.Camera.Left.Y + angleCos * MyTransparentGeometry.Camera.Up.Y;
            billboardAxisY.Z = -angleSin * MyTransparentGeometry.Camera.Left.Z + angleCos * MyTransparentGeometry.Camera.Up.Z;

            //	Coordinates of four points of a billboard's quad
            billboard.Position0.X = position.X + billboardAxisX.X + billboardAxisY.X;
            billboard.Position0.Y = position.Y + billboardAxisX.Y + billboardAxisY.Y;
            billboard.Position0.Z = position.Z + billboardAxisX.Z + billboardAxisY.Z;

            billboard.Position1.X = position.X - billboardAxisX.X + billboardAxisY.X;
            billboard.Position1.Y = position.Y - billboardAxisX.Y + billboardAxisY.Y;
            billboard.Position1.Z = position.Z - billboardAxisX.Z + billboardAxisY.Z;

            billboard.Position2.X = position.X - billboardAxisX.X - billboardAxisY.X;
            billboard.Position2.Y = position.Y - billboardAxisX.Y - billboardAxisY.Y;
            billboard.Position2.Z = position.Z - billboardAxisX.Z - billboardAxisY.Z;

            billboard.Position3.X = position.X + billboardAxisX.X - billboardAxisY.X;
            billboard.Position3.Y = position.Y + billboardAxisX.Y - billboardAxisY.Y;
            billboard.Position3.Z = position.Z + billboardAxisX.Z - billboardAxisY.Z;
        }


        private static void GetBillboardQuadRotated(VRageRender.MyBillboard billboard, ref Vector3D position, Vector2 radius, ref Matrix transform)
        {
            GetBillboardQuadRotated(billboard, ref position, radius, ref transform, MyTransparentGeometry.Camera.Left, MyTransparentGeometry.Camera.Up);
        }

        private static void GetBillboardQuadRotated(VRageRender.MyBillboard billboard, ref Vector3D position, Vector2 radius, ref Matrix transform, Vector3 left, Vector3 up)
        {
            //	Two main vectors of a billboard rotated around the view axis/vector
            Vector3 billboardAxisX = new Vector3();
            billboardAxisX.X = radius.X * left.X;
            billboardAxisX.Y = radius.X * left.Y;
            billboardAxisX.Z = radius.X * left.Z;

            Vector3 billboardAxisY = new Vector3();
            billboardAxisY.X = radius.Y * up.X;
            billboardAxisY.Y = radius.Y * up.Y;
            billboardAxisY.Z = radius.Y * up.Z;

            Vector3D v1 = Vector3.TransformNormal(billboardAxisX + billboardAxisY, transform);
            Vector3D v2 = Vector3.TransformNormal(billboardAxisX - billboardAxisY, transform);

            //	Coordinates of four points of a billboard's quad
            billboard.Position0.X = position.X + v1.X;
            billboard.Position0.Y = position.Y + v1.Y;
            billboard.Position0.Z = position.Z + v1.Z;

            billboard.Position1.X = position.X - v2.X;
            billboard.Position1.Y = position.Y - v2.Y;
            billboard.Position1.Z = position.Z - v2.Z;

            billboard.Position2.X = position.X - v1.X;
            billboard.Position2.Y = position.Y - v1.Y;
            billboard.Position2.Z = position.Z - v1.Z;

            billboard.Position3.X = position.X + v2.X;
            billboard.Position3.Y = position.Y + v2.Y;
            billboard.Position3.Z = position.Z + v2.Z;

        }


        public bool IsValid()
        {
            if (Life <= 0)
            {
                return false;
            }
            return MyUtils.IsValid(StartPosition) &&
            MyUtils.IsValid(Angle) &&
            MyUtils.IsValid(Velocity) &&
            MyUtils.IsValid(m_actualPosition) &&
            MyUtils.IsValid(m_actualAngle);
        }
    }
}

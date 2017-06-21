using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Lights;
using VRageRender.Messages;

namespace Sandbox.Game.Lights
{
    public class MyLight
    {
        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()

        /// <summary>
        /// Light type, flags, could be combined
        /// </summary>
        [Flags]
        public enum LightTypeEnum
        {
            None = 0,
            PointLight = 1 << 0,
            Spotlight = 1 << 1,
            Hemisphere = 1 << 2,
        }

        public enum LightOwnerEnum
        {
            None,
            SmallShip,
            LargeShip,
            Missile
        }

        #region Static

        public static float ConeRadiansToConeMaxAngleCos(float value)
        {
            return 1 - (float)Math.Cos(value / 2);
        }

        public static float ConeDegreesToConeMaxAngleCos(float value)
        {
            return ConeRadiansToConeMaxAngleCos(MathHelper.ToRadians(value));
        }

        public static float ConeMaxAngleCosToRadians(float reflectorConeMaxAngleCos)
        {
            return (float)Math.Acos(1 - reflectorConeMaxAngleCos) * 2;
        }

        public static float ConeMaxAngleCosToDegrees(float reflectorConeMaxAngleCos)
        {
            return MathHelper.ToDegrees(ConeMaxAngleCosToRadians(reflectorConeMaxAngleCos));
        }

        #endregion

        #region Fields
        private uint m_renderObjectID = MyRenderProxy.RENDER_ID_UNASSIGNED;
        private bool m_propertiesDirty;
        private bool m_positionDirty;
        private bool m_spotParamsDirty;

        private Vector3D m_position;
        private int m_parentID = -1;
        private bool m_useInForwardRender;
        private string m_glareMaterial = "LightGlare";
        private float m_glareMaxDistance;
        private float m_glareIntensity;
        private float m_glareQuerySize;
        private float m_glareSize;
        private MyGlareTypeEnum m_glareType;
        private bool m_glareOn;

        private Color m_color = Color.White;
        private Color m_specularColor = Color.White;
        private float m_falloff;
        private float m_glossFactor;
        private float m_diffuseFactor;
        private float m_range;
        private float m_intensity;
        private bool m_lightOn;        //  If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.

        private LightTypeEnum m_lightType = LightTypeEnum.PointLight; // Type of the light
        private LightOwnerEnum m_lightOwner = LightOwnerEnum.None;

        // Reflector parameters are also parameters for spot light
        private float m_reflectorIntensity;
        private bool m_reflectorOn = false;
        private Vector3 m_reflectorDirection;
        private Vector3 m_reflectorUp;
        private float m_reflectorConeMaxAngleCos;
        private Color m_reflectorColor;
        private float m_reflectorRange;
        private float m_reflectorFalloff;
        private float m_reflectorGlossFactor;
        private float m_reflectorDiffuseFactor;
        private string m_reflectorTexture;

        private bool m_castShadows;

        private BoundingBoxD m_spotBoundingBox;
        private MatrixD m_spotWorld;

        private float m_pointLightOffset;
        private BoundingSphereD m_pointBoundingSphere;

        private float m_shadowDistance;
        private float m_glareQueryFreqMinMs;
        private float m_glareQueryFreqRndMs;

        #endregion

        #region Properties
        public LightTypeEnum LightType
        {
            get { return m_lightType; }
            set
            {
                if (m_lightType != value)
                {
                    m_lightType = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public Vector3D Position
        {
            get { return m_position; }
            set
            {
                if (Vector3D.DistanceSquared(m_position, value) > 0.0001)
                {
                    value.AssertIsValid();
                    m_position        = value;
                    m_propertiesDirty = true;
                    m_spotParamsDirty = true;
                    m_positionDirty   = true;
                    UpdatePositionWithOffset();
                    m_pointBoundingSphere.Center = PositionWithOffset;
                }
            }
        }

        public int ParentID
        {
            get { return m_parentID; }
            set
            {
                if (m_parentID != value)
                {
                    m_parentID = value;
                    m_propertiesDirty = true;
                }
            }
        }

        /// <summary>
        /// Value from 0 to 1 indication light offset in direction of reflector
        /// 0 means zero offset, 1 means radius offset
        /// </summary>
        public float PointLightOffset
        {
            get { return m_pointLightOffset; }
            set
            {
                if (m_pointLightOffset != value)
                {
                    m_pointLightOffset = value;
                    m_propertiesDirty = true;
                    UpdatePositionWithOffset();
                    m_pointBoundingSphere.Center = PositionWithOffset;
                }
            }
        }

        public Color Color
        {
            get { return m_color; }
            set
            {
                if (m_color != value)
                {
                    m_color = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public Color SpecularColor
        {
            get { return m_specularColor; }
            set
            {
                if (m_specularColor != value)
                {
                    m_specularColor = value;
                    m_propertiesDirty = true;
                }
            }
        }

        /// <summary>
        /// Exponential falloff (1 = linear, 2 = quadratic, etc)
        /// </summary>
        public float Falloff
        {
            get { return m_falloff; }
            set
            {
                if (m_falloff != value)
                {
                    m_falloff = value;
                    m_propertiesDirty = true;
                }
            }
        }
        public float GlossFactor
        {
            get { return m_glossFactor; }
            set
            {
                if (m_glossFactor != value)
                {
                    m_glossFactor = value;
                    m_propertiesDirty = true;
                }
            }
        }
        public float DiffuseFactor
        {
            get { return m_diffuseFactor; }
            set
            {
                if (m_diffuseFactor != value)
                {
                    m_diffuseFactor = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float Range
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_range > 0, "Point light range not set");
                return m_range;
            }
            set
            {
                if (m_range != value)
                {
                    //Debug.Assert(value > 0, "Cannot set zero point light range");
                    if (value <= 0) value = 0.5f;
                    Debug.Assert(value <= MyLightsConstants.MAX_POINTLIGHT_RADIUS, "Cannot set point light range bigger than MyLightsConstants.MAX_POINTLIGHT_RADIUS");

                    m_range = value;
                    m_pointBoundingSphere.Radius = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float Intensity
        {
            get { return m_intensity; }
            set
            {
                if (m_intensity != value)
                {
                    m_intensity = value;
                    m_propertiesDirty = true;
                }
            }
        }

        /// <summary>
        /// If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.
        /// </summary>
        public bool LightOn
        {
            get { return m_lightOn; }
            set
            {
                if (m_lightOn != value)
                {
                    m_lightOn = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public bool UseInForwardRender
        {
            get { return m_useInForwardRender; }
            set
            {
                if (m_useInForwardRender != value)
                {
                    m_useInForwardRender = value;
                    m_propertiesDirty = true;
                }
            }
        }

        /// <summary>
        /// Reflector parameters are also parameters for spot light
        /// </summary>
        public float ReflectorIntensity
        {
            get { return m_reflectorIntensity; }
            set
            {
                if (m_reflectorIntensity != value)
                {
                    m_reflectorIntensity = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public bool ReflectorOn
        {
            get { return m_reflectorOn; }
            set
            {
                if (m_reflectorOn != value)
                {
                    m_reflectorOn = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public Vector3 ReflectorDirection
        {
            get { return m_reflectorDirection; }
            set
            {
                if (Vector3.DistanceSquared(m_reflectorDirection, value) > 0.00001f)
                {
                    m_reflectorDirection = value;
                    m_spotParamsDirty = true;
                    m_propertiesDirty = true;
                }
            }
        }

        public Vector3 ReflectorUp
        {
            get { return m_reflectorUp; }
            set
            {
                if (Vector3.DistanceSquared(m_reflectorUp, value) > 0.00001f)
                {
                    m_reflectorUp = MyUtils.Normalize(value);
                    m_spotParamsDirty = true;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ReflectorConeMaxAngleCos
        {
            get { return m_reflectorConeMaxAngleCos; }
            set
            {
                System.Diagnostics.Debug.Assert(SpotlightNotTooLarge(value, m_reflectorRange), "Spot light is too large, reduce range or cone angle, AABB diagonal size must be smaller than 2500m");
                if (m_reflectorConeMaxAngleCos != value)
                {
                    m_reflectorConeMaxAngleCos = value;
                    m_spotParamsDirty = true;
                    m_propertiesDirty = true;
                }
            }
        }

        public Color ReflectorColor
        {
            get { return m_reflectorColor; }
            set
            {
                if (m_reflectorColor != value)
                {
                    m_reflectorColor = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ReflectorRange
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_reflectorRange > 0, "Spot light range not set");
                return m_reflectorRange;
            }
            set
            {
                System.Diagnostics.Debug.Assert(value > 0, "Cannot set spot light range to zero");
                System.Diagnostics.Debug.Assert(SpotlightNotTooLarge(m_reflectorConeMaxAngleCos, value), "Spot light is too large, reduce range or cone angle, AABB diagonal size must be smaller than 2500m");

                if (m_reflectorRange != value)
                {
                    m_reflectorRange = value;
                    m_spotParamsDirty = true;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ReflectorFalloff
        {
            get
            {
                return m_reflectorFalloff;
            }
            set
            {
                if (m_reflectorFalloff != value)
                {
                    m_reflectorFalloff = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ReflectorGlossFactor
        {
            get
            {
                return m_reflectorGlossFactor;
            }
            set
            {
                if (m_reflectorGlossFactor != value)
                {
                    m_reflectorGlossFactor = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ReflectorDiffuseFactor
        {
            get
            {
                return m_reflectorDiffuseFactor;
            }
            set
            {
                if (m_reflectorDiffuseFactor != value)
                {
                    m_reflectorDiffuseFactor = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public string ReflectorTexture
        {
            get { return m_reflectorTexture; }
            set
            {
                if (m_reflectorTexture != value)
                {
                    m_reflectorTexture = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float ShadowDistance
        {
            get { return m_shadowDistance; }
            set
            {
                if (m_shadowDistance != value)
                {
                    m_shadowDistance = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public bool CastShadows
        {
            get { return m_castShadows; }
            set
            {
                if (m_castShadows != value)
                {
                    m_castShadows = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public bool GlareOn
        {
            get { return m_glareOn; }
            set
            {
                if (m_glareOn != value)
                {
                    m_glareOn = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public MyGlareTypeEnum GlareType
        {
            get { return m_glareType; }
            set
            {
                if (m_glareType != value)
                {
                    m_glareType = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareSize
        {
            get { return m_glareSize; }
            set
            {
                if (m_glareSize != value)
                {
                    m_glareSize = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareQuerySize
        {
            get { return m_glareQuerySize; }
            set
            {
                if (m_glareQuerySize != value)
                {
                    m_glareQuerySize = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareQueryFreqMinMs
        {
            get { return m_glareQueryFreqMinMs; }
            set
            {
                if (m_glareQueryFreqMinMs != value)
                {
                    m_glareQueryFreqMinMs = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareQueryFreqRndMs
        {
            get { return m_glareQueryFreqRndMs; }
            set
            {
                if (m_glareQueryFreqRndMs != value)
                {
                    m_glareQueryFreqRndMs = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareIntensity
        {
            get { return m_glareIntensity; }
            set
            {
                if (m_glareIntensity != value)
                {
                    m_glareIntensity = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public string GlareMaterial
        {
            get { return m_glareMaterial; }
            set
            {
                if (m_glareMaterial != value)
                {
                    m_glareMaterial = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public float GlareMaxDistance
        {
            get { return m_glareMaxDistance; }
            set
            {
                if (m_glareMaxDistance != value)
                {
                    m_glareMaxDistance = value;
                    m_propertiesDirty = true;
                }
            }
        }

        public Vector3D PositionWithOffset { get; private set; }

        // Should be updated only from inside
        public BoundingSphereD PointBoundingSphere { get { return m_pointBoundingSphere; } }

        public BoundingBoxD SpotBoundingBox
        {
            get
            {
                UpdateSpotParams();
                return m_spotBoundingBox;
            }
        }

        public MatrixD SpotWorld
        {
            get
            {
                UpdateSpotParams();
                return m_spotWorld;
            }
        }

        /// <summary>
        /// Sets reflector cone angle in degrees, minimum is 0, teoretical maximum is PI
        /// </summary>
        public float ReflectorConeRadians
        {
            get
            {
                return ConeMaxAngleCosToRadians(ReflectorConeMaxAngleCos);
            }
            set
            {
                ReflectorConeMaxAngleCos = ConeRadiansToConeMaxAngleCos(value);
            }
        }

        /// <summary>
        /// Sets reflector cone angle in degrees, minimum is 0, teoretical maximum is 180
        /// </summary>
        public float ReflectorConeDegrees
        {
            get
            {
                return ConeMaxAngleCosToDegrees(ReflectorConeMaxAngleCos);
            }
            set
            {
                ReflectorConeMaxAngleCos = ConeDegreesToConeMaxAngleCos(value);
            }
        }

        public bool IsTypePoint
        {
            get
            {
                return (LightType & MyLight.LightTypeEnum.PointLight) != 0 && !IsTypeHemisphere;
            }
        }

        public bool IsTypeHemisphere
        {
            get
            {
                return (LightType & MyLight.LightTypeEnum.Hemisphere) != 0;
            }
        }

        public bool IsTypeSpot
        {
            get
            {
                return (LightType & MyLight.LightTypeEnum.Spotlight) != 0;
            }
        }

        public LightOwnerEnum LightOwner
        {
            get { return m_lightOwner; }
            set { m_lightOwner = value; }
        }

        public uint RenderObjectID
        {
            get { return m_renderObjectID; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        /// So don't initialize members here, do it in Start()
        /// </summary>
        public MyLight()
        {
        }

        public void Start(LightTypeEnum lightType, Vector3 position, Vector4 color, float falloff, float range)
        {
            Start(lightType, color, falloff, range);
            Position = position;
        }

        public void Start(LightTypeEnum lightType, Vector4 color, float falloff, float range)
        {
            Start(lightType, falloff);

            Color = color;
            Range = range;
        }

        public void Start(LightTypeEnum lightType, float falloff)
        {
            LightType = lightType;

            if ((lightType & MyLight.LightTypeEnum.PointLight) != 0)
            {
                Falloff = falloff;
            }

            if ((lightType & MyLight.LightTypeEnum.Hemisphere) != 0)
            {
                Falloff = falloff;
            }

            if ((lightType & MyLight.LightTypeEnum.Spotlight) != 0)
            {
                ReflectorFalloff = falloff;
                ReflectorOn = true;
            }

            LightOwner = LightOwnerEnum.None;

            Start();
        }

        #endregion

        public void UpdateLight()
        {
            UpdateSpotParams();

            if (m_propertiesDirty || m_positionDirty)
            {
                ProfilerShort.Begin("UpdateRenderLight");

                m_propertiesDirty = m_positionDirty = false;

                MyLightLayout pointLight = new MyLightLayout()
                {
                    Range = Range,
                    Color = Color,
                    Falloff = Falloff,
                    GlossFactor = GlossFactor,
                    DiffuseFactor = DiffuseFactor,
                };
                
                MySpotLightLayout spotLight = new MySpotLightLayout()
                {
                    Light = new MyLightLayout()
                    {
                        Range = ReflectorRange,
                        Color = ReflectorColor,
                        Falloff = ReflectorFalloff,
                        GlossFactor = ReflectorGlossFactor,
                        DiffuseFactor = ReflectorDiffuseFactor,
                    },
                    Up = ReflectorUp,
                    Direction = ReflectorDirection,
                };

                MyFlareDesc glare = new MyFlareDesc()
                {
                    Enabled = GlareOn,
                    Direction = ReflectorDirection,
                    Range = Range,
                    Color = Color,
                    Type = GlareType,
                    Size = GlareSize,
                    QuerySize = GlareQuerySize,
                    QueryFreqMinMs = GlareQueryFreqMinMs,
                    QueryFreqRndMs = GlareQueryFreqRndMs,
                    Intensity = GlareIntensity,
                    Material = MyStringId.GetOrCompute(GlareMaterial),
                    MaxDistance = GlareMaxDistance,
                    ParentGID = ParentID,
                };

                UpdateRenderLightData renderLightData = new UpdateRenderLightData()
                {
                    ID = RenderObjectID,
                    Position = Position,
                    Type = (VRageRender.LightTypeEnum)(int)LightType,
                    ParentID = ParentID,
                    PointPositionOffset = PointLightOffset,
                    SpecularColor = SpecularColor,
                    UseInForwardRender = UseInForwardRender,
                    ReflectorConeMaxAngleCos = ReflectorConeMaxAngleCos,
                    ShadowDistance = ShadowDistance,
                    CastShadows = CastShadows,
                    PointLightOn = LightOn,
                    PointLightIntensity = Intensity,
                    PointLight = pointLight,
                    SpotLightOn = ReflectorOn,
                    SpotLightIntensity = ReflectorIntensity,
                    SpotLight = spotLight,
                    ReflectorTexture = ReflectorTexture,
                    Glare = glare,
                };

                MyRenderProxy.UpdateRenderLight(ref renderLightData);

                ProfilerShort.End();
            }
        }

        /// <summary>
        /// Can be called only from MyLights.RemoveLight.
        /// </summary>
        public void Clear()
        {
            VRageRender.MyRenderProxy.RemoveRenderObject(RenderObjectID);
            m_renderObjectID = MyRenderProxy.RENDER_ID_UNASSIGNED;
        }

        public void MarkPositionDirty()
        {
            m_positionDirty = true;
        }

        public void MarkPropertiesDirty()
        {
            m_propertiesDirty = true;
        }

        public bool IsPointLightInFrustum()
        {
            return MySector.MainCamera.IsInFrustum(ref m_pointBoundingSphere);
        }

        public bool IsSpotLightInFrustum()
        {
            UpdateSpotParams();
            return MySector.MainCamera.IsInFrustum(ref m_spotBoundingBox);
        }

        /// <summary>
        /// When setting Reflector properties, use this function to test whether properties are in bounds and light AABB is not too large.
        /// Properties which affects calculations are ReflectorRange and ReflectorConeMaxAngleCos (ReflectorConeDegrees, ReflectorConeRadians)
        /// </summary>
        /// <returns></returns>
        public bool SpotlightNotTooLarge(float reflectorConeMaxAngleCos, float reflectorRange)
        {
            return reflectorConeMaxAngleCos <= MyLightsConstants.MAX_SPOTLIGHT_ANGLE_COS && reflectorRange <= MyLightsConstants.MAX_SPOTLIGHT_RANGE;

            //BoundingBox bbox = new BoundingBox();
            //float scaleXY, scaleZ;

            //// Up and direction are both used worst possible
            //Vector3 direction = MyMwcUtils.Normalize(new Vector3(1, 1, 1));
            //Vector3 help = new Vector3(1, 0, 0);
            //Vector3 up = MyMwcUtils.Normalize(Vector3.Cross(direction, help));

            //CalculateAABB(ref bbox, out scaleXY, out scaleZ, direction, up, reflectorConeMaxAngleCos, reflectorRange);
            //return TestAABB(ref bbox);
        }

        /// <summary>
        /// Use when setting both values and previous state of both value is undefined
        /// </summary>
        /// <param name="reflectorConeMaxAngleCos"></param>
        /// <param name="range"></param>
        public void UpdateReflectorRangeAndAngle(float reflectorConeMaxAngleCos, float reflectorRange)
        {
            Debug.Assert(SpotlightNotTooLarge(reflectorConeMaxAngleCos, reflectorRange));
            m_reflectorRange = reflectorRange;
            m_reflectorConeMaxAngleCos = reflectorConeMaxAngleCos;
        }

        /// <summary>
        /// IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        /// </summary>
        private void Start()
        {
            m_positionDirty   = true;
            m_propertiesDirty = true;
            m_spotParamsDirty = true;

            /// todo: defaults should be supplied from Environemnt.sbc
            ReflectorRange     = 1;
            ReflectorUp        = Vector3.Up;
            ReflectorDirection = Vector3.Forward;
            ReflectorGlossFactor = 1.0f;
            ReflectorDiffuseFactor = 3.14f;
            LightOn            = true;
            Intensity          = 1.0f;
            UseInForwardRender = false;
            GlareOn            = false;
            GlareQueryFreqMinMs = 150;
            GlareQueryFreqRndMs = 100;
            PointLightOffset   = 0;
            CastShadows        = true;
            Range              = 0.5f;
            GlossFactor        = 1.0f;
            DiffuseFactor      = 3.14f;
            ShadowDistance     = MyLightsConstants.MAX_SPOTLIGHT_SHADOW_RANGE;
            ParentID           = -1;

            m_renderObjectID = VRageRender.MyRenderProxy.CreateRenderLight();
            UpdateLight();
        }

        private void UpdateSpotParams()
        {
            if (!m_spotParamsDirty)
                return;
            if (ReflectorConeMaxAngleCos == 0)
                return;

            ProfilerShort.Begin("UpdateSpotParams");
            m_spotParamsDirty = false;
            float scaleZ, scaleXY;
            Vector3D position = Position;
            CalculateAABB(ref m_spotBoundingBox, out scaleZ, out scaleXY, position, m_reflectorDirection, m_reflectorUp, ReflectorConeMaxAngleCos, ReflectorRange);
            m_spotWorld = MatrixD.CreateScale(scaleXY, scaleXY, scaleZ) * MatrixD.CreateWorld(Position, ReflectorDirection, ReflectorUp);
            ProfilerShort.End();
        }

        private void UpdatePositionWithOffset()
        {
            PositionWithOffset = Position + ReflectorDirection * Range * PointLightOffset;
        }

        private static void CalculateAABB(ref BoundingBoxD bbox, out float scaleZ, out float scaleXY, Vector3D position, Vector3 direction, Vector3 up, float reflectorConeMaxAngleCos, float reflectorRange)
        {
            float cosAngle = 1 - reflectorConeMaxAngleCos;
            scaleZ = reflectorRange;
            // Calculate cone side (hypotenuse of triangle)
            float side = reflectorRange / cosAngle;
            // Calculate cone bottom scale (Pythagoras theorem)
            scaleXY = (float)System.Math.Sqrt(side * side - reflectorRange * reflectorRange) * 2;

            Vector3 coneSideDirection = Vector3.Cross(up, direction);
            coneSideDirection = MyUtils.Normalize(coneSideDirection);
            
            Vector3D coneCenter = position + direction * scaleZ;
            Vector3D pt1 = coneCenter + coneSideDirection * scaleXY / 2 + up * scaleXY / 2;
            Vector3D pt2 = coneCenter - coneSideDirection * scaleXY / 2 + up * scaleXY / 2;
            Vector3D pt3 = coneCenter + coneSideDirection * scaleXY / 2 - up * scaleXY / 2;
            Vector3D pt4 = coneCenter - coneSideDirection * scaleXY / 2 - up * scaleXY / 2;

            bbox = BoundingBoxD.CreateInvalid();
            bbox = bbox.Include(ref position);
            //bbox = bbox.Include(ref coneCenter);
            bbox = bbox.Include(ref pt1);
            bbox = bbox.Include(ref pt2);
            bbox = bbox.Include(ref pt3);
            bbox = bbox.Include(ref pt4);
        }
    }
}

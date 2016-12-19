using System;
using System.Xml.Serialization;
using VRage;

namespace VRageRender
{
    public class MyShadowsSettings
    {
        float[] m_shadowCascadeSmallSkipThresholds;
        bool[] m_shadowCascadeFrozen;

        [XmlElement(Type = typeof(MyStructXmlSerializer<Struct>))]
        public Struct Data = Struct.Default;

        public MyShadowsSettings()
        {
            m_shadowCascadeSmallSkipThresholds = new float[] { 1000, 5000, 200, 1000, 1000, 1000 };
            m_shadowCascadeFrozen = new bool[6];
            m_cascades = new Cascade[8];

            // no stabilization, csm resolution 2048, hard shadows:
            float baseCoef = 5.0f;
            float cascadePow = 5.0f;
            float extMult = 2;
            float snoPart = 758.0f;
            float skippingSmallObjectsThreshold = 0.0f;
            for (int i = 0; i < 8; i++)
            {
                float cascadeDepth = baseCoef * (float)Math.Pow(cascadePow, i);
                m_cascades[i].FullCoverageDepth = cascadeDepth;
                m_cascades[i].ExtendedCoverageDepth = cascadeDepth * extMult;
                m_cascades[i].ShadowNormalOffset = (cascadeDepth + extMult) / snoPart;
                m_cascades[i].SkippingSmallObjectThreshold = skippingSmallObjectsThreshold;
            };
        }

        [XmlArrayItem("Value")]
        public float[] ShadowCascadeSmallSkipThresholds
        {
            get { return m_shadowCascadeSmallSkipThresholds; }
            set
            {
                if (ShadowCascadeSmallSkipThresholds.Length != value.Length)
                    ShadowCascadeSmallSkipThresholds = new float[value.Length];
                value.CopyTo(ShadowCascadeSmallSkipThresholds, 0);
            }
        }

        [XmlIgnore]
        public bool[] ShadowCascadeFrozen
        {
            get { return m_shadowCascadeFrozen; }
            set
            {
                if (ShadowCascadeFrozen.Length != value.Length)
                    ShadowCascadeFrozen = new bool[value.Length];
                value.CopyTo(ShadowCascadeFrozen, 0);
            }
        }

        [XmlType("MyShadowSettings.Struct")]
        public struct Struct
        {
            [StructDefault]
            public static readonly Struct Default;

            static Struct()
            {
                Default = new Struct()
                {
                    ShowShadowCascadeSplits = false,
                    UpdateCascadesEveryFrame = false,
                    EnableShadowBlur = true,
                    ShadowCascadeMaxDistance = 300.0f,
                    ShadowCascadeMaxDistanceMultiplierMedium = 2f,
                    ShadowCascadeMaxDistanceMultiplierHigh = 3.5f,
                    ShadowCascadeSpreadFactor = 0.5f,
                    ShadowCascadeZOffset = 400,
                    DisplayFrozenShadowCascade = false,
                };
            }

            public bool ShowShadowCascadeSplits;
            public bool UpdateCascadesEveryFrame;
            public bool EnableShadowBlur;
            public float ShadowCascadeMaxDistance;
            public float ShadowCascadeMaxDistanceMultiplierMedium;
            public float ShadowCascadeMaxDistanceMultiplierHigh;
            public float ShadowCascadeSpreadFactor;
            public float ShadowCascadeZOffset;
            public bool DisplayFrozenShadowCascade;
        }


        public void CopyFrom(MyShadowsSettings settings)
        {
            //ShadowCascadeSmallSkipThresholds = settings.ShadowCascadeSmallSkipThresholds;
            //ShadowCascadeFrozen = settings.ShadowCascadeFrozen;
            Data = settings.Data;
            NewData = settings.NewData;
            Cascades = settings.Cascades;
        }

        //[XmlElement(Type = typeof(MyStructXmlSerializer<Cascade>))]

        [XmlType("MyShadowSettings.Cascade")]
        public struct Cascade
        {
            public float FullCoverageDepth;
            public float ExtendedCoverageDepth;
            public float ShadowNormalOffset;
            public float SkippingSmallObjectThreshold;
        };

        Cascade[] m_cascades;

        [XmlArrayItem("Cascade")]
        public Cascade[] Cascades
        {
            get { return m_cascades; }
            set
            {
                if (m_cascades.Length != value.Length)
                    m_cascades = new Cascade[value.Length];
                value.CopyTo(m_cascades, 0);
            }
        }


        [XmlElement(Type = typeof(MyStructXmlSerializer<NewStruct>))]
        public NewStruct NewData = NewStruct.Default;

        [XmlType("MyShadowSettings.NewStruct")]
        public struct NewStruct
        {
            [StructDefault]
            public static readonly NewStruct Default;

            static NewStruct()
            {
                Default = new NewStruct()
                {
                    StabilizeMovement = true,
                    StabilizeRotation = false,
                    DrawVolumes = false,
                    FreezeSunDirection = false,
                    FreezeShadowVolumePositions = false,
                    FreezeShadowMaps = false,
                    DisplayCascadeCoverage = false,
                    DisplayHardShadows = false,
                    DisplaySimpleShadows = false,
                    EnableFXAAOnShadows = false,
                    SunAngleThreshold = 0.15f,
                    ZOffset = 20000.0f,
                    CascadesCount = 6,
                };
            }

            public bool StabilizeMovement;
            public bool StabilizeRotation;
            public bool DrawVolumes;
            public bool FreezeSunDirection;
            public bool FreezeShadowVolumePositions;
            public bool FreezeShadowMaps;
            public bool DisplayCascadeCoverage;
            public bool DisplayHardShadows;
            public bool DisplaySimpleShadows;
            public bool EnableFXAAOnShadows;
            public float SunAngleThreshold;
            public float ZOffset;
            public int CascadesCount;
        }
    }
}

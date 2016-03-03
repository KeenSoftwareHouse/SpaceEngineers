using System;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using VRage.Utils;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VoxelMaterialDefinition))]
    public class MyVoxelMaterialDefinition : MyDefinitionBase
    {
        private static byte m_indexCounter;

		public string MaterialTypeName;
        public string MinedOre;
        public float MinedOreRatio;
        public bool CanBeHarvested;
        public bool IsRare;
        public float DamageRatio;
        public int MinVersion;
        public bool SpawnsInAsteroids;
        public bool SpawnsFromMeteorites;
        
        public string DiffuseXZ;
        public string NormalXZ;
        public string DiffuseY;
        public string NormalY;
        public MyParticleEffectsIDEnum ParticleEffect;
        public float SpecularPower;
        public float SpecularShininess;

        public int DamageThreshold;

        public MyStringHash DamagedMaterial;

        private int m_damagedMaterialId = -1;
        public byte DamagedMaterialId
        {
            get
            {
                if (m_damagedMaterialId == -1)
                {
                    var mat = MyDefinitionManager.Static.GetVoxelMaterialDefinition(DamagedMaterial.ToString());
                    m_damagedMaterialId = mat != null ? mat.Index : 255;
                }

                return (byte)m_damagedMaterialId;
            }
        }

        /// <summary>
        /// Value generated at runtime to ensure correctness. Do not serialize or deserialize.
        /// This is what the old cast to int used to result into, but now numbers depend on order in XML file.
        /// TODO Serialize to XML and ensure upon loading that these values are starting from 0 and continuous.
        /// </summary>
        public byte Index
        {
            get;
            private set;
        }

        public bool HasDamageMaterial { get { return DamagedMaterial != MyStringHash.NullOrEmpty; } }

        public void AssignIndex()
        {
            // We can't have more than 256 materials, since voxel files store these materials as byte values.
            Debug.Assert(m_indexCounter < 255, "Too many voxel materials.");
            Index = m_indexCounter++;
        }

        public static void ResetIndexing()
        {
            m_indexCounter = 0;
        }

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            var builder = ob as MyObjectBuilder_VoxelMaterialDefinition;
            MyDebug.AssertDebug(builder != null);

			MaterialTypeName	   = builder.MaterialTypeName;
            MinedOre               = builder.MinedOre;
            MinedOreRatio          = builder.MinedOreRatio;
            CanBeHarvested         = builder.CanBeHarvested;
            IsRare                 = builder.IsRare;
            SpawnsInAsteroids      = builder.SpawnsInAsteroids;
            SpawnsFromMeteorites   = builder.SpawnsFromMeteorites;
            DamageRatio            = builder.DamageRatio;
            DiffuseXZ              = builder.DiffuseXZ;
            DiffuseY               = builder.DiffuseY;
            NormalXZ               = builder.NormalXZ;
            NormalY                = builder.NormalY;
            SpecularPower          = builder.SpecularPower;
            SpecularShininess      = builder.SpecularShininess;
            MinVersion             = builder.MinVersion;
            if (!string.IsNullOrEmpty(builder.ParticleEffect))
            {
                ParticleEffect = (MyParticleEffectsIDEnum)Enum.Parse(typeof(MyParticleEffectsIDEnum), builder.ParticleEffect);
            }
            else
            {
                ParticleEffect = MyParticleEffectsIDEnum.None;
            }
            DamageThreshold = (int) (builder.DamageThreashold*255);
            DamagedMaterial = MyStringHash.GetOrCompute(builder.DamagedMaterial);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            MyObjectBuilder_VoxelMaterialDefinition ob = (MyObjectBuilder_VoxelMaterialDefinition)base.GetObjectBuilder();

			ob.MaterialTypeName			= MaterialTypeName;
            ob.MinedOre                 = MinedOre;
            ob.MinedOreRatio            = MinedOreRatio;
            ob.CanBeHarvested           = CanBeHarvested;
            ob.IsRare                   = IsRare;
            ob.SpawnsInAsteroids        = SpawnsInAsteroids;
            ob.SpawnsFromMeteorites     = SpawnsFromMeteorites;
            ob.DamageRatio              = DamageRatio;
            ob.DiffuseXZ                = DiffuseXZ;
            ob.DiffuseY                 = DiffuseY;
            ob.NormalXZ                 = NormalXZ;
            ob.NormalY                  = NormalY;
            ob.SpecularPower            = SpecularPower;
            ob.SpecularShininess        = SpecularShininess;
            ob.ParticleEffect           = ParticleEffect.ToString();
            ob.DamagedMaterial          = DamagedMaterial.ToString();
            ob.DamageThreashold         = DamageThreshold / 255f;

            return ob;
        }

        public virtual void CreateRenderData(out MyRenderVoxelMaterialData renderData)
        {
            renderData = new MyRenderVoxelMaterialData()
            {
                Index             = Index,
                DiffuseXZ         = DiffuseXZ,
                NormalXZ          = NormalXZ,
                DiffuseY          = DiffuseY,
                NormalY           = NormalY,
                SpecularPower     = SpecularPower,
                SpecularShininess = SpecularShininess
            };
        }
    }
}

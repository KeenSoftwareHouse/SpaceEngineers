using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VoxelMaterialDefinition))]
    public class MyVoxelMaterialDefinition : MyDefinitionBase
    {
        private static byte m_indexCounter;

        public string MinedOre;
        public float MinedOreRatio;
        public bool CanBeHarvested;
        public bool IsRare;
        public float DamageRatio;
        public int MinVersion;
        public bool SpawnsInAsteroids;
        public bool SpawnsFromMeteorites;
        public bool SpawnsFlora;

        public string DiffuseXZ;
        public string NormalXZ;
        public string DiffuseY;
        public string NormalY;
        public float SpecularPower;
        public float SpecularShininess;


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

            this.MinedOre               = builder.MinedOre;
            this.MinedOreRatio          = builder.MinedOreRatio;
            this.CanBeHarvested         = builder.CanBeHarvested;
            this.IsRare                 = builder.IsRare;
            this.SpawnsInAsteroids      = builder.SpawnsInAsteroids;
            this.SpawnsFromMeteorites   = builder.SpawnsFromMeteorites;
            this.DamageRatio            = builder.DamageRatio;
            this.DiffuseXZ              = builder.DiffuseXZ;
            this.DiffuseY               = builder.DiffuseY;
            this.NormalXZ               = builder.NormalXZ;
            this.NormalY                = builder.NormalY;
            this.SpecularPower          = builder.SpecularPower;
            this.SpecularShininess      = builder.SpecularShininess;
            this.MinVersion             = builder.MinVersion;
            this.SpawnsFlora            = builder.SpawnsFlora;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            MyObjectBuilder_VoxelMaterialDefinition ob = (MyObjectBuilder_VoxelMaterialDefinition)base.GetObjectBuilder();

            ob.MinedOre                 = this.MinedOre;
            ob.MinedOreRatio            = this.MinedOreRatio;
            ob.CanBeHarvested           = this.CanBeHarvested;
            ob.IsRare                   = this.IsRare;
            ob.SpawnsInAsteroids        = this.SpawnsInAsteroids;
            ob.SpawnsFromMeteorites     = this.SpawnsFromMeteorites;
            ob.DamageRatio              = this.DamageRatio;
            ob.DiffuseXZ                = this.DiffuseXZ;
            ob.DiffuseY                 = this.DiffuseY;
            ob.NormalXZ                 = this.NormalXZ;
            ob.NormalY                  = this.NormalY;
            ob.SpecularPower            = this.SpecularPower;
            ob.SpecularShininess        = this.SpecularShininess;
            ob.SpawnsFlora              = this.SpawnsFlora;

            return ob;
        }

        public virtual void CreateRenderData(out MyRenderVoxelMaterialData renderData)
        {
            renderData = new MyRenderVoxelMaterialData()
            {
                Index             = this.Index,
                DiffuseXZ         = this.DiffuseXZ,
                NormalXZ          = this.NormalXZ,
                DiffuseY          = this.DiffuseY,
                NormalY           = this.NormalY,
                SpecularPower     = this.SpecularPower,
                SpecularShininess = this.SpecularShininess
            };
        }
    }
}

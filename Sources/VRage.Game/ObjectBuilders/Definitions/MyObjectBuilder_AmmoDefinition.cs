using ProtoBuf;
using VRage.ObjectBuilders;
using System.ComponentModel;
using VRage.Utils;

namespace VRage.Game
{
    public enum MyAmmoType
    {
        Unknown = -1,
        HighSpeed = 0,
        Missile,
        Laser,
        Plasma,
        Basic,
    }

    public static class MyDamageType 
    {
        public static MyStringHash Unknown       = MyStringHash.GetOrCompute("Unknown");
        public static MyStringHash Explosion     = MyStringHash.GetOrCompute("Explosion");
        public static MyStringHash Rocket        = MyStringHash.GetOrCompute("Rocket");
        public static MyStringHash Bullet        = MyStringHash.GetOrCompute("Bullet");
        public static MyStringHash Mine          = MyStringHash.GetOrCompute("Mine");
        public static MyStringHash Environment   = MyStringHash.GetOrCompute("Environment");
        public static MyStringHash Drill         = MyStringHash.GetOrCompute("Drill");
        public static MyStringHash Radioactivity = MyStringHash.GetOrCompute("Radioactivity");
        public static MyStringHash Deformation   = MyStringHash.GetOrCompute("Deformation");
        public static MyStringHash Suicide       = MyStringHash.GetOrCompute("Suicide");
        public static MyStringHash Fall          = MyStringHash.GetOrCompute("Fall");
        public static MyStringHash Weapon        = MyStringHash.GetOrCompute("Weapon");
        public static MyStringHash Fire          = MyStringHash.GetOrCompute("Fire");
        public static MyStringHash Squeez        = MyStringHash.GetOrCompute("Squeez");
        public static MyStringHash Grind         = MyStringHash.GetOrCompute("Grind");
        public static MyStringHash Weld          = MyStringHash.GetOrCompute("Weld");
        public static MyStringHash Asphyxia      = MyStringHash.GetOrCompute("Asphyxia");
        public static MyStringHash LowPressure   = MyStringHash.GetOrCompute("LowPressure");
        public static MyStringHash Bolt          = MyStringHash.GetOrCompute("Bolt");
        public static MyStringHash Destruction   = MyStringHash.GetOrCompute("Destruction");
    }

    public enum MyCustomHitMaterialMethodType
    {
        None = -2,
        Unknown = -1,
        Small = 0,
        Normal,
    }

    public enum MyCustomHitParticlesMethodType
    {
        None = -2,
        Unknown = -1,
        Biochem = 0,
        EMP,
        Basic,
        BasicSmall,
        Piercing,
        Explosive,
        HighSpeed
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AmmoDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class AmmoBasicProperties
        {
            [ProtoMember]
            public float DesiredSpeed;
            [ProtoMember]
            public float SpeedVariance;
            [ProtoMember]
            public float MaxTrajectory;
            [ProtoMember, DefaultValue(false)]
            public bool IsExplosive;
            [ProtoMember, DefaultValue(0.0f)]
            public float BackkickForce;
            [ProtoMember]
            public string PhysicalMaterial = "";
        }

        [ProtoMember]
        public AmmoBasicProperties BasicProperties;
    }
}
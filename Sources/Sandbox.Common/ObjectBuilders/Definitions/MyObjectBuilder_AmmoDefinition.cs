using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
     
    public enum MyDamageType
    {
        Unknown,
        Explosion,
        Rocket,
        Bullet,
        Mine,
        Environment,
        Drill,
        Radioactivity,
        Deformation,
        Suicide,
        Fall,
        Weapon,
        Fire,
        Squeez
    }

    public enum MyCustomHitMaterialMethodType
    {
        Unknown = -1,
        Small = 0,
        Normal,
    }

    public enum MyCustomHitParticlesMethodType
    {
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
        }

        [ProtoMember]
        public AmmoBasicProperties BasicProperties;
    }
}

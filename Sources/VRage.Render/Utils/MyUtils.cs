using System.Xml;
using VRageRender.Animations;

namespace VRageRender.Utils
{
    public static class MyTransparentMaterialInterpolator
    {
        public static void Switch(ref MyTransparentMaterial val1, ref MyTransparentMaterial val2, float time,
            out MyTransparentMaterial value)
        {
            value = time < 0.5f ? val1 : val2;
        }
    }

    public class MyAnimatedProperty2DTransparentMaterial :
        MyAnimatedProperty2D<MyAnimatedPropertyTransparentMaterial, MyTransparentMaterial, int>
    {
        public MyAnimatedProperty2DTransparentMaterial(string name)
            : this(name, null)
        {
        }

        public MyAnimatedProperty2DTransparentMaterial(string name,
            MyAnimatedProperty<MyTransparentMaterial>.InterpolatorDelegate interpolator)
            : base(name, interpolator)
        {
        }

        public override string ValueType
        {
            get
            {
                return "String";
            }
        }

        public override string BaseValueType
        {
            get
            {
                return "MyTransparentMaterial";
            }
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            MyAnimatedPropertyTransparentMaterial prop = new MyAnimatedPropertyTransparentMaterial(this.Name,
                m_interpolator2);
            prop.Deserialize(reader);
            value = prop;
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedProperty2DTransparentMaterial prop = new MyAnimatedProperty2DTransparentMaterial(Name);
            Duplicate(prop);
            return prop;
        }

        public override void ApplyVariance(ref MyTransparentMaterial interpolatedValue, ref int variance,
            float multiplier, out MyTransparentMaterial value)
        {
            value = interpolatedValue;
        }
    }

    public class MyAnimatedPropertyTransparentMaterial : MyAnimatedProperty<MyTransparentMaterial>
    {
        public MyAnimatedPropertyTransparentMaterial()
        {
        }

        public MyAnimatedPropertyTransparentMaterial(string name)
            : this(name, null)
        {
        }

        public MyAnimatedPropertyTransparentMaterial(string name, InterpolatorDelegate interpolator)
            : base(name, false, interpolator)
        {
        }

        public override string ValueType
        {
            get
            {
                return "String";
            }
        }

        public override string BaseValueType
        {
            get
            {
                return "MyTransparentMaterial";
            }
        }

        protected override void Init()
        {
            Interpolator = MyTransparentMaterialInterpolator.Switch;
            base.Init();
        }

        public override IMyConstProperty Duplicate()
        {
            MyAnimatedPropertyTransparentMaterial prop = new MyAnimatedPropertyTransparentMaterial(Name);
            Duplicate(prop);
            return prop;
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((MyTransparentMaterial) value).Name);
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = MyTransparentMaterials.GetMaterial((string) value);
        }

        protected override bool EqualsValues(object value1, object value2)
        {
            return ((MyTransparentMaterial) value1).Name == ((MyTransparentMaterial) value2).Name;
        }
    }

    public class MyConstPropertyTransparentMaterial : MyConstProperty<MyTransparentMaterial>
    {
        public MyConstPropertyTransparentMaterial()
        {
        }

        public MyConstPropertyTransparentMaterial(string name)
            : base(name)
        {
        }

        protected override void Init()
        {
            base.Init();
        }

        public override string ValueType
        {
            get
            {
                return "String";
            }
        }

        public override string BaseValueType
        {
            get
            {
                return "MyTransparentMaterial";
            }
        }

        public override IMyConstProperty Duplicate()
        {
            MyConstPropertyTransparentMaterial prop = new MyConstPropertyTransparentMaterial(Name);
            Duplicate(prop);
            return prop;
        }

        public override void SerializeValue(XmlWriter writer, object value)
        {
            writer.WriteValue(((MyTransparentMaterial)value).Name);
        }

        public override void DeserializeValue(XmlReader reader, out object value)
        {
            base.DeserializeValue(reader, out value);
            value = MyTransparentMaterials.GetMaterial((string)value);
        }
    }
}

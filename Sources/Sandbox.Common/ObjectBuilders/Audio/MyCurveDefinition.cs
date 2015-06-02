using Sandbox.Definitions;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Audio
{
    [MyDefinitionType(typeof(MyObjectBuilder_CurveDefinition))]
    public class MyCurveDefinition : MyDefinitionBase
    {
        public Curve Curve;
        protected override void Init(Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CurveDefinition;
            Curve = new Curve();
            foreach (var point in ob.Points)
                Curve.Keys.Add(new CurveKey(point.Time, point.Value));
        }
    }
}

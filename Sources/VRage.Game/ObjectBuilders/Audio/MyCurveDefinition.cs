using VRage.Game.Definitions;
using VRageMath;

namespace VRage.Game
{
    [MyDefinitionType(typeof(MyObjectBuilder_CurveDefinition))]
    public class MyCurveDefinition : MyDefinitionBase
    {
        public Curve Curve;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CurveDefinition;
            Curve = new Curve();
            foreach (var point in ob.Points)
                Curve.Keys.Add(new CurveKey(point.Time, point.Value));
        }
    }
}

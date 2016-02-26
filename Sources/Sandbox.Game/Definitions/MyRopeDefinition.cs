using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RopeDefinition))]
    public class MyRopeDefinition : MyDefinitionBase
    {
        public bool EnableRayCastRelease;
        public bool IsDefaultCreativeRope;
        public string ColorMetalTexture;
        public string NormalGlossTexture;
        public string AddMapsTexture;
        public MySoundPair AttachSound;
        public MySoundPair DetachSound;
        public MySoundPair WindingSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            var ob = (MyObjectBuilder_RopeDefinition)builder;
            this.EnableRayCastRelease = ob.EnableRayCastRelease;
            this.IsDefaultCreativeRope = ob.IsDefaultCreativeRope;
            this.ColorMetalTexture = ob.ColorMetalTexture;
            this.NormalGlossTexture = ob.NormalGlossTexture;
            this.AddMapsTexture = ob.AddMapsTexture;
            if (!string.IsNullOrEmpty(ob.AttachSound)) this.AttachSound = new MySoundPair(ob.AttachSound);
            if (!string.IsNullOrEmpty(ob.DetachSound)) this.DetachSound = new MySoundPair(ob.DetachSound);
            if (!string.IsNullOrEmpty(ob.WindingSound)) this.WindingSound = new MySoundPair(ob.WindingSound);
            base.Init(builder);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_RopeDefinition)base.GetObjectBuilder();
            ob.EnableRayCastRelease = this.EnableRayCastRelease;
            ob.IsDefaultCreativeRope = this.IsDefaultCreativeRope;
            ob.ColorMetalTexture = this.ColorMetalTexture;
            ob.NormalGlossTexture = this.NormalGlossTexture;
            ob.AddMapsTexture = this.AddMapsTexture;
            // Currently, this might not be correct since MySoundPair tries to prefix original string used to initialize it.
            ob.AttachSound = (this.AttachSound != null) ? this.AttachSound.SoundId.ToString() : null;
            ob.DetachSound = (this.DetachSound != null) ? this.DetachSound.SoundId.ToString() : null;
            ob.WindingSound = (this.WindingSound != null) ? this.WindingSound.SoundId.ToString() : null;
            return ob;
        }

    }
}

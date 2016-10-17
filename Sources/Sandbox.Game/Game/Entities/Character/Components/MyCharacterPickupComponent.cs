using Sandbox.Game.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.Entities.Character.Components
{
    [MyComponentType(typeof(MyCharacterPickupComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_CharacterPickupComponent))]
    public class MyCharacterPickupComponent : MyCharacterComponent
    {
        public virtual void PickUp()
        {
            MyCharacterDetectorComponent detectorComponent = Character.Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent != null && detectorComponent.UseObject != null)
            {
                if (detectorComponent.UseObject.IsActionSupported(UseActionEnum.PickUp))
                {
                    if (detectorComponent.UseObject.PlayIndicatorSound)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
                        Character.SoundComp.StopStateSound(true);
                    }
                    detectorComponent.UseObject.Use(UseActionEnum.PickUp, Character);
                }
                return;
            }

            return;
        }

        public virtual void PickUpContinues()
        {
            MyCharacterDetectorComponent detectorComponent = Character.Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent != null && detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.PickUp) && detectorComponent.UseObject.ContinuousUsage)
            {
                detectorComponent.UseObject.Use(UseActionEnum.PickUp, Character);
                return;
            }

            return;
        }

        public virtual void PickUpFinished()
        {
            MyCharacterDetectorComponent detectorComponent = Character.Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.UseFinished))
            {
                detectorComponent.UseObject.Use(UseActionEnum.UseFinished, Character);
                return;
            }

            return;
        }
    }
}

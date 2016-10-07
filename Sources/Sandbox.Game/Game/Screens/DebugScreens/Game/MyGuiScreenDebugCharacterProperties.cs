using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using VRage;
using VRageMath;
using Sandbox.Game.World;

namespace Sandbox.Game.Gui
{

#if !XB1

    [MyDebugScreen("Game", "Character properties")]
    class MyGuiScreenDebugCharacterProperties : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugCharacterProperties()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("System character properties", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            MyCharacter character = MySession.Static.LocalCharacter;
            if (character == null)
                return;

            AddLabel("Front light", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Reflector Distance CONST", 1, 500, () => MyCharacter.REFLECTOR_RANGE, (s) => { });
            AddSlider("Reflector Intensity CONST", 0.0f, 2.0f, () => MyCharacter.REFLECTOR_INTENSITY, (s) => { });
            //AddColor(new StringBuilder("Reflector Color"), null, MemberHelper.GetMember(() => MyCharacter.REFLECTOR_COLOR));
            //AddSlider("Reflector angle", 0.001f, 0.8f, null, MemberHelper.GetMember(() => MyCharacter.REFLECTOR_CONE_ANGLE));
            //AddSlider("Reflector direction", -89.0f, 89.0f, null, MemberHelper.GetMember(() => MyCharacter.REFLECTOR_DIRECTION));
            //AddSlider("Point Light Range", 0.0f, 100.0f, null, MemberHelper.GetMember(() => MyCharacter.POINT_LIGHT_RANGE));
            //AddSlider("Point Light Intensity", 0.0f, 2.0f, null, MemberHelper.GetMember(() => MyCharacter.POINT_LIGHT_INTENSITY));
            //AddColor(new StringBuilder("Point Light Color"), null, MemberHelper.GetMember(() => MyCharacter.POINT_COLOR));
            //AddColor(new StringBuilder("Point Light Color Specular"), null, MemberHelper.GetMember(() => MyCharacter.POINT_COLOR_SPECULAR));
            ////AddSlider(new StringBuilder("Jetpack glare size"), 0.01f, 10.0f, character.Definition, MemberHelper.GetMember(() => character.Definition.JetpackGlareSize));
            ////AddSlider(new StringBuilder("Headlight glare size"), 0.005f, 10.0f, character.Definition, MemberHelper.GetMember(() => character.Definition.LightGlareSize));
            //AddSlider("Welder glare size", 0.01f, 10.0f, null, MemberHelper.GetMember(() => MyEngineerToolBase.GLARE_SIZE));

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCharacterProperties";
        }
    }

#endif
}

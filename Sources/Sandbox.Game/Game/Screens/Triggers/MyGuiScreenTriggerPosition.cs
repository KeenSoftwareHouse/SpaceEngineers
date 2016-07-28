using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using System.Threading;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Triggers
{
    public class MyGuiScreenTriggerPosition: MyGuiScreenTrigger
    {
        MyGuiControlLabel m_labelInsX;
        protected MyGuiControlTextbox m_xCoord;
        MyGuiControlLabel m_labelInsY;
        protected MyGuiControlTextbox m_yCoord;
        MyGuiControlLabel m_labelInsZ;
        protected MyGuiControlTextbox m_zCoord;
        MyGuiControlLabel m_labelRadius;
        protected MyGuiControlTextbox m_radius;
        protected MyGuiControlButton m_pasteButton;
        const float WINSIZEX = 0.4f, WINSIZEY=0.37f;
        const float spacingH = 0.01f;
        public MyGuiScreenTriggerPosition(MyTrigger trg)
            : base(trg, new Vector2(WINSIZEX + 0.1f, WINSIZEY + 0.05f))
        {
            float left = MIDDLE_PART_ORIGIN.X-WINSIZEX/2;
            float top = -WINSIZEY / 2f + MIDDLE_PART_ORIGIN.Y;
            //X,Y,Z:
            m_labelInsX = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.01f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_X).ToString()
            );
            left += m_labelInsX.Size.X + spacingH;
            m_xCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2((WINSIZEX - spacingH) / 3 - 2 * spacingH - m_labelInsX.Size.X, 0.035f),
                Name = "textX"
            };
            m_xCoord.Enabled = false;
            left += m_xCoord.Size.X + spacingH;

            m_labelInsY = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(WINSIZEX - 0.012f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Y).ToString()
            );
            left += m_labelInsY.Size.X + spacingH;
            m_yCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2((WINSIZEX - spacingH) / 3 - 2 * spacingH - m_labelInsY.Size.X, 0.035f),
                Name = "textY"
            };
            m_yCoord.Enabled = false;
            left += m_yCoord.Size.X + spacingH;

            m_labelInsZ = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.01f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.TerminalTab_GPS_Z).ToString()
            );
            left += m_labelInsZ.Size.X + spacingH;
            m_zCoord = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2((WINSIZEX - spacingH) / 3 - 2 * spacingH - m_labelInsZ.Size.X, 0.035f),
                Name = "textZ"
            };
            m_zCoord.Enabled = false;

            left = MIDDLE_PART_ORIGIN.X - WINSIZEX / 2;
            top += m_zCoord.Size.Y + 2*VERTICAL_OFFSET;
            m_labelRadius = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.01f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.GuiTriggerPositionRadius).ToString()
            );
            left += m_labelRadius.Size.X + spacingH;
            m_radius = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2((WINSIZEX - spacingH) / 3 - 2 * spacingH - m_labelInsZ.Size.X, 0.035f),
                Name = "radius"
            };
            m_radius.TextChanged += OnRadiusChanged;

            left += m_radius.Size.X + spacingH + 0.05f;
            m_pasteButton = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.GuiTriggerPasteGps),
                visualStyle : MyGuiControlButtonStyleEnum.Small,
                onButtonClick: OnPasteButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left,top));


            Controls.Add(m_labelInsX);
            Controls.Add(m_xCoord);
            Controls.Add(m_labelInsY);
            Controls.Add(m_yCoord);
            Controls.Add(m_labelInsZ);
            Controls.Add(m_zCoord);
            Controls.Add(m_labelRadius);
            Controls.Add(m_radius);
            Controls.Add(m_pasteButton);
        }
        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            double? radius = StrToDouble(m_radius.Text);
            base.OnOkButtonClick(sender);
        }
        
        #region paste
        string m_clipboardText;
        protected bool m_coordsChanged = false;
        protected Vector3D m_coords = new Vector3D();
        void PasteFromClipboard()
        {
#if !XB1
            m_clipboardText = System.Windows.Forms.Clipboard.GetText();
#else
            System.Diagnostics.Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
        }
        private void OnPasteButtonClick(MyGuiControlButton sender)
        {
            Thread thread = new Thread(() => PasteFromClipboard());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (ScanText(m_clipboardText))
                m_coordsChanged=true;
        }
        private static readonly string m_ScanPattern = @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):";
        private bool ScanText(string input)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            // GPS:name without doublecolons:123.4:234.5:3421.6:
            foreach (Match match in Regex.Matches(input, m_ScanPattern))
            {
                String name = match.Groups[1].Value;
                double x, y, z;
                try
                {
                    x = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    x = Math.Round(x, 2);
                    y = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    y = Math.Round(y, 2);
                    z = double.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    z = Math.Round(z, 2);
                }
                catch (SystemException)
                {
                    continue;//search for next GPS in the input
                }
                m_xCoord.Text = x.ToString();
                m_coords.X = x;
                m_yCoord.Text = y.ToString();
                m_coords.Y = y;
                m_zCoord.Text = z.ToString();
                m_coords.Z = z;
                return true;//first match only
            }
#endif // !XB1

            return false;
        }
        #endregion paste

        public void OnRadiusChanged(MyGuiControlTextbox sender)
        {
            if (null != StrToDouble(sender.Text))
            {
                sender.ColorMask = Vector4.One;
                m_okButton.Enabled = true;
            }
            else
            {
                sender.ColorMask = Color.Red.ToVector4();
                m_okButton.Enabled = false;
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerPosition";
        }
    }
}


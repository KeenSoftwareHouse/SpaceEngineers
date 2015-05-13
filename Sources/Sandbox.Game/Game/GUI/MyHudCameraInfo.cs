using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    public class MyHudCameraInfo
    {
        private bool Visible { get; set; }

        private string CameraName { get; set; }
        private string ShipName { get; set; }

        private bool IsDirty { get; set; }

        public MyHudCameraInfo()
        {
            Visible = false;
            IsDirty = true;
        }

        public void Enable(string shipName, string cameraName)
        {
            Visible = true;
            ShipName = shipName;
            CameraName = cameraName;

            IsDirty = true;
        }

        public void Disable()
        {
            Visible = false;
            IsDirty = true;
        }

        public void Draw(MyGuiControlMultilineText control)
        {
            if (Visible)
            {
                if (IsDirty)
                {
                    control.Clear();
                    control.AppendText(CameraName);
                    control.AppendLine();
                    control.AppendText(ShipName);

                    IsDirty = false;
                }
            }
            else
            {
                if (IsDirty)
                {
                    control.Clear();
                    IsDirty = false;
                }
            }
        }
    }
}

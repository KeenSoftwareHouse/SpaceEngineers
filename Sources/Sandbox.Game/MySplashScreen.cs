#if !XB1
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sandbox.Game.Gui
{
    public class MySplashScreen : Form
    {
        private System.Drawing.Graphics m_graphics;
        private string m_imageFile;
        private Image m_image;
        private PointF m_scale;

        public MySplashScreen(string image, PointF scale)
        {
            try
            {
                m_image = Bitmap.FromFile(image);
                m_scale = scale;
            }
            catch (Exception)
            {
                m_image = null;
                return;
            }
            InitializeComponent();

            m_graphics = this.CreateGraphics();
            m_imageFile = image;
        }

        public void Draw()
        {
            if (m_image != null)
            {
                this.Show();
                RectangleF rekt = new RectangleF(0f, 0f, m_image.Width * m_scale.X, m_image.Height * m_scale.Y);
                m_graphics.DrawImage(m_image, rekt);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SplashScreen
            // 
            var w = (float)m_image.Width * m_scale.X;
            var h = (float)m_image.Height * m_scale.Y;
            this.ClientSize = new System.Drawing.Size((int)w, (int)h);
            this.Name = "SplashScreen";
            this.ResumeLayout(false);
            this.TopMost = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.CenterToScreen();
        }
    }
}
#endif // !XB1

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageViewer
{
    public partial class ViewerApp : Form
    {
        public ViewerApp()
        {
            InitializeComponent();
        }

        static int ColorComponent(float x)
        {
            return (int)Math.Max(0, Math.Min(255, 256 * x));
        }

        /// <summary>
        /// Load a binary raw bitmap from a file into a Bitmap object
        /// </summary>
        static Bitmap LoadRBM(string filename)
        {
            BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

            int width = reader.ReadInt32(); //read width value
            int height = reader.ReadInt32(); // read height value

            Bitmap bmp = new Bitmap(width, height);

            for (int py = 0; py < height; py++)
                for (int px = 0; px < width; px++)
                {
                    float r = reader.ReadSingle();
                    float g = reader.ReadSingle();
                    float b = reader.ReadSingle();
                    float a = reader.ReadSingle();
                    a = 1;// force no transparent

                    bmp.SetPixel(px, py, Color.FromArgb(ColorComponent(a), ColorComponent(r), ColorComponent(g), ColorComponent(b)));
                }
            reader.Close();

            return bmp;
        }

        public string FileName
        {
            set
            {
                //try
                {
                    viewer.Image = LoadRBM(value);
                    viewer.Invalidate();
                }
                //catch
                {
                  //  MessageBox.Show("Error showing the image.");
                }
            }
        }
    }
}

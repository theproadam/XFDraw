using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cppShaderInitializer
{
    public partial class ShadowConfigurator : Form
    {
        public ShadowConfigurator()
        {
            InitializeComponent();
        }

        public Panel GetPanel1()
        {
            return panel1;
        }

        public float[] Values;

        private void ShadowConfigurator_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            float bias, norm;

            if (!float.TryParse(textBox1.Text, out bias))
            {
                MessageBox.Show("Invalid bias!");
                return;
            }

            if (!float.TryParse(textBox2.Text, out norm))
            {
                MessageBox.Show("Invalid normal bias!");
                return;
            }

            Values[0] = bias;
            Values[1] = norm;

        }
    }
}

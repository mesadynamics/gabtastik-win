using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Gabtastik
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;

            this.Hide();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
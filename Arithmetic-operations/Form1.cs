using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 四則演算
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int first;
            int second;

            if (int.TryParse(textBox1.Text, out first) && int.TryParse(textBox2.Text, out second))
            {
                if(comboBox1.SelectedIndex == 0)
                {
                    double result = (double)first + (double)second;
                    textBox3.Text = result.ToString();
                }
                else if(comboBox1.SelectedIndex == 1)
                {
                    double result = (double)first - (double)second;
                    textBox3.Text = result.ToString();
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    double result = (double)first * (double)second;
                    textBox3.Text = result.ToString();
                }
                else if (comboBox1.SelectedIndex == 3)
                {
                    double result = (double)first / (double)second;
                    textBox3.Text = result.ToString();
                }
                else
                {
                    textBox3.Text = "計算方法が選択されていません．";
                }

            }
            else
            {
                textBox3.Text = "エラー！！";
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}

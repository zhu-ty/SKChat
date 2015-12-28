using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKChat
{
    public partial class SKAddFriendForm : Form
    {
        public string stu_num = string.Empty;
        public SKAddFriendForm()
        {
            InitializeComponent();
        }

        private void add_form_Load(object sender, EventArgs e)
        {
            textBox1.Focus();
            textBox1.SelectAll();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                uint _stu_num = uint.Parse(textBox1.Text);
                if (_stu_num < 2000000000 || _stu_num > 3000000000)
                    throw new Exception();
            }
            catch (Exception)
            {
                MessageBox.Show("学号输入错误哦");
                return;
            }
            stu_num = textBox1.Text;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stu_num = string.Empty;
            Close();
        }

        private void add_form_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void SKAddFriendForm_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
            textBox1.SelectAll();
        }
    }
}

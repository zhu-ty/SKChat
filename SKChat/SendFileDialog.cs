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
    public partial class SendFileDialog : Form
    {
        string _stu_num;
        public SendFileDialog()
        {
            InitializeComponent();
        }
        public void init(int max, string stu_num)
        {
            _stu_num = stu_num;
            label1.Text = "正在发送文件给" + stu_num + "...";
            progressBar1.Maximum = max;
            Update();
        }
        public void update(int _value)
        {
            label1.Text = "正在发送文件给" + _stu_num + "..." + (100*_value/progressBar1.Maximum)+"%";
            progressBar1.Value = _value;
            Update();
        }
    }
}

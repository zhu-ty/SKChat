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
    public partial class SKMsgWindow : Form
    {
        public SKMsgCore.SKFriend friend;
        SKMsgCore core;
        public SKMsgWindow(SKMsgCore.SKFriend _friend, SKMsgCore _core)
        {
            InitializeComponent();
            friend = _friend;
            core = _core;
            label1.Text = friend.show_name + "(" + friend.stu_num + ")";
            label2.Text = friend.comment;
            if (friend.Img != null)
                pictureBox1.Image = friend.Img;
            this.Visible = true;
        }
        public void add_rev_text(SKMsgInfoText t)
        {
            Action<SKMsgInfoText> rich1act = (x) =>
            {
                if (richTextBox1.Text != string.Empty)
                    richTextBox1.AppendText("\r\n");
                int c1 = richTextBox1.Text.Length;
                richTextBox1.AppendText(t.text_pack.name + "  " + t.timestamp.ToString() + "\r\n" + t.text_pack.text);
                richTextBox1.Select(c1, richTextBox1.Text.Length - c1);
                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.Select(0, 0);
                label1.Text = friend.show_name + "(" + friend.stu_num + ")";
                label2.Text = friend.comment;
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                //friend.name = t.text_pack.name;
            };
            richTextBox1.Invoke(rich1act, t);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (richTextBox2.Text == string.Empty)
                return;
            core.send_text(friend.stu_num, richTextBox2.Text);
            if (richTextBox1.Text != string.Empty)
                richTextBox1.AppendText("\r\n");
            richTextBox1.AppendText(core.master.get_name() + "  " + DateTime.Now.ToString() + "\r\n" + richTextBox2.Text);
            richTextBox2.Text = "";
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            richTextBox2.Focus();
        }
        public void refresh_lable()
        {
            label1.Text = friend.show_name + "(" + friend.stu_num + ")";
            label2.Text = friend.comment;
            if (friend.Img != null)
                pictureBox1.Image = friend.Img;
        }
        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText); 
        }
    }
}

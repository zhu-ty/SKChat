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
    public partial class SKGroupMsgWindow : Form
    {
        public List<SKMsgCore.SKFriend> friends;
        public SKMsgCore core;
        public SKGroupMsgWindow(List<SKMsgCore.SKFriend> _friends, SKMsgCore _core)
        {
            friends = _friends;
            core = _core;
            InitializeComponent();
            foreach (SKMsgCore.SKFriend f in friends)
            {
                listBox1.Items.Add(f);
            }
            this.Visible = true;
        }

        private void SKGroupMsgWindow_Load(object sender, EventArgs e)
        {

        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText); 
        }
        private void add_text_rich1(string text, Color c)
        {
            int len1 = richTextBox1.Text.Length;
            //richTextBox1.Text += text;
            richTextBox1.AppendText(text);
            richTextBox1.Select(len1, richTextBox1.Text.Length - len1);
            richTextBox1.SelectionColor = c;
            richTextBox1.Select(0, 0);
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (richTextBox2.Text == string.Empty)
                return;
            core.send_group_text(friends, richTextBox2.Text);
            if (richTextBox1.Text != string.Empty)
                richTextBox1.AppendText("\r\n");
            add_text_rich1(core.master.get_name() + "  " + DateTime.Now.ToString() + "\r\n" + richTextBox2.Text, Color.Black);
            richTextBox2.Text = "";
            richTextBox2.Focus();
        }

        public void add_text(SKMsgInfoGroupText t)
        {
            if (richTextBox1.Text != string.Empty)
                richTextBox1.AppendText("\r\n");
            add_text_rich1(t.text_pack.name + "  " + t.timestamp.ToString() + "\r\n" + t.text_pack.text, Color.Green);
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.Items.Count > 0 && listBox1.SelectedItem != null)
            {
                core.master.refresh();
                core.new_window((SKMsgCore.SKFriend)listBox1.SelectedItem);
            }
        }
    }
}

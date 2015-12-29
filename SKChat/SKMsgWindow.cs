using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace SKChat
{
    public partial class SKMsgWindow : Form
    {
        public SKMsgCore.SKFriend friend;
        SKMsgCore core;
        public bool waiting_file_rev = false;
        public string rev_file_full_name = "";
        public bool waiting_file_sen = false;
        public string sen_file_full_name = "";
        public struct file_piece
        {
            public byte[] file_piece_bytes;
            public int num;
        }
        public List<file_piece> file_pieces = new List<file_piece>();
        FileStream fs = null;
        BinaryWriter sw = null;
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
        public void rev_text(SKMsgInfoText t)
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
        public void rev_file_invite(SKMsgInfoFileInvite e)
        {
            DialogResult dr = MessageBox.Show("是否接收文件？\n文件名为" + e.file_name + "\n文件大小为：" + (e.size / 1024) + "KB\n若您正在接收上一文件，将会终止传输", "接收文件确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != System.Windows.Forms.DialogResult.Yes)
            {
                core.send_response(friend.stu_num, e.id + 1);
                add_text_rich1("\r\n你没有同意接收文件", Color.Blue);
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "选择保存文件位置";
                sfd.FileName = e.file_name;
                DialogResult drr = sfd.ShowDialog();
                if (drr == System.Windows.Forms.DialogResult.OK)
                {
                    bool is_file = false;
                    string filename = "";
                    try
                    {
                        filename = Path.GetFileName(sfd.FileName);
                        if (filename != null && filename != "")
                            is_file = true;
                    }
                    catch(Exception)
                    {
                        is_file = false;
                    }
                    if (is_file)
                    {
                        rev_file_full_name = sfd.FileName;
                        core.send_response(friend.stu_num, e.id);
                        add_text_rich1("\r\n你同意了接收文件请求，即将开始接收", Color.Blue);
                        if (waiting_file_rev)
                            core.rev_abort(friend.stu_num);
                        file_pieces.Clear();
                        waiting_file_rev = true;
                    }
                    else
                    {
                        core.send_response(friend.stu_num, e.id + 1);
                        waiting_file_rev = false;
                        add_text_rich1("\r\n你没有同意接收文件", Color.Blue);
                    }
                }
                else
                {
                    add_text_rich1("\r\n你没有同意接收文件", Color.Blue);
                    core.send_response(friend.stu_num, e.id + 1);
                    waiting_file_rev = false;
                }
            }
        }
        public void rev_response(SKMsgInfoBase e)
        {
            if(e.id == 99999 && waiting_file_sen == true)//同意接收
            {
                //int len1 = richTextBox1.Text.Length;
                add_text_rich1("\r\n对方接受了您的请求，开始传送文件", Color.Blue);
                //发送文件
                core.send_file(friend.stu_num, sen_file_full_name);
            }
            else if (e.id == 99999 + 1 && waiting_file_sen == true)//不同意接受
            {
                waiting_file_sen = false;
                add_text_rich1("\r\n对方没有同意接收你的文件", Color.Blue);
            }
            else if (e.id == 99999 - 1 && waiting_file_sen == true)
            {
                waiting_file_sen = false;
                add_text_rich1("\r\n文件发送完成", Color.Blue);
            }
        }
        public void rev_file_piece(SKMsgInfoFile e,byte[] b)
        {
            if (waiting_file_rev == false)
                return;
            try
            {
                label3.Visible = true;
                progressBar1.Visible = true;
                progressBar1.Maximum = e.max_fragment;
                progressBar1.Value = e.this_fragment;
                file_piece fp = new file_piece();
                fp.num = e.this_fragment;
                fp.file_piece_bytes = b;
                int i = 0;
                for (i = 0; i < file_pieces.Count; i++)
                    if (file_pieces[i].num > fp.num)
                        break;
                file_pieces.Insert(i, fp);
                if (file_pieces.Count == e.max_fragment)
                {
                    if (File.Exists(rev_file_full_name))
                        File.Delete(rev_file_full_name);
                    fs = new FileStream(rev_file_full_name, FileMode.Create);
                    sw = new BinaryWriter(fs);
                    for (int j = 0; j < file_pieces.Count; j++)
                    {
                        sw.Write(file_pieces[j].file_piece_bytes);
                    }
                    add_text_rich1("\r\n文件接收完成", Color.Blue);
                    core.send_response(friend.stu_num, 99999 - 1);
                    waiting_file_rev = false;
                    sw.Close();
                    fs.Close();
                    fs = null;
                    sw = null;
                    file_pieces.Clear();
                    label3.Visible = false;
                    progressBar1.Visible = false;
                }
                else if (file_pieces[file_pieces.Count - 1].num == e.max_fragment)
                {
                    add_text_rich1("\r\n文件接受失败！请再次发送", Color.Blue);
                    core.send_response(friend.stu_num, 99999 - 1);
                    waiting_file_rev = false;
                    sw.Close();
                    fs.Close();
                    fs = null;
                    sw = null;
                    file_pieces.Clear();
                    label3.Visible = false;
                    progressBar1.Visible = false;
                }
                #region old_version
                //if (e.this_fragment == 0)
                //{
                //    if (File.Exists(rev_file_full_name))
                //        File.Delete(rev_file_full_name);
                //    fs = new FileStream(rev_file_full_name, FileMode.Create);
                //    sw = new BinaryWriter(fs);
                //}
                //if ((fs == null || sw == null) && e.this_fragment == e.max_fragment - 1)
                //{
                //    add_text_rich1("\r\n文件传输貌似失败了呢", Color.Blue);
                //    core.send_response(friend.stu_num, 99999 - 1);
                //    waiting_file_rev = false;
                //}
                //if (fs == null || sw == null)
                //    return;
                //sw.Write(b);
                //if (e.this_fragment == e.max_fragment - 1)
                //{
                //    sw.Close();
                //    fs.Close();
                //    fs = null;
                //    sw = null;
                //    core.send_response(friend.stu_num, 99999 - 1);
                //    add_text_rich1("\r\n文件接收完成", Color.Blue);
                //    waiting_file_rev = false;
                //}
                #endregion
            }
            catch(Exception ee)
            {
                MessageBox.Show("接收文件失败！" + ee.Message);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (richTextBox2.Text == string.Empty)
                return;
            core.send_text(friend.stu_num, richTextBox2.Text);
            if (richTextBox1.Text != string.Empty)
                richTextBox1.AppendText("\r\n");
            add_text_rich1(core.master.get_name() + "  " + DateTime.Now.ToString() + "\r\n" + richTextBox2.Text, Color.Black);
            richTextBox2.Text = "";
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
        private void button2_Click(object sender, EventArgs e)
        {
            if (waiting_file_sen == true)
            {
                DialogResult drr = MessageBox.Show("您正在等待发送或发送上一文件，若现在再次发送将会中断上次发送，确定吗?","中断文件发送？",MessageBoxButtons.YesNo);
                if (drr == System.Windows.Forms.DialogResult.No)
                    return;
                else
                {
                    waiting_file_sen = false;
                    core.send_abort(friend.stu_num);
                }
            }
            OpenFileDialog ofp = new OpenFileDialog();
            ofp.Title = "选择要发送的文件";
            DialogResult dr = ofp.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            //此处有bug，若对方同意接受前移除文件，则会卡死无法再发
            if(!File.Exists(ofp.FileName))
            {
                MessageBox.Show("文件不存在啊哥");
            }
            try
            {
                waiting_file_sen = core.send_file_invite(friend.stu_num, ofp.FileName, 99999);
                if (waiting_file_sen)
                {
                    sen_file_full_name = ofp.FileName;
                    add_text_rich1("\r\n等待对方同意...", Color.Blue);
                }
            }
            catch (Exception ee)
            {
                waiting_file_sen = false;
                MessageBox.Show(ee.Message);
            }
        }

        private void SKMsgWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (waiting_file_rev || waiting_file_sen)
            {
                DialogResult dr = MessageBox.Show("您正在发送或接收文件，退出窗口将导致文件传输失效，确定吗？", "传送文件时退出", MessageBoxButtons.YesNo);
                if (dr == System.Windows.Forms.DialogResult.Yes)
                {
                    if (waiting_file_rev)
                        core.rev_abort(friend.stu_num);
                    if (waiting_file_sen)
                        core.send_abort(friend.stu_num);
                    waiting_file_rev = false;
                    waiting_file_sen = false;
                }
            }
        }
    }
}

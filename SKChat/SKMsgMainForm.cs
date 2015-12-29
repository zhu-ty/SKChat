using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace SKChat
{
    public partial class SKMsgMainForm : Form
    {
        string stu_num;
        SKMsgCore msg_core = null;
        public ListBoxEx listBox1;


        public SKMsgMainForm()
        {
            InitializeComponent();
            this.listBox1 = new ListBoxEx();
            // 
            // listBox1
            // 
            this.Controls.Add(this.listBox1);
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(12, 78);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(350, 400);
            this.listBox1.TabIndex = 4;
            this.listBox1.MouseDoubleClick += listbox1_double_click;
            listBox1.MouseDown += SKListBox_MouseDown;
            listBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listBox1.SelectionMode = SelectionMode.MultiExtended;
        }

        private void SKMsgMainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(msg_core != null)
                msg_core.stop();
        }

        private void SKMsgMainForm_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            timer2.Enabled = false;
            LoginForm.login_info login_info = new LoginForm.login_info();
            System.Net.Sockets.Socket login_socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Stream,System.Net.Sockets.ProtocolType.Tcp);
            (new LoginForm(ref login_socket, ref login_info)).ShowDialog();
            if (login_info.suc)
                this.Visible = true;
            else
                Close();
            stu_num = login_info.stu_num;
            msg_core = new SKMsgCore(stu_num, login_socket,this);
            timer2.Enabled = true;
            refresh();
        }
        /// <summary>
        /// 姓名长度限制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 20)
                textBox1.Text = textBox1.Text.Substring(0, 20);
            if (msg_core != null)
            {
                msg_core._my_name = textBox1.Text;
                refresh();
            }
        }

        /// <summary>
        /// 时钟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = DateTime.Now.ToString();
        }

        /// <summary>
        /// 签名长度限制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length > 30)
                textBox2.Text = textBox2.Text.Substring(0, 30);
            if (msg_core != null)
            {
                msg_core._my_comment = textBox2.Text;
                refresh();
            }
        }
        /// <summary>
        /// 打开新窗口，调用刷新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listbox1_double_click(object sender, EventArgs e)
        {
            //MessageBox.Show(((ListBoxItemEx)(listBox1.SelectedItem)).ID);
            //refresh();
            if (listBox1.Items.Count <= 0)
                return;
            if (listBox1.SelectedItem != null && ((SKMsgCore.SKFriend)(listBox1.SelectedItem)).isCategory == false)
            {
                object c = listBox1.SelectedItem;
                refresh();
                msg_core.new_window((SKMsgCore.SKFriend)(c));
            }
            //refresh();
        }

        /// <summary>
        /// 窗口调用的添加好友
        /// </summary>
        /// <param name="id"></param>
        /// <param name="remarks"></param>
        /// <param name="comment"></param>
        /// <param name="img"></param>
        public SKMsgCore.SKFriend add_friend(string id, string remarks, string comment, Bitmap img)
        {
            SKMsgCore.SKFriend f = msg_core.add_friend(id, remarks, comment);
            refresh();
            return f;
        }
        /// <summary>
        /// 获得本人名字
        /// </summary>
        /// <returns></returns>
        public string get_name()
        {
            return textBox1.Text;
        }
        /// <summary>
        /// 获得本人的签名
        /// </summary>
        /// <returns></returns>
        public string get_comment()
        {
            return textBox2.Text;
        }
        /// <summary>
        /// 只在开始时调用一次
        /// </summary>
        /// <param name="__name"></param>
        public void set_name(string __name)
        {
            textBox1.Text = __name;
        }
        /// <summary>
        /// 只在开始时调用一次
        /// </summary>
        /// <param name="__name"></param>
        public void set_comment(string __comment)
        {
            textBox2.Text = __comment;
        }
        /// <summary>
        /// 刷新（会调用内核刷新）
        /// </summary>
        public void refresh()
        {
            Action refresh_act = () =>
            {
                msg_core.refresh();
                listBox1.Items.Clear();
                foreach (SKMsgCore.SKFriend f in msg_core.friend_list)
                {
                    listBox1.Items.Add(f);
                }
            };
            if(this.Visible)
                listBox1.Invoke(refresh_act);
            //msg_core.refresh();
            //listBox1.Items.Clear();
            //foreach (SKMsgCore.SKFriend f in msg_core.friend_list)
            //{
            //    listBox1.Items.Add(f);
            //}
        }

        private void SKListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (listBox1.SelectedItems.Count > 0)
                {
                    if (listBox1.SelectedItems.Count > 1)
                    {
                        发起群聊ToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        发起群聊ToolStripMenuItem.Enabled = false;
                    }
                    RClick.Show(MousePosition.X, MousePosition.Y);
                }
                
            }
        }

        private void button_add_Click(object sender, EventArgs e)
        {
            SKAddFriendForm saff = new SKAddFriendForm();
            saff.ShowDialog();
            if (saff.stu_num != "")
                add_friend(saff.stu_num, "", "", null);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            refresh();
        }

        private void 删除好友ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<SKMsgCore.SKFriend> to_remove = new List<SKMsgCore.SKFriend>();
            foreach (SKMsgCore.SKFriend ff in listBox1.SelectedItems)
            {
                to_remove.Add(ff);
            }
            //SKMsgCore.SKFriend f = (SKMsgCore.SKFriend)listBox1.SelectedItem;
            //msg_core.remove_friend(f);
            for (int i = 0; i < to_remove.Count; i++)
            {
                msg_core.remove_friend(to_remove[i]);
            }
        }

        private void 发起群聊ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<SKMsgCore.SKFriend> _friends_ = new List<SKMsgCore.SKFriend>();
            foreach (object i in listBox1.SelectedItems)
            {
                SKMsgCore.SKFriend fi = (i as SKMsgCore.SKFriend);
                _friends_.Add(fi);
            }
            msg_core.new_g_window(_friends_);
        }
    }

    public class ListBoxEx : ListBox
    {
        //public new ArrayList Items = new ArrayList();
        SKMsgCore.SKFriend UnderMouseItem = null;
        public ListBoxEx()
        {
            this.DrawMode = DrawMode.OwnerDrawVariable;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); //！！！
            UpdateStyles();
        }
        protected override void OnMeasureItem(MeasureItemEventArgs e) //设置各项高度
        {
            try
            {
                if (e.Index >= 0)
                {
                    base.OnMeasureItem(e);
                    SKMsgCore.SKFriend i = Items[e.Index] as SKMsgCore.SKFriend;
                    if (i != null)
                    {
                        if (i.isCategory)
                        {
                            e.ItemHeight = 30;
                        }
                        else
                        {
                            e.ItemHeight = 60;
                        }
                    }
                }
            }
            catch
            {

            }
        }
        protected override void OnPaint(PaintEventArgs e) //重绘
        {
            base.OnPaint(e);
            //e.Graphics.DrawImage(Resources._12321, new Point(0, 0)); //背景
            //e.Graphics.DrawImage(Resources.bk, new Rectangle(0, 0, Width, Height)); //半透明
            for (int i = 0; i < Items.Count; ++i)
            {
                SKMsgCore.SKFriend item = Items[i] as SKMsgCore.SKFriend;
                if (item != null)
                {
                    Rectangle bound = GetItemRectangle(i);
                    if (item.isCategory) //分类
                    {
                        if (in_select(item,this.SelectedItems)) //选中项
                        {
                            e.Graphics.FillRectangle(Brushes.Gray, bound);
                            e.Graphics.DrawString(item.stu_num, new Font("微软雅黑", 12), Brushes.White, new PointF(bound.Left + 5, bound.Top + 6));
                        }
                        else if (item == UnderMouseItem) //鼠标滑过
                        {
                            e.Graphics.FillRectangle(Brushes.Ivory, bound);
                            e.Graphics.DrawString(item.stu_num, new Font("微软雅黑", 12), Brushes.Black, new PointF(bound.Left + 5, bound.Top + 6));
                        }
                        else  //正常项
                        {
                            e.Graphics.DrawString(item.stu_num, new Font("微软雅黑", 12), Brushes.Black, new PointF(bound.Left + 5, bound.Top + 6));
                        }
                    }
                    else  //项
                    {
                        if (in_select(item, this.SelectedItems))
                        {
                            e.Graphics.FillRectangle(Brushes.Gray, bound);
                            if (item.Img != null)
                                e.Graphics.DrawImage(item.Img, new Rectangle(bound.Left + 5, bound.Top + 6, 40, 48));
                            e.Graphics.DrawString(item.show_name + "(" + item.stu_num + ")", new Font("微软雅黑", 10), Brushes.White, new PointF(bound.Left + 5 + 50, bound.Top + 6));
                            e.Graphics.DrawString("个性签名:“" + item.comment + "”", new Font("微软雅黑", 11), Brushes.White, new PointF(bound.Left + 5 + 50, bound.Top + 20 + 6));

                        }
                        else if (item == UnderMouseItem)
                        {
                            e.Graphics.FillRectangle(Brushes.Ivory, bound);
                            if (item.Img != null)
                                e.Graphics.DrawImage(item.Img, new Rectangle(bound.Left + 5, bound.Top + 6, 40, 48));
                            e.Graphics.DrawString(item.show_name + "(" + item.stu_num + ")", new Font("微软雅黑", 10), Brushes.Black, new PointF(bound.Left + 5 + 50, bound.Top + 6));
                            e.Graphics.DrawString("个性签名:“" + item.comment + "”", new Font("微软雅黑", 11), Brushes.Black, new PointF(bound.Left + 5 + 50, bound.Top + 20 + 6));
                        }
                        else
                        {
                            if (item.Img != null)
                                e.Graphics.DrawImage(item.Img, new Rectangle(bound.Left + 5, bound.Top + 6, 40, 48));
                            e.Graphics.DrawString(item.show_name + "(" + item.stu_num + ")", new Font("微软雅黑", 10), Brushes.Black, new PointF(bound.Left + 5 + 50, bound.Top + 6));
                            e.Graphics.DrawString("个性签名:“" + item.comment + "”", new Font("微软雅黑", 11), Brushes.Black, new PointF(bound.Left + 5 + 50, bound.Top + 20 + 6));
                        }
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) //鼠标滑过
        {
            base.OnMouseMove(e);
            for (int i = 0; i < Items.Count; ++i)
            {
                Rectangle rect = this.GetItemRectangle(i);
                if (rect.Contains(e.Location))
                {
                    UnderMouseItem = Items[i] as SKMsgCore.SKFriend;
                    Invalidate();
                    break;
                }
            }
        }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
        }
        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
        }

        private bool in_select(object item, SelectedObjectCollection items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (item == items[i])
                    return true;
            }
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace SKChat
{
    public partial class LoginForm : Form
    {
        Socket login_socket;

        public class login_info
        {
            public bool suc =false;
            public string stu_num = "";
        }
        login_info welcome;

        public LoginForm(ref Socket _login_socket,ref login_info _welcome)
        {
            login_socket = _login_socket;
            welcome = _welcome;
            InitializeComponent();
            this.Opacity = 0;
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.BackColor = Color.FromArgb(0, 1, 1, 1);
            label2.BackColor = Color.FromArgb(0, 1, 1, 1);
            label3.BackColor = Color.FromArgb(0, 1, 1, 1);
            label4.BackColor = Color.FromArgb(0, 1, 1, 1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //login_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(textBox3.Text);
                int port = int.Parse(textBox4.Text);
                IAsyncResult connect_result = login_socket.BeginConnect(ip, port, null, null);
                connect_result.AsyncWaitHandle.WaitOne(10 * 1000);//10s
                if (!connect_result.IsCompleted)
                {
                    login_socket.Close();
                    Failed();
                }
                string to_send = string.Empty;
                to_send += textBox1.Text;
                to_send += "_";
                to_send += textBox2.Text;
                login_socket.Send(Encoding.Default.GetBytes(to_send));
                byte[] receive = new byte[100];
                int len = login_socket.Receive(receive);
                if (len == 3 && Encoding.Default.GetString(receive, 0, len) == "lol")
                    Success();
                else
                {
                    Failed();
                }
            }
            catch (Exception)
            {
                Failed();
            }
        }

        private void Failed()
        {
            MessageBox.Show("登录失败！");
            //login_socket.Close();
        }

        private void Success()
        {
            MessageBox.Show("登陆成功！");
            welcome.suc = true;
            welcome.stu_num = textBox1.Text;
            //login_socket.Close();
            this.Close();
        }

        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Opacity += 0.1;
            if (this.Opacity == 1)
                timer1.Stop();
        }
    }
}

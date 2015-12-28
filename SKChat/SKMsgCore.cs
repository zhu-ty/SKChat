using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using DA32ProtocolCsharp;
using System.Drawing;

namespace SKChat
{
    /// <summary>
    /// 主程序核心
    /// </summary>
    public class SKMsgCore
    {
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="_stu_num"></param>
        public SKMsgCore(string _stu_num,Socket _login_socket,SKMsgMainForm _master)
        {
            master = _master;
            my_stu_num = _stu_num;
            login_socket = _login_socket;
            login_socket.ReceiveTimeout = 100;
            servers.ServerCall += receive_mes;
            servers.start_listening();
            file_init();
        }
        /// <summary>
        /// 退出
        /// </summary>
        public void stop()
        {
            clients.SendExitToAll();
            Thread.Sleep(100);
            file_save();
            servers.stop_listening();
            try
            {
                login_socket.Send(Encoding.Default.GetBytes("logout"+my_stu_num));
            }
            catch (Exception) { }
        }
        public void new_window(SKFriend listboxitem)
        {
            for (int i = 0;i < window_list.Count;i++)
            {
                SKMsgWindow msg_window = window_list[i];
                if (msg_window == null || msg_window.Visible == false)
                {
                    window_list.Remove(msg_window);
                    i--;
                }
                else if (msg_window.friend.stu_num == listboxitem.stu_num)
                {
                    return;
                }
            }
            window_list.Add(new SKMsgWindow(listboxitem,this));
        }
        public void refresh()
        {
            for (int i = 0; i < window_list.Count; i++)
            {
                SKMsgWindow msg_window = window_list[i];
                if (msg_window == null || msg_window.Visible == false)
                {
                    window_list.Remove(msg_window);
                    i--;
                }
                else
                {
                    msg_window.refresh_lable();
                }
            }
            foreach (SKFriend f in friend_list)
            {
                try
                {
                    login_socket.Send(Encoding.Default.GetBytes("q" + f.stu_num));
                    byte[] receive_buffer = new byte[100];
                    int len = login_socket.Receive(receive_buffer);
                    IPAddress ip = IPAddress.Parse(Encoding.Default.GetString(receive_buffer,0,len));
                    f.ip = ip;
                    f.online = true;
                }
                catch
                {
                    f.ip = null;
                    f.online = false;
                }
            }
        }
        public void receive_mes(object sender,SKMsgInfoBase msg_info)
        {
            if (msg_info.type == SKMsgInfoBase.mestype.TEXT)
            {
                string this_stu_num = "";
                foreach (SKFriend f in friend_list)
                {
                    if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
                    {
                        this_stu_num = f.stu_num;
                        f.name = (msg_info as SKMsgInfoText).text_pack.name;
                        break;
                    }
                }
                for (int i = 0; i < window_list.Count; i++)
                {
                    SKMsgWindow msg_window = window_list[i];
                    if (msg_window == null || msg_window.Visible == false)
                    {
                        window_list.Remove(msg_window);
                        i--;
                    }
                    else if (msg_window.friend.stu_num == this_stu_num)
                    {
                        msg_window.add_rev_text((SKMsgInfoText)msg_info);
                        break;
                    }
                }
            }
        }
        public void send_text(string target_stu_num, string text)
        {
            IPAddress tar_ip = find_ip(target_stu_num);
            if (tar_ip != null)
            {
                //clients.SendText(random.Next(0, 65535), master.get_name(), text, tar_ip, DateTime.Now);
                SKMsgInfoText sit = new SKMsgInfoText();
                sit.text_pack.text = text;
                sit.text_pack.name = master.get_name();
                sit.id = random.Next(0, 65535);
                sit.type = SKMsgInfoBase.mestype.TEXT;
                sit.timestamp = DateTime.Now;
                clients.SendNotFile(sit, tar_ip);
            }
        }
        public void add_friend(string add_stu_num,string add_stu_name = "",string add_stu_note = "")
        {
            foreach (SKFriend ff in friend_list)
            {
                if (ff.stu_num == add_stu_num)
                    return;
            }
            SKFriend f = new SKFriend(add_stu_num, add_stu_name, add_stu_note);
            friend_list.Add(f);
            refresh();
        }

        void file_init()
        {
            string directory = System.Environment.CurrentDirectory + "\\Data\\";
            //string directory = "/";
            directory += my_stu_num;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            FileStream friend_list_stream = new FileStream(directory + "\\friend.list", FileMode.OpenOrCreate);
            StreamReader sr = new StreamReader(friend_list_stream);
            while (!sr.EndOfStream)
            {
                string __stu_num = sr.ReadLine();
                string __name = sr.ReadLine();
                string __note = sr.ReadLine();
                friend_list.Add(new SKFriend(__stu_num, __name, __note));
            }
            sr.Close();
            friend_list_stream.Close();
        }
        void file_save()
        {
            try
            {
                string directory = System.Environment.CurrentDirectory + "\\Data\\";
                directory += my_stu_num;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (File.Exists(directory + "\\friend.list"))
                    File.Delete(directory + "\\friend.list");
                FileStream friend_list_stream = new FileStream(directory + "\\friend.list", FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(friend_list_stream);
                for (int i = 0; i < friend_list.Count; i++)
                {
                    sw.WriteLine(friend_list[i].stu_num);
                    sw.WriteLine(friend_list[i].name);
                    sw.WriteLine(friend_list[i].note);
                }
                sw.Close();
                friend_list_stream.Close();
            }
            catch(Exception)
            {
                return;
            }
        }
        IPAddress find_ip(string __stu_num)
        {
            foreach(SKFriend f in friend_list)
            {
                if (__stu_num == f.stu_num && f.online)
                {
                    return f.ip;
                    break;
                }
                else if (!f.online)
                    return null;
            }
            return null;
        }

        public SKMsgMainForm master;
        SKClient clients = new SKClient();
        SKServer servers = new SKServer();
        string my_stu_num;
        Socket login_socket;
        Random random = new Random();

        public List<SKFriend> friend_list = new List<SKFriend>();
        List<SKMsgWindow> window_list = new List<SKMsgWindow>();
        public class SKFriend
        {
            public SKFriend(string _stu_num, string _name, string _note)
            {
                stu_num = _stu_num;
                name = _name;
                note = _note;
            }
            public string stu_num = "";
            public string name = "";
            public string note = "";
            public IPAddress ip = null;
            public bool online = false;
            public Bitmap Img = null;
            public string show_name
            {
                get
                {
                    return name + ((online) ? "（在线）" : "（离线）");
                }
                set
                {
                    name = value;
                }
            }
            public bool isCategory = false;
        }
    }
}

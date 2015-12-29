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
        bool starting = true;
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
            SKServer.ListenPorts = int.Parse(_stu_num.Substring(_stu_num.Length - 4));
            servers.start_listening();
            head = Properties.Resources.nomarl_head;
            file_init();
            starting = false;
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
        public SKMsgWindow new_window(SKFriend listboxitem)
        {
            if (listboxitem == null)
                return null;
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
                    return msg_window;
                }
            }
            SKMsgWindow neww = new SKMsgWindow(listboxitem, this);
            window_list.Add(neww);
            return neww;
        }
        public SKGroupMsgWindow new_g_window(List<SKFriend> _friends)
        {
            bool find_me = false;
            if (_friends == null || _friends.Count == 0)
                return null;
            for (int i = 0; i < g_window_list.Count; i++)
            {
                if (g_window_list[i] == null || g_window_list[i].Visible == false)
                {
                    g_window_list.Remove(g_window_list[i]);
                    i--;
                }
                else if (compare_friends(_friends, g_window_list[i].friends))
                    return g_window_list[i];
            }
            foreach (SKFriend f in _friends)
                if (f.stu_num == my_stu_num)
                    find_me = true;
            if (!find_me)
                _friends.Add(me);
            SKGroupMsgWindow newg = new SKGroupMsgWindow(_friends, this);
            g_window_list.Add(newg);
            return newg;
        }
        public void refresh()
        {
            //login_socket.Send(Encoding.Default.GetBytes(my_stu_num + "_net2015"));
            //try
            //{
            //    int tmp = login_socket.ReceiveTimeout;
            //    login_socket.ReceiveTimeout = 100;
            //    byte[] tmp2 = new byte[3];
            //    login_socket.Receive(tmp2);
            //    login_socket.ReceiveTimeout = tmp;
            //}
            //catch (Exception)
            //{ 
            //}
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
                    int i = 0;
                    for (i = 0; i < len; i++)
                        if (receive_buffer[i] > ((byte)'0' - 1) && receive_buffer[i] < ((byte)'9' + 1))
                            break;
                    int j = i;
                    for (j = i; j < len; j++)
                        if ((receive_buffer[j] != (byte)'.') && (receive_buffer[i] <= ((byte)'0' - 1) || receive_buffer[i] >= ((byte)'9' + 1)))
                            break;
                    IPAddress ip = IPAddress.Parse(Encoding.Default.GetString(receive_buffer, i, j - i));
                    f.ip = ip;
                    f.online = true;
                    if (f.stu_num == my_stu_num)
                    {
                        f.name = master.get_name();
                        f.comment = master.get_comment();
                    }
                }
                catch
                {
                    f.ip = null;
                    f.online = false;
                }
            }
        }
        public void receive_mes(object sender,SKMsgInfoBase _msg_info,byte[] file_piece = null)
        {
            Action<SKMsgInfoBase> receive_act = (msg_info) =>
            {
                #region TEXT
                if (msg_info.type == SKMsgInfoBase.mestype.TEXT)
                {
                    string this_stu_num = "";
                    SKFriend ff = null;
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == msg_info.stu_num)
                        {
                            this_stu_num = f.stu_num;
                            f.name = (msg_info as SKMsgInfoText).text_pack.name;
                            ff = f;
                            break;
                        }
                    }
                    if (this_stu_num == "")
                    {
                        SKFriend newf = master.add_friend(msg_info.stu_num, ((SKMsgInfoText)msg_info).text_pack.name, "", null);
                        SKMsgWindow neww = new_window(newf);
                        neww.rev_text((SKMsgInfoText)msg_info);
                    }
                    else
                    {
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
                                //找到了已经打开的窗口
                                msg_window.rev_text((SKMsgInfoText)msg_info);
                                return;
                            }
                        }
                        //未找到已打开的窗口
                        SKMsgWindow neww = new_window(ff);
                        neww.rev_text((SKMsgInfoText)msg_info);
                    }
                }
                #endregion
                #region RESPONSE
                else if (msg_info.type == SKMsgInfoBase.mestype.RESPONSE)
                {
                    string this_stu_num = "";
                    SKFriend ff = null;
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == msg_info.stu_num)
                        {
                            this_stu_num = f.stu_num;
                            ff = f;
                            break;
                        }
                    }
                    if (this_stu_num == "")
                    {
                        SKFriend newf = master.add_friend(msg_info.stu_num, "", "", null);
                        SKMsgWindow neww = new_window(newf);
                        neww.rev_response(msg_info);
                    }
                    else
                    {
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
                                //找到了已经打开的窗口
                                msg_window.rev_response(msg_info);
                                return;
                            }
                        }
                        //未找到已打开的窗口
                        SKMsgWindow neww = new_window(ff);
                        neww.rev_response(msg_info);
                    }
                }
                #endregion
                #region FILE
                else if (msg_info.type == SKMsgInfoBase.mestype.FILE)
                {
                    string this_stu_num = "";
                    SKFriend ff = null;
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == msg_info.stu_num)
                        {
                            this_stu_num = f.stu_num;
                            ff = f;
                            break;
                        }
                    }
                    if (this_stu_num == "")
                    {

                    }
                    else
                    {
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
                                //找到了已经打开的窗口
                                msg_window.rev_file_piece((SKMsgInfoFile)msg_info, file_piece);
                                return;
                            }
                        }
                    }
                }
                #endregion
                #region FILE_INVITE
                else if (msg_info.type == SKMsgInfoBase.mestype.FILE_INVITE)
                {
                    string this_stu_num = "";
                    SKFriend ff = null;
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == msg_info.stu_num)
                        {
                            this_stu_num = f.stu_num;
                            ff = f;
                            break;
                        }
                    }
                    if (this_stu_num == "")
                    {
                        SKFriend newf = master.add_friend(msg_info.stu_num, "", "", null);
                        SKMsgWindow neww = new_window(newf);
                        neww.rev_file_invite((SKMsgInfoFileInvite)msg_info);
                    }
                    else
                    {
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
                                //找到了已经打开的窗口
                                msg_window.rev_file_invite((SKMsgInfoFileInvite)msg_info);
                                return;
                            }
                        }
                        //未找到已打开的窗口
                        SKMsgWindow neww = new_window(ff);
                        neww.rev_file_invite((SKMsgInfoFileInvite)msg_info);
                    }
                }
                #endregion
                #region GROUP_TEXT
                else if (msg_info.type == SKMsgInfoBase.mestype.GROUP_TEXT)
                {
                    List<SKFriend> friend_list_new = new List<SKFriend>();//用于生成当前群聊的好友列表
                    //把群聊所有人加为好友，构造friendlist
                    foreach (string one_more in ((SKMsgInfoGroupText)msg_info).stu_num_list)
                    {
                        SKFriend is_new_friend = null;
                        foreach (SKFriend f in friend_list)
                        {
                            if (f != null && f.ip != null && f.stu_num == one_more)
                            {
                                is_new_friend = f;
                                friend_list_new.Add(f);
                                break;
                            }
                        }
                        if (is_new_friend == null && one_more != my_stu_num)
                        {
                            SKFriend newf = master.add_friend(one_more,"", "", null);
                            friend_list_new.Add(newf);
                        }
                    }
                    //更新发送方的姓名
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == (msg_info as SKMsgInfoGroupText).stu_num)
                        {
                            f.name = (msg_info as SKMsgInfoGroupText).text_pack.name;
                        }
                    }
                    //查看群聊窗口是否存在
                    SKGroupMsgWindow sgmw = null;
                    foreach (SKGroupMsgWindow n in g_window_list)
                    {
                        if (compare_friends(friend_list_new, n.friends))
                        {
                            sgmw = n;
                            break;
                        }
                    }
                    if (sgmw == null)
                    {
                        sgmw = new_g_window(friend_list_new);
                    }
                    sgmw.add_text(msg_info as SKMsgInfoGroupText);
                }
                #endregion
                #region SYNC
                else if (msg_info.type == SKMsgInfoBase.mestype.SYNC)
                {
                    SKMsgInfoSync msg_sync = (SKMsgInfoSync)msg_info;
                    string this_stu_num = "";
                    SKFriend ff = null;
                    foreach (SKFriend f in friend_list)
                    {
                        if (f != null && f.ip != null && f.stu_num == msg_sync.stu_num)
                        {
                            this_stu_num = f.stu_num;
                            ff = f;
                            break;
                        }
                    }
                    if (this_stu_num == "")
                    {
                        SKFriend newf = master.add_friend(msg_sync.stu_num, msg_sync.name, msg_sync.comment, msg_sync.head_60_60);
                    }
                    else
                    {
                        ff.name = msg_sync.name;
                        ff.comment = msg_sync.comment;
                        ff.Img = msg_sync.head_60_60;
                    }
                }
                #endregion
                master.refresh();
            };
            master.BeginInvoke(receive_act, _msg_info);
            #region no_invoke
            //if (msg_info.type == SKMsgInfoBase.mestype.TEXT)
            //{
            //    string this_stu_num = "";
            //    SKFriend ff = null;
            //    foreach (SKFriend f in friend_list)
            //    {
            //        if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
            //        {
            //            this_stu_num = f.stu_num;
            //            f.name = (msg_info as SKMsgInfoText).text_pack.name;
            //            ff = f;
            //            break;
            //        }
            //    }
            //    if (this_stu_num == "")
            //    {
            //        SKFriend newf = master.add_friend(msg_info.stu_num,"","",null);
            //        SKMsgWindow neww = new_window(newf);
            //        neww.add_rev_text((SKMsgInfoText)msg_info);
            //    }
            //    else
            //    {
            //        for (int i = 0; i < window_list.Count; i++)
            //        {
            //            SKMsgWindow msg_window = window_list[i];
            //            if (msg_window == null || msg_window.Visible == false)
            //            {
            //                window_list.Remove(msg_window);
            //                i--;
            //            }
            //            else if (msg_window.friend.stu_num == this_stu_num)
            //            {
            //                //找到了已经打开的窗口
            //                msg_window.add_rev_text((SKMsgInfoText)msg_info);
            //                return;
            //            }
            //        }
            //        //未找到已打开的窗口
            //        SKMsgWindow neww = new_window(ff);
            //        neww.add_rev_text((SKMsgInfoText)msg_info);
            //    }
            //}
            #endregion
        }
        public void send_text(string target_stu_num, string text)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(target_stu_num);
            if (tar_ip != null)
            {
                //clients.SendText(random.Next(0, 65535), master.get_name(), text, tar_ip, DateTime.Now);
                SKMsgInfoText sit = new SKMsgInfoText();
                sit.stu_num = my_stu_num;
                sit.text_pack.text = text;
                sit.text_pack.name = master.get_name();
                sit.id = random.Next(0, 65535);
                sit.type = SKMsgInfoBase.mestype.TEXT;
                sit.timestamp = DateTime.Now;
                clients.SendNotFile(sit, tar_ip,target_stu_num);
            }
        }
        public bool send_file_invite(string target_stu_num,string file, int file_invite_id = 99999)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(target_stu_num);
            if(tar_ip != null)
            {
                try
                {
                    SKMsgInfoFileInvite smifi = new SKMsgInfoFileInvite();
                    smifi.id = file_invite_id;
                    smifi.stu_num = my_stu_num;
                    smifi.timestamp = DateTime.Now;
                    smifi.type = SKMsgInfoBase.mestype.FILE_INVITE;
                    smifi.file_name = Path.GetFileName(file);
                    long len = (new FileInfo(file)).Length;
                    if(len > int.MaxValue)
                        throw new Exception("文件不能大于2G");
                    smifi.size = (int)len;
                    return clients.SendNotFile(smifi,tar_ip,target_stu_num);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
        public bool send_response(string target_stu_num, int id)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(target_stu_num);
            if (tar_ip != null)
            {
                try
                {
                    SKMsgInfoBase sib = new SKMsgInfoBase();
                    sib.id = id;
                    sib.stu_num = my_stu_num;
                    sib.timestamp = DateTime.Now;
                    sib.type = SKMsgInfoBase.mestype.RESPONSE;
                    return clients.SendNotFile(sib,tar_ip,target_stu_num);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
        public bool send_file(string target_stu_num, string file_full_path)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(target_stu_num);
            if (tar_ip != null)
            {
                clients.SendFile(tar_ip, file_full_path, my_stu_num,target_stu_num);
            }
            return false;
        }
        public void rev_abort(string from_stu_num)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(from_stu_num);
            if (tar_ip != null)
            {
                servers.abort(tar_ip);
            }
        }
        public void send_abort(string target_stu_num)
        {
            master.refresh();
            IPAddress tar_ip = find_ip(target_stu_num);
            if (tar_ip != null)
            {
                clients.file_abort(tar_ip);
            }
            return;
        }
        public void send_group_text(List<SKFriend> fs, string text)
        {
            master.refresh();
            SKMsgInfoGroupText sigt = new SKMsgInfoGroupText();
            sigt.id = random.Next(65535);
            sigt.stu_num = my_stu_num;
            sigt.text_pack.name = master.get_name();
            sigt.text_pack.text = text;
            sigt.type = SKMsgInfoBase.mestype.GROUP_TEXT;
            sigt.timestamp = DateTime.Now;
            foreach (SKFriend f in fs)
                sigt.stu_num_list.Add(f.stu_num);
            foreach (SKFriend f in fs)
                if (f.online && f.stu_num != my_stu_num)
                    clients.SendNotFile(sigt, f.ip, f.stu_num);
        }
        public void send_sync()
        {
            refresh();
            SKMsgInfoSync info_sync = new SKMsgInfoSync();
            info_sync.head_60_60 = head;
            info_sync.id =0;
            info_sync.comment = my_comment;
            info_sync.name = my_name;
            info_sync.timestamp = DateTime.Now;
            info_sync.type = SKMsgInfoBase.mestype.SYNC;
            info_sync.stu_num = my_stu_num;
            foreach (SKFriend f in friend_list)
            {
                if (f.stu_num != my_stu_num && f.online)
                    clients.SendNotFile(info_sync, f.ip, f.stu_num);
            }
        }
        /// <summary>
        /// 只能被MainForm调用 否则会不同步
        /// </summary>
        /// <param name="add_stu_num"></param>
        /// <param name="add_stu_name"></param>
        /// <param name="add_stu_comment"></param>
        /// <returns></returns>
        public SKFriend add_friend(string add_stu_num, string add_stu_name = "", string add_stu_comment = "",Bitmap _image = null)
        {
            foreach (SKFriend ff in friend_list)
            {
                if (ff.stu_num == add_stu_num)
                    return ff;
            }
            SKFriend f = new SKFriend(add_stu_num, add_stu_name, add_stu_comment,_image);
            friend_list.Add(f);
            if(!starting)
                master.refresh();  
            return f;
        }
        public void remove_friend(SKFriend f)
        {
            friend_list.Remove(f);
            master.refresh();
        }
        void file_init()
        {
            string directory = System.Environment.CurrentDirectory + "\\Data\\";
            //string directory = "/";
            directory += my_stu_num;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            //if (!File.Exists(directory + "\\myself.info"))
            //{
            //    FileStream my_info_stream22 = new FileStream(directory + "\\myself.info", FileMode.OpenOrCreate);
            //    StreamWriter sw222 = new StreamWriter(my_info_stream22);
            //    sw222.WriteLine(my_name);
            //    sw222.WriteLine(my_comment);
            //    sw222.Close();
            //    my_info_stream22.Close();
            //}
            FileStream my_info_stream = new FileStream(directory + "\\myself.info", FileMode.OpenOrCreate);
            StreamReader sr2 = new StreamReader(my_info_stream);
            if (!sr2.EndOfStream)
                my_name = sr2.ReadLine();
            if (!sr2.EndOfStream)
                my_comment = sr2.ReadLine();
            sr2.Close();
            my_info_stream.Close();
            me = add_friend(my_stu_num, my_name, my_comment);

            if (File.Exists(directory + "\\head.bmp"))
                head = new Bitmap(directory + "\\head.bmp");

            FileStream friend_list_stream = new FileStream(directory + "\\friend.list", FileMode.OpenOrCreate);
            StreamReader sr = new StreamReader(friend_list_stream);
            while (!sr.EndOfStream)
            {
                string __stu_num = sr.ReadLine();
                string __name = sr.ReadLine();
                string __comment = sr.ReadLine();
                //friend_list.Add(new SKFriend(__stu_num, __name, __comment));
                string directory2 = directory + "\\" + __stu_num;
                bool pic = false;
                if (Directory.Exists(directory2) && File.Exists(directory2 + "\\head.bmp"))
                {
                    try
                    {
                        Bitmap b = new Bitmap(directory2 + "\\head.bmp");
                        add_friend(__stu_num, __name, __comment, b);
                        pic = true;
                    }
                    catch (Exception) { }
                }
                if(!pic)
                    add_friend(__stu_num, __name, __comment);
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
                    sw.WriteLine(friend_list[i]._name);
                    sw.WriteLine(friend_list[i]._comment);
                    if (friend_list[i].Img != null)
                    {
                        string directory2 = directory + "\\" + friend_list[i].stu_num;
                        if (!Directory.Exists(directory2))
                            Directory.CreateDirectory(directory2);
                        if (File.Exists(directory2 + "\\head.bmp"))
                            File.Delete(directory2 + "\\head.bmp");
                        friend_list[i].Img.Save(directory2 + "\\head.bmp");
                    }
                }
                sw.Close();
                friend_list_stream.Close();

                head.Save(directory + "\\head.bmp");

                FileStream my_info_stream = new FileStream(directory + "\\myself.info", FileMode.OpenOrCreate);
                StreamWriter sw2 = new StreamWriter(my_info_stream);
                sw2.WriteLine(my_name);
                sw2.WriteLine(my_comment);
                sw2.Close();
                my_info_stream.Close();
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
                else if (__stu_num == f.stu_num && !f.online)
                    return null;
            }
            return null;
        }
        private bool compare_friends(List<SKFriend> a, List<SKFriend> b)
        {
            if (a.Count != b.Count)
                return false;
            if (a == b)
                return true;
            if(a.Count == b.Count && a.Count == 0)
                return true;
            bool suc = true;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    if (a[i].stu_num == b[j].stu_num)
                        break;
                    else if (a[i].stu_num != b[j].stu_num && j == b.Count - 1)
                        return false;
            return suc;
        }

        public SKMsgMainForm master;
        SKClient clients = new SKClient();
        SKServer servers = new SKServer();
        public string my_stu_num;
        public string _my_name = "HelloWorld";
        public string _my_comment = "生于忧患，死于安乐";
        Bitmap _head = null;

        public string my_name
        {
            get
            {
                return _my_name;
            }
            set
            {
                _my_name = value;
                master.set_name(value);
                send_sync();
            }
        }
        public string my_comment
        {
            get
            {
                return _my_comment;
            }
            set
            {
                _my_comment = value;
                master.set_comment(value);
                send_sync();
            }
        }
        public Bitmap head
        {
            get
            {
                return _head;
            }
            set
            {
                _head = new Bitmap(value, new Size(10, 10));

                //master.pictureBox1.Image = _head;
                master.set_head(new Bitmap(_head, new Size(60, 60)));
                send_sync();
            }
        }
        Socket login_socket;
        Random random = new Random();
        SKFriend me;

        public List<SKFriend> friend_list = new List<SKFriend>();
        List<SKMsgWindow> window_list = new List<SKMsgWindow>();
        List<SKGroupMsgWindow> g_window_list = new List<SKGroupMsgWindow>();
        public class SKFriend
        {
            public SKFriend(string _stu_num, string _name, string _comment,Bitmap img = null)
            {
                stu_num = _stu_num;
                name = _name;
                comment = _comment;
                Img = img;
            }
            public string stu_num = "";
            public string _name = "";
            public string _comment = "";
            public string name
            {
                get
                {
                    if (_name == "")
                        return "无名氏";
                    else
                        return _name;
                }
                set
                {
                    _name = value;
                }
            }
            public string comment
            {
                get
                {
                    if (_comment == "")
                        return "这家伙很懒，没有留下签名哦";
                    else
                        return _comment;
                }
                set
                {
                    _comment = value;
                }
            }
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
            public override string ToString()
            {
                return show_name+"("+stu_num+")";
            }
        }
    }
}

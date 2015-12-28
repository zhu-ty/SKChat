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
                        if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
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
                        if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
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
                        if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
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
                        if (f != null && f.ip != null && f.ip.ToString() == msg_info.ip.ToString())
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
                master.refresh();
            };
            master.Invoke(receive_act, _msg_info);
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
                clients.SendNotFile(sit, tar_ip);
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
                    return clients.SendNotFile(smifi,tar_ip);
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
                    return clients.SendNotFile(sib,tar_ip);
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
                clients.SendFile(tar_ip, file_full_path, my_stu_num);
            }
            return false;
        }
        /// <summary>
        /// 只能被MainForm调用 否则会不同步
        /// </summary>
        /// <param name="add_stu_num"></param>
        /// <param name="add_stu_name"></param>
        /// <param name="add_stu_comment"></param>
        /// <returns></returns>
        public SKFriend add_friend(string add_stu_num, string add_stu_name = "", string add_stu_comment = "")
        {
            foreach (SKFriend ff in friend_list)
            {
                if (ff.stu_num == add_stu_num)
                    return ff;
            }
            SKFriend f = new SKFriend(add_stu_num, add_stu_name, add_stu_comment);
            friend_list.Add(f);
            refresh();
            return f;
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
                string __comment = sr.ReadLine();
                friend_list.Add(new SKFriend(__stu_num, __name, __comment));
            }
            sr.Close();
            friend_list_stream.Close();

            FileStream my_info_stream = new FileStream(directory + "\\myself.info", FileMode.OpenOrCreate);
            StreamReader sr2 = new StreamReader(my_info_stream);
            if (!sr2.EndOfStream)
                my_name = sr2.ReadLine();
            if (!sr2.EndOfStream)
                my_comment = sr2.ReadLine();
            sr2.Close();
            my_info_stream.Close();
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
                }
                sw.Close();
                friend_list_stream.Close();

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

        public SKMsgMainForm master;
        SKClient clients = new SKClient();
        SKServer servers = new SKServer();
        string my_stu_num;
        public string _my_name = "HelloWorld";
        public string _my_comment = "生于忧患，死于安乐";
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
            }
        }
        Socket login_socket;
        Random random = new Random();

        public List<SKFriend> friend_list = new List<SKFriend>();
        List<SKMsgWindow> window_list = new List<SKMsgWindow>();
        public class SKFriend
        {
            public SKFriend(string _stu_num, string _name, string _comment)
            {
                stu_num = _stu_num;
                name = _name;
                comment = _comment;
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
        }
    }
}

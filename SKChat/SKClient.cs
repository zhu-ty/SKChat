using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using SKChat;

namespace DA32ProtocolCsharp
{
    /// <summary>
    /// 运行在字节层与JSON层之间的Client
    /// 请注意，各常量值已经在且仅在Server给出定义。
    /// </summary>
    public class SKClient
    {
        /// <summary>
        /// 连接最长等待时间
        /// </summary>
        public const int max_connect_senconds = 1;
        /// <summary>
        /// Name最大长度
        /// </summary>
        public const int max_name_len = 100;
        /// <summary>
        /// text最大长度
        /// </summary>
        public const int max_text_len = 20000;

        #region old_version
        /*
        /// <summary>
        /// 发送一个Response信息
        /// <para>若为新的连接，尝试连接时间为10秒</para>
        /// </summary>
        /// <param name="id">包内id号</param>
        /// <param name="target_ip">目标IP，若不在已有socket中将会被创建</param>
        /// <param name="dt">时间戳</param>
        /// <returns>是否发送成功</returns>
        public bool SendResponse(int id,IPAddress target_ip,DateTime dt)
        {
            Socket c_now = null;
            client_lock.WaitOne();
            for (int i = 0; i < client_communication_sockets.Count; i++)
            {
                Socket c = client_communication_sockets[i];
                if (c == null)
                {
                    client_communication_sockets.Remove(c);
                    i--;
                    continue;
                }
                if (((IPEndPoint)(c.RemoteEndPoint)).Address.ToString() == target_ip.ToString())
                {
                    if (c.Connected == true)
                    {
                        c_now = c;
                        break;
                    }
                    else
                    {
                        client_communication_sockets.Remove(c);
                        c.Close();
                        i--;
                        continue;
                    }
                }
            }
            //新建连接
            if (c_now == null)
            {
                c_now = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult connect_result = c_now.BeginConnect(target_ip, SKServer.ListenPort, null, null);
                connect_result.AsyncWaitHandle.WaitOne(max_connect_senconds * 1000);//10s
                if (!connect_result.IsCompleted)
                {
                    c_now.Close();
                    client_lock.ReleaseMutex();
                    return false;
                }
                client_communication_sockets.Add(c_now);
            }
            byte[] send_byte;
            skmessage.set_send_mes(dt,id);//此举将会更新下一个待发包的id与time
            skmessage.encodemes(SKMessage.mestype.RESPONSE, out send_byte);
            long full_len = send_byte.Length + SKServer.head_byte_size + SKServer.end_byte_size;
            byte[] len_info = BitConverter.GetBytes(full_len);
            List<byte[]> send_byte_buffer = new List<byte[]>();
            send_byte_buffer.Add(SKServer.head_2_bytes);
            send_byte_buffer.Add(len_info);
            send_byte_buffer.Add(send_byte);
            send_byte_buffer.Add(SKServer.end_2_bytes);
            byte[] final_send = byte_connect(send_byte_buffer);
            try
            {
                c_now.Send(final_send);
            }
            catch(Exception e)
            {
                //连接已经关闭
                client_communication_sockets.Remove(c_now);
                if(c_now != null)
                    c_now.Close();
                client_lock.ReleaseMutex();
                return false;
            }
            client_lock.ReleaseMutex();
            return true;
        }

        /// <summary>
        /// 发送一个Exit信息，关闭连接
        /// <para>若为新的连接，将不会建立</para>
        /// </summary>
        /// <param name="id">包内id号</param>
        /// <param name="target_ip">目标IP，若不在已有socket中将会被创建</param>
        /// <param name="dt">时间戳</param>
        /// <returns>是否发送成功</returns>
        public bool SendExit(int id, IPAddress target_ip, DateTime dt)
        {
            Socket c_now = null;
            client_lock.WaitOne();
            for (int i = 0; i < client_communication_sockets.Count; i++)
            {
                Socket c = client_communication_sockets[i];
                if (c == null)
                {
                    client_communication_sockets.Remove(c);
                    i--;
                    continue;
                }
                if (((IPEndPoint)(c.RemoteEndPoint)).Address.ToString() == target_ip.ToString())
                {
                    if (c.Connected == true)
                    {
                        c_now = c;
                        break;
                    }
                    else
                    {
                        client_communication_sockets.Remove(c);
                        c.Close();
                        i--;
                        continue;
                    }
                }
            }
            if (c_now == null)
            {
                client_lock.ReleaseMutex();
                return true;
            }
            byte[] send_byte;
            skmessage.set_send_mes(dt, id);//此举将会更新下一个待发包的id与time
            skmessage.encodemes(SKMessage.mestype.EXIT, out send_byte);
            long full_len = send_byte.Length + SKServer.head_byte_size + SKServer.end_byte_size;
            byte[] len_info = BitConverter.GetBytes(full_len);
            List<byte[]> send_byte_buffer = new List<byte[]>();
            send_byte_buffer.Add(SKServer.head_2_bytes);
            send_byte_buffer.Add(len_info);
            send_byte_buffer.Add(send_byte);
            send_byte_buffer.Add(SKServer.end_2_bytes);
            byte[] final_send = byte_connect(send_byte_buffer);
            try
            {
                c_now.Send(final_send);
                //EXIT包已经发送，关闭连接
                client_communication_sockets.Remove(c_now);
                c_now.Close();
            }
            catch (Exception e)
            {
                //连接已经关闭
                client_communication_sockets.Remove(c_now);
                c_now.Close();
                client_lock.ReleaseMutex();
                return false;//虽然返回是false，但已经成功exit。
            }
            client_lock.ReleaseMutex();
            return true;
        }

        /// <summary>
        /// 发送一个text信息
        /// <para>name最大长度目前被规定为100</para>
        /// <para>text最大长度目前被规定为20000</para>
        /// <para>若为新的连接，尝试连接时间为10秒</para>
        /// </summary>
        /// <param name="id">包内id号</param>
        /// <param name="name">发送方昵称</param>
        /// <param name="text">发送内容</param>
        /// <param name="target_ip">目标IP</param>
        /// <param name="dt">时间戳</param>
        /// <returns>是否发送成功</returns>
        public bool SendText(int id, string name, string text, IPAddress target_ip, DateTime dt)
        {
            client_lock.WaitOne();
            if (name.Length > max_name_len || text.Length > max_text_len || name.Length == 0 || text.Length == 0)
                return false;
            Socket c_now = null;
            for(int i = 0;i < client_communication_sockets.Count;i++)
            {
                Socket c = client_communication_sockets[i];
                if (c == null)
                {
                    client_communication_sockets.Remove(c);
                    i--;
                    continue;
                }
                if (((IPEndPoint)(c.RemoteEndPoint)).Address.ToString() == target_ip.ToString())
                {
                    if (c.Connected == true)
                    {
                        c_now = c;
                        break;
                    }
                    else
                    {
                        client_communication_sockets.Remove(c);
                        c.Close();
                        i--;
                        continue;
                    }
                }
            }
            //新建连接
            if (c_now == null)
            {
                c_now = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult connect_result = c_now.BeginConnect(target_ip, SKServer.ListenPort, null, null);
                connect_result.AsyncWaitHandle.WaitOne(max_connect_senconds * 1000);//10s
                if (!connect_result.IsCompleted)
                {
                    c_now.Close();
                    client_lock.ReleaseMutex();
                    return false;
                }
                client_communication_sockets.Add(c_now);
            }
            byte[] send_byte;
            skmessage.set_send_textmes(name, text, dt, id);//此举将会更新下一个待发包的id与time
            skmessage.encodemes(SKMessage.mestype.TEXT, out send_byte);
            long full_len = send_byte.Length + SKServer.head_byte_size + SKServer.end_byte_size;
            byte[] len_info = BitConverter.GetBytes(full_len);
            List<byte[]> send_byte_buffer = new List<byte[]>();
            send_byte_buffer.Add(SKServer.head_2_bytes);
            send_byte_buffer.Add(len_info);
            send_byte_buffer.Add(send_byte);
            send_byte_buffer.Add(SKServer.end_2_bytes);
            byte[] final_send = byte_connect(send_byte_buffer);
            try
            {
                c_now.Send(final_send);
            }
            catch (Exception e)
            {
                //连接已经关闭
                client_communication_sockets.Remove(c_now);
                c_now.Close();
                client_lock.ReleaseMutex();
                return false;
            }
            client_lock.ReleaseMutex();
            return true;
        }
        */
        #endregion

        /// <summary>
        /// 全新的发送
        /// <para>发送非文件的内容</para>
        /// <para>暂不支持文件</para>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="target_ip"></param>
        /// <returns></returns>
        public bool SendNotFile(SKMsgInfoBase info, IPAddress target_ip, string tar_stu_num)
        {
            client_lock.WaitOne();
            if ((info.type == SKMsgInfoBase.mestype.TEXT || info.type== SKMsgInfoBase.mestype.GROUP_TEXT) &&
                (((SKMsgInfoText)info).text_pack.name.Length > max_name_len || 
                ((SKMsgInfoText)info).text_pack.text.Length > max_text_len || 
                ((SKMsgInfoText)info).text_pack.name.Length == 0 || 
                ((SKMsgInfoText)info).text_pack.text.Length == 0))
                return false;
            Socket c_now = null;
            for (int i = 0; i < client_communication_sockets.Count; i++)
            {
                Socket c = client_communication_sockets[i];
                if (c == null)
                {
                    client_communication_sockets.Remove(c);
                    i--;
                    continue;
                }
                if (((IPEndPoint)(c.RemoteEndPoint)).Address.ToString() == target_ip.ToString() && ((IPEndPoint)(c.RemoteEndPoint)).Port.ToString() == int.Parse(tar_stu_num.Substring(tar_stu_num.Length - 4)).ToString())
                {
                    if (c.Connected == true)
                    {
                        c_now = c;
                        break;
                    }
                    else
                    {
                        client_communication_sockets.Remove(c);
                        c.Close();
                        i--;
                        continue;
                    }
                }
            }
            //新建连接
            if (c_now == null && info.type != SKMsgInfoBase.mestype.EXIT)
            {
                c_now = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                c_now.SendBufferSize = SKServer.max_byte_once;
                IAsyncResult connect_result = c_now.BeginConnect(target_ip, int.Parse(tar_stu_num.Substring(tar_stu_num.Length - 4)), null, null);
                connect_result.AsyncWaitHandle.WaitOne(max_connect_senconds * 1000);//1s
                if (!connect_result.IsCompleted)
                {
                    c_now.Close();
                    client_lock.ReleaseMutex();
                    return false;
                }
                client_communication_sockets.Add(c_now);
            }
            else if (c_now == null)
            {
                client_lock.ReleaseMutex();
                return true;
            }

            byte[] send_byte;
            if (!skmessage.encodemes(info, out send_byte))
            {
                client_lock.ReleaseMutex();
                return false;
            }
            List<byte[]> send_byte_buffer = new List<byte[]>();
            send_byte_buffer.Add(SKServer.head_2_bytes);
            send_byte_buffer.Add(send_byte);
            send_byte_buffer.Add(SKServer.end_2_bytes);
            byte[] final_send = byte_connect(send_byte_buffer);
            try
            {
                c_now.Send(final_send);
            }
            catch (Exception)
            {
                //连接已经关闭
                client_communication_sockets.Remove(c_now);
                c_now.Close();
                client_lock.ReleaseMutex();
                return false;
            }
            client_lock.ReleaseMutex();
            return true;
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="target_ip"></param>
        /// <param name="file_full"></param>
        /// <param name="stu_num"></param>
        /// <returns></returns>
        public bool SendFile(IPAddress target_ip, string file_full, string stu_num, string tar_stu_num)
        {
            Thread file_thread = new Thread(SendFileThread);
            file_thread.IsBackground = true;
            ip_with_file_path this_object = new ip_with_file_path();
            this_object.ip = target_ip;
            this_object.stu_num = stu_num;
            this_object.file_full_path = file_full;
            this_object.t = file_thread;
            this_object.m = new Mutex();
            this_object.tar_stu_num = tar_stu_num;
            file_threads.Add(this_object);
            file_thread.Start(this_object);
            return true;
        }
        private void SendFileThread(object this_object)
        {
            try
            {
                SendFileDialog sfd = new SendFileDialog();
                ip_with_file_path ip_and_file_path = (ip_with_file_path)this_object;
                Socket send_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                send_socket.SendBufferSize = SKServer.max_byte_once;
                IAsyncResult connect_result = send_socket.BeginConnect(ip_and_file_path.ip,int.Parse(
                    ip_and_file_path.tar_stu_num.Substring(ip_and_file_path.tar_stu_num.Length - 4)), null, null);
                connect_result.AsyncWaitHandle.WaitOne(max_connect_senconds * 1000);//10s
                if (!connect_result.IsCompleted)
                {
                    send_socket.Close();
                    return;
                }
                if (!File.Exists(ip_and_file_path.file_full_path))
                    throw new Exception("文件不存在！");
                long size = (new FileInfo(ip_and_file_path.file_full_path)).Length;
                if (size > int.MaxValue)
                    throw new Exception("文件大小超过2G！");
                int size_i = (int)size;
                int max_fragment = (int)Math.Ceiling((double)size_i / SKServer.max_fragment_size);
                FileStream fs = new FileStream(ip_and_file_path.file_full_path, FileMode.Open);
                BinaryReader sr = new BinaryReader(fs);
                sfd.init(max_fragment, ip_and_file_path.stu_num);
                sfd.Show();
                for (int i = 0; i < max_fragment; i++)
                {
                    sfd.update(i);
                    int this_time_send_len = ((i == max_fragment - 1) && 
                        (size_i % SKServer.max_fragment_size == 0)) ? 
                        (size_i % SKServer.max_fragment_size) : SKServer.max_fragment_size;
                    byte[] to_send = new byte[this_time_send_len];
                    int len = sr.Read(to_send, 0, this_time_send_len);
                    SKMsgInfoFile smif = new SKMsgInfoFile();
                    smif.id = i;
                    smif.max_fragment = max_fragment;
                    smif.this_fragment = i;
                    smif.timestamp = DateTime.Now;
                    smif.type = SKMsgInfoBase.mestype.FILE;
                    smif.stu_num = ip_and_file_path.stu_num;
                    byte[] send_byte;
                    if (!skmessage.encodemes(smif, out send_byte, to_send))
                    {
                        throw new Exception("转码失败，中断发送");
                    }
                    List<byte[]> send_byte_buffer = new List<byte[]>();
                    send_byte_buffer.Add(SKServer.head_2_bytes);
                    send_byte_buffer.Add(send_byte);
                    send_byte_buffer.Add(SKServer.end_2_bytes);
                    byte[] final_send = byte_connect(send_byte_buffer);
                    send_socket.Send(final_send);
                    //Thread.Sleep(10);
                    byte[] small_response = new byte[2];
                    send_socket.Receive(small_response);
                    if (small_response[0] == 0x0D && small_response[1] == 0x0A)
                        continue;
                    else
                        Thread.Sleep(1000);
                }
                sr.Close();
                fs.Close();
                SKMsgInfoBase exit_info = new SKMsgInfoBase();
                exit_info.id = 1;
                exit_info.stu_num = ip_and_file_path.stu_num;
                exit_info.timestamp = DateTime.Now;
                exit_info.type = SKMsgInfoBase.mestype.EXIT;
                byte[] send_byte2;
                if (!skmessage.encodemes(exit_info, out send_byte2))
                {
                    throw new Exception("转码失败，中断发送");
                }
                List<byte[]> send_byte_buffer2 = new List<byte[]>();
                send_byte_buffer2.Add(SKServer.head_2_bytes);
                send_byte_buffer2.Add(send_byte2);
                send_byte_buffer2.Add(SKServer.end_2_bytes);
                byte[] final_send2 = byte_connect(send_byte_buffer2);
                send_socket.Send(final_send2);
                Thread.Sleep(10);
                //send_socket.Close();
                ip_and_file_path.m.WaitOne();
                file_threads.Remove(ip_and_file_path);
                ip_and_file_path.m.ReleaseMutex();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("文件传输中断");
                return;
            }
            return;
        }
        public void file_abort(IPAddress target_ip)
        {
            for (int i = 0; i < file_threads.Count; i++)
            {
                ip_with_file_path iwfp = file_threads[i];
                iwfp.m.WaitOne();
                if (iwfp.ip.ToString() == target_ip.ToString())
                {
                    iwfp.t.Abort();
                    file_threads.Remove(iwfp);
                    i--;
                }
                iwfp.m.ReleaseMutex();
                
            }
        }
        /// <summary>
        /// （更新）强行向所有目标发送exit
        /// </summary>
        /// <returns>是否都发送成功了</returns>
        public bool SendExitToAll()
        {
            bool ret = true;
            client_lock.WaitOne();
            for(int i = 0;i < client_communication_sockets.Count;i++)
            {
                Socket c_now = client_communication_sockets[i];
                if (c_now == null)
                {
                    client_communication_sockets.Remove(c_now);
                    i--;
                    continue;
                }
                SKMsgInfoBase exit_info = new SKMsgInfoBase();
                exit_info.id = 0;
                exit_info.timestamp = DateTime.Now;
                exit_info.type = SKMsgInfoBase.mestype.EXIT;
                byte[] send_byte;
                skmessage.encodemes(exit_info, out send_byte);
                List<byte[]> send_byte_buffer = new List<byte[]>();
                send_byte_buffer.Add(SKServer.head_2_bytes);
                send_byte_buffer.Add(send_byte);
                send_byte_buffer.Add(SKServer.end_2_bytes);
                byte[] final_send = byte_connect(send_byte_buffer);
                try
                {
                    c_now.Send(final_send);
                    //EXIT包已经发送，关闭连接
                    client_communication_sockets.Remove(c_now);
                    c_now.Close();
                }
                catch (Exception e)
                {
                    //连接已经关闭
                    client_communication_sockets.Remove(c_now);
                    c_now.Close();
                    ret = false;
                }
            }
            client_lock.ReleaseMutex();
            return ret;
        }

        private struct ip_with_file_path
        {
            public IPAddress ip;
            public string file_full_path;
            public string stu_num;
            public Thread t;
            public Mutex m;
            public string tar_stu_num;
        }
        private List<ip_with_file_path> file_threads = new List<ip_with_file_path>();
        private Mutex client_lock = new Mutex();
        private byte[] byte_connect(List<byte[]> btlist)
        {
            int length = 0;
            int now = 0;
            for (int i = 0; i < btlist.Count; i++)
                length += btlist[i].Length;
            byte[] ret = new byte[length];
            for (int i = 0; i < btlist.Count; i++)
            {
                Array.Copy(btlist[i], 0, ret, now, btlist[i].Length);
                now += btlist[i].Length;
            }
            return ret;
        }
        private List<Socket> client_communication_sockets = new List<Socket>();
        private SKMessage skmessage = new SKMessage();
    }
}

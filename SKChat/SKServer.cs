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
    /// 运行在字节层与JSON层之间的Server
    /// </summary>
    public class SKServer
    {
        /// <summary>
        /// 连接监听端口
        /// </summary>
        public static int ListenPorts = 3232;
        /// <summary>
        /// 最大同时连接数量
        /// </summary>
        public const int max_connection = 100;
        /// <summary>
        /// 最大单次收取字节数
        /// </summary>
        public const int max_byte_once = 500000;
        /// <summary>
        /// 包前缀长度
        /// </summary>
        public const int head_byte_size = 10;
        /// <summary>
        /// 包后缀长度
        /// </summary>
        public const int end_byte_size = 2;
        /// <summary>
        /// 包固定前缀的内容
        /// </summary>
        public static readonly byte[] head_2_bytes = { 0x32, 0xA0 };
        /// <summary>
        /// 包固定后缀内容
        /// </summary>
        public static readonly byte[] end_2_bytes = { 0x42,0xF0};
        /// <summary>
        /// 文件片段大小（字节数）
        /// </summary>
        public const int max_fragment_size = 1000;
        /// <summary>
        /// 回调事件
        /// </summary>
        public event OnServerCall ServerCall;
        public delegate void OnServerCall(object sender, SKMsgInfoBase e,byte[] file_piece = null);
        /// <summary>
        /// 开始在3232上监听，请确保回调事件已经注册
        /// </summary>
        /// <returns></returns>
        public bool start_listening()
        {
            bool ret = false;
            server_lock.WaitOne();
            {
                if (started || ServerCall == null)
                    ret = false;
                else
                {
                    try
                    {
                        server_listen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
                        foreach (IPAddress ip in ips)
                        {
                            if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                            {
                                server_listen_socket.Bind(new IPEndPoint(ip, ListenPorts));
                                break;
                            }
                        }
                        server_listen_socket.ReceiveBufferSize = max_byte_once;
                        server_listen_socket.Listen(max_connection);
                        started = true;
                        Thread listenthread = new Thread(main_listener);
                        listenthread.IsBackground = true;
                        listenthread.Start();
                        ret = true;
                    }
                    catch (Exception)
                    {
                        started = false;
                        ret = false;
                    }
                }
            }
            server_lock.ReleaseMutex();
            return ret;
        }

        public void abort(IPAddress from_ip_address)
        {
            server_lock.WaitOne();
            for (int i = 0; i < server_communication_sockets.Count; i++)
            {
                if (server_communication_sockets[i] != null &&
                    ((IPEndPoint)server_communication_sockets[i].RemoteEndPoint).Address.ToString() == from_ip_address.ToString())
                {
                    server_communication_sockets[i].Close();
                    server_communication_sockets.Remove(server_communication_sockets[i]);
                    i--;
                }
            }
            server_lock.ReleaseMutex();
        }
        /// <summary>
        /// 结束接听和所有的连接
        /// </summary>
        /// <returns></returns>
        public bool stop_listening()
        {
            server_lock.WaitOne();
            started = false;
            server_communication_sockets.Clear();
            server_lock.ReleaseMutex();
            return true;
        }

        private void main_listener()
        {
            while (true)
            {
                server_lock.WaitOne();
                if (!started)
                    break;
                server_lock.ReleaseMutex();
                Socket c;
                c = server_listen_socket.Accept();
                server_lock.WaitOne();
                server_communication_sockets.Add(c);
                Thread new_communication_thread = new Thread(communication);
                new_communication_thread.IsBackground = true;
                new_communication_thread.Start(c);
                server_lock.ReleaseMutex();
            }
            server_lock.ReleaseMutex();
        }

        private void communication(object RecvServer)
        {
            Socket c = (Socket)RecvServer;
            //c.ReceiveTimeout = 10 * 60 * 1000;//10min
            c.ReceiveBufferSize = max_byte_once;
            IPAddress this_ip = ((IPEndPoint)(c.RemoteEndPoint)).Address;
            while (true)
            {
                try
                {
                    server_lock.WaitOne();
                    if (!started)
                    {
                        server_communication_sockets.Remove(c);
                        if (c != null)
                            c.Close();
                        server_lock.ReleaseMutex();
                        break;
                    }
                    server_lock.ReleaseMutex();
                    byte[] head = new byte[head_byte_size];
                    int len = c.Receive(head);
                    if (len == 0)
                        throw (new Exception());
                    if (len == head_byte_size && head[0] == head_2_bytes[0] && head[1] == head_2_bytes[1])
                    {
                        int len_then = BitConverter.ToInt32(head, 2) + BitConverter.ToInt32(head, 6) - head_byte_size - end_byte_size;
                        byte[] then = new byte[len_then];
                        byte[] end = new byte[end_byte_size];
                        int len_recv = c.Receive(then);
                        int end_recv = c.Receive(end);
                        if (len_recv != len_then || end_recv != end_byte_size || end[0] != end_2_bytes[0] || end[1] != end_2_bytes[1])
                            continue;
                        byte[] head_len = new byte[8];
                        Array.Copy(head, 2, head_len, 0, 8);
                        List<byte[]> to_connect = new List<byte[]>();
                        to_connect.Add(head_len);
                        to_connect.Add(then);
                        byte[] new_then = byte_connect(to_connect);
                        byte[] file_fra;
                        SKMsgInfoBase eventarg;
                        if (skmessage.decodemes(new_then, out eventarg, out file_fra))
                        {
                            eventarg.ip = ((IPEndPoint)(c.RemoteEndPoint)).Address;
                            if (eventarg.type == SKMsgInfoBase.mestype.FILE)
                            {
                                ServerCall(this, eventarg, file_fra);
                                c.Send(new byte[2] { 0x0D, 0x0A });
                            }
                            else
                            {
                                if (eventarg.type == SKMsgInfoBase.mestype.EXIT)
                                {
                                    server_lock.WaitOne();
                                    server_communication_sockets.Remove(c);
                                    server_lock.ReleaseMutex();
                                    if (c.Connected)
                                    {
                                        c.Close();
                                        //server_lock.ReleaseMutex();
                                        break;
                                    }
                                }
                                ServerCall(this, eventarg);
                            }
                        }
                        #region old_version
                        /*
                        //if (skmessage.decodemes(then, out type))
                        //{
                        //    switch (type)
                        //    {
                        //        case SKMessage.mestype.EXIT:
                        //            {
                        //                SKMsgInfoBase exit_event = new SKMsgInfoBase();
                        //                exit_event.type = SKMsgInfoBase.mestype.EXIT;
                        //                exit_event.ip = ((IPEndPoint)(c.RemoteEndPoint)).Address;
                        //                ServerCall(this, exit_event);
                        //                server_communication_sockets.Remove(c);
                        //                c.Close();
                        //                break;
                        //            }
                        //        case SKMessage.mestype.RESPONSE:
                        //            {
                        //                SKMsgInfoBase response_event = new SKMsgInfoBase();
                        //                response_event.type = SKMsgInfoBase.mestype.RESPONSE;
                        //                response_event.ip = ((IPEndPoint)(c.RemoteEndPoint)).Address;
                        //                ServerCall(this, response_event);
                        //                break;
                        //            }
                        //        case SKMessage.mestype.TEXT:
                        //            {
                        //                SKMsgInfoText text_event = new SKMsgInfoText();
                        //                text_event.type = SKMsgInfoBase.mestype.TEXT;
                        //                text_event.ip = ((IPEndPoint)(c.RemoteEndPoint)).Address;
                        //                text_event.text_pack = skmessage.get_last_text();
                        //                ServerCall(this, text_event);
                        //                break;
                        //            }
                        //        default:
                        //            {
                        //                break;
                        //            }
                        //    }
                        //}
                        */
                        #endregion
                        //server_lock.ReleaseMutex();
                    }
                }
                catch (Exception e)//超时或Socket已关闭
                {
                    //server_lock.ReleaseMutex();
                    server_lock.WaitOne();
                    SKMsgInfoBase exit_event = new SKMsgInfoBase();
                    exit_event.type = SKMsgInfoBase.mestype.EXIT;
                    exit_event.ip = this_ip;
                    ServerCall(this, exit_event);
                    server_lock.ReleaseMutex();

                    if (c != null)
                        c.Close();
                    break;
                }
            }
        }

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
        private SKMessage skmessage = new SKMessage();
        private bool started = false;//线程共享
        private Mutex server_lock = new Mutex();//线程共享
        private Socket server_listen_socket;
        private List<Socket> server_communication_sockets = new List<Socket>();
    }
}

﻿using System;
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
        public const int max_connect_senconds = 10;
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
        public bool SendNotFile(SKMsgInfoBase info, IPAddress target_ip)
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
            if (c_now == null && info.type != SKMsgInfoBase.mestype.EXIT)
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

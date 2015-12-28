using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using SKChat;

namespace DA32ProtocolCsharp
{
    /// <summary>
    /// 运行在json层与信息层之间的类，将剔除头尾和长度信息的字节层信息翻译成信息层。
    /// </summary>
    public class SKMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        //public enum mestype { TEXT, RESPONSE, EXIT, UNDEFINED };
        /// <summary>
        /// 一个text包的data内容
        /// </summary>
        public struct textmes
        {
            public string name;
            public string text;
        }

        /// <summary>
        /// 构造函数，注意到内部的两个text_mes都只有默认值
        /// </summary>
        public SKMessage()
        {
            last_textmes.name = "DA32";
            last_textmes.text = "Haven't set yet.";
            send_textmes.name = "DA32";
            send_textmes.text = "Haven't set yet.";
        }
        /// <summary>
        /// 全新的decode，将去头去尾的bytes放进来decode即可
        /// </summary>
        /// <param name="full_byte">去头去尾后的整个报文</param>
        /// <param name="info_base">返回值</param>
        /// <param name="file_fragment">若为文件类型报文，将携带文件片段信息</param>
        /// <returns></returns>
        public bool decodemes(byte[] full_byte, out SKMsgInfoBase info_base, out byte[] file_fragment)
        {
            file_fragment = null;
            info_base = null;
            bool ret = false;
            try
            {
                int json_len = BitConverter.ToInt32(full_byte, 0) - 12;
                int another_len = BitConverter.ToInt32(full_byte, 4);
                byte[] json_bytes = new byte[json_len];
                Array.Copy(full_byte, 8, json_bytes, 0, json_len);
                JObject json_object = JObject.Parse(Encoding.UTF8.GetString(json_bytes));
                int id = (int)json_object["id"];
                string type_s = (string)json_object["type"];
                string time_s = (string)json_object["time"];
                string md5 = (string)json_object["md5"];
                string stu_num = (string)json_object["stu_num"];
                md5 = md5.ToLower();
                SKMsgInfoBase.mestype type = get_type(type_s);
                switch (type)
                {
                    case SKMsgInfoBase.mestype.EXIT:
                    case SKMsgInfoBase.mestype.RESPONSE:
                    case SKMsgInfoBase.mestype.FRIEND_INVITE:
                        {
                            info_base = new SKMsgInfoBase();
                            info_base.stu_num = stu_num;
                            info_base.id = id;
                            info_base.type = type;
                            info_base.verified = true;
                            //暂时关闭了md5校验……
                            info_base.timestamp = DateTime.ParseExact(time_s, "yyyy.MM.dd HH:mm:ss", null);
                            break;
                        }
                    case SKMsgInfoBase.mestype.FILE:
                        {
                            info_base = new SKMsgInfoFile();
                            info_base.stu_num = stu_num;
                            SKMsgInfoFile info_file = (SKMsgInfoFile)info_base;
                            info_file.id = id;
                            info_file.type = type;
                            info_file.verified = true;
                            //暂时关闭了md5校验……
                            info_file.timestamp = DateTime.ParseExact(time_s, "yyyy.MM.dd HH:mm:ss", null);
                            info_file.max_fragment = (int)json_object["data"]["max_fragment"];
                            info_file.this_fragment = (int)json_object["data"]["this_fragment"];
                            if (another_len != 0)
                            {
                                file_fragment = new byte[another_len];
                                Array.Copy(full_byte, 8 + json_len, file_fragment, 0, another_len);
                            }
                            break;
                        }
                    case SKMsgInfoBase.mestype.FILE_INVITE:
                        {
                            info_base = new SKMsgInfoFileInvite();
                            info_base.stu_num = stu_num;
                            SKMsgInfoFileInvite info_file_invite = (SKMsgInfoFileInvite)info_base;
                            info_file_invite.id = id;
                            info_file_invite.type = type;
                            info_file_invite.verified = true;
                            //暂时关闭了md5校验……
                            info_file_invite.timestamp = DateTime.ParseExact(time_s, "yyyy.MM.dd HH:mm:ss", null);
                            info_file_invite.size = (int)json_object["data"]["size"];
                            info_file_invite.file_name = (string)json_object["data"]["file_name"];
                            break;
                        }
                    case SKMsgInfoBase.mestype.GROUP_TEXT:
                        {
                            info_base = new SKMsgInfoGroupText();
                            info_base.stu_num = stu_num;
                            SKMsgInfoGroupText info_group_text = (SKMsgInfoGroupText)info_base;
                            info_group_text.id = id;
                            info_group_text.type = type;
                            info_group_text.verified = true;
                            //暂时关闭了md5校验……
                            info_group_text.timestamp = DateTime.ParseExact(time_s, "yyyy.MM.dd HH:mm:ss", null);
                            int list_len = (int)json_object["data"]["list_len"];
                            info_group_text.text_pack.name = (string)json_object["data"]["name"];
                            info_group_text.text_pack.text = (string)json_object["data"]["text"];
                            for (int i = 0; i < list_len; i++)
                            {
                                info_group_text.stu_num_list.Add((string)json_object["data"]["stu_num_list"][i]);
                            }
                            break;
                        }
                    case SKMsgInfoBase.mestype.TEXT:
                        {
                            info_base = new SKMsgInfoText();
                            info_base.stu_num = stu_num;
                            SKMsgInfoText info_text = (SKMsgInfoText)info_base;
                            info_text.id = id;
                            info_text.type = type;
                            info_text.verified = true;
                            //暂时关闭了md5校验……
                            info_text.timestamp = DateTime.ParseExact(time_s, "yyyy.MM.dd HH:mm:ss", null);
                            info_text.text_pack.name = (string)json_object["data"]["name"];
                            info_text.text_pack.text = (string)json_object["data"]["text"];
                            break;
                        }
                    default:
                        {
                            ret = false;
                            break;
                        }
                }
                ret = true;
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
        /// <summary>
        /// 全新的encode，将SKMsgInfoBase放进来即可
        /// </summary>
        /// <param name="info_base">要发送的消息</param>
        /// <param name="jsonb">返回的字节数组</param>
        /// <param name="file_fragment">可选，文件片段</param>
        /// <returns></returns>
        public bool encodemes(SKMsgInfoBase info_base, out byte[] jsonb,byte[] file_fragment = null)
        {
            int len1 = 0;
            jsonb = null;
            bool ret = false;
            try
            {
                string s = string.Empty;
                JObject j = new JObject();
                s += "{";
                s += "\"else\":{},";
                s += "\"id\":" + info_base.id.ToString() + ",";
                s += "\"time\":\"" + info_base.timestamp.ToString("yyyy.MM.dd HH:mm:ss") + "\",";
                s += "\"stu_num\":\"" + info_base.stu_num + "\",";
                switch (info_base.type)
                {
                    case SKMsgInfoBase.mestype.EXIT:
                        {
                            s += "\"type\":\"exit\",";
                            s += "\"data\":{},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_base.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("exit"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_base.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.FILE:
                        {
                            SKMsgInfoFile info_file = (SKMsgInfoFile)info_base;
                            s += "\"type\":\"file\",";
                            s += "\"data\":{";
                            s += "\"max_fragment\":" + info_file.max_fragment.ToString() + ",";
                            s += "\"this_fragment\":" + info_file.this_fragment.ToString();
                            s += "},";
                            s += "\"md5\":\"" + getmd5(file_fragment) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.FILE_INVITE:
                        {
                            SKMsgInfoFileInvite info_file_invite = (SKMsgInfoFileInvite)info_base;
                            s += "\"type\":\"file_invite\",";
                            s += "\"data\":{";
                            s += "\"size\":" + info_file_invite.size.ToString() + ",";
                            s += "\"file_name\":\"" + info_file_invite.file_name.ToString()+"\"";
                            s += "},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_base.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("file_invite"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_base.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.GROUP_TEXT:
                        {
                            SKMsgInfoGroupText info_group_text = (SKMsgInfoGroupText)info_base;
                            s += "\"type\":\"group_text\",";
                            s += "\"data\":{";
                            s += "\"name\":\"" + add_special_char(info_group_text.text_pack.name) + "\",";
                            s += "\"text\":\"" + add_special_char(info_group_text.text_pack.text) + "\",";
                            s += "\"stu_num_list\":[";
                            for (int i = 0; i < info_group_text.stu_num_list.Count - 1; i++)
                                s += info_group_text.stu_num_list[i] + ",";
                            s += info_group_text.stu_num_list[info_group_text.stu_num_list.Count - 1];
                            s += "]";
                            s += "},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_group_text.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("text"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_group_text.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_group_text.text_pack.name));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_group_text.text_pack.text));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.RESPONSE:
                        {
                            s += "\"type\":\"response\",";
                            s += "\"data\":{},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_base.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("response"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_base.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.FRIEND_INVITE:
                        {
                            s += "\"type\":\"friend_invite\",";
                            s += "\"data\":{},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_base.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("friend_invite"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_base.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.TEXT:
                        {
                            SKMsgInfoText info_text = (SKMsgInfoText)info_base;
                            s += "\"type\":\"text\",";
                            s += "\"data\":{";
                            s += "\"name\":\"" + add_special_char(info_text.text_pack.name) + "\",";
                            s += "\"text\":\"" + add_special_char(info_text.text_pack.text) + "\"";
                            s += "},";
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(info_text.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("text"));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_text.timestamp.ToString("yyyy.MM.dd HH:mm:ss")));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_text.text_pack.name));
                            con_byte.Add(Encoding.UTF8.GetBytes(info_text.text_pack.text));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            ret = true;
                            break;
                        }
                    default:
                        {
                            ret = false;
                            jsonb = null;
                            break;
                        }
                }
                if (ret)
                {
                    List<byte[]> final = new List<byte[]>();
                    byte[] first_part = Encoding.UTF8.GetBytes(s);
                    len1 = first_part.Length + 12;
                    if (info_base.type == SKMsgInfoBase.mestype.FILE)
                    {
                        byte[] len1b = BitConverter.GetBytes(len1);
                        byte[] len2b = BitConverter.GetBytes(file_fragment.Length);
                        final.Add(len1b);
                        final.Add(len2b);
                        final.Add(first_part);
                        final.Add(file_fragment);
                        jsonb = byte_connect(final);
                    }
                    else
                    {
                        byte[] len1b = BitConverter.GetBytes(len1);
                        byte[] len2b = BitConverter.GetBytes(0);
                        final.Add(len1b);
                        final.Add(len2b);
                        final.Add(first_part);
                        jsonb = byte_connect(final);
                    }
                }
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
        #region old_version
        /*
        ///<summary>
        ///当bool=true且type为TEXT时，更新last_textmes
        /// </summary>
        public bool decodemes(byte[] jsonb, out SKMsgInfoBase.mestype type)
        {
            bool ret = false;
            type = SKMsgInfoBase.mestype.UNDEFINED;
            try
            {
                string jsonmes = Encoding.UTF8.GetString(jsonb);
                JObject jsonobject = JObject.Parse(jsonmes);
                int id = (int)jsonobject["id"];
                string type_s = (string)jsonobject["type"];
                string time_s = (string)jsonobject["time"];
                string md5 = (string)jsonobject["md5"];
                md5 = md5.ToLower();
                type = get_type(type_s);
                switch (type)
                {
                    case SKMsgInfoBase.mestype.RESPONSE:
                    case SKMsgInfoBase.mestype.EXIT:
                        {
                            List<byte[]> con_byte = new List<byte[]>();
                            con_byte.Add(BitConverter.GetBytes(id));
                            con_byte.Add(Encoding.UTF8.GetBytes(type_s));
                            con_byte.Add(Encoding.UTF8.GetBytes(time_s));
                            if (md5_verification(byte_connect(con_byte),md5))
                                ret = true;
                            else
                            {
                                type = SKMsgInfoBase.mestype.UNDEFINED;
                                ret = false;
                            }
                            break;
                        }
                    case SKMsgInfoBase.mestype.TEXT:
                        {
                            List<byte[]> con_byte = new List<byte[]>();
                            string data_name = (string)jsonobject["data"]["name"];
                            string data_text_utf8 = (string)jsonobject["data"]["text"];
                            con_byte.Add(BitConverter.GetBytes(id));
                            con_byte.Add(Encoding.UTF8.GetBytes(type_s));
                            con_byte.Add(Encoding.UTF8.GetBytes(time_s));
                            con_byte.Add(Encoding.UTF8.GetBytes(data_name));
                            con_byte.Add(Encoding.UTF8.GetBytes(data_text_utf8));
                            if (md5_verification(byte_connect(con_byte), md5))
                            {
                                last_textmes.name = data_name;
                                last_textmes.text = data_text_utf8;
                                ret = true;
                            }
                            else
                            {
                                type = SKMsgInfoBase.mestype.UNDEFINED;
                                ret = false;
                            }
                            break;
                        }
                    default:
                        {
                            type = SKMsgInfoBase.mestype.UNDEFINED;
                            ret = false;
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                ret = false;
            }
            return ret;
        }
        ///<summary>
        ///当bool=true且type为TEXT时，从send_textmes中拿取信息生成jsonb，否则直接生成其他的type
        ///<para>生成其他的type的时候，会利用到send_textmes中的id与Time信息。</para>
        /// </summary>
        public bool encodemes(SKMsgInfoBase.mestype type, out byte[] jsonb)
        {
            bool ret = false;
            jsonb = null;
            try
            {
                string s = string.Empty;
                s += "{";
                //s += "\"id\":" + send_textmes.id.ToString() + ",";
                switch (type)
                {
                    case SKMsgInfoBase.mestype.EXIT:
                        {
                            s += "\"type\":\"exit\",";
                            //s += "\"time\":\"" + send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss") + "\",";
                            s += "\"else\":{},";
                            s += "\"data\":{},";
                            List<byte[]> con_byte = new List<byte[]>();
                            //con_byte.Add(BitConverter.GetBytes(send_textmes.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("exit"));
                            //con_byte.Add(Encoding.UTF8.GetBytes(send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            jsonb = Encoding.UTF8.GetBytes(s);
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.RESPONSE:
                        {
                            s += "\"type\":\"response\",";
                            //s += "\"time\":\"" + send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss") + "\",";
                            s += "\"else\":{},";
                            s += "\"data\":{},";
                            List<byte[]> con_byte = new List<byte[]>();
                            //con_byte.Add(BitConverter.GetBytes(send_textmes.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("response"));
                            //con_byte.Add(Encoding.UTF8.GetBytes(send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss")));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            jsonb = Encoding.UTF8.GetBytes(s);
                            ret = true;
                            break;
                        }
                    case SKMsgInfoBase.mestype.TEXT:
                        {
                            s += "\"type\":\"text\",";
                            //s += "\"time\":\"" + send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss") + "\",";
                            s += "\"else\":{},";
                            s += "\"data\":{";
                            s += "\"name\":\"" + add_special_char(send_textmes.name) + "\",";
                            s += "\"text\":\"" + add_special_char(send_textmes.text) + "\"";
                            s += "},";
                            List<byte[]> con_byte = new List<byte[]>();
                            //con_byte.Add(BitConverter.GetBytes(send_textmes.id));
                            con_byte.Add(Encoding.UTF8.GetBytes("text"));
                            //con_byte.Add(Encoding.UTF8.GetBytes(send_textmes.time.ToString("yyyy.MM.dd HH:mm:ss")));
                            con_byte.Add(Encoding.UTF8.GetBytes(send_textmes.name));
                            con_byte.Add(Encoding.UTF8.GetBytes(send_textmes.text));
                            s += "\"md5\":\"" + getmd5(byte_connect(con_byte)) + "\"";
                            s += "}";
                            jsonb = Encoding.UTF8.GetBytes(s);
                            ret = true;
                            break;
                        }
                    default:
                        {
                            ret = false;
                            jsonb = null;
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                ret = false;
                jsonb = null;
            }
            return ret;
        }
        ///<summary>
        ///获得上一个译码后的消息
        /// </summary>
        public textmes get_last_text()
        {
            return last_textmes;
        }

        /// <summary>
        /// 设置下一个将要编码的消息，自动打时间戳
        /// <para>调用此函数将不会更改上一个待发送消息的Name和text，但仍旧会更新id与time</para>
        /// </summary>
        /// <param name="id">ID号</param>
        /// <returns>是否设置成功</returns>
        public bool set_send_mes(int id = 1)
        {
            return true;
        }

        /// <summary>
        /// 设置下一个将要编码的消息，手动打时间戳
        /// <para>调用此函数将不会更改上一个待发送消息的Name和text，但仍旧会更新id与time</para>
        /// </summary>
        /// <param name="dt">时间戳</param>
        /// <param name="id">ID号</param>
        /// <returns>是否设置成功</returns>
        public bool set_send_mes(DateTime dt, int id = 1)
        {
            return true;
        }
        /// <summary>
        /// 设置下一个将要编码的消息，自动打时间戳
        /// </summary>
        /// <param name="name">发送方的名字</param>
        /// <param name="text">待编码的消息</param>
        /// <param name="id">ID号</param>
        /// <returns>是否设置成功</returns>
        public bool set_send_textmes(string name, string text, int id = 1)
        {
            send_textmes.text = text;
            send_textmes.name = name;
            return true;
        }
        /// <summary>
        /// 设置下一个将要编码的消息，手动打时间戳
        /// </summary>
        /// <param name="name">发送方的名字</param>
        /// <param name="text">待编码的消息</param>
        /// <param name="dt">时间戳</param>
        /// <param name="id">ID号</param>
        /// <returns>是否设置成功</returns>
        public bool set_send_textmes(string name, string text,DateTime dt, int id = 1)
        {
            send_textmes.text = text;
            send_textmes.name = name;
            return true;
        }
        */
        #endregion
        private SKMsgInfoBase.mestype get_type(string s)
        {
            if (s.ToLower() == "text")
                return SKMsgInfoBase.mestype.TEXT;
            else if (s.ToLower() == "response")
                return SKMsgInfoBase.mestype.RESPONSE;
            else if (s.ToLower() == "exit")
                return SKMsgInfoBase.mestype.EXIT;
            else if (s.ToLower() == "group_text")
                return SKMsgInfoBase.mestype.GROUP_TEXT;
            else if (s.ToLower() == "file")
                return SKMsgInfoBase.mestype.FILE;
            else if (s.ToLower() == "file_invite")
                return SKMsgInfoBase.mestype.FILE_INVITE;
            else if (s.ToLower() == "friend_invite")
                return SKMsgInfoBase.mestype.FRIEND_INVITE;
            else
                return SKMsgInfoBase.mestype.UNDEFINED;
        }
        private bool md5_verification(byte[] bt, string md5_v)
        {
            MD5 mymd5 = new MD5CryptoServiceProvider();
            byte[] md5o = mymd5.ComputeHash(bt);
            string mymd5_s = BitConverter.ToString(md5o).Replace("-", "");
            if (md5_v.ToLower() == mymd5_s.ToLower())
                return true;
            else
                return false;
        }
        private string getmd5(byte[] bt)
        {
            MD5 mymd5 = new MD5CryptoServiceProvider();
            byte[] md5o = mymd5.ComputeHash(bt);
            return (BitConverter.ToString(md5o).Replace("-", "").ToLower());
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
        private string add_special_char(string origin)
        {
            string ret = origin;
            ret = ret.Replace("\\", "\\\\"); 
            ret = ret.Replace("\n", "\\n");
            ret = ret.Replace("\r", "\\r");
            ret = ret.Replace("\"", "\\\"");
            ret = ret.Replace("\'", "\\\'");
            return ret;
        }
        private textmes last_textmes;
        private textmes send_textmes;
    }
}

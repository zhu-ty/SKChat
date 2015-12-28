using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace SKChat
{
    public class SKMsgInfoBase
    {
        public enum mestype
        {
            TEXT,
            RESPONSE,
            EXIT,
            GROUP_TEXT,
            FILE,
            FILE_INVITE,
            FRIEND_INVITE,
            UNDEFINED
        }
        public mestype type = mestype.UNDEFINED;
        public bool verified = false;
        public int id;
        public DateTime timestamp;
        public IPAddress ip;
        public string stu_num;
    }
    public class SKMsgInfoText : SKMsgInfoBase
    {
        //public string text;
        //public string name;
        public DA32ProtocolCsharp.SKMessage.textmes text_pack;
    }
    public class SKMsgInfoGroupText : SKMsgInfoText
    {
        public List<string> stu_num_list = new List<string>();
    }
    public class SKMsgInfoFile : SKMsgInfoBase
    {
        public int max_fragment;
        public int this_fragment;
    }
    public class SKMsgInfoFileInvite : SKMsgInfoBase
    {
        public int size;
        public string file_name;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AutoToolHelper
{
    class UDPSocket
    {
        public const int UDP_DATA_LEN=8192;
        public bool m_bInit=false;
        public int m_nUDPPort=0;
        public StringBuilder m_sendData=new StringBuilder();
        public StringBuilder m_recvData = new StringBuilder();
        public enum TRANSTYPE
        {
            TRANS_NONE = 0,				//初始状态
            TRANS_REQUEST_ORDER = 1,				//机器人请求订单数据					( ROBOT -> RC2 )
            TRANS_ORDER_DATA = 2,				//RC2将订单数据传输给机器人				( RC2 -> ROBOT )
            TRANS_ORDER_DATA_RET = 3,				//机器人返回收到订单数据的确认信息		( ROBOT -> RC2 )

            TRANS_GAME_DATA = 10,				//机器人返回订单的游戏数据				( ROBOT -> RC2 ) 
            TRANS_GAME_DATA_RET = 11,				//RC2确认收到机器人的游戏数据			( RC2 -> ROBOT )

            TRANS_GOODS_STAUTS = 12,				//机器人返回执行的物流状态				( ROBOT -> RC2 )
            TRANS_GOODS_STATUS_RET = 13,				//告诉机器人收到 TRANS_GOODS_STAUTS 消息( RC2 -> ROBOT )

            TRANS_ORDER_NEW_LOG = 18,				//机器人告诉RC2处理日志 [此记录用于主站数据统计]
            TRANS_ORDER_NEW_LOG_RET = 19,				//RC2告诉机器人收到日志 [此记录用于主站数据统计]

            TRANS_AGREE_SENDMAIL = 35,				//申请发送邮件
            TRANS_SEND_MAIL = 36,				//同意发送邮件

            TRANS_ORDER_END = 50,				//订单处理完成，正常移交。				( ROBOT -> RC2 )
            TRANS_ORDER_END_RET = 51,				//告诉机器人收到 TRANS_ORDER_END 消息	( RC2 -> ROBOT )

            TRANS_ORDER_CANCEL = 52,				//申请撤单								( ROBOT -> RC2 )
            TRANS_ORDER_CANCEL_RET = 53,				//告诉机器人收到 TRANS_ORDER_CANCEL 消息( RC2 -> ROBOT )

            TRANS_ORDER_ABOLISH = 54,				//取消订单								( ROBOT -> RC2 )
            TRANS_ORDER_ABOLISH_RET = 55,				//告诉机器人收到 TRANS_ORDER_ABOLISH 消息( RC2 -> ROBOT )

            TRANS_ORDER_OP = 56,				//转人工								( ROBOT -> RC2 )
            TRANS_ORDER_OP_RET = 57,				//告诉机器人收到 TRANS_ORDER_OP 消息	( RC2 -> ROBOT )
            TRANS_REBOOTCOMPUT = 60				//机器人请求重启电脑					( ROBOT -> RC2 )
        };
        //数据结构
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UDP_DATA
        {
            public int type;				//数据类型	(  0: 数据  10:状态  20:异常  )
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
            public char[] szData;		//详细数据 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] 
            public char[] szOrderNo;		//订单编号  
        };
    }
}

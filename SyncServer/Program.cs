using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPLib;

namespace SyncServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //指定IP和端口号及最大监听数目的方式
            TCPLib.TCPServer s1 = new TCPServer("127.0.0.1", 6001, 10);
            //指定端口号及最大监听数目的方式
            //TCPLib.TCPServer s2 = new TCPServer(6001, 10);

            //执行Start方法
            s1.Start();
        }
    }
}

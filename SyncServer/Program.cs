using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            TCPServerTest server = new TCPServerTest(IPAddress.Any.ToString(), 6001, 50);
            server.bullupPath = "C:\\斗牛电竞1.31\\bullup-front";
            server.autoprogramPath = "C:\\斗牛电竞1.31\\auto_program";
            server.updateFileDictionary(server.bullupPath, server.autoprogramPath);
            server.Start();
        }
    }
}

﻿using System;
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
            server.bullupPath = "E:\\NodeWorkspace\\BullupEsportPlatform\\BullupFrontend";
            server.autoprogramPath = "C:\\Users\\Public\\Bullup\\BullupAutoScript";
            server.updateFileDictionary(server.bullupPath, server.autoprogramPath);
            server.Start();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPLib
{
    public class TCPServer
    {
        private byte[] result = new byte[1024];
        /// <summary>
        /// 最大的监听数量
        /// </summary>
        private int maxClientCount;
        public int MaxClientCount
        {
            get { return maxClientCount; }
            set { maxClientCount = value; }
        }

        /// <summary>
        /// IP地址
        /// </summary>
        private string ip;
        public string IP
        {
            get { return ip; }
            set { ip = value; }
        }

        /// <summary>
        /// 端口号
        /// </summary>
        private int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        /// 客户端列表
        /// </summary>
        private List<Socket> mClientSockets;
        public List<Socket> ClientSockets
        {
            get { return mClientSockets; }
        }

        /// <summary>
        /// IP终端
        /// </summary>
        private IPEndPoint ipEndPoint;

        /// <summary>
        /// 服务端Socket
        /// </summary>
        private Socket mServerSocket;

        /// <summary>
        /// 当前客户端Socket
        /// </summary>
        private Socket mClientSocket;
        public Socket ClientSocket
        {
            get { return mClientSocket; }
            set { mClientSocket = value; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="count">监听的最大树目</param>
        public TCPServer(int port, int count)
        {
            this.ip = IPAddress.Any.ToString();
            this.port = port;
            this.maxClientCount = count;

            this.mClientSockets = new List<Socket>();

            //初始化IP终端
            this.ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            //初始化服务端Socket
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //端口绑定
            this.mServerSocket.Bind(this.ipEndPoint);
            //设置监听数目
            this.mServerSocket.Listen(maxClientCount);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">ip地址</param>
        /// <param name="port">端口号</param>
        /// <param name="count">监听的最大数目</param>
        public TCPServer(string ip, int port, int count)
        {
            this.ip = ip;
            this.port = port;
            this.maxClientCount = count;

            this.mClientSockets = new List<Socket>();

            //初始化IP终端
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            //初始化服务端Socket
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //端口绑定
            this.mServerSocket.Bind(this.ipEndPoint);
            //设置监听数目
            this.mServerSocket.Listen(maxClientCount);

        }

        /// <summary>
        /// 定义一个Start方法将构造函数中的方法分离出来
        /// </summary>
        public void Start()
        {
            //创建服务端线程，实现客户端连接请求的循环监听
            var mServerThread = new Thread(this.ListenClientConnect);
            //服务端线程开启
            mServerThread.Start();
        }

        /// <summary>
        /// 监听客户端链接
        /// </summary>
        private void ListenClientConnect()
        {
            //设置循环标志位
            bool flag = true;
            while (flag)
            {
                //获取连接到服务端的客户端
                this.ClientSocket = this.mServerSocket.Accept();
                //将获取到的客户端添加到客户端列表
                this.mClientSockets.Add(this.ClientSocket);
                //向客户端发送一条消息
                this.SendMessage(string.Format("客户端{0}已成功连接到服务器", this.ClientSocket.RemoteEndPoint));
                //创建客户端消息线程，实现客户端消息的循环监听
                var mReveiveThread = new Thread(this.ReceiveClient);
                //注意到ReceiveClient方法传入了一个参数
                //实际上这个参数就是此时连接到服务器的客户端
                //即ClientSocket
                mReveiveThread.Start(this.ClientSocket);
            }
        }

        /// <summary>
        /// 接收客户端消息的方法
        /// </summary>
        private void ReceiveClient(object obj)
        {
            //获取当前客户端
            //因为每次发送消息的可能并不是同一个客户端，所以需要使用var来实例化一个新的对象
            //可是我感觉这里用局部变量更好一点
            var mClientSocket = (Socket)obj;
            // 循环标志位
            bool flag = true;
            while (flag)
            {
                try
                {
                    //获取数据长度
                    int receiveLength = mClientSocket.Receive(result);
                    //获取客户端消息
                    string clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                    //服务端负责将客户端的消息分发给各个客户端
                    this.SendMessage(string.Format("客户端{0}发来消息:{1}", mClientSocket.RemoteEndPoint, clientMessage));

                }
                catch (Exception e)
                {
                    //从客户端列表中移除该客户端
                    this.mClientSockets.Remove(mClientSocket);
                    //向其它客户端告知该客户端下线
                    this.SendMessage(string.Format("服务器发来消息:客户端{0}从服务器断开,断开原因:{1}", mClientSocket.RemoteEndPoint, e.Message));
                    //断开连接
                    mClientSocket.Shutdown(SocketShutdown.Both);
                    mClientSocket.Close();
                    break;
                }
            }

        }

        /// <summary>
        /// 向所有的客户端群发消息
        /// </summary>
        /// <param name="msg">message</param>
        public void SendMessage(string msg)
        {
            //确保消息非空以及客户端列表非空
            if (msg == string.Empty || this.mClientSockets.Count <= 0) return;
            //向每一个客户端发送消息
            foreach (Socket s in this.mClientSockets)
            {
                (s as Socket).Send(Encoding.UTF8.GetBytes(msg));
            }
        }

        /// <summary>
        /// 向指定的客户端发送消息
        /// </summary>
        /// <param name="ip">ip</param>
        /// <param name="port">port</param>
        /// <param name="msg">message</param>
        public void SendMessage(string ip, int port, string msg)
        {
            //构造出一个终端地址
            IPEndPoint _IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            //遍历所有客户端
            foreach (Socket s in mClientSockets)
            {
                if (_IPEndPoint == (IPEndPoint)s.RemoteEndPoint)
                {
                    s.Send(Encoding.UTF8.GetBytes(msg));
                }
            }
        }
    }
}
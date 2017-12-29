using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPLib
{
    public class TCPServer{
        private byte[] result = new byte[1024];
        
        private int maxClientCount;
        public int MaxClientCount{
            get { return maxClientCount; }
            set { maxClientCount = value; }
        }

        private string ip;
        public string IP{
            get { return ip; }
            set { ip = value; }
        }

        private int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        private List<Socket> mClientSockets;
        public List<Socket> ClientSockets{
            get { return mClientSockets; }
        }

        private IPEndPoint ipEndPoint;

        private Socket mServerSocket;

        private Socket mClientSocket;
       
        public Socket ClientSocket{
            get { return mClientSocket; }
            set { mClientSocket = value; }
        }

        public TCPServer(int port, int count){
            this.ip = IPAddress.Any.ToString();
            this.port = port;
            this.maxClientCount = count;
            this.mClientSockets = new List<Socket>();
            this.ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mServerSocket.Bind(this.ipEndPoint);
            this.mServerSocket.Listen(maxClientCount);
        }

        public TCPServer(string ip, int port, int count){
            this.ip = ip;
            this.port = port;
            this.maxClientCount = count;
            this.mClientSockets = new List<Socket>();
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mServerSocket.Bind(this.ipEndPoint);
            this.mServerSocket.Listen(maxClientCount);
        }

        public void Start(){
            var mServerThread = new Thread(this.ListenClientConnect);
            mServerThread.Start();
        }

        private void ListenClientConnect(){
            bool flag = true;
            while (flag){
                this.ClientSocket = this.mServerSocket.Accept();
                this.mClientSockets.Add(this.ClientSocket);
                this.SendMessage(string.Format("客户端{0}已成功连接到服务器", this.ClientSocket.RemoteEndPoint));
                var mReveiveThread = new Thread(this.ReceiveClient);
                mReveiveThread.Start(this.ClientSocket);
            }
        }

        private void ReceiveClient(object obj){
            var mClientSocket = (Socket)obj;

            try{
                int receiveLength = mClientSocket.Receive(result);
                string clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                
                
                
                this.SendMessage(string.Format("客户端{0}发来消息:{1}", mClientSocket.RemoteEndPoint, clientMessage));
                    
                
            }catch (Exception e){
                this.mClientSockets.Remove(mClientSocket);    
                this.SendMessage(string.Format("服务器发来消息:客户端{0}从服务器断开,断开原因:{1}", mClientSocket.RemoteEndPoint, e.Message));
                mClientSocket.Shutdown(SocketShutdown.Both);
                mClientSocket.Close();
            }
        }

        public void SendMessage(string msg){
            if (msg == string.Empty || this.mClientSockets.Count <= 0) return;
            foreach (Socket s in this.mClientSockets){
                (s as Socket).Send(Encoding.UTF8.GetBytes(msg));
            }
        }

        public void SendMessage(string ip, int port, string msg){
            IPEndPoint _IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            foreach (Socket s in mClientSockets){
                if (_IPEndPoint == (IPEndPoint)s.RemoteEndPoint){
                    s.Send(Encoding.UTF8.GetBytes(msg));
                }
            }
        }
    }
}
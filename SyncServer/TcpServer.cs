﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections;

namespace TCPLib
{
    public class TCPServer{
        //private byte[] result = new byte[1024];
        
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


        private IPEndPoint ipEndPoint;

        private Socket mServerSocket;

        public TCPServer(int port, int count){
            this.ip = IPAddress.Any.ToString();
            this.port = port;
            this.maxClientCount = count;
            this.ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mServerSocket.Bind(this.ipEndPoint);
            this.mServerSocket.Listen(maxClientCount);
        }

        public TCPServer(string ip, int port, int count){
            this.ip = ip;
            this.port = port;
            this.maxClientCount = count;
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
                Socket newClientSocket = this.mServerSocket.Accept();
                Thread mReveiveThread = new Thread(this.ReceiveClientNew);
                mReveiveThread.Start(newClientSocket);
            }
        }

        public void CreateDirectory(String directoryPath) {
            Directory.CreateDirectory(directoryPath);
        }

        public void GetAllFiles(DirectoryInfo rootDirectory, ArrayList files) {
            foreach (FileInfo file in rootDirectory.GetFiles("*")) {
                files.Add(file.FullName);
                //Console.WriteLine(file.FullName);
            }

            DirectoryInfo[] directories = rootDirectory.GetDirectories();
            foreach (DirectoryInfo directory in directories) {
                GetAllFiles(directory, files);
            }
        }

        public Dictionary<String, byte[]> fileDataDictionary = new Dictionary<String, byte[]>();
        public Dictionary<String, long> fileSizeDictionary = new Dictionary<String, long>();

        //updating
        public void updateFileDictionary(String bullupPath, String autoProgramPath) {
            fileDataDictionary.Clear();
            fileSizeDictionary.Clear();

            DirectoryInfo bullupDir = new DirectoryInfo(bullupPath);
            DirectoryInfo autoprogramDir = new DirectoryInfo(autoProgramPath);
            ArrayList bullupFiles = new ArrayList();
            ArrayList autoprogramFiles = new ArrayList();
            GetAllFiles(bullupDir, bullupFiles);
            GetAllFiles(autoprogramDir, autoprogramFiles);
            int fileCount = bullupFiles.Count + autoprogramFiles.Count;

            for (int i = 0; i < autoprogramFiles.Count; i++) {
                bullupFiles.Add(autoprogramFiles[i]);
            }

            for (int i = 0; i < bullupFiles.Count; i++) {
                String filePath = (String)bullupFiles[i];
                FileInfo file = new FileInfo(filePath);
                long fileSize = file.Length;
                byte[] fileData = null;// = new byte[file.Length];
                ReadFile(filePath, ref fileData);
                fileDataDictionary.Add(filePath, fileData);
                fileSizeDictionary.Add(filePath, fileSize);
            }
        }

        public long ReadFileFromMemery(String filePath, ref byte[] fileData) {
            long fileSize = fileSizeDictionary[filePath];
            byte[] memoryData = fileDataDictionary[filePath];
            fileData = new byte[fileSize];
            try {
                for (long i = 0; i < fileSize; i++) {
                    fileData[i] = memoryData[i];
                }
                //fileDataDictionary[filePath].CopyTo(fileData, fileSize);
                //Array.Copy(fileData, 0, , 0, fileSize);
            } catch (Exception e) {
                Console.WriteLine("ReadFileFromMemory报错：" + e.ToString());
            }
            return fileSize;
        }


        private void ReceiveClientNew(object obj) {
            Socket mClientSocket = (Socket)obj;
            Console.WriteLine(mClientSocket.RemoteEndPoint.ToString() + "开始接收");
            try {
                byte[] result = new byte[1024];
                int receiveLength = mClientSocket.Receive(result);
                String clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                String clientPath = clientMessage.Substring(clientMessage.IndexOf("PATH#") + 5);
                clientPath = clientPath.Substring(0, clientPath.IndexOf("#"));
                ArrayList sendFilePaths = new ArrayList();
                DirectoryInfo bullupDir = new DirectoryInfo(bullupPath);
                DirectoryInfo autoprogramDir = new DirectoryInfo(autoprogramPath);
                ArrayList bullupFiles = new ArrayList();
                ArrayList autoprogramFiles = new ArrayList();
                GetAllFiles(bullupDir, bullupFiles);
                GetAllFiles(autoprogramDir, autoprogramFiles);
                int fileCount = fileSizeDictionary.Count;

                for (int i = 0; i < bullupFiles.Count; i++) {
                    sendFilePaths.Add(clientPath + ((String)bullupFiles[i]).Substring(bullupPath.Length));
                }
                for (int i = 0; i < autoprogramFiles.Count; i++) {
                    bullupFiles.Add(autoprogramFiles[i]);
                    //sendFilePaths.Add("C:\\Users\\Public\\Bullup\\auto_program" + ((String)autoprogramFiles[i]).Substring(autoprogramPath.Length));
                    sendFilePaths.Add(clientPath + ((String)autoprogramFiles[i]).Substring(autoprogramPath.Length));
                }
                SendMessage(mClientSocket, string.Format("INSTALLFILECOUNT#{0}#", fileCount));
                int transedCount = 0;
                while (fileCount != transedCount) {
                    //接收待传输文件编号
                    //try {
                    //    receiveLength = mClientSocket.Receive(result);
                    //} catch (Exception e) {
                    //    Console.WriteLine("-1");
                    //}
                    ////要开始传第几个文件
                    //transedCount = Int32.Parse(Encoding.UTF8.GetString(result, 0, receiveLength));
                    //Console.WriteLine(transedCount);

                    //获取要传输的文件信息
                    String filePath = (String)bullupFiles[transedCount];
                    byte[] fileData = null;
                    long fileSize = 0;
                    //fileSize = ReadFileFromMemery(filePath, ref fileData);
                    String sendFilePath = (String)sendFilePaths[transedCount];

                    //拼路径和大小字符串并发送
                    String fileInfoString = "FILESIZE#" + fileSize + "#FILEPATH#" + sendFilePath + "#";
                    mClientSocket.Send(Encoding.UTF8.GetBytes(fileInfoString));

                    //Console.WriteLine("Before receive: " + mClientSocket.Connected.ToString());

                    //try {
                    //    receiveLength = mClientSocket.Receive(result);
                    //} catch (Exception e) {
                    //    Console.WriteLine("1");
                    //}
                    //clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                    //if (clientMessage != "DATA_READY") {
                    //    Console.WriteLine("传输第{0}个文件错误", transedCount);
                    //}

                    //int blockSize = 4 * 1024 * 1024;
                    //byte[] filePiece = new byte[blockSize];
                    //int sendSize = 0;
                    //while (sendSize != fileSize) {
                    //    if (sendSize + blockSize <= fileSize) {
                    //        for (int i = 0; i < blockSize; i++) {
                    //            filePiece[i] = fileData[sendSize + i];
                    //        }
                    //        sendSize += mClientSocket.Send(filePiece);
                    //    } else {
                    //        for (int i = 0; i < fileSize - sendSize; i++) {
                    //            filePiece[i] = fileData[sendSize + i];
                    //        }
                    //        sendSize += mClientSocket.Send(filePiece, (int)(fileSize - sendSize), 0);
                    //    }

                    //}
                    //mClientSocket.Send(fileData);

                    //try {
                    //    receiveLength = mClientSocket.Receive(result);
                    //} catch (Exception e) {
                    //    Console.WriteLine("2");
                    //}
                    //clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                    //if (clientMessage != "DATA_OK") {
                    //    Console.WriteLine("传输{0}错误", filePath);
                    //}
                    transedCount++;
                }
                Console.WriteLine("传输完成");
            } catch (Exception e) {
                Console.WriteLine(mClientSocket.RemoteEndPoint.ToString() + "已断开");
                //SendMessage(mClientSocket, string.Format("服务器发来消息:客户端{0}从服务器断开,断开原因:{1}", mClientSocket.RemoteEndPoint, e.Message));
                mClientSocket.Shutdown(SocketShutdown.Both);
                mClientSocket.Close();
            }
        }

        public long ReadFile(String filePath, ref byte[] fileData) {
            FileInfo file = new FileInfo(filePath);
            long fileSize = file.Length;
            fileData = new byte[fileSize];
            //读取文件数据
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            byte[] buffer = new byte[1024];
            int receievedSize = 0;
            while (receievedSize != fileSize) {
                int tempSize = 0;
                if (fileSize - receievedSize > 1024) {
                    tempSize = br.Read(buffer, 0, 1024);
                } else {
                    tempSize = br.Read(buffer, 0, (int)(fileSize - receievedSize));
                }
                for (int i = 0; i < tempSize; i++) {
                    fileData[receievedSize + i] = buffer[i];
                }
                receievedSize += tempSize;
            }
            br.Close();
            fs.Close();
            return fileSize;
        }

        public static string GetMD5HashFromFile(string fileName) {
            try {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++) {
                    stringBuilder.Append(retVal[i].ToString("x2"));
                }
                return stringBuilder.ToString();
            } catch (Exception ex) {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }


        public static void DeleteDirectory(string directoryPath) {
            if (Directory.Exists(directoryPath)) {
                Directory.Delete(directoryPath);
            }
        }

        public static void DeleteFile(string directoryPath) {
            if (File.Exists(directoryPath)) {
                File.Delete(directoryPath);
            }
        }

        public String bullupPath = "E:\\Win32CSharpWorkSpace";
        public String autoprogramPath = "E:\\ttttttt";

        public void SendMessage(Socket socket, string msg){
            if (msg == string.Empty) return;
            socket.Send(Encoding.UTF8.GetBytes(msg));
        }
    }
}
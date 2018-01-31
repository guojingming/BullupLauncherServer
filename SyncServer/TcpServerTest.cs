using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections;
using System.Web.Script.Serialization;

namespace TCPLib
{
    public class TCPServerTest{
        //private byte[] result = new byte[1024];
        private int maxClientCount;
        private string ip;
        private int port;
        private IPEndPoint ipEndPoint;
        private Socket mServerSocket;
        public Dictionary<String, byte[]> fileDataDictionary = new Dictionary<String, byte[]>();
        public Dictionary<String, long> fileSizeDictionary = new Dictionary<String, long>();
        public String bullupPath = "E:\\Bullup\\win64";
        public String autoprogramPath = "C:\\Users\\Public\\Bullup\\BullupAutoScript";

        private Dictionary<String, String> fileMd5Dictionary = new Dictionary<String, String>();
        

        public void SendMessage(Socket socket, string msg) {
            if (msg == string.Empty) return;
            try {
                socket.Send(Encoding.UTF8.GetBytes(msg));
            } catch (Exception e) {
                Console.WriteLine("{0}已强制断开连接", socket.RemoteEndPoint.ToString());
            }
           
        }

        public int RecieveMessage(ref Socket mClientSocket, ref byte[] result) {
            Array.Clear(result, 0, result.Length);
            return mClientSocket.Receive(result);
        }

        private void ReceiveClientNew(object obj) {
            Socket mClientSocket = (Socket)obj;
            Console.WriteLine(mClientSocket.RemoteEndPoint.ToString() + "开始接收");
            JavaScriptSerializer jsonSerialize = new JavaScriptSerializer();
            String bullupJsonStr = "";
            String autoprogramJsonStr = "";
            int jsonStrSize = 0;
            int curJsonStrSize = 0;

            try {
                byte[] result = new byte[300];
RE_FILECOUNT:
int receiveLength = RecieveMessage(ref mClientSocket, ref result);
                try{
                    String bullupFileLengthStr = Encoding.UTF8.GetString(result, 0, receiveLength);
                    jsonStrSize = Int32.Parse(bullupFileLengthStr);
mClientSocket.Send(Encoding.UTF8.GetBytes("BULLUP_FILE_LENGTH_OK"));
                }
                catch (Exception e) {
mClientSocket.Send(Encoding.UTF8.GetBytes("BULLUP_FILE_LENGTH_FAIL"));
                    goto RE_FILECOUNT;
                }
CONTINUE_BULLUP_JSON:
receiveLength = RecieveMessage(ref mClientSocket, ref result);
curJsonStrSize += receiveLength;
                bullupJsonStr += Encoding.UTF8.GetString(result, 0, receiveLength);
                if (curJsonStrSize < jsonStrSize)
                {
                    //curJsonStrSize = 0;
                    goto CONTINUE_BULLUP_JSON;
                }
                curJsonStrSize = 0;
                Dictionary<String, String> bullupDic = null;
                try {
                    bullupDic = jsonSerialize.Deserialize<Dictionary<String, String>>(bullupJsonStr);
                    bullupJsonStr = "";
                } catch (Exception e) {
                    Console.WriteLine("Bullup json str:" + bullupJsonStr);
                    Console.WriteLine("Bullup file json decode failed");
mClientSocket.Send(Encoding.UTF8.GetBytes("BULLUP_FILE_FAIL"));
                    bullupJsonStr = "";
                    goto RE_FILECOUNT;
                }
mClientSocket.Send(Encoding.UTF8.GetBytes("BULLUP_FILE_OK"));
RE_AUTOPROGRAM:
receiveLength = RecieveMessage(ref mClientSocket, ref result);
                try
                {
                    String autoFileLengthStr = Encoding.UTF8.GetString(result, 0, receiveLength);
                    jsonStrSize = Int32.Parse(autoFileLengthStr);
                    mClientSocket.Send(Encoding.UTF8.GetBytes("AUTOSCRIPT_FILE_LENGTH_OK"));
                }
                catch (Exception e)
                {
                    mClientSocket.Send(Encoding.UTF8.GetBytes("AUTOSCRIPT_FILE_LENGTH_FAIL"));
                    goto RE_AUTOPROGRAM;
                }
CONTINUE_AUTOPROGRAM_JSON:
receiveLength = RecieveMessage(ref mClientSocket, ref result);
                curJsonStrSize += receiveLength;
                autoprogramJsonStr += Encoding.UTF8.GetString(result, 0, receiveLength);
                if (curJsonStrSize < jsonStrSize)
                {
                    //
                    goto CONTINUE_AUTOPROGRAM_JSON;
                }

                Dictionary<String, String> autoprogramDic = null;
                try {
                    autoprogramDic = jsonSerialize.Deserialize<Dictionary<String, String>>(autoprogramJsonStr);
                    autoprogramJsonStr = "";
                } catch (Exception e) {
                    Console.WriteLine("Auto json str:" + autoprogramJsonStr);
                    Console.WriteLine("Autoprogram file json decode failed");
mClientSocket.Send(Encoding.UTF8.GetBytes("AUTOSCRIPT_FILE_FAIL"));
                    autoprogramJsonStr = "";
                    goto RE_AUTOPROGRAM;
                }
mClientSocket.Send(Encoding.UTF8.GetBytes("AUTOSCRIPT_FILE_OK"));
                

//接路径
receiveLength = RecieveMessage(ref mClientSocket, ref result);
                String clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                String clientPath = clientMessage.Substring(clientMessage.IndexOf("PATH$") + 5);
                clientPath = clientPath.Substring(0, clientPath.IndexOf("$"));
                ArrayList sendFilePaths = new ArrayList();
                DirectoryInfo bullupDir = new DirectoryInfo(bullupPath);
                DirectoryInfo autoprogramDir = new DirectoryInfo(autoprogramPath);
                ArrayList bullupFiles = new ArrayList();
                ArrayList autoprogramFiles = new ArrayList();
                GetAllFiles(bullupDir, bullupFiles);
                GetAllFiles(autoprogramDir, autoprogramFiles);
                
                for (int i = 0; i < bullupFiles.Count; i++) {
                    //
                    if (bullupDic == null || bullupDic.Count == 0) {
                        sendFilePaths.Add(clientPath + ((String)bullupFiles[i]).Substring(bullupPath.Length));
                    } else {
                        String key = ((String)bullupFiles[i]).Substring(bullupPath.Length);
                        String serverFileMd5 = fileMd5Dictionary[key];
                        String clientFileMd5 = null;
                        if (bullupDic.ContainsKey(key)) {
                            clientFileMd5 = bullupDic[key];
                        }
                        if (serverFileMd5 != clientFileMd5) {
                            sendFilePaths.Add(clientPath + key);
                        } 
                        //else {
                        //    bullupFiles.RemoveAt(i);
                        //}
                    }   
                }
                for (int i = 0; i < autoprogramFiles.Count; i++) {
                    //
                    if (autoprogramDic == null || autoprogramDic.Count == 0) {
                        bullupFiles.Add(autoprogramFiles[i]);
                        sendFilePaths.Add("C:\\Users\\Public\\Bullup\\auto_program" + ((String)autoprogramFiles[i]).Substring(autoprogramPath.Length));
                    } else {
                        String key = ((String)autoprogramFiles[i]).Substring(autoprogramPath.Length);
                        String serverFileMd5 = fileMd5Dictionary[key];
                        String clientFileMd5 = null;
                        if (autoprogramDic.ContainsKey(key)) {
                            clientFileMd5 = autoprogramDic[key];
                        }
                        if (serverFileMd5 != clientFileMd5) {
                            bullupFiles.Add(autoprogramFiles[i]);
                            sendFilePaths.Add("C:\\Users\\Public\\Bullup\\auto_program" + ((String)autoprogramFiles[i]).Substring(autoprogramPath.Length));
                        }
                    }   
                    
                }
                int fileCount = sendFilePaths.Count;
//发送安装文件数量字符串
mClientSocket.Send(Encoding.UTF8.GetBytes(String.Format("INSTALLFILECOUNT${0}$", fileCount)));
RecieveMessage(ref mClientSocket, ref result);
                if (Encoding.UTF8.GetString(result).IndexOf("FILECOUNT_OK") == 0) {

                } else {
                    Console.WriteLine("FILECOUNT_TIP:" + Encoding.UTF8.GetString(result));
                    Console.WriteLine("FILECOUNT重发");
                    goto RE_FILECOUNT;
                }
                int transedCount = 0;
                while (fileCount != transedCount) {
                    //获取要传输的文件信息
RE_SEND:
                    //filepath有问题 
                    //String filePath = (String)bullupFiles[transedCount];
                    String sendFilePath = (String)sendFilePaths[transedCount];
                    String filePath = null;
                    if (sendFilePath.IndexOf("C:\\Users\\Public\\Bullup\\auto_program") != 0) {
                        filePath = bullupDir + sendFilePath.Substring(sendFilePath.IndexOf(clientPath) + clientPath.Length);
                    } else {
                        filePath = autoprogramDir + sendFilePath.Substring(sendFilePath.IndexOf("C:\\Users\\Public\\Bullup\\auto_program") + "C:\\Users\\Public\\Bullup\\auto_program".Length);
                    }
                    
                    
                    byte[] fileData = null;
                    long fileSize = 0;
                    fileSize = ReadFileFromMemery(filePath, ref fileData);
                   
                    //拼路径和大小字符串并发送
                    String fileSizeString = "FILESIZE$" + fileSize;
mClientSocket.Send(Encoding.UTF8.GetBytes(fileSizeString));
RecieveMessage(ref mClientSocket, ref result);
                    if (Encoding.UTF8.GetString(result).IndexOf("SIZE_OK") == 0) {
                        //Console.WriteLine("{0} ok", transedCount);
                    } else {
                        Console.WriteLine("重发fileSize");
                        goto RE_SEND;
                    }
                    String filePathString = "FILEPATH$" + sendFilePath + "$";
mClientSocket.Send(Encoding.UTF8.GetBytes(filePathString));
RecieveMessage(ref mClientSocket, ref result);
                    if (Encoding.UTF8.GetString(result).IndexOf("PATH_OK") == 0) {
                        //Console.WriteLine("{0} ok", transedCount);
                    } else {
                        //Console.WriteLine("重发filePath");
                        //goto RE_SEND;
                    }
                    int blockSize = 200 * 1024;
                    byte[] filePiece = new byte[blockSize];
                    int sendSize = 0;
                    while (sendSize != fileSize) {
                        if (sendSize + blockSize <= fileSize) {
                            for (int i = 0; i < blockSize; i++) {
                                filePiece[i] = fileData[sendSize + i];
                            }
sendSize += mClientSocket.Send(filePiece);
                        } else {
                            for (int i = 0; i < fileSize - sendSize; i++) {
                                filePiece[i] = fileData[sendSize + i];
                            }
sendSize += mClientSocket.Send(filePiece, (int)(fileSize - sendSize), 0);
                        }
                    }
RecieveMessage(ref mClientSocket, ref result);
                        if (Encoding.UTF8.GetString(result).IndexOf("DATA_OK") == 0) {
                            //Console.WriteLine("{0} ok", transedCount);
                        } else {
                            Console.WriteLine("重发fileData");
                            goto RE_SEND;
                        }
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

            fileMd5Dictionary.Clear();
            
            for (int i = 0; i < bullupFiles.Count; i++) {
                String filePath = (String)bullupFiles[i];
                String totalPath = filePath;
                String localPath = totalPath.Substring(totalPath.IndexOf(bullupPath) + bullupPath.Length);

                fileMd5Dictionary.Add(localPath, GetMD5HashFromFile(totalPath));
            }

            for (int i = 0; i < autoprogramFiles.Count; i++) {
                String filePath = autoprogramFiles[i].ToString();
                bullupFiles.Add(filePath);

                String totalPath = filePath;
                String localPath = totalPath.Substring(totalPath.IndexOf(autoProgramPath) + autoProgramPath.Length);
                fileMd5Dictionary.Add(localPath, GetMD5HashFromFile(totalPath));
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
                File.SetAttributes(fileName, FileAttributes.Normal);
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

        public TCPServerTest(string ip, int port, int count) {
            this.ip = ip;
            this.port = port;
            this.maxClientCount = count;
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            this.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mServerSocket.Bind(this.ipEndPoint);
            this.mServerSocket.Listen(maxClientCount);
        }
        
        public void Start() {
            var mServerThread = new Thread(this.ListenClientConnect);
            mServerThread.Start();
        }

        private void ListenClientConnect() {
            bool flag = true;
            while (flag) {
                Socket newClientSocket = this.mServerSocket.Accept();
                Thread mReveiveThread = new Thread(this.ReceiveClientNew);
                mReveiveThread.Start(newClientSocket);
            }
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
    }
}
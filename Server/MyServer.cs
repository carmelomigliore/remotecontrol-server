using System.IO;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

        class MyServer
        {
            private UdpClient _server;
            private TcpListener _tcpListener;
            
            private bool _accepting=true;
            private TcpClient _tcpClient;
            private byte[] _publicKey;
            private ECDiffieHellmanCng _exch;
            private bool _authorized = false;
            private ClipboardManager _clipboardManager;
            private InputManager _inputManager;
            public MainWindow Window { get; set; }

            


            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool LogonUser( string lpszUsername,  string lpszDomain,  string lpszPassword,  int dwLogonType, int dwLogonProvider, ref IntPtr phToken );

            public MyServer()
            {
                //InputManager._altPressed = false;
                //_alreadyDown = new List<short>();
                _exch = new ECDiffieHellmanCng(256);
                _exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                _exch.HashAlgorithm = CngAlgorithm.Sha256;
                _publicKey = _exch.PublicKey.ToByteArray();
                _inputManager = new InputManager();
                _server = new UdpClient();
                _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localpt = new IPEndPoint(IPAddress.Any, 3000);
                _tcpListener = new TcpListener(localpt);
                _tcpListener.Start();
                _server.DontFragment = true;
                _server.Client.Bind(localpt);
                Thread t = new Thread(ReceiveUdp);
                t.Start();
                Thread t2 = new Thread(ReceiveTcp);
                t2.SetApartmentState(ApartmentState.STA);
                t2.Start();
            }

            public void ReceiveTcp()
            {
                byte [] data = new byte[512];
                int command;
                while (_accepting)
                {
                    _tcpClient = _tcpListener.AcceptTcpClient();
                    _tcpClient.GetStream().ReadTimeout = Timeout.Infinite;
                    byte[] clientPublicKey = new byte[72];
                    _tcpClient.GetStream().Read(clientPublicKey, 0, 72);
                    byte[] derivedKey = _exch.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
                    _tcpClient.GetStream().Write(_publicKey,0,_publicKey.Length);
                    while (true)
                    {
                        try
                        {
                            StreamReader streamReader = new StreamReader(_tcpClient.GetStream());

                            // ReSharper disable once AssignNullToNotNullAttribute
                            command = int.Parse(streamReader.ReadLine());
                            Console.WriteLine(command);
                            switch (command)
                            {
                                case 0: // LOGIN
                                    // ReSharper disable once PossibleNullReferenceException
                                    string currentUserName =
                                        System.Security.Principal.WindowsIdentity.GetCurrent()
                                            .Name.Split(new char[] {'\\'})[1];
                                    string receivedUserName = streamReader.ReadLine();
                                    string domain = streamReader.ReadLine();
                                    Aes aes = new AesCryptoServiceProvider();
                                    aes.Key = derivedKey;
                                    byte[] bytes = new byte[aes.BlockSize/8];
                                    bytes.Initialize();
                                    System.Buffer.BlockCopy(currentUserName.ToCharArray(), 0, bytes, 0,
                                        bytes.Length > currentUserName.Length*sizeof (char)
                                            ? currentUserName.Length*sizeof (char)
                                            : bytes.Length);

                                    aes.IV = bytes;
                                    MemoryStream ms = new MemoryStream(64);
                                    ICryptoTransform encryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                                    CryptoStream csEncrypt = new CryptoStream(ms, encryptor,
                                        CryptoStreamMode.Write);
                                    byte[] buffer = new byte[256];
                                    int length = _tcpClient.GetStream().Read(buffer, 0, 256);
                                    csEncrypt.Write(buffer, 0, length);
                                    csEncrypt.Flush();
                                    csEncrypt.Close();
                                    string password = Encoding.UTF8.GetString(ms.ToArray());
                                    IntPtr th = IntPtr.Zero;

                                    _authorized = LogonUser(receivedUserName, domain, password, 2, 0, ref th);
                                    byte[] auth = BitConverter.GetBytes(_authorized);
                                    _tcpClient.GetStream().Write(auth, 0, sizeof (bool));
                                    if (_authorized)
                                    {
                                        _clipboardManager = new ClipboardManager();
                                        Thread t = new Thread(_clipboardManager.InitializeShare);
                                        t.Start();
                                        Thread t1 = new Thread(_clipboardManager.AddConnection);
                                        t1.Start((_tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                        //_clipboardManager.AddConnection((_tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                    }
                                    break;

                                case 1:
                                    byte[] clip = _clipboardManager.GetClipboardData();
                                    byte[] len = BitConverter.GetBytes(clip != null ? clip.Length : 0);
                                    _tcpClient.GetStream().Write(len,0,sizeof(int));
                                    if (clip != null)
                                    {
                                        _tcpClient.GetStream().Write(clip,0,clip.Length);
                                    }
                                    break;
                                case 2:
                                    try
                                    {
                                        byte[] recClipLen = new byte[sizeof (int)];
                                        _tcpClient.GetStream().Read(recClipLen, 0, sizeof (int));
                                        int recLen = BitConverter.ToInt32(recClipLen, 0);
                                        if (recLen > 0)
                                        {
                                            int read = 0;
                                            byte[] recClip = new byte[recLen];
                                            while (read < recLen)
                                            {
                                                read += _tcpClient.GetStream().Read(recClip, read, recLen - read);
                                            }
                                            using (var memStream = new MemoryStream())
                                            {
                                                var binForm = new BinaryFormatter();
                                                memStream.Write(recClip, 0, recClip.Length);
                                                memStream.Seek(0, SeekOrigin.Begin);
                                                var obj = binForm.Deserialize(memStream);
                                               Window.Dispatcher.Invoke( new Action(() =>
                                               {
                                                   _clipboardManager.SetClipboard(((IPEndPoint) _tcpClient.Client.RemoteEndPoint).Address.ToString(), obj);
                                               }));
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            _authorized = false;
                            break;
                            //TODO Notification in tray
                        }
                    }
                }
            }

            public void ReceiveUdp()
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    var data = _server.Receive(ref ip);
                   // Console.WriteLine("diazzo");
                    bool keybd = BitConverter.ToBoolean(data, 0);
                    //short vkCode = BitConverter.ToInt16(data, sizeof(bool));
                    //Console.WriteLine("dione " + vkCode);
                    if (keybd)
                    {
                        _inputManager.ProcessInputKeyboard(data);
                    }
                    else
                    {
                        _inputManager.ProcessInputMouse(data);
                    }
                       
                }
            }
        }
    
}

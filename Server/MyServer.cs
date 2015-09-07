using System.DirectoryServices.AccountManagement;
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

            [DllImport("Sas.dll", SetLastError = true)]
            public static extern void SendSAS(bool asUser);

            public MyServer(int port)
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
                IPEndPoint localpt = new IPEndPoint(IPAddress.Any, port);
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
                try
                {
                    byte[] data = new byte[512];
                    int command;
                    while (_accepting)
                    {
                        _tcpClient = _tcpListener.AcceptTcpClient();
                        _tcpClient.GetStream().ReadTimeout = Timeout.Infinite;
                        SetTcpKeepAlive(_tcpClient.Client, 3000, 1);
                        byte[] clientPublicKey = new byte[72];
                        _tcpClient.GetStream().Read(clientPublicKey, 0, 72);
                        byte[] derivedKey =
                            _exch.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        _tcpClient.GetStream().Write(_publicKey, 0, _publicKey.Length);
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
                                        Console.WriteLine("Mannaggia quel bastardo di padre pio");
                                        // ReSharper disable once PossibleNullReferenceException
                                        string currentUserName =
                                            System.Security.Principal.WindowsIdentity.GetCurrent()
                                                .Name.Split(new char[] {'\\'})[1];
                                        Console.WriteLine("Mannaggia quel bastardo di gianpaolo");
                                        string receivedUserName = streamReader.ReadLine();
                                        Console.WriteLine("Mannaggia quel bastardo di geova");
                                        string domain = streamReader.ReadLine();
                                        Console.WriteLine("Mannaggia quei coglioni di medjugorie");
                                        Aes aes = new AesCryptoServiceProvider();
                                        aes.Key = derivedKey;
                                        byte[] bytes = new byte[aes.BlockSize/8];
                                        Console.WriteLine("Mannaggia quel bastardo dei tre pastorelli");
                                        bytes.Initialize();
                                        System.Buffer.BlockCopy(currentUserName.ToCharArray(), 0, bytes, 0,
                                            bytes.Length > currentUserName.Length*sizeof (char)
                                                ? currentUserName.Length*sizeof (char)
                                                : bytes.Length);
                                        Console.WriteLine("Mannaggia quel bastardo di fatima " + currentUserName);
                                        aes.IV = bytes;
                                        MemoryStream ms = new MemoryStream(64);
                                        ICryptoTransform encryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                                        CryptoStream csEncrypt = new CryptoStream(ms, encryptor,
                                            CryptoStreamMode.Write);
                                        Console.WriteLine("Mannaggia quel cane del clero");


                                        Console.WriteLine("bastardo del signore");
                                        //int passSize = Int32.Parse(streamReader.ReadLine());
                                        //Console.WriteLine("Diaccio " + passSize);
                                        string encpassword = streamReader.ReadLine();
                                        byte[] buffer = Convert.FromBase64String(encpassword);
                                        /*int length = _tcpClient.GetStream().Read(buffer, 0, 256);*/
                                        Console.WriteLine("Masalaccio");
                                        csEncrypt.Write(buffer, 0, buffer.Length);
                                        Console.WriteLine("cristaccio");
                                        csEncrypt.Flush();
                                        csEncrypt.Close();
                                        Console.WriteLine("Mannaggia quel bastardo di giovanardi");
                                        string password = Encoding.UTF8.GetString(ms.ToArray());
                                        //   IntPtr th = IntPtr.Zero;

                                        // _authorized = LogonUser(receivedUserName, domain, password, 3, 0, ref th);

                                        Console.WriteLine("Mannaggia la madonna " + password);

                                        PrincipalContext pc = new PrincipalContext(ContextType.Machine, null);
                                        Console.WriteLine("Mannaggia il cristo");
                                        _authorized = pc.ValidateCredentials(receivedUserName, password);
                                        Console.WriteLine("Mannaggia dio");



                                        /* Window.Dispatcher.Invoke(new Action(() =>
                                    {
                                        MessageBox.Show("Gesu: " + _authorized);
                                    }));*/
                                        //_authorized = true;
                                        Console.WriteLine("mannaggia cristo: " + _authorized);
                                        byte[] auth = BitConverter.GetBytes(_authorized);
                                        _tcpClient.GetStream().Write(auth, 0, sizeof (bool));
                                        _tcpClient.GetStream().Flush();
                                        if (_authorized)
                                        {
                                            _clipboardManager = new ClipboardManager();
                                            Thread t = new Thread(_clipboardManager.InitializeShare);
                                            t.Start();
                                            Thread t1 = new Thread(_clipboardManager.AddConnection);
                                            t1.Start((_tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                            Console.WriteLine("badogna");
                                            SendSAS(false);
                                            Console.WriteLine("bubba");
                                            //_clipboardManager.AddConnection((_tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                        }
                                        break;

                                    case 1:
                                        byte[] clip = _clipboardManager.GetClipboardData();
                                        byte[] len = BitConverter.GetBytes(clip != null ? clip.Length : 0);
                                        _tcpClient.GetStream().Write(len, 0, sizeof (int));
                                        if (clip != null)
                                        {
                                            _tcpClient.GetStream().Write(clip, 0, clip.Length);
                                        }
                                        Window.Dispatcher.Invoke(new Action(() =>
                                        {
                                            Window.UnderControl.StrokeThickness = 0;
                                        }));
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
                                                    Window.Dispatcher.Invoke(new Action(() =>
                                                    {
                                                        _clipboardManager.SetClipboard(
                                                            ((IPEndPoint) _tcpClient.Client.RemoteEndPoint).Address
                                                                .ToString(), obj);
                                                        Window.UnderControl.StrokeThickness = 12;
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
                                Console.WriteLine("Bastardo di quel merda del bambinello " + e.Message);
                                Window.Dispatcher.Invoke(new Action(() =>
                                {
                                    Window.UnderControl.StrokeThickness = 0;
                                }));

                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Si fotta anche il diaccio "+e.Message + e.StackTrace);
                }
            }

            public void ReceiveUdp()
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] data;
                    try
                    {
                       data = _server.Receive(ref ip);
                    }
                    catch (Exception se)
                    {
                        Console.WriteLine("Si fotta anche gesù " + se.Message);
                        return;
                    }
                   
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

            private static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
            {
                /* the native structure
                struct tcp_keepalive {
                ULONG onoff;
                ULONG keepalivetime;
                ULONG keepaliveinterval;
                };
                */

                // marshal the equivalent of the native structure into a byte array
                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

                // write SIO_VALS to Socket IOControl
                socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            }

            public void Stop()
            {
                try
                {
                    _accepting = false;
                   _tcpListener.Stop();
                    Console.WriteLine("mannaggia il gesuello");
                    Console.WriteLine("Mannaggia il gesuino");
                    //_tcpClient.GetStream().Dispose();
                    Console.WriteLine("Mannaggia la pelata di Pio");
                   
                    Console.WriteLine("Mannaggia il gesuazzo");
                    _server.Close();
                    Console.WriteLine("Mannaggia il barabba");
                    //_server.Close();
                    Console.WriteLine("cacca");
                    _tcpListener = null;
                    _server = null;
                    if(_tcpClient!= null)
                        _tcpClient.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Mannaggia Giasone" + e.Message + e.StackTrace + e.Source);
                    _tcpListener = null;
                    _tcpClient = null;
                    _server = null;
                }
            }
        }
    
}

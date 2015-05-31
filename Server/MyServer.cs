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
            private static Boolean _altPressed;
            private List<short> _alreadyDown;
            private bool _accepting=true;
            private TcpClient _tcpClient;
            private byte[] _publicKey;
            private ECDiffieHellmanCng _exch;
            private bool _authorized = false;
            private ClipboardManager _clipboardManager;
            public MainWindow Window { get; set; }

            public struct INPUT
            {
                internal uint type;
                internal InputUnion U;
                internal static int Size
                {
                    get { return Marshal.SizeOf(typeof(INPUT)); }
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct InputUnion
            {
                [FieldOffset(0)]
                internal MOUSEINPUT mi;
                [FieldOffset(0)]
                internal KEYBDINPUT ki;
                [FieldOffset(0)]
                internal HARDWAREINPUT hi;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT
            {
                internal int dx;
                internal int dy;
                internal int mouseData;
                internal uint dwFlags;
                internal uint time;
                internal UIntPtr dwExtraInfo;
            }


            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT
            {
                internal short wVk;
                internal short wScan;
                internal uint dwFlags;
                internal int time;
                internal UIntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct HARDWAREINPUT
            {
                internal int uMsg;
                internal short wParamL;
                internal short wParamH;
            }


            [DllImport("user32.dll", SetLastError = true)]
            private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

            private enum MouseMessages
            {
                WM_MOUSEMOVE = 0x0200,
                WM_LBUTTONDOWN = 0x0201,
                WM_LBUTTONUP = 0x0202,
                WM_LBUTTONDBLCLK = 0x203, 
                WM_RBUTTONDOWN = 0x0204,
                WM_RBUTTONUP = 0x0205,
                WM_RBUTTONDBLCLK = 0x206,
                WM_MBUTTONDOWN = 0x207,
                WM_MBUTTONUP = 0x208,
                WM_MBUTTONDBLCLK = 0x209,
                WM_MOUSEWHEEL = 0x020A,
                WM_XBUTTONDOWN = 0x20B,
                WM_XBUTTONUP = 0x20C,
                WM_XBUTTONDBLCLK = 0x20D,
                WM_MOUSEHWHEEL = 0x20E
            }

            private enum MouseEvents
            {
                MOUSEEVENTF_ABSOLUTE = 0x8000,
                MOUSEEVENTF_LEFTDOWN = 0x0002,
                MOUSEEVENTF_LEFTUP = 0x0004,
                MOUSEEVENTF_MIDDLEDOWN = 0x0020,
                MOUSEEVENTF_MIDDLEUP = 0x0040,
                MOUSEEVENTF_MOVE = 0x0001,
                MOUSEEVENTF_RIGHTDOWN = 0x0008,
                MOUSEEVENTF_RIGHTUP = 0x0010,
                MOUSEEVENTF_XDOWN = 0x0080,
                MOUSEEVENTF_XUP = 0x0100,
                MOUSEEVENTF_WHEEL = 0x0800,
                MOUSEEVENTF_HWHEEL = 0x01000
            }


            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool LogonUser( string lpszUsername,  string lpszDomain,  string lpszPassword,  int dwLogonType, int dwLogonProvider, ref IntPtr phToken );

            public MyServer()
            {
                _altPressed = false;
                _alreadyDown = new List<short>();
                _exch = new ECDiffieHellmanCng(256);
                _exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                _exch.HashAlgorithm = CngAlgorithm.Sha256;
                _publicKey = _exch.PublicKey.ToByteArray();


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
                                        _clipboardManager.InitializeShare();
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
                                                   _clipboardManager.SetClipboard(
                                                       (_tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString
                                                           (),
                                                       obj);
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
                        ProcessInputKeyboard(data);
                    }
                    else
                    {
                        ProcessInputMouse(data);
                    }
                       
                }
            }

            public void ProcessInputMouse(byte[] data)
            {
                int x = BitConverter.ToInt32(data, sizeof (bool));
                int y = BitConverter.ToInt32(data, sizeof (bool) + sizeof (Int32));
                int wParam = BitConverter.ToInt32(data, sizeof (bool) + 2*sizeof (Int32));
                short mouseData = BitConverter.ToInt16(data, sizeof (bool) + 3*sizeof (Int32));
                INPUT[] inputs = new INPUT[1];
                inputs[0].type = 0;
                inputs[0].U.mi.dx = x;
                inputs[0].U.mi.dy = y;
                inputs[0].U.mi.time = 0;
                inputs[0].U.mi.dwFlags = (uint)(MouseEvents.MOUSEEVENTF_ABSOLUTE); // TODO struct mouseevents
                switch (wParam) 
                {
                    case (int)MouseMessages.WM_LBUTTONDOWN:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_LEFTDOWN;
                        break;
                    case (int)MouseMessages.WM_LBUTTONUP:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_LEFTUP;
                        break;
                    case (int)MouseMessages.WM_RBUTTONDOWN:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_RIGHTDOWN;
                        break;
                    case (int)MouseMessages.WM_RBUTTONUP:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_RIGHTUP;
                        break;
                    case (int)MouseMessages.WM_MOUSEMOVE:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_MOVE;
                        break;
                    case (int)MouseMessages.WM_MBUTTONDOWN:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_MIDDLEDOWN;
                        break;
                    case (int)MouseMessages.WM_MBUTTONUP:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_MIDDLEUP;
                        break;
                    case (int)MouseMessages.WM_LBUTTONDBLCLK:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_LEFTDOWN | (uint) MouseEvents.MOUSEEVENTF_LEFTUP;
                        break;
                    case (int)MouseMessages.WM_RBUTTONDBLCLK:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_RIGHTDOWN | (uint) MouseEvents.MOUSEEVENTF_RIGHTUP;
                        break;
                    case (int)MouseMessages.WM_MBUTTONDBLCLK:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_MIDDLEDOWN | (uint) MouseEvents.MOUSEEVENTF_MIDDLEUP;
                        break;
                    case (int)MouseMessages.WM_MOUSEWHEEL:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_WHEEL;
                        inputs[0].U.mi.mouseData = mouseData;
                        break;
                    case (int)MouseMessages.WM_MOUSEHWHEEL:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)MouseEvents.MOUSEEVENTF_HWHEEL;
                        inputs[0].U.mi.mouseData = mouseData;
                        break;
                    case (int)MouseMessages.WM_XBUTTONDOWN:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_XDOWN;
                        break;
                    case (int)MouseMessages.WM_XBUTTONUP:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint) MouseEvents.MOUSEEVENTF_XUP;
                        break;
                    case (int)MouseMessages.WM_XBUTTONDBLCLK:
                        inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)MouseEvents.MOUSEEVENTF_XDOWN | (uint)MouseEvents.MOUSEEVENTF_XUP;
                        break;
                }
                SendInput(1, inputs, Marshal.SizeOf(inputs[0]));
                if (wParam == (int) MouseMessages.WM_LBUTTONDBLCLK || wParam == (int) MouseMessages.WM_RBUTTONDBLCLK ||
                    wParam == (int) MouseMessages.WM_MBUTTONDBLCLK || wParam == (int) MouseMessages.WM_XBUTTONDBLCLK)
                {
                    SendInput(1, inputs, Marshal.SizeOf(inputs[0]));    //resend the event if doubleclick
                }
            }

            public void ProcessInputKeyboard(byte[] data)
            {
                bool keyUp = BitConverter.ToBoolean(data, sizeof (bool));
                short vkCode = BitConverter.ToInt16(data, 2*sizeof (bool));
                if (!keyUp && (vkCode == 164 || vkCode == 165))
                {
                   // MessageBox.Show("alt pressed");
                    _altPressed = true;
                }

                else if (keyUp && (vkCode == 164 || vkCode == 165))
                {
                    _altPressed = false;
                    _alreadyDown.Clear();
                }
                   

                INPUT[] inputs = new INPUT[1];
                inputs[0].type = 1; // 1 = keyboard, 0 = mouse
                inputs[0].U.ki.dwFlags = keyUp ? (uint)2 : (uint)0;
                inputs[0].U.ki.wVk = vkCode;
                inputs[0].U.ki.time = 0;
                if (_altPressed && !keyUp && vkCode != 164 && vkCode != 165)
                {
                    if (_alreadyDown.Contains(vkCode))
                    {
                        inputs[0].U.ki.dwFlags = 2;
                        _alreadyDown.Remove(vkCode);
                    }
                    else
                    {
                        _alreadyDown.Add(vkCode);
                    }
                    // MessageBox.Show("alt pressed");

                }
               
                    //Console.WriteLine("alt NOT pressed");
                SendInput(1, inputs, Marshal.SizeOf(inputs[0]));     
               // Console.WriteLine("babba " + vkCode);
            }

        }
    
}

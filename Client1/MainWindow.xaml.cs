using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;

namespace Client1
{
    public enum protocol
    {
        udp,
        tcp,
    }
    public partial class MainWindow : Window
    {
        public static Random rnd = new Random();
        public static TcpClient clientTcp;
        public static UdpClient clientUdp;
        public static NetworkStream stream;
        public static IPEndPoint groupEp;
        public static Int32 portAddressTcp, portAddressUdp;
        public protocol type;
        public static bool OnLine = false, checkFlag=false;
        public static string error;
        DispatcherTimer timer;
        public static int countTimer;

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void ConnectButton(object sender, RoutedEventArgs e)
        {
            if (CheckConnectError()&&!OnLine)
            {
                ConnectServer("127.0.0.1",Username.Text);
                ConnectBtn.Content = "Disconnect";
                OnLine = true;
            }
            else if(CheckConnectError())
            {
                Disconnect(Username.Text);
                ConnectBtn.Content = "Connect";
                OnLine=false;
            }
        }

        private bool CheckConnectError()
        {
            if (Username.Text == "")
            {
                error = "You need to insert your name";
                SetShowError();
                return false;
            }
            return true;

        }

        private void SetShowError()
        {
            countTimer = 5;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += ShowErrorConnect;
            this.Dispatcher.Invoke(() =>
            {
                ConnectError.Text = "You need to insert your name please";
            });
            timer.Start();
        }

        private void ShowErrorConnect(object? sender, EventArgs e)
        {
            countTimer--;
            if (countTimer == 0)
            {
                ConnectError.Text = "";
                timer.Stop();
            }
        }

        static void Disconnect(string name)
        {
            
            string msg = $"{name}/Disconnect";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);            
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent: {0}", msg);
            stream.Close();
            clientTcp.Close();
        }

        private void can_Loaded(object sender, RoutedEventArgs e)
        {
            portAddressTcp = rnd.Next(5000, 13000);
            portAddressUdp = rnd.Next(15000, 20000);
            groupEp = new IPEndPoint(IPAddress.Any, 0);
            Thread t1 = new Thread(AlwaysReciveTcp);
            Thread t2=new Thread(AlwaysReciveUdp);
            t1.Start();
            t2.Start();            
            type = protocol.tcp;
            tcpChoose.IsEnabled = false;
            udpChoose.IsEnabled = true;
        }

        private void AlwaysReciveUdp()
        {
            checkFlag = true;
            while(true)
            {
                if (clientUdp!=null&&OnLine)
                {              
                    Byte[] receiveBytes = clientUdp.Receive(ref groupEp);
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    addTextToReciveBox(returnData);
                }
            }
        }

        private void AlwaysReciveTcp()
        {            
            while(true)
            {
                if (OnLine)
                {                    
                    Byte[] data = new Byte[256];
                    NetworkStream stream = clientTcp.GetStream();
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    CheckReciveMessage(data, bytes);
                    
                }
            }   
        }

        private void CheckReciveMessage(byte[] data, int bytes)
        {
            String reciveMsg = String.Empty;
            reciveMsg = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            string[] msg = reciveMsg.Split('-');
            if (msg[msg.Length - 1] == "Contacts")
            {
                ChangeContactList(reciveMsg.Replace("-Contacts", ""));
            }
            else
            {
                addTextToReciveBox(reciveMsg.Replace("-Message", ""));
            }
        }

        private void ChangeContactList(string reciveMsg)
        {
            this.Dispatcher.Invoke(() =>
            {
                contactsComboBox.Items.Clear();
                string[] onlineMembers = reciveMsg.Split('/');
                for (int i = 0; i < onlineMembers.Length-1; i++)
                {
                    contactsComboBox.Items.Add(onlineMembers[i]);
                }
            });
        }

        private void addTextToReciveBox(string reciveMsg)
        {
            this.Dispatcher.Invoke(() =>
            {
                reciveBox.Text += $"{reciveMsg}\n";
            });
            
        }

        public static void ConnectServer(String server,string name)
        {
            TcpConnectionServer(server, name);
            UdpConnectionServer(name);
        }

        private static void UdpConnectionServer(string name)
        {
            Int32 portUdp = 14000;
            clientUdp = new UdpClient(portAddressUdp);
            try
            {
                clientUdp.Connect("127.0.0.1", portUdp);
                string message = $"{name}/{portAddressUdp}/ConnectUdp";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                clientUdp.Send(data);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Function name: UdpConnectionServer\nException: {e.Message}");
            }
            
        }

        private static void TcpConnectionServer(string server, string name)
        {
            Int32 port = 13000;
            clientTcp = new TcpClient(server, port);
            try
            {
                string message = $"{name}/{portAddressTcp}/ConnectTcp";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                stream = clientTcp.GetStream();
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);
            }catch(Exception e)
            {
                Debug.WriteLine($"Function name: TcpConnectionServer\nException: {e.Message}");
            }
        }

        private void SendButton(object sender, RoutedEventArgs e)//Send msg
        {
            string msg = $"{Username.Text}/{contactsComboBox.SelectedItem}/{toSend.Text}/Send";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            if (type == protocol.tcp)
            {
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", msg);
            }
            else
            {
                clientUdp.Send(data);
            }

        }

        private void ChooseTcp(object sender, RoutedEventArgs e) {
            type = protocol.tcp;
            tcpChoose.IsEnabled = false;
            udpChoose.IsEnabled = true;
        }

        private void ChooseUdp(object sender, RoutedEventArgs e)
        {
            type = protocol.udp;
            tcpChoose.IsEnabled = true;
            udpChoose.IsEnabled = false;
        }
    }
}

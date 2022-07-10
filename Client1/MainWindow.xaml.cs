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
        public static Int32 portAddressTcp;
        public static Int32 portAddressUdp;
        public protocol type;
        public static ComboBox contactsComboBox;
        public static TextBlock reciveBox;
        public static bool OnLine = false, checkFlag=false;
        public MainWindow()
        {
            InitializeComponent();            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!OnLine)
            {
                ConnectServer("127.0.0.1",Username.Text);
                ConnectBtn.Content = "Disconnect";
                OnLine = true;
            }
            else
            {
                Disconnect(Username.Text);
                ConnectBtn.Content = "Connect";
                OnLine=false;
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
            Thread t1 = new Thread(() => AlwaysReciveTcp());
            Thread t2=new Thread(() => AlwaysReciveUdp());
            t1.Start();
            t2.Start();
            contactsComboBox = new ComboBox();
            contactsComboBox.Width = 125;
            contactsComboBox.Height = 35;
            Canvas.SetLeft(contactsComboBox, 250);
            Canvas.SetTop(contactsComboBox, 97);
            reciveBox = new TextBlock();
            reciveBox.Width = 322;
            reciveBox.Height = 250;
            Canvas.SetLeft(reciveBox, 512);
            Canvas.SetTop(reciveBox, 226);
            reciveBox.HorizontalAlignment = HorizontalAlignment.Left;
            reciveBox.VerticalAlignment = VerticalAlignment.Center;
            can.Children.Add(contactsComboBox);
            can.Children.Add(reciveBox);
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
                    String reciveMsg = String.Empty;
                    NetworkStream stream = clientTcp.GetStream();
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    reciveMsg = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    string[] msg=reciveMsg.Split('-');
                    if (msg[msg.Length-1] =="Contacts")
                    {
                        ChangeContactList(reciveMsg.Replace("-Contacts",""));
                    }
                    else
                    {
                        addTextToReciveBox(reciveMsg.Replace("-Message", ""));
                    }
                }
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
            try
            {
                /*----- Tcp Connection -----*/
                Int32 port = 13000;
                clientTcp = new TcpClient(server, port);
                string message = $"{name}/{portAddressTcp}/ConnectTcp";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                stream = clientTcp.GetStream();
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);

                /* --------Contact List  --------*/
                data = new Byte[256];
                string onlineMem = string.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                onlineMem = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", onlineMem);
                onlineMem = onlineMem.Replace("-Contacts", "");
                string[] namesOnline = onlineMem.Split('/');
                for (int i = 0; i < namesOnline.Length; i++)
                {
                    if (namesOnline[i] !="Contacts")
                        contactsComboBox.Items.Add(namesOnline[i]);
                }

                /*---- Udp Connection ----*/
                Int32 portUdp = 14000;
                clientUdp=new UdpClient(portAddressUdp);
                clientUdp.Connect("127.0.0.1", portUdp);
                message = $"{name}/{portAddressUdp}/ConnectUdp";
                data = System.Text.Encoding.ASCII.GetBytes(message);
                clientUdp.Send(data);              
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)//Send msg
        {

            if (type == protocol.tcp)
            {
                string msg = $"{Username.Text}/{contactsComboBox.SelectedItem}/{toSend.Text}/Send";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", msg);
            }
            else
            {
                string msg = $"{Username.Text}/{contactsComboBox.SelectedItem}/{toSend.Text}/Send";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                clientUdp.Send(data);
            }

        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            type = protocol.tcp;
            tcpChoose.IsEnabled = false;
            udpChoose.IsEnabled = true;
        }

        private void udpChoose_Click(object sender, RoutedEventArgs e)
        {
            type = protocol.udp;
            tcpChoose.IsEnabled = true;
            udpChoose.IsEnabled = false;
        }
    }
}

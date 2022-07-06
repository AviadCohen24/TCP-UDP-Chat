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
        public static TcpClient client;
        public static TcpListener reciver;
        public static NetworkStream stream;
        public static Int32 portAddress;
        public protocol type=protocol.tcp;
        public static ComboBox contactsComboBox;
        public static TextBlock reciveBox;
        public static bool OnLine = false;
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
            client.Close();
        }

        private void can_Loaded(object sender, RoutedEventArgs e)
        {
            portAddress = rnd.Next(5000, 13000);
            Thread t1 = new Thread(() => AlwaysRecive());
            t1.Start();
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

        }

        private void AlwaysRecive()
        {
            
            while(true)
            {
                if (OnLine)
                {
                    
                    Byte[] data = new Byte[256];
                    String reciveMsg = String.Empty;
                    NetworkStream stream = client.GetStream();
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    reciveMsg = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    addTextToReciveBox(reciveMsg);
                }
            }
            
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
                Int32 port = 13000;
                client = new TcpClient(server, port);               
                string message = $"{name}/{portAddress}/Connect";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                stream = client.GetStream();
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);

                /* --------Contact List  --------*/
                data = new Byte[256];
                String onlineMem = String.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                onlineMem = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", onlineMem);
                string [] namesOnline=onlineMem.Split('/');
                for(int i = 0; i < namesOnline.Length; i++)
                {
                    contactsComboBox.Items.Add(namesOnline[i]);
                }

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
            
            
            string msg = $"{contactsComboBox.SelectedItem}/{toSend.Text}/Send";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent: {0}", msg);
            

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            type = protocol.tcp;
            tcpChoose.IsEnabled = false;
            udpChoose.IsEnabled = false;
        }

        private void udpChoose_Click(object sender, RoutedEventArgs e)
        {
            type = protocol.udp;
            tcpChoose.IsEnabled = false;
            udpChoose.IsEnabled = false;
        }
    }
}

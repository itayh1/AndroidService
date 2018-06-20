using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using ImageService.LoggingModal;

namespace ImageService
{
    public class Communicator
    {
        private static Mutex mutex = new Mutex();

        private readonly int server_port = 6000;
        private TcpListener listener;
        private List<TcpClient> clients;
        private LoggingService loggingService;
        public event EventHandler<CommandRecievedEventArgs> OnCommandRecieved;
        public ConfigurationData Configurations;

        public Communicator(ConfigurationData configData, LoggingService loggingS)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), this.server_port);
            this.listener = new TcpListener(ep);
            this.loggingService = loggingS;
            this.Configurations = configData;
            this.clients = new List<TcpClient>();
        }
        public void Start()
        {
            this.listener.Start();

            while (true)
            {
                try
                {
                    Console.WriteLine("Waiting for connections...");
                    TcpClient client = this.listener.AcceptTcpClient();
                    this.clients.Add(client);
                    Console.WriteLine("New client connection");
                    this.loggingService.Log(string.Format("Client with socket {0} connected", client.ToString()), MessageTypeEnum.INFO);
                    HandleClient(client);
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                    break;
                }
            }
            Console.WriteLine("Server stopped");
        }

        public void HandleClient(TcpClient client)
        {
            bool running = true;
            NetworkStream stream = client.GetStream();
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);
            string msg = string.Empty;
            byte[] bytes = new byte[4096];
            int bytesRead;
            int picSize;
            string picName;

            while (running)
            {
                try
                {
                    //name 
                    bytesRead = reader.Read(bytes, 0, bytes.Length);
                    picName = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                    Console.WriteLine(picName);
                    //size

                    writer.Write("ok for name");
                    
                    bytesRead = reader.Read(bytes, 0, bytes.Length);
                    msg = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                    Console.WriteLine(msg);
                    if (!int.TryParse(msg, out picSize)) break;
                    bytes = new byte[picSize];

                    writer.Write("ok for size");

                    bytesRead = stream.Read(bytes, 0, bytes.Length);
                    int tempBytes = bytesRead;

                    while (tempBytes < bytes.Length)
                    {
                        bytesRead = stream.Read(bytes, tempBytes, bytes.Length - tempBytes);
                        tempBytes += bytesRead;
                    }
                    writer.Write("ok for picture");

                    File.WriteAllBytes(this.Configurations.Handlers[0] + @"\" + picName, bytes);
                }
                catch (Exception ex)
                {
                    running = false;
                    clients.Remove(client);
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine("closing client");
        }

        public void Close()
        {
            CommandRecievedEventArgs command = new CommandRecievedEventArgs((int)CommandEnum.ExitCommand, null, string.Empty);
            // Send for every client to exit
            listener.Stop();
        }
    }
}

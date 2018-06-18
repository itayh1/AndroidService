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

        private readonly int server_port = 8888;
        private TcpListener listener;
        private List<TcpClient> clients;
        private LoggingService loggingService;
        public event EventHandler<CommandRecievedEventArgs> OnCommandRecieved;
        public ConfigurationData Configurations;

        public Communicator(ConfigurationData configData, LoggingService loggingS)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.22.128"), this.server_port);
            this.listener = new TcpListener(ep);
            this.loggingService = loggingS;
            this.Configurations = configData;
            this.clients = new List<TcpClient>();
        }
        public void Start()
        {
            this.listener.Start();
            Console.WriteLine("Waiting for connections...");

            //Task task = new Task(() =>
            //{
                while (true)
                {
                    try
                    {
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
           // });
           // task.Start();
        }

        public void HandleClient(TcpClient client)
        {

            Task task = new Task(() =>
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
                        picName = reader.ReadString();
                        //bytesRead = stream.Read(bytes, 0, bytes.Length);
                        //picName = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                        //size
                        msg = reader.ReadString();
                        if (!int.TryParse(msg, out picSize)) break;
                        bytes = new byte[picSize];

                        bytesRead = stream.Read(bytes, 0, bytes.Length);
                        int tempBytes = bytesRead;
                        //byte[] bytesCurrent;
                        while (tempBytes < bytes.Length)
                        {
                            //bytesCurrent = new byte[int.Parse(picSize)];
                            bytesRead = stream.Read(bytes, tempBytes, bytes.Length - tempBytes);
                            tempBytes += bytesRead;
                        }
                        writer.Write("ok");
                        File.WriteAllBytes(this.Configurations.Handlers[0] + @"\" + picName + ".png", bytes);
                    }
                    catch (Exception ex)
                    {
                        running = false;
                        clients.Remove(client);
                        Console.WriteLine(ex.Message);
                    }
                }
                Console.WriteLine("closing client");
            });
            task.Start();
        }
        
        public void Close()
        {
            CommandRecievedEventArgs command = new CommandRecievedEventArgs((int)CommandEnum.ExitCommand, null, string.Empty);
            // Send for every client to exit.
            listener.Stop();
        }
    }
}

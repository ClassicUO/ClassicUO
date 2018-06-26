using System;
using ClassicUO.Network;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {

            string ip = args[1];
            ushort port = ushort.Parse(args[3]);
            string username = args[5];
            string password = args[7];
            string charname = args[9];
            
           
            bool startKeepAlive = false;
            DateTime keepAliveTime = DateTime.Now;
            bool isRunning = true;



            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(CLIENT_VERSION.CV_70331);



            NetClient client = new NetClient();
            client.Connect(ip, port);

            NetClient.PacketReceived += (sender, e) =>
            {
                Console.WriteLine(">> Received\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length);
                switch (e.ID)
                {
                    case 0xA8: // servers list
                        client.Send(new PChooseServer(0));
                        break;
                    case 0x8C: // server relay, enable compression!
                        client.EnableCompression();
                        e.Skip(4 + 2); // ip + port
                        client.Send(new PGameLoginAccount(e.ReadUInt(), username, password));
                        break;
                    case 0xA9: // chars list
                        client.Send(new PLoginCharacter(charname, 0, BitConverter.ToUInt32(new byte[] { 127, 0, 0, 1 }, 0)));
                        break;
                    case 0xBD: // version request
                        client.Send(new PClientVersion(new byte[4] { 7, 0, 59, 8 }));
                        break;
                    case 0x1B: // login confim
                        startKeepAlive = true;
                        ConsoleColor prev = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("You are into the world!");
                        Console.ForegroundColor = prev;
                        break;
                    case 0x55:
                        if (!startKeepAlive)
                        {
                            prev = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" >> MISSED LOGIN CONFIRM!! <<");
                            Console.ForegroundColor = prev;
                        }
                        break;
                    case 0xF0: // negotiate features:
                        client.Send(new PNegotiateFeatures());
                        break;
                }
            };

            NetClient.PacketSended += (sender, e) =>
            {
                Console.WriteLine("<< Sended\t\tID:   0x{0:X2}\t\t Length:   {1}", e.ID, e.Length);
            };

            NetClient.Connected += (sender, e) =>
            {
                Console.WriteLine("Connected!");

                client.Send(new PSeed(new byte[4] { 7, 0 , 59, 8}));
                client.Send(new PLoginAccount(username, password));
            };
            NetClient.Disconnected += (sender, e) =>
            {
                isRunning = false;
                Console.WriteLine("Disconnected!");
            };

            while (isRunning)
            {              
                client.Slice();

                if (startKeepAlive && DateTime.Now > keepAliveTime)
                {
                    client.Send(new PPingPacket());
                    keepAliveTime = DateTime.Now.AddSeconds(10);
                }

                System.Threading.Thread.Sleep(10);
            }

            Console.ReadLine();
        }
    }
}

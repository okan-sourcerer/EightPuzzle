using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EightPuzzle
{

    [SupportedOSPlatform("windows")]
    class Program
    {
        public static IPAddress LOCAL_IP = IPAddress.Parse("127.0.0.1");
        public static IPAddress localIP = GetLocalIPv4Address();
        public static IPAddress publicIP;

        public static int[][] initialBoard;

        public static List<TcpClient> clients = new List<TcpClient>();

        static void Main(string[] args)
        {
            try
            {
                publicIP = IPAddress.Parse(GetPublicIPv4Address());
            }
            catch (HttpRequestException) { }
            _ = new InputHandler();


            Console.ReadKey();
        }

        public static void HostServer(int playerCount)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8001); // create server socket

            listener.Start(); // start listening the port.

            TcpClient host = listener.AcceptTcpClient(); // first one who connects is probably the host. If he exits, close server. Talk to the host to get the number of players.
            clients.Add(host); // wait for client connection and add client.

            if(playerCount != 1)
            {
                playerCount = GetPlayerCount(host);
            }

            XmlSerializer serializer = new(typeof(int[]));

            SendMessage("\nSet your board dimensions (Format:x y): ", host); // ask board size
            string sizes = ReceiveMessage(host);
            int[] dimensions = (int[])serializer.Deserialize(new StringReader(sizes)); // get array from it

            Task hostTask = ClientListener(host);
            while (clients.Count != playerCount)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client); // wait for client connection and add client.
            }
            bool solvable = false;
            while (!solvable) // create a solvable board.
            {
                initialBoard = EightPuzzle.CreateBoard(dimensions[0], dimensions[1]);
                solvable = isSolvable(initialBoard);
            }

            DistributeBoard(initialBoard);

            Task[] moveListeners = new Task[playerCount];
            for (int i = 0; i < clients.Count; i++)
            {
                TcpClient cli = clients[i];
                moveListeners[i] = MoveListener(cli);
            }
            hostTask.Wait();
            listener.Stop();
        }

        static async Task ClientListener(TcpClient client) // listens for the moves of the clients. Sends every move to other clients for syncing. Delivers users messages to (maybe)
        {
            await Task.Run(() => {
                while (client.Connected)
                {
                    Thread.Sleep(100); // check if host is still connected 60 times a second.
                }
                Console.WriteLine("Closed client connection");
                client.Close();
            });
        }

        static int GetPlayerCount(TcpClient host)
        {
            SendMessage("\nPlease enter number of players to join \n" +
                    "(Game does not start until number of players have joined 1-4): ", host);

            return Int32.Parse(ReceiveMessage(host));
        }

        public static void SendMessage(string message, TcpClient client)
        {
            byte[] buf = Encoding.UTF8.GetBytes(message);
            client.GetStream().Write(buf, 0, buf.Length);
        }

        public static string ReceiveMessage(TcpClient client)
        {
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int length = client.GetStream().Read(buffer, 0, buffer.Length);
            byte[] final = new byte[length];
            Array.Copy(buffer, final, length);
            return Encoding.UTF8.GetString(final);
        }

        // listen to the clients. Send move info that client sent to all.
        public static void SendMove(TcpClient sender, int playerNo, int move)
        {
            XmlSerializer serializer = new(typeof(int[])); // will contain player number and move.
            serializer.Serialize(sender.GetStream(), new int[] { playerNo, move });
        }

        public static int[] ReceiveMove(TcpClient receiver)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(int[]));
            return (int[])serializer.Deserialize(new StringReader(ReceiveMessage(receiver)));
        }

        public static async Task MoveListener(TcpClient client)
        {
            await Task.Run(() => 
            {
                while (client.Connected)
                {
                    try
                    {
                        int[] moveInfo = ReceiveMove(client); // client moved.
                        foreach (TcpClient cl in clients)// send client's move to all players.
                        {
                            if (client != cl)
                            {
                                SendMove(cl, moveInfo[0], moveInfo[1]);
                            }
                        }
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Client left");
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Send the created board to all clients in the list.
        /// </summary>
        /// <param name="board">Board that is created</param>
        public static void DistributeBoard(int[][] board)
        {
            XmlSerializer serial = new(typeof(int[][]));
            XmlSerializer intSerial = new(typeof(int[]));
            for (int i = 0; i < clients.Count; i++)
            {
                TcpClient client = clients[i];
                serial.Serialize(client.GetStream(), board); // send board to all clients
                Thread.Sleep(100); // both streams are representing different things. There needs to be a
                                    // delay for them to work apart.
                intSerial.Serialize(client.GetStream(), new int[] { i, clients.Count}); // send current position of the client
            }
        }

        public static string GetPublicIPv4Address() => new HttpClient().GetStringAsync("http://ifconfig.me").GetAwaiter().GetResult().Replace("\n", "");

        public static IPAddress GetLocalIPv4Address()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                try
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
                catch (SocketException) { }
            }

            return IPAddress.Parse("127.0.0.1");
            
        }

        static int getInvCount(int[][] arr)
        {
            int inv_count = 0;
            for (int i = 0; i < arr[0].Length - 1; i++)
                for (int j = i + 1; j < arr.Length; j++)

                    // Value 0 is used for empty space
                    if (arr[j][i] > 0 && arr[j][i] > 0 &&
                            arr[j][i] > arr[i][j])
                        inv_count++;
            return inv_count;
        }

        // This function returns true
        // if given 8 puzzle is solvable.
        static bool isSolvable(int[][] puzzle)
        {
            // Count inversions in given 8 puzzle
            int invCount = getInvCount(puzzle);

            // return true if inversion count is odd.
            return (invCount % 2 == 0);
        }
    }
}

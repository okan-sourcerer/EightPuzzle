using System;
using System.Threading;
using System.Runtime.Versioning;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace EightPuzzle
{

    public static class Action
    {
        public const int WON = 0;
        public const int UP = 1;
        public const int RIGHT = 2;
        public const int DOWN = 3;
        public const int LEFT = 4;
    }

    [SupportedOSPlatform("windows")]
    class InputHandler
    {

        public readonly ConsoleKey keyQuit = ConsoleKey.End; // keys can be in a different static subclass like KeyMap. If this gets complicated, migrate kyes to there.
        public readonly ConsoleKey keyRestart = ConsoleKey.Home; // restart the board for local play

        private EightPuzzle _puzzle;

        private Thread server;
        private TcpClient client;

        private int currentPos;

        private bool otherWon = false;

        public InputHandler()
        {
            StartMenu();
        }


        private void GameLoop()
        {
            bool run = true;
            bool won = false;

            while (run && !won)
            {
                bool moved = false;
                int lastAction = 0;

                if (otherWon)
                {
                    break;
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    if(key == keyQuit)
                    {
                        Console.WriteLine("Quitting the game...");
                        break;
                    }
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            moved = _puzzle.Move(0, -1);
                            lastAction = Action.UP;
                            break;
                        case ConsoleKey.DownArrow:
                            moved = _puzzle.Move(0, 1);
                            lastAction = Action.DOWN;
                            break;
                        case ConsoleKey.LeftArrow:
                            moved = _puzzle.Move(-1, 0);
                            lastAction = Action.LEFT;
                            break;
                        case ConsoleKey.RightArrow:
                            moved = _puzzle.Move(1, 0);
                            lastAction = Action.RIGHT;
                            break;
                        case ConsoleKey.Q:
                            run = false;
                            client.Close();
                            Console.WriteLine("You disconected from the server.");
                            break;
                        case ConsoleKey.Enter: // başkalarıyla oynandığı zaman entera basınca mesajı TCP üzerinden gönderecek şekilde düzenlenmeli.
                            Console.WriteLine();
                            break;
                        case var value when value==keyRestart: // restart the game.
                            _puzzle.Start(_puzzle.GetBoard()[0].Length, _puzzle.GetBoard().Length);
                            break;
                    }

                    if (moved)
                    {
                        // send data to the server indicating user has moved. LastAction indicates last move.
                        // send move data with user position and move
                        Program.SendMove(client, currentPos, lastAction);
                    }
                }
                won = _puzzle.HasWon();  // user won. quit.
            }
            if (won && !otherWon)
            {
                Program.SendMove(client, currentPos, Action.WON);
                Console.WriteLine("You have won! Congratulations! {0} {1}", won, otherWon);
                Console.ReadLine();
            }
        }

        private void StartMenu()
        {
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.WriteLine("Welcome to Eight Puzzle!\n" +
                    "1- Start solo local game\n" +
                    "2- Start local server\n" +
                    "3- Join online server");
                key = Console.ReadKey().Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1: // numpad and normal keys both work.
                        StartServer(1);
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        StartServer(2);
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        JoinServer();
                        break;
                }

            } while (key != keyQuit);
            
        }

        private int[] FindBoardSize()
        {
            int sizeX = 3, sizeY = 3;
            string boardSize = Console.ReadLine();
            if (boardSize.Trim().Contains(" "))
            {
                var sizes = boardSize.Split(" ", 2);
                try
                {
                    sizeX = int.Parse(sizes[0]);
                    sizeY = int.Parse(sizes[1]);
                }catch (Exception)
                {
                    Console.WriteLine("Format size is not recognized. 3x3 board selected as default.");
                }
            }
            return new int[2]{ sizeX, sizeY};
        }

        /// <summary>
        /// Host communicates with the server for determining server configuration. Player count board etc.
        /// Then starts the game by calling GameLoop
        /// </summary>
        /// <param name="count">Which option is selected in the switch statement</param>
        private void StartServer(int count)
        {
            server = new Thread(() => Program.HostServer(count)); // game is started by the server.
            server.Start();
            client = new();
            client.Connect(Program.localIP, 8001); // if user is starting the server, we are the host and we need localIp to connect to the server.

            if(count != 1) // number of players is not defined. Send server that info
            {

                string playerCountRequest = Program.ReceiveMessage(client);
                Console.WriteLine(playerCountRequest);

                bool correctFormat = false;
                while (!correctFormat)
                {
                    string received = Console.ReadLine();
                    count = int.Parse(received.Trim());
                    if (count > 1 || count < 4)
                    {
                        correctFormat = true;
                    }
                }
                Program.SendMessage($"{count}", client); // send number of players to the server
            }

            Console.WriteLine(Program.ReceiveMessage(client)); // wait until everyone joins.

            int[] sizes = FindBoardSize();

            XmlSerializer serializer = new XmlSerializer(typeof(int[]));
            serializer.Serialize(client.GetStream(), sizes); // send board sizes

            // bütün kullanıcılar oyuna bağlandı mı kontrol et.

            if(count != 1) // multiplayer game
            {
                Console.WriteLine("Waiting other users to join..");
            }

            SetGame();
            // user won. Notice everyone. Make everyone quit too.
            client.Close();
            server.Join();
        }

        private void JoinServer()
        {
            Console.WriteLine("Please enter server ip address you want to join:");
            string ip = Console.ReadLine();
            client = new();
            client.Connect(System.Net.IPAddress.Parse(ip), 8001);

            SetGame();
        }

        private void SetGame()
        {
            XmlSerializer serializer = new(typeof(int[][]));
            string gameBoard = Program.ReceiveMessage(client); // wait server to send the board
            int[][] board = (int[][])serializer.Deserialize(new StringReader(gameBoard));

            serializer = new XmlSerializer(typeof(int[]));
            Console.Clear();

            // format is { this client's position, total player count}
            int[] info = (int[])serializer.Deserialize(new StringReader(Program.ReceiveMessage(client)));
            currentPos = info[0];
            _ = GetUserInput();
            UIHandler.Instance().StartGame(info[1], board); // at this point, number of users is fixed value
            _puzzle = UIHandler.Instance().GetPuzzle(currentPos);
            GameLoop();
        }

        public async Task GetUserInput() // listen user input for every player/client
        {
            await Task.Run(() =>
            {
                while (client.Connected)
                {
                    // moveinfo formnat is = {moved player, move}
                    int[] moveInfo = Program.ReceiveMove(client); // got a move from another player
                    //UIHandler.Instance().GetPuzzle(moveInfo[0]).Move(moveInfo[1]);
                    if (moveInfo[1] == Action.WON)
                    {
                        Console.WriteLine($"Player {moveInfo[0]} WON!");
                        otherWon = true;
                    }
                    else
                    {
                        UIHandler.Instance().GetPuzzle(moveInfo[0]).Move(moveInfo[1]);
                    }
                    // apply the move to the screen.

                    // ÖNEMLİ 20.03.22 : Bütün kullanıcıların boardunu matrix olarak tut. değişiklikleri orada da belirt
                    // Zaten initial state'ler aynı olacaklar. Serverdan gelen hareketleri uygula. Böylece boş alanın nerede
                    // olduğu da kullanılarak dışarıdan değer almadan değiştirilebilecek.
                }
            });
        }
    }
}

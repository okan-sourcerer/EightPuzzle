using System;
using System.Runtime.Versioning;

namespace EightPuzzle
{
    /// this class will handle UI and display the board with different colored numbers.
    /// This class will also handle logging (can be isolated if necessary)
    [SupportedOSPlatform("windows")]
    class UIHandler
    {
        public const int LEFT_OFFSET = 50;
        public const int TOP_OFFSET = 0;

        private static UIHandler handler;

        private int horizontalCharDivisionPoint = 6; // this will determine the width of the board should be even
        private int verticalCharDivisionPoint = 4; // this will determine the height of the board. should be even
        private int[] colors;

        public int BoardWidth { get; private set; }
        public int BoardHeight { get; private set; }

        public int NumberOfPlayers { get; private set; }
        private PlayerUIElement[] players;

        private object _sync = new object();


        private UIHandler() {}

        public static UIHandler Instance()
        {
            if(handler == null)
            {
                handler = new UIHandler();
            }
            return handler;
        }

        /// <summary>
        /// Example of a 3x3 board. 
        /// #############
        /// #   #   #   #
        /// #   #   #   #
        /// #   #   #   #
        /// #############
        /// #   #   #   #
        /// #   #   #   #
        /// #   #   #   #
        /// #############
        /// #   #   #   #
        /// #   #   #   #
        /// #   #   #   #
        /// #############
        /// </summary>
        /// <param name="board">
        /// matrix representing the board
        /// </param>
        /// <param name="width">
        /// width of the board in elements. We cannot get the amount of element from the matrix, it should be given.
        /// </param>
        /// <param name="height">
        /// height of the matrix.
        /// </param>
        public void PrintBoard(int[][] board, PlayerUIElement element)
        {
            int width = board[0].Length, height = board.Length;
            CreateNumberColors(width, height);

            int widthRequiredChar = width * horizontalCharDivisionPoint + 1; // find horizontal # chars
            BoardWidth = widthRequiredChar;
            int heightRequiredChar = height * verticalCharDivisionPoint + 1; // find vertical # chars
            BoardHeight = heightRequiredChar;

            bool printedAll = false;

            int currentX = 0, currentY = 0; // initialize

            Console.CursorLeft = 0;
            while (!printedAll)
            {
                if(currentY % verticalCharDivisionPoint == 0 || currentX % horizontalCharDivisionPoint == 0)
                {
                    Console.Write("#");
                }
                else
                {
                    Console.Write(" ");
                }

                currentX++; // increment the x axis.
                if(currentX == widthRequiredChar) // we are at the end of the line. Go to the next line.
                {
                    currentX = 0;
                    currentY++;
                    Console.Write("\n");
                }
                if(currentY == heightRequiredChar)
                {
                    printedAll = true;
                }
            }
            RepositionBoard(widthRequiredChar, heightRequiredChar, element);
        }

        /// <summary>
        /// Repositions the board with Console.MoveBufferArea with OFFSETS.
        /// </summary>
        /// <param name="width">Horizontal length of the board</param>
        /// <param name="height">Vertical height of the board</param>
        private void RepositionBoard(int width, int height, PlayerUIElement player)
        {
            int topPos = Console.CursorTop;
            Console.MoveBufferArea(0, topPos - height, width, height, player.offsetLeft, player.offsetTop);
            Console.CursorTop -= height; // Reset the cursor position so there is no gap in the between.
        }

        public void PrintValues(int[][] board, PlayerUIElement player)
        {
            int cursorOldTop = Console.CursorTop;
            int cursorOldLeft = Console.CursorLeft;

            // find the middle point for a single cell
            int middleOfHorizontal = horizontalCharDivisionPoint / 2;
            int middleOfVertical = verticalCharDivisionPoint / 2;

            int totalOffsetTop = player.offsetTop + middleOfVertical;
            for(int i = 0; i < board.Length; i++)
            {
                int totalOffsetLeft = player.offsetLeft + middleOfHorizontal;
                for(int j = 0; j < board[0].Length; j++) // on the same line.
                {
                    Console.SetCursorPosition(totalOffsetLeft, totalOffsetTop);
                    Print(board[i][j], board[i][j] != 0 ? colors[board[i][j] - 1] : 0);
                    totalOffsetLeft += horizontalCharDivisionPoint;
                }
                totalOffsetTop += verticalCharDivisionPoint;
            }

            Console.SetCursorPosition(cursorOldLeft, cursorOldTop);
        }

        /// <summary>
        /// This method will print an integer to the console with given color then go back to the old color.
        /// </summary>
        /// <param name="number">Int to print</param>
        /// <param name="color">Color</param>
        private void Print(int number, int color)
        {
            if (number == 0)
            {
                return;
            }
            ConsoleColor consoleColor = Console.ForegroundColor;

            Console.ForegroundColor = (ConsoleColor)color;
            Console.Write(number);
            Console.ForegroundColor = consoleColor;
        }

        public void SwitchElements(int position, EightPuzzle.Position emptyCell, EightPuzzle.Position newCell, int value) // Element ile uyumlu yap
        {
            lock (_sync) // Thread safe printing.
            {
                PlayerUIElement element = players[position];
                int valueLeftOffset = element.offsetLeft + horizontalCharDivisionPoint / 2 + emptyCell.x * horizontalCharDivisionPoint;
                int valueTopOffset = element.offsetTop + verticalCharDivisionPoint / 2 + emptyCell.y * verticalCharDivisionPoint;

                int cursorOldTop = Console.CursorTop;
                int cursorOldLeft = Console.CursorLeft;

                Console.SetCursorPosition(valueLeftOffset, valueTopOffset);
                Print(value, colors[value - 1]);

                valueLeftOffset = element.offsetLeft + horizontalCharDivisionPoint / 2 + newCell.x * horizontalCharDivisionPoint;
                valueTopOffset = element.offsetTop + verticalCharDivisionPoint / 2 + newCell.y * verticalCharDivisionPoint;

                Console.SetCursorPosition(valueLeftOffset, valueTopOffset);
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = Console.BackgroundColor;
                Console.Write("  ");

                Console.ForegroundColor = color;

                Console.SetCursorPosition(cursorOldLeft, cursorOldTop);
            }
        }

        private void CreateNumberColors(int width, int height)
        {
            Random rnd = new Random();
            int totalElements = width * height - 1; // 1 is empty.

            colors = new int[totalElements];
            for(int i = 0; i < totalElements; i++)
            {
                colors[i] = rnd.Next(1, 16); // excluded Black. It is a common background color.
            }
        }

        public EightPuzzle GetPuzzle(int pos)
        {
            return players[pos].puzzle;
        }

        /// <summary>
        /// Sets the number of players and Draws the board for them. Also calculates the offset values of the players
        /// </summary>
        /// <param name="players">Number of players in the game</param>
        public void StartGame(int playerCount, int[][] board)
        {
            NumberOfPlayers = playerCount;
            players = new PlayerUIElement[playerCount];

            int width = board[0].Length * horizontalCharDivisionPoint + 1;
            int height = board.Length * verticalCharDivisionPoint + 1;
            int additionalTopOffset = -height - 2; // if player count is big enough that all players cannot fit into the same line

            for(int i = 0; i < playerCount; i++)
            {
                if(i % 2 == 0)
                {
                    additionalTopOffset += height + 2;
                }
                players[i] = new PlayerUIElement(i, LEFT_OFFSET + ((i % 2) * (width + 5)), TOP_OFFSET + additionalTopOffset, board);
                PrintBoard(board, players[i]);
                PrintValues(board, players[i]);
            }
        }

        //TODO 19.03.22 : UIElement her bir kullanıcının bilgilerini içermeli. Kullanıcılar bir hareket
        // yaptıklarında kendi ekranlarında anında gerçekleşmeli, sunucuya hangi hareketin yapıldığı
        // gönderilmeli ve aynı zamanda sunucu, gönderen hariç herkese bu bilgileri yaymalı.
        // Sunucuya sadece hareketler gönderilmeli. Sunucudan gelen hareketler UIHandler tarafından ele alınmalı.
        // UIHandler sınıfı Bir Thread ile çalışmalı veya direkt olarak sunucu değerleri değiştirmeli.
        // Uygulama yeni açıldığında isim isteme olabilir. Kullanıcının ismi sonraki
        //seferlerde hatırlamak adına bir dosyaya kaydedilebilir. 

        public class PlayerUIElement
        {
            public int playerPosition; // which position the current player is.

            public int offsetLeft; // These offset will be set at the start
            public int offsetTop; // before drawing the boards.

            public EightPuzzle puzzle;

            public PlayerUIElement(int position, int left, int top, int[][]gameBoard)
            {
                playerPosition = position;
                offsetLeft = left;
                offsetTop = top;
                puzzle = new EightPuzzle(position);
                puzzle.Start(gameBoard);
            }
        }
    }
}

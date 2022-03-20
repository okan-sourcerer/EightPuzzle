using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EightPuzzle
{
    class EightPuzzle
    {
        private int[][] positions;

        public Position emptyCell;

        public int userPosition;

        public EightPuzzle(int position)
        {
            userPosition = position;
        }

        public static int[][] CreateBoard(int width, int height)
        {
            int[][] board = new int[height][]; // create matrix
            for (int i = 0; i < height; i++)
            {
                board[i] = new int[width];
            }

            int totalElements = width * height - 1; // find total number of elements for the board

            List<Position> listPos = new List<Position>(); // create temporary list for storing points before randomizing.
            int currentPosition, currentX, currentY;
            currentPosition = currentX = currentY = 0;
            while (currentPosition < height * width)
            {
                if (currentX == width)
                {
                    currentX = 0;
                    currentY++;
                }

                listPos.Add(new Position(currentX, currentY));
                currentX++;
                currentPosition++;
            }

            Shuffle(listPos); // shuffle the list elements using this method.

            // assign all the randonmized positions to a value
            for (int i = 0; i < totalElements; i++)
            {
                Position pos = listPos[i];
                board[pos.y][pos.x] = i + 1;
            }

            return board;
        }

        private Position FindEmptyCell()
        {
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = 0; j < positions[0].Length; j++)
                {
                    if (positions[i][j] == 0)
                        return new Position(j, i);
                }
            }
            return new Position(0, 0);
        }

        /// <summary>
        /// Move the empty cell in the board with the given amount. Both won't be same ever.
        /// </summary>
        /// <param name="x">Can be -1, 0, 1</param>
        /// <param name="y">Can be -1, 0, 1</param>
        /// <returns>returns true if user did move.</returns>
        public bool Move(int x, int y)
        {
            int nextX = emptyCell.x + x;
            int nextY = emptyCell.y + y;
            if(nextX < 0 || nextX == positions[0].Length || nextY < 0 || nextY == positions.Length)
            {
                return false;
            }
            SwitchCells(nextX, nextY);
            return true;
        }

        /// <summary>
        /// Move the empty cell in the board with the given action.
        /// Actions are @InputHandler.cs Action class
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool Move(int action)
        {
            bool moved = false;
            switch (action) // actions mapped according to their respective values in Action class
            {
                case 1:
                    moved = Move(0, -1);
                    break;
                case 3:
                    moved = Move(0, 1);
                    break;
                case 4:
                    moved = Move(-1, 0);
                    break;
                case 2:
                    moved = Move(1, 0);
                    break;
            }

            return moved;
        }

        private void SwitchCells(int x, int y)
        {
            int temp = positions[y][x];
            UIHandler.Instance().SwitchElements(userPosition, emptyCell, new Position(x, y), temp);
            positions[emptyCell.y][emptyCell.x] = temp;
            emptyCell.x = x;
            emptyCell.y = y;
        }

        public bool HasWon()
        {
            int current = 1;
            for(int i = 0; i < positions.Length; i++)
            {
                for(int j = 0; j < positions[0].Length; j++)
                {
                    if(i == positions.Length - 1 && j == positions[0].Length - 1)
                    {
                        return true;
                    }
                    int cur = positions[i][j];
                    if (cur != current)
                    {
                        return false;
                    }
                    current++;
                }
            }
            return true;
        }

        private static void Shuffle(List<Position> list) // shuffle list elements
        {
            Random rng = new();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Position value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public int[][] GetBoard()
        {
            return positions;
        }

        public void Start(int boardSizeX, int boardSizeY)
        {
            positions = CreateBoard(boardSizeX, boardSizeY);
            emptyCell = FindEmptyCell();
        }

        public void Start(int[][] board)
        {
            positions = board;
            emptyCell = FindEmptyCell();
        }

        /// <summary>
        /// temporary representation for position on the board. Only used when the board is just initialized.
        /// After initialization, numbers are kept in the matrix. Position is not needed.
        /// </summary>
        public class Position
        {
            public int x { get; set; }
            public int y { get; set; }

            public Position(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}

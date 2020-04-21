using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

#region Move class
class Move
{
    public char movePerformed;
    public Tuple<int, int> constraintShoot;
    public int? constraintSquare;
    public Tuple<int, int> constraintMine;
    public List<int> constraintNegative;
    public Move(char move)
    {
        this.movePerformed = move;
        this.constraintSquare = null;
        this.constraintSquare = null;
        this.constraintMine = null;
        constraintNegative = new List<int>();
    }
}
#endregion

class Player
{

    static bool[,] mapa = new bool[20, 20];
    static bool[,] wasHere = new bool[20, 20];

    static int width, height;
    static int playerx, playery;

    #region Calculate Quadrant of position
    static int PositionToQuadrant(int x, int y)
    {
        return (y / 5) * 3 + (x / 5) + 1;
    }
    #endregion

    #region Check if position is valid
    static bool ValidPosition(int x, int y, bool myMoves = false)
    {
        if (!(x >= 0 && x < width && y >= 0 && y < height && mapa[y, x]))
            return false;
        if (myMoves)
            if (wasHere[y, x])
                return false;
        return true;
    }
    #endregion

    #region DFS VARIABLES AND FUNCTIONS
    static int[] addX = { 1, -1, 0, 0 };
    static int[] addY = { 0, 0, 1, -1 };
    static bool[,] visited = new bool[20, 20];
    static int DFS(int x, int y, bool myMoves = false)
    {
        visited = new bool[20, 20];
        return dfsHelper(x, y, myMoves);
    }
    static int dfsHelper(int x, int y, bool myMoves)
    {
        visited[y, x] = true;
        int r = 1;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + addX[i], ny = y + addY[i];
            if (ValidPosition(nx, ny, myMoves) && !visited[ny, nx])
            {
                r += dfsHelper(nx, ny, myMoves);
            }
        }
        return r;
    }
    #endregion

    #region Prediction variables and functions
    static BigInteger[,,] memEnemy = new BigInteger[20, 20, 750]; //x,y,pos
    static int[,,] memMe = new int[20, 20, 750]; //x,y,pos
    static double precission = 0;
    static int lastSonarCheck = 0;
    static List<Move> opponentMoves = new List<Move>();
    static List<char> myMoves = new List<char>();

    static BigInteger PossiblePositionForEnemy(int x, int y, int pos)
    {
        if (!ValidPosition(x, y))
        {
            return 0;
        }
        if (pos == -1)
        {
            return 1;
        }
        if (memEnemy[x, y, pos] != -1)
        {
            return memEnemy[x, y, pos];
        }

        if (opponentMoves[pos].constraintSquare != null)
        {
            if (opponentMoves[pos].constraintSquare != PositionToQuadrant(x, y))
            {
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = 0;
                return 0;
            }
        }
        if (opponentMoves[pos].constraintShoot != null)
        {
            if (Math.Abs(x - opponentMoves[pos].constraintShoot.Item1) + Math.Abs(y - opponentMoves[pos].constraintShoot.Item2) > 4)
            {
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = 0;
                return 0;
            }
        }
        foreach (var mv in opponentMoves[pos].constraintNegative)
        {
            if (PositionToQuadrant(x, y) == mv)
            {
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = 0;
                return 0;
            }
        }

        switch (opponentMoves[pos].movePerformed)
        {
            case 'N':
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = PossiblePositionForEnemy(x, y + 1, pos - 1);
                return PossiblePositionForEnemy(x, y + 1, pos - 1);

            case 'S':
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = PossiblePositionForEnemy(x, y - 1, pos - 1);
                return PossiblePositionForEnemy(x, y - 1, pos - 1);

            case 'W':
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = PossiblePositionForEnemy(x + 1, y, pos - 1);
                return PossiblePositionForEnemy(x + 1, y, pos - 1);

            case 'E':
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = PossiblePositionForEnemy(x - 1, y, pos - 1);
                return PossiblePositionForEnemy(x - 1, y, pos - 1);

            case 'X':
                BigInteger sum = 0;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j <= 4; j++)
                    {
                        if (!ValidPosition(x + addX[i] * j, y + addY[i] * j))
                        {
                            break;
                        }
                        sum += PossiblePositionForEnemy(x + addX[i] * j, y + addY[i] * j, pos - 1);
                    }
                }
                if (pos < opponentMoves.Count - 3)
                    memEnemy[x, y, pos] = sum;

                return sum;
        }
        return 0;
    }
    static int PossiblePositionsForMe(int x, int y, int pos, bool first = true)
    {
        if (!ValidPosition(x, y))
        {
            return 0;
        }
        if (pos == -1)
        {
            return 1;
        }
        if (memMe[x, y, pos] != -1)
        {
            return memMe[x, y, pos];
        }

        switch (myMoves[pos])
        {
            case 'N':
                if (!first)
                    memMe[x, y, pos] = PossiblePositionsForMe(x, y + 1, pos - 1, false);
                return PossiblePositionsForMe(x, y + 1, pos - 1, false);

            case 'S':
                if (!first)
                    memMe[x, y, pos] = PossiblePositionsForMe(x, y - 1, pos - 1, false);
                return PossiblePositionsForMe(x, y - 1, pos - 1, false);

            case 'W':
                if (!first)
                    memMe[x, y, pos] = PossiblePositionsForMe(x + 1, y, pos - 1, false);
                return PossiblePositionsForMe(x + 1, y, pos - 1, false);

            case 'E':
                if (!first)
                    memMe[x, y, pos] = PossiblePositionsForMe(x - 1, y, pos - 1, false);
                return PossiblePositionsForMe(x - 1, y, pos - 1, false);
        }
        return 0;
    }
    #endregion

    static void Main(string[] args)
    {
        #region Initial inputs
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        width = int.Parse(inputs[0]);
        height = int.Parse(inputs[1]);
        int myId = int.Parse(inputs[2]);

        //Reset memory arrays
        for (int i = 0; i < 19; i++)
            for (int j = 0; j < 19; j++)
                for (int k = 0; k < 199; k++)
                {
                    memEnemy[i, j, k] = -1;
                    memMe[i, j, k] = -1;
                }

        //Get map layout
        for (int i = 0; i < height; i++)
        {
            string line = Console.ReadLine();

            for (int j = 0; j < width; j++)
            {
                mapa[i, j] = (line[j] == '.');
            }
        }
        #endregion

        #region Find Starting position
        //Find starting position
        int xmax = 0, ymax = 0, maxStart = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (mapa[i, j])
                {
                    int counter = 0;
                    bool ok = true;
                    for (int k = 1; ok; k++)
                    {
                        for (int f = i - k; f <= i + k; f++)
                        {
                            for (int d = j - k; d <= j + k; d++)
                            {
                                if (ValidPosition(d, f))
                                {
                                    counter++;
                                }
                                else
                                    ok = false;
                            }
                        }
                    }
                    if (counter > maxStart)
                    {
                        xmax = j;
                        ymax = i;
                        maxStart = counter;
                    }
                }
            }
        }
        #endregion

        playerx = xmax; playery = ymax;
        Console.WriteLine(playerx + " " + playery);
        wasHere[playery, playerx] = true;

        //Game loop
        while (true)
        {
            #region Loop input
            inputs = Console.ReadLine().Split(' ');
            playerx = int.Parse(inputs[0]);
            playery = int.Parse(inputs[1]);
            wasHere[playery, playerx] = true;
            int myLife = int.Parse(inputs[2]);
            int oppLife = int.Parse(inputs[3]);
            int torpedoCooldown = int.Parse(inputs[4]);
            int sonarCooldown = int.Parse(inputs[5]);
            int silenceCooldown = int.Parse(inputs[6]);
            int mineCooldown = int.Parse(inputs[7]);
            string sonarResult = Console.ReadLine();
            string opponentOrders = Console.ReadLine();
            #endregion


            int before = (int)(opponentMoves.Count - 1);
            //Check if first round
            if (opponentOrders != "NA")
            {
                var sortedOrders = opponentOrders.Split('|').ToList();
                sortedOrders.ForEach((ins) =>
                {
                    switch (ins.Split(' ')[0])
                    {
                        case "MOVE":
                            char direction = ins.Split(' ')[1][0];
                            opponentMoves.Add(new Move(direction));
                            break;
                        case "TORPEDO":
                            try
                            {
                                Tuple<int, int> position = new Tuple<int, int>(int.Parse(ins.Split(' ')[1]), int.Parse(ins.Split(' ')[2]));
                                opponentMoves[opponentMoves.Count - 1].constraintShoot = position;
                            }
                            catch { }
                            break;
                        case "SURFACE":
                            try
                            {
                                int quadran = int.Parse(ins.Split(' ')[1]);
                                opponentMoves[before].constraintSquare = quadran;
                            }
                            catch { }
                            break;
                        case "SILENCE":
                            opponentMoves.Add(new Move('X'));
                            break;
                    }
                });
            }

            //If i got a sonar result from last round
            if (sonarResult != "NA")
            {
                if (sonarResult == "Y")
                {
                    opponentMoves[before].constraintSquare = lastSonarCheck;
                }
                else
                {
                    opponentMoves[before].constraintNegative.Add(lastSonarCheck);
                }
            }

            BigInteger totalPossible = 0;
            long closestx = 0, closesty = 0;
            BigInteger closeValue = 0;
            Console.Error.WriteLine("");
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Console.Error.Write((wasHere[i, j] + "")[0]);
                }
            }
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    BigInteger sum = PossiblePositionForEnemy(j, i, opponentMoves.Count - 1);
                    totalPossible += sum;
                    if ((closeValue < sum) ||
                        (closeValue == sum
                        && Math.Abs(playerx - j) + Math.Abs(playery - i) <= Math.Abs(playerx - closestx) + Math.Abs(playery - closesty)))
                    {
                        closeValue = sum;
                        closestx = j;
                        closesty = i;
                    }
                    Console.Error.Write(sum + " ");

                }
                Console.Error.WriteLine("");

            }

            Console.Error.WriteLine(closeValue + " " + totalPossible);
            if (DFS(playerx, playery, true) == 1)
            {
                Console.Write("SURFACE|");
                wasHere = new bool[20, 20];
                wasHere[playery, playerx] = true;
            }
            precission = (double)closeValue / (double)totalPossible;

            long maximOption = -15000000; int maximDir = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = playerx + addX[i], ny = playery + addY[i];
                if (ValidPosition(nx, ny, true) && !wasHere[ny, nx])
                {
                    wasHere[ny, nx] = true;

                    if (i == 0)
                        myMoves.Add('E');
                    if (i == 1)
                        myMoves.Add('W');
                    if (i == 2)
                        myMoves.Add('S');
                    if (i == 3)
                        myMoves.Add('N');

                    long totalPositions = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            totalPositions += PossiblePositionsForMe(x, y, myMoves.Count - 1, true);
                        }
                    }

                    long access = DFS(nx, ny, true);
                    access += (int)(totalPositions * 2);
                    int dist = (int)(Math.Abs(closestx - nx) + Math.Abs(closesty - ny));

                    if ((myLife >= oppLife && precission < 0.15) || torpedoCooldown != 0)
                    {
                        access += (int)(dist * 1.5);
                    }
                    else
                    {
                        if (Math.Max(Math.Abs(closestx - nx), Math.Abs(closesty - ny)) <= 2)
                            access += (int)(dist * 1.5);
                        else
                            access -= (int)(dist * 1.5);
                    }

                    Console.Error.WriteLine(access);

                    if (access > maximOption)
                    {
                        maximOption = access;
                        maximDir = i;
                    }
                    myMoves.Remove(myMoves.Last());
                    wasHere[ny, nx] = false;

                }
            }
            switch (maximDir)
            {

                case 0:
                    Console.Write("MOVE E ");
                    playerx++;
                    if (torpedoCooldown == 0 && precission > 0.15)
                    {
                        Console.Write("SILENCE");
                        silenceCooldown--;
                    }
                    else if (torpedoCooldown != 0)
                    {
                        Console.Write("TORPEDO");
                        torpedoCooldown--;
                    }
                    else
                    {
                        Console.Write("SONAR");
                        sonarCooldown--;
                    }
                    myMoves.Add('E');
                    break;

                case 1:
                    Console.Write("MOVE W ");
                    playerx--;
                    if (torpedoCooldown == 0 && precission > 0.15)
                    {
                        Console.Write("SILENCE");
                        silenceCooldown--;
                    }
                    else if (torpedoCooldown != 0)
                    {
                        Console.Write("TORPEDO");
                        torpedoCooldown--;
                    }
                    else
                    {
                        Console.Write("SONAR");
                        sonarCooldown--;
                    }
                    myMoves.Add('W');

                    break;

                case 2:
                    Console.Write("MOVE S ");
                    playery++;
                    if (torpedoCooldown == 0 && precission > 0.15)
                    {
                        Console.Write("SILENCE");
                        silenceCooldown--;
                    }
                    else if (torpedoCooldown != 0)
                    {
                        Console.Write("TORPEDO");
                        torpedoCooldown--;
                    }
                    else
                    {
                        Console.Write("SONAR");
                        sonarCooldown--;
                    }
                    myMoves.Add('S');
                    break;

                case 3:
                    Console.Write("MOVE N ");
                    playery--;
                    if (torpedoCooldown == 0 && precission > 0.15)
                    {
                        Console.Write("SILENCE");
                        silenceCooldown--;
                    }
                    else if (torpedoCooldown != 0)
                    {
                        Console.Write("TORPEDO");
                        torpedoCooldown--;
                    }
                    else
                    {
                        Console.Write("SONAR");
                        sonarCooldown--;
                    }
                    myMoves.Add('N');

                    break;

            }
            wasHere[playery, playerx] = true;
            if (silenceCooldown == 0)
            {
                bool changed = false;
                for (int i = 0; i < 4; i++)
                {
                    int nx = playerx + addX[i], ny = playery + addY[i];
                    if (ValidPosition(nx, ny, true) && !wasHere[ny, nx])
                    {
                        wasHere[ny, nx] = true;

                        if (i == 0)
                            myMoves.Add('E');
                        if (i == 1)
                            myMoves.Add('W');
                        if (i == 2)
                            myMoves.Add('S');
                        if (i == 3)
                            myMoves.Add('N');

                        long totalPositions = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                totalPositions += PossiblePositionsForMe(x, y, myMoves.Count - 1, true);
                            }
                        }

                        long access = DFS(nx, ny, true);
                        access += (int)(totalPositions * 2);
                        int dist = (int)(Math.Abs(closestx - nx) + Math.Abs(closesty - ny));

                        if ((myLife >= oppLife && precission < 0.15) || torpedoCooldown == 0)
                        {
                            access += (int)(dist * 1.5);
                        }
                        else
                        {
                            if (Math.Max(Math.Abs(closestx - nx), Math.Abs(closesty - ny)) <= 2)
                                access += (int)(dist * 1.5);
                            else
                                access -= (int)(dist * 1.5);
                        }

                        Console.Error.WriteLine(access);

                        if (access > maximOption)
                        {
                            maximOption = access;
                            changed = true;
                            maximDir = i;
                        }
                        myMoves.Remove(myMoves.Last());
                        wasHere[ny, nx] = false;

                    }
                }

                Console.Write("|SILENCE ");
                switch (maximDir)
                {
                    case 0:
                        Console.Write("E");
                        break;
                    case 1:
                        Console.Write("W");
                        break;
                    case 2:
                        Console.Write("S");
                        break;
                    case 3:
                        Console.Write("N");
                        break;
                }
                if (changed)
                {
                    switch (maximDir)
                    {
                        case 0:
                            myMoves.Add('E');
                            playerx++;
                            break;
                        case 1:
                            myMoves.Add('W');
                            playerx--;
                            break;
                        case 2:
                            myMoves.Add('S');
                            playery--;
                            break;
                        case 3:
                            myMoves.Add('N');
                            playery++;
                            break;
                    }
                    wasHere[playery, playerx] = true;
                    bool better = false;
                    int nx = playerx + addX[maximDir], ny = playery + addY[maximDir];
                    if (ValidPosition(nx, ny, true) && !wasHere[ny, nx])
                    {
                        wasHere[ny, nx] = true;

                        if (maximDir == 0)
                            myMoves.Add('E');
                        if (maximDir == 1)
                            myMoves.Add('W');
                        if (maximDir == 2)
                            myMoves.Add('S');
                        if (maximDir == 3)
                            myMoves.Add('N');

                        long totalPositions = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                totalPositions += PossiblePositionsForMe(x, y, myMoves.Count - 1, true);
                            }
                        }

                        long access = DFS(nx, ny, true);
                        access += (int)(totalPositions * 2);
                        int dist = (int)(Math.Abs(closestx - nx) + Math.Abs(closesty - ny));

                        if ((myLife >= oppLife && precission < 0.15) || torpedoCooldown != 0)
                        {
                            access += (int)(dist * 1.5);
                        }
                        else
                        {
                            if (Math.Max(Math.Abs(closestx - nx), Math.Abs(closesty - ny)) <= 2)
                                access += (int)(dist * 1.5);
                            else
                                access -= (int)(dist * 1.5);
                        }

                        Console.Error.WriteLine(access);

                        if (access > maximOption)
                        {
                            better = true;
                        }
                        myMoves.Remove(myMoves.Last());
                        wasHere[ny, nx] = false;

                    }
                    if (better) Console.Write(" 2");
                    else
                        Console.Write(" 1");
                }
                else
                    Console.Write(" 0");
                wasHere[playery, playerx] = true;
            }
            if (Math.Abs(playerx - closestx) + Math.Abs(playery - closesty) <= 4 && torpedoCooldown == 0 && precission > 0.15 && !(myLife <= oppLife && Math.Max(Math.Abs(playerx - closestx), Math.Abs(playery - closesty)) <= 1))
            {
                Console.Write($"|TORPEDO {closestx} {closesty}|");
            }

            if (sonarCooldown == 0)
            {
                Console.Write("|SONAR " + PositionToQuadrant((int)closestx, (int)closesty) + "|");
                lastSonarCheck = PositionToQuadrant((int)closestx, (int)closesty);
            }


            Console.WriteLine("|||MSG " + precission);
            Console.Error.WriteLine(closestx + " " + closesty);
        }
    }
}
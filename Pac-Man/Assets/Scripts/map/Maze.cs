using UnityEngine;

public static class Maze
{
    public const char Wall = '#';
    public const char Pellet = '.';
    public const char PowerPellet = 'o';
    public const char Empty = ' ';
    public const char Door = '-';

    public const int TunnelRow = 16; // grid y координата туннел€

    public static readonly string[] Layout = new string[]
    {
        "############################",
        "#............##............#",
        "#.####.#####.##.#####.####.#",
        "#o####.#####.##.#####.####o#",
        "#.####.#####.##.#####.####.#",
        "#..........................#",
        "#.####.##.########.##.####.#",
        "#.####.##.########.##.####.#",
        "#......##....##....##......#",
        "######.##### ## #####.######",
        "     #.##### ## #####.#     ",
        "     #.##          ##.#     ",
        "     #.## ###--### ##.#     ",
        "######.## #      # ##.######",
        "      .   #      #   .      ",
        "######.## #      # ##.######",
        "     #.## ######## ##.#     ",
        "     #.##          ##.#     ",
        "     #.## ######## ##.#     ",
        "######.## ######## ##.######",
        "#............##............#",
        "#.####.#####.##.#####.####.#",
        "#.####.#####.##.#####.####.#",
        "#o..##.......  .......##..o#",
        "###.##.##.########.##.##.###",
        "###.##.##.########.##.##.###",
        "#......##....##....##......#",
        "#.##########.##.##########.#",
        "#.##########.##.##########.#",
        "#..........................#",
        "############################"
    };

    public static int Width => Layout[0].Length;
    public static int Height => Layout.Length;

    // x = колонка, y = строка снизу (0 = нижн€€ строка)
    public static char GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return Wall;
        int row = Height - 1 - y;
        return Layout[row][x];
    }

    public static bool IsWalkable(int x, int y, bool ghost = false)
    {
        // “уннель: выход за границы по горизонтали на TunnelRow Ч разрешЄн
        if (y == TunnelRow && (x < 0 || x >= Width)) return true;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        char c = GetTile(x, y);
        if (c == Wall) return false;
        if (c == Door && !ghost) return false;
        return true;
    }
}
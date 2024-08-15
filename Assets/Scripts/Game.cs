using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Game : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public int mine_count = 32;

    private void OnValidate()
    {
        mine_count = Mathf.Clamp(mine_count, 0, width * height);
    }

    private Board board;
    private Cell[,] state;

    private bool game_over;

    private void Awake()
    {
        board = GetComponentInChildren<Board>();
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        game_over = false;
        state = new Cell[width, height];
        
        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2, height / 2, -10f);
        board.Draw(state);
    }

    private void GenerateCells()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Cell cell = new Cell();
                cell.position = new Vector3Int(i, j, 0);
                cell.type = Cell.Type.Empty;
                state[i, j] = cell;
            }
        }
    }

    private void GenerateMines()
    {
        for (int i = 0; i < mine_count; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (state[x,y].type == Cell.Type.Mine)
            {
                x++;
                if (x >= width)
                {
                    x = 0;
                    y++;

                    if (y >= height) y = 0;
                }
            }

            state[x, y].type = Cell.Type.Mine;
            //state[x, y].revealed = true;
        }
    }

    private void GenerateNumbers()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Cell cell = state[i, j];

                if (cell.type == Cell.Type.Mine) continue;

                cell.number = CountMines(i, j);

                if (cell.number > 0) cell.type = Cell.Type.Number;

                //cell.revealed = true;
                state[i, j] = cell;
            }
        }
    }

    private int CountMines(int cell_x, int cell_y)
    {
        int counter = 0;
        int[] Xs = { cell_x - 1, cell_x, cell_x + 1 };
        int[] Ys = { cell_y - 1, cell_y, cell_y + 1 };

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (GetCell(Xs[i], Ys[j]).type == Cell.Type.Mine) counter++;
            }
        }
        //int counter = 0;
        //int[] Xs = { cell_x - 1, cell_x, cell_x + 1 };
        //int[] Ys = { cell_y - 1, cell_y, cell_y + 1 };

        //for (int i = 0; i < 3; i++)
        //{
        //    for (int j = 0; j < 3; j++)
        //    {
        //        if (Xs[i] < 0 || Ys[j] < 0) continue;
        //        if (Xs[i] >= width || Ys[j] >= height) continue;

        //        if (state[Xs[i], Ys[j]].type == Cell.Type.Mine) counter++;
        //    }
        //}

        // alternative algorithm //
        //    for (int x_ = -1; x_ <= 1; x_++)
        //    {
        //        for (int y_ = -1; y_ <= 1; y_++)
        //        {
        //            if (x_ == 0 && y_ == 0) continue;

        //            int x = cell_x + x_;
        //            int y = cell_y + y_;

        //            if (x < 0 || y < 0 || x >= width || y >= height) continue;

        //            if (state[x,y].type == Cell.Type.Mine) counter++;
        //        }
        //    }
        return counter;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) NewGame();

        else if (!game_over)
        {
            if (Input.GetMouseButtonDown(1)) Flag();
            else if (Input.GetMouseButtonDown(0)) Reveal();
        }
    }

    private void Flag()
    {
        Vector3 world_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell_pos = board.tilemap.WorldToCell(world_pos);
        Cell cell = GetCell(cell_pos.x, cell_pos.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed) return;

        cell.flagged = !cell.flagged;

        state[cell_pos.x, cell_pos.y] = cell;

        board.Draw(state);
    }

    private void Reveal()
    {
        Vector3 world_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell_pos = board.tilemap.WorldToCell(world_pos);
        Cell cell = GetCell(cell_pos.x, cell_pos.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Empty: Spread(cell); CheckWinCondition(); break;
            case Cell.Type.Mine: Explode(cell); break;
            default:
                cell.revealed = true;
                state[cell_pos.x, cell_pos.y] = cell;
                CheckWinCondition();
                break;
        }
        
        board.Draw(state);

    }

    private void Spread(Cell cell)
    {
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) return;

        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (cell.type == Cell.Type.Empty)
        {
            Spread(GetCell(cell.position.x - 1, cell.position.y));
            Spread(GetCell(cell.position.x + 1, cell.position.y));
            Spread(GetCell(cell.position.x, cell.position.y - 1));
            Spread(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell)
    {
        game_over = true;

        cell.revealed = true;
        cell.boom = true;

        state[cell.position.x, cell.position.y] = cell;

        for (int i = 0; i<width; i++) 
        {
            for (int j = 0; j < height; j++)
            {
                cell = state[i, j];
                
                if (cell.type == Cell.Type.Mine)
                {
                    cell.revealed = true;
                    state[i, j] = cell;
                }
            }
        }
        Debug.Log("lose");
    }

    private void CheckWinCondition()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Cell cell = state[i, j];

                if (!cell.revealed && cell.type == Cell.Type.Mine)
                {
                    return;
                }
            }
        }

        game_over = true;
        Debug.Log("win");

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Cell cell = state[i, j];

                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                    state[i, j] = cell;
                }
            }
        }
    }

    private Cell GetCell(int x, int y)
    {
        if (IsValid(x, y)) return state[x, y];
        else return new Cell();
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
}
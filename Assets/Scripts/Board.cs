using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;



public enum TileType { Red, Blue, Green, Yellow, Purple, Orange }

[System.Serializable]
public struct ColorGoal
{
    public TileType type;   // which color
    public int count;       // how many to clear
}


public class Board : MonoBehaviour
{
    bool IsAlive(Tile t) => t != null && t.gameObject != null;
    // Put in Board
    static bool Alive(Tile t) => t != null && t.gameObject != null;   // MissingReference-safe


    [Header("Board Settings")]
    public int width = 8;
    public int height = 8;
    public float spacing = 1.1f;
    public int minMatch = 3;

    [Header("UI & Game")]
  
    public TMP_Text scoreText;
    public TMP_Text movesText;
    public TMP_Text goalText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Level Goal")]

    [Header("Level Goal")]
    public List<ColorGoal> goals = new List<ColorGoal>() {
    new ColorGoal { type = TileType.Blue,  count = 10 },
    new ColorGoal { type = TileType.Red,   count = 10 },
    // I can add more color in future improvement
};
    public int moves = 20;


    private Tile[,] grid;
    private Sprite[] gemSprites;
    private AudioSource audioSource;
    private AudioClip swapClip, matchClip;
    private int score = 0;
    private bool inputLocked = false;

    // selection support (if Tile calls board.Select(this))
    private Tile firstSelected;

    float offX, offY; // board centering offsets
    void ComputeOffsets()
    {
        offX = -(width - 1) * spacing * 0.5f;
        offY = -(height - 1) * spacing * 0.5f;
    }

    Vector3 CellPos(int x, int y)
    {
        return new Vector3(x * spacing + offX, y * spacing + offY, 0f);
    }

    void Awake()
    {
        gemSprites = new Sprite[6];
        gemSprites[0] = Resources.Load<Sprite>("Gems/gem_red");
        gemSprites[1] = Resources.Load<Sprite>("Gems/gem_blue");
        gemSprites[2] = Resources.Load<Sprite>("Gems/gem_green");
        gemSprites[3] = Resources.Load<Sprite>("Gems/gem_yellow");
        gemSprites[4] = Resources.Load<Sprite>("Gems/gem_purple");
        gemSprites[5] = Resources.Load<Sprite>("Gems/gem_orange");

        swapClip = Resources.Load<AudioClip>("Audio/swap");
        matchClip = Resources.Load<AudioClip>("Audio/match");

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        SetupBoard();
        UpdateUI();
    }

    //void SetupBoard()
    //{
    //    grid = new Tile[width, height];

    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            CreateTile(x, y, GetRandomTypeAvoidingStartMatch(x, y));
    //        }
    //    }
    //    CenterBoard();
    //}

    void SetupBoard()
    {
        grid = new Tile[width, height];

        ComputeOffsets(); 

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                CreateTile(x, y, GetRandomTypeAvoidingStartMatch(x, y));

        CenterBoard();
    }

    //void CenterBoard()
    //{
    //    float offX = -(width - 1) * spacing * 0.5f;
    //    float offY = -(height - 1) * spacing * 0.5f;

    //    for (int x = 0; x < width; x++)
    //        for (int y = 0; y < height; y++)
    //            if (grid[x, y] != null)
    //                grid[x, y].transform.position = new Vector3(x * spacing + offX, y * spacing + offY, 0);
    //}
    void CenterBoard()
    {
        // If I change width/height/spacing at runtime, recompute and replace:
        ComputeOffsets();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] != null)
                    grid[x, y].transform.position = CellPos(x, y);
    }

    TileType GetRandomTypeAvoidingStartMatch(int x, int y)
    {
        for (int guard = 0; guard < 100; guard++)
        {
            TileType t = (TileType)Random.Range(0, gemSprites.Length);

            if (x >= 2 &&
                grid[x - 1, y] != null && grid[x - 2, y] != null &&
                grid[x - 1, y].type == t && grid[x - 2, y].type == t)
                continue;

            if (y >= 2 &&
                grid[x, y - 1] != null && grid[x, y - 2] != null &&
                grid[x, y - 1].type == t && grid[x, y - 2].type == t)
                continue;

            return t;
        }
        return TileType.Blue;
    }

 
    void CreateTile(int x, int y, TileType type)
    {
        GameObject go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(transform); // parent = Board (fine)

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = gemSprites[(int)type];
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.48f;

        var tile = go.AddComponent<Tile>();
        tile.Init(x, y, type, this);

        // proper starting position at its own column/row 
        go.transform.position = CellPos(x, y);

        grid[x, y] = tile;
    }

    public void Select(Tile t)
    {
        if (inputLocked || !Alive(t)) return;

        if (!Alive(firstSelected)) firstSelected = null;
        if (firstSelected == null) { firstSelected = t; return; }
        if (t == firstSelected) { firstSelected = null; return; }

        TrySwap(firstSelected, t);
        firstSelected = null;
    }

    public void TrySwap(Tile a, Tile b)
    {
        if (inputLocked || !Alive(a) || !Alive(b)) return;
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return;
        StartCoroutine(SwapAndResolve(a, b));
    }



    IEnumerator SwapAndResolve(Tile a, Tile b)
    {
        inputLocked = true;

        // If either vanished (popped during another cascade)
        if (!IsAlive(a) || !IsAlive(b))
        {
            inputLocked = false;
            yield break;
        }

        // Adjacent check (ignore diagonals), in case TrySwap was called directly.
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
        {
            inputLocked = false;
            yield break;
        }

        if (swapClip) audioSource.PlayOneShot(swapClip);

        // the grid swap and animate it.
        SwapGrid(a, b);
        yield return StartCoroutine(AnimateSwap(a, b));

        // They might have been destroyed during the animation.
        if (!IsAlive(a) || !IsAlive(b))
        {
            inputLocked = false;
            yield break;
        }

        // Check for matches after the swap.
        var matched = FindMatches();
        if (matched.Count == 0)
        {
            // No match: give a little feedback and revert the swap if both still exist.(Did not work yet, need to focus)
            if (IsAlive(a)) yield return StartCoroutine(Shake(a.transform));
            if (IsAlive(b)) yield return StartCoroutine(Shake(b.transform));

            if (IsAlive(a) && IsAlive(b))
            {
                SwapGrid(a, b);
                yield return StartCoroutine(AnimateSwap(a, b));
            }

            inputLocked = false;
            yield break;
        }

        // Valid swap ,spend a move and update UI.
        moves--;
        UpdateUI();

        // Resolve cascades. to avoid infinite loops if something goes wrong.
        int safety = 100;
        while (matched.Count > 0 && safety-- > 0)
        {
            if (matchClip) audioSource.PlayOneShot(matchClip);

            // Clear, collapse, refill each step guarded internally.
            yield return StartCoroutine(ClearMatches(matched));
            yield return StartCoroutine(CollapseColumns());
            yield return StartCoroutine(RefillBoard());

            matched = FindMatches();
        }

        // Win/Lose checks
        //bool allCleared = (goals != null && goals.TrueForAll(g => g.count <= 0));
        //if (allCleared)
        //{
        //    if (winPanel) winPanel.SetActive(true);
        //    inputLocked = false;
        //    yield break;
        //}

        bool allCleared = (goals != null && goals.TrueForAll(g => g.count <= 0));
        if (allCleared)
        {
            if (winPanel) winPanel.SetActive(true);
            if (autoAdvanceOnWin)
            {
                inputLocked = true;                         // lock input while we wait
                Invoke(nameof(LoadNextLevel), nextLevelDelay);
            }
            yield break;
        }

        if (moves <= 0)
        {
            if (losePanel) losePanel.SetActive(true);
            inputLocked = false;
            yield break;
        }

        inputLocked = false;
    }



    void SwapGrid(Tile a, Tile b)
    {
        var ax = a.x; var ay = a.y;
        var bx = b.x; var by = b.y;

        grid[ax, ay] = b; b.SetPos(ax, ay);
        grid[bx, by] = a; a.SetPos(bx, by);
    }

    //IEnumerator AnimateSwap(Tile a, Tile b)
    //{
    //    Vector3 aPos = a.transform.position;
    //    Vector3 bPos = b.transform.position;
    //    float t = 0f;
    //    while (t < 1f)
    //    {
    //        t += Time.deltaTime * 8f;
    //        a.transform.position = Vector3.Lerp(aPos, bPos, t);
    //        b.transform.position = Vector3.Lerp(bPos, aPos, t);
    //        yield return null;
    //    }
    //    a.transform.position = bPos;
    //    b.transform.position = aPos;
    //}

    IEnumerator AnimateSwap(Tile a, Tile b)
    {
        if (!Alive(a) || !Alive(b)) yield break;

        Vector3 aPos = a.transform.position;
        Vector3 bPos = b.transform.position;
        float t = 0f;
        while (t < 1f)
        {
            if (!Alive(a) || !Alive(b)) yield break;   // got destroyed while animating
            t += Time.deltaTime * 8f;
            a.transform.position = Vector3.Lerp(aPos, bPos, t);
            b.transform.position = Vector3.Lerp(bPos, aPos, t);
            yield return null;
        }
        if (Alive(a)) a.transform.position = bPos;
        if (Alive(b)) b.transform.position = aPos;
    }

    IEnumerator Shake(Transform tr, float dur = 0.1f, float mag = 0.05f)
    {
        Vector3 start = tr.position;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tr.position = start + (Vector3)Random.insideUnitCircle * mag;
            yield return null;
        }
        tr.position = start;
    }

    HashSet<Tile> FindMatches()
    {
        var result = new HashSet<Tile>();

        // Horizontal
        for (int y = 0; y < height; y++)
        {
            int run = 1;
            for (int x = 1; x < width; x++)
            {
                if (grid[x, y] != null && grid[x - 1, y] != null &&
                    grid[x, y].type == grid[x - 1, y].type) run++;
                else
                {
                    if (run >= minMatch)
                        for (int k = 0; k < run; k++) result.Add(grid[x - 1 - k, y]);
                    run = 1;
                }
            }
            if (run >= minMatch)
                for (int k = 0; k < run; k++) result.Add(grid[width - 1 - k, y]);
        }

        // Vertical
        for (int x = 0; x < width; x++)
        {
            int run = 1;
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] != null && grid[x, y - 1] != null &&
                    grid[x, y].type == grid[x, y - 1].type) run++;
                else
                {
                    if (run >= minMatch)
                        for (int k = 0; k < run; k++) result.Add(grid[x, y - 1 - k]);
                    run = 1;
                }
            }
            if (run >= minMatch)
                for (int k = 0; k < run; k++) result.Add(grid[x, height - 1 - k]);
        }

        return result;
    }

    IEnumerator ClearMatches(HashSet<Tile> matched)
    {
        foreach (var t in matched)
        {
            if (!IsAlive(t)) continue;

            // cache everything you’ll need BEFORE destroy
            int cx = t.x;
            int cy = t.y;
            TileType ctype = t.type;

            // clear selection if the selected tile is being removed
            if (firstSelected == t) firstSelected = null;

            //for single color
            //score += 10;
            //if (ctype == targetType) targetCount--;

            score += 10;
            // decrement the matching goal (clamped at 0 so It won't show negatives)
            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].type == ctype)
                {
                    goals[i] = new ColorGoal { type = goals[i].type, count = Mathf.Max(0, goals[i].count - 1) };
                    break; // one color per tile
                }
            }


            // start the pop animation; do NOT read from 't' after this line
            StartCoroutine(PopAndDestroy(t));

            // update board data using cached coords
            grid[cx, cy] = null;
        }

        UpdateUI();
        yield return new WaitForSeconds(0.15f);
    }


    IEnumerator PopAndDestroy(Tile t)
    {
        Transform tr = t.transform;
        Vector3 s0 = tr.localScale;
        float t0 = 0f;
        while (t0 < 1f)
        {
            t0 += Time.deltaTime * 12f;
            tr.localScale = Vector3.Lerp(s0, s0 * 1.3f, t0);
            tr.Rotate(0, 0, 10f);
            yield return null;
        }
        Destroy(t.gameObject);
    }

    IEnumerator CollapseColumns()
    {
        for (int x = 0; x < width; x++)
        {
            int empty = 0;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null) empty++;
                else if (empty > 0)
                {
                    Tile t = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y - empty] = t;
                    t.SetPos(x, y - empty);
                    StartCoroutine(AnimateFall(t, empty));
                }
            }
        }
        yield return new WaitForSeconds(0.15f);
    }

    //IEnumerator AnimateFall(Tile t, int spaces)
    //{
    //    //  ignore 'spaces' for the target; always fall TO the exact cell
    //    Vector3 target = CellPos(t.x, t.y);

    //    while ((t.transform.position - target).sqrMagnitude > 0.0001f)
    //    {
    //        t.transform.position = Vector3.MoveTowards(
    //            t.transform.position, target, Time.deltaTime * 8f);
    //        yield return null;
    //    }
    //    t.transform.position = target;
    //}
    IEnumerator AnimateFall(Tile t, int spaces)
    {
        if (endingLevel || !Alive(t)) yield break;

        Vector3 target = CellPos(t.x, t.y);

        while (!endingLevel && Alive(t) &&
               (t.transform.position - target).sqrMagnitude > 0.0001f)
        {
            // bail out immediately if the tile was destroyed mid-loop
            if (!Alive(t)) yield break;

            t.transform.position = Vector3.MoveTowards(
                t.transform.position, target, Time.deltaTime * 8f);

            yield return null;
        }

        if (Alive(t))
            t.transform.position = target;
    }

    IEnumerator RefillBoard()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == null)
                {
                    CreateTile(x, y, (TileType)Random.Range(0, gemSprites.Length));

                    // start 'spaces' rows above the cell, but with the CORRECT column X
                    int spaces = 2;
                    var t = grid[x, y];
                    t.transform.position = CellPos(x, y) + new Vector3(0f, spacing * spaces, 0f);

                    StartCoroutine(AnimateFall(t, spaces)); //  pass spaces
                }
        yield return new WaitForSeconds(0.2f);
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (movesText) movesText.text = $"Moves: {moves}";

        if (goalText)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Goals:");
            foreach (var g in goals)
                sb.AppendLine($"Clear {g.type}: {Mathf.Max(0, g.count)}");
            goalText.text = sb.ToString();
        }
    }



    void Update()
    {
        if (inputLocked) return;
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 p = new Vector2(world.x, world.y);

            // find the topmost collider under the cursor
            var hit = Physics2D.OverlapPoint(p);
            if (hit)
            {
                var t = hit.GetComponent<Tile>();
                if (t != null) Select(t); // same Select() i already have
            }
        }
    }
    public Tile GetNeighbor(Tile t, Vector2 dir)
    {
        int nx = t.x + (int)Mathf.Sign(dir.x);
        int ny = t.y + (int)Mathf.Sign(dir.y);
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) ny = t.y; else nx = t.x;
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return null;
        return grid[nx, ny];
    }

    // for level 2, need to work farther
    [Header("Scene Flow")]
    public bool autoAdvanceOnWin = true;
    public float nextLevelDelay = 1.5f;   // seconds
    private bool endingLevel = false;  // guards coroutines when finishing the level

    public void LoadNextLevel()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);          //  Level2
        else
            SceneManager.LoadScene("MainMenu");    // fallback if no next scene
    }
    private IEnumerator LoadNextAfterDelay(float d)
    {
        yield return new WaitForSeconds(d);
        LoadNextLevel();
    }

    // (Optional hooks for UI buttons on Win/Lose panels) will worl later may be after 10/16 submission
    public void OnWinNextButton() => LoadNextLevel();
    public void OnMainMenuButton() => SceneManager.LoadScene("MainMenu");
    public void OnRestartButton() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);


    //for prevent the tile broken


}

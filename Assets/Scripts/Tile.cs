using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; 
#endif

public class Tile : MonoBehaviour
{
    // Grid coordinates (owned by the Board)
    public int x, y;
    public TileType type;

    private Board board;
    private Vector2 pressScreenPos;
    private bool pointerDown;

    // Tune: how many screen pixels count as a swipe vs. a tap
    private const float MinSwipePixels = 18f;

    // This is Optional: keep a collider for reliable clicks
    void Awake()
    {
        // Ensure to have a 2D collider for OnMouse events / Physics2D.OverlapPoint
        if (!TryGetComponent<Collider2D>(out _))
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            ((CircleCollider2D)col).radius = 0.48f;
        }
    }

    public void Init(int x, int y, TileType type, Board board)
    {
        this.x = x;
        this.y = y;
        this.type = type;
        this.board = board;
    }

    public void SetPos(int nx, int ny)
    {
        x = nx;
        y = ny;
    }

    // Pointer Helpers (works with either Input System) 
    private static Vector2 GetPointerScreenPos()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
        return (Vector2)Input.mousePosition;
    }

    //  Pointer Flow 
    void OnMouseDown()
    {
        if (board == null) return;
        pressScreenPos = GetPointerScreenPos();
        pointerDown = true;

        // Optional: let board know that I touched this tile (simple select highlight)
        board.Select(this);
    }

    void OnMouseUp()
    {
        if (board == null || !pointerDown) return;
        pointerDown = false;

        Vector2 release = GetPointerScreenPos();
        Vector2 delta = release - pressScreenPos;

        // Tap? Do nothing more (Select already ran on down)
        if (delta.sqrMagnitude < MinSwipePixels * MinSwipePixels) return;

        // Choose the dominant axis and direction
        Vector2 dir;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dir = (delta.x > 0f) ? Vector2.right : Vector2.left;
        else
            dir = (delta.y > 0f) ? Vector2.up : Vector2.down;

        // Ask the board for the neighbor in that direction and try the swap
        Tile neighbor = board.GetNeighbor(this, dir);
        if (neighbor != null)
            board.TrySwap(this, neighbor);
    }
}

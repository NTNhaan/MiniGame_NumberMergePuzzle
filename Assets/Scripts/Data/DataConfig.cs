using UnityEngine;

public static class DataConfig
{
    // GRID -------------------------------------------------
    public const int GRID_COLUMNS = 5;
    public const int GRID_ROWS = 6;
    public static readonly Vector2 GRID_CELL_SIZE = new(120, 120);
    public static readonly Vector2 GRID_CELL_SPACING = new(10, 10);
    public const bool GRID_USE_LAYOUT_GROUP = true;

    // QUEUE ------------------------------------------------
    public const int QUEUE_SIZE = 3;
    public const int BASE_VALUE = 2;
    public static readonly int[] ALLOWED_VALUES = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 2096, 8192 };
    public static readonly Color DEFAULT_TILE_COLOR = new Color(1f, 0.2f, 0.6f);
    public const float QUEUE_ACTIVE_SCALE = 1f;
    public const float QUEUE_INACTIVE_SCALE = 1f;
    public const float QUEUE_SCALE_LERP_SPEED = 12f;
    public const float QUEUE_ACTIVE_Y_OFFSET = 10f;
    public const float QUEUE_NEW_ITEM_POP_DURATION = 0.2f;

    // ECONOMY ---------------------------------------------
    public const int COIN_BOOSTER = 150; // cost per booster use

    // TILE SPAWN ANIMATION ---------------------------------
    public const float TILE_MOVE_DURATION = 0.35f;
    public const bool TILE_CONSTANT_SPEED = true;
    public const float TILE_PIXELS_PER_SECOND = 1600f;
    public const float TILE_MIN_DURATION = 0.15f;
    public const float TILE_MAX_DURATION = 0.6f;
    public const bool TILE_VERTICAL_FROM_BELOW = true;
    public const bool TILE_START_AT_COLUMN_BOTTOM = true;
    public const bool TILE_TWO_PHASE_FROM_QUEUE = true;
    public const bool TILE_FORCE_PURE_VERTICAL = false;
    public const bool TILE_L_SHAPE_TWO_PHASE = true;
    public const bool TILE_PRESERVE_QUEUE_START = true;
    public const bool TILE_USE_ROOT_CANVAS_FOR_QUEUE_START = true;

    // DEBUG DEFAULTS ---------------------------------------
    public const bool DEBUG_ENABLE_TILE_SYSTEM = true;
    public const bool DEBUG_ENABLE_SPAWN_QUEUE = true;
    public const bool DEBUG_ENABLE_GRID = true;
}

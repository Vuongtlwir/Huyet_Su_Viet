using UnityEngine;

public static class LayerConfig
{
    public static readonly int Player = LayerMask.NameToLayer("Player");
    public static readonly int Enemy  = LayerMask.NameToLayer("Enemy");
    public static readonly int Wall   = LayerMask.NameToLayer("Wall");
    public static readonly int Ground = LayerMask.NameToLayer("Ground");

    public static readonly int WallMask   = 1 << Wall;
    public static readonly int GroundMask = 1 << Ground;
}
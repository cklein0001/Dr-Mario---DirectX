using System;

namespace Dr_Mario.Object_Classes
{
    public enum vColors
    {
        R = 1, B = 2, Y = 4, NULL = 0
    }

    public enum JoinDirection
    {
        LEFT=0, RIGHT=1,UP=2, DOWN=3 
    }

    [Flags]
    public enum Movement
    {
        None = 0,
        Left = 1,
        Right = 2, 
        Down = 4, 
        Clockwise = 8,
        CounterClockwise = 16,
        NoRotate = 32,
        NoMovement = 64
    }

    public enum GameOverReason { VirusClear, EntranceCollision }

    public enum Player { One, Two, Three, Four, System, Computer }
}
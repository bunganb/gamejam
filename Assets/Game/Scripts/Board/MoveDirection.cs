using UnityEngine;

namespace GameJam.Gameplay
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class MoveDirectionExtensions
    {
        public static Vector2Int ToVector(this MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.Up => Vector2Int.up,
                MoveDirection.Down => Vector2Int.down,
                MoveDirection.Left => Vector2Int.left,
                _ => Vector2Int.right
            };
        }
    }
}

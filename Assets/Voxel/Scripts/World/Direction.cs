namespace VoxelWorld
{
    /// <summary>
    /// チャンクの隣接方向を示す列挙型
    /// </summary>
    public enum Direction
    {
        /// <summary>前方 (+Z)</summary>
        Forward,

        /// <summary>後方 (-Z)</summary>
        Back,

        /// <summary>上方 (+Y)</summary>
        Up,

        /// <summary>下方 (-Y)</summary>
        Down,

        /// <summary>右方 (+X)</summary>
        Right,

        /// <summary>左方 (-X)</summary>
        Left
    }
}

namespace StructureGeneration
{
    /// <summary>
    /// 構造物生成に関する定数定義
    /// </summary>
    public static class StructureConstants
    {
        // 接続点設定
        public const float CONNECTION_FAN_ANGLE_DEGREES = 120f;
        public const float CONNECTION_RADIUS_RATIO = 0.15f;
        public const float DEFAULT_CONNECTION_INSET = 3f;

        // ノイズ設定
        public const float DEFAULT_NOISE_SCALE = 0.15f;
        public const float INNER_NOISE_SCALE_RATIO = 0.5f;

        // 生成設定
        public const int DEFAULT_FRAME_YIELD_INTERVAL = 1;
    }
}

namespace StructureGeneration
{
    /// <summary>
    /// マップ生成に関する定数定義
    /// </summary>
    public static class MapGeneratorConstants
    {
        // フレーム分散設定
        public const int STRUCTURE_YIELD_INTERVAL = 1;      // 構造物生成時のフレーム分散間隔
        public const int VOXEL_YIELD_INTERVAL = 10000;      // ボクセル生成時のフレーム分散間隔

        // 進捗値の定数（0.0～1.0）
        public const float PROGRESS_BOUNDARY_WALLS = 0.05f;         //境界壁生成
        public const float PROGRESS_FILL_VOXELS = 0.10f;            //地下埋めボクセル生成
        public const float PROGRESS_SURFACE_VOXELS = 0.15f;         //地表ボクセル生成
        public const float PROGRESS_STRUCTURE_POSITIONS = 0.20f;    //構造物配置位置決定
        public const float PROGRESS_STRUCTURES = 0.25f;             //構造物生成
        public const float PROGRESS_STRUCTURE_VOXELS = 0.30f;       //構造物ボクセルデータ生成
        public const float PROGRESS_CONNECTIONS = 0.35f;            //接続生成
        public const float PROGRESS_CONNECTION_VOXELS = 0.40f;      //接続ボクセルデータ生成
        public const float PROGRESS_VOXEL_PLACEMENT_START = 0.5f;   //ボクセル配置開始
        public const float PROGRESS_VOXEL_PLACEMENT_END = 1.0f;     //ボクセル配置完了
        public const float PROGRESS_COMPLETE = 1.0f;                //完了
    }
}

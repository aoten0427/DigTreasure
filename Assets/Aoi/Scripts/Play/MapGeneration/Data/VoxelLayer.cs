using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// 深さに応じたボクセルレイヤー設定
    /// </summary>
    [System.Serializable]
    public struct VoxelLayer
    {
        [Tooltip("このレイヤーの最小高度（ワールド座標Y）")]
        public float minHeight;

        [Tooltip("このレイヤーの最大高度（ワールド座標Y）")]
        public float maxHeight;

        [Tooltip("使用するボクセルID")]
        public int voxelId;

        [Tooltip("レイヤー名（Inspector表示用）")]
        public string layerName;

        public VoxelLayer(float minHeight, float maxHeight, int voxelId, string layerName)
        {
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            this.voxelId = voxelId;
            this.layerName = layerName;
        }
    }
}

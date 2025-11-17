using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 接続（トンネル）の生成設定
    /// </summary>
    [CreateAssetMenu(fileName = "ConnectionSettings", menuName = "StructureGeneration/ConnectionSettings")]
    public class ConnectionSettings : ScriptableObject
    {
        [Header("空洞トンネル設定")]
        [Tooltip("空洞トンネルの半径（メートル）")]
        public float openTunnelRadius = 2f;

        [Tooltip("空洞トンネルに使用するボクセルID（空気）")]
        public byte openTunnelVoxelId = 0;

        [Header("埋まったトンネル設定")]
        [Tooltip("埋まったトンネルの外側半径（メートル）")]
        public float filledTunnelOuterRadius = 3f;

        [Tooltip("埋まったトンネルの内側半径（メートル）")]
        public float filledTunnelInnerRadius = 2f;

        [Tooltip("外側に使用するボクセルID（硬い岩）")]
        public byte filledTunnelOuterVoxelId = 3;

        [Tooltip("内側に使用するボクセルID（柔らかい岩）")]
        public byte filledTunnelInnerVoxelId = 2;

        [Header("経路生成")]
        [Tooltip("経路生成モード")]
        public PathGenerationMode pathMode = PathGenerationMode.Bezier;

        [Tooltip("ベジェ曲線の制御点オフセット（0-1）")]
        [Range(0f, 1f)]
        public float bezierControlOffset = 0.3f;

        [Header("生成の詳細")]
        [Tooltip("経路上のサンプリング間隔（メートル）")]
        public float samplingInterval = 0.5f;
    }
}

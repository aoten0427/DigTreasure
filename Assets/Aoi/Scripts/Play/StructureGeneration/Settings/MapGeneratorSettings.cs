using System.Collections.Generic;
using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// マップ生成全体の設定
    /// </summary>
    [CreateAssetMenu(fileName = "MapGeneratorSettings", menuName = "Structure Generation/Map Generator Settings")]
    public class MapGeneratorSettings : ScriptableObject
    {
        [Header("基本設定")]
        [Tooltip("マップ生成のシード値（0で毎回ランダム）")]
        public int masterSeed = 0;

        [Header("構造物の配置")]
        [Tooltip("チャンク範囲ベースの配置を使用")]
        public bool useChunkBasedPlacement = true;

        [Tooltip("配置範囲の最小チャンク座標（X, Y, Z）")]
        public Vector3Int minChunkCoord = new Vector3Int(-10, -5, -10);

        [Tooltip("配置範囲の最大チャンク座標（X, Y, Z）")]
        public Vector3Int maxChunkCoord = new Vector3Int(10, 0, 10);

        [Tooltip("範囲を埋めるボクセルID（0で埋めない）")]
        [Range(0, 10)]
        public byte fillVoxelId = 1;

        [Tooltip("範囲を埋める")]
        public bool fillPlacementArea = true;

        [Header("構造物設定への参照")]
        public TreasureCaveSettings treasureCaveSettings;
        public HardFloorCaveSettings hardFloorCaveSettings;

        [Header("接続設定")]
        [Tooltip("接続（トンネル）の生成設定")]
        public ConnectionSettings connectionSettings;

        [Header("接続生成パラメータ")]
        [Tooltip("各構造物からの接続数")]
        [Range(1, 5)]
        public int connectionsPerStructure = 2;

        [Tooltip("開いたトンネルの生成比率（0.0 - 1.0）")]
        [Range(0f, 1f)]
        public float openTunnelRatio = 1.0f;

        [Header("高度差による接続制限")]
        [Tooltip("接続可能な最大の高度差")]
        public float maxConnectionHeightDiff = 30f;

        [Header("境界壁設定")]
        [Tooltip("境界壁を生成する")]
        public bool generateBoundaryWalls = true;

        [Tooltip("境界壁のボクセルID（破壊不可ブロック）")]
        [Range(0, 10)]
        public byte boundaryWallVoxelId = 3;

        [Header("透明境界壁設定")]
        [Tooltip("透明な当たり判定壁を生成する")]
        public bool generateInvisibleBoundaryColliders = true;

        [Tooltip("天井（y+）の高さ（メートル）")]
        [Range(0f, 100f)]
        public float ceilingHeight = 50f;

        [Header("地表生成設定")]
        [Tooltip("地表を生成する")]
        public bool generateSurfaceTerrain = true;

        [Tooltip("地表の基準高さ（Y座標、通常は0）")]
        public float surfaceBaseHeight = 0f;

        [Tooltip("中心部の地表高さ（メートル）")]
        [Range(0f, 20f)]
        public float surfaceCenterHeight = 2f;

        [Tooltip("中心部の平坦な範囲（0.0-1.0、マップ全体に対する割合）")]
        [Range(0f, 1f)]
        public float surfaceFlatCenterRatio = 0.3f;

        [Tooltip("境界部の地表高さ（メートル）")]
        [Range(0f, 50f)]
        public float surfaceEdgeHeight = 15f;

        [Tooltip("境界をカバーする内側距離（メートル）")]
        [Range(1f, 10f)]
        public float surfaceBoundaryInset = 3f;

        [Tooltip("地表のボクセルID（土・草）")]
        [Range(0, 10)]
        public byte surfaceVoxelId = 2;

        [Tooltip("地表ノイズの振幅（メートル）")]
        [Range(0f, 5f)]
        public float surfaceNoiseAmplitude = 1.5f;

        [Tooltip("地表ノイズの周波数")]
        [Range(0.01f, 0.5f)]
        public float surfaceNoiseFrequency = 0.08f;

       

    }
}

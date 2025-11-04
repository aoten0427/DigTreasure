using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// SeparationDetector設定管理
    /// </summary>
    [System.Serializable]
    public class SeparationDetectorSettings
    {
        [Header("分離検出設定")]
        [SerializeField] private int m_maxSeparationSize = 2000;
        [SerializeField] private int m_minSeparationSize = 1;
        [SerializeField] private bool m_enableDiagonalConnection = false;
        [SerializeField] private int m_maxChunkRange = 3;
        [SerializeField] private bool m_enableChunkRangeLimit = true;

        [Header("パフォーマンス設定")]
        [SerializeField] private int m_maxChecksPerFrame = 500;
        [SerializeField] private bool m_enableFrameDistribution = true;
        [SerializeField] private float m_maxProcessingTimePerFrame = 0.016f;
        [SerializeField] private int m_maxConcurrentGroups = 5;
        [SerializeField] private float m_frameDistributionYieldTime = 0.001f;

        [Header("破壊率最適化設定")]
        [SerializeField] private bool m_enableDestructionRateOptimization = true;
        [SerializeField] [Range(0.05f, 0.5f)] private float m_destructionRateThreshold = 0.15f;

        [Header("2段階判定最適化設定")]
        [SerializeField] private bool m_enableTwoStageDetection = true;
        [SerializeField] [Range(30, 500)] private int m_suspiciousSizeThreshold = 500;

        [Header("デバッグ設定")]
        [SerializeField] private bool m_enableDetailedLogging = false;
        [SerializeField] private bool m_enablePerformanceLogging = true;

        [SerializeField] public bool m_isLog = false;

        // プロパティ
        public int MaxSeparationSize 
        { 
            get => m_maxSeparationSize; 
            set => m_maxSeparationSize = Mathf.Max(1, value); 
        }
        
        public int MinSeparationSize 
        { 
            get => m_minSeparationSize; 
            set => m_minSeparationSize = Mathf.Max(1, value); 
        }
        
        public bool EnableDiagonalConnection
        {
            get => m_enableDiagonalConnection;
            set => m_enableDiagonalConnection = value;
        }

        public int MaxChunkRange
        {
            get => m_maxChunkRange;
            set => m_maxChunkRange = Mathf.Max(1, value);
        }

        public bool EnableChunkRangeLimit
        {
            get => m_enableChunkRangeLimit;
            set => m_enableChunkRangeLimit = value;
        }

        public int MaxChecksPerFrame 
        { 
            get => m_maxChecksPerFrame; 
            set => m_maxChecksPerFrame = Mathf.Max(1, value); 
        }
        
        public bool EnableFrameDistribution 
        { 
            get => m_enableFrameDistribution; 
            set => m_enableFrameDistribution = value; 
        }
        
        public float MaxProcessingTimePerFrame 
        { 
            get => m_maxProcessingTimePerFrame; 
            set => m_maxProcessingTimePerFrame = Mathf.Max(0.001f, value); 
        }
        
        public int MaxConcurrentGroups 
        { 
            get => m_maxConcurrentGroups; 
            set => m_maxConcurrentGroups = Mathf.Max(1, value); 
        }
        
        public bool EnableDetailedLogging 
        { 
            get => m_enableDetailedLogging; 
            set => m_enableDetailedLogging = value; 
        }
        
        public bool EnablePerformanceLogging 
        { 
            get => m_enablePerformanceLogging; 
            set => m_enablePerformanceLogging = value; 
        }
        
        public float FrameDistributionYieldTime
        {
            get => m_frameDistributionYieldTime;
            set => m_frameDistributionYieldTime = Mathf.Max(0f, value);
        }

        public bool EnableDestructionRateOptimization
        {
            get => m_enableDestructionRateOptimization;
            set => m_enableDestructionRateOptimization = value;
        }

        public float DestructionRateThreshold
        {
            get => m_destructionRateThreshold;
            set => m_destructionRateThreshold = Mathf.Clamp(value, 0.05f, 0.5f);
        }

        public bool EnableTwoStageDetection
        {
            get => m_enableTwoStageDetection;
            set => m_enableTwoStageDetection = value;
        }

        public int SuspiciousSizeThreshold
        {
            get => m_suspiciousSizeThreshold;
            set => m_suspiciousSizeThreshold = Mathf.Clamp(value, 30, 200);
        }

        // デフォルト設定作成
        public static SeparationDetectorSettings CreateDefault()
        {
            return new SeparationDetectorSettings();
        }
    }
    
    /// <summary>
    /// ボクセル近隣検索ユーティリティ
    /// </summary>
    public static class VoxelNeighborUtility
    {
        // 6方向の近隣オフセット（上下左右前後）
        private static readonly Vector3[] s_sixDirections = {
            Vector3.up,    Vector3.down,
            Vector3.left,  Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        // 26方向の近隣オフセット（対角線含む）
        private static readonly Vector3[] s_twentySixDirections;
        
        static VoxelNeighborUtility()
        {
            var directions = new List<Vector3>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        directions.Add(new Vector3(x, y, z));
                    }
                }
            }
            s_twentySixDirections = directions.ToArray();
        }
        
        // 近隣座標取得（6方向）
        public static Vector3[] GetSixDirectionNeighbors(Vector3 position)
        {
            var neighbors = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = position + s_sixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }
            return neighbors;
        }
        
        // 近隣座標取得（26方向）
        public static Vector3[] GetTwentySixDirectionNeighbors(Vector3 position)
        {
            var neighbors = new Vector3[26];
            for (int i = 0; i < 26; i++)
            {
                neighbors[i] = position + s_twentySixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }
            return neighbors;
        }
        
        // 設定に基づく近隣座標取得
        public static Vector3[] GetNeighbors(Vector3 position, bool includeDiagonal)
        {
            return includeDiagonal ?
                GetTwentySixDirectionNeighbors(position) :
                GetSixDirectionNeighbors(position);
        }

        // 高速化: オフセット配列を直接取得（座標計算不要）
        public static Vector3[] GetNeighborOffsets(bool includeDiagonal)
        {
            if (includeDiagonal)
            {
                // 26方向オフセット（既にVOXEL_SIZEでスケール済み）
                var offsets = new Vector3[26];
                for (int i = 0; i < 26; i++)
                {
                    offsets[i] = s_twentySixDirections[i] * VoxelConstants.VOXEL_SIZE;
                }
                return offsets;
            }
            else
            {
                // 6方向オフセット（既にVOXEL_SIZEでスケール済み）
                var offsets = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    offsets[i] = s_sixDirections[i] * VoxelConstants.VOXEL_SIZE;
                }
                return offsets;
            }
        }
        
        // 範囲内近隣フィルタリング
        public static List<Vector3> FilterValidNeighbors(Vector3[] neighbors, IVoxelProvider provider)
        {
            var validNeighbors = new List<Vector3>();
            foreach (var neighbor in neighbors)
            {
                if (provider.IsValidPosition(neighbor))
                {
                    validNeighbors.Add(neighbor);
                }
            }
            return validNeighbors;
        }
    }
}
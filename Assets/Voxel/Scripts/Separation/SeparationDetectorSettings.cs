using UnityEngine;

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

        /// <summary>
        /// デフォルト設定作成
        /// </summary>
        public static SeparationDetectorSettings CreateDefault()
        {
            return new SeparationDetectorSettings();
        }
    }
}

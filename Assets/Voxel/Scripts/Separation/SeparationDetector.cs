using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 分離検出アルゴリズムクラス
    /// </summary>
    [System.Serializable]
    public partial class SeparationDetector
    {
        [SerializeField] private SeparationDetectorSettings m_settings = new SeparationDetectorSettings();

        private bool m_isInitialized = false;

        // 設定オブジェクトへの直接アクセス
        public SeparationDetectorSettings Settings
        {
            get
            {
                EnsureInitialized();
                return m_settings;
            }
            set
            {
                m_settings = value ?? new SeparationDetectorSettings();
                m_isInitialized = false;
            }
        }

        /// <summary>
        /// 初期化を確実に実行
        /// </summary>
        private void EnsureInitialized()
        {
            if (!m_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Initialize()
        {
            if (m_settings == null)
            {
                m_settings = new SeparationDetectorSettings();
            }

            m_isInitialized = true;
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public SeparationDetector()
        {
            // デフォルト設定は既にSerializeFieldで設定済み
            if (m_settings == null)
            {
                m_settings = new SeparationDetectorSettings();
            }
        }

        /// <summary>
        /// 設定オブジェクト付きコンストラクタ
        /// </summary>
        /// <param name="settings">分離検出設定</param>
        public SeparationDetector(SeparationDetectorSettings settings)
        {
            m_settings = settings ?? new SeparationDetectorSettings();
        }

        /// <summary>
        /// デフォルト設定でSeparationDetectorを作成
        /// </summary>
        /// <returns>デフォルト設定のSeparationDetector</returns>
        public static SeparationDetector CreateDefault()
        {
            return new SeparationDetector(SeparationDetectorSettings.CreateDefault());
        }
    }
}

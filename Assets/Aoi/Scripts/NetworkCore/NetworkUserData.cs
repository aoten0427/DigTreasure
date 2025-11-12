using Fusion;
using UnityEngine;

namespace NetWork
{
    /// <summary>
    /// ネットワーク用のユーザーデータ
    /// </summary>
    public struct NetworkUserData : INetworkStruct
    {
        public int m_id;//ネット用ID
        public NetworkString<_16> m_name;//名前
        public bool m_isPlayData;
        public int m_treasureCount;//取ったお宝の数
        public int m_treasurePoint;//お宝のポイント
        public int m_digPoint;//掘ったポイント



        /// <summary>
        /// ポイントの合計を返す
        /// </summary>
        /// <returns></returns>
        public int GetTotalScore()
        {
            return 0;
        }

        public void Reset()
        {
            m_id = 0;
            m_name = "";
            m_isPlayData = false;
            m_treasureCount = 0;
            m_treasurePoint = 0;
            m_digPoint = 0;
        }
    }
}

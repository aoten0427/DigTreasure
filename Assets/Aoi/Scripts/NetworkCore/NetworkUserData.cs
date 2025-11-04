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
    }
}

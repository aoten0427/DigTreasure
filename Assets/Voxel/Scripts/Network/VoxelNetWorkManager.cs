using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;
using System;
using NetWork;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルのネットワーク同期を管理
    /// </summary>
    public class VoxelNetWorkManager : NetworkBehaviour
    {
        WorldManager m_worldManager;

        //一度に送れる制限数（OnReliableDataReceived使用: 8バイト/個、約450個まで可能）
        private const int MAX_CHANGES_PER_PACKET = 450;

        //ログ
        [SerializeField] bool m_isLog = false;

        // ReliableKeyの定義（データ識別用）
        private static readonly Fusion.Sockets.ReliableKey VOXEL_UPDATE_KEY = Fusion.Sockets.ReliableKey.FromInts(1, 0, 0, 0);

        // バッチ蓄積用
        private class BatchInfo
        {
            public int TotalBatches;
            public int ReceivedCount;
            public List<VoxelUpdate> Updates = new List<VoxelUpdate>();
        }

        // ユーザー識別キー
        private Dictionary<string, BatchInfo> m_pendingBatches = new Dictionary<string, BatchInfo>();

        // 各プレイヤーのバッチIDカウンター
        private Dictionary<PlayerRef, int> m_playerBatchCounters = new Dictionary<PlayerRef, int>();

        // GameLauncher参照（OnReliableDataReceivedイベント登録用）
        private NetWork.GameLauncher m_gameLauncher;

        public override void Spawned()
        {
            m_worldManager = WorldManager.Instance;

            //VoxelOperationManagerのイベント設定
            if (m_worldManager != null && m_worldManager.Voxels != null)
            {
                // イベントに登録
                m_worldManager.Voxels.OnVoxelChanged += SyncVoxelUpdates;

                // StateAuthorityを持つクライアントのみイベント発火
                //m_worldManager.Voxels.SetEventFireCondition(() => Object.HasStateAuthority);
            }

            // GameLauncherを取得してOnReliableDataReceivedに登録
            m_gameLauncher = GameLauncher.Instance;
            m_gameLauncher.OnReliableDataReceived += OnReliableDataReceived;
            
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // イベント登録解除
            if (m_worldManager != null && m_worldManager.Voxels != null)
            {
                m_worldManager.Voxels.OnVoxelChanged -= SyncVoxelUpdates;
            }

            // OnReliableDataReceived登録解除
            if (m_gameLauncher != null)
            {
                m_gameLauncher.OnReliableDataReceived -= OnReliableDataReceived;
            }
        }
 

        /// <summary>
        /// ボクセル変更をネットワーク同期（RPCモードorReliableDataモード）
        /// </summary>
        public void SyncVoxelUpdates(List<VoxelUpdate> updates)
        {
            Debug.Log("変更受信");
            if (updates == null || updates.Count == 0) return;

            // OnReliableDataReceived方式
            SyncVoxelUpdatesWithReliableData(updates);
            
        }

        /// <summary>
        /// NetworkVoxelChange（8バイト/個）で送信
        /// </summary>
        private void SyncVoxelUpdatesWithReliableData(List<VoxelUpdate> updates)
        {
            // VoxelUpdate → NetworkVoxelChange に変換
            var networkChanges = new List<NetworkVoxelChange>();

            foreach (var update in updates)
            {
                networkChanges.Add(NetworkVoxelChange.FromVoxelUpdate(update));
            }

            //バッチIDを取得
            PlayerRef localPlayer = Runner.LocalPlayer;
            if (!m_playerBatchCounters.ContainsKey(localPlayer))
            {
                m_playerBatchCounters[localPlayer] = 0;
            }
            int batchId = m_playerBatchCounters[localPlayer]++;

            //送信
            int totalBatches = Mathf.CeilToInt((float)networkChanges.Count / MAX_CHANGES_PER_PACKET);

            for (int i = 0; i < networkChanges.Count; i += MAX_CHANGES_PER_PACKET)
            {
                int count = Mathf.Min(MAX_CHANGES_PER_PACKET, networkChanges.Count - i);
                var batch = networkChanges.GetRange(i, count);
                int batchIndex = i / MAX_CHANGES_PER_PACKET;

                // バイト配列にシリアライズ
                byte[] dataBytes = SerializeVoxelChanges(batch, localPlayer, batchId, batchIndex, totalBatches);

                // 全プレイヤーに送信
                foreach (var targetPlayer in Runner.ActivePlayers)
                {
                    if (targetPlayer == Runner.LocalPlayer) continue; // 自分には送らない

                    Runner.SendReliableDataToPlayer(targetPlayer, VOXEL_UPDATE_KEY, dataBytes);
                }
            }

            if(m_isLog)Debug.Log($"[VoxelNetWorkManager] ReliableDataモード: {updates.Count}個送信（{totalBatches}バッチ、Player={localPlayer}, BatchID={batchId}）");
        }

        /// <summary>
        /// NetworkVoxelChangeリストをバイト配列にシリアライズ（8バイト/個）
        /// データ構造: [PlayerID(4)] [BatchID(4)] [BatchIndex(4)] [TotalBatches(4)] [Count(4)] [Change1(8)] [Change2(8)] ...
        /// </summary>
        private byte[] SerializeVoxelChanges(List<NetworkVoxelChange> changes, PlayerRef sender, int batchId, int batchIndex, int totalBatches)
        {
            int headerSize = 4 + 4 + 4 + 4 + 4; // 20バイト
            int dataSize = changes.Count * 8; // 8バイト/個（short×4 = 2×4）
            byte[] data = new byte[headerSize + dataSize];

            int offset = 0;

            // ヘッダー情報を書き込み
            BitConverter.GetBytes(sender.PlayerId).CopyTo(data, offset);
            offset += 4;
            BitConverter.GetBytes(batchId).CopyTo(data, offset);
            offset += 4;
            BitConverter.GetBytes(batchIndex).CopyTo(data, offset);
            offset += 4;
            BitConverter.GetBytes(totalBatches).CopyTo(data, offset);
            offset += 4;
            BitConverter.GetBytes(changes.Count).CopyTo(data, offset);
            offset += 4;

            // NetworkVoxelChangeを書き込み（8バイト/個）
            foreach (var change in changes)
            {
                BitConverter.GetBytes(change.x).CopyTo(data, offset);
                offset += 2;
                BitConverter.GetBytes(change.y).CopyTo(data, offset);
                offset += 2;
                BitConverter.GetBytes(change.z).CopyTo(data, offset);
                offset += 2;
                BitConverter.GetBytes(change.voxelID).CopyTo(data, offset);
                offset += 2;
            }

            return data;
        }

        /// <summary>
        /// OnReliableDataReceivedでデータを受信
        /// </summary>
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            //キー取得
            int key0, key1, key2, key3;
            key.GetInts(out key0, out key1, out key2, out key3);
            if (key0 != 1) return;

            // デシリアライズ
            int offset = 0;
            int senderId = BitConverter.ToInt32(data.Array, data.Offset + offset);
            offset += 4;
            int batchId = BitConverter.ToInt32(data.Array, data.Offset + offset);
            offset += 4;
            int batchIndex = BitConverter.ToInt32(data.Array, data.Offset + offset);
            offset += 4;
            int totalBatches = BitConverter.ToInt32(data.Array, data.Offset + offset);
            offset += 4;
            int count = BitConverter.ToInt32(data.Array, data.Offset + offset);
            offset += 4;

            // NetworkVoxelChange → VoxelUpdate に変換
            var voxelUpdates = new List<VoxelUpdate>();
            for (int i = 0; i < count; i++)
            {
                short x = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;
                short y = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;
                short z = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;
                short voxelID = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;

                var networkChange = new NetworkVoxelChange
                {
                    x = x,
                    y = y,
                    z = z,
                    voxelID = voxelID
                };

                voxelUpdates.Add(networkChange.ToVoxelUpdate());
            }

            //ユニークキーを生成
            string batchKey = $"{senderId}_{batchId}";

            //バッチ情報を初期化または取得
            if (!m_pendingBatches.ContainsKey(batchKey))
            {
                m_pendingBatches[batchKey] = new BatchInfo
                {
                    TotalBatches = totalBatches,
                    ReceivedCount = 0
                };
            }

            var batchInfo = m_pendingBatches[batchKey];
            batchInfo.Updates.AddRange(voxelUpdates);
            batchInfo.ReceivedCount++;

            if(m_isLog)Debug.Log($"[VoxelNetWorkManager] ReliableData受信: Player={senderId}, BatchID={batchId}, {batchInfo.ReceivedCount}/{batchInfo.TotalBatches}");

            //全バッチが揃ったら一括適用
            if (batchInfo.ReceivedCount >= batchInfo.TotalBatches)
            {
                if(m_isLog)Debug.Log($"[VoxelNetWorkManager] ReliableData全バッチ受信完了: Player={senderId}, BatchID={batchId}, 合計={batchInfo.Updates.Count}個");

                //SetVoxel呼び出し
                m_worldManager.Voxels.SetVoxels(batchInfo.Updates, false);

                // クリーンアップ
                m_pendingBatches.Remove(batchKey);

                if(m_isLog)Debug.Log($"[VoxelNetWorkManager] ReliableData SetVoxels完了: Player={senderId}, BatchID={batchId}");
            }
        }
    }


}

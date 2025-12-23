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

        // ネットワークキー定数
        private const int KEY_VOXEL_UPDATE = 1;
        private const int KEY_VOXEL_DESTRUCTION = 2;

        // データサイズ定数
        private const int HEADER_SIZE = 20;  // 4 + 4 + 4 + 4 + 4 バイト
        private const int BYTES_PER_CHANGE = 8;   // short × 4
        private const int BYTES_PER_DESTRUCTION = 6;  // short × 3

        //一度に送れる制限数（OnReliableDataReceived使用: 8バイト/個、約450個まで可能）
        private const int MAX_CHANGES_PER_PACKET = 450;
        //破壊専用の制限数（6バイト/個、約680個まで可能）
        private const int MAX_DESTRUCTIONS_PER_PACKET = 680;

        //ログ
        [SerializeField] bool m_isLog = false;

        // ReliableKeyの定義（データ識別用）
        private static readonly Fusion.Sockets.ReliableKey VOXEL_UPDATE_KEY = Fusion.Sockets.ReliableKey.FromInts(KEY_VOXEL_UPDATE, 0, 0, 0);
        // 破壊専用キー
        private static readonly Fusion.Sockets.ReliableKey VOXEL_DESTRUCTION_KEY = Fusion.Sockets.ReliableKey.FromInts(KEY_VOXEL_DESTRUCTION, 0, 0, 0);

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

            // WorldManager のNullチェック
            if (m_worldManager == null)
            {
                Debug.LogError("[VoxelNetWorkManager] WorldManager.Instance が Null です");
                return;
            }

            // VoxelOperationManagerのNullチェック
            if (m_worldManager.Voxels == null)
            {
                Debug.LogError("[VoxelNetWorkManager] WorldManager.Voxels が Null です");
                return;
            }

            // イベントに登録
            m_worldManager.Voxels.OnVoxelChanged += SyncVoxelUpdates;

            // GameLauncherを取得してOnReliableDataReceivedに登録
            m_gameLauncher = GameLauncher.Instance;
            if (m_gameLauncher == null)
            {
                Debug.LogError("[VoxelNetWorkManager] GameLauncher.Instance が Null です");
                return;
            }
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
        /// ボクセル変更をネットワーク同期（自動判定）
        /// </summary>
        public void SyncVoxelUpdates(List<VoxelUpdate> updates)
        {
            if (m_isLog) Debug.Log("[VoxelNetWorkManager] ボクセル変更受信");
            if (updates == null || updates.Count == 0) return;

            // すべて破壊(VoxelID=0)かチェック（1回のループで判定）
            bool hasNonDestruction = false;
            for (int i = 0; i < updates.Count; i++)
            {
                if (updates[i].VoxelID != 0)
                {
                    hasNonDestruction = true;
                    break;
                }
            }

            if (hasNonDestruction)
            {
                // 通常送信（8バイト/個）
                SyncVoxelUpdatesWithReliableData(updates);
            }
            else
            {
                // 破壊専用送信（6バイト/個）
                SyncVoxelDestructionsWithReliableData(updates);
            }
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
            int dataSize = changes.Count * BYTES_PER_CHANGE;
            byte[] data = new byte[HEADER_SIZE + dataSize];

            // ヘッダー情報を書き込み
            var header = new NetworkPacketHeader
            {
                SenderId = sender.PlayerId,
                BatchId = batchId,
                BatchIndex = batchIndex,
                TotalBatches = totalBatches,
                Count = changes.Count
            };
            int offset = VoxelNetworkSerializer.WriteHeader(data, 0, header);

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
        /// 破壊専用のネットワーク同期（6バイト/個）
        /// </summary>
        private void SyncVoxelDestructionsWithReliableData(List<VoxelUpdate> updates)
        {
            // VoxelUpdate → NetworkVoxelDestruction に変換
            var networkDestructions = new List<NetworkVoxelDestruction>();

            foreach (var update in updates)
            {
                // VoxelID が 0 (Empty) であることを確認
                if (update.VoxelID == 0)
                {
                    networkDestructions.Add(NetworkVoxelDestruction.FromVoxelUpdate(update));
                }
                else
                {
                    Debug.LogWarning($"[VoxelNetWorkManager] 破壊でない変更が含まれています: VoxelID={update.VoxelID}");
                }
            }

            if (networkDestructions.Count == 0) return;

            // バッチID を取得
            PlayerRef localPlayer = Runner.LocalPlayer;
            if (!m_playerBatchCounters.ContainsKey(localPlayer))
            {
                m_playerBatchCounters[localPlayer] = 0;
            }
            int batchId = m_playerBatchCounters[localPlayer]++;

            // 送信
            int totalBatches = Mathf.CeilToInt((float)networkDestructions.Count / MAX_DESTRUCTIONS_PER_PACKET);

            for (int i = 0; i < networkDestructions.Count; i += MAX_DESTRUCTIONS_PER_PACKET)
            {
                int count = Mathf.Min(MAX_DESTRUCTIONS_PER_PACKET, networkDestructions.Count - i);
                var batch = networkDestructions.GetRange(i, count);
                int batchIndex = i / MAX_DESTRUCTIONS_PER_PACKET;

                // バイト配列にシリアライズ
                byte[] dataBytes = SerializeVoxelDestructions(batch, localPlayer, batchId, batchIndex, totalBatches);

                // 全プレイヤーに送信
                foreach (var targetPlayer in Runner.ActivePlayers)
                {
                    if (targetPlayer == Runner.LocalPlayer) continue;
                    Runner.SendReliableDataToPlayer(targetPlayer, VOXEL_DESTRUCTION_KEY, dataBytes);
                }
            }

            if (m_isLog) Debug.Log($"[VoxelNetWorkManager] 破壊専用送信: {networkDestructions.Count}個（{totalBatches}バッチ、Player={localPlayer}, BatchID={batchId}）");
        }

        /// <summary>
        /// NetworkVoxelDestructionリストをバイト配列にシリアライズ（6バイト/個）
        /// データ構造: [PlayerID(4)] [BatchID(4)] [BatchIndex(4)] [TotalBatches(4)] [Count(4)] [Dest1(6)] [Dest2(6)] ...
        /// </summary>
        private byte[] SerializeVoxelDestructions(
            List<NetworkVoxelDestruction> destructions,
            PlayerRef sender,
            int batchId,
            int batchIndex,
            int totalBatches)
        {
            int dataSize = destructions.Count * BYTES_PER_DESTRUCTION;
            byte[] data = new byte[HEADER_SIZE + dataSize];

            // ヘッダー情報を書き込み
            var header = new NetworkPacketHeader
            {
                SenderId = sender.PlayerId,
                BatchId = batchId,
                BatchIndex = batchIndex,
                TotalBatches = totalBatches,
                Count = destructions.Count
            };
            int offset = VoxelNetworkSerializer.WriteHeader(data, 0, header);

            // NetworkVoxelDestructionを書き込み（6バイト/個）
            foreach (var dest in destructions)
            {
                BitConverter.GetBytes(dest.x).CopyTo(data, offset);
                offset += 2;
                BitConverter.GetBytes(dest.y).CopyTo(data, offset);
                offset += 2;
                BitConverter.GetBytes(dest.z).CopyTo(data, offset);
                offset += 2;
            }

            return data;
        }

        /// <summary>
        /// OnReliableDataReceivedでデータを受信
        /// </summary>
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            // キー取得
            int key0, key1, key2, key3;
            key.GetInts(out key0, out key1, out key2, out key3);

            // 通常変更
            if (key0 == KEY_VOXEL_UPDATE)
            {
                HandleVoxelUpdateReceived(data);
            }
            // 破壊専用
            else if (key0 == KEY_VOXEL_DESTRUCTION)
            {
                HandleVoxelDestructionReceived(data);
            }
        }

        /// <summary>
        /// 既存の通常変更受信処理
        /// </summary>
        private void HandleVoxelUpdateReceived(ArraySegment<byte> data)
        {
            // ヘッダーをデシリアライズ
            int offset = 0;
            var header = VoxelNetworkSerializer.ReadHeader(data, ref offset);

            // NetworkVoxelChange → VoxelUpdate に変換
            var voxelUpdates = new List<VoxelUpdate>();
            for (int i = 0; i < header.Count; i++)
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
            string batchKey = $"{header.SenderId}_{header.BatchId}";

            //バッチ情報を初期化または取得
            if (!m_pendingBatches.ContainsKey(batchKey))
            {
                m_pendingBatches[batchKey] = new BatchInfo
                {
                    TotalBatches = header.TotalBatches,
                    ReceivedCount = 0
                };
            }

            var batchInfo = m_pendingBatches[batchKey];
            batchInfo.Updates.AddRange(voxelUpdates);
            batchInfo.ReceivedCount++;

            if (m_isLog) Debug.Log($"[VoxelNetWorkManager] ReliableData受信: Player={header.SenderId}, BatchID={header.BatchId}, {batchInfo.ReceivedCount}/{batchInfo.TotalBatches}");

            //全バッチが揃ったら一括適用
            if (batchInfo.ReceivedCount >= batchInfo.TotalBatches)
            {
                if (m_isLog) Debug.Log($"[VoxelNetWorkManager] ReliableData全バッチ受信完了: Player={header.SenderId}, BatchID={header.BatchId}, 合計={batchInfo.Updates.Count}個");

                //SetVoxel呼び出し
                m_worldManager.Voxels.SetVoxels(batchInfo.Updates, false);

                // クリーンアップ
                m_pendingBatches.Remove(batchKey);

                if (m_isLog) Debug.Log($"[VoxelNetWorkManager] ReliableData SetVoxels完了: Player={header.SenderId}, BatchID={header.BatchId}");
            }
        }

        /// <summary>
        /// 破壊専用データの受信処理
        /// </summary>
        private void HandleVoxelDestructionReceived(ArraySegment<byte> data)
        {
            // ヘッダーをデシリアライズ
            int offset = 0;
            var header = VoxelNetworkSerializer.ReadHeader(data, ref offset);

            // NetworkVoxelDestruction → VoxelUpdate に変換
            var voxelUpdates = new List<VoxelUpdate>();
            for (int i = 0; i < header.Count; i++)
            {
                short x = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;
                short y = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;
                short z = BitConverter.ToInt16(data.Array, data.Offset + offset);
                offset += 2;

                var networkDest = new NetworkVoxelDestruction
                {
                    x = x,
                    y = y,
                    z = z
                };

                voxelUpdates.Add(networkDest.ToVoxelUpdate());
            }

            // ユニークキーを生成（通常変更と区別するために"dest_"プレフィックス）
            string batchKey = $"dest_{header.SenderId}_{header.BatchId}";

            // バッチ情報を初期化または取得
            if (!m_pendingBatches.ContainsKey(batchKey))
            {
                m_pendingBatches[batchKey] = new BatchInfo
                {
                    TotalBatches = header.TotalBatches,
                    ReceivedCount = 0
                };
            }

            var batchInfo = m_pendingBatches[batchKey];
            batchInfo.Updates.AddRange(voxelUpdates);
            batchInfo.ReceivedCount++;

            if (m_isLog) Debug.Log($"[VoxelNetWorkManager] 破壊受信: Player={header.SenderId}, BatchID={header.BatchId}, {batchInfo.ReceivedCount}/{batchInfo.TotalBatches}");

            // 全バッチが揃ったら一括適用
            if (batchInfo.ReceivedCount >= batchInfo.TotalBatches)
            {
                if (m_isLog) Debug.Log($"[VoxelNetWorkManager] 破壊全バッチ完了: Player={header.SenderId}, BatchID={header.BatchId}, 合計={batchInfo.Updates.Count}個");

                // SetVoxel呼び出し
                m_worldManager.Voxels.SetVoxels(batchInfo.Updates, false);

                // クリーンアップ
                m_pendingBatches.Remove(batchKey);

                if (m_isLog) Debug.Log($"[VoxelNetWorkManager] 破壊適用完了: Player={header.SenderId}, BatchID={header.BatchId}");
            }
        }
    }


}

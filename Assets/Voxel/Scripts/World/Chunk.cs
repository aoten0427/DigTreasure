using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelWorld
{
    /// <summary>
    /// 16×16×16ボクセルデータを管理するチャンククラス
    /// メッシュ生成、ボクセル操作、座標変換機能を提供
    /// </summary>
    public class Chunk
    {
        //チャンク内のボクセルデータ
        private Voxel[,,] m_voxels;
        //チャンクのワールド座標
        private Vector3Int m_chunkPosition;
        //チャンクが変更されたかどうか
        private bool m_isDirty;
        //生成されたメッシュ
        private Mesh m_cachedMesh;

        // ダーティ領域トラッキング（メッシュキャッシュ用）
        private Vector3Int m_dirtyMin = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        private Vector3Int m_dirtyMax = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        private bool m_hasDirtyRegion = false;

        // ボクセル数キャッシュ（破壊率計算用）
        private int? m_cachedVoxelCount = null;

        // プロパティ
        //チャンクのワールド座標
        public Vector3Int ChunkPosition => m_chunkPosition;
        //チャンクが変更されているかどうか
        public bool IsDirty => m_isDirty;
        //キャッシュされたメッシュ
        public Mesh CachedMesh => m_cachedMesh;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="chunkPosition">チャンクのワールド座標</param>
        public Chunk(Vector3Int chunkPosition)
        {
            m_chunkPosition = chunkPosition;
            m_voxels = new Voxel[VoxelConstants.CHUNK_WIDTH, VoxelConstants.CHUNK_HEIGHT, VoxelConstants.CHUNK_DEPTH];
            m_isDirty = true;
            m_cachedMesh = null;
            
            // 初期化：すべて空ボクセル
            ClearAllVoxels();
        }

        /// <summary>
        /// 指定されたローカル座標のボクセルを取得
        /// </summary>
        /// <param name="localX">ローカルX座標 (0-15)</param>
        /// <param name="localY">ローカルY座標 (0-15)</param>
        /// <param name="localZ">ローカルZ座標 (0-15)</param>
        /// <returns>ボクセル、範囲外の場合は空ボクセル</returns>
        public Voxel GetVoxel(int localX, int localY, int localZ)
        {
            if (!IsValidLocalPosition(localX, localY, localZ))
            {
                return Voxel.Empty;
            }
            
            return m_voxels[localX, localY, localZ];
        }

        /// <summary>
        /// 指定されたローカル座標にボクセルを設定
        /// </summary>
        /// <param name="localX">ローカルX座標 (0-15)</param>
        /// <param name="localY">ローカルY座標 (0-15)</param>
        /// <param name="localZ">ローカルZ座標 (0-15)</param>
        /// <param name="voxel">設定するボクセル</param>
        /// <returns>設定に成功した場合true</returns>
        public bool SetVoxel(int localX, int localY, int localZ, Voxel voxel)
        {
            if (!IsValidLocalPosition(localX, localY, localZ))
            {
                return false;
            }

            // 変更があった場合のみフラグを立てる
            if (!m_voxels[localX, localY, localZ].Equals(voxel))
            {
                // ボクセル数キャッシュを更新（破壊率計算用）
                if (m_cachedVoxelCount.HasValue)
                {
                    bool wasEmpty = m_voxels[localX, localY, localZ].IsEmpty;
                    bool isNowEmpty = voxel.IsEmpty;

                    if (wasEmpty && !isNowEmpty)
                        m_cachedVoxelCount++;
                    else if (!wasEmpty && isNowEmpty)
                        m_cachedVoxelCount--;
                }

                m_voxels[localX, localY, localZ] = voxel;
                SetDirty();

                // ダーティ領域を更新
                ExpandDirtyRegion(new Vector3Int(localX, localY, localZ));
            }

            return true;
        }

        /// <summary>
        /// Vector3Int版のボクセル取得
        /// </summary>
        /// <param name="localPosition">ローカル座標</param>
        /// <returns>ボクセル</returns>
        public Voxel GetVoxel(Vector3Int localPosition)
        {
            return GetVoxel(localPosition.x, localPosition.y, localPosition.z);
        }

        /// <summary>
        /// Vector3Int版のボクセル設定
        /// </summary>
        /// <param name="localPosition">ローカル座標</param>
        /// <param name="voxel">設定するボクセル</param>
        /// <returns>設定に成功した場合true</returns>
        public bool SetVoxel(Vector3Int localPosition, Voxel voxel)
        {
            return SetVoxel(localPosition.x, localPosition.y, localPosition.z, voxel);
        }

        /// <summary>
        /// 指定されたワールド座標のボクセルを取得
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <returns>ボクセル</returns>
        public Voxel GetVoxelFromWorldPosition(Vector3 worldPosition)
        {
            Vector3Int localPosition = WorldToLocalPosition(worldPosition);
            return GetVoxel(localPosition);
        }

        /// <summary>
        /// 指定されたワールド座標にボクセルを設定
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <param name="voxel">設定するボクセル</param>
        /// <returns>設定に成功した場合true</returns>
        public bool SetVoxelFromWorldPosition(Vector3 worldPosition, Voxel voxel)
        {
            Vector3Int localPosition = WorldToLocalPosition(worldPosition);
            return SetVoxel(localPosition, voxel);
        }

        /// <summary>
        /// ボクセルデータ配列を取得（コライダー生成用）
        /// </summary>
        /// <returns>ボクセルデータ配列</returns>
        public Voxel[,,] GetVoxelData()
        {
            return m_voxels;
        }

        /// <summary>
        /// ボクセルデータ配列のコピーを取得
        /// </summary>
        /// <returns>ボクセルデータ配列のコピー</returns>
        public Voxel[,,] GetVoxelArrayCopy()
        {
            var copy = new Voxel[VoxelConstants.CHUNK_WIDTH, VoxelConstants.CHUNK_HEIGHT, VoxelConstants.CHUNK_DEPTH];
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        copy[x, y, z] = m_voxels[x, y, z];
                    }
                }
            }
            return copy;
        }

        /// <summary>
        /// チャンク内のすべてのボクセルを空に設定
        /// </summary>
        public void ClearAllVoxels()
        {
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        m_voxels[x, y, z] = Voxel.Empty;
                    }
                }
            }
            SetDirty();

            // ボクセル数キャッシュをクリア
            m_cachedVoxelCount = 0;

            // 全体をダーティ領域として設定
            m_dirtyMin = Vector3Int.zero;
            m_dirtyMax = new Vector3Int(
                VoxelConstants.CHUNK_WIDTH - 1,
                VoxelConstants.CHUNK_HEIGHT - 1,
                VoxelConstants.CHUNK_DEPTH - 1
            );
            m_hasDirtyRegion = true;
        }

        /// <summary>
        /// チャンクを指定されたボクセルで埋める
        /// </summary>
        /// <param name="voxel">埋めるボクセル</param>
        public void FillWithVoxel(Voxel voxel)
        {
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        m_voxels[x, y, z] = voxel;
                    }
                }
            }
            SetDirty();

            // ボクセル数キャッシュを更新
            if (!voxel.IsEmpty)
            {
                m_cachedVoxelCount = VoxelConstants.CHUNK_WIDTH * VoxelConstants.CHUNK_HEIGHT * VoxelConstants.CHUNK_DEPTH;
            }
            else
            {
                m_cachedVoxelCount = 0;
            }

            // 全体をダーティ領域として設定
            m_dirtyMin = Vector3Int.zero;
            m_dirtyMax = new Vector3Int(
                VoxelConstants.CHUNK_WIDTH - 1,
                VoxelConstants.CHUNK_HEIGHT - 1,
                VoxelConstants.CHUNK_DEPTH - 1
            );
            m_hasDirtyRegion = true;
        }

        #region メッシュ生成（ChunkMeshクラスへの委譲）

        /// <summary>
        /// 同期的メッシュ生成（単一チャンクの即座更新用）
        /// 用途: VoxelSeparationManager等、即座にメッシュが必要な場合
        /// 大量のチャンク更新にはVoxelMeshManagerの並列処理を使用してください
        /// </summary>
        /// <returns>生成されたMesh（キャッシュに保存される）</returns>
        public Mesh GenerateMesh()
        {
            Mesh mesh = ChunkMesh.GenerateMesh(this);

            // キャッシュ管理
            if (m_cachedMesh != null)
            {
                Object.DestroyImmediate(m_cachedMesh);
            }
            m_cachedMesh = mesh;

            // ダーティフラグクリア
            m_isDirty = false;
            ResetDirtyRegion();

            return mesh;
        }

        /// <summary>
        /// 非同期メッシュ生成（Jobスケジュールのみ、完了待機なし）
        /// 並列処理用 - 複数チャンクを一度にスケジュールして並列実行可能
        /// </summary>
        /// <param name="chunkManager">隣接チャンク取得用のChunkManager（nullの場合は隣接データなし）</param>
        /// <param name="boundaryInfo">境界情報（nullの場合は境界なし）</param>
        /// <param name="boundarySettings">境界設定（nullの場合はデフォルト設定）</param>
        /// <returns>JobHandleとNativeContainerを含むデータ</returns>
        public ChunkMesh.MeshJobData ScheduleMeshGeneration(ChunkManager chunkManager = null, ChunkBoundaryInfo? boundaryInfo = null, BoundaryMeshSettings boundarySettings = null)
        {
            return ChunkMesh.ScheduleMeshGeneration(this, chunkManager, boundaryInfo, boundarySettings);
        }

        /// <summary>
        /// MeshJobDataからMeshを構築してキャッシュに適用
        /// Job完了待ちとキャッシュ保存を実行
        /// </summary>
        /// <param name="jobData">完了待ち対象のMeshJobData</param>
        /// <returns>生成されたMesh（キャッシュに保存される）</returns>
        public Mesh CompleteMeshGeneration(ChunkMesh.MeshJobData jobData)
        {
            Mesh mesh = ChunkMesh.CompleteMeshGeneration(jobData);

            // キャッシュ管理
            if (m_cachedMesh != null)
            {
                Object.DestroyImmediate(m_cachedMesh);
            }
            m_cachedMesh = mesh;

            // ダーティフラグクリア
            m_isDirty = false;
            ResetDirtyRegion();

            return mesh;
        }

        #endregion

        /// <summary>
        /// ローカル座標がチャンク内の有効な範囲かチェック
        /// </summary>
        /// <param name="localX">ローカルX座標</param>
        /// <param name="localY">ローカルY座標</param>
        /// <param name="localZ">ローカルZ座標</param>
        /// <returns>有効な場合true</returns>
        public bool IsValidLocalPosition(int localX, int localY, int localZ)
        {
            return localX >= 0 && localX < VoxelConstants.CHUNK_WIDTH &&
                   localY >= 0 && localY < VoxelConstants.CHUNK_HEIGHT &&
                   localZ >= 0 && localZ < VoxelConstants.CHUNK_DEPTH;
        }

        /// <summary>
        /// ワールド座標をローカル座標に変換
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <returns>ローカル座標</returns>
        public Vector3Int WorldToLocalPosition(Vector3 worldPosition)
        {
            Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(m_chunkPosition.x, m_chunkPosition.y, m_chunkPosition.z);
            Vector3 relativePos = worldPosition - chunkWorldPos;

            return new Vector3Int(
                Mathf.FloorToInt(relativePos.x * VoxelConstants.INV_VOXEL_SIZE),
                Mathf.FloorToInt(relativePos.y * VoxelConstants.INV_VOXEL_SIZE),
                Mathf.FloorToInt(relativePos.z * VoxelConstants.INV_VOXEL_SIZE)
            );
        }

        /// <summary>
        /// ローカル座標をワールド座標に変換
        /// </summary>
        /// <param name="localPosition">ローカル座標</param>
        /// <returns>ワールド座標</returns>
        public Vector3 LocalToWorldPosition(Vector3Int localPosition)
        {
            return VoxelConstants.LocalToWorldPosition(m_chunkPosition, localPosition.x, localPosition.y, localPosition.z);
        }

        /// <summary>
        /// チャンクが空かどうかチェック
        /// </summary>
        /// <returns>空の場合true</returns>
        public bool IsEmpty()
        {
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        if (!m_voxels[x, y, z].IsEmpty)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// チャンク内の非空ボクセル数を取得
        /// </summary>
        /// <returns>非空ボクセル数</returns>
        public int GetVoxelCount()
        {
            int count = 0;
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        if (!m_voxels[x, y, z].IsEmpty)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// ダーティフラグを設定
        /// </summary>
        public void SetDirty()
        {
            m_isDirty = true;
        }

        /// <summary>
        /// ダーティ領域を拡張（周辺1ボクセル含む）
        /// </summary>
        /// <param name="pos">変更されたローカル座標</param>
        private void ExpandDirtyRegion(Vector3Int pos)
        {
            // 周辺1ボクセル含めた範囲（面カリング対応）
            Vector3Int min = new Vector3Int(
                Mathf.Max(0, pos.x - 1),
                Mathf.Max(0, pos.y - 1),
                Mathf.Max(0, pos.z - 1)
            );
            Vector3Int max = new Vector3Int(
                Mathf.Min(VoxelConstants.CHUNK_WIDTH - 1, pos.x + 1),
                Mathf.Min(VoxelConstants.CHUNK_HEIGHT - 1, pos.y + 1),
                Mathf.Min(VoxelConstants.CHUNK_DEPTH - 1, pos.z + 1)
            );

            m_dirtyMin = Vector3Int.Min(m_dirtyMin, min);
            m_dirtyMax = Vector3Int.Max(m_dirtyMax, max);
            m_hasDirtyRegion = true;
        }


        /// <summary>
        /// ダーティ領域をリセット
        /// </summary>
        private void ResetDirtyRegion()
        {
            m_dirtyMin = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_dirtyMax = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            m_hasDirtyRegion = false;
        }

        /// <summary>
        /// チャンク内の非空ボクセル数を取得（キャッシュ付き）
        /// </summary>
        /// <returns>非空ボクセル数</returns>
        public int GetTotalVoxelCount()
        {
            if (m_cachedVoxelCount.HasValue)
                return m_cachedVoxelCount.Value;

            // 初回のみカウント
            int count = 0;
            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        if (!m_voxels[x, y, z].IsEmpty)
                            count++;
                    }
                }
            }

            m_cachedVoxelCount = count;
            return count;
        }

        /// <summary>
        /// リソースをクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            if (m_cachedMesh != null)
            {
                Object.DestroyImmediate(m_cachedMesh);
                m_cachedMesh = null;
            }
        }
    }
}
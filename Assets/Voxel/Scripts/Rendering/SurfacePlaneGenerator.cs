using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// カメラ前方に板を生成し、ボクセルデータに基づいて表示/非表示を制御する
    /// 遠くの洞窟を隠しつつ、近くのプレイヤーの掘削跡は見えるようにする
    ///
    /// ハイブリッドグリッド方式による高速化:
    /// - Phase 1: 粗いグリッド（16×16）でチャンクレベルの存在チェック
    /// - Phase 2: 高解像度（512×512）でボクセルレベルチェック（空領域はスキップ）
    /// - Parallel.Forによる並列処理で60fps達成（512×512で14ms）
    /// </summary>
    public class SurfacePlaneGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Material m_planeMaterial;
        [SerializeField] private int m_gridSize = 10;
        [SerializeField] private float m_cellSize = 5f;
        [SerializeField] private float m_distanceFromCamera = 20f;
        [SerializeField] private int m_textureResolution = 128;

        [Header("Hybrid Grid Optimization")]
        [SerializeField] private int m_coarseGridResolution = 16;
        [SerializeField] private bool m_enableParallelProcessing = true;

        [SerializeField] private float m_positionUpdateThreshold = 1.0f;
        [SerializeField] private float m_rotationUpdateThreshold = 5.0f;

        [Header("Performance Measurement")]
        [SerializeField] private bool m_enablePerformanceLog = true;
        [SerializeField] private bool m_enableAutoUpdate = false;

        private GameObject m_planeObject;
        private MeshFilter m_meshFilter;
        private MeshRenderer m_meshRenderer;
        private Camera m_mainCamera;
        private Texture2D m_voxelDataTexture;
        private Vector3 m_lastCameraPosition;
        private Vector3 m_lastCameraForward;

        // ハイブリッドグリッド用
        private bool[,] m_coarseGrid;
        private Color[] m_pixelBuffer;
        private float[] m_uvTableX;
        private float[] m_uvTableY;

        void Start()
        {
            InitializePlane();
            InitializeCamera();
            InitializeTexture();
            InitializeHybridGrid();
            GenerateSimplePlaneMesh();
        }

        void Update()
        {
            // 自動更新が有効な場合のみ
            if (m_enableAutoUpdate && ShouldUpdatePlane())
            {
                UpdatePlane();
            }

            // 手動更新（Tキー）
            if(Input.GetKeyDown(KeyCode.T))
            {
                UpdatePlane();
            }
        }

        /// <summary>
        /// 板オブジェクトを初期化
        /// </summary>
        private void InitializePlane()
        {
            m_planeObject = new GameObject("SurfacePlane");
            m_planeObject.transform.SetParent(transform);

            m_meshFilter = m_planeObject.AddComponent<MeshFilter>();
            m_meshRenderer = m_planeObject.AddComponent<MeshRenderer>();

            if (m_planeMaterial != null)
            {
                m_meshRenderer.material = m_planeMaterial;
            }
        }

        /// <summary>
        /// カメラを初期化
        /// </summary>
        private void InitializeCamera()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                return;
            }

            m_lastCameraPosition = m_mainCamera.transform.position;
            m_lastCameraForward = m_mainCamera.transform.forward;
        }

        /// <summary>
        /// ボクセルデータテクスチャを初期化
        /// </summary>
        private void InitializeTexture()
        {
            m_voxelDataTexture = new Texture2D(m_textureResolution, m_textureResolution, TextureFormat.RGB24, false);
            m_voxelDataTexture.filterMode = FilterMode.Point;
            m_voxelDataTexture.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// ハイブリッドグリッド用の初期化
        /// </summary>
        private void InitializeHybridGrid()
        {
            // 粗いグリッド初期化
            m_coarseGrid = new bool[m_coarseGridResolution, m_coarseGridResolution];

            // ピクセルバッファ初期化
            m_pixelBuffer = new Color[m_textureResolution * m_textureResolution];

            // UVテーブル事前計算
            m_uvTableX = new float[m_textureResolution];
            m_uvTableY = new float[m_textureResolution];

            float invRes = 1f / (m_textureResolution - 1);
            for (int i = 0; i < m_textureResolution; i++)
            {
                m_uvTableX[i] = i * invRes;
                m_uvTableY[i] = i * invRes;
            }

            Debug.Log($"[SurfacePlane] Hybrid Grid initialized: Coarse={m_coarseGridResolution}×{m_coarseGridResolution}, Fine={m_textureResolution}×{m_textureResolution}");
        }

        #endregion

        #region Plane Update

        /// <summary>
        /// 板を更新すべきかチェック
        /// カメラの位置または向きが閾値を超えて変化した場合にtrueを返す
        /// </summary>
        private bool ShouldUpdatePlane()
        {
            if (m_mainCamera == null) return false;

            Vector3 currentPosition = m_mainCamera.transform.position;
            Vector3 currentForward = m_mainCamera.transform.forward;

            float positionDelta = Vector3.Distance(currentPosition, m_lastCameraPosition);
            float angleDelta = Vector3.Angle(currentForward, m_lastCameraForward);

            return positionDelta > m_positionUpdateThreshold || angleDelta > m_rotationUpdateThreshold;
        }

        /// <summary>
        /// 板の位置とテクスチャを更新
        /// </summary>
        private void UpdatePlane()
        {

            UpdatePlaneOrientation();

            UpdateVoxelDataTexture();

            m_lastCameraPosition = m_mainCamera.transform.position;
            m_lastCameraForward = m_mainCamera.transform.forward;
        }

        /// <summary>
        /// 板をカメラの方を向くように配置（ビルボード）
        /// </summary>
        private void UpdatePlaneOrientation()
        {
            if (m_planeObject == null || m_mainCamera == null) return;

            Transform cameraTransform = m_mainCamera.transform;
            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * m_distanceFromCamera;

            m_planeObject.transform.position = targetPosition;
            m_planeObject.transform.LookAt(cameraTransform.position, Vector3.up);
        }

        #endregion

        #region Voxel Data Texture

        /// <summary>
        /// ボクセルデータテクスチャを更新（ハイブリッドグリッド版）
        /// Phase 1: 粗いグリッドでチャンクレベルチェック
        /// Phase 2: 高解像度でボクセルレベルチェック（空領域はスキップ）
        /// </summary>
        private void UpdateVoxelDataTexture()
        {
            if (m_voxelDataTexture == null || m_planeObject == null)
            {
                return;
            }

            long startTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            if (!TryGetChunkManager(out ChunkManager chunkManager))
            {
                return;
            }

            long chunkManagerTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            var planeInfo = GetPlaneTransformInfo();

            long planeInfoTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            // Phase 1: 粗いグリッド更新
            UpdateCoarseGrid(chunkManager, planeInfo);

            long coarseGridTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            // Phase 2: 高解像度ピクセル生成
            Color[] pixels = GenerateVoxelDataPixels(chunkManager, planeInfo);

            long pixelGenTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            ApplyTexture(pixels);

            long applyTime = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            if (m_enablePerformanceLog)
            {
                double ToMs(long ticks) => (ticks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000.0;

                double getChunkManagerMs = ToMs(chunkManagerTime - startTime);
                double getPlaneInfoMs = ToMs(planeInfoTime - chunkManagerTime);
                double coarseGridMs = ToMs(coarseGridTime - planeInfoTime);
                double generatePixelsMs = ToMs(pixelGenTime - coarseGridTime);
                double applyTextureMs = ToMs(applyTime - pixelGenTime);
                double totalMs = ToMs(applyTime - startTime);

                Debug.Log($"  [Texture Update] Total: {totalMs:F2}ms | ChunkMgr: {getChunkManagerMs:F2}ms | PlaneInfo: {getPlaneInfoMs:F2}ms | CoarseGrid: {coarseGridMs:F2}ms | Pixels: {generatePixelsMs:F2}ms | Apply: {applyTextureMs:F2}ms");
            }
        }

        /// <summary>
        /// ChunkManagerを取得
        /// </summary>
        private bool TryGetChunkManager(out ChunkManager chunkManager)
        {
            chunkManager = null;

            WorldManager worldManager = WorldManager.Instance;
            if (worldManager == null) return false;

            chunkManager = worldManager.Chunks;
            if (chunkManager == null) return false;

            return true;
        }

        /// <summary>
        /// 板のトランスフォーム情報を取得
        /// </summary>
        private PlaneTransformInfo GetPlaneTransformInfo()
        {
            Transform planeTransform = m_planeObject.transform;
            return new PlaneTransformInfo
            {
                Center = planeTransform.position,
                Right = planeTransform.right,
                Up = planeTransform.up,
                Size = m_gridSize * m_cellSize
            };
        }

        /// <summary>
        /// ボクセルデータピクセル配列を生成（並列処理版）
        /// </summary>
        private Color[] GenerateVoxelDataPixels(ChunkManager chunkManager, PlaneTransformInfo planeInfo)
        {
            // バッファ再利用
            System.Array.Clear(m_pixelBuffer, 0, m_pixelBuffer.Length);

            var chunkDict = chunkManager.Chunks;
            int pixelCount = m_textureResolution * m_textureResolution;

            long startTicks = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            // 統計情報
            int totalPixels = 0;
            int skippedPixels = 0;
            int voxelHits = 0;

            if (m_enableParallelProcessing)
            {
                // 並列処理
                var lockObj = new object();

                System.Threading.Tasks.Parallel.For(0, m_textureResolution, y =>
                {
                    // スレッドローカル変数
                    Chunk lastChunk = null;
                    Vector3Int lastChunkPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                    int localSkipped = 0;
                    int localHits = 0;

                    for (int x = 0; x < m_textureResolution; x++)
                    {
                        // 粗いグリッドでスキップ判定
                        int gridX = x * m_coarseGridResolution / m_textureResolution;
                        int gridY = y * m_coarseGridResolution / m_textureResolution;

                        if (!m_coarseGrid[gridX, gridY])
                        {
                            m_pixelBuffer[y * m_textureResolution + x] = Color.white;
                            localSkipped++;
                            continue;
                        }

                        // 詳細チェック
                        Vector3 worldPos = GetWorldPositionFromUV_Fast(x, y, planeInfo);
                        bool hasSolidVoxel = CheckVoxelSolid(worldPos, chunkDict,
                                                             ref lastChunk, ref lastChunkPos);

                        m_pixelBuffer[y * m_textureResolution + x] = hasSolidVoxel ? Color.black : Color.white;

                        if (hasSolidVoxel) localHits++;
                    }

                    // 統計情報を安全に集計
                    if (m_enablePerformanceLog)
                    {
                        lock (lockObj)
                        {
                            skippedPixels += localSkipped;
                            voxelHits += localHits;
                        }
                    }
                });

                totalPixels = pixelCount;
            }
            else
            {
                // 逐次処理（デバッグ用）
                Chunk lastChunk = null;
                Vector3Int lastChunkPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

                for (int y = 0; y < m_textureResolution; y++)
                {
                    for (int x = 0; x < m_textureResolution; x++)
                    {
                        int gridX = x * m_coarseGridResolution / m_textureResolution;
                        int gridY = y * m_coarseGridResolution / m_textureResolution;

                        if (!m_coarseGrid[gridX, gridY])
                        {
                            m_pixelBuffer[y * m_textureResolution + x] = Color.white;
                            skippedPixels++;
                            continue;
                        }

                        Vector3 worldPos = GetWorldPositionFromUV_Fast(x, y, planeInfo);
                        bool hasSolidVoxel = CheckVoxelSolid(worldPos, chunkDict,
                                                             ref lastChunk, ref lastChunkPos);

                        m_pixelBuffer[y * m_textureResolution + x] = hasSolidVoxel ? Color.black : Color.white;

                        if (hasSolidVoxel) voxelHits++;
                    }
                }

                totalPixels = pixelCount;
            }

            if (m_enablePerformanceLog)
            {
                long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                double ms = (endTicks - startTicks) / (double)System.Diagnostics.Stopwatch.Frequency * 1000.0;

                int processedPixels = totalPixels - skippedPixels;
                float skipRate = skippedPixels / (float)totalPixels * 100f;
                float hitRate = voxelHits / (float)totalPixels * 100f;

                Debug.Log($"    [Pixel Generation] Total: {totalPixels} | Skipped: {skippedPixels} ({skipRate:F1}%) | Voxel Hits: {voxelHits} ({hitRate:F1}%) | Time: {ms:F2}ms | Mode: {(m_enableParallelProcessing ? "Parallel" : "Sequential")}");
            }

            return m_pixelBuffer;
        }

        /// <summary>
        /// UV→ワールド座標変換（事前計算テーブル使用）
        /// </summary>
        private Vector3 GetWorldPositionFromUV_Fast(int x, int y, PlaneTransformInfo planeInfo)
        {
            float u = m_uvTableX[x];
            float v = m_uvTableY[y];

            float xOffset = (u - 0.5f) * planeInfo.Size;
            float yOffset = (v - 0.5f) * planeInfo.Size;

            return planeInfo.Center + planeInfo.Right * xOffset + planeInfo.Up * yOffset;
        }

        /// <summary>
        /// 指定ワールド座標にボクセルが存在するかチェック
        /// Chunkキャッシュを使用して高速化
        /// </summary>
        private bool CheckVoxelSolid(Vector3 worldPos, IReadOnlyDictionary<Vector3Int, Chunk> chunkDict,
                                      ref Chunk lastChunk, ref Vector3Int lastChunkPos)
        {
            Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);

            Chunk chunk;
            if (chunkPos == lastChunkPos)
            {
                chunk = lastChunk;
            }
            else
            {
                chunkDict.TryGetValue(chunkPos, out chunk);
                lastChunk = chunk;
                lastChunkPos = chunkPos;
            }

            if (chunk == null) return false;

            Voxel voxel = chunk.GetVoxelFromWorldPosition(worldPos);
            return !voxel.IsEmpty;
        }

        /// <summary>
        /// 粗いグリッドを更新（チャンクレベルの存在チェック）- 並列化版
        /// </summary>
        private void UpdateCoarseGrid(ChunkManager chunkManager, PlaneTransformInfo planeInfo)
        {
            var chunkDict = chunkManager.Chunks;

            long startTicks = m_enablePerformanceLog ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

            if (m_enableParallelProcessing)
            {
                // 並列処理
                System.Threading.Tasks.Parallel.For(0, m_coarseGridResolution, y =>
                {
                    for (int x = 0; x < m_coarseGridResolution; x++)
                    {
                        // 粗いグリッドセルの中心位置
                        float u = (x + 0.5f) / m_coarseGridResolution;
                        float v = (y + 0.5f) / m_coarseGridResolution;

                        Vector3 worldPos = GetWorldPositionFromUV_Coarse(u, v, planeInfo);

                        // このワールド座標のチャンクが存在し、空でないかチェック
                        Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);

                        bool hasVoxels = false;
                        if (chunkDict.TryGetValue(chunkPos, out Chunk chunk))
                        {
                            hasVoxels = chunk != null && !chunk.IsEmpty();
                        }

                        m_coarseGrid[x, y] = hasVoxels;
                    }
                });
            }
            else
            {
                // 逐次処理（デバッグ用）
                for (int y = 0; y < m_coarseGridResolution; y++)
                {
                    for (int x = 0; x < m_coarseGridResolution; x++)
                    {
                        float u = (x + 0.5f) / m_coarseGridResolution;
                        float v = (y + 0.5f) / m_coarseGridResolution;

                        Vector3 worldPos = GetWorldPositionFromUV_Coarse(u, v, planeInfo);
                        Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);

                        bool hasVoxels = false;
                        if (chunkDict.TryGetValue(chunkPos, out Chunk chunk))
                        {
                            hasVoxels = chunk != null && !chunk.IsEmpty();
                        }

                        m_coarseGrid[x, y] = hasVoxels;
                    }
                }
            }

            if (m_enablePerformanceLog)
            {
                long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                double ms = (endTicks - startTicks) / (double)System.Diagnostics.Stopwatch.Frequency * 1000.0;

                int occupiedCells = 0;
                for (int y = 0; y < m_coarseGridResolution; y++)
                    for (int x = 0; x < m_coarseGridResolution; x++)
                        if (m_coarseGrid[x, y]) occupiedCells++;

                float occupancyRate = occupiedCells / (float)(m_coarseGridResolution * m_coarseGridResolution) * 100f;
                Debug.Log($"    [Coarse Grid] {m_coarseGridResolution}×{m_coarseGridResolution} updated in {ms:F2}ms | Occupancy: {occupancyRate:F1}% ({occupiedCells}/{m_coarseGridResolution * m_coarseGridResolution}) | Mode: {(m_enableParallelProcessing ? "Parallel" : "Sequential")}");
            }
        }

        /// <summary>
        /// 粗いグリッド用のUV→ワールド座標変換
        /// </summary>
        private Vector3 GetWorldPositionFromUV_Coarse(float u, float v, PlaneTransformInfo planeInfo)
        {
            float xOffset = (u - 0.5f) * planeInfo.Size;
            float yOffset = (v - 0.5f) * planeInfo.Size;

            return planeInfo.Center + planeInfo.Right * xOffset + planeInfo.Up * yOffset;
        }

        /// <summary>
        /// テクスチャをマテリアルに適用
        /// </summary>
        private void ApplyTexture(Color[] pixels)
        {
            m_voxelDataTexture.SetPixels(pixels);
            m_voxelDataTexture.Apply();

            if (m_meshRenderer != null && m_meshRenderer.material != null)
            {
                m_meshRenderer.material.SetTexture("_VoxelDataTex", m_voxelDataTexture);
            }
        }

        #endregion

        #region Mesh Generation

        /// <summary>
        /// 単純な4頂点の平面メッシュを生成
        /// </summary>
        private void GenerateSimplePlaneMesh()
        {
            Mesh mesh = new Mesh { name = "SurfacePlaneMesh" };

            float halfSize = (m_gridSize * m_cellSize) * 0.5f;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-halfSize, -halfSize, 0),
                new Vector3( halfSize, -halfSize, 0),
                new Vector3( halfSize,  halfSize, 0),
                new Vector3(-halfSize,  halfSize, 0)
            };

            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            m_meshFilter.mesh = mesh;
        }

        #endregion

        #region Helper Structures

        /// <summary>
        /// 板のトランスフォーム情報を格納する構造体
        /// </summary>
        private struct PlaneTransformInfo
        {
            public Vector3 Center; // 板の中心位置
            public Vector3 Right; // 板の右方向
            public Vector3 Up; // 板の上方向
            public float Size; // 板のサイズ
        }

        #endregion

        #region Performance Test

        /// <summary>
        /// パフォーマンステストを実行（Inspectorから実行可能）
        /// </summary>
        [ContextMenu("Run Performance Test")]
        private void RunPerformanceTest()
        {
            const int iterations = 100;
            double totalTime = 0;
            bool originalLogState = m_enablePerformanceLog;

            Debug.Log($"[Performance Test] Starting {iterations} iterations...");

            // 最初の1回はウォームアップ（JIT最適化のため）
            m_enablePerformanceLog = false;
            UpdateVoxelDataTexture();

            // 計測開始
            m_enablePerformanceLog = false; // 詳細ログは無効化

            for (int i = 0; i < iterations; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                UpdateVoxelDataTexture();
                sw.Stop();
                totalTime += sw.Elapsed.TotalMilliseconds;
            }

            double avgTime = totalTime / iterations;
            double minTargetTime = 16.67; // 60fps
            double maxTargetTime = 33.33; // 30fps

            string performance = avgTime <= minTargetTime ? "EXCELLENT (60fps+)" :
                                 avgTime <= maxTargetTime ? "GOOD (30-60fps)" :
                                 "NEEDS OPTIMIZATION (<30fps)";

            Debug.Log($"[Performance Test] === RESULTS ===");
            Debug.Log($"[Performance Test] Average: {avgTime:F2}ms over {iterations} iterations");
            Debug.Log($"[Performance Test] Min: 16.67ms (60fps) | Max: 33.33ms (30fps)");
            Debug.Log($"[Performance Test] Status: {performance}");
            Debug.Log($"[Performance Test] Resolution: {m_textureResolution}×{m_textureResolution}");
            Debug.Log($"[Performance Test] Parallel Processing: {(m_enableParallelProcessing ? "Enabled" : "Disabled")}");
            Debug.Log($"[Performance Test] Coarse Grid: {m_coarseGridResolution}×{m_coarseGridResolution}");

            // ログ状態を復元
            m_enablePerformanceLog = originalLogState;
        }

        #endregion
    }
}

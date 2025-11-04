using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// カメラ前方に板を生成し、ボクセルデータに基づいて表示/非表示を制御する
    /// 遠くの洞窟を隠しつつ、近くのプレイヤーの掘削跡は見えるようにする
    /// </summary>
    public class SurfacePlaneGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Material m_planeMaterial;
        [SerializeField] private int m_gridSize = 10;
        [SerializeField] private float m_cellSize = 5f;
        [SerializeField] private float m_distanceFromCamera = 20f;
        [SerializeField] private int m_textureResolution = 128;

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

        // パフォーマンス測定用
        private System.Diagnostics.Stopwatch m_stopwatch = new System.Diagnostics.Stopwatch();


        void Start()
        {
            InitializePlane();
            InitializeCamera();
            InitializeTexture();
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
            if (m_enablePerformanceLog)
            {
                m_stopwatch.Restart();
            }

            UpdatePlaneOrientation();

            long orientationTime = 0;
            if (m_enablePerformanceLog)
            {
                orientationTime = m_stopwatch.ElapsedMilliseconds;
            }

            UpdateVoxelDataTexture();

            if (m_enablePerformanceLog)
            {
                long totalTime = m_stopwatch.ElapsedMilliseconds;
                long textureTime = totalTime - orientationTime;

                Debug.Log($"[SurfacePlane Performance] Total: {totalTime}ms | Orientation: {orientationTime}ms | Texture: {textureTime}ms");
            }

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
        /// ボクセルデータテクスチャを更新
        /// 板の表面上の各ピクセル位置にボクセルが存在するかをチェックしてテクスチャに書き込む
        /// </summary>
        private void UpdateVoxelDataTexture()
        {
            if (m_voxelDataTexture == null || m_planeObject == null)
            {
                return;
            }

            long startTime = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

            if (!TryGetChunkManager(out ChunkManager chunkManager))
            {
                return;
            }

            long chunkManagerTime = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

            var planeInfo = GetPlaneTransformInfo();

            long planeInfoTime = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

            Color[] pixels = GenerateVoxelDataPixels(chunkManager, planeInfo);

            long pixelGenTime = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

            ApplyTexture(pixels);

            long applyTime = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

            if (m_enablePerformanceLog)
            {
                double ToMs(long ticks) => (ticks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000.0;

                double getChunkManagerMs = ToMs(chunkManagerTime - startTime);
                double getPlaneInfoMs = ToMs(planeInfoTime - chunkManagerTime);
                double generatePixelsMs = ToMs(pixelGenTime - planeInfoTime);
                double applyTextureMs = ToMs(applyTime - pixelGenTime);

                Debug.Log($"  [Texture Breakdown] GetChunkManager: {getChunkManagerMs:F2}ms | GetPlaneInfo: {getPlaneInfoMs:F2}ms | GeneratePixels: {generatePixelsMs:F2}ms | ApplyTexture: {applyTextureMs:F2}ms");
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
        /// ボクセルデータピクセル配列を生成
        /// </summary>
        private Color[] GenerateVoxelDataPixels(ChunkManager chunkManager, PlaneTransformInfo planeInfo)
        {
            Color[] pixels = new Color[m_textureResolution * m_textureResolution];
            var chunkDict = chunkManager.Chunks;

            Chunk lastChunk = null;
            Vector3Int lastChunkPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            long uvCalcTime = 0;
            long voxelCheckTime = 0;
            int pixelCount = m_textureResolution * m_textureResolution;
            int voxelHitCount = 0;
            int chunkCacheHits = 0;
            int chunkCacheMisses = 0;

            for (int y = 0; y < m_textureResolution; y++)
            {
                for (int x = 0; x < m_textureResolution; x++)
                {
                    long uvStart = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

                    Vector3 worldPos = GetWorldPositionFromUV(x, y, planeInfo);

                    long voxelStart = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

                    bool hasSolidVoxel = CheckVoxelSolid(worldPos, chunkDict, ref lastChunk, ref lastChunkPos, out bool wasCacheHit);

                    long voxelEnd = m_enablePerformanceLog ? m_stopwatch.ElapsedTicks : 0;

                    if (m_enablePerformanceLog)
                    {
                        uvCalcTime += voxelStart - uvStart;
                        voxelCheckTime += voxelEnd - voxelStart;
                        if (hasSolidVoxel) voxelHitCount++;
                        if (wasCacheHit) chunkCacheHits++;
                        else chunkCacheMisses++;
                    }

                    int pixelIndex = y * m_textureResolution + x;
                    pixels[pixelIndex] = hasSolidVoxel ? Color.black : Color.white;
                }
            }

            if (m_enablePerformanceLog)
            {
                double ToMs(long ticks) => (ticks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000.0;

                double totalUvMs = ToMs(uvCalcTime);
                double totalVoxelMs = ToMs(voxelCheckTime);
                double avgUvUs = (totalUvMs * 1000.0) / pixelCount;
                double avgVoxelUs = (totalVoxelMs * 1000.0) / pixelCount;
                float cacheHitRate = chunkCacheHits / (float)(chunkCacheHits + chunkCacheMisses) * 100f;

                Debug.Log($"    [Pixel Generation] Total Pixels: {pixelCount} | Voxel Hits: {voxelHitCount} ({(voxelHitCount / (float)pixelCount * 100f):F1}%)");
                Debug.Log($"    [Pixel Generation] UV Calc: {totalUvMs:F2}ms (Avg: {avgUvUs:F3}µs/pixel) | Voxel Check: {totalVoxelMs:F2}ms (Avg: {avgVoxelUs:F3}µs/pixel)");
                Debug.Log($"    [Chunk Cache] Hits: {chunkCacheHits} | Misses: {chunkCacheMisses} | Hit Rate: {cacheHitRate:F1}%");
            }

            return pixels;
        }

        /// <summary>
        /// UV座標からワールド座標を計算
        /// </summary>
        private Vector3 GetWorldPositionFromUV(int x, int y, PlaneTransformInfo planeInfo)
        {
            float u = (float)x / (m_textureResolution - 1);
            float v = (float)y / (m_textureResolution - 1);

            float xOffset = (u - 0.5f) * planeInfo.Size;
            float yOffset = (v - 0.5f) * planeInfo.Size;

            return planeInfo.Center + planeInfo.Right * xOffset + planeInfo.Up * yOffset;
        }

        /// <summary>
        /// 指定ワールド座標にボクセルが存在するかチェック
        /// Chunkキャッシュを使用して高速化
        /// </summary>
        private bool CheckVoxelSolid(Vector3 worldPos, IReadOnlyDictionary<Vector3Int, Chunk> chunkDict,
                                      ref Chunk lastChunk, ref Vector3Int lastChunkPos, out bool wasCacheHit)
        {
            Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);

            Chunk chunk;
            if (chunkPos == lastChunkPos)
            {
                chunk = lastChunk;
                wasCacheHit = true;
            }
            else
            {
                chunkDict.TryGetValue(chunkPos, out chunk);
                lastChunk = chunk;
                lastChunkPos = chunkPos;
                wasCacheHit = false;
            }

            if (chunk == null) return false;

            Voxel voxel = chunk.GetVoxelFromWorldPosition(worldPos);
            return !voxel.IsEmpty;
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
    }
}

using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// PyramidStructure の単体テスト用スクリプト
    /// </summary>
    public class PyramidStructureTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [Tooltip("テストを実行する")]
        public bool runTest = false;

        [Header("ピラミッド設定")]
        [Tooltip("生成位置")]
        public Vector3 basePosition = new Vector3(0, 0, 0);

        [Tooltip("高さ（メートル）")]
        [Range(5f, 50f)]
        public float height = 20f;

        [Tooltip("底面の半径（メートル）")]
        [Range(2f, 20f)]
        public float baseRadius = 5f;

        [Tooltip("ボクセルID")]
        [Range(1, 10)]
        public byte voxelId = 3;

        [Tooltip("シード値")]
        public int seed = 12345;

        private VoxelOperationManager operationManager;

        private void Start()
        {
            // VoxelOperationManager を取得
            operationManager = FindObjectOfType<WorldManager>().Voxels;
            if (operationManager == null)
            {
                Debug.LogError("VoxelOperationManager が見つかりません");
            }
        }

        private void Update()
        {
            if (runTest)
            {
                runTest = false;
                _ = RunTestAsync();
            }
        }

        private async Task RunTestAsync()
        {
            if (operationManager == null)
            {
                Debug.LogError("VoxelOperationManager が見つかりません");
                return;
            }

            Debug.Log("=== ピラミッド単体テスト開始 ===");

            // ピラミッドを生成
            var pyramid = new PyramidStructure("test_pyramid", seed);
            var voxels = await pyramid.GenerateAsync(basePosition, height, baseRadius, voxelId);

            Debug.Log($"生成されたボクセル数: {voxels.Count}");

            // ワールドに配置
            operationManager.SetVoxels(voxels);

            Debug.Log("=== ピラミッド単体テスト完了 ===");
            Debug.Log($"位置: {basePosition}, 高さ: {height}m, 底面半径: {baseRadius}m");
            Debug.Log($"バウンディングボックス: {pyramid.GetBounds()}");
        }
    }
}

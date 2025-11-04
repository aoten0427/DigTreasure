//using UnityEngine;
//using System.Collections.Generic;

//namespace VoxelWorld
//{
//    /// <summary>
//    /// ボクセル変更操作の種類
//    /// </summary>
//    public enum VoxelChangeType
//    {
//        //ボクセル設定
//        Set,
//        //ボクセル破壊
//        Destroy
//    }

//    /// <summary>
//    /// ボクセル変更結果を表すデータ構造
//    /// サーバーからクライアントに送信される全ボクセル操作の結果
//    /// </summary>
//    [System.Serializable]
//    public class VoxelChangeResult
//    {
//        //変更操作の種類
//        public VoxelChangeType ChangeType;
        
//        //操作要求ID
//        public string RequestId;
        
//        //実際に適用されたボクセル変更リスト
//        public List<VoxelUpdate> AppliedChanges;
        
//        //影響を受けたチャンク座標リスト
//        public List<Vector3Int> AffectedChunks;
        
//        //変更結果作成時刻
//        public System.DateTime Timestamp;
        
//        /// <summary>
//        /// コンストラクタ
//        /// </summary>
//        public VoxelChangeResult()
//        {
//            AppliedChanges = new List<VoxelUpdate>();
//            AffectedChunks = new List<Vector3Int>();
//            Timestamp = System.DateTime.Now;
//        }
        
//        /// <summary>
//        /// 破壊専用コンストラクタ
//        /// </summary>
//        /// <param name="destructionResult">破壊結果</param>
//        public VoxelChangeResult(DestructionResult destructionResult)
//        {
//            ChangeType = VoxelChangeType.Destroy;
//            RequestId = destructionResult.RequestId;
//            AppliedChanges = new List<VoxelUpdate>();
            
//            // 破壊結果を VoxelUpdate に変換
//            foreach (var position in destructionResult.DestroyedPositions)
//            {
//                AppliedChanges.Add(new VoxelUpdate 
//                { 
//                    WorldPosition = position, 
//                    VoxelID = VoxelID.Empty 
//                });
//            }
            
//            AffectedChunks = destructionResult.AffectedChunks;
//            Timestamp = destructionResult.Timestamp;
//        }
        
//    }
//}
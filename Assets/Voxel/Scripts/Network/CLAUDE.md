# ボクセルゲーム ネットワーク実装メモ

## 破壊フローのネットワーク同期設計

### 現在の破壊フロー
```
1. プレイヤーが破壊実行
   ↓
2. VoxelDestructionManager がキューに追加
   ↓
3. VoxelOperationManager が実際の破壊処理
   ↓
4. SeparationDetector が分離検出（Flood Fill）
   ↓
5. 分離グループから SeparatedVoxelObject を作成
   ↓
6. SeparationManager に登録・物理挙動開始
```

### ネットワーク同期方針

**採用アプローチ: Client-Side Prediction + Server Reconciliation**
- 破壊を実行したクライアント（StateAuthority）がローカルで全て計算
- 結果（ボクセル変更と分離オブジェクト情報）のみをネットワーク送信
- 他のクライアントは受信した結果を適用

#### 同期ポイント
- **ステップ3**: チャンクボクセルの変更をRPC送信（詳細は後で決定）
- **ステップ5**: 分離オブジェクトの生成情報をRPC送信（以下に詳細）

---

## 分離オブジェクトの同期実装（TODO）

### 問題点と対策

#### 1. 決定論的な実行の保証

**問題**: HashSetの列挙順序は非決定的 → 異なるクライアントで異なる分離結果になる可能性

**解決策**: 影響を受けたボクセルをソートする

```csharp
// SeparationDetector.cs の DetectSeparations メソッド内（480行目付近）
var affectedVoxels = GetAffectedVoxels(destroyedPositions, voxelProvider);

// ✅ 決定論的にソート
affectedVoxels.Sort((a, b) =>
{
    if (a.x != b.x) return a.x.CompareTo(b.x);
    if (a.y != b.y) return a.y.CompareTo(b.y);
    return a.z.CompareTo(b.z);
});
```

#### 2. 分離オブジェクトの物理演算同期

**問題**: ネットワーク遅延により、クライアント間で生成タイミングがずれる → 物理オブジェクトの位置がずれる

**解決策**: NetworkRigidbodyを使用

```csharp
// SeparatedVoxelObject.cs
using Fusion;

[RequireComponent(typeof(NetworkRigidbody))]
public class SeparatedVoxelObject : NetworkBehaviour
{
    private NetworkRigidbody m_networkRigidbody;

    public void Initialize(Voxel[,,] voxelData, Vector3Int size, Vector3 worldPosition, string objectId = null)
    {
        // 既存の初期化処理
        // ...

        // ✅ NetworkRigidbodyの取得
        m_networkRigidbody = GetComponent<NetworkRigidbody>();
        // Fusionが物理演算を自動同期
    }
}
```

#### 3. 分離オブジェクトの生成同期

**オプションA: NetworkObject として Spawn（推奨）**

```csharp
// VoxelOperationManager.cs の CreateSeparatedObjects 内
private List<SeparatedVoxelObject> CreateSeparatedObjects(List<List<Vector3>> separatedGroups)
{
    var separatedObjects = new List<SeparatedVoxelObject>();

    for (int i = 0; i < separatedGroups.Count; i++)
    {
        var group = separatedGroups[i];
        string objectId = System.Guid.NewGuid().ToString("N").Substring(0, 8);

        // ✅ NetworkObjectとしてSpawn
        var prefab = Resources.Load<GameObject>("SeparatedVoxelObjectPrefab");
        var networkObject = Runner.Spawn(
            prefab,
            position,
            Quaternion.identity,
            Object.InputAuthority
        );

        var separatedObject = networkObject.GetComponent<SeparatedVoxelObject>();

        // 初期化データを送信
        RPC_InitializeSeparatedObject(
            networkObject.Id,
            voxelData,
            size,
            position,
            objectId
        );

        separatedObjects.Add(separatedObject);
    }

    return separatedObjects;
}

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_InitializeSeparatedObject(
    NetworkId objectId,
    byte[] compressedVoxelData,
    Vector3Int size,
    Vector3 position,
    string separatedObjectId)
{
    var networkObject = Runner.FindObject(objectId);
    if (networkObject == null) return;

    var separatedObject = networkObject.GetComponent<SeparatedVoxelObject>();
    var voxelData = DecompressVoxelData(compressedVoxelData, size);

    separatedObject.Initialize(voxelData, size, position, separatedObjectId);
}
```

**オプションB: データのみ送信（軽量だが同期が複雑）**

```csharp
// VoxelNetworkData.cs
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_CreateSeparatedObject(
    byte[] compressedVoxelData,
    Vector3Int size,
    Vector3 position,
    string objectId,
    Vector3 initialVelocity,
    Vector3 initialAngularVelocity)
{
    // 各クライアントでローカル生成
    var gameObject = new GameObject($"SepObj_{objectId}");
    var separatedObject = gameObject.AddComponent<SeparatedVoxelObject>();

    var voxelData = DecompressVoxelData(compressedVoxelData, size);
    separatedObject.Initialize(voxelData, size, position, objectId);

    // 初期速度を設定（物理演算の初期条件を揃える）
    var rb = separatedObject.GetComponent<Rigidbody>();
    rb.velocity = initialVelocity;
    rb.angularVelocity = initialAngularVelocity;
}
```

### 推奨実装手順

1. **SeparatedVoxelObjectをNetworkObject化**
   - Prefabを作成（NetworkObject, NetworkRigidbody, SeparatedVoxelObjectコンポーネント付き）
   - FusionのNetworkPrefabリストに登録

2. **VoxelOperationManagerに同期処理を追加**
   ```csharp
   // VoxelOperationManager.cs
   private VoxelNetworkData m_networkManager;

   public void SetNetworkManager(VoxelNetworkData networkManager)
   {
       m_networkManager = networkManager;
   }

   // DestroyVoxelsWithPower の最後で呼び出し
   if (destroyedCount > 0 && m_networkManager != null)
   {
       // 分離オブジェクト生成情報を送信
       m_networkManager.SyncSeparatedObjects(generatedSeparatedObjects);
   }
   ```

3. **SeparationDetectorに決定論的ソートを追加**
   - 上記「問題点1」の解決策を実装

4. **VoxelNetworkDataに分離オブジェクト同期RPCを追加**
   - オプションAまたはBを選択して実装

---

## Photon Fusion 参考情報

### ネットワーク特性
- **TickRate**: 60 Hz (16.67ms/tick)
- **RPC最大サイズ**: 約1KB
- **同一フレーム内のRPC**: 同じTickでまとめて送信される
- **予測ラグ**: 50～120ms（TickRate遅延 + ping + 処理時間）

### VoxelUpdate構造体のサイズ
```csharp
public struct VoxelUpdate
{
    public Vector3 WorldPosition;  // 12バイト
    public int VoxelID;            // 4バイト
}
// 合計: 16バイト/個
// → 1回のRPCで約40個送信可能（実用値）
```

---

## 関連ファイル

- `VoxelOperationManager.cs` - ボクセル操作と分離検出
- `SeparationDetector.cs` - Flood Fillアルゴリズム
- `SeparationManager.cs` - 分離オブジェクト管理
- `SeparatedVoxelObject.cs` - 分離オブジェクト本体
- `VoxelDestructionManager.cs` - 破壊キュー管理
- `VoxelNetworkData.cs` - ネットワーク同期（実装予定）
- `VoxelNetWorkManager.cs` - ネットワーク管理（基本のみ）

---

**最終更新**: 2025-10-11

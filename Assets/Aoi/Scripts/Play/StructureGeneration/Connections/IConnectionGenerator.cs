using System.Collections.Generic;
using System.Threading.Tasks;
using VoxelWorld;


public enum ConnectionType
{
    OpenTunnel,    // 空洞のトンネル
    FilledTunnel   // 周りが硬く中が柔らかい埋まった道
}

/// <summary>
/// 経路生成モード
/// </summary>
public enum PathGenerationMode
{
    Straight,  // 直線
    Bezier     // ベジェ曲線（滑らかな曲線）
}

namespace StructureGeneration
{
    /// <summary>
    /// 接続ジェネレーターのインターフェース
    /// </summary>
    public interface IConnectionGenerator
    {
        /// <summary>
        /// 接続のボクセルデータを生成
        /// </summary>
        /// <param name="connection">接続データ</param>
        /// <param name="seed">シード値</param>
        /// <returns>ボクセル更新リスト</returns>
        Task<List<VoxelUpdate>> GenerateAsync(ConnectionData connection, int seed);

        /// <summary>
        /// このジェネレーターがサポートする接続タイプ
        /// </summary>
        ConnectionType SupportedType { get; }
    }
}

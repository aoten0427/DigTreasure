namespace StructureGeneration
{
    /// <summary>
    /// 接続の種類
    /// </summary>
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
}

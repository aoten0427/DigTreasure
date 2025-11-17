using System.Collections.Generic;
using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// トンネル経路生成の共通ロジック
    /// OpenTunnelGeneratorとFilledTunnelGeneratorで共有
    /// </summary>
    public static class PathGenerator
    {
        // ベジェ曲線設定の定数
        private const float BEZIER_CONTROL_POINT_OFFSET_RATIO = 0.25f;
        private const float STEEP_SLOPE_THRESHOLD = 15f;
        private const float STEEP_SLOPE_HEIGHT_MULTIPLIER = 0.3f;
        private const float SEGMENT_DISTANCE = 1f;

        /// <summary>
        /// パスポイントを生成（直線またはベジェ曲線）
        /// </summary>
        /// <param name="start">開始位置</param>
        /// <param name="end">終了位置</param>
        /// <param name="sourceDirection">開始点の方向</param>
        /// <param name="targetDirection">終了点の方向</param>
        /// <param name="pathMode">経路生成モード</param>
        /// <param name="curveHeight">曲線の高さ</param>
        /// <returns>経路上のポイントリスト</returns>
        public static List<Vector3> GeneratePathPoints(
            Vector3 start,
            Vector3 end,
            Vector3 sourceDirection,
            Vector3 targetDirection,
            PathGenerationMode pathMode,
            float curveHeight)
        {
            var pathPoints = new List<Vector3>();
            float distance = Vector3.Distance(start, end);

            if (pathMode == PathGenerationMode.Straight)
            {
                // 直線パス
                int segments = Mathf.CeilToInt(distance / SEGMENT_DISTANCE);
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    pathPoints.Add(Vector3.Lerp(start, end, t));
                }
            }
            else // PathGenerationMode.Bezier
            {
                // ベジェ曲線パス
                Vector3 control1, control2;
                CalculateBezierControlPoints(
                    start, end,
                    sourceDirection, targetDirection,
                    curveHeight,
                    out control1, out control2);

                // ベジェ曲線に沿ってポイントを生成
                int segments = Mathf.CeilToInt(distance / SEGMENT_DISTANCE);
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    pathPoints.Add(CalculateCubicBezierPoint(t, start, control1, control2, end));
                }
            }

            return pathPoints;
        }

        /// <summary>
        /// ベジェ曲線の制御点を計算
        /// </summary>
        /// <param name="start">開始位置</param>
        /// <param name="end">終了位置</param>
        /// <param name="sourceDirection">開始点の方向</param>
        /// <param name="targetDirection">終了点の方向</param>
        /// <param name="curveHeight">曲線の高さ</param>
        /// <param name="control1">制御点1（出力）</param>
        /// <param name="control2">制御点2（出力）</param>
        private static void CalculateBezierControlPoints(
            Vector3 start,
            Vector3 end,
            Vector3 sourceDirection,
            Vector3 targetDirection,
            float curveHeight,
            out Vector3 control1,
            out Vector3 control2)
        {
            // 2つの構造物の位置関係を分析
            float horizontalDist = new Vector3(end.x - start.x, 0, end.z - start.z).magnitude;
            float verticalDiff = Mathf.Abs(end.y - start.y);

            // 曲線の高さを決定（Y軸の差が大きい場合は上方向に迂回）
            float actualCurveHeight = curveHeight;
            if (verticalDiff > STEEP_SLOPE_THRESHOLD)
            {
                // 大きな高度差がある場合は、より高く迂回
                actualCurveHeight = curveHeight + verticalDiff * STEEP_SLOPE_HEIGHT_MULTIPLIER;
            }

            // 制御点1: 開始点から少し進んで上方向にオフセット
            control1 = start + sourceDirection * (horizontalDist * BEZIER_CONTROL_POINT_OFFSET_RATIO)
                       + Vector3.up * actualCurveHeight;

            // 制御点2: 終了点から少し戻って上方向にオフセット
            control2 = end - targetDirection * (horizontalDist * BEZIER_CONTROL_POINT_OFFSET_RATIO)
                       + Vector3.up * actualCurveHeight;
        }

        /// <summary>
        /// 3次ベジェ曲線上の点を計算
        /// </summary>
        /// <param name="t">パラメータ（0.0～1.0）</param>
        /// <param name="p0">開始点</param>
        /// <param name="p1">制御点1</param>
        /// <param name="p2">制御点2</param>
        /// <param name="p3">終了点</param>
        /// <returns>ベジェ曲線上の点</returns>
        private static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;      // (1-t)^3 * P0
            p += 3 * uu * t * p1;      // 3(1-t)^2 * t * P1
            p += 3 * u * tt * p2;      // 3(1-t) * t^2 * P2
            p += ttt * p3;             // t^3 * P3

            return p;
        }
    }
}

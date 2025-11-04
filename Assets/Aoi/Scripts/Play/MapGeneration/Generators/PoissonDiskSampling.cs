using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Poisson Disk Samplingアルゴリズムによる2D点配置
    /// 参考: https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
    /// </summary>
    public class PoissonDiskSampling
    {
        /// <summary>
        /// 指定範囲内にPoisson Disk Samplingで点を生成
        /// </summary>
        /// <param name="minBounds">範囲の最小値（X, Z）</param>
        /// <param name="maxBounds">範囲の最大値（X, Z）</param>
        /// <param name="minDistance">点同士の最小距離</param>
        /// <param name="maxAttempts">各点の周辺探索の最大試行回数</param>
        /// <param name="seed">ランダムシード</param>
        /// <returns>生成された点のリスト（Vector2 = X, Z座標）</returns>
        public static List<Vector2> GeneratePoints(
            Vector2 minBounds,
            Vector2 maxBounds,
            float minDistance,
            int maxAttempts = 30,
            int seed = 0)
        {
            Random.InitState(seed);

            // グリッドセルのサイズ（最小距離 / √2）
            float cellSize = minDistance / Mathf.Sqrt(2f);

            // グリッドの大きさを計算
            Vector2 bounds = maxBounds - minBounds;
            int gridWidth = Mathf.CeilToInt(bounds.x / cellSize);
            int gridHeight = Mathf.CeilToInt(bounds.y / cellSize);

            // グリッド（各セルに点のインデックスを格納、-1は空）
            int[,] grid = new int[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = -1;
                }
            }

            // 結果リスト
            List<Vector2> points = new List<Vector2>();

            // アクティブリスト（周辺に新しい点を生成する候補）
            List<Vector2> activeList = new List<Vector2>();

            // 最初の点をランダムに配置
            Vector2 firstPoint = new Vector2(
                Random.Range(minBounds.x, maxBounds.x),
                Random.Range(minBounds.y, maxBounds.y)
            );

            points.Add(firstPoint);
            activeList.Add(firstPoint);

            Vector2Int firstGridPos = GetGridPosition(firstPoint, minBounds, cellSize);
            grid[firstGridPos.x, firstGridPos.y] = 0;

            // アクティブリストが空になるまで処理
            while (activeList.Count > 0)
            {
                // ランダムにアクティブな点を選択
                int randomIndex = Random.Range(0, activeList.Count);
                Vector2 currentPoint = activeList[randomIndex];

                bool foundValidPoint = false;

                // 最大試行回数まで新しい点を探す
                for (int i = 0; i < maxAttempts; i++)
                {
                    // 現在の点の周辺（minDistance ~ 2*minDistance）にランダムな点を生成
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(minDistance, minDistance * 2f);

                    Vector2 newPoint = new Vector2(
                        currentPoint.x + Mathf.Cos(angle) * radius,
                        currentPoint.y + Mathf.Sin(angle) * radius
                    );

                    // 範囲チェック
                    if (newPoint.x < minBounds.x || newPoint.x >= maxBounds.x ||
                        newPoint.y < minBounds.y || newPoint.y >= maxBounds.y)
                    {
                        continue;
                    }

                    // グリッド位置を取得
                    Vector2Int gridPos = GetGridPosition(newPoint, minBounds, cellSize);

                    // 周辺グリッドに点が存在しないかチェック
                    if (IsValidPoint(newPoint, gridPos, points, grid, gridWidth, gridHeight, minDistance, cellSize))
                    {
                        // 有効な点として追加
                        int pointIndex = points.Count;
                        points.Add(newPoint);
                        activeList.Add(newPoint);
                        grid[gridPos.x, gridPos.y] = pointIndex;

                        foundValidPoint = true;
                        break;
                    }
                }

                // 有効な点が見つからなかった場合、この点をアクティブリストから削除
                if (!foundValidPoint)
                {
                    activeList.RemoveAt(randomIndex);
                }
            }

            return points;
        }

        /// <summary>
        /// ワールド座標をグリッド座標に変換
        /// </summary>
        private static Vector2Int GetGridPosition(Vector2 point, Vector2 minBounds, float cellSize)
        {
            int x = Mathf.FloorToInt((point.x - minBounds.x) / cellSize);
            int y = Mathf.FloorToInt((point.y - minBounds.y) / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// 点が有効かどうか（周辺に他の点がないか）をチェック
        /// </summary>
        private static bool IsValidPoint(
            Vector2 point,
            Vector2Int gridPos,
            List<Vector2> points,
            int[,] grid,
            int gridWidth,
            int gridHeight,
            float minDistance,
            float cellSize)
        {
            // 周辺2セル分をチェック
            int searchRadius = 2;
            float minDistSq = minDistance * minDistance;

            for (int x = Mathf.Max(0, gridPos.x - searchRadius); x <= Mathf.Min(gridWidth - 1, gridPos.x + searchRadius); x++)
            {
                for (int y = Mathf.Max(0, gridPos.y - searchRadius); y <= Mathf.Min(gridHeight - 1, gridPos.y + searchRadius); y++)
                {
                    int pointIndex = grid[x, y];
                    if (pointIndex != -1)
                    {
                        // 距離をチェック
                        float distSq = (point - points[pointIndex]).sqrMagnitude;
                        if (distSq < minDistSq)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 生成された点から指定数をランダムに選択
        /// </summary>
        public static List<Vector2> SelectRandomPoints(List<Vector2> allPoints, int count, int seed)
        {
            Random.InitState(seed);

            if (count >= allPoints.Count)
            {
                return new List<Vector2>(allPoints);
            }

            // シャッフルして先頭からcount個を取得
            List<Vector2> shuffled = new List<Vector2>(allPoints);

            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2 temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            return shuffled.GetRange(0, count);
        }
    }
}

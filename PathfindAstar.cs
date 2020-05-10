using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class PathfindAstar : TileMap {
    private const float BASE_LINE_WIDTH = 3;
    private readonly Color DRAW_COLOR = Colors.White;

    [Export] private Vector2 mapSize = new Vector2(16, 16);

    private Vector2 pathStartPosition;
    private Vector2 pathEndPosition;

    private List<Vector3> pointPath;

    private AStar astarNode = new AStar();
    private List<Vector2> obstacles;
    private Vector2 halfCellSize;

    public Vector2 PathStartPosition {
        get => pathStartPosition;
        set {
            if (obstacles.Contains(value)) return;
            if (IsOutsideMapBounds(value)) return;

            SetCell((int) pathStartPosition.x, (int) pathStartPosition.y, -1);
            SetCell((int) value.x, (int) value.y, 1);
            pathStartPosition = value;
            if (pathEndPosition != pathStartPosition) {
                RecalculatePath();
            }
        }
    }

    public Vector2 PathEndPosition {
        get => pathEndPosition;
        set {
            if (obstacles.Contains(value)) return;
            if (IsOutsideMapBounds(value)) return;

            SetCell((int) pathStartPosition.x, (int) pathStartPosition.y, -1);
            SetCell((int) value.x, (int) value.y, 2);
            pathEndPosition = value;
            if (pathStartPosition != value) {
                RecalculatePath();
            }
        }
    }

    public override void _Ready() {
        obstacles = new List<Vector2>(new Array<Vector2>(GetUsedCells()));
        halfCellSize = CellSize / 2F;

        var walkableCellsList = AStarAddWalkableCells(obstacles);
        AStarConnectWalkableCells(walkableCellsList);
    }

    public override void _Draw() {
        if (pointPath == null || pointPath.Count < 1) return;
        var pointStart = pointPath[0];
        var pointEnd = pointPath[pointPath.Count - 1];

        SetCell((int) pointStart.x, (int) pointStart.y, 1);
        SetCell((int) pointEnd.x, (int) pointEnd.y, 2);

        var lastPoint = MapToWorld(new Vector2(pointStart.x, pointStart.y)) + halfCellSize;
        for (var i = 1; i < pointPath.Count; i++) {
            var point = pointPath[i];
            var currentPoint = MapToWorld(new Vector2(point.x, point.y)) + halfCellSize;
            DrawLine(lastPoint, currentPoint, DRAW_COLOR, BASE_LINE_WIDTH, true);
            DrawCircle(currentPoint, BASE_LINE_WIDTH * 2.0F, DRAW_COLOR);
            lastPoint = currentPoint;
        }
    }

    private List<Vector2> AStarAddWalkableCells(ICollection<Vector2> obstacleList = null) {
        var pointsList = new List<Vector2>();
        for (var y = 0; y < mapSize.y; y++) {
            for (var x = 0; x < mapSize.x; x++) {
                var point = new Vector2(x, y);
                if (obstacleList != null && obstacleList.Contains(point)) continue;
                pointsList.Add(point);

                // The AStar class references points with indices.
                // Using a function to calculate the index from a point's coordinates
                // ensures we always get the same index with the same input point.
                var pointIndex = CalculatePointIndex(point);

                // AStar works for both 2d and 3d, so we have to convert the point
                // coordinates from and to Vector3s.
                astarNode.AddPoint(pointIndex, new Vector3(point.x, point.y, 0));
            }
        }

        return pointsList;
    }

    private void AStarConnectWalkableCells(IEnumerable<Vector2> walkableCellsList) {
        foreach (var point in walkableCellsList) {
            var pointIndex = CalculatePointIndex(point);
            var pointsRelative = new[] {
                point + Vector2.Right,
                point + Vector2.Left,
                point + Vector2.Down,
                point + Vector2.Up
            };
            foreach (var pointRelative in pointsRelative) {
                var pointRelativeIndex = CalculatePointIndex(pointRelative);
                if (IsOutsideMapBounds(pointRelative)) continue;
                if (!astarNode.HasPoint(pointRelativeIndex)) continue;
                // Note the 3rd argument. It tells the astarNode that we want the
                // connection to be bilateral: from point A to B and B to A.
                // If you set this value to false, it becomes a one-way path.
                // As we loop through all points we can set it to false.
                astarNode.ConnectPoints(pointIndex, pointRelativeIndex, bidirectional: false);
            }
        }
    }

    // This is a variation of the method above.
    // It connects cells horizontally, vertically AND diagonally.
    private void AStarConnectWalkableCellsDiagonal(IEnumerable<Vector2> points) {
        foreach (var point in points) {
            var pointIndex = CalculatePointIndex(point);
            for (var localY = -1; localY <= 1; localY++) {
                for (var localX = -1; localX <= 1; localX++) {
                    var pointRelative = new Vector2(point.x + localX, point.y + localY);
                    var pointRelativeIndex = CalculatePointIndex(pointRelative);
                    if (pointRelative == point || IsOutsideMapBounds(pointRelative)) continue;
                    if (!astarNode.HasPoint(pointRelativeIndex)) continue;
                    astarNode.ConnectPoints(pointIndex, pointRelativeIndex, bidirectional: true);
                }
            }
        }
    }

    private int CalculatePointIndex(Vector2 point) {
        return (int) (point.x + mapSize.x * point.y);
    }

    private void ClearPreviousPathDrawing() {
        if (pointPath == null || pointPath.Count == 0) return;
        var pointStart = pointPath[0];
        var pointEnd = pointPath[pointPath.Count - 1];
        SetCell((int) pointStart.x, (int) pointStart.y, -1);
        SetCell((int) pointEnd.x, (int) pointEnd.y, -1);
    }

    private bool IsOutsideMapBounds(Vector2 point) => point.x < 0 || point.y < 0 || point.x >= mapSize.x || point.y >= mapSize.y;

    public List<Vector2> GetAStarPath(Vector2 worldStart, Vector2 worldEnd) {
        PathStartPosition = WorldToMap(worldStart);
        PathEndPosition = WorldToMap(worldEnd);
        RecalculatePath();
        var pathWorld = new List<Vector2>();
        foreach (var point in pointPath) {
            var pointWorld = MapToWorld(new Vector2(point.x, point.y)) + halfCellSize;
            pathWorld.Add(pointWorld);
        }

        return pathWorld;
    }

    private void RecalculatePath() {
        ClearPreviousPathDrawing();
        var startPointIndex = CalculatePointIndex(PathStartPosition);
        var endPointIndex = CalculatePointIndex(PathEndPosition);

        pointPath = new List<Vector3>(astarNode.GetPointPath(startPointIndex, endPointIndex));
        Update();
    }
}
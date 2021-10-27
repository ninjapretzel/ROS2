using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class AStar {
	public class Scores : Dictionary<Vector2Int, float> {
		public new float this[Vector2Int key] {
			get {
				if (!ContainsKey(key)) { return float.PositiveInfinity; }
				return ((Dictionary<Vector2Int, float>)this)[key];
			}
			set {
				((Dictionary<Vector2Int, float>)this)[key] = value;
			}
		}
	}

	public static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current) {
		List<Vector2Int> path = new List<Vector2Int>();
		path.Add(current);

		while (cameFrom.ContainsKey(current)) {
			current = cameFrom[current];
			path.Add(current);
		}

		path.Reverse();
		return path;
	}

	public static List<Vector2Int> Pathfind(Vector2Int start, Vector2Int goal, OccupancyGrid grid) {
		if (grid[start] >= 100) { return null; }
		Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
		ConcurrentSet<Vector2Int> openSet = new ConcurrentSet<Vector2Int>();
		openSet.Add(start);

		// Heuristic function
		Scores gScore = new Scores();
		gScore[start] = 0;
		Scores fScore = new Scores();
		fScore[start] = h(start);
		float d(Vector2Int from, Vector2Int to) {
			if (from == to) { return 0; }
			if (grid[to] >= 100) { return float.PositiveInfinity; }
			Vector2Int diff = from - to;
			return Mathf.Abs(diff.x) + Mathf.Abs(diff.y);
		}
		float h(Vector2Int point) {
			//if (fScore.ContainsKey(point)) { return fScore[point]; }
			//if (gScore.ContainsKey(point)) { return gScore[point]; }
			// return d(start, point);
			if (fScore.ContainsKey(point)) { return fScore[point] + d(point, goal); }
			// if (gScore.ContainsKey(point)) { return gScore[point] + d(point, goal); }
			return d(point, goal);
		}
		Vector2Int min() {
			Vector2Int? m = null;
			foreach (var p in openSet) {
				if (!m.HasValue) { m = p; }
				if (fScore[p]< fScore[m.Value]) { 
					m = p;
				}
			}
			if (!m.HasValue) { throw new Exception("No points, no min value!");}
			return m.Value;
		}

		int safetyWall = 20000;
		while (!openSet.IsEmpty) {
			var current = min();
			if (current == goal) { return ReconstructPath(cameFrom, current); }

			openSet.Remove(current);
			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0) { continue; }
					int nx = current.x + x; int ny = current.y + y;
					if (nx >= 0 && nx < grid.width && ny >= 0 && ny < grid.height) {

						var neighbor = new Vector2Int(nx, ny);
						if (grid[neighbor] >= 100) { continue; }
						var tgScore = gScore[current] + d(current, neighbor);
						
						if (tgScore < gScore[neighbor]) {
							cameFrom[neighbor] = current;
							gScore[neighbor] = tgScore;
							float hn = h(neighbor);
							if (float.IsPositiveInfinity(hn)) { continue; }
							fScore[neighbor] = tgScore + hn;
							
							if (!openSet.Contains(neighbor)) {
								openSet.Add(neighbor);
							}
						}
					}
				}
			}

			if (safetyWall-- < 0) { Debug.LogWarning("AStar: Exited pathfinding with safetywall..."); break; }
		}
		


		return null;
	}
	
	
}

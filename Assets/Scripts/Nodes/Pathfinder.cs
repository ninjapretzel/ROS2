﻿using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Path { 
	public List<Vector3> points;
	public Color? color = null;
	public Path(List<Vector3> points) { this.points = points; }
	public void Visualize(float alpha) {
		Color c = color ?? Color.green;
		c.a *= alpha;
		Gizmos.color = c;
		for (int i = 1; i< points.Count; i++) {
			Gizmos.DrawLine(points[i-1], points[i]);
		}
	}
}
public class Pathfinder : MonoBehaviour {

	public string gridChannel = "undefined";
	public string targetChannel = "moveTargets";
	public string pathChannel = "path";

	ISub<OccupancyGrid> gridSub;
	ISub<Vector3> targetSub;
	IPub<Path> pathPub;

	public OccupancyGrid grid;
	public Vector3? target = null;

	void OnEnable() {
		gridSub = MessageBus<OccupancyGrid>.SubscribeTo(gridChannel, OnGrid);
		targetSub = MessageBus<Vector3>.SubscribeTo(targetChannel, OnTarget);
		pathPub = MessageBus<Path>.PublishTo(pathChannel);
	}
	void OnDisable() {
		gridSub.Unsubscribe();
		targetSub.Unsubscribe();
		grid = null;
		target = null;
	}
	
	void OnGrid(OccupancyGrid grid) {
		this.grid = grid;
		if (target != null) { UpdatePath(); }
	}

	void OnTarget(Vector3 target) {
		this.target = target;
		if (grid != null) { UpdatePath(); }
	}

	void UpdatePath() {
		
		if (target != null && grid != null) {
			Vector2Int st = grid.WorldXZToGrid(transform.position);
			Vector2Int tg = grid.WorldXZToGrid(target.Value);

			var result = AStar.Pathfind(st, tg, grid);
			
			if (result != null) {
				var points = result.Select(it => grid.GridToWorldXZ(it)).ToList();
				var path = new Path(points);
				pathPub.Publish(path);
			}
		}

	}


	



	
}

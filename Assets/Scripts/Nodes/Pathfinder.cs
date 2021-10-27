using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour {

	public string gridChannel = "undefined";
	public string targetChannel = "moveTargets";
	public string pathChannel = "path";

	ISub<OccupancyGrid> gridSub;
	ISub<Vector3> targetSub;
	IPub<List<Vector3>> pathPub;

	public OccupancyGrid grid;
	public Vector3? target = null;

	void OnEnable() {
		gridSub = MessageBus<OccupancyGrid>.SubscribeTo(gridChannel, OnGrid);
		targetSub = MessageBus<Vector3>.SubscribeTo(targetChannel, OnTarget);
		pathPub = MessageBus<List<Vector3>>.PublishTo(pathChannel);
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
				pathPub.Publish(result.Select(it=>grid.GridToWorldXZ(it)).ToList());
			}
		}

	}


	



	
}

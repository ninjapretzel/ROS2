using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Marker {
	public Vector3 point;
	public Vector3 size;
	public Color color;
	public int kind;
	public const int SPHERE = 0;
	public const int CUBE = 1;

	public void DrawGizmo() {
		Gizmos.color = color;
		if (kind == SPHERE) {
			Gizmos.DrawSphere(point, size.magnitude);
		}
		if (kind == CUBE) {
			Gizmos.DrawCube(point, size);
		}
	}

}

public class FrontierClusterer : Node {
	public string inputChannel = "frontiers";
	public string outputChannel = "frontiers";
	public Marker[] markers;
	public bool visualize = false;
	IPub<Marker[]> pub;
	ISub<OccupancyGrid> sub;
	void Awake() {
		pub = MessageBus<Marker[]>.PublishTo(outputChannel);
	}
	void OnEnable() {
		sub = MessageBus<OccupancyGrid>.SubscribeTo(inputChannel, Handler);
	}
	void OnDisable() {
		sub.Unsubscribe();
	}
	void OnDrawGizmos() {
		if (visualize && markers != null && markers.Length > 0) {
			foreach(var marker in markers) { marker.DrawGizmo(); }
		}	
	}

	void Handler(OccupancyGrid input) {
		List<Vector4> points = new List<Vector4>();
		for (int i = 0; i < input.size; i++) {
			if (input.data[i] == 100) {
				var xz = input.IndexToGrid(i);
				var pt = input.GridToWorld(xz.x, 0, xz.y);
				points.Add(pt);
			}
		}
		int nClusters = 4;
		int[] clusters = KMeans.Cluster(points.ToArray(), nClusters);
		List<Vector4>[] cs = new List<Vector4>[nClusters];
		for (int i = 0; i < cs.Length; i++) { cs[i] = new List<Vector4>(); }
		for (int i = 0; i < clusters.Length; i++) { cs[clusters[i]].Add(points[i]); }

		markers = new Marker[nClusters];
		for (int i = 0; i < cs.Length; i++) {
			Vector4 avg = Vector4.zero;
			for (int k = 0; k < cs[i].Count; k++) {
				avg += cs[i][k];
			}
			avg /= cs[i].Count;

			markers[i] = new Marker() { point=avg, size=Vector3.one, color=Color.magenta, kind = Marker.SPHERE };
			
		}
		pub.Publish(markers);
	}

	
}

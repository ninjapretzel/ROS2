﻿using UnityEngine;
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

	public void Visualize(float alpha) {
		Color c = color;
		c.a *= alpha;
		Gizmos.color = c;
		if (kind == SPHERE) { Gizmos.DrawSphere(point, size.magnitude); }
		if (kind == CUBE) { Gizmos.DrawCube(point, size); }
	}
}

public class Markers : IEnumerable<Marker> {
	public Markers(params Marker[] markers) {
		this.markers = markers;
	}
	public int Length { get { return markers.Length; } }
	public Marker this[int i] {
		get { return markers[i]; }
		set { markers[i] = value; }
	}
	private Marker[] markers;
	public void Visualize(float alpha) {
		foreach (var marker in markers) {
			marker.Visualize(alpha);
		}
	}

	public IEnumerator<Marker> GetEnumerator() { return (IEnumerator<Marker>)markers.GetEnumerator(); }
	IEnumerator IEnumerable.GetEnumerator() { return markers.GetEnumerator(); }
}

public class FrontierClusterer : Node {
	public string inputChannel = "frontiers";
	public string markerChannel = "frontier_markers";
	public string targetChannel = "moveTargets";

	public int nClusters = 4;
	public Markers markers;
	public bool visualize = false;
	IPub<Markers> markerPub;
	IPub<Vector3> targetPub;
	ISub<OccupancyGrid> sub;
	void Awake() {
		markerPub = MessageBus<Markers>.PublishTo(markerChannel);
		targetPub = MessageBus<Vector3>.PublishTo(targetChannel);
	}
	void OnEnable() {
		sub = MessageBus<OccupancyGrid>.SubscribeTo(inputChannel, Handler);
	}
	void OnDisable() {
		sub.Unsubscribe();
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
		if (points.Count < nClusters) { return; }
		int[] clusters = KMeans.Cluster(points.ToArray(), nClusters);
		List<Vector4>[] cs = new List<Vector4>[nClusters];
		for (int i = 0; i < cs.Length; i++) { cs[i] = new List<Vector4>(); }
		for (int i = 0; i < clusters.Length; i++) { cs[clusters[i]].Add(points[i]); }

		var mks = new Marker[nClusters];
		markers = new Markers(mks);
		for (int i = 0; i < cs.Length; i++) {
			Vector4 avg = Vector4.zero;
			for (int k = 0; k < cs[i].Count; k++) {
				avg += cs[i][k];
			}
			avg /= cs[i].Count;

			markers[i] = new Marker() { point=avg, size=Vector3.one, color=Color.magenta, kind = Marker.SPHERE };
			
		}
		markerPub.Publish(markers);
		targetPub.Publish(markers[0].point);
	}

	
}

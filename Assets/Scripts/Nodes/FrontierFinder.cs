using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FrontierFinder : Node {

	public string inputChannel = "undefined"; // would be "/map" in ros
	public string outputChannel = "frontiers"; // would be "/frontier_map" in ros
	public string grownChannel = "grown"; 

	public int growthFactor = 2;
	
	[Range(0,1)] public float visualizationAlpha = .5f;
	public bool visualizeGrown = false;
	public bool visualizeFronts = false;

	ISub<OccupancyGrid> sub;
	IPub<OccupancyGrid> frontierPub;
	IPub<OccupancyGrid> grownPub;
	public Color grownColor = Color.green;
	public Color frontiersColor = Color.red;
	public OccupancyGrid grown;
	public OccupancyGrid frontiers;
	
	void Awake() {
		frontierPub = MessageBus<OccupancyGrid>.PublishTo(outputChannel);
		grownPub = MessageBus<OccupancyGrid>.PublishTo(grownChannel);
	}
	void OnEnable() {
		sub = MessageBus<OccupancyGrid>.SubscribeTo(inputChannel, Handler); // in ros, this is just "/map"
	}
	void OnDisable() {
		sub.Unsubscribe();
	}

	void OnDrawGizmos() {
		if (visualizeGrown && grown.info != null) { grown.DrawGizmos(visualizationAlpha, null, grownColor, null); }	
		if (visualizeFronts && frontiers.info != null) { frontiers.DrawGizmos(visualizationAlpha, null, null, frontiersColor); }	

	}
	void Handler(OccupancyGrid input) {
		// Debug.Log($"Got a grid of {input}");
		sbyte[] data = Grow(input, growthFactor);
		grown = new OccupancyGrid(input.header, input.info, data);
		

		grownPub.Publish(grown);
		sbyte[] fronts = Find(grown);
		frontiers = new OccupancyGrid(input.header, input.info, fronts);

		// for some reason in ros, you have to reuse the input OccupancyGrid
		// input.data = fronts; // in ROS , just reassign the `OccupancyGrid.data` field

		frontierPub.Publish(frontiers);
	}

	static sbyte[] Find(OccupancyGrid grown, int amt = 1) {
		sbyte[] data = new sbyte[grown.size];
		if (amt < 1) { amt = 1; }
		for (int i = 0; i < grown.size; i++) {
			Vector2Int pt = grown.IndexToGrid(i);
			int x = pt.x;
			int y = pt.y;

			int occ = 0; // occupied neighbors 
			int unk = 0; // unknown neighbors
			for (int xx = -amt; xx <= amt; xx++) {
				for (int yy = -amt; yy <= amt; yy++) {
					if (xx == 0 && yy == 0) { continue; }
					int idx = grown.GridToIndex(x + xx, y + yy);
					if (idx >= 0 && idx < grown.size) {
						if (grown.data[idx] > 50) { occ++; }
						if (grown.data[idx] < 0) { unk++; }
					}
				}
			}
			data[i] = -1;
			sbyte sample = grown.data[i];
			if (sample >= 0 && sample < 50) {
				data[i] = (sbyte)((occ == 0 && unk > 0) ? 100 : -1);
			}
		}
		return data;
	}

	static sbyte[] Grow(OccupancyGrid input, int amt = 1) {
		sbyte[] data = new sbyte[input.size];
		Array.Copy(input.data, data, data.Length);
		if (amt < 1) { amt = 1; }
		for (int i = 0; i < input.size; i++) {
			Vector2Int pt = input.IndexToGrid(i);
			int x = pt.x; // convert from 1d index to 2d
			int y = pt.y;
			// data[i] = input.data[i];

			// check all neighbors within some distance (amt)
			for (int xx = -amt; xx <= amt; xx++) {
				for (int yy = -amt; yy <= amt; yy++) {
					if (xx == 0 && yy == 0) { continue; }
					int idx = input.GridToIndex(x + xx, y + yy);
					// convert back from the neighbors' 2d index to 1d index
					// see if they're inside the grid 
					if (idx >= 0 && idx < input.size) {
						if (input.data[idx] > 50) {  // if they're occupied
							data[i] = 100; //any occupied neighbors, we consider ourselves occupied
							goto next;
						}
					}
				}
			}

			next:;;

		}
		return data;
	}
}


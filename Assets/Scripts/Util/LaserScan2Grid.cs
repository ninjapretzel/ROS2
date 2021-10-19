using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

public class LaserScan2Grid : Node {

	public string scanPath = "undefined";
	public string gridPath = "undefined";

	public int width = 400;
	public int height = 400;
	/// <summary> Scale relative to scan resolution </summary>
	public float resolution = 1.0f;
	public Vector3 center = Vector3.zero;

	public OccupancyGrid.Header header;
	public OccupancyGrid.Info info;
	public OccupancyGrid last; 

	MessageBus<LaserScanner.ScanData>.Subscriber sub;
	MessageBus<OccupancyGrid>.Publisher pub;
	void Awake() {
		InitNode("LaserScanner", true);
		
		pub = MessageBus<OccupancyGrid>.PublishTo(gridPath);
		sub = MessageBus<LaserScanner.ScanData>.SubscribeTo(scanPath, Convert);
		
		Vector3 origin = center - new Vector3(width, 0, height) * resolution  / 2f;
		info = new OccupancyGrid.Info(width, height, resolution, origin);
		header = new OccupancyGrid.Header(gridPath);
		sbyte[] initial = new sbyte[info.size];
		
		for (int i = 0; i < info.size; i++) { initial[i] = -1; }
		
		last = new OccupancyGrid(header, info, initial);
	}

	public void Convert(LaserScanner.ScanData scan) {

		header = header.Next();
		sbyte[] data = new sbyte[info.size];
		OccupancyGrid next = new OccupancyGrid(header, info, data);

		for (int i = 0; i < scan.rays.Count; i++) {
			Ray r = scan.rays[i];
			Vector3 p = r.origin;
			
			
		}

		
		pub.Publish(next);
	}
}

public class OccupancyGrid {
	public class Header {
		public readonly string frame;
		public readonly DateTime timestamp;
		public readonly long seq;
		public Header(string frame) {
			this.frame = frame;
			timestamp = DateTime.UtcNow;
			seq = 0;
		}
		private Header(Header prev) {
			frame = prev.frame;
			timestamp = DateTime.UtcNow;
			seq = prev.seq + 1;
		}
		public Header Next() {
			return new Header(this);
		}
	}
	public class Info {
		public readonly float resolution;
		public readonly int width;
		public readonly int height;
		public readonly Vector3 origin;
		public int size { get { return width * height; } }
		public Info(int width, int height, float resolution, Vector3 origin) {
			this.width = width;
			this.height = height;
			this.resolution = resolution;
			this.origin = origin;
		}
	}
	public readonly Info info;
	public readonly Header header;
	public int width { get { return info.width; } }
	public int height { get { return info.height; } }
	public int resolution { get { return info.resolution; } }
	public int size { get { return width * height; } }
	public Vector3 origin { get { return info.origin; } }
	public readonly sbyte[] data;

	public OccupancyGrid(Header header, Info info, sbyte[] data) {
		this.info = info;
		this.header = header;
		if (data.Length != size) {
			throw new Exception($"OccupancyGrid: data.Length must match width * height. Expected {size}, got {data.Length}.");
		}
		this.data = data;
	}
	public Vector3Int Translate(Vector3 pt) {
		Vector3 d = pt-origin;
		Vector3Int idx = Vector3Int.zero;
		d /= resolution;
		idx.x = (int)d.x;
		idx.y = (int)pt.y;
		idx.z = (int)d.z;

		return idx;
	}

}
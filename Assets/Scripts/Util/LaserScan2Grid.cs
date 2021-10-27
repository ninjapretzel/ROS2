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

	[Range(0, 4.1f)]
	public int fuzzyGrow = 1;
	public float fuzzyStep = .33f;

	public OccupancyGrid.Header header;
	public OccupancyGrid.Info info;
	public OccupancyGrid last; 

	ISub<LaserScanData> sub;
	IPub<OccupancyGrid> pub;
	void Awake() {
		InitNode("LaserScanner", true);
		
		pub = MessageBus<OccupancyGrid>.PublishTo(gridPath);
		
		Vector3 origin = center - new Vector3(width, 0, height) * resolution  / 2f;
		info = new OccupancyGrid.Info(width, height, resolution, origin);
		header = new OccupancyGrid.Header(gridPath);
		sbyte[] initial = new sbyte[info.size];
		
		for (int i = 0; i < info.size; i++) { initial[i] = -1; }
		
		last = new OccupancyGrid(header, info, initial);
	}

	void OnEnable() {
		sub = MessageBus<LaserScanData>.SubscribeTo(scanPath, Convert);
	}
	void OnDisable() {
		sub.Unsubscribe();
	}
	

	public void Convert(LaserScanData scan) {
		// Debug.Log($"Converting a scan with {scan.lines.Count} lasers");
		header = header.Next();
		sbyte[] data = new sbyte[info.size];
		Array.Copy(last.data, data, info.size);
		OccupancyGrid next = new OccupancyGrid(header, info, data);
		void ClearLine(LaserLine line) {
			float d = 0; 
			while (d < line.distance) {
				d += info.resolution * fuzzyStep;
				Vector3Int idx = next.WorldToGrid(line.ray.GetPoint(d));
				int index = idx.x + idx.z * info.width;
				if (index >= 0 && index < info.size) { data[index] = 0; }
			}
		}
		void TerminateLine(LaserLine line, int grow = 1) {
			if (grow < 0) { grow = 0; }
			for (int x = -grow; x <= grow; x++) {
				for (int z = -grow; z <= grow; z++) {
					Vector3Int idx = next.WorldToGrid(line.end + new Vector3(x,0,z) * resolution * fuzzyStep);
					int index = idx.x + idx.z * info.width;
					if (index >= 0 && index < info.size) { data[index] = 100; }
				}
			}
		}

		for (int i = 0; i < scan.lines.Count; i++) { ClearLine(scan.lines[i]); }
		for (int i = 0; i < scan.lines.Count; i++) {  
			if (scan.lines[i].hit) { TerminateLine(scan.lines[i], fuzzyGrow); }
		}

		pub.Publish(next);
		last = next;
	}

}

[System.Serializable]
public class OccupancyGrid {
	[System.Serializable]
	public class Header {
		public string frame;
		public long seq;
		public DateTime timestamp;
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
		public override string ToString() {
			return $"Header {{ frame: \"{frame}\", seq: {seq}, timestamp: {timestamp.UnixTimestamp()} }}";
		}
	}
	[System.Serializable]
	public class Info {
		public int width;
		public int height;
		public float resolution;
		public Vector3 extents { get { return new Vector3(width, 0, height) * resolution / 2f; } }
		public Vector3 center { get { return origin + extents;}}
		public Vector3 origin;
		public int size { get { return width * height; } }
		public Info(int width, int height, float resolution, Vector3 origin) {
			this.width = width;
			this.height = height;
			this.resolution = resolution;
			this.origin = origin;
		}
		public override string ToString() {
			return $"Info {{ width: {width}, height: {height}, resolution: {resolution}, origin: {origin} }}";
		}
	}
	public Info info;
	public Header header;
	public int width { get { return info.width; } }
	public int height { get { return info.height; } }
	public float resolution { get { return info.resolution; } }
	public int size { get { return width * height; } }
	public Vector3 extents { get { return info.extents; } }
	public Vector3 center { get { return info.center; } }
	public Vector3 origin { get { return info.origin; } }
	public sbyte[] data;
	public sbyte this[Vector2Int p] { get { return this[p.x, p.y]; } }
	public sbyte this[int x, int y] { get { return this[x+y*width]; } }
	public sbyte this[int i] { get { return (i >= 0 && i < size) ? data[i] : (sbyte)100; } }


	public Color? bg = null;
	public Color? opn = null;
	public Color? occ = null;

	public OccupancyGrid(Header header, Info info, sbyte[] data) {
		this.info = info;
		this.header = header;
		if (data.Length != size) {
			throw new Exception($"OccupancyGrid: data.Length must match width * height. Expected {size}, got {data.Length}.");
		}
		this.data = data;
	}

	public Vector3Int WorldToGrid(float x, float y, float z) { return WorldToGrid(new Vector3(x,y,z)); }
	public Vector3Int WorldToGrid(Vector3 pt) {
		Vector3 d = (pt-origin)/resolution;
		return new Vector3Int((int)d.x, (int)d.y, (int)d.z);
	}

	public Vector2Int WorldXZToGrid(float x, float y, float z) { return WorldXZToGrid(new Vector3(x,y,z)); }
	public Vector2Int WorldXZToGrid(Vector3 pt) { 
		Vector3 d = (pt-origin)/resolution;
		return new Vector2Int((int)d.x, (int)d.z);
	}
	public Vector2Int WorldXYToGrid(float x, float y, float z) { return WorldXZToGrid(new Vector3(x, y, z)); }
	public Vector2Int WorldXYToGrid(Vector3 pt) {
		Vector3 d = (pt - origin) / resolution;
		return new Vector2Int((int)d.x, (int)d.y);
	}

	public Vector3 GridToWorldXZ(Vector2Int idx) { return GridToWorld(new Vector3Int(idx.x, 0, idx.y)); }
	public Vector3 GridToWorldXY(Vector2Int idx) { return GridToWorld(new Vector3Int(idx.x, idx.y, 0)); }

	public Vector3 GridToWorld(int x, int y, int z) { return GridToWorld(new Vector3Int(x,y,z)); }
	public Vector3 GridToWorld(Vector3Int idx) {
		return ((Vector3)idx) * resolution + origin;;
	}

	public int GridToIndex(int x, int y) { return x + y * width; }
	public Vector2Int IndexToGrid(int i) { return new Vector2Int(i % width, i / width); }

	public override string ToString() {
		return $"OccupancyGrid {{\n\theader:{header},\n\tinfo:{info},\n\tdata: [ ...{size}... ]\n}}";
	}

	public void Visualize(float visualizationAlpha) {
		Color background = bg ?? new Color(.5f, .5f, .5f);
		background.a = visualizationAlpha;
		background.a *= visualizationAlpha;
		Gizmos.color = background;
		
		Gizmos.DrawCube(center, extents * 2f);

		Color occupied = occ ?? new Color(0,0,0);
		Color open = opn ?? new Color(1,1,1);

		Vector3 size = Vector3.one * info.resolution;
		size.y *= .05f;
		Vector3 halfSize = size * .5f;
		for (int i = 0; i < info.size; i++) {
			if (data[i] >= 0) {
				float f = 1f - data[i] / 100f;
				Color c = Color.Lerp(occupied, open, f);
				c.a = visualizationAlpha;
				Gizmos.color = c;
				int x = i % info.width;
				int z = i / info.width;
				Vector3 pt = GridToWorld(x, 0, z) + halfSize;
				Gizmos.DrawCube(pt, size);
			}
		}

	}

}
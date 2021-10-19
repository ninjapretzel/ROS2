using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TRS {
	public Vector3 position;
	public Vector3 scale;
	public Quaternion rotation;
	public TRS() {
		position = Vector3.zero;
		scale = Vector3.one;
		rotation = Quaternion.identity;
	}
	public TRS(Vector3 position) : this() { this.position = position; }
	public TRS(Vector3 position, Vector3 scale) : this() { this.position = position; this.scale = scale; }
	public TRS(Vector3 position, Quaternion rotation) : this() { this.position = position; this.rotation = rotation; }
	public TRS(Vector3 position, Vector3 scale, Quaternion rotation) {
		this.position = position;
		this.scale = scale;
		this.rotation = rotation;
	}
	public override string ToString() {
		return $"TRS {{ t:{position}, r:{rotation.eulerAngles}, s:{scale} }}";
	}

	public static implicit operator TRS(Transform t) { return new TRS(t.position, t.localScale, t.rotation); }
}

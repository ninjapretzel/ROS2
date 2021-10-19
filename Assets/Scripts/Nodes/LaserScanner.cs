using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct LaserLine {
	public readonly float distance;
	public readonly Ray ray;
	public readonly bool hit;
	public Vector3 origin { get { return ray.origin; } }
	public Vector3 direction { get { return ray.direction; } }
	public Vector3 end { get { return origin + direction * distance; } }
	public LaserLine(Ray r, float d, bool h) { distance = d; ray = r; hit = h; }
	public static implicit operator LaserLine((Ray r, float d, bool h) _) { return new LaserLine(_.r, _.d, _.h); }
}
[System.Serializable]
public class LaserScanData {
	public List<LaserLine> lines;
	public LaserScanData() {
		lines = new List<LaserLine>();
	}
}

public class LaserScanner : Node {
	

	public string channel = "undefined";
	const float TAU = Mathf.PI * 2;
	public float maxDist = 50f;
	[Range(0, 360)]
	public float sweep = 360;
	[Range(20,720)]
	public int numLasers = 100;
	public float offset = .1f;
	[Range(0, .5f)]
	public float laserWobble = .1f;
	public float uncertainty { get { return .01f * laserWobble; } }

	public bool visualizeLine = false;
	public bool visualizeHit = false;
	
	IPub<LaserScanData> pub;

	void Awake() {
		InitNode("LaserScanner", true);
		pub = MessageBus<LaserScanData>.PublishTo(channel);
	}

	void OnDestroy() {
		
	}

	public override void Tick() {

		LaserScanData scan = new LaserScanData();
		Vector3 center = transform.position;
		Vector3 direction = transform.forward;
		Vector3 axis = transform.up;
		float deltaAngle = sweep / numLasers;
		
		for (int i = 0; i < numLasers; i++) {
			float angle = -sweep/2 + deltaAngle * i;
			Vector3 dir = Quaternion.AngleAxis(angle, axis) * direction;
			dir += Random.insideUnitSphere * uncertainty;
		
			Vector3 p = center + dir.normalized * offset;
			Ray ray = new Ray(p, dir);
			RaycastHit rayhit;

			float dist = maxDist;
			bool hit;
			if (hit = Physics.Raycast(ray, out rayhit, maxDist)) {
				if (rayhit.distance > 0 && rayhit.distance < dist) { dist = rayhit.distance; }

				if (visualizeHit) {
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.up + Random.insideUnitSphere * uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.up + Random.insideUnitSphere * uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.right + Random.insideUnitSphere * uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.right + Random.insideUnitSphere * uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.forward + Random.insideUnitSphere * uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.forward + Random.insideUnitSphere * uncertainty, Color.red, delay); 
				}
			}
			
			scan.lines.Add((ray, dist, hit));
			if (visualizeLine) {
				Debug.DrawLine(p, p+dir.normalized * (dist > 0 ? dist : maxDist), dist > 0 ? Color.green : Color.red, delay);
				// Debug.DrawLine(p, Random.insideUnitSphere * .1f + dir.normalized * (dist > 0 ? dist : maxDist), dist > 0 ? Color.green : Color.red, delay);
			}

		}

		pub.Publish(scan);	

	}

}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserScanner : Node {
	
	public class ScanData {
		public List<float> distances;
		public List<Ray> rays;
		public ScanData() {
			distances = new List<float>();
			rays = new List<Ray>();
		}
	}

	public string channel = "undefined";
	const float TAU = Mathf.PI * 2;
	public int granularity = 100;
	[Range(0, 360)]
	public float sweep = 360;
	public float offset = .1f;
	public float maxDist = 50f;
	[Range(0, .5f)]
	public float uncertaintyv = .1f;
	public float uncertainty { get { return .1f * uncertaintyv; } }

	public bool visualizeLine = false;
	public bool visualizeHit = false;
	
	MessageBus<ScanData>.Publisher pub;

	void Awake() {
		InitNode("LaserScanner", true);
		pub = MessageBus<ScanData>.PublishTo(channel);

	}

	public override void Tick() {

		ScanData scan = new ScanData();
		Vector3 center = transform.position;
		Vector3 direction = transform.forward;
		Vector3 axis = transform.up;
		float deltaAngle = sweep / granularity;
		
		for (int i = 0; i < granularity; i++) {
			float angle = -sweep/2 + deltaAngle * i;
			Vector3 dir = Quaternion.AngleAxis(angle, axis) * direction;
			dir += Random.insideUnitSphere * uncertainty;
		
			Vector3 p = center + dir.normalized * offset;
			Ray ray = new Ray(p, dir);
			RaycastHit rayhit;

			float dist = -1;
			if (Physics.Raycast(ray, out rayhit, maxDist)) {
				if (rayhit.distance > 0) {
					dist = rayhit.distance;
				}
				if (visualizeHit) {
					Debug.DrawLine(rayhit.point, rayhit.point-dir + Random.insideUnitSphere*uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.up + Random.insideUnitSphere* uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.up + Random.insideUnitSphere* uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.right + Random.insideUnitSphere* uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.right + Random.insideUnitSphere* uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point+Vector3.forward+ Random.insideUnitSphere* uncertainty, Color.red, delay); 
					Debug.DrawLine(rayhit.point, rayhit.point-Vector3.forward+ Random.insideUnitSphere*uncertainty, Color.red, delay); 
				}
			}
			
			scan.distances.Add(dist);
			scan.rays.Add(ray);
			if (visualizeLine) {
				Debug.DrawLine(p, p+dir.normalized * (dist > 0 ? dist : maxDist), dist > 0 ? Color.green : Color.red, delay);
				// Debug.DrawLine(p, Random.insideUnitSphere * .1f + dir.normalized * (dist > 0 ? dist : maxDist), dist > 0 ? Color.green : Color.red, delay);
			}

		}

		pub.Publish(scan);	

	}

}


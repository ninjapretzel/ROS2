using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class WallFollow : Node {
	public string scanPath = "undefined";
	public string feelPath = "undefined";

	public float[] defaultActions = new float[] { 1f, 0f, 0f, 0f };
	public float speed = 11f;
	public float turnSpeed = 88f;
	public FeelerRegion[] regions;
	public Dictionary<string, float[]> qt;

	public CollisionFlags lastFlags = CollisionFlags.None;
	public float[] lastActions = new float[] { 0,0,0,0 };

	public int[] currentState = new int[] { 0,0,0,0 };
	public string currentStateCode = "0000";

	CharacterController c;
	ISub<LaserScanData> scanSub;
	IPub<Feeler> feelPub;
	void Awake() {
		InitNode("LaserScanFeeler", true);
		
	}
	void Start() {
		Reset();
		
	}

	void OnEnable() {
		scanSub = MessageBus<LaserScanData>.SubscribeTo(scanPath, OnScan);
		feelPub = MessageBus<Feeler>.PublishTo(feelPath);
	}

	
	void OnDisable() {
		scanSub.Unsubscribe();
		
	}

	public void Reset() {
		currentState = new int[regions.Length];
		currentStateCode = "";
		for (int i = 0; i < regions.Length; i++) {
			currentState[i] = regions[i].max;
			currentStateCode += CHARS[currentState[i]];
		}
		lastActions = defaultActions;
		lastFlags = CollisionFlags.None;
	}

	public static T[] Duplicate<T>(T[] ts) {
		T[] copy = new T[ts.Length];
		for (int i = 0; i < ts.Length; i++) { copy[i] = ts[i]; }
		return copy;
	}
	public Dictionary<string, float[]> InitializeTable() {
		int nStates = 1;
		foreach (var region in regions) { nStates *= region.nStates; }

		var table = new Dictionary<string, float[]>();
		char[] code = new char[regions.Length];
		for (int i = 0; i < code.Length; i++) { code[i] = CHARS[0]; }
		for (int i = 0; i < nStates; i++) {
			int n = i;
			for (int k = regions.Length-1; k >= 0; k--) {
				var region = regions[k];
				int bit = n % region.nStates;
				n /= region.nStates;
				code[k] = CHARS[bit];
			}
			string kk= new string(code);
			table[kk] = Duplicate(defaultActions);
			//Debug.Log($"Added code {kk}");
		}

		return table;
	}

	new void Update() {
		((Node)this).Update();

		float[] actions =  qt.ContainsKey(currentStateCode) ? qt[currentStateCode] : defaultActions;
		string s = "[ ";
		for (int i = 0; i < actions.Length; i++) { s += $"{actions[i]:f000}, "; }
		s += "]";
		if (lastActions != actions) {
			if (qt.ContainsKey(currentStateCode)) {
				Debug.Log($"Withdrew actions {s} from code = [{currentStateCode}]");
			} else {
				Debug.Log($"code {currentStateCode} not found, using defaultActions = {s}");
			}
		}


		if (c == null) { c = GetComponent<CharacterController>(); }
		if (c == null) { c = gameObject.AddComponent<CharacterController>(); }

		float velocity = speed * (actions[0] - actions[1]);
		float angVel = turnSpeed * (actions[2] - actions[3]);
		transform.Rotate(0, angVel * Time.deltaTime, 0);
		c.Move(transform.forward * velocity * Time.deltaTime);;

		lastFlags = c.collisionFlags;
		
		lastActions = actions;
	}
	
	static bool Within(float angle, float start, float end) {
		if (angle >= start && angle <= end) { return true; }
		angle += 360;
		if (angle >= start && angle <= end) { return true; }
		angle -= 720;
		if (angle >= start && angle <= end) { return true; }
		return false;
	}
	void OnScan(LaserScanData scan) {
		// File.WriteAllText("scan.json", Json.Reflect(scan).PrettyPrint());
		int[] state = new int[regions.Length];
		for (int i = 0; i < regions.Length; i++) { state[i] = regions[i].max; }

		for (int i = 0; i < scan.lines.Count; i++) {
			var angle = scan.lines[i].angle;
			for (int k = 0; k < regions.Length; k++) {
				var region = regions[k];

				if (Within(-angle, region.start, region.end)) {
					var dist = scan.lines[i].distance;
					for (int j = region.max-1; j >= 0; j--) {
						if (dist < region.distances[j] && state[k] > j) { state[k] = j; }
						else { break; }
					}
				}

			}
		}

		string s = "";
		for (int i = 0; i < state.Length; i++) {
			s += CHARS[state[i]];
		}

		currentState = state;
		currentStateCode = s;

		feelPub.Publish(new Feeler(regions, scan, state, this));
	}

	public const string CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	[System.Serializable]
	public class FeelerRegion {
		public string name;
		public float start; 
		public float end;
		public float[] distances;
		public float maxDistance { get { return distances[max-1]; } }
		public int max { get { return distances.Length; } }
		public int nStates { get { return max+1; } }

	}

	[System.Serializable]
	public class Feeler {
		public FeelerRegion[] regions;
		public LaserScanData scan;
		public int[] state;
		public string code;
		public Node node;
		public Feeler(FeelerRegion[] regions, LaserScanData scan, int[] state, Node node) {
			this.regions = regions;
			this.scan = scan;
			this.state = state;
			this.node = node;
			code = "";
			for (int i = 0; i < state.Length; i++) {
				code += CHARS[i];
			}
		}
		const float H = 1f;
		const float L = .4f;
		public static readonly Color[] CS = new Color[] {
			new Color(H, L, L),
			new Color(L, H, L),
			new Color(L, L, H),
			new Color(L, H, H),
			new Color(H, L, H),
			new Color(H, H, L),
		};
		public void Visualize(float alpha) {
			if (!node) { return; }
			Vector3 forward = node.transform.forward;
			void setColor(int i, float scale = 1.0f) {
				Color col = CS[i % CS.Length];
				col.a = alpha * scale;
				Gizmos.color = col;
			}
			
			for (int i = 0; i < regions.Length; i++) {
				var region = regions[i];

				for (int j = 0; j < region.max; j++) {
					float dist = region.distances[j];
					
					setColor(i, state[i] <= j ? .44f : 1f);
					Vector3 last = node.pos;
					float segments = 8;
					for (int k = 0; k < segments; k++) {
						Quaternion r = Quaternion.Euler(0, -Mathf.Lerp(region.start, region.end, k/(segments-1)), 0);
						Vector3 next = node.pos + r * forward * dist;
						Gizmos.DrawLine(last, next);
						last = next;
					}

					Gizmos.DrawLine(last, node.pos);
				}

				for (var k = 0; k < scan.lines.Count; k++) {
					var line = scan.lines[k];
					if (Within(-line.angle, region.start, region.end)) {
						setColor(i, .4f);
						Gizmos.DrawLine(line.ray.origin, line.ray.origin + line.ray.direction * line.distance);

					}
				}
						
			}
		}

	}
}



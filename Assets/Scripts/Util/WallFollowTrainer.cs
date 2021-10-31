using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(WallFollowTrainer))]
public class WFTEditor : Editor {
	public void OnSceneGUI() {
		var t = target as WallFollowTrainer;
		Quaternion id = Quaternion.identity;
		Handles.TransformHandle(ref t.initialPosition, ref id);
	}
}
#endif

public class WallFollowTrainer : MonoBehaviour {

	public class QTData {
		public float score;
		public int generation;
		public int trial;
		public Dictionary<string, float[]> data;
	}

	public WallFollow wallFollow;
	public Dictionary<string, float[]> best;
	public bool sim = false;
	public Vector3 initialPosition;
	[Range(0, 360)] public float initialRotation = 0;
	public string filename = "qt";
	public int generation = 0;
	public int trial = 0;
	public float bestScore = 0;
	public float currentScore;
	public float mutateChance = .01f;
	public float drift = .1f;
	public float noScoreTimeout = 30f;
	public float noScore = 0;
	public float gain;

	public float[] rightDistanceMults = new float[] { 1f, 10f, 2f, 1f, 0f };
	public float forwardScoreMult = 10f;
	public float maxForwardScore = 5f;

	public float simDelay = .1f;

	public static T[] Duplicate<T>(T[] ts) {
		T[] copy = new T[ts.Length];
		for (int i = 0; i < ts.Length; i++) { copy[i] = ts[i]; }
		return copy;
	}
	public static Dictionary<K, T[]> Copy<K, T>(Dictionary<K, T[]> d) {
		Dictionary<K, T[]> copy = new Dictionary<K, T[]>();
		foreach (var pair in d) {
			copy[pair.Key] = Duplicate<T>(pair.Value);
		}
		return copy;
	}

	void Awake() {
		if (File.Exists(filename+".json")) {
			JsonObject result = Json.Parse<JsonObject>(File.ReadAllText(filename + ".json"));
			if (result != null) {
				QTData qtdata = Json.GetValue<QTData>(result);
				best = qtdata.data;
				bestScore = qtdata.score;
				generation = qtdata.generation;
				trial = qtdata.trial;

				Debug.Log($"Initialized best qt table from {filename}.json: {best.Count} states.");
			}
		}
		if (best == null) {
			Debug.Log($"Using new qt for {filename}.json");
			best = wallFollow.InitializeTable();
		}
	}
	
	void Start() {
		PrepareSim();
	}

	void OnDisable() {
		if (best != null) {
			QTData qt = new QTData() { data = best, score = bestScore, trial = trial, generation = generation };
			File.WriteAllText(filename + ".json", Json.Reflect(qt).ToString());
			Debug.Log($"Saved best qt to {filename}.json");
		}
	}

	void LateUpdate() {
		if (!sim) { return; }
		if (!wallFollow || !wallFollow.isActiveAndEnabled) { return; }

		// Objective function:
		gain = 1;
		int rightState = wallFollow.currentState[3];
		float forwardSpeed = wallFollow.lastActions[0] - wallFollow.lastActions[1];

		// Score based on wall dist to right,
		// eg [ 1, 10, 3, 2, 1 ]  for indexes of [ reallyClose, close, medium, far, reallyFar ]
		float gainA = rightDistanceMults[rightState];
		// relatively square the gain so that many more points are gained when moving near full speed,
		// and fewer gained when not moving close to full speed
		float gainB = Mathf.Lerp(0, forwardScoreMult, Mathf.Clamp01(forwardSpeed));
		gainB /= forwardScoreMult;
		gainB *= gainB;
		gainB *= forwardScoreMult;

		gain *= gainA * gainB;
		float sc = currentScore;
		currentScore += gain * Time.deltaTime;

		if (!(currentScore > sc)) {
			noScore += Time.deltaTime; 
		} else {
			noScore -= Time.deltaTime;
			if (noScore < 0) { noScore = 0; }
		}
		
		if ((wallFollow.lastFlags & CollisionFlags.Sides) != CollisionFlags.None) {
			Debug.Log("Terminating sim - Collision Detected");
			EndSim();
			return;
		}
		if (noScore > noScoreTimeout) {
			Debug.Log("Terminating sim - Too long since score gained");
			EndSim();
			return;
		}


	}

	void PrepareSim() {
		wallFollow.transform.position = wallFollow.pos = initialPosition;
		wallFollow.transform.rotation = wallFollow.rot = Quaternion.Euler(0, initialRotation, 0);
		Debug.Log($"Teleported robot to {wallFollow.transform.position} / {wallFollow.transform.rotation}");// 
		trial++;
		wallFollow.qt = Mutate(best);
		currentScore = 0;
		noScore = 0;
		gain = 0;
		wallFollow.Reset();
		
		Invoke("StartSim", simDelay);
	}
	void StartSim() {
		sim = true;
		wallFollow.gameObject.SetActive(true);
	}

	void EndSim() {
		sim = false;
		wallFollow.gameObject.SetActive(false);

		if (currentScore  >= bestScore - Time.deltaTime * gain) {
			Debug.Log($"{currentScore} beat {bestScore} within one frame worth of score");
			best = wallFollow.qt;
			bestScore = currentScore;
			generation++;
		}
		
		PrepareSim();
	}
		


	const int DRIFT = 0;
	const int SET = 1;
	Dictionary<string, float[]> Mutate(Dictionary<string, float[]> d) {
		
		var copy = Copy(d);

		foreach (var pair in copy) {
			var vals = pair.Value;
			for (int i = 0; i < vals.Length; i++) {
				
				if (Random.value < mutateChance) {
					
					int mutateKind = Random.Range(0, 2);
					if (mutateKind == DRIFT) { // drift action by some small amount
						float roll = Random.value;
						float change = (-1 + roll * 2) * drift;
						vals[i] = Mathf.Clamp01(vals[i] + drift);

					} else if (mutateKind == SET) { // set action to some random value
						vals[i] = Random.value;
					}

				}

			}

		}

		return copy;
	}
}
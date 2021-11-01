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
		public int species; 
		public int trial;
		public Dictionary<string, float[]> data;
	}

	[Header("Setup")]
	public WallFollow wallFollow;
	public Dictionary<string, float[]> best;
	public bool sim = false;
	public bool resimulate = false;
	public float simDelay = .1f;
	public Vector3 initialPosition;
	[Range(0, 360)] public float initialRotation = 0;

	[Header("Save/Load/Tracking")]
	public string filename = "qt";
	public int generation = 0;
	public int species = 0;
	public int trial = 0;
	public float simTime = 0;
	public float bestScore = 0;
	public float currentScore;
	public float gain;

	[Header("Scoring Settings")]
	public float[] rightDistanceMults = new float[] { 1f, 2f, 1.5f, 1f, 0f, 0f, 0f, 0f, 0f }; 
	// public float[] rightDistanceMults = new float[] { 1f, 10f, 2f, 1f, 0f, 0f, 0f, 0f, 0f }; // Note: over-valuing distances is bad.
	public float forwardScoreMult = 10f;
	public float maxForwardScore = 5f;
	
	[Header("Mutation Settings")]
	public float mutateChance = .01f;
	public float drift = .1f;
	public float driftChance = .9f;

	[Header("Damage Settings ")]
	public float maxHealth = 30f;
	public float curHealth = 0;
	public float heal = 1.0f;
	public float healShrink = 1000;
	public float damage = 1.0f;
	public float damageGrow = 180;
	[SerializeField] private float damageRate = 1.0f;
	[SerializeField] private float healRate = 1.0f;



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
				species = qtdata.species;
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
			SaveBest();
		}
	}

	private void SaveBest() {
		QTData qt = new QTData() { data = best, score = bestScore, generation = generation, species = species, trial = trial, };
		File.WriteAllText(filename + ".json", Json.Reflect(qt).ToString());
		Debug.Log($"Saved best qt to {filename}.json");
	}

	void LateUpdate() {
		if (!sim) { return; }
		if (!wallFollow || !wallFollow.isActiveAndEnabled) { return; }

		// Objective function:
		gain = 1;
		simTime += Time.deltaTime;
		int rightState = wallFollow.currentState[3];
		float forwardSpeed = wallFollow.lastActions[0] - wallFollow.lastActions[1];

		// Score based on wall dist to right,
		// eg [ 1, 10, 3, 2, 1 ]  for indexes of [ reallyClose, close, medium, far, reallyFar ]
		float gainA = rightDistanceMults[rightState];
		// relatively square the gain so that many more points are gained when moving near full speed,
		// and fewer gained when not moving close to full speed
		float gainB = Mathf.Lerp(0, forwardScoreMult, forwardSpeed);
		gainB /= forwardScoreMult;
		gainB *= gainB * gainB; // cubed to allow negative scores for reverse
		gainB *= forwardScoreMult;

		gain *= gainA * gainB;
		float sc = currentScore;
		currentScore += gain * Time.deltaTime;
		damageRate = damage + simTime / damageGrow;
		healRate = heal * (healShrink / (healShrink + simTime));

		if (!(currentScore > sc)) {
			curHealth -= Time.deltaTime * damageRate;
		} else {
			curHealth += Time.deltaTime * healRate;
			if (curHealth >= maxHealth) { curHealth = maxHealth; }
		}
		
		if ((wallFollow.lastFlags & CollisionFlags.Sides) != CollisionFlags.None) {
			Debug.Log("Terminating sim - Collision Detected");
			EndSim();
			return;
		}
		if (curHealth <= 0) {
			Debug.Log("Terminating sim - Not gaining enough score");
			EndSim();
			return;
		}


	}

	void PrepareSim() {
		wallFollow.transform.position = wallFollow.pos = initialPosition;
		wallFollow.transform.rotation = wallFollow.rot = Quaternion.Euler(0, initialRotation, 0);
		
		Debug.Log($"Setting up trial {trial} @ generation {generation}");
		//Debug.Log($"Teleported robot to {wallFollow.transform.position} / {wallFollow.transform.rotation}");
		if (resimulate) {
			resimulate = false;
			wallFollow.qt = best;
			bestScore = 0;
		} else {
			wallFollow.qt = Mutate(best); 
			species++;
		}
		currentScore = 0;
		curHealth = maxHealth;
		simTime = 0;
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
		trial++;
		wallFollow.gameObject.SetActive(false);

		if (currentScore  >= bestScore - Time.deltaTime * gain) {
			if (wallFollow.qt != best) {
				Debug.Log($"{currentScore} beat {bestScore} within one frame worth of score");
				bestScore = currentScore;
				best = wallFollow.qt;
				generation++;
				species = 0;
			} else {
				bestScore = currentScore;
				Debug.Log($"Rescored best to {bestScore}");
			}
			SaveBest();
		}
		
		PrepareSim();
	}
	
	Dictionary<string, float[]> Mutate(Dictionary<string, float[]> d) {
		
		var copy = Copy(d);

		foreach (var pair in copy) {
			var vals = pair.Value;
			for (int i = 0; i < vals.Length; i++) {
				
				if (Random.value < mutateChance) {
					
					if (Random.value < driftChance) { // drift action by some small amount
						float roll = Random.value;
						float change = (-1 + roll * 2) * drift;
						vals[i] = Mathf.Clamp01(vals[i] + drift);
					} else { // set action to some random value
						vals[i] = Random.value;
					}

				}

			}

		}

		return copy;
	}
}
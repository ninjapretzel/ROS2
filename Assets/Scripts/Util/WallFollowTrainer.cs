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
	public bool generateRegions = true;
	public int numRegions = 36;
	public float targetDistance = 4f;
	public float targetFalloff = 5f;
	public float distanceStep = 2f;
	public float maxDistance = 20f;
	
	public FeelerRegion[] GenerateRegions() {
		float angleDiff = 360f / numRegions;

		FeelerRegion[] regions = new FeelerRegion[numRegions];
		int nD = (int)(maxDistance/distanceStep);
		float[] distances = new float[nD];
		for (int i = 0; i < nD; i++) { distances[i] = (i+1) * distanceStep; }

		rightDistanceMults = new float[nD + 1];
		for (int i = 0; i < nD + 1; i++) {
			float dist = Mathf.Abs(targetDistance - (i * distanceStep));
			float f = dist / targetFalloff;
			float score = Mathf.Clamp01(Mathf.Lerp(1, 0, f));
			rightDistanceMults[i] = score;
		}
		Debug.Log($"Generated mults: {Json.Reflect(rightDistanceMults)}");

		float startAngle = -90 - angleDiff / 2f;
		for (int i = 0 ; i < numRegions; i++) {
			FeelerRegion region = new FeelerRegion();
			float start = startAngle + angleDiff * i;
			float end = startAngle + angleDiff * (i+1);
			region.name = $"{start:f2}-{end:f2}";
			region.start = start; 
			region.end = end;
			region.distances = distances;
			regions[i] = region;
		}
		regions[0].name = "right";

		return regions;
	}

	public float simDelay = .1f;
	public Vector3 initialPosition;
	[Range(0, 360)] public float initialRotation = 0;

	[Header("Save/Load/Tracking")]
	public string configFile = "setup1";
	public string qtFile = "qt";
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
		if (File.Exists(configFile+".json")) {
			JsonObject result = Json.Parse<JsonObject>(File.ReadAllText(configFile + ".json"));
			if (result != null) {
				Json.ReflectInto(result, this);
				Debug.Log($"Loaded config from {configFile}.json");

				if (result.Has("regions")) {
					if (result["regions"].isArray) {
						wallFollow.regions = Json.GetValue<FeelerRegion[]>(result["regions"]);
					}
				}
			}
		}


		if (File.Exists(qtFile+".json")) {
			JsonObject result = Json.Parse<JsonObject>(File.ReadAllText(qtFile + ".json"));
			if (result != null) {
				QTData qtdata = Json.GetValue<QTData>(result);
				best = qtdata.data;
				bestScore = qtdata.score;
				generation = qtdata.generation;
				species = qtdata.species;
				trial = qtdata.trial;

				Debug.Log($"Initialized best qt table from {qtFile}.json: {best.Count} states.");
			}
		}

		if (generateRegions) {
			Debug.Log($"Generating feeler region table of {numRegions} cnt");
			wallFollow.regions = GenerateRegions();
		}
			
		var test = wallFollow.InitializeTable();

		if (best == null) {
			try {

				best = wallFollow.InitializeTable();
			
				Debug.Log($"Using new qt of size {best.Count} for {qtFile}.json");
			}catch(System.Exception e) {
				gameObject.SetActive(false);
				wallFollow.gameObject.SetActive(false);
				Debug.LogError("Not initializing due to error " + e);
			}
		}
		if (best.Count != test.Count) {
			Debug.LogError($"qt size mismatch. not testing. Regions indiciate {test.Count} states, but qt has {best.Count}.");
			gameObject.SetActive(false);
			wallFollow.gameObject.SetActive(false);
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
		File.WriteAllText(qtFile + ".json", Json.Reflect(qt).ToString());
		Debug.Log($"Saved best qt to {qtFile}.json");
	}

	public static int IndexOf<T>(T[] ts, T t) {
		for (int i = 0; i < ts.Length; i++) {
			if (ts[i].Equals(t)) { return i; }
		}
		return -1;
	}
	public static int IndexOf<T>(T[] ts, System.Func<T, bool> selector) {
		for (int i = 0; i < ts.Length; i++) {
			if (selector(ts[i])) { return i; }
		}
		return -1;
	}
	void LateUpdate() {
		if (!sim) { return; }
		if (!wallFollow || !wallFollow.isActiveAndEnabled) { return; }

		// Objective function:
		gain = 1;
		simTime += Time.deltaTime;

		int rightIndex = IndexOf(wallFollow.regions, (it)=>{ return it.name=="right"; });
		if (rightIndex == -1) {
			Debug.LogWarning("WallFollowTrainer: To train, at least one region must have the name \"right\"!");
			gameObject.SetActive(false);
			wallFollow.gameObject.SetActive(false);
			return;
		}
		int rightState = wallFollow.currentState[rightIndex];
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
				SaveBest();
			} else {
				bestScore = currentScore;
				Debug.Log($"Rescored best to {bestScore}");
			}
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
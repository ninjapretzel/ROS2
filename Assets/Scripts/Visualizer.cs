using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
public class Visualizer : MonoBehaviour {
	public static Visualizer instance;

	public bool visualizeByDefault = true;
	Dictionary<object, float> visualize;
	public float this[object o] {
		get { 
			if (visualize == null) { visualize = new Dictionary<object, float>(); }
			return visualize.ContainsKey(o) ? visualize[o] : -1;
		}
		set { 
			if (visualize == null) { visualize = new Dictionary<object, float>(); }
			visualize[o] = value; 
		}
	}
	Dictionary<Type, bool> warned;
	
	void OnEnable() {
		if (instance == null) { instance = this; }
	}
	void OnDisable() {
		if (instance == this) { instance = null; }
	}

	void Start() {
		
	}

	void OnDrawGizmos() {
		if (visualize == null) { visualize = new Dictionary<object, float>(); }
		foreach (var pair in visualize) {
			if (pair.Value <= 0) { continue; }
			dynamic k = pair.Key;
			float alpha = pair.Value;
			if (k.Latched && k.HasValue) {
				dynamic v = k.LatchedValue;
				try {
					v.Visualize(alpha);
				
				} catch (Exception e) { 
					if (warned == null) { warned = new Dictionary<Type, bool>(); }
					Type t = v.GetType();
					if (!warned.ContainsKey(t)) {
						Debug.LogWarning($"Tried to visualize a {t}, but either it has no `Visualize(float)` method, or it failed with error.\n{e}");
						warned[t] = true;
					}
				}
			}

		}
	}

	void Update() {
		
	}
	
}

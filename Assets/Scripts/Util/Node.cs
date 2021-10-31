using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

public class Node : MonoBehaviour {

	public static IDictionary<string, Node> nodes = new ConcurrentDictionary<string, Node>();
	public string nodeName;
	public Vector3 pos;
	public Quaternion rot;
	public Vector3 scale;

	public static string Register(string name, Node node, bool anonymous = false) {
		if (anonymous) {
			name += "-"+Guid.NewGuid().ToString();
		}
		if (nodes.ContainsKey(name)) {
			throw new Exception($"Node named {name} already registered");
		}
		nodes[name] = node;
		return name;
	}
	public void InitNode(string name, bool anonymous = false) {
		nodeName = Register(name, this, anonymous);
	}

	public float rate = 1;
	public float delay { get { return 1f/rate; } }
	float timeout = 0;
	void Update() {
		timeout += Time.deltaTime * rate;
		pos = transform.position;
		rot = transform.rotation;
		scale = transform.lossyScale;
		while (timeout > 1) {
			timeout -= 1;
			Tick();
		}
	}
	public virtual void Tick() {

	}

}

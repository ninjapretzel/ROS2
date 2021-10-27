using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DirectlyFollowPath : MonoBehaviour {

	public string pathChannel = "path";
	public float speed = 5.5f;
	public Vector3 offset = Vector3.up;
	public int i = 1;
	public List<Vector3> targets;
	ISub<List<Vector3>> targetSub;

	void OnEnable() {
		targetSub = MessageBus<List<Vector3>>.SubscribeTo(pathChannel, OnPath);
	}
	void OnDisable() {
		targetSub.Unsubscribe();
	}

	void OnPath(List<Vector3> path) {
		targets = path;
		i = 1;
	}

	void Start() {
		
	}
	
	void Update() {
		if (targets != null && targets.Count > 1 && i < targets.Count) {
			Vector3 target = targets[i] + offset;
			Vector3 pos = transform.position;
			Vector3 newPos = transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
			if ((target-transform.position).sqrMagnitude < .1) { i++; }

			Quaternion oldRot = transform.rotation;
			transform.LookAt(transform.position + (newPos - pos));
			transform.rotation = Quaternion.Slerp(oldRot, transform.rotation, Time.deltaTime * 11);

		}
	}
	
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DirectlyFollowPath : MonoBehaviour {

	public string pathChannel = "path";
	public float speed = 5.5f;
	public float approachDist = 1.1f;
	public Vector3 offset = Vector3.up;
	public int i = 1;
	public List<Vector3> targets;
	ISub<Path> targetSub;

	CharacterController c;

	void OnEnable() {
		targetSub = MessageBus<Path>.SubscribeTo(pathChannel, OnPath);
	}
	void OnDisable() {
		targetSub.Unsubscribe();
	}

	void OnPath(Path path) {

		targets = path.points;
		i = 1;
	}

	void Start() {
		
	}
	
	void Update() {
		if (c == null) { c = GetComponent<CharacterController>(); }
		if (c == null) { c = gameObject.AddComponent<CharacterController>(); }
		if (targets != null && targets.Count > 1 && i < targets.Count) {
			Vector3 target = targets[i] + offset;
			Vector3 pos = transform.position;
			Vector3 newPos = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
			Vector3 dir = (newPos - pos).normalized;

			c.Move(dir * speed * Time.deltaTime);
			if ((target-transform.position).sqrMagnitude < approachDist) { i++; }


			Quaternion oldRot = transform.rotation;
			transform.LookAt(transform.position + (newPos - pos));
			transform.rotation = Quaternion.Slerp(oldRot, transform.rotation, Time.deltaTime * 11);

		}
	}
	
}

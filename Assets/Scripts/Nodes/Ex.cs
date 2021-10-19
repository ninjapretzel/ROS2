using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ex : Node {

	MessageBus<TRS>.Publisher pub;
	MessageBus<TRS>.Subscriber sub;
	void Awake() {
		InitNode("yeet");
		
		pub = MessageBus<TRS>.PublishTo("yeet1");
		
		sub = MessageBus<TRS>.SubscribeTo("yeet1", (it)=>{
			Debug.Log($"Got TRS: {it}");	
		});


	}

	public override void Tick() {
		pub.Publish(transform);
	}



}
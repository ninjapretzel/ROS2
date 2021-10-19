using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class ChannelViewer : EditorWindow {
	
	[MenuItem("Window/ChannelViewer")]
	public static void Init() {
		ChannelViewer w = EditorWindow.GetWindow<ChannelViewer>();
		w.Show();
	}

	public ChannelViewer() {

	}

	void OnGUI() {
		int k = 0;

		foreach (var pair in MessageBus.publishers) {
			var type = pair.Key;
			var pubs = pair.Value;
			dynamic subs = MessageBus.subscribers[type];
			
			
			GUI.color = (k%2==0) ? Color.white : new Color(.8f,.8f,.8f);
			GUILayout.BeginVertical("box");  {
				GUILayout.Label($"Channels of {type}:");
				int i = 0;
				foreach (dynamic pair2 in pubs) {
					string path = pair2.Key;
					dynamic pub = pair2.Value;
					dynamic sub = subs.ContainsKey(path) ? subs[path] : null;

					GUI.color = (i % 2 == 0) ? Color.white : new Color(.8f, .8f, .8f);
					GUILayout.BeginHorizontal("box"); {
						GUILayout.Label($"publisher @ {path} with {(sub != null ? sub.Count : 0)} subscribers");
					} GUILayout.EndHorizontal();
					i++;
				}

			} GUILayout.EndVertical();


			k++;
		}

	}


}

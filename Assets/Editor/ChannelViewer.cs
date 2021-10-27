using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class ChannelViewer : EditorWindow {
	
	[MenuItem("Window/ChannelViewer")]
	public static void Init() {
		ChannelViewer w = EditorWindow.GetWindow<ChannelViewer>();
		w.Show();
	}


	Vector2 scroll;
	public ChannelViewer() {
	}

	void OnGUI() {
		int k = 0;

		scroll = GUILayout.BeginScrollView(scroll, false, true); {

			IEnumerable<KeyValuePair<System.Type, IEnumerable>> publishers = MessageBus.publishers;
			List<KeyValuePair<System.Type, IEnumerable>> list = publishers.ToList();
			list.Sort((a,b)=>{return a.Key.Name.CompareTo(b.Key.Name);});
			publishers = list;
			float p = .6f;
			var v = Visualizer.instance;

			foreach (var pair in publishers) {
				var type = pair.Key;
				var pubs = pair.Value;
				dynamic subs = MessageBus.subscribers[type];
			
			
				GUI.color = (k%2==0) ? Color.white : new Color(p,p,p);
				GUILayout.BeginVertical("box");  {
					GUILayout.Label($"Channels of {type}:");
					int i = 0;
					foreach (dynamic pair2 in pubs) {
						string path = pair2.Key;
						dynamic pub = pair2.Value;
						dynamic sub = subs.ContainsKey(path) ? subs[path] : null;

						GUI.color = (i % 2 == 0) ? Color.white : new Color(.8f, .8f, .8f);
						GUILayout.BeginVertical("box"); {
							GUILayout.Label($"publisher @ {path} with {(sub != null ? sub.Count : 0)} subscribers");
							if (pub.Latched && v != null) {
								float alpha = v[(object)pub];

								bool vis = v.visualizeByDefault ? true : alpha > 0;

								vis = GUILayout.Toggle(vis, "Visualize?");
								if (alpha <= 0) { alpha = .5f; }

								if (!vis) { alpha = 0; }
								else { alpha = GUILayout.HorizontalSlider(alpha, 0, 1); }

								v[(object)pub] = alpha;
							}

						} GUILayout.EndVertical();
						i++;
					}

				} GUILayout.EndVertical();


				k++;
			}

		} GUILayout.EndScrollView();
	}


}

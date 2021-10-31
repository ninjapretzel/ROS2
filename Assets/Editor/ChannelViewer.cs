using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class ChannelViewer : EditorWindow {
	
	[MenuItem("Window/ChannelViewer")]
	public static void Init() {
		ChannelViewer w = EditorWindow.GetWindow<ChannelViewer>();
		
		w.Show();
	}

	void Reload() {
		if (alphas == null) { alphas = new Dictionary<string, float>(); }
		
	}

	static Dictionary<string, float> alphas;
	Vector2 scroll;
	float defaultAlpha = .5f;

	public ChannelViewer() {
	}
	public static readonly Color[] CS = new Color[] {
		new Color(1f, .8f, .8f),
		new Color(.8f, 1f, .8f),
		new Color(.8f, .8f, 1f),
		new Color(.8f, 1f, 1f),
		new Color(1f, .8f, 1f),
		new Color(1f, 1f, .8f),
	};
	void OnGUI() {
		int k = 0;

		scroll = GUILayout.BeginScrollView(scroll, false, true); {

			IEnumerable<KeyValuePair<System.Type, IEnumerable>> publishers = MessageBus.publishers;
			List<KeyValuePair<System.Type, IEnumerable>> list = publishers.ToList();
			list.Sort((a,b)=>{return a.Key.Name.CompareTo(b.Key.Name);});
			publishers = list;
			var v = Visualizer.instance;
			bool vbd = v?.visualizeByDefault ?? false;
			bool vbd2 = vbd;
			if (v != null) {
				vbd2 = GUILayout.Toggle(v.visualizeByDefault, "Visualize by default?");
				v.visualizeByDefault = vbd2;
				GUILayout.Label("Alpha:");
				defaultAlpha = GUILayout.HorizontalSlider(defaultAlpha, 0, 1);
			}
			bool vbdToggled = vbd != vbd2;	

			foreach (var pair in publishers) {
				var type = pair.Key;
				var pubs = pair.Value;
				dynamic subs = MessageBus.subscribers[type];
			
				
				GUI.color = CS[k % CS.Length];
				GUILayout.BeginVertical("box");  {
					GUILayout.Label($"Channels of {type}:");
					int i = 0;
					foreach (dynamic pair2 in pubs) {
						string path = pair2.Key;
						dynamic pub = pair2.Value;
						dynamic sub = subs.ContainsKey(path) ? subs[path] : null;

						GUI.color = CS[i % CS.Length];
						GUILayout.BeginVertical("box"); {
							GUILayout.Label($"publisher @ {path} with {(sub != null ? sub.Count : 0)} subscribers");
							if (pub.Latched && v != null) {
								float alpha = v[(object)pub];

								bool vis = v.visualizeByDefault ? true : alpha > 0;

								vis = GUILayout.Toggle(vis, "Visualize?");
								if (alpha <= 0) { alpha = .5f; }

								if (!vis) { alpha = 0; }
								else { alpha = GUILayout.HorizontalSlider(alpha, 0, 1); }
								if (vbdToggled) {
									alpha = vbd2 ? .5f : 0f;
								}

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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading;
using UnityEditor;

/// <summary> Keeps track of what message types have been initialized so far. </summary>
public static class MessageBus {
	public static readonly IDictionary<Type, IEnumerable> publishers = new ConcurrentDictionary<Type, IEnumerable>();
	public static readonly IDictionary<Type, IEnumerable> subscribers = new ConcurrentDictionary<Type, IEnumerable>();
	public static void Register(Type t, IEnumerable pubs, IEnumerable subs) {
		publishers[t] = pubs;
		subscribers[t] = subs;
	}

	private static ConcurrentQueue<Action> todo = new ConcurrentQueue<Action>();
	public static void RunOnMain(Action a) {
		todo.Enqueue(a);
	}

	public static Thread running;
	public static readonly bool init = Init();
	private static bool Init() {
		running = new Thread(Run);
		running.Start();
		return true;
	}
	private static void Run() {
		while (true) {
			try {
				RunQueuedActions();
				foreach (var pair in subscribers) {
					Type key = pair.Key;
					IEnumerable allSubs = pair.Value;
					foreach (dynamic pair2 in allSubs) { 
						string channel = pair2.Key;
						IEnumerable subs = pair2.Value;
						foreach (dynamic sub in subs) {
							sub.Run();
						}
					}
				}


			}
			catch (ThreadAbortException) { }
			catch (Exception e) {
				Debug.LogWarning($"Error in run loop: {e}"); 
				//RunOnMain(() => {
				//});
			}
		}
	}

	private static void RunQueuedActions() {
		while (!todo.IsEmpty) {
			Action a;
			if (todo.TryDequeue(out a)) {
				try {
					a();
				} catch (Exception e) {
					Debug.LogError($"MessageBus.Step(): Error during step: {e.GetType()} /\n{e}");
				}
			}
		}
	}

}

public interface IPub<T> {
	void Publish(T t);
}
public interface ISub<T> { 
	void Unsubscribe();
}

public static class MessageBus<T> {
	public class Publisher : IPub<T> {
		private readonly bool latched;
		private readonly string path;
		private bool hasValue = false;
		private T latchedValue;
		public bool HasValue { get { return hasValue; } }
		public bool Latched { get { return latched; } }
		public T LatchedValue { get {
				if (latched) { return latchedValue; }
				throw new Exception($"MessageBus<{typeof(T)}>.Publisher: Cannot get latched value from non-latched publisher.");
		} }
		internal Publisher(string path, bool latched = true) {
			this.path = path;
			this.latched = latched;
			latchedValue = default(T);
		}
		public void Publish(T t) {
			hasValue = true;
			if (subscribers.ContainsKey(path)) {
				foreach (var sub in subscribers[path]) { sub.On(t); }
			}
			if (latched) { latchedValue = t; }
		}
	}
	public class Subscriber : ISub<T> {
		private readonly Action<T> callback;
		private readonly string path;
		private readonly ConcurrentQueue<T> queue;
		internal Subscriber(string path, Action<T> callback) {
			this.callback = callback;
			this.path = path;
			queue = new ConcurrentQueue<T>();
		}
		internal void On(T t) { 
			queue.Enqueue(t);
		}
		internal void Run() {
			while (!queue.IsEmpty) {
				T t;
				if (queue.TryDequeue(out t)) {
					try {
						callback(t);
					} catch (Exception e) {
						Debug.LogError($"MessageBus<{typeof(T)}>.Run(): Error in callback with value {t}:\n{e}");
						//MessageBus.RunOnMain(()=>{ 
						//});
					}
				}
			}
		}
		public void Unsubscribe() {
			subscribers[path].Remove(this);
		}
	}
	
	private static readonly IDictionary<string, Publisher> publishers = new ConcurrentDictionary<string, Publisher>();
	private static readonly IDictionary<string, ConcurrentSet<Subscriber>> subscribers = new ConcurrentDictionary<string, ConcurrentSet<Subscriber>>(); 
	private static bool Init() { MessageBus.Register(typeof(T), publishers, subscribers); return true; }
	public static readonly bool ready = Init();

	public static Subscriber SubscribeTo(string path, Action<T> callback) {
		if (!subscribers.ContainsKey(path)) {
			subscribers[path] = new ConcurrentSet<Subscriber>();
		}
		Subscriber s = new Subscriber(path, callback);
		subscribers[path].Add(s);
		if (publishers.ContainsKey(path)) {
			var pub = publishers[path];
			if (pub.Latched && pub.HasValue) { s.On(pub.LatchedValue); }
		}
		return s;
	}

	public static Publisher PublishTo(string path, bool latched = true) {
		if (!publishers.ContainsKey(path)) { publishers[path] = new Publisher(path, latched); }
		return publishers[path];		
	}

}

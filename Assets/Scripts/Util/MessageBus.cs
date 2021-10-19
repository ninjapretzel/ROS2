using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

/// <summary> Keeps track of what message types have been initialized so far. </summary>
public static class MessageBus {
	public static readonly IDictionary<Type, IEnumerable> publishers = new ConcurrentDictionary<Type, IEnumerable>();
	public static readonly IDictionary<Type, IEnumerable> subscribers = new ConcurrentDictionary<Type, IEnumerable>();
	public static void Register(Type t, IEnumerable pubs, IEnumerable subs) {
		publishers[t] = pubs;
		subscribers[t] = subs;
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
		private T latchedValue;
		public bool Latched { get { return latched; } }
		public T LatchedValue { get {
				if (latched) { return latchedValue; }
				throw new Exception($"MessageBus<{typeof(T)}>.Publisher: Cannot get latched value from non-latched publisher.");
		} }
		internal Publisher(string path, bool latched = false) {
			this.path = path;
			this.latched = latched;
			latchedValue = default(T);
		}
		public void Publish(T t) {
			if (subscribers.ContainsKey(path)) {
				foreach (var sub in subscribers[path]) { sub.On(t); }
			}
			if (latched) { latchedValue = t; }
		}
	}
	public class Subscriber : ISub<T> {
		private readonly Action<T> callback;
		private readonly string path;
		internal Subscriber(string path, Action<T> callback) {
			this.callback = callback;
			this.path = path;
		}
		internal void On(T t) { 
			try {
				callback(t);
			} catch (Exception e) {
				Debug.LogError($"Error in callback for Subscriber{typeof(T)}: {e.GetType()}\n{e}");
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
			if (pub.Latched) { s.On(pub.LatchedValue); }
		}
		return s;
	}

	public static Publisher PublishTo(string path, bool latched = false) {
		if (!publishers.ContainsKey(path)) { publishers[path] = new Publisher(path, latched); }
		return publishers[path];		
	}

}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

/// <summary> Keeps track of what message types have been initialized so far. </summary>
internal static class MessageBus {
	internal static readonly IDictionary<Type, object> publishers = new ConcurrentDictionary<Type, object>();
	internal static readonly IDictionary<Type, object> subscribers = new ConcurrentDictionary<Type, object>();
	internal static void Register(Type t, object pubs, object subs) {
		publishers[t] = pubs;
		subscribers[t] = subs;
	}
}

public static class MessageBus<T> {
	public class Publisher {
		private string path;
		internal Publisher(string path) {
			this.path = path;
		}
		public void Publish(T t) {
			if (subscribers.ContainsKey(path)) {
				foreach (var sub in subscribers[path]) { sub.On(t); }
			}
		}
	}
	public class Subscriber {
		private Action<T> callback;
		internal Subscriber(Action<T> callback) {
			this.callback = callback;
		}
		internal void On(T t) { 
			try {
				callback(t);
			} catch (Exception e) {
				Debug.LogError($"Error in callback for Subscriber{typeof(T)}: {e.GetType()}\n{e}");
			}
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
		Subscriber s = new Subscriber(callback);
		subscribers[path].Add(s);
		return s;
	}

	public static Publisher PublishTo(string path) {
		if (!publishers.ContainsKey(path)) { publishers[path] = new Publisher(path); }
		return publishers[path];		
	}

}

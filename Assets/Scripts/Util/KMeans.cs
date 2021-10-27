using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Rng = System.Random;

public static class KMeans {
	
	public static int[] Cluster(Vector4[] raw, int clusters, int? seed = null) {
		var data = raw;
		bool changed = true;
		bool success = true;
		int[] clustering = InitClustering(data.Length, clusters, seed);
		Vector4[] means = new Vector4[clusters];
		int maxCount = raw.Length * 100;
		int ct = 0;

		while (changed && success && ct < maxCount) {
			ct++;
			success = UpdateMeans(data, clustering, means);
			changed = UpdateClustering(data, clustering, means);
		}

		return clustering;
	}

	public static Vector4[] Normalize(Vector4[] data) {
		Vector4 avg = Vector4.zero;
		Vector4[] ns = new Vector4[data.Length];
		for(int i = 0; i < data.Length; i++) { 
			avg += data[i]; 
			ns[i] = data[i];
		}
		avg /= data.Length;
		Vector4 sd = Vector4.zero;
		for(int i = 0; i < data.Length; i++) {
			Vector4 d = (ns[i] - avg);
			sd += Vector4.Scale(d,d);
		}
		sd /= data.Length;
		if (sd.x == 0) { sd.x = 1; }
		if (sd.y == 0) { sd.y = 1; }
		if (sd.z == 0) { sd.z = 1; }
		if (sd.w == 0) { sd.w = 1; }

		sd = new Vector4(1f/sd.x, 1f/sd.y, 1f/sd.z, 1f/sd.w);
		for (int i = 0; i < data.Length; i++) {
			ns[i] = Vector4.Scale(ns[i] - avg, sd);
		}

		return ns;
	}

	public static int[] InitClustering(int n, int c, int? seed = null) {
		Rng random = seed.HasValue ? new Rng(seed.Value) : new Rng();
		int[] clustering = new int[n];
		for (int i = 0; i < c; i++) { clustering[i] = i; }
		for (int i = c; i < n; i++) { clustering[i] = random.Next(0, c); }
		return clustering;
	}
	
	public static bool UpdateMeans(Vector4[] data, int[] clustering, Vector4[] means) {
		int c = means.Length;
		int[] counts = new int[c];
		for (int i = 0; i < clustering.Length; i++) { counts[clustering[i]]++; }

		for (int i = 0; i < c; i++) { 
			if (counts[i] == 0) { return false; } 
		}

		for (int i = 0; i < means.Length; i++) { means[i] = Vector4.zero; }
		for (int i = 0; i < data.Length; i++) {
			int cluster = clustering[i];
			means[cluster] += data[i];
		}
		for (int k = 0; k < means.Length; k++) {
			Vector4 mc = Vector4.one * (1f/counts[k]);
			
			means[k] = Vector4.Scale(means[k], mc);
		}

		return true;
	}

	public static bool UpdateClustering(Vector4[] data, int[] clustering, Vector4[] means) {
		int c = means.Length;
		bool changed = false;
		int[] newClustering = new int[clustering.Length];
		Array.Copy(clustering, newClustering, clustering.Length);
		float[] distances = new float[c];
		
		for (int i = 0; i < data.Length; i++) {
			for (int k = 0; k < c; k++) {
				distances[k] = (data[i]-means[k]).sqrMagnitude;
			}
			int id = FindMin(distances);
			if (id != newClustering[i]) { changed = true; newClustering[i] = id; }
		}

		if (!changed) { return false; }

		int[] counts = new int[c];
		for (int i = 0; i < data.Length; i++) {
			counts[newClustering[i]]++;
		}
		for (int i = 0; i < counts.Length; i++) {
			if (counts[i] == 0) { return false; }
		}
		Array.Copy(newClustering, clustering, clustering.Length);
		return true;
	}

	public static int FindMin(float[] distances) {
		int min = -1;
		float mv = float.PositiveInfinity;
		for (int i = 0; i < distances.Length; i++) {
			if (distances[i] < mv) { mv = distances[i]; min = i; }
		}
		return min;
	}


}

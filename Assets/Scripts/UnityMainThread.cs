﻿using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// allow to add events to the main thread of monobehaviour
/// </summary>
class UnityMainThread : MonoBehaviour {
	internal static UnityMainThread wkr;
	readonly Queue<Action> jobs = new Queue<Action>();

	void Awake() => wkr = this;

	void Update() {
		while (jobs.Count > 0) {
			jobs.Dequeue().Invoke();
		}
	}

	internal void AddJob(Action newJob) => jobs.Enqueue(newJob);
}

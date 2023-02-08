using System.Collections;
using UnityEngine;
namespace Proyecto26 {
	public static class StaticCoroutine {

		static CoroutineHolder _runner;
		static CoroutineHolder Runner {
			get {
				if (_runner == null) {
					_runner = new GameObject("Static Coroutine RestClient").AddComponent<CoroutineHolder>();
					Object.DontDestroyOnLoad(_runner);
				}
				return _runner;
			}
		}

		public static Coroutine StartCoroutine(IEnumerator coroutine) => Runner.StartCoroutine(coroutine);
		class CoroutineHolder : MonoBehaviour { }
	}
}

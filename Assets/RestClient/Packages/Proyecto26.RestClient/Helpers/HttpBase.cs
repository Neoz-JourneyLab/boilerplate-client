using Proyecto26.Common;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace Proyecto26 {
	public static class HttpBase {
		public static IEnumerator CreateRequestAndRetry(RequestHelper options, Action<RequestException, ResponseHelper> callback) {
			int retries = 0;
			do {
				using (UnityWebRequest request = CreateRequest(options)) {
					yield return request.SendWebRequestWithOptions(options);
					ResponseHelper response = request.CreateWebResponse();
					if (request.IsValidRequest(options)) {
						DebugLog(options.EnableDebug,
							string.Format("RestClient - Response\nUrl: {0}\nMethod: {1}\nStatus: {2}\nResponse: {3}",
								options.Uri,
								options.Method,
								request.responseCode,
								options.ParseResponseBody ? response.Text : "body not parsed"),
							false);
						callback(null, response);
						break;
					}
					if (!options.IsAborted && retries < options.Retries) {
						yield return new WaitForSeconds(options.RetrySecondsDelay);
						retries++;
						if (options.RetryCallback != null) options.RetryCallback(CreateException(options, request), retries);
						DebugLog(options.EnableDebug, string.Format("RestClient - Retry Request\nUrl: {0}\nMethod: {1}", options.Uri, options.Method), false);
					} else {
						RequestException err = CreateException(options, request);
						DebugLog(options.EnableDebug, err, true);
						callback(err, response);
						break;
					}
				}
			} while (retries <= options.Retries);
		}

		static UnityWebRequest CreateRequest(RequestHelper options) {
			string url = options.Uri.BuildUrl(options.Params);
			DebugLog(options.EnableDebug, string.Format("RestClient - Request\nUrl: {0}", url), false);
			if (options.FormData is WWWForm && options.Method == UnityWebRequest.kHttpVerbPOST) return UnityWebRequest.Post(url, options.FormData);
			return new UnityWebRequest(url, options.Method);
		}

		static RequestException CreateException(RequestHelper options, UnityWebRequest request) =>
		new RequestException(request.error,
			request.isHttpError,
			request.isNetworkError,
			request.responseCode,
			options.ParseResponseBody ? request.downloadHandler.text : "body not parsed");

		static void DebugLog(bool debugEnabled, object message, bool isError) {
			if (debugEnabled) {
				if (isError) Debug.LogError(message);
				else Debug.Log(message);
			}
		}

		public static IEnumerator DefaultUnityWebRequest(RequestHelper options, Action<RequestException, ResponseHelper> callback) =>
		CreateRequestAndRetry(options, callback);

		public static IEnumerator DefaultUnityWebRequest<TResponse>(RequestHelper options, Action<RequestException, ResponseHelper, TResponse> callback) =>
		CreateRequestAndRetry(options,
			(err, res) => {
				TResponse body = default;
				try {
					if (err == null && res.Data != null && options.ParseResponseBody) body = JsonUtility.FromJson<TResponse>(res.Text);
				} catch (Exception error) {
					DebugLog(options.EnableDebug, string.Format("RestClient - Invalid JSON format\nError: {0}", error.Message), true);
					err = new RequestException(error.Message);
				} finally {
					callback(err, res, body);
				}
			});

		public static IEnumerator DefaultUnityWebRequest<TResponse>(RequestHelper options, Action<RequestException, ResponseHelper, TResponse[]> callback) =>
		CreateRequestAndRetry(options,
			(err, res) => {
				TResponse[] body = default;
				try {
					if (err == null && res.Data != null && options.ParseResponseBody) body = JsonHelper.ArrayFromJson<TResponse>(res.Text);
				} catch (Exception error) {
					DebugLog(options.EnableDebug, string.Format("RestClient - Invalid JSON format\nError: {0}", error.Message), true);
					err = new RequestException(error.Message);
				} finally {
					callback(err, res, body);
				}
			});
	}
}

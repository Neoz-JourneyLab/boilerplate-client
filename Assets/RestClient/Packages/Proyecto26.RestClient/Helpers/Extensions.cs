using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
namespace Proyecto26.Common {
  public static class Extensions {
    /// <summary>
    ///   Create an object with the response of the server
    /// </summary>
    /// <param name="request">An UnityWebRequest object.</param>
    /// <returns>An object with the response.</returns>
    public static ResponseHelper CreateWebResponse(this UnityWebRequest request) => new ResponseHelper(request);

    /// <summary>
    ///   Validate if the request is OK with the current options
    /// </summary>
    /// <param name="request">An UnityWebRequest object.</param>
    /// <param name="options">The options of the request.</param>
    /// <returns>A boolean that indicates if the request is valid.</returns>
    public static bool IsValidRequest(this UnityWebRequest request, RequestHelper options) =>
    request.isDone && !request.isNetworkError && (!request.isHttpError || options.IgnoreHttpException);

    /// <summary>
    ///   Escapes characters in a string to ensure they are URL-friendly
    /// </summary>
    /// <param name="queryParam">A query string param</param>
    /// <returns>Escaped query string param.</returns>
    public static string EscapeURL(this string queryParam) {
      #if UNITY_2018_3_OR_NEWER
      return UnityWebRequest.EscapeURL(queryParam);
      #else
            return WWW.EscapeURL(queryParam);
      #endif
    }

    /// <summary>
    ///   Generate the url and escape params
    /// </summary>
    /// <param name="uri">The URI of the resource to retrieve via HTTP.</param>
    /// <param name="queryParams">Query string parameters.</param>
    /// <returns>The full url with query string params.</returns>
    public static string BuildUrl(this string uri, Dictionary<string, string> queryParams) {
      string url = uri;
      Dictionary<string, string> defaultParams = RestClient.DefaultRequestParams;
      if (defaultParams.Any() || queryParams.Any()) {
        Dictionary<string, string>.KeyCollection urlParamKeys = queryParams.Keys;
        url += (url.Contains("?") ? "&" : "?") + string.Join("&", queryParams.Concat(defaultParams.Where(p => !urlParamKeys.Contains(p.Key))).Select(p => string.Format("{0}={1}", p.Key, p.Value.EscapeURL())).ToArray());
      }
      return url;
    }
  }
}

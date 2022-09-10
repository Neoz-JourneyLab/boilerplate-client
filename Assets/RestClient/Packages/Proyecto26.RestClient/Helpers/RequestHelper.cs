using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
namespace Proyecto26 {
  public partial class RequestHelper {

    Dictionary<string, string> _headers;

    Dictionary<string, string> _params;

    /// <summary>
    ///   Defines the target URL for the UnityWebRequest to communicate with
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    ///   Defines the HTTP verb used by this UnityWebRequest, such as GET or POST.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    ///   The data to send to the server, encoding the body with JsonUtility
    /// </summary>
    public object Body { get; set; }

    /// <summary>
    ///   The data serialized as string to send to the web server (Using other tools instead of JsonUtility)
    /// </summary>
    public string BodyString { get; set; }

    /// <summary>
    ///   The data as byte array to send to the server
    /// </summary>
    public byte[] BodyRaw { get; set; }

    /// <summary>
    ///   Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    ///   Override the content type of the request manually
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    ///   The number of retries of the request
    /// </summary>
    public int Retries { get; set; }

    /// <summary>
    ///   Seconds of delay to make a retry
    /// </summary>
    public float RetrySecondsDelay { get; set; }

    /// <summary>
    ///   A callback executed before to retry a request
    /// </summary>
    public Action<RequestException, int> RetryCallback { get; set; }

    /// <summary>
    ///   Enable logs of the requests for debug mode
    /// </summary>
    public bool EnableDebug { get; set; }

    /// <summary>
    ///   Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
    /// </summary>
    public bool? ChunkedTransfer { get; set; }

    /// <summary>
    ///   Determines whether this UnityWebRequest will include Expect: 100-Continue in its outgoing request headers. (Default:
    ///   true).
    /// </summary>
    public bool? UseHttpContinue { get; set; } = true;

    /// <summary>
    ///   Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit
    ///   Exceeded” system error.
    /// </summary>
    public int? RedirectLimit { get; set; }

    /// <summary>
    ///   Prevent to catch http exceptions
    /// </summary>
    public bool IgnoreHttpException { get; set; }

    /// <summary>
    ///   The form data to send to the web server using WWWForm
    /// </summary>
    public WWWForm FormData { get; set; }

    /// <summary>
    ///   The form data to send to the web server using Dictionary
    /// </summary>
    public Dictionary<string, string> SimpleForm { get; set; }

    /// <summary>
    ///   The form data to send to the web server using IMultipartFormSection
    /// </summary>
    public List<IMultipartFormSection> FormSections { get; set; }

    /// <summary>
    ///   Holds a reference to the UploadHandler object which manages body data to be uploaded to the remote server.
    /// </summary>
    public UploadHandler UploadHandler { get; set; }

    /// <summary>
    ///   Holds a reference to a DownloadHandler object, which manages body data received from the remote server by this
    ///   UnityWebRequest.
    /// </summary>
    public DownloadHandler DownloadHandler { get; set; }

    /// <summary>
    ///   The HTTP headers added manually to send with the request
    /// </summary>
    public Dictionary<string, string> Headers {
      get {
        if (_headers == null) _headers = new Dictionary<string, string>();
        return _headers;
      }
      set => _headers = value;
    }
    /// <summary>
    ///   The HTTP query string params to send with the request
    /// </summary>
    public Dictionary<string, string> Params {
      get {
        if (_params == null) _params = new Dictionary<string, string>();
        return _params;
      }
      set => _params = value;
    }

    /// <summary>
    ///   Whether to parse the response body as JSON or not. Note: parsing a large non-text file will have severe performance
    ///   impact.
    /// </summary>
    public bool ParseResponseBody { get; set; } = true;

    #if UNITY_2018_1_OR_NEWER
    /// <summary>
    ///   Holds a reference to a CertificateHandler object, which manages certificate validation for this UnityWebRequest.
    /// </summary>
    public CertificateHandler CertificateHandler { get; set; }
    #endif
  }
}

using UnityEngine;
public class UniClipboard {
  static IBoard _board;
  static IBoard board {
    get {
      if (_board == null) {
        #if UNITY_EDITOR
        _board = new EditorBoard();
        #elif UNITY_ANDROID
                _board = new AndroidBoard();
        #elif UNITY_IOS
                _board = new IOSBoard ();
        #endif
      }
      return _board;
    }
  }

  public static void SetText(string str) => board.SetText(str);

  public static string GetText() => board.GetText();
}

interface IBoard {
  void SetText(string str);
  string GetText();
}

class EditorBoard : IBoard {
  public void SetText(string str) => GUIUtility.systemCopyBuffer = str;

  public string GetText() => GUIUtility.systemCopyBuffer;
}

#if UNITY_IOS
class IOSBoard : IBoard {
    [DllImport("__Internal")]
    static extern void SetText_ (string str);
    [DllImport("__Internal")]
    static extern string GetText_();

    public void SetText(string str){
        if (Application.platform != RuntimePlatform.OSXEditor) {
            SetText_ (str);
        }
    }

    public string GetText(){
        return GetText_();
    }
}
#endif

#if UNITY_ANDROID
class AndroidBoard : IBoard {

  readonly AndroidJavaClass cb = new AndroidJavaClass("jp.ne.donuts.uniclipboard.Clipboard");

  public void SetText(string str) {
    Debug.Log("Set Text At AndroidBoard: " + str);
    cb.CallStatic("setText", str);
  }

  public string GetText() => cb.CallStatic<string>("getText");
}
#endif

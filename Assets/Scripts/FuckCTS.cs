using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FuckCTS : MonoBehaviour
{
  [SerializeField] TMP_Text timer;
  [SerializeField] TMP_Text expire;
  [SerializeField] TMP_Text uuid;
  [SerializeField] LayoutElement qr;

  DateTime expiration;
  string guid = Guid.NewGuid().ToString();

  private void Start() {
    expiration = DateTime.Now.AddMinutes(44);
    InvokeRepeating(nameof(SetText), 1, 1);
  }

  void SetText() {
    TimeSpan span = expiration - DateTime.Now;
    timer.text = span.Hours.ToString("00") + ":" + span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
    expire.text = "Expire à " + expiration.Hour.ToString("00") + ":" + expiration.Minute.ToString("00") + " le " +
      expiration.Day.ToString("00") + "/" + expiration.Month.ToString("00") + "/" + expiration.Year.ToString("00");
    uuid.text = expiration.Day.ToString("00") + "/" + expiration.Month.ToString("00") + "/" + expiration.Year.ToString("00") + " " +
     expiration.Hour.ToString("00") + ":" + expiration.Minute.ToString("00") + ":" + expiration.Second.ToString("00") + "\n" + 
     "V0.3_" + guid;
  }

  int pref_heigh = 420;
  public void Swap() {
    if (pref_heigh == 420) pref_heigh = 610;
    else pref_heigh = 420;
    qr.preferredHeight = pref_heigh;
  } 
}

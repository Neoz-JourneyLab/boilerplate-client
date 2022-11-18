using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using static Crypto;

public class MainClass : MonoBehaviour {
  const string urlRealm = "https://auth-fr.herokuapp.com";
  public static Console console;
  [SerializeField] Console consoleGO;
  void Start() {
    console = consoleGO;
  }
}

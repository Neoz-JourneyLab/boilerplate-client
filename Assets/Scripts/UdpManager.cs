using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System;
using System.Security.Cryptography;

public class UdpManager : MonoBehaviour {
  UdpClient udpClient;
  IPEndPoint serverEndPoint;
  // Start is called before the first frame update
  void Start() {
    udpClient = new UdpClient();
    serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000);
    InvokeRepeating(nameof(Test), 1, 1);
    ReceiveAck();
  }

  void Test() {
    StringBuilder sb = new StringBuilder();
    for (int i = 0; i < 200000; i++) {
      sb.Append(i.ToString() + ";");
    }
    Send(sb.ToString());
  }

  void Send(string message) {
    byte[] data = Encoding.UTF8.GetBytes(message);
    udpClient.Send(data, data.Length, serverEndPoint);
  }

  void ReceiveAck() {
    udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
  }

  void OnReceive(IAsyncResult result) {
    try {
      // Fin de la réception et récupération des données
      byte[] receivedData = udpClient.EndReceive(result, ref serverEndPoint);
      string receivedMessage = Encoding.UTF8.GetString(receivedData);
      Debug.Log("Message reçu du serveur: " + Convert.ToBase64String(SHA256.Create().ComputeHash(receivedData)));
    }
    catch (Exception e) {
      Debug.LogError("Erreur de réception: " + e.Message);
    }
    finally {
      // Continuer la réception en continu
      ReceiveAck();
    }
  }

  void OnDestroy() {
    udpClient.Close();
  }
}

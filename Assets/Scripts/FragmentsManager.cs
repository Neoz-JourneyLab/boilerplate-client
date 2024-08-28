using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class FragmentsManager : MonoBehaviour {

  //le contenu de chaque fragment d'un message
  public class Fragment {
    public uint fragment_id;
    public byte[] buffer;
  }

  //la liste des tous les messages et de leurs fragments
  public static Dictionary<ushort, List<Fragment>> pending_fragments = new Dictionary<ushort, List<Fragment>>();
  //pour coloriser la liste des pings reçus
  static List<int> pings_id_received_list = new List<int>();

  //lorsqu'un fragment avec le fanion "dernier fragment" est reçu, on tente de recomposer le message.
  public static void ReformFragment(ushort message_id, byte fragment_type) {
    if (!pending_fragments.ContainsKey(message_id)) return;
    List<byte> buffer = new List<byte>();
    pending_fragments[message_id] = pending_fragments[message_id].OrderBy(x => x.fragment_id).ToList();
    for (int fid = 0; fid < pending_fragments[message_id].Count; fid++) {
      //Si un fragment du message est manquant, on annule et on re-tente 1 secondes plus tard
      if (pending_fragments[message_id].All(x => x.fragment_id != fid)) {
        new Thread(() => {
          Thread.Sleep(1000);
          UnityMainThread.wkr.AddJob(() => {
            ReformFragment(message_id, fragment_type);
          });
        }).Start();
        return;
      }
      Fragment f = pending_fragments[message_id][fid];
      buffer.AddRange(f.buffer);
    }
    //le Vecteur d'initialisation (IV) compose les 16 premiers octets du message
    byte[] IV = new byte[16];
    //le reste est le message chiffré en AES avec l'IV et le hash du secret partagé de DiffieHellman
    byte[] cipher = new byte[buffer.Count - 16];
    Buffer.BlockCopy(buffer.ToArray(), 0, IV, 0, 16);
    Buffer.BlockCopy(buffer.ToArray(), 16, cipher, 0, cipher.Length);

    string decrypted = AESManager.DecryptAES(cipher, DiffieHellman.__SHARED__hash_DiffieHellman__, IV);
    pending_fragments.Remove(message_id);
    if (fragment_type == 0) {
      Debug.Log("MESSAGE RECU DU SERVEUR : " + decrypted);
    } else if (fragment_type == 1) { //réception d'un ping
      byte[] r_IV = new byte[16];
      int ping_id = int.Parse(decrypted);
      int row = ping_id / 53;
      int col = ping_id % 53;

      Debug.Log("PING ID RECU " + ping_id);
      //colorisation de la case "ping id reçu"
      if (pings_id_received_list.Contains(ping_id)) {
        FindObjectOfType<UdpManager>().pings_id_received.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 1);
      } else {
        FindObjectOfType<UdpManager>().pings_id_received.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);
        pings_id_received_list.Add(ping_id);
      }
    }
  }
}

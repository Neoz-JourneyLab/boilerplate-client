using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;
using UnityEngine.UI;
using static FragmentsManager;
using System.Threading;

public class UdpManager : MonoBehaviour {
  //paramètres UDP
  UdpClient udpClient;
  IPEndPoint serverEndPoint;
  readonly int MTU = 1200;

  [SerializeField] internal GameObject pings_id_received;
  [SerializeField] GameObject sequence_id_received;
  [SerializeField] GameObject ping_id_sent;
  [SerializeField] GameObject sequence_id_sent;
  [SerializeField] GameObject ack_sequence_id_sent;
  [SerializeField] GameObject ack_ping_id_sent;

  //pour afficher les cases colorées sur les pings envoyés
  Dictionary<ushort, ushort> sequence_id_ping_id = new Dictionary<ushort, ushort>();
  List<int> pings_id_sent_list = new List<int>();
  ushort ping_counter = 0;

  //paramètres d'accusé de réception du serveur-client
  ushort cur_message_id = 0;
  readonly ushort[] acks = new ushort[32];
  ushort last_ack = 65535;
  ushort sequence_id = 0;
  bool received_once = false;

  Dictionary<ushort, PacketSent> packets_sent = new Dictionary<ushort, PacketSent>();


  // Start is called before the first frame update
  void Start() {
    DiffieHellman.GenerateDiffieHellman();

    udpClient = new UdpClient();
    serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 69);
    StartListenUdp();
    Send(DiffieHellman.PUBLIC_DiffieHellman, 2);
    InvokeRepeating(nameof(Ping), 0.3f, 0.25f);
  }

  //envoie un message de type 1 (ping) et colore la case associée à ce ping ID
  //En vert si c'est la première fois qu'on l'envoie, et en bleu si c'est un renvoi.
  void Ping() {
    ushort ping_id = ping_counter++;
    Send(ping_id.ToString(), 1, ping_id);
  }

  //détecte les input pour envoyer des messages
  void Update() {
    if (Input.GetKeyDown(KeyCode.Space))
      Send("Mais, vous savez, moi je ne crois pas\nqu'il y ait de bonne ou de mauvaise situation.\nMoi, si je devais résumer ma vie aujourd'hui avec vous,\nje dirais que c'est d'abord des rencontres,\nDes gens qui m'ont tendu la main,\npeut-être à un moment où je ne pouvais pas, où j'étais seul chez moi.\nEt c'est assez curieux de se dire que les hasards,\nles rencontres forgent une destinée...\nParce que quand on a le goût de la chose,\nquand on a le goût de la chose bien faite,\nLe beau geste, parfois on ne trouve pas l'interlocuteur en face,\nje dirais, le miroir qui vous aide à avancer.\nAlors ce n'est pas mon cas, comme je le disais là,\npuisque moi au contraire, j'ai pu ;\nEt je dis merci à la vie, je lui dis merci,\nje chante la vie, je danse la vie... Je ne suis qu'amour!\nEt finalement, quand beaucoup de gens aujourd'hui me disent :\n\"Mais comment fais-tu pour avoir cette humanité ?\",\nEh bien je leur réponds très simplement,\nje leur dis que c'est ce goût de l'amour,\nCe goût donc qui m'a poussé aujourd'hui\nà entreprendre une construction mécanique,\nMais demain, qui sait, peut-être simplement\nà me mettre au service de la communauté,\nà faire le don, le don de soi...");
  }

  //Envoie un message de type texte
  void Send(string message, byte type = 0, ushort ping_id = 0) {
    byte[] message_bin = Encoding.UTF8.GetBytes(message);
    Send(message_bin, type, false, ping_id);
  }

  //envoie un message de type tableau d'octet
  void Send(byte[] message_bin, byte type = 0, bool bypass_IV = false, ushort ping_id = 0) {
    //génère un IV pour le message (sauf pour l'établissement de connexion ou notre clé publique est envoyée en clair)
    byte[] initializationVector = (type == 2 || bypass_IV) ? new byte[0] : AESManager.GenerateRandomBytes(16);
    byte[] cipher = (type == 2 || bypass_IV) ? message_bin : AESManager.EncryptAES(message_bin, DiffieHellman.__SHARED__hash_DiffieHellman__, initializationVector);
    byte[] encrypted_bin = new byte[cipher.Length + initializationVector.Length];

    //place l'IV au début du message, et ensuite place le message chiffré
    Buffer.BlockCopy(initializationVector, 0, encrypted_bin, 0, initializationVector.Length);
    Buffer.BlockCopy(cipher, 0, encrypted_bin, initializationVector.Length, cipher.Length);

    uint fragments_count = (uint)Math.Ceiling(encrypted_bin.Length / (float)MTU);
    /*
     * Longueur du fragment_id :
0 = no fragment id
1 = 8 bits fid (255 frag max)
2 = 16 bits fid (65 535 frag max)
3 = 32 bits fid (4 294 967 295 max)
     */
    byte frag_len_val = (byte)(fragments_count == 1 ? 0 : (fragments_count <= 255 ? 1 : (fragments_count <= 65535 ? 2 : 3)));
    ushort frag_len = (ushort)((frag_len_val & 0b00000011) << 6);
    //la taille de l'entête (header) dépend de la taille du fragment ID
    ushort header_size = (ushort)(15 + frag_len_val + (frag_len_val == 3 ? 1 : 0));
    /*
0: message
1 : ping
2: connect
3: disconnect
     */
    ushort frag_type = (ushort)((type & 0b00000011) << 3);
    if (cur_message_id == ushort.MaxValue) cur_message_id = 0;
    ushort message_id = cur_message_id++;

    //découpage du message en fragments compatible avec le MTU
    for (uint f = 0; f < fragments_count; f++) {
      byte is_last_frag = (byte)(((byte)((f == (fragments_count - 1)) ? 1 : 0) & 0b00000001) << 5);
      byte flags = (byte)(0 ^ frag_len | is_last_frag | frag_type);
      byte[] fragment = new byte[Math.Min(MTU, encrypted_bin.Length - f * MTU)];
      Buffer.BlockCopy(encrypted_bin, (int)(MTU * f), fragment, 0, fragment.Length);
      byte[] buffer = new byte[header_size + fragment.Length];

      //0 - 3 : checksum
      //4 - 5 : seq id
      //6 - 7 : message id
      //8 : flags [FL L FT 000]
      //9 - 10 : ack id
      //11 - 14 : ack bitfield
      //15 - 15>18 : fragment id

      //Message : IV + EV + DATA

      if (sequence_id == ushort.MaxValue) sequence_id = 0;
      ushort packet_id = sequence_id++;

      if (type == 1) {
        sequence_id_ping_id.Add(packet_id, ping_id);
        int row_sequence = ping_id / 53;
        int col_sequence = ping_id % 53;
        UnityMainThread.wkr.AddJob(() => {
          if (pings_id_sent_list.Contains(ping_id)) {
            ping_id_sent.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 1);
          } else {
            ping_id_sent.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 0);
            pings_id_sent_list.Add(ping_id);
          }
        });
      }

      int row = packet_id / 53;
      int col = packet_id % 53;
      sequence_id_sent.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);

      Buffer.BlockCopy(new ushort[1] { packet_id }, 0, buffer, 4, 2); //sequence ID (packet ID)
      Buffer.BlockCopy(new ushort[1] { message_id }, 0, buffer, 6, 2);  //message ID
      buffer[8] = flags; //FLAG
      Buffer.BlockCopy(new ushort[1] { last_ack }, 0, buffer, 9, 2); //dernier ACK ID des messages recçu
      uint olds_aks = 0;
      for (int i = 1; i <= 32; i++) {
        int old_ack = last_ack - i;
        if (old_ack < 0) old_ack += 65536;
        if (acks.Contains((ushort)old_ack)) {
          olds_aks |= (uint)(1 << (i - 1));
        }
      }
      Buffer.BlockCopy(new uint[1] { olds_aks }, 0, buffer, 11, 4); //bitfield des 32 acks reçu précédents

      //fragment ID
      if (frag_len_val == 1) {
        Buffer.BlockCopy(new byte[1] { (byte)f }, 0, buffer, 15, 1);
      } else if (frag_len_val == 2) {
        Buffer.BlockCopy(new ushort[1] { (ushort)f }, 0, buffer, 15, 2);
      } else if (frag_len_val == 3) {
        Buffer.BlockCopy(new uint[1] { f }, 0, buffer, 15, 4);
      }

      //fragment du message
      Buffer.BlockCopy(fragment, 0, buffer, header_size, fragment.Length);

      //rajout de la checksum au début de l'entête
      uint checksum = Crc32.Compute(buffer);
      Buffer.BlockCopy(new uint[1] { checksum }, 0, buffer, 0, 4);

      //mise en mémoire des packets envoyé (pour attendre l'ack du serveur)
      packets_sent[packet_id] = new PacketSent(fragment, type);

      udpClient.Send(buffer, buffer.Length, serverEndPoint);
    }
  }

  void StartListenUdp() {
    udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
  }

  //lors de la réception d'un fragment du serveur
  void OnReceive(IAsyncResult result) {
    try {
      byte[] receivedData = udpClient.EndReceive(result, ref serverEndPoint);
      //extraction du checksum
      uint sent_checksum = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(receivedData, 0));
      //mise à 0 des 4 premiers octets (place du checksum)
      Buffer.BlockCopy(new uint[] { 0 }, 0, receivedData, 0, 4);
      //re-calcul du checksum
      uint received_checksum = Crc32.Compute(receivedData);
      if (received_checksum != sent_checksum) {
        Debug.Log("Checksum invalide : " + received_checksum + " vs " + sent_checksum);
        return;
      }
      //l'ID du packet contenant le fragment
      ushort r_sequence_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 4));
      //l'ID du message dont le fragment est issu
      ushort r_message_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 6));

      //fanions
      ushort r_flags = receivedData[8];
      byte r_fragment_len_flag = (byte)(r_flags & 0b11000000 >> 6);
      byte r_fragment_len = (byte)(r_fragment_len_flag + (r_fragment_len_flag == 3 ? 1 : 0));
      bool r_last_fragment = ((r_flags & 0b00100000) >> 5) != 0;
      byte r_fragment_type = (byte)((r_flags & 0b00011000) >> 3);
      ushort r_ack_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 9));
      uint r_ack_bitfield = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(receivedData, 11));

      //fragment ID (si nécéssaire, sinon 0)
      uint r_fragment_id = BinaryPrimitives.ReverseEndianness(r_fragment_len == 0 ? 0 :
        (r_fragment_len == 1 ? receivedData[15] :
        (r_fragment_len == 2 ? BitConverter.ToUInt16(receivedData, 15) :
        BitConverter.ToUInt32(receivedData, 15))));
      byte[] message_bin = new byte[receivedData.Length - (15 + r_fragment_len)];

      //extraction du cipher (message chiffré du fragment)
      Buffer.BlockCopy(receivedData, 15 + r_fragment_len, message_bin, 0, message_bin.Length);

      //si c'est un fragment de ping ou de message, on stocke le fragment
      if (r_fragment_type == 0 || r_fragment_type == 1) {
        Fragment fragment = new Fragment() { buffer = message_bin, fragment_id = r_fragment_id };
        if (pending_fragments.ContainsKey(r_message_id)) {
          pending_fragments[r_message_id].Add(fragment);
        } else {
          pending_fragments.Add(r_message_id, new List<Fragment>() { fragment });
        }
        //si ce fragment est marqué comme "dernier" alors on tente de reconstituer le message
        if (r_last_fragment) {
          UnityMainThread.wkr.AddJob(() => {
            ReformFragment(r_message_id, r_fragment_type);
          });
        }
      }

      //on colorise la case sequence ID
      UnityMainThread.wkr.AddJob(() => {
        int row_sequence = r_sequence_id / 53;
        int col_sequence = r_sequence_id % 53;
        sequence_id_received.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 0);
      });

      //on vérifie les 32 bitfield + last ack ID présents dans l'entête du message reçu
      for (int a = 0; a <= 32; a++) {
        ushort temp_ack_id = (ushort)(r_ack_id >= a ? (r_ack_id - a) : ((r_ack_id + 65536) - a));
        //si on a pas envoyé de packet correspondant, on ignore
        if (!packets_sent.ContainsKey(temp_ack_id)) continue;

        bool was_ack = false;
        if (a == 0) {
          was_ack = true;
        } else if (((r_ack_bitfield >> (a - 1)) & 0x00000001) == 1) {
          was_ack = true;
        }

        //si le packet a été envoyé, on confirme la livraison
        if (was_ack) {
          UnityMainThread.wkr.AddJob(() => {
            int element = temp_ack_id;
            int row = element / 53;
            int col = element % 53;
            ack_sequence_id_sent.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);

            if (sequence_id_ping_id.ContainsKey(temp_ack_id)) {
              //Debug.Log("Packet ID ack par le serveur " + temp_ack_id + " Ping ID " + sequence_id_ping_id[temp_ack_id]);
              int element_ping_id = sequence_id_ping_id[temp_ack_id];
              int row_ping_id = element_ping_id / 53;
              int col_ping_id = element_ping_id % 53;
              ack_ping_id_sent.transform.GetChild(row_ping_id).GetChild(col_ping_id).GetComponent<Image>().color = new Color(0, 1, 0);
            }
          });
          packets_sent.Remove(temp_ack_id);
        }
        //sinon, le packet n'a pas été confirmé par le serveur
        else {
          UnityMainThread.wkr.AddJob(() => {
            int element = temp_ack_id;
            int row = element / 53;
            int col = element % 53;
            ack_sequence_id_sent.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(1, 0, 0);

            if (sequence_id_ping_id.ContainsKey(temp_ack_id)) {
              int element_ping_id = sequence_id_ping_id[temp_ack_id];
              int row_ping_id = element_ping_id / 53;
              int col_ping_id = element_ping_id % 53;
              ack_ping_id_sent.transform.GetChild(row_ping_id).GetChild(col_ping_id).GetComponent<Image>().color = new Color(1, 0, 0);
            }
          });
          if (!ack_id_pending_resent.Contains(temp_ack_id)) {
            ack_id_pending_resent.Add(temp_ack_id);
            new Thread(() => {
              ResendPacketIfMissing(temp_ack_id);
            }).Start();
          }
        }
      }

      //si on a déjà reçu au moins un packet du serveur, on décale la liste des ID des 32 derniers reçus
      if (received_once) {
        for (int a = 0; a < 31; a++) {
          acks[a] = acks[a + 1];
        }
        acks[31] = last_ack;
      }
      received_once = true;
      last_ack = r_sequence_id;
    }
    catch (Exception e) {
      if (!e.Message.Contains("Cannot access a disposed object.")) {
        Debug.LogError("Erreur de réception: " + e.Message);
        Debug.LogError(e.StackTrace);
      }
    }
    finally {
      StartListenUdp();
    }
  }

  List<ushort> ack_id_pending_resent = new List<ushort>();
  void ResendPacketIfMissing(ushort temp_ack_id) {
    //si cela fait - d'une seconde qu'on l'a envoyé, on attends
    if ((DateTime.UtcNow - packets_sent[temp_ack_id].sent_at).TotalMilliseconds <= 1000) {
      var sleep_time = Math.Max(500 - (int)(10 + (DateTime.UtcNow - packets_sent[temp_ack_id].sent_at).TotalMilliseconds), 10);
      //Debug.Log("Sleep " + sleep_time + " for " + temp_ack_id);
      Thread.Sleep(sleep_time);
    }

    UnityMainThread.wkr.AddJob(() => {
      if (!packets_sent.ContainsKey(temp_ack_id)) {
        return;
      }
      //Debug.Log("Renvoi du packet " + temp_ack_id + " contient Ping " + sequence_id_ping_id[temp_ack_id]);
      Send(packets_sent[temp_ack_id].fragment, packets_sent[temp_ack_id].type, true, sequence_id_ping_id[temp_ack_id]);
      packets_sent.Remove(temp_ack_id);
    });
  }

  void OnDestroy() {
    udpClient.Close();
  }
}
class PacketSent {
  public DateTime sent_at = DateTime.UtcNow;
  public byte[] fragment;
  public byte type;

  public PacketSent(byte[] f, byte t) {
    fragment = f; type = t;
  }
}

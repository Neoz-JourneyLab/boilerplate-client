using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;
using UnityEngine.UI;
using System.Xml.Linq;

public class UdpManager : MonoBehaviour {
  UdpClient udpClient;
  IPEndPoint serverEndPoint;
  [SerializeField] GameObject rows;
  List<int> received = new List<int>();
  // Start is called before the first frame update
  void Start() {
    udpClient = new UdpClient();
    serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 69);
    //InvokeRepeating(nameof(Test), 1, 1);
    ReceiveAck();
    Send("hello server", 2);
    InvokeRepeating(nameof(Ping), 1, 1);
  }

  void Ping() {
    foreach (var element in received) {
      int row = element / 30;
      int col = element % 30;
      rows.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);
    }
    Send(((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString(), 1);
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.Space))
      Send("BONJOUR SERVEUR COMMENT TU VAS ?");

    if (Input.GetKeyDown(KeyCode.Q))
      Send("Déco manuelle", 3);

    if (Input.GetKeyDown(KeyCode.P))
      Ping();
  }

  readonly int MTU = 1200;
  ushort mid = 0;
  ushort[] acks = new ushort[32];
  ushort last_ack = 65535;
  ushort sequence_id = 0;
  bool received_once = false;
  Dictionary<ushort, byte[]> packets_sent = new Dictionary<ushort, byte[]>();
  void Send(string message, byte type = 0) {
    byte[] message_bin = Encoding.UTF8.GetBytes(message);
    uint fragments_count = (uint)Math.Ceiling(message_bin.Length / (float)MTU);
    /*
0 = no fragment id
1 = 8 bits fid (255 frag max)
2 = 16 bits fid (65 535 frag max)
3 = 32 bits fid (4 294 967 295 max)
     */
    byte frag_len_val = (byte)(fragments_count == 1 ? 0 : (fragments_count <= 255 ? 1 : (fragments_count <= 65535 ? 2 : 3)));
    ushort frag_len = (ushort)((frag_len_val & 0b00000011) << 6);
    ushort header_size = (ushort)(15 + frag_len_val + (frag_len_val == 3 ? 1 : 0));
    /*
0: message
1 : ping
2: connect
3: disconnect
     */
    ushort frag_type = (ushort) ((type & 0b00000011) << 3);
    if (mid == ushort.MaxValue) mid = 0;
    ushort message_id = mid++;
    for (uint f = 0; f < fragments_count; f++) {
      byte is_last_frag = (byte)(((byte)((f == (fragments_count - 1)) ? 1 : 0) & 0b00000001) << 5);
      byte flags = (byte)(0 ^ frag_len | is_last_frag | frag_type);
      byte[] fragment = new byte[Math.Min(MTU, message_bin.Length - f * MTU)];
      Buffer.BlockCopy(message_bin, (int)(MTU * f), fragment, 0, fragment.Length);
      byte[] buffer = new byte[header_size + fragment.Length];
      //0 - 3 : checksum
      //4 - 5 : seq id
      //6 - 7 : message id
      //8 : flags [FL L FT 000]
      //9 - 10 : ack id
      //11 - 14 : ack bitfield
      //15 - 18 : fragment id
      if (sequence_id == ushort.MaxValue) sequence_id = 0;
      ushort packet_id = sequence_id++;
      Buffer.BlockCopy(new ushort[1] { packet_id }, 0, buffer, 4, 2);
      Buffer.BlockCopy(new ushort[1] { message_id }, 0, buffer, 6, 2);
      buffer[8] = flags;
      Buffer.BlockCopy(new ushort[1] { last_ack }, 0, buffer, 9, 2);
      uint olds_aks = 0;
      for (int i = 1; i <= 32; i++) {
        int old_ack = last_ack - i;
        if (acks.Contains((ushort)old_ack)) {
          olds_aks |= (uint)(1 << (i - 1));
        }
      }
      Buffer.BlockCopy(new uint[1] { olds_aks }, 0, buffer, 11, 4);

      if (frag_len_val == 1) {
        Buffer.BlockCopy(new byte[1] { (byte)f }, 0, buffer, 15, 1);
      } else if (frag_len_val == 2) {
        Buffer.BlockCopy(new ushort[1] { (ushort)f }, 0, buffer, 15, 2);
      } else if (frag_len_val == 3) {
        Buffer.BlockCopy(new uint[1] { f }, 0, buffer, 15, 4);
      }
      Buffer.BlockCopy(fragment, 0, buffer, header_size, fragment.Length);
      uint checksum = Crc32.Compute(buffer);
      Buffer.BlockCopy(new uint[1] { checksum }, 0, buffer, 0, 4);
      udpClient.Send(buffer, buffer.Length, serverEndPoint);
      packets_sent[packet_id] = fragment;
    }
  }

  void ReceiveAck() {
    udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
  }

  void OnReceive(IAsyncResult result) {
    try {
      // Fin de la réception et récupération des données
      byte[] receivedData = udpClient.EndReceive(result, ref serverEndPoint);
      uint sent_checksum = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(receivedData, 0));
      Buffer.BlockCopy(new uint[] { 0 }, 0, receivedData, 0, 4);
      uint received_checksum = Crc32.Compute(receivedData);
      ushort sequence_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 4));
      ushort r_message_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 6));
      ushort r_flags = receivedData[8];
      byte r_fragment_len_flag = (byte)(r_flags & 0b11000000 >> 6);
      byte r_fragment_len = (byte)(r_fragment_len_flag + (r_fragment_len_flag == 3 ? 1 : 0));
      bool r_last_fragment = ((r_flags & 0b00100000) >> 5) != 0;
      byte r_fragment_type = (byte)((r_flags & 0b00011000) >> 3);
      ushort r_ack_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 9));
      uint r_ack_bitfield = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(receivedData, 11));
      uint r_fragment_id = BinaryPrimitives.ReverseEndianness(r_fragment_len == 0 ? 0 :
        (r_fragment_len == 1 ? receivedData[15] :
        (r_fragment_len == 2 ? BitConverter.ToUInt16(receivedData, 15) :
        BitConverter.ToUInt32(receivedData, 15))));
      string message = Encoding.UTF8.GetString(receivedData, 15 + r_fragment_len, receivedData.Length - (15 + r_fragment_len));

      if (r_fragment_type == 1) {
        //var date = new DateTime(1970, 1, 1).AddMilliseconds(ulong.Parse(message));
        Debug.Log("Ping from server " + message + " (" + sequence_id + ")");
        int element = int.Parse(message);
        received.Add(element);
        //Debug.Log("Ping from server " + sequence_id + " " + date.ToLongTimeString());
      } else {
        Debug.Log("Checksum reçu: " + sent_checksum + " vs " + received_checksum + (sent_checksum == received_checksum ? " OK" : " ERROR"));
        Debug.Log("sequence_id " + sequence_id);
        Debug.Log("message_id " + r_message_id);
        Debug.Log("flag " + Convert.ToString(r_flags, 16));
        Debug.Log("last_fragment " + (r_last_fragment ? " YES" : " NO"));
        Debug.Log("fragment_type " + r_fragment_type);
        Debug.Log("ack_id " + r_ack_id);
        Debug.Log("ack_bitfield " + Convert.ToString(r_ack_bitfield, 2));
        Debug.Log("fragment_id " + r_fragment_id);
        Debug.Log("message " + message);
      }

      /*
 *     console.log('ack bitfield:', abf)
for (let a = 0; a <= 32; a++) {
if ((ack_id - a) in client.packets_sent && (a === 0 || (a !== 0 && ((ack_bitfield >> a) & 0b00000001)) === 1)) {
  delete client.packets_sent[ack_id - a]
  if (a === 0) {
    console.log(chalk.green('packet', ack_id - a, 'has been ack !'))
  } else {
    console.log(chalk.green('packet', ack_id - a, 'has been ack but ack packet was missed out ! a = ', a))
  }
}
}
 */
      for (int a = 0; a <= 32; a++) {
        ushort temp_ack_id = (ushort)(r_ack_id >= a ? (r_ack_id - a) : ((r_ack_id + 65536) - a));
        if (packets_sent.ContainsKey(temp_ack_id)) {
          if(a == 0) {
            //Debug.Log("Direct ack : " + temp_ack_id);
            packets_sent.Remove(temp_ack_id);
          } else if(((r_ack_bitfield >> (a - 1)) & 0x00000001) == 1) {
            //Debug.Log("ack but was missing main ack: " + temp_ack_id);
            packets_sent.Remove(temp_ack_id);
          }
        }
      }

      for (int a = 0; a < 31; a++) {
        acks[a] = acks[a + 1];
      }
      if (received_once) {
        acks[31] = last_ack;
      }
      received_once = true;
      last_ack = sequence_id;
    } catch (Exception e) {
      Debug.LogError("Erreur de réception: " + e.Message);
      Debug.LogError(e.StackTrace);
    } finally {
      // Continuer la réception en continu
      ReceiveAck();
    }
  }

  void OnDestroy() {
    udpClient.Close();
  }
}

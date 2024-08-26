using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;
using UnityEngine.UI;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

public class UdpManager : MonoBehaviour {
  UdpClient udpClient;
  IPEndPoint serverEndPoint;
  [SerializeField] GameObject pings_id_received;
  [SerializeField] GameObject sequence_id_received;
  [SerializeField] GameObject ping_id_sent;
  [SerializeField] GameObject sequence_id_sent;
  [SerializeField] GameObject ack_sequence_id_sent;
  [SerializeField] GameObject ack_ping_id_sent;

  Dictionary<ushort, ushort> sequence_id_ping_id = new Dictionary<ushort, ushort>();
  List<int> pings_id_received_list = new List<int>();
  List<int> pings_id_sent_list = new List<int>();

  byte[] clientDiffieHellman_public;
  byte[] __SHARED__DiffieHellman__;

  Dictionary<ushort, List<Fragment>> pending_fragments = new Dictionary<ushort, List<Fragment>>();

  class Fragment {
    public uint fragment_id;
    public byte[] buffer;
  }

  // Start is called before the first frame update
  void Start() {
    DiffieHellman();
    udpClient = new UdpClient();
    serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 69);
    ReceiveAck();
    Send(clientDiffieHellman_public, 2);
    InvokeRepeating(nameof(Ping), 0.5f, 1);
  }

  void DiffieHellman() {
    var clientKeyPair = GenerateECKeyPair();

    var clientPublicKey = (ECPublicKeyParameters)clientKeyPair.Public;
    byte[] rawPublicKey = clientPublicKey.Q.GetEncoded(true);
    clientDiffieHellman_public = rawPublicKey;

    byte[] serverPublicKeyBytes = Convert.FromBase64String("BOwpvxeKBjH8xhWFEQgpL1EFjYn2aB1mu+oq2e0RxZpySG7Rikwq4Xw69tH0Dp2JaKcup5+EfQianzhGVEBUOBI=");
    ECPublicKeyParameters serverPublicKey = LoadECPublicKey(serverPublicKeyBytes);

    byte[] sharedSecret = DeriveSharedSecret(clientKeyPair.Private, serverPublicKey);
    if (sharedSecret.Length == 33) sharedSecret = sharedSecret.Skip(1).ToArray();
    __SHARED__DiffieHellman__ = SHA256.Create().ComputeHash(sharedSecret);

    Debug.Log("Public : " + BitConverter.ToString(rawPublicKey).Replace("-", "").ToLower());
    Debug.Log("Shared : " + BitConverter.ToString(sharedSecret).Replace("-", "").ToLower());
  }

  private AsymmetricCipherKeyPair GenerateECKeyPair() {
    var ecP = CustomNamedCurves.GetByName("secp256k1");
    var domainParams = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H);

    var keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
    var generator = new ECKeyPairGenerator();
    generator.Init(keyGenParams);

    return generator.GenerateKeyPair();
  }

  private ECPublicKeyParameters LoadECPublicKey(byte[] publicKeyBytes) {
    var ecP = CustomNamedCurves.GetByName("secp256k1");
    var domainParams = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H);

    var q = ecP.Curve.DecodePoint(publicKeyBytes);
    return new ECPublicKeyParameters(q, domainParams);
  }

  static string DecryptAES(byte[] cipherText, byte[] key, byte[] iv) {
    using Aes aesAlg = Aes.Create();
    aesAlg.Key = key;
    aesAlg.IV = iv;
    aesAlg.Mode = CipherMode.CBC;
    aesAlg.Padding = PaddingMode.PKCS7;

    using MemoryStream msDecrypt = new MemoryStream(cipherText);
    using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
    using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
    using StreamReader srDecrypt = new StreamReader(csDecrypt);
    return srDecrypt.ReadToEnd();
  }


  static byte[] EncryptAES(byte[] buffer, byte[] key, byte[] iv) {
    using Aes aesAlg = Aes.Create();
    aesAlg.Key = key;
    aesAlg.IV = iv;
    aesAlg.Mode = CipherMode.CBC;
    aesAlg.Padding = PaddingMode.PKCS7;

    using MemoryStream msEncrypt = new MemoryStream();
    using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
    using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
    csEncrypt.Write(buffer, 0, buffer.Length);
    csEncrypt.FlushFinalBlock();
    return msEncrypt.ToArray();
  }

  static byte[] GenerateRandomBytes(int length) {
    using var rng = new RNGCryptoServiceProvider();
    byte[] randomBytes = new byte[length];
    rng.GetBytes(randomBytes);
    return randomBytes;
  }

  private byte[] DeriveSharedSecret(AsymmetricKeyParameter privateKey, ECPublicKeyParameters publicKey) {
    var agreement = new ECDHBasicAgreement();
    agreement.Init(privateKey);
    var sharedSecret = agreement.CalculateAgreement(publicKey);

    // Convert to byte array
    return sharedSecret.ToByteArray();
  }

  uint ping_counter = 0;
  void Ping() {
    Send((ping_counter++).ToString(), 1);
    //Send(((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString(), 1);
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
  class PacketSent {
    public DateTime sent_at = DateTime.UtcNow;
    public byte[] fragment;
    public byte type;

    public PacketSent(byte[] f, byte t) {
      fragment = f; type = t;
    }
  }
  Dictionary<ushort, PacketSent> packets_sent = new Dictionary<ushort, PacketSent>();
  void Send(string message, byte type = 0) {
    byte[] message_bin = Encoding.UTF8.GetBytes(message);
    Send(message_bin, type);
  }
  void Send(byte[] message_bin, byte type = 0) {
    byte[] initializationVector = type == 2 ? new byte[0] : GenerateRandomBytes(16);
    byte[] cipher = type == 2 ? message_bin : EncryptAES(message_bin, __SHARED__DiffieHellman__, initializationVector);
    byte[] encrypted_bin = new byte[cipher.Length + initializationVector.Length];
    Buffer.BlockCopy(initializationVector, 0, encrypted_bin, 0, initializationVector.Length);
    Buffer.BlockCopy(cipher, 0, encrypted_bin, initializationVector.Length, cipher.Length);

    uint fragments_count = (uint)Math.Ceiling(encrypted_bin.Length / (float)MTU);
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
    ushort frag_type = (ushort)((type & 0b00000011) << 3);
    if (mid == ushort.MaxValue) mid = 0;
    ushort message_id = mid++;

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
      //15 - 18 : fragment id
      //Message : IV + EV + DATA

      if (sequence_id == ushort.MaxValue) sequence_id = 0;
      ushort packet_id = sequence_id++;

      int row = packet_id / 53;
      int col = packet_id % 53;
      sequence_id_sent.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);

      if (type == 1) {
        ushort ping_id = ushort.Parse(Encoding.UTF8.GetString(message_bin));
        sequence_id_ping_id.Add(packet_id, ping_id);
        int row_sequence = ping_id / 53;
        int col_sequence = ping_id % 53;
        if (pings_id_sent_list.Contains(ping_id)) {
          ping_id_sent.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 1);
        } else {
          ping_id_sent.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 0);
          pings_id_sent_list.Add(ping_id);
        }
      }
      Buffer.BlockCopy(new ushort[1] { packet_id }, 0, buffer, 4, 2);
      Buffer.BlockCopy(new ushort[1] { message_id }, 0, buffer, 6, 2);
      buffer[8] = flags;
      Buffer.BlockCopy(new ushort[1] { last_ack }, 0, buffer, 9, 2);
      uint olds_aks = 0;
      for (int i = 1; i <= 32; i++) {
        int old_ack = last_ack - i;
        if (old_ack < 0) old_ack += 65536;
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
      packets_sent[packet_id] = new PacketSent(fragment, type);
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
      ushort r_sequence_id = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(receivedData, 4));
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
      byte[] message_bin = new byte[receivedData.Length - (15 + r_fragment_len)];
      Buffer.BlockCopy(receivedData, 15 + r_fragment_len, message_bin, 0, message_bin.Length);

      if (r_fragment_type == 0 || r_fragment_type == 1) {
        Fragment fragment = new Fragment() { buffer = message_bin, fragment_id = r_fragment_id };
        if (pending_fragments.ContainsKey(r_message_id)) {
          pending_fragments[r_message_id].Add(fragment);
        } else {
          pending_fragments.Add(r_message_id, new List<Fragment>() { fragment });
        }
        if (r_last_fragment) {
          UnityMainThread.wkr.AddJob(() => {
            ReformFragment(r_message_id, r_fragment_type);
          });
        }
      }
      UnityMainThread.wkr.AddJob(() => {
        int row_sequence = r_sequence_id / 53;
        int col_sequence = r_sequence_id % 53;
        sequence_id_received.transform.GetChild(row_sequence).GetChild(col_sequence).GetComponent<Image>().color = new Color(0, 1, 0);
      });
      string bitfield = Convert.ToString(r_ack_bitfield, 2);
      while (bitfield.Length < 32) bitfield = "0" + bitfield;
      for (int a = 0; a <= 32; a++) {
        ushort temp_ack_id = (ushort)(r_ack_id >= a ? (r_ack_id - a) : ((r_ack_id + 65536) - a));
        if (packets_sent.ContainsKey(temp_ack_id)) {
          bool was_ack = false;
          if (a == 0) {
            was_ack = true;
            Debug.Log("Direct ack : " + temp_ack_id);
          } else if (((r_ack_bitfield >> (a - 1)) & 0x00000001) == 1) {
            was_ack = true;
            Debug.Log("ack but was missing main ack: " + temp_ack_id);
          }

          if (was_ack) {
            UnityMainThread.wkr.AddJob(() => {
              int element = temp_ack_id;
              int row = element / 53;
              int col = element % 53;
              ack_sequence_id_sent.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);

              if (sequence_id_ping_id.ContainsKey(temp_ack_id)) {
                int element_ping_id = sequence_id_ping_id[temp_ack_id];
                int row_ping_id = element_ping_id / 53;
                int col_ping_id = element_ping_id % 53;
                ack_ping_id_sent.transform.GetChild(row_ping_id).GetChild(col_ping_id).GetComponent<Image>().color = new Color(0, 1, 0);
              }
            });
            packets_sent.Remove(temp_ack_id);
          } else {
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

              //renvoi du packet si on l'a envoyé il y a + d'une seconde mais qu'il n'est pas ACK
              if (packets_sent.ContainsKey(temp_ack_id) && (DateTime.UtcNow - packets_sent[temp_ack_id].sent_at).TotalMilliseconds > 1000) {
                Debug.Log("Renvoi du packet " + temp_ack_id);
                Send(packets_sent[temp_ack_id].fragment, packets_sent[temp_ack_id].type);
                packets_sent.Remove(temp_ack_id);
              }
            });
          }
        }
      }

      if (received_once) {
        for (int a = 0; a < 31; a++) {
          acks[a] = acks[a + 1];
        }
        acks[31] = last_ack;
      }
      received_once = true;
      last_ack = r_sequence_id;
      Debug.Log("Last server packet ack : " + r_sequence_id);
    }
    catch (Exception e) {
      if (!e.Message.Contains("Cannot access a disposed object.")) {
        Debug.LogError("Erreur de réception: " + e.Message);
        Debug.LogError(e.StackTrace);
      }
    }
    finally {
      // Continuer la réception en continu
      ReceiveAck();
    }
  }

  void ReformFragment(ushort message_id, byte fragment_type) {
    if (!pending_fragments.ContainsKey(message_id)) return;
    List<byte> buffer = new List<byte>();
    pending_fragments[message_id] = pending_fragments[message_id].OrderBy(x => x.fragment_id).ToList();
    for (int fid = 0; fid < pending_fragments[message_id].Count; fid++) {
      if (pending_fragments[message_id].All(x => x.fragment_id != fid)) {
        Debug.Log("Missing fragment " + fid + " for message " + message_id);
        new Thread(() => {
          Thread.Sleep(2000);
          UnityMainThread.wkr.AddJob(() => {
            ReformFragment(message_id, fragment_type);
          });
        }).Start();
        return;
      }
      Fragment f = pending_fragments[message_id][fid];
      buffer.AddRange(f.buffer);
    }
    byte[] IV = new byte[16];
    byte[] cipher = new byte[buffer.Count - 16];
    Buffer.BlockCopy(buffer.ToArray(), 0, IV, 0, 16);
    Buffer.BlockCopy(buffer.ToArray(), 16, cipher, 0, cipher.Length);
    Debug.Log("IV " + Convert.ToBase64String(IV) + " cipher " + Convert.ToBase64String(cipher));
    string decrypted = DecryptAES(cipher, __SHARED__DiffieHellman__, IV);

    if (fragment_type == 0) {
      Debug.Log("MESSAGE : " + message_id + " : " + decrypted);
    } else if (fragment_type == 1) {
      byte[] r_IV = new byte[16];
      Debug.Log("Ping from server " + decrypted);
      int ping_id = int.Parse(decrypted);
      int row = ping_id / 53;
      int col = ping_id % 53;
      if (pings_id_received_list.Contains(ping_id)) {
        pings_id_received.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 1);
      } else {
        pings_id_received.transform.GetChild(row).GetChild(col).GetComponent<Image>().color = new Color(0, 1, 0);
        pings_id_received_list.Add(ping_id);
      }
    }
  }

  void OnDestroy() {
    udpClient.Close();
  }
}

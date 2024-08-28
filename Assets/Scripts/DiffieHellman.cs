using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Org.BouncyCastle.Crypto.Agreement;

public class DiffieHellman : MonoBehaviour
{
  public static byte[] PUBLIC_DiffieHellman;
  public static byte[] __SHARED__hash_DiffieHellman__;

  //Génère un nombre secret à l'aide de l'algo DiffieHellman, en fonction de la clé publique du serveur.
  public static void GenerateDiffieHellman() {
    //génère notre clé privée/publique
    var clientKeyPair = GenerateECKeyPair();

    var clientPublicKey = (ECPublicKeyParameters)clientKeyPair.Public;
    byte[] rawPublicKey = clientPublicKey.Q.GetEncoded(true);
    //sauvegarde notre clé publique pour l'envoyer au serveur lors de la connexion
    PUBLIC_DiffieHellman = rawPublicKey;

    //charge la clé par défaut du serveur
    byte[] serverPublicKeyBytes = Convert.FromBase64String("BOwpvxeKBjH8xhWFEQgpL1EFjYn2aB1mu+oq2e0RxZpySG7Rikwq4Xw69tH0Dp2JaKcup5+EfQianzhGVEBUOBI=");
    ECPublicKeyParameters serverPublicKey = LoadECPublicKey(serverPublicKeyBytes);

    byte[] sharedSecret = DeriveSharedSecret(clientKeyPair.Private, serverPublicKey);
    if (sharedSecret.Length == 33) sharedSecret = sharedSecret.Skip(1).ToArray();
    //sauvegarde la clé partagée
    __SHARED__hash_DiffieHellman__ = SHA256.Create().ComputeHash(sharedSecret);

    Debug.Log("Public : " + BitConverter.ToString(rawPublicKey).Replace("-", "").ToLower());
    Debug.Log("Shared : " + BitConverter.ToString(sharedSecret).Replace("-", "").ToLower());
  }

  private static byte[] DeriveSharedSecret(AsymmetricKeyParameter privateKey, ECPublicKeyParameters publicKey) {
    var agreement = new ECDHBasicAgreement();
    agreement.Init(privateKey);
    var sharedSecret = agreement.CalculateAgreement(publicKey);

    return sharedSecret.ToByteArray();
  }


  private static AsymmetricCipherKeyPair GenerateECKeyPair() {
    var ecP = CustomNamedCurves.GetByName("secp256k1");
    var domainParams = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H);

    var keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
    var generator = new ECKeyPairGenerator();
    generator.Init(keyGenParams);

    return generator.GenerateKeyPair();
  }

  private static ECPublicKeyParameters LoadECPublicKey(byte[] publicKeyBytes) {
    var ecP = CustomNamedCurves.GetByName("secp256k1");
    var domainParams = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H);

    var q = ecP.Curve.DecodePoint(publicKeyBytes);
    return new ECPublicKeyParameters(q, domainParams);
  }
}

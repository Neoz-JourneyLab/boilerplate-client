using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    SceneManager.LoadScene("Lobby");
    }

}

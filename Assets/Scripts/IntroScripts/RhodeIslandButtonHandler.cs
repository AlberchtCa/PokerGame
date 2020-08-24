using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RhodeIslandButtonHandler : MonoBehaviour
{
    public void LoadRhodeIslandScene()
    {
        SceneManager.LoadScene("RhodeScene", LoadSceneMode.Single);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeducButtonHandler : MonoBehaviour
{
    public void LoadLeducScene()
    {
        SceneManager.LoadScene("LeducScene", LoadSceneMode.Single);
    }
}

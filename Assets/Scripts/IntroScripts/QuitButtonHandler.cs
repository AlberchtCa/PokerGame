using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitButtonHandler : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("Quitting..");
        Application.Quit();
        
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButtonHandler : MonoBehaviour
{
    public Text txt;
    
    String GetScore()
    {
        string path = "Assets/score.txt";
        string output;

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        output = reader.ReadToEnd();
        reader.Close();
        return output;
    }
    
    void UpdateScore()
    {
        string path = "Assets/score.txt";
        string scene_score = txt.text;

        scene_score = scene_score.Substring(scene_score.IndexOf(": ") + 2);
        
        scene_score = (Int32.Parse(scene_score) + Int32.Parse(GetScore())).ToString();

        File.WriteAllText(path, string.Concat(scene_score));

        txt.text = "Score: 0";
    }

    public void ReturnToIntroScene()
    {
        UpdateScore();
        SceneManager.LoadScene("IntroScene", LoadSceneMode.Single);
    }
}

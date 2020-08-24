using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;

public class ScoreHandler : MonoBehaviour
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
    // Start is called before the first frame update
    void Start()
    {
        txt.text = String.Concat("Overall Score: ", GetScore());
    }
}


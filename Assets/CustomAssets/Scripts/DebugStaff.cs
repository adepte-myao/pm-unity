using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DebugStaff : MonoBehaviour
{
    private StreamWriter writer;

    void OnEnable()
    {
        var path = Application.persistentDataPath + "log.txt";
        writer = new StreamWriter(path, true);

        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        writer.WriteLine(logString + "\n");
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}

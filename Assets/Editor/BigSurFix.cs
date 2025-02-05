// A temporary fix for WebGL builds on MacOS Big Sur
// Script written by Unity forums member Unity1990
// https://forum.unity.com/threads/bug-generated-unityloader-js-fails-in-ios-14-public-beta.942484/#post-6152134

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class WebglPostBuild
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
            return;

        Debug.Log(pathToBuiltProject);

        string[] filePaths = Directory.GetFiles(pathToBuiltProject, "*.js", SearchOption.AllDirectories);

        foreach (string file in filePaths)
        {
            if (file.ToLower().Contains("loader.js"))
            {
                string text = File.ReadAllText(file);
                text = text.Replace(@"Mac OS X (10[\.\_\d]+)", @"Mac OS X (1[\.\_\d][\.\_\d]+)");
                File.WriteAllText(file, text);
            }
        }
    }
}

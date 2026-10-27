using System;
using System.IO;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QingmingMcpStartup
{
    private static bool starting;

    static QingmingMcpStartup()
    {
        EditorConfigurationCache.Instance.PinStdioForSession();
        EditorApplication.update += StartWhenReady;
    }

    private static async void StartWhenReady()
    {
        if (starting || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        starting = true;
        EditorApplication.update -= StartWhenReady;
        try
        {
            const string scenePath = "Assets/Scenes/Qingming.unity";
            const string initialized = "ProjectSettings/QingmingMcpInitialized.txt";
            if (!File.Exists(initialized))
            {
                if (!File.Exists(scenePath) && !EditorSceneManager.GetActiveScene().isDirty)
                {
                    ImportGodotWorld.Build();
                }
                if (File.Exists(scenePath))
                    File.WriteAllText(initialized, "MCP setup initialized.\n");
            }
            bool ready = MCPServiceLocator.Bridge.IsRunning || await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log("[Qingming MCP] Local bridge " + (ready ? "ready" : "not ready")
                      + "; port=" + MCPServiceLocator.Bridge.CurrentPort);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }
}

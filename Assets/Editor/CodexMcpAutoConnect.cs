using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace Gamejam2026.Editor
{
    [InitializeOnLoad]
    public static class CodexMcpAutoConnect
    {
        private const string UseHttpTransportKey = "MCPForUnity.UseHttpTransport";
        private const string HttpTransportScopeKey = "MCPForUnity.HttpTransportScope";
        private const string HttpUrlKey = "MCPForUnity.HttpUrl";
        private const string AutoStartOnLoadKey = "MCPForUnity.AutoStartOnLoad";
        private const string UvxPathKey = "MCPForUnity.UvxPath";
        private const string UvxPath = @"C:\Users\User\AppData\Local\Microsoft\WinGet\Packages\astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe\uvx.exe";
        private static bool isStarting;

        static CodexMcpAutoConnect()
        {
            EditorApplication.delayCall += StartWhenEditorIsReady;
        }

        [MenuItem("Tools/Gamejam/Reconnect MCP for Unity")]
        private static void ReconnectFromMenu()
        {
            _ = StartAsync();
        }

        private static void StartWhenEditorIsReady()
        {
            _ = StartAsync();
        }

        private static async Task StartAsync()
        {
            if (isStarting)
            {
                return;
            }

            isStarting = true;

            try
            {
                EditorPrefs.SetBool(UseHttpTransportKey, true);
                EditorPrefs.SetString(HttpTransportScopeKey, "local");
                EditorPrefs.SetString(HttpUrlKey, "http://127.0.0.1:8080");
                EditorPrefs.SetBool(AutoStartOnLoadKey, true);
                EditorPrefs.SetString(UvxPathKey, UvxPath);
                EditorConfigurationCache.Instance.Refresh();

                if (!MCPServiceLocator.Server.IsLocalHttpServerReachable() && System.IO.File.Exists(UvxPath))
                {
                    MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);
                }

                for (var i = 0; i < 120; i++)
                {
                    if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    EditorConfigurationCache.Instance.Refresh();
                    if (await MCPServiceLocator.Bridge.StartAsync())
                    {
                        Debug.Log("[CodexMcpAutoConnect] MCP for Unity connected to Codex.");
                        return;
                    }

                    await Task.Delay(500);
                }

                Debug.LogWarning("[CodexMcpAutoConnect] MCP for Unity did not connect within 60 seconds.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodexMcpAutoConnect] Failed to start MCP for Unity: {ex.Message}");
            }
            finally
            {
                isStarting = false;
            }
        }
    }
}

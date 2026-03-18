using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;

namespace Project.Editor
{
    [InitializeOnLoad]
    internal static class CoplayUnityMcpStartupOverride
    {
        private const string UseHttpTransportKey = "MCPForUnity.UseHttpTransport";
        private const string ResumeHttpAfterReloadKey = "MCPForUnity.ResumeHttpAfterReload";
        private const string ResumeStdioAfterReloadKey = "MCPForUnity.ResumeStdioAfterReload";

        static CoplayUnityMcpStartupOverride()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            EditorPrefs.SetBool(UseHttpTransportKey, true);
            EditorPrefs.DeleteKey(ResumeHttpAfterReloadKey);
            EditorPrefs.DeleteKey(ResumeStdioAfterReloadKey);

            if (StdioBridgeHost.IsRunning)
            {
                StdioBridgeHost.Stop();
            }
        }
    }
}

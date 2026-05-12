#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEngine;

/// <summary>
/// Post-process build script for iOS.
/// Automatically adds NSAppTransportSecurity to Info.plist
/// so that HTTP (non-HTTPS) URLs work in WKWebView.
/// </summary>
public static class MobileWebViewPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        // ── Modify Info.plist ──────────────────────────────────────────

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        // Add NSAppTransportSecurity → NSAllowsArbitraryLoads = true
        // This allows HTTP (non-HTTPS) traffic in WKWebView
        PlistElementDict atsDict = rootDict.CreateDict("NSAppTransportSecurity");
        atsDict.SetBoolean("NSAllowsArbitraryLoads", true);

        plist.WriteToFile(plistPath);

        Debug.Log("[MobileWebView] iOS: NSAppTransportSecurity added to Info.plist (HTTP allowed)");

        // ── Add WebKit.framework ───────────────────────────────────────

        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        // UnityFramework target (Unity 2019.3+)
        string targetGuid = proj.GetUnityFrameworkTargetGuid();
        if (string.IsNullOrEmpty(targetGuid))
        {
            // Fallback for older Unity
            targetGuid = proj.GetUnityMainTargetGuid();
        }

        // Add WebKit.framework (required for WKWebView)
        if (!proj.ContainsFramework(targetGuid, "WebKit.framework"))
        {
            proj.AddFrameworkToProject(targetGuid, "WebKit.framework", false);
            Debug.Log("[MobileWebView] iOS: WebKit.framework added to Xcode project");
        }

        proj.WriteToFile(projPath);
    }
}
#endif

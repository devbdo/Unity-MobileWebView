using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically configures Android and iOS internet permissions
/// when the platform is switched or Unity is loaded.
/// </summary>
[InitializeOnLoad]
public static class MobileWebViewBuildConfig
{
    static MobileWebViewBuildConfig()
    {
        ConfigureAndroid();
        ConfigureIOS();
    }

    // ── Android ────────────────────────────────────────────────────────

    private static void ConfigureAndroid()
    {
        // Force INTERNET permission in AndroidManifest
        if (!PlayerSettings.Android.forceInternetPermission)
        {
            PlayerSettings.Android.forceInternetPermission = true;
            Debug.Log("[MobileWebView] Android: Internet permission enabled (INTERNET)");
        }
    }

    // ── iOS ────────────────────────────────────────────────────────────

    private static void ConfigureIOS()
    {
        // iOS has internet access by default — no manifest needed.
        // ATS (App Transport Security) for HTTP is handled in the post-process build step.
        // Here we just ensure "Require" is set:
        if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
        {
            // Unity 2022.1+ has this setting — allows HTTP traffic
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            Debug.Log("[MobileWebView] iOS/Android: Insecure HTTP (cleartext) traffic allowed");
        }
    }

    // ── Menu Item — Manual trigger ─────────────────────────────────────

    [MenuItem("Tools/MobileWebView/Apply Network Permissions")]
    public static void ApplyPermissions()
    {
        ConfigureAndroid();
        ConfigureIOS();
        Debug.Log("[MobileWebView] Network permissions applied successfully!");
    }
}

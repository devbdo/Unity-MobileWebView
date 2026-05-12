using UnityEngine;
using System;

// ============================================================================
// MobileWebView — Unity Mobile WebView Plugin
// Herhangi bir script'ten MobileWebView.Open(url) ile native WebView açar.
// Android: android.webkit.WebView overlay
// iOS: WKWebView overlay
// Editor: Application.OpenURL fallback
// ============================================================================

public static class MobileWebView
{
    // ── Events ──────────────────────────────────────────────────────────
    /// <summary>Sayfa yüklenmeye başladığında tetiklenir.</summary>
    public static event Action<string> OnPageStarted;

    /// <summary>Sayfa yüklenmesi tamamlandığında tetiklenir.</summary>
    public static event Action<string> OnPageFinished;

    /// <summary>Sayfa yüklenirken hata oluştuğunda tetiklenir.</summary>
    public static event Action<string> OnPageError;

    /// <summary>WebView kapatıldığında tetiklenir.</summary>
    public static event Action OnClosed;

    /// <summary>Web sayfasından Unity'ye mesaj geldiğinde tetiklenir (JS Bridge).</summary>
    public static event Action<string> OnMessageReceived;

    // ── Properties ──────────────────────────────────────────────────────
    /// <summary>WebView şu anda açık mı?</summary>
    public static bool IsOpen { get; private set; }

    // Receiver GameObject adı — UnitySendMessage bu ismi kullanır
    private const string RECEIVER_OBJ_NAME = "__MobileWebViewReceiver__";
    private static MobileWebViewReceiver _receiver;

    // ====================================================================
    // Open — WebView'ı aç
    // ====================================================================

    /// <summary>
    /// Verilen URL'yi native WebView overlay olarak açar.
    /// </summary>
    /// <param name="url">Gösterilecek web adresi</param>
    public static void Open(string url)
    {
        Open(url, 0, 0);
    }

    /// <summary>
    /// Verilen URL'yi kenar boşluklarıyla native WebView overlay olarak açar.
    /// </summary>
    /// <param name="url">Gösterilecek web adresi</param>
    /// <param name="marginTop">Üstten boşluk (piksel)</param>
    /// <param name="marginBottom">Alttan boşluk (piksel)</param>
    public static void Open(string url, int marginTop, int marginBottom)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[MobileWebView] URL boş olamaz!");
            return;
        }

        EnsureReceiver();

        Debug.Log($"[MobileWebView] Opening: {url}");

#if UNITY_ANDROID && !UNITY_EDITOR
        OpenAndroid(url, marginTop, marginBottom);
#elif UNITY_IOS && !UNITY_EDITOR
        OpenIOS(url, marginTop, marginBottom);
#else
        // Editor fallback — tarayıcıda aç
        Debug.Log($"[MobileWebView] Editor mode — opening in browser: {url}");
        Application.OpenURL(url);
#endif

        IsOpen = true;
    }

    // ====================================================================
    // Close — WebView'ı kapat
    // ====================================================================

    /// <summary>
    /// Açık olan WebView'ı kapatır.
    /// </summary>
    public static void Close()
    {
        Debug.Log("[MobileWebView] Closing WebView");

#if UNITY_ANDROID && !UNITY_EDITOR
        CloseAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        CloseIOS();
#endif

        IsOpen = false;
    }

    // ====================================================================
    // Android Native Calls
    // ====================================================================

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _pluginInstance;

    private static AndroidJavaObject GetPlugin()
    {
        if (_pluginInstance == null)
        {
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.avcisoft.mobilewebview.MobileWebViewPlugin"))
            {
                _pluginInstance = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
            }
        }
        return _pluginInstance;
    }

    private static void OpenAndroid(string url, int marginTop, int marginBottom)
    {
        try
        {
            AndroidJavaObject plugin = GetPlugin();
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                plugin.Call("openWebView", activity, url, RECEIVER_OBJ_NAME, marginTop, marginBottom);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MobileWebView] Android error: {e.Message}");
        }
    }

    private static void CloseAndroid()
    {
        try
        {
            AndroidJavaObject plugin = GetPlugin();
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                plugin.Call("closeWebView", activity);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MobileWebView] Android close error: {e.Message}");
        }
    }
#endif

    // ====================================================================
    // iOS Native Calls
    // ====================================================================

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _MobileWebView_Open(string url, string receiverName, int marginTop, int marginBottom);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _MobileWebView_Close();

    private static void OpenIOS(string url, int marginTop, int marginBottom)
    {
        try
        {
            _MobileWebView_Open(url, RECEIVER_OBJ_NAME, marginTop, marginBottom);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MobileWebView] iOS error: {e.Message}");
        }
    }

    private static void CloseIOS()
    {
        try
        {
            _MobileWebView_Close();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MobileWebView] iOS close error: {e.Message}");
        }
    }
#endif

    // ====================================================================
    // Receiver — UnitySendMessage callback handler
    // ====================================================================

    private static void EnsureReceiver()
    {
        if (_receiver != null) return;

        GameObject go = new GameObject(RECEIVER_OBJ_NAME);
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        _receiver = go.AddComponent<MobileWebViewReceiver>();
    }

    // Native taraftan çağrılan callback'ler (UnitySendMessage → Receiver → buraya)
    internal static void HandlePageStarted(string url)
    {
        Debug.Log($"[MobileWebView] Page started: {url}");
        OnPageStarted?.Invoke(url);
    }

    internal static void HandlePageFinished(string url)
    {
        Debug.Log($"[MobileWebView] Page finished: {url}");
        OnPageFinished?.Invoke(url);
    }

    internal static void HandlePageError(string error)
    {
        Debug.LogWarning($"[MobileWebView] Page error: {error}");
        OnPageError?.Invoke(error);
    }

    internal static void HandleClosed()
    {
        Debug.Log("[MobileWebView] WebView closed");
        IsOpen = false;
        OnClosed?.Invoke();
    }

    internal static void HandleMessage(string message)
    {
        Debug.Log($"[MobileWebView] Message from web: {message}");
        OnMessageReceived?.Invoke(message);
    }
}

// ============================================================================
// MobileWebViewReceiver — UnitySendMessage hedef GameObject'i
// Native taraf bu GameObject'in metodlarını çağırır.
// ============================================================================

public class MobileWebViewReceiver : MonoBehaviour
{
    // UnitySendMessage("__MobileWebViewReceiver__", "OnPageStarted", url)
    public void OnPageStarted(string url)
    {
        MobileWebView.HandlePageStarted(url);
    }

    // UnitySendMessage("__MobileWebViewReceiver__", "OnPageFinished", url)
    public void OnPageFinished(string url)
    {
        MobileWebView.HandlePageFinished(url);
    }

    // UnitySendMessage("__MobileWebViewReceiver__", "OnPageError", error)
    public void OnPageError(string error)
    {
        MobileWebView.HandlePageError(error);
    }

    // UnitySendMessage("__MobileWebViewReceiver__", "OnWebViewClosed", "")
    public void OnWebViewClosed(string unused)
    {
        MobileWebView.HandleClosed();
    }

    // UnitySendMessage("__MobileWebViewReceiver__", "OnWebMessage", message)
    public void OnWebMessage(string message)
    {
        MobileWebView.HandleMessage(message);
    }
}

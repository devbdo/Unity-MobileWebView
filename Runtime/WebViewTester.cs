using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test script to trigger MobileWebView from a UI button.
/// Attach this to any GameObject in your scene, then wire up the buttons in Inspector.
/// </summary>
public class WebViewTester : MonoBehaviour
{
    [Header("Test URL")]
    [Tooltip("The URL to load when the Open button is pressed")]
    public string testUrl = "https://www.google.com";

    [Header("UI References")]
    public Button openButton;
    public Button closeButton;

    void Start()
    {
        // Subscribe to events
        MobileWebView.OnPageStarted   += OnPageStarted;
        MobileWebView.OnPageFinished  += OnPageFinished;
        MobileWebView.OnPageError     += OnPageError;
        MobileWebView.OnClosed        += OnWebViewClosed;
        MobileWebView.OnMessageReceived += OnWebMessage;

        // Wire up buttons
        if (openButton != null)
            openButton.onClick.AddListener(OpenWebView);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseWebView);

        Debug.Log("[WebViewTester] Ready. Press the Open button to launch WebView.");
    }

    public void OpenWebView()
    {
        Debug.Log($"[WebViewTester] Opening WebView: {testUrl}");
        MobileWebView.Open(testUrl);
    }

    public void CloseWebView()
    {
        Debug.Log("[WebViewTester] Closing WebView");
        MobileWebView.Close();
    }

    // ── Event Handlers ─────────────────────────────────────────────────

    private void OnPageStarted(string url)
    {
        Debug.Log($"[WebViewTester] Page started loading: {url}");
    }

    private void OnPageFinished(string url)
    {
        Debug.Log($"[WebViewTester] Page finished loading: {url}");
    }

    private void OnPageError(string error)
    {
        Debug.LogWarning($"[WebViewTester] Page error: {error}");
    }

    private void OnWebViewClosed()
    {
        Debug.Log("[WebViewTester] WebView was closed");
    }

    private void OnWebMessage(string message)
    {
        Debug.Log($"[WebViewTester] JS Bridge message: {message}");
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid leaks
        MobileWebView.OnPageStarted   -= OnPageStarted;
        MobileWebView.OnPageFinished  -= OnPageFinished;
        MobileWebView.OnPageError     -= OnPageError;
        MobileWebView.OnClosed        -= OnWebViewClosed;
        MobileWebView.OnMessageReceived -= OnWebMessage;
    }
}

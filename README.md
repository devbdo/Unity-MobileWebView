# 🌐 Unity MobileWebView

A lightweight, cross-platform **native WebView plugin** for Unity that renders real web content inside your mobile games. Open any URL as a full-screen overlay with a single line of C# — no third-party SDKs required.

[![Unity](https://img.shields.io/badge/Unity-2020.3%2B-black?logo=unity)](https://unity.com)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-brightgreen)](#platform-support)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#license)

---

## ✨ Features

- **One-Line API** — `MobileWebView.Open("https://example.com")` is all you need
- **True Native Rendering** — Uses `android.webkit.WebView` on Android and `WKWebView` on iOS
- **JavaScript Bridge** — Bidirectional communication between web pages and Unity via `unity://` URL scheme
- **Event System** — Rich callbacks for page lifecycle (`OnPageStarted`, `OnPageFinished`, `OnPageError`, `OnClosed`, `OnMessageReceived`)
- **Built-in Top Bar** — Native close button (✕) and animated progress indicator out of the box
- **Configurable Margins** — Adjust top/bottom margins to avoid overlapping your game UI
- **Automatic Build Configuration** — Editor scripts handle Android INTERNET permission, iOS ATS, and WebKit.framework linking automatically
- **Zero Dependencies** — Pure native implementations; no Gradle plugins, CocoaPods, or external libraries
- **Editor Fallback** — Opens URLs in the system browser during development

---

## 📦 Installation

### Unity Package Manager (UPM) — Git URL

1. Open **Window → Package Manager** in Unity
2. Click the **+** button → **Add package from git URL…**
3. Paste the repository URL:
   ```
   https://github.com/devbdo/Unity-MobileWebView.git
   ```
4. Click **Add** — Unity will import the package automatically

### Manual Installation

1. Download or clone this repository
2. Copy the folder into your project's `Assets/` or `Packages/` directory
3. Unity will detect the `MobileWebView.asmdef` and compile the plugin

---

## 🚀 Quick Start

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // Open a URL in the native WebView overlay
        MobileWebView.Open("https://www.google.com");
    }
}
```

That's it! The WebView opens as a full-screen overlay with a built-in close button and progress bar.

---

## 📖 API Reference

### `MobileWebView` (Static Class)

#### Methods

| Method | Description |
|--------|-------------|
| `Open(string url)` | Opens the given URL in a full-screen native WebView overlay |
| `Open(string url, int marginTop, int marginBottom)` | Opens the URL with custom top/bottom pixel margins |
| `Close()` | Programmatically closes the active WebView overlay |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsOpen` | `bool` | Returns `true` if the WebView is currently visible |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnPageStarted` | `Action<string>` | Fired when a page begins loading (receives URL) |
| `OnPageFinished` | `Action<string>` | Fired when a page finishes loading (receives URL) |
| `OnPageError` | `Action<string>` | Fired when a page fails to load (receives error message) |
| `OnClosed` | `Action` | Fired when the WebView overlay is dismissed |
| `OnMessageReceived` | `Action<string>` | Fired when the web page sends a message via the JS Bridge |

---

## 🔗 JavaScript Bridge

Web pages can send messages back to Unity using the `unity://` URL scheme:

```html
<!-- In your web page -->
<a href="unity://purchase_complete">Notify Unity</a>

<script>
  // Or via JavaScript
  window.location.href = "unity://{"action":"buy","itemId":42}";
</script>
```

Handle incoming messages in Unity:

```csharp
void Start()
{
    MobileWebView.OnMessageReceived += OnWebMessage;
    MobileWebView.Open("https://my-web-store.com");
}

void OnWebMessage(string message)
{
    Debug.Log("Received from web: " + message);
    // message = "purchase_complete" or JSON payload
}
```

---

## 🎯 Advanced Usage

### Custom Margins

Keep the WebView away from your game's HUD or navigation bar:

```csharp
// 100px from top, 80px from bottom
MobileWebView.Open("https://example.com", 100, 80);
```

### Full Event Handling

```csharp
void Start()
{
    MobileWebView.OnPageStarted   += url => Debug.Log("Loading: " + url);
    MobileWebView.OnPageFinished  += url => Debug.Log("Loaded: " + url);
    MobileWebView.OnPageError     += err => Debug.LogWarning("Error: " + err);
    MobileWebView.OnClosed        += ()  => Debug.Log("WebView closed");
    MobileWebView.OnMessageReceived += msg => Debug.Log("JS Message: " + msg);
}
```

### Programmatic Close

```csharp
// Close after 10 seconds
StartCoroutine(AutoClose());

IEnumerator AutoClose()
{
    yield return new WaitForSeconds(10f);
    if (MobileWebView.IsOpen)
        MobileWebView.Close();
}
```

---

## 🏗️ Project Structure

```
Unity-MobileWebView/
├── Editor/
│   ├── MobileWebViewBuildConfig.cs     # Auto-configures network permissions
│   └── MobileWebViewPostBuild.cs       # iOS post-build: ATS + WebKit.framework
├── Plugins/
│   ├── Android/
│   │   └── MobileWebViewPlugin.java    # Native Android WebView implementation
│   └── iOS/
│       └── MobileWebViewPlugin.mm      # Native iOS WKWebView implementation
├── Runtime/
│   ├── MobileWebView.cs               # Main C# API (static class + receiver)
│   └── WebViewTester.cs               # Example test script with UI buttons
└── MobileWebView.asmdef               # Assembly definition
```

---

## ⚙️ Platform Support

| Platform | WebView Engine | Status |
|----------|---------------|--------|
| **Android** | `android.webkit.WebView` | ✅ Fully Supported |
| **iOS** | `WKWebView` | ✅ Fully Supported |
| **Unity Editor** | System Browser (fallback) | ✅ `Application.OpenURL` |

### Minimum Requirements

- **Unity**: 2020.3 LTS or later
- **Android**: API Level 21+ (Lollipop 5.0)
- **iOS**: 11.0+
- **Xcode**: 12.0+ (for iOS builds)

---

## 🔧 Automatic Build Configuration

The plugin includes editor scripts that **automatically** handle platform-specific requirements:

### Android
- Enables `INTERNET` permission via `PlayerSettings.Android.forceInternetPermission`
- Allows cleartext (HTTP) traffic via `PlayerSettings.insecureHttpOption`

### iOS
- Injects `NSAppTransportSecurity` → `NSAllowsArbitraryLoads = true` into `Info.plist`
- Links `WebKit.framework` to the Xcode project

> You can also trigger these manually via **Tools → MobileWebView → Apply Network Permissions** in the Unity menu bar.

---

## 🧪 Testing

A ready-to-use test script (`WebViewTester.cs`) is included in the `Runtime/` folder:

1. Attach `WebViewTester` to any GameObject in your scene
2. Create two UI Buttons (Open & Close)
3. Assign them to the `openButton` and `closeButton` fields in the Inspector
4. Set your desired `testUrl` (defaults to `https://www.google.com`)
5. Press **Play** and click the Open button

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 👨‍💻 Developer

**Süleyman Ekici**

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ for the Unity community
</p>

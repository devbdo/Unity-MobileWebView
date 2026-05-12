package com.avcisoft.mobilewebview;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Build;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

public class MobileWebViewPlugin {

    private static MobileWebViewPlugin instance;
    private FrameLayout rootContainer;
    private WebView webView;
    private ProgressBar progressBar;
    private String unityReceiverName;

    public static MobileWebViewPlugin getInstance() {
        if (instance == null) instance = new MobileWebViewPlugin();
        return instance;
    }

    private MobileWebViewPlugin() {}

    public void openWebView(final Activity activity, final String url,
                            final String receiverName,
                            final int marginTop, final int marginBottom) {
        this.unityReceiverName = receiverName;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                removeWebView(activity);

                rootContainer = new FrameLayout(activity);
                rootContainer.setBackgroundColor(Color.WHITE);
                rootContainer.setClickable(true);

                FrameLayout.LayoutParams rootParams = new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        ViewGroup.LayoutParams.MATCH_PARENT);
                rootParams.topMargin = marginTop;
                rootParams.bottomMargin = marginBottom;

                LinearLayout topBar = createTopBar(activity);

                webView = new WebView(activity);
                webView.setBackgroundColor(Color.WHITE);

                WebSettings s = webView.getSettings();
                s.setJavaScriptEnabled(true);
                s.setDomStorageEnabled(true);
                s.setLoadWithOverviewMode(true);
                s.setUseWideViewPort(true);
                s.setSupportZoom(true);
                s.setBuiltInZoomControls(true);
                s.setDisplayZoomControls(false);
                s.setAllowFileAccess(true);
                s.setMediaPlaybackRequiresUserGesture(false);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP)
                    s.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);

                webView.setWebViewClient(new WebViewClient() {
                    @Override
                    public void onPageStarted(WebView v, String u, android.graphics.Bitmap f) {
                        super.onPageStarted(v, u, f);
                        if (progressBar != null) progressBar.setVisibility(View.VISIBLE);
                        sendToUnity("OnPageStarted", u);
                    }
                    @Override
                    public void onPageFinished(WebView v, String u) {
                        super.onPageFinished(v, u);
                        if (progressBar != null) progressBar.setVisibility(View.GONE);
                        sendToUnity("OnPageFinished", u);
                    }
                    @Override
                    public void onReceivedError(WebView v, WebResourceRequest req, WebResourceError err) {
                        super.onReceivedError(v, req, err);
                        String msg = Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
                                ? err.getDescription().toString() : "WebView error";
                        sendToUnity("OnPageError", msg);
                    }
                    @Override
                    public boolean shouldOverrideUrlLoading(WebView v, String u) {
                        if (u != null && u.startsWith("unity://")) {
                            sendToUnity("OnWebMessage", u.substring(8));
                            return true;
                        }
                        v.loadUrl(u);
                        return true;
                    }
                });

                webView.setWebChromeClient(new WebChromeClient() {
                    @Override
                    public void onProgressChanged(WebView v, int p) {
                        if (progressBar != null) {
                            progressBar.setProgress(p);
                            if (p >= 100) progressBar.setVisibility(View.GONE);
                        }
                    }
                });

                int barH = dpToPx(activity, 48);
                FrameLayout.LayoutParams topP = new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT, barH);
                topP.gravity = Gravity.TOP;

                FrameLayout.LayoutParams webP = new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        ViewGroup.LayoutParams.MATCH_PARENT);
                webP.topMargin = barH;

                rootContainer.addView(webView, webP);
                rootContainer.addView(topBar, topP);
                activity.addContentView(rootContainer, rootParams);
                webView.loadUrl(url);
            }
        });
    }

    public void closeWebView(final Activity activity) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                removeWebView(activity);
                sendToUnity("OnWebViewClosed", "");
            }
        });
    }

    private LinearLayout createTopBar(final Activity activity) {
        LinearLayout bar = new LinearLayout(activity);
        bar.setOrientation(LinearLayout.VERTICAL);
        bar.setBackgroundColor(Color.parseColor("#1A1A2E"));

        LinearLayout row = new LinearLayout(activity);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        int pad = dpToPx(activity, 8);
        row.setPadding(pad, dpToPx(activity, 4), pad, dpToPx(activity, 4));
        row.setLayoutParams(new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(activity, 44)));

        TextView closeBtn = new TextView(activity);
        closeBtn.setText("\u2715");
        closeBtn.setTextColor(Color.WHITE);
        closeBtn.setTextSize(TypedValue.COMPLEX_UNIT_SP, 20);
        closeBtn.setTypeface(Typeface.DEFAULT_BOLD);
        closeBtn.setGravity(Gravity.CENTER);
        closeBtn.setPadding(dpToPx(activity, 12), 0, dpToPx(activity, 12), 0);
        closeBtn.setOnClickListener(new View.OnClickListener() {
            @Override public void onClick(View v) { closeWebView(activity); }
        });

        row.addView(closeBtn);
        bar.addView(row);

        progressBar = new ProgressBar(activity, null, android.R.attr.progressBarStyleHorizontal);
        progressBar.setMax(100);
        progressBar.setProgress(0);
        progressBar.setVisibility(View.GONE);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP)
            progressBar.setProgressTintList(
                    android.content.res.ColorStateList.valueOf(Color.parseColor("#4CC9F0")));
        bar.addView(progressBar, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, dpToPx(activity, 3)));
        return bar;
    }

    private void removeWebView(Activity activity) {
        if (webView != null) { webView.stopLoading(); webView.destroy(); webView = null; }
        if (rootContainer != null) {
            ViewGroup p = (ViewGroup) rootContainer.getParent();
            if (p != null) p.removeView(rootContainer);
            rootContainer = null;
        }
        progressBar = null;
    }

    private void sendToUnity(String method, String msg) {
        if (unityReceiverName == null || unityReceiverName.isEmpty()) return;
        try { UnityPlayer.UnitySendMessage(unityReceiverName, method, msg != null ? msg : ""); }
        catch (Exception e) { /* silent */ }
    }

    private int dpToPx(Activity a, int dp) {
        return Math.round(dp * a.getResources().getDisplayMetrics().density);
    }
}

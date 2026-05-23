#if MACOS
using AppKit;
using WebKit;

namespace OverdriveServer;

public static class MacWindow {
    public static void Run() {
        NSApplication.Init();
        var app = NSApplication.SharedApplication;

        var window = new NSWindow(
            new CoreGraphics.CGRect(0, 0, 1400, 900),
            NSWindowStyle.Titled | NSWindowStyle.Resizable | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable,
            NSBackingStore.Buffered,
            false);

        var webView = new WKWebView(window.ContentView!.Bounds, new WKWebViewConfiguration());
        window.ContentView!.AddSubview(webView);

        webView.LoadRequest(new NSUrlRequest(new NSUrl("http://127.0.0.1:7117")));
        
        window.WillClose += (_, _) =>
        {
            // Try to exit without sending a crash log
            webView.EvaluateJavaScript("sendWebSocketMessage(\"client_closed\", null); disconnectWebSocket();", null);
        };

        window.MakeKeyAndOrderFront(null);
        app.ActivationPolicy = NSApplicationActivationPolicy.Regular;
        app.Run();
    }
}
#endif
#import <WebKit/WebKit.h>
#import <UIKit/UIKit.h>

// UnitySendMessage declaration
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);
extern UIViewController* UnityGetGLViewController(void);

// ============================================================================
// MobileWebViewPlugin — iOS WKWebView Overlay
// ============================================================================

static WKWebView* _webView = nil;
static UIView* _containerView = nil;
static UIView* _topBar = nil;
static UIProgressView* _progressView = nil;
static NSString* _receiverName = nil;

#pragma mark - Helper

static void SendToUnity(NSString* method, NSString* message) {
    if (_receiverName == nil || _receiverName.length == 0) return;
    UnitySendMessage(
        [_receiverName UTF8String],
        [method UTF8String],
        [message ? message : @"" UTF8String]
    );
}

#pragma mark - Navigation Delegate

@interface MWVNavigationDelegate : NSObject <WKNavigationDelegate>
@end

@implementation MWVNavigationDelegate

- (void)webView:(WKWebView*)wv didStartProvisionalNavigation:(WKNavigation*)nav {
    _progressView.hidden = NO;
    [_progressView setProgress:0.1 animated:YES];
    SendToUnity(@"OnPageStarted", wv.URL.absoluteString);
}

- (void)webView:(WKWebView*)wv didFinishNavigation:(WKNavigation*)nav {
    _progressView.hidden = YES;
    [_progressView setProgress:1.0 animated:NO];
    SendToUnity(@"OnPageFinished", wv.URL.absoluteString);
}

- (void)webView:(WKWebView*)wv didFailNavigation:(WKNavigation*)nav
      withError:(NSError*)error {
    _progressView.hidden = YES;
    SendToUnity(@"OnPageError", error.localizedDescription);
}

- (void)webView:(WKWebView*)wv didFailProvisionalNavigation:(WKNavigation*)nav
      withError:(NSError*)error {
    _progressView.hidden = YES;
    SendToUnity(@"OnPageError", error.localizedDescription);
}

- (void)webView:(WKWebView*)wv
    decidePolicyForNavigationAction:(WKNavigationAction*)action
    decisionHandler:(void (^)(WKNavigationActionPolicy))handler {
    NSString* url = action.request.URL.absoluteString;
    // unity:// scheme -> JS Bridge
    if ([url hasPrefix:@"unity://"]) {
        NSString* message = [url substringFromIndex:8];
        SendToUnity(@"OnWebMessage", message);
        handler(WKNavigationActionPolicyCancel);
        return;
    }
    handler(WKNavigationActionPolicyAllow);
}

@end

#pragma mark - KVO Observer for Progress

@interface MWVProgressObserver : NSObject
@property (nonatomic, strong) WKWebView* observedWebView;
@property (nonatomic, assign) BOOL isObserving;
@end

@implementation MWVProgressObserver

- (instancetype)init {
    self = [super init];
    if (self) {
        _isObserving = NO;
    }
    return self;
}

- (void)startObserving:(WKWebView*)wv {
    [self stopObserving];
    self.observedWebView = wv;
    [wv addObserver:self forKeyPath:@"estimatedProgress"
            options:NSKeyValueObservingOptionNew context:nil];
    self.isObserving = YES;
}

- (void)stopObserving {
    if (self.isObserving && self.observedWebView) {
        [self.observedWebView removeObserver:self forKeyPath:@"estimatedProgress"];
        self.isObserving = NO;
    }
    self.observedWebView = nil;
}

- (void)observeValueForKeyPath:(NSString*)keyPath ofObject:(id)object
                        change:(NSDictionary*)change context:(void*)ctx {
    if ([keyPath isEqualToString:@"estimatedProgress"] && _progressView) {
        float progress = (float)_webView.estimatedProgress;
        [_progressView setProgress:progress animated:YES];
        _progressView.hidden = (progress >= 1.0);
    }
}

- (void)dealloc {
    [self stopObserving];
}

@end

static MWVNavigationDelegate* _navDelegate = nil;
static MWVProgressObserver* _progressObserver = nil;

// Forward declaration
static void CloseWebView(void);

#pragma mark - Close Helper (forward-declared for use in CloseWebView and button target)

@interface MWVCloseHelper : NSObject
+ (instancetype)shared;
- (void)closeTapped;
@end

@implementation MWVCloseHelper

+ (instancetype)shared {
    static MWVCloseHelper* _shared = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ _shared = [[self alloc] init]; });
    return _shared;
}

- (void)closeTapped {
    CloseWebView();
}

@end

#pragma mark - Close Action

static void CloseWebView(void) {
    if (_progressObserver) {
        [_progressObserver stopObserving];
        _progressObserver = nil;
    }
    if (_webView) {
        [_webView stopLoading];
        [_webView removeFromSuperview];
        _webView = nil;
    }
    if (_containerView) {
        [_containerView removeFromSuperview];
        _containerView = nil;
    }
    _topBar = nil;
    _progressView = nil;
    _navDelegate = nil;

    SendToUnity(@"OnWebViewClosed", @"");
}

#pragma mark - C Interface (called from Unity C#)

extern "C" {

void _MobileWebView_Open(const char* urlCStr, const char* receiverCStr,
                          int marginTop, int marginBottom) {
    NSString* urlStr = [NSString stringWithUTF8String:urlCStr];
    _receiverName = [NSString stringWithUTF8String:receiverCStr];

    dispatch_async(dispatch_get_main_queue(), ^{
        // Remove previous
        CloseWebView();
        // Re-set receiver (CloseWebView clears it via callback)
        _receiverName = [NSString stringWithUTF8String:receiverCStr];

        UIViewController* vc = UnityGetGLViewController();
        UIView* parentView = vc.view;
        CGRect parentBounds = parentView.bounds;

        // Safe area
        CGFloat safeTop = 0, safeBottom = 0;
        if (@available(iOS 11.0, *)) {
            safeTop = vc.view.safeAreaInsets.top;
            safeBottom = vc.view.safeAreaInsets.bottom;
        }

        CGFloat totalTop = safeTop + (CGFloat)marginTop;
        CGFloat totalBottom = safeBottom + (CGFloat)marginBottom;
        CGFloat barHeight = 48.0;

        // Container
        CGRect containerFrame = CGRectMake(
            0, totalTop,
            parentBounds.size.width,
            parentBounds.size.height - totalTop - totalBottom
        );
        _containerView = [[UIView alloc] initWithFrame:containerFrame];
        _containerView.backgroundColor = [UIColor whiteColor];
        _containerView.autoresizingMask = UIViewAutoresizingFlexibleWidth |
                                           UIViewAutoresizingFlexibleHeight;

        // Top bar
        _topBar = [[UIView alloc] initWithFrame:CGRectMake(0, 0,
                    containerFrame.size.width, barHeight)];
        _topBar.backgroundColor = [UIColor colorWithRed:0.102 green:0.102
                                                   blue:0.180 alpha:1.0];
        _topBar.autoresizingMask = UIViewAutoresizingFlexibleWidth;

        // Close button
        UIButton* closeBtn = [UIButton buttonWithType:UIButtonTypeSystem];
        closeBtn.frame = CGRectMake(0, 0, 48, barHeight);
        [closeBtn setTitle:@"\u2715" forState:UIControlStateNormal];
        closeBtn.titleLabel.font = [UIFont boldSystemFontOfSize:20];
        [closeBtn setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
        [closeBtn addTarget:[MWVCloseHelper shared]
                     action:@selector(closeTapped)
           forControlEvents:UIControlEventTouchUpInside];
        [_topBar addSubview:closeBtn];

        // Progress bar
        _progressView = [[UIProgressView alloc]
            initWithProgressViewStyle:UIProgressViewStyleDefault];
        _progressView.frame = CGRectMake(0, barHeight - 2,
                                          containerFrame.size.width, 2);
        _progressView.progressTintColor = [UIColor colorWithRed:0.298
                                            green:0.788 blue:0.941 alpha:1.0];
        _progressView.trackTintColor = [UIColor clearColor];
        _progressView.autoresizingMask = UIViewAutoresizingFlexibleWidth;
        _progressView.hidden = YES;
        [_topBar addSubview:_progressView];
        [_containerView addSubview:_topBar];

        // WKWebView
        WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
        config.allowsInlineMediaPlayback = YES;

        CGRect webFrame = CGRectMake(0, barHeight,
                                      containerFrame.size.width,
                                      containerFrame.size.height - barHeight);
        _webView = [[WKWebView alloc] initWithFrame:webFrame configuration:config];
        _webView.autoresizingMask = UIViewAutoresizingFlexibleWidth |
                                     UIViewAutoresizingFlexibleHeight;
        _webView.backgroundColor = [UIColor whiteColor];

        _navDelegate = [[MWVNavigationDelegate alloc] init];
        _webView.navigationDelegate = _navDelegate;

        _progressObserver = [[MWVProgressObserver alloc] init];
        [_progressObserver startObserving:_webView];

        [_containerView addSubview:_webView];
        [parentView addSubview:_containerView];

        // Load URL
        NSURL* url = [NSURL URLWithString:urlStr];
        if (url) {
            [_webView loadRequest:[NSURLRequest requestWithURL:url]];
        }
    });
}

void _MobileWebView_Close(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        CloseWebView();
    });
}

} // extern "C"

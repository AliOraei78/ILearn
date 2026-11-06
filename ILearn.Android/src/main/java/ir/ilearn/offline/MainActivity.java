package ir.ilearn.offline;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.webkit.JavascriptInterface;
import android.webkit.CookieManager;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Toast;
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.util.HashMap;
import java.util.Map;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class MainActivity extends Activity {
    private static final String HOST = "appassets.androidplatform.net";
    private static final String HOME = "https://" + HOST + "/";
    private static final int OPEN_BACKUP = 10, SAVE_BACKUP = 11;
    private WebView web;
    private ValueCallback<Uri[]> fileCallback;
    private String pendingBackup;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        web = new WebView(this);
        WebView.setWebContentsDebuggingEnabled((getApplicationInfo().flags & android.content.pm.ApplicationInfo.FLAG_DEBUGGABLE) != 0);
        web.setBackgroundColor(Color.rgb(20, 27, 25));
        setContentView(web);
        WebSettings settings = web.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setTextZoom(Math.round(100 * getResources().getConfiguration().fontScale));
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        settings.setJavaScriptCanOpenWindowsAutomatically(false);
        settings.setSupportMultipleWindows(false);
        settings.setSafeBrowsingEnabled(true);
        settings.setGeolocationEnabled(false);
        CookieManager.getInstance().setAcceptCookie(false);
        CookieManager.getInstance().setAcceptThirdPartyCookies(web, false);
        web.addJavascriptInterface(new BackupBridge(), "ILearnAndroid");
        web.setWebViewClient(new WebViewClient() {
            @Override public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
                return assetResponse(request.getUrl(), request.getMethod(), request.isForMainFrame());
            }
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                if (isLocal(uri) && request.isForMainFrame()) return false;
                if (request.isForMainFrame() && "https".equals(uri.getScheme())) {
                    new AlertDialog.Builder(MainActivity.this)
                        .setMessage("باز کردن منبع در مرورگر به اینترنت نیاز دارد.")
                        .setPositiveButton("باز کردن مرورگر", (dialog, which) -> {
                            try { startActivity(new Intent(Intent.ACTION_VIEW, uri)); }
                            catch (ActivityNotFoundException ex) { toast("مرورگری روی دستگاه پیدا نشد."); }
                        }).setNegativeButton("انصراف", null).show();
                }
                return true;
            }
            @Override public void onPageStarted(WebView view, String url, android.graphics.Bitmap icon) {
                if (!isLocal(Uri.parse(url))) { view.stopLoading(); view.loadUrl(HOME); }
            }
        });
        web.setWebChromeClient(new WebChromeClient() {
            @Override public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback, FileChooserParams params) {
                if (fileCallback != null) fileCallback.onReceiveValue(null);
                fileCallback = callback;
                Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT).setType("application/json").addCategory(Intent.CATEGORY_OPENABLE);
                try { startActivityForResult(intent, OPEN_BACKUP); }
                catch (ActivityNotFoundException ex) { fileCallback.onReceiveValue(null); fileCallback = null; toast("انتخاب فایل در دسترس نیست."); }
                return true;
            }
        });
        if (state == null || web.restoreState(state) == null) web.loadUrl(HOME);
    }

    private static boolean isLocal(Uri uri) {
        return "https".equals(uri.getScheme()) && HOST.equals(uri.getHost()) && uri.getPort() == -1 && uri.getUserInfo() == null;
    }

    private WebResourceResponse assetResponse(Uri uri, String method, boolean mainFrame) {
        if (!mainFrame && "https".equals(uri.getScheme()) && "openrouter.ai".equals(uri.getHost()) && uri.getPort() == -1 && uri.getUserInfo() == null && uri.getQuery() == null &&
            "/api/v1/chat/completions".equals(uri.getPath()) && ("POST".equals(method) || "OPTIONS".equals(method))) return null;
        if (!isLocal(uri) || !("GET".equals(method) || "HEAD".equals(method))) return response(403, "Forbidden", "text/plain", new ByteArrayInputStream(new byte[0]));
        String path = uri.getPath();
        if (path == null || path.contains("..") || path.contains("\\")) return response(403, "Forbidden", "text/plain", new ByteArrayInputStream(new byte[0]));
        path = path.substring(1);
        if (path.isEmpty() || path.equals("lobby") || path.equals("code-room") || path.equals("interview") || path.equals("profile")) path = "index.html";
        if (!mainFrame && path.endsWith(".html")) return response(403, "Forbidden", "text/plain", new ByteArrayInputStream(new byte[0]));
        try { return response(200, "OK", mime(path), getAssets().open(path)); }
        catch (IOException ex) { return response(404, "Not Found", "text/plain", new ByteArrayInputStream(new byte[0])); }
    }

    private static String mime(String path) {
        if (path.endsWith(".wasm")) return "application/wasm";
        if (path.endsWith(".js") || path.endsWith(".mjs")) return "text/javascript";
        if (path.endsWith(".json")) return "application/json";
        if (path.endsWith(".css")) return "text/css";
        if (path.endsWith(".svg")) return "image/svg+xml";
        if (path.endsWith(".woff2")) return "font/woff2";
        if (path.endsWith(".html")) return "text/html";
        return "application/octet-stream";
    }

    private static WebResourceResponse response(int status, String reason, String mime, InputStream data) {
        Map<String, String> headers = new HashMap<>();
        headers.put("Cache-Control", "no-store");
        headers.put("X-Content-Type-Options", "nosniff");
        headers.put("Referrer-Policy", "no-referrer");
        headers.put("Content-Security-Policy", "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self' https://openrouter.ai/api/v1/chat/completions; worker-src 'self' blob:; base-uri 'self'; object-src 'none'; frame-src 'none'; frame-ancestors 'none'; form-action 'none'");
        return new WebResourceResponse(mime, mime.startsWith("text/") || mime.equals("application/json") ? "UTF-8" : null,
            status, reason, headers, data);
    }

    private final class BackupBridge {
        @JavascriptInterface public synchronized boolean setApiKey(String value) {
            if (value == null || (!value.isEmpty() && (value.length() < 20 || value.length() > 256 || !value.matches("sk-or-[A-Za-z0-9_-]+")))) return false;
            try {
                if (value.isEmpty()) return getPreferences(MODE_PRIVATE).edit().remove("apiKey").commit();
                Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
                cipher.init(Cipher.ENCRYPT_MODE, secretKey());
                String encrypted = Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP) + "." +
                    Base64.encodeToString(cipher.doFinal(value.getBytes(StandardCharsets.UTF_8)), Base64.NO_WRAP);
                return getPreferences(MODE_PRIVATE).edit().putString("apiKey", encrypted).commit();
            } catch (Exception ex) { return false; }
        }
        @JavascriptInterface public synchronized String getApiKey() {
            try {
                String encrypted = getPreferences(MODE_PRIVATE).getString("apiKey", "");
                if (encrypted.isEmpty()) return "";
                String[] parts = encrypted.split("\\.");
                Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
                cipher.init(Cipher.DECRYPT_MODE, secretKey(), new GCMParameterSpec(128, Base64.decode(parts[0], Base64.NO_WRAP)));
                return new String(cipher.doFinal(Base64.decode(parts[1], Base64.NO_WRAP)), StandardCharsets.UTF_8);
            } catch (Exception ex) { return ""; }
        }
        @JavascriptInterface public void saveBackup(String json) {
            if (json == null || json.length() > 32 * 1024 * 1024) return;
            runOnUiThread(() -> {
                if (pendingBackup != null) return;
                pendingBackup = json;
                Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT).setType("application/json")
                    .addCategory(Intent.CATEGORY_OPENABLE).putExtra(Intent.EXTRA_TITLE, "ILearn-backup.json");
                try { startActivityForResult(intent, SAVE_BACKUP); }
                catch (ActivityNotFoundException ex) { pendingBackup = null; toast("ذخیرهٔ فایل در دسترس نیست."); }
            });
        }
    }

    private SecretKey secretKey() throws Exception {
        KeyStore store = KeyStore.getInstance("AndroidKeyStore");
        store.load(null);
        if (!store.containsAlias("ILearnOpenRouter")) {
            KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
            generator.init(new KeyGenParameterSpec.Builder("ILearnOpenRouter", KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());
            generator.generateKey();
        }
        return (SecretKey) store.getKey("ILearnOpenRouter", null);
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        Uri uri = result == RESULT_OK && data != null ? data.getData() : null;
        if (request == OPEN_BACKUP && fileCallback != null) {
            fileCallback.onReceiveValue(uri == null ? null : new Uri[]{uri}); fileCallback = null;
        }
        if (request == SAVE_BACKUP) {
            String json = pendingBackup; pendingBackup = null;
            if (uri != null && json != null) {
                new Thread(() -> {
                    try (OutputStream output = getContentResolver().openOutputStream(uri, "wt")) {
                        if (output == null) throw new IOException("No destination");
                        output.write(json.getBytes(StandardCharsets.UTF_8));
                        runOnUiThread(() -> toast("پشتیبان ذخیره شد."));
                    } catch (IOException ex) { runOnUiThread(() -> toast("پشتیبان ذخیره نشد؛ دوباره امتحان کن.")); }
                }).start();
            }
        }
    }

    private void toast(String message) { Toast.makeText(this, message, Toast.LENGTH_LONG).show(); }
    @Override public void onBackPressed() {
        web.evaluateJavascript("(()=>{const menu=document.querySelector('.sidebar-toggle[aria-expanded=\"true\"]');if(menu){menu.click();return false;}if(history.length>1){history.back();return false;}return true;})()",
            result -> { if ("true".equals(result)) finish(); });
    }
    @Override protected void onSaveInstanceState(Bundle state) { web.saveState(state); super.onSaveInstanceState(state); }
    @Override protected void onPause() { web.onPause(); super.onPause(); }
    @Override protected void onResume() { super.onResume(); web.onResume(); }
    @Override protected void onDestroy() { if (fileCallback != null) fileCallback.onReceiveValue(null); web.removeJavascriptInterface("ILearnAndroid"); web.destroy(); super.onDestroy(); }
}

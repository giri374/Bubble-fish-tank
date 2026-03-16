using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

/// <summary>
/// Listens for incoming HTTP POST requests on localhost:8080/message
/// Expected JSON body: { "message": "Hello World" }
/// 
/// This script is the ONLY integration point between Unity and the outside world.
/// The sending app only needs to know: POST JSON to http://localhost:8080/message
/// </summary>
public class MessageReceiver : MonoBehaviour
{
    public static MessageReceiver Instance { get; private set; }

    [Header("Server Settings")]
    public int port = 8080;

    // Thread-safe queue: background thread writes, Unity main thread reads
    private readonly System.Collections.Generic.Queue<string> _pendingMessages = new();
    private readonly object _lock = new();

    private HttpListener _listener;
    private Thread _listenerThread;

    public event Action<string> OnMessageReceived;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartServer();
    }

    void Update()
    {
        // Drain the queue on the main thread so Unity APIs are safe to call
        lock (_lock)
        {
            while (_pendingMessages.Count > 0)
            {
                string msg = _pendingMessages.Dequeue();
                OnMessageReceived?.Invoke(msg);
            }
        }
    }

    void OnDestroy() => StopServer();

    // ── Server ────────────────────────────────────────────────────────────────

    void StartServer()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/message/");
        _listener.Start();

        _listenerThread = new Thread(ListenLoop) { IsBackground = true };
        _listenerThread.Start();

        Debug.Log($"[MessageReceiver] Listening on http://localhost:{port}/message/");
    }

    void StopServer()
    {
        _listener?.Stop();
        _listenerThread?.Abort();
    }

    void ListenLoop()
    {
        while (_listener.IsListening)
        {
            try
            {
                var context = _listener.GetContext();          // blocks until request arrives
                HandleRequest(context);
            }
            catch { /* server was stopped */ }
        }
    }

    void HandleRequest(HttpListenerContext context)
    {
        var req  = context.Request;
        var resp = context.Response;

        // Allow browser / local apps to POST without CORS issues
        resp.Headers.Add("Access-Control-Allow-Origin", "*");
        resp.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")          // preflight
        {
            resp.StatusCode = 204;
            resp.Close();
            return;
        }

        if (req.HttpMethod == "POST")
        {
            string body = new StreamReader(req.InputStream).ReadToEnd();
            string message = ParseMessage(body);

            if (!string.IsNullOrEmpty(message))
            {
                lock (_lock) { _pendingMessages.Enqueue(message); }
                Respond(resp, 200, "{\"status\":\"ok\"}");
            }
            else
            {
                Respond(resp, 400, "{\"status\":\"error\",\"detail\":\"missing message field\"}");
            }
        }
        else
        {
            Respond(resp, 405, "{\"status\":\"method not allowed\"}");
        }
    }

    // Minimal JSON parse — avoids a dependency on Newtonsoft or JsonUtility wrapper
    string ParseMessage(string json)
    {
        // Expects: { "message": "some text" }
        const string key = "\"message\"";
        int keyIdx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIdx < 0) return null;

        int colon = json.IndexOf(':', keyIdx + key.Length);
        if (colon < 0) return null;

        int openQuote = json.IndexOf('"', colon + 1);
        if (openQuote < 0) return null;

        int closeQuote = json.IndexOf('"', openQuote + 1);
        if (closeQuote < 0) return null;

        return json.Substring(openQuote + 1, closeQuote - openQuote - 1);
    }

    static void Respond(HttpListenerResponse resp, int code, string body)
    {
        resp.StatusCode  = code;
        resp.ContentType = "application/json";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
        resp.OutputStream.Write(bytes, 0, bytes.Length);
        resp.Close();
    }
}

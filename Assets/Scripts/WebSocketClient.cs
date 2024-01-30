using System.Collections.Concurrent;
using UnityEngine;
using WebSocketSharp;

namespace WebRTCTutorial
{
    public delegate void MessageHandler(string message);

    public class WebSocketClient : MonoBehaviour
    {
        public event MessageHandler MessageReceived;

        public void SendWebSocketMessage(string message) => _ws.Send(message);

        // Awake is called automatically by Unity when a script attached to a gameObject is loaded
        protected void Awake()
        {
            // Create WebSocket instance and connect to localhost
            var url = string.IsNullOrEmpty(_url) ? "ws://localhost:8080" : _url;
            _ws = new WebSocket(url);

            // Subscribe to events
            _ws.OnMessage += OnMessage;
            _ws.OnError += OnError;

            // Connect
            _ws.Connect();
        }

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        protected void Update()
        {
            // Process received messages on the main thread - Unity functions can only be called from the main thread
            while (_receivedMessages.TryDequeue(out var message))
            {
                Debug.Log("WS Message Received: " + message);
                MessageReceived?.Invoke(message);
            }
        }

        // Called by Unity -> https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html
        protected void OnDestroy()
        {
            if (_ws == null)
            {
                return;
            }

            // Unsubscribe from events
            _ws.OnMessage -= OnMessage;
            _ws.OnError -= OnError;
            _ws = null;
        }

        [SerializeField]
        private string _url;

        private WebSocket _ws;

        private readonly ConcurrentQueue<string> _receivedMessages = new ConcurrentQueue<string>();

        private void OnMessage(object sender, MessageEventArgs e)
        {
            _receivedMessages.Enqueue(e.Data);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Debug.LogError("WS error: " + e.Message);
        }
    }
}
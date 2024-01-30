using System;
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
            _ws.OnOpen += OnOpen;
            _ws.OnClose += OnClose;
            _ws.OnError += OnError;
        
            // Connect
            _ws.Connect();
        }

        // OnDestroy is called automatically by Unity when the script is destroyed
        protected void OnDestroy()
        {
            if (_ws == null)
            {
                return;
            }
        
            // Unsubscribe from events
            _ws.OnMessage -= OnMessage;
            _ws.OnOpen -= OnOpen;
            _ws.OnClose -= OnClose;
            _ws.OnError -= OnError;
            _ws = null;
        }

        [SerializeField]
        private string _url;
    
        private WebSocket _ws;

        private void OnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log("WS Message Received: " + e.Data);
            MessageReceived?.Invoke(e.Data);
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            Debug.Log("WS Closed");
        }

        private void OnOpen(object sender, EventArgs e)
        {
            // Send a test message
            Debug.Log("WS Connected. Send test message");
            //_ws.Send("Test Message");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Debug.LogError("WS error: " + e.Message);
        }
    }
}

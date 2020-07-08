﻿using System;
using System.Timers;
using Huobi.SDK.Core.Model;
using Newtonsoft.Json;
using WebSocketSharp;

namespace Huobi.SDK.Core.Client.WebSocketClientBase
{
    /// <summary>
    /// The abstract class that responsible to get data from websocket
    /// </summary>
    /// <typeparam name="DataResponseType"></typeparam>
    public abstract class WebSocketClientBase<DataResponseType>
    {
        public delegate void OnConnectionOpenHandler();
        public event OnConnectionOpenHandler OnConnectionOpen;
        public delegate void OnResponseReceivedHandler(DataResponseType response);
        public event OnResponseReceivedHandler OnResponseReceived;

        protected const string DEFAULT_HOST = "api.huobi.pro/ws";
        private string _host;

        protected WebSocket _WebSocket;

        private bool _autoConnect;
        private Timer _timer;
        private const int TIMER_INTERVAL_SECOND = 5;
        private DateTime _lastReceivedTime;
        private const int RECONNECT_WAIT_SECOND = 60;
        private const int RENEW_WAIT_SECOND = 120;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">websocket host</param>
        public WebSocketClientBase(string host = DEFAULT_HOST)
        {
            _host = host;

            _timer = new Timer(TIMER_INTERVAL_SECOND * 1000);
            _timer.Elapsed += _timer_Elapsed;

            InitializeWebSocket();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double elapsedSecond = (DateTime.UtcNow - _lastReceivedTime).TotalSeconds;
            //Console.WriteLine($"WebSocket received data {elapsedSecond} sec ago");

            if (elapsedSecond > RECONNECT_WAIT_SECOND && elapsedSecond <= RENEW_WAIT_SECOND)
            {
                Console.WriteLine("WebSocket reconnecting...");
                _WebSocket.Close();
                _WebSocket.Connect();
            }
            else if (elapsedSecond > RENEW_WAIT_SECOND)
            {
                Console.WriteLine("WebSocket re-initialize...");
                Disconnect();
                UninitializeWebSocket();
                InitializeWebSocket();                
                Connect();
            }
        }

        private void InitializeWebSocket()
        {
            _WebSocket = new WebSocket($"wss://{_host}");
            _WebSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;

            _WebSocket.OnError += _WebSocket_OnError;
            _WebSocket.OnOpen += _WebSocket_OnOpen;

            _lastReceivedTime = DateTime.UtcNow;
        }

        private void UninitializeWebSocket()
        {
            _WebSocket.OnOpen -= _WebSocket_OnOpen;
            _WebSocket.OnError -= _WebSocket_OnError;
            _WebSocket = null;
        }

        /// <summary>
        /// Connect to websocket server
        /// </summary>
        /// <param name="autoConnect">whether auto connect to server after it is disconnected</param>
        public void Connect(bool autoConnect = true)
        {
            _WebSocket.OnMessage += _WebSocket_OnMessage;

            _WebSocket.Connect();

            _autoConnect = autoConnect;
            if (_autoConnect)
            {
                _timer.Enabled = true;
            }
        }

        /// <summary>
        /// Disconnect to websocket server
        /// </summary>
        public void Disconnect()
        {
            _timer.Enabled = false;

            _WebSocket.OnMessage -= _WebSocket_OnMessage;

            _WebSocket.Close(CloseStatusCode.Normal);
        }

        private void _WebSocket_OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket Opened");

            _lastReceivedTime = DateTime.UtcNow;

            OnConnectionOpen?.Invoke();
        }

        private void _WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            _lastReceivedTime = DateTime.UtcNow;

            if (e.IsBinary)
            {
                string data = GZipDecompresser.Decompress(e.RawData);

                var pingMessage = JsonConvert.DeserializeObject<PingMessage>(data);
                if (pingMessage != null && pingMessage.ping != 0)
                {
                    //Console.WriteLine($"received ping:{pingMessage.ping}");
                    string pongData = $"{{\"pong\":{pingMessage.ping}}}";
                    _WebSocket.Send(pongData);
                    //Console.WriteLine($"replied pong:{pingMessage.ping}");
                }
                else
                {
                    var response = JsonConvert.DeserializeObject<DataResponseType>(data);

                    OnResponseReceived?.Invoke(response);
                }
            }
        }

        private void _WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"WebSocket Error: {e.Message}");
        }
    }
}
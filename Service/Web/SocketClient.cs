using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Service.Web
{
    public class SocketClient
    {
        // event definitions
        public event EventHandler? OnDisconnected;
        public event EventHandler? OnConnected;
        public event PostDeletedEventHandler? OnPostDeleted;
        public event PostUpdatedEventHandler? OnPostUpdated;
        public event PostCreatedEventHandler? OnPostCreated;

        public delegate void PostDeletedEventHandler(string id);
        public delegate void PostUpdatedEventHandler(string id, string newContent);
        public delegate void PostCreatedEventHandler(FeedEntry data);

        // private data
        private PlugifyWebSocketClient _client = new();

        // public fields
        public bool IsConnected => _client.IsOpen;

        public SocketClient()
        {
            _client.OnMessage += _client_OnMessage;
            _client.OnClose += _client_OnClose;
        }

        private void _client_OnClose(object? sender, EventArgs e)
        {
            OnDisconnected?.Invoke(sender, e);
            Debug.WriteLine("Socket Closed");
        }

        private void _client_OnMessage(object? sender, string data)
        {
            Debug.WriteLine(data);

            try
            {
                FeedMessage? message = FeedMessage.Deserialize(data);

                if (message is DeletePostMessage deletePost)
                {
                    OnPostDeleted?.Invoke(deletePost.ID);
                }
                else if (message is UpdatePostMessage updatePost)
                {
                    OnPostUpdated?.Invoke(updatePost.ID, updatePost.NewContent);
                }
                else if (message is NewPostMessage createdPost)
                {
                    OnPostCreated?.Invoke(createdPost.Data);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("WebSocket data read exception: " + ex);

            }

        }

        public async Task Connect()
        {
            if (IsConnected) return;

            _client.SetUrl((ApiClient.ApiBase.Contains("http") ? "ws://" : "wss:// ") + ApiClient.ApiBase.Replace("https://", "").Replace("http://", ""));
            await _client.Start();
            OnConnected?.Invoke(new(), new());
        }

        public void Disconnect()
        {
            _client.Close();
        }
    }
}

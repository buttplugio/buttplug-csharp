using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using JetBrains.Annotations;

namespace ButtplugKiirooPlatformEmulator
{
    public class KiirooPlatformEventArgs : EventArgs
    {
        public ushort Position { get; }

        public KiirooPlatformEventArgs(ushort aPos)
        {
            Position = aPos;
        }
    }

    public class KiirooPlatformEmulator
    {
        [NotNull]
        private readonly HttpListener _httpListener;
        private bool _stop;
        private bool _isRunning;

        public event EventHandler<KiirooPlatformEventArgs> OnKiirooPlatformEvent;

        public KiirooPlatformEmulator()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:6969/");
            _stop = false;
            _isRunning = false;
        }

        // http://stackoverflow.com/questions/5197579/getting-form-data-from-httplistenerrequest
        private static string GetRequestPostData([NotNull] HttpListenerRequest aRequest)
        {
            if (!aRequest.HasEntityBody)
            {
                return null;
            }

            var body = aRequest.InputStream;
            try
            {
                using (var reader = new StreamReader(body, aRequest.ContentEncoding))
                {
                    body = null;
                    return reader.ReadToEnd();
                }
            }
            finally
            {
                body?.Close();
            }
        }

        public async void StartServer()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;
            _stop = false;
            _httpListener.Start();
            while (!_stop)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _httpListener.GetContextAsync();
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 995)
                    {
                        StopServer();
                        _isRunning = false;
                        return;
                    }
                }

                if (ctx == null) continue;

                //got a request
                var response = ctx.Response;
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add(HttpResponseHeader.CacheControl, "private, no-store");
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = (int)HttpStatusCode.OK;
                if (ctx.Request.Url.Segments.Length < 2)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    continue;
                }
                var methodName = ctx.Request.Url.Segments[1].Replace("/", "");
                string json;
                switch (methodName)
                {
                    case "status":
                        json = "{\"connectedDeviceName\":\"ONYX\",\"bluetoothAddress\":\"8CDE52B866B5\",\"firmwareUpdateProgres\":0,\"remoteDevice\":\"not connected\",\"devicestatus\":\"NORMAL\",\"localDevice\":\"connected\",\"previousdevice_connectionurl\":\"btspp://8CDE52B866B5:1;authenticate=false;encrypt=false;master=false\",\"readOnlyMode\":false,\"streamToDeviceEnabled\":true,\"delay\":0,\"writeOnlyMode\":false,\"currentFW\":\"91\",\"waitingforusbcable\":true,\"bluetoothOn\":true,\"previousdevice_name\":\"ONYX\",\"uienabled\":true,\"newFWVersionAvailable\":false,\"previousdevice_bluetoothaddress\":\"8CDE52B866B5\",\"statusCode\":1}";
                        break;
                    case "senddata":
                        json = "{}";
                        var post = GetRequestPostData(ctx.Request);
                        var data = HttpUtility.ParseQueryString(post);
                        if (!data.HasKeys())
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                            continue;
                        }
                        try
                        {
                            var position = ushort.Parse(data["data"]);
                            OnKiirooPlatformEvent?.Invoke(this, new KiirooPlatformEventArgs(position));
                        }
                        catch (FormatException)
                        {
                            // Swallow format exceptions, as sometimes scripts can send "undefined".
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                            continue;
                        }
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        response.Close();
                        continue;
                }

                var messageBytes = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = messageBytes.Length;
                await response.OutputStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                response.OutputStream.Close();
                response.Close();
            }
            _isRunning = false;
        }

        public void StopServer()
        {
            if (!_isRunning)
            {
                return;
            }
            _stop = true;
            _httpListener.Stop();
        }
    }
}
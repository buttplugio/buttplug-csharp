using System.Text;
using Buttplug.Core;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;
using WebSocketSharp.Net;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServer
    {
        private HttpServer _wsServer;

        public void StartServer([NotNull] ButtplugServiceFactory aFactory, int aPort = 12345, bool aSecure = false)
        {
            _wsServer = new HttpServer(aPort, aSecure);
            _wsServer.RemoveWebSocketService("/buttplug");
            _wsServer.OnGet += OnGetHandler;
            if (aSecure)
            {
                _wsServer.SslConfiguration.ServerCertificate = CertUtils.GetCert("Buttplug");
            }
            _wsServer.WebSocketServices.AddService<ButtplugWebsocketServerBehavior>("/buttplug", (aObj) => aObj.Service = aFactory.GetService());
            _wsServer.Start();
        }

        public void StopServer()
        {
            if (_wsServer is null)
            {
                return;
            }
            _wsServer.Stop();
            _wsServer.RemoveWebSocketService("/buttplug");
            _wsServer = null;
        }

        private static void OnGetHandler(object aSender, HttpRequestEventArgs aEvent)
        {
            var req = aEvent.Request;
            var res = aEvent.Response;

            // Wouldn't it be cool to present syncydink here?

            var path = req.RawUrl;
            if (path == "/")
                path += "index.html";

            if (path != "/index.html")
            {
                res.StatusCode = (int)HttpStatusCode.TemporaryRedirect;
                res.RedirectLocation = "/index.html";
                return;
            }

            res.ContentType = "text/html";
            res.ContentEncoding = Encoding.UTF8;
            res.WriteContent(Encoding.UTF8.GetBytes("<html><head><title>Buttplug server</title></head><body><h1>Buttplug server</h1><p>The server is running.</p></body></html>"));

        }
    }

}

using System;
using System.Text;
using Buttplug.Core;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;
using WebSocketSharp.Net;
using System.Security.Cryptography.X509Certificates;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServer
    {
        [NotNull]
        private HttpServer _wsServer;

        public ButtplugWebsocketServer()
        {
            _wsServer = new HttpServer(12345, true);
            _wsServer.RemoveWebSocketService("/buttplug");
            _wsServer.OnGet += OnGetHandler;
            _wsServer.SslConfiguration.ServerCertificate = CertUtils.GetCert("Buttplug");
        }

        public void StartServer([NotNull] ButtplugService aService)
        {
            _wsServer.WebSocketServices.AddService<ButtplugWebsocketServerBehavior>("/buttplug", (obj) => obj.Service = aService);
            _wsServer.Start();
        }

        public void StopServer()
        {
            _wsServer.Stop();
            _wsServer.RemoveWebSocketService("/buttplug");
        }

        protected void OnGetHandler(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

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

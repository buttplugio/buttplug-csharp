﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.SessionState;

namespace SharpRaven.UnitTests.Utilities
{
    public enum HttpVerb
    {
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
    }

    /// <summary>
    /// Useful class for simulating the HttpContext. This does not actually 
    /// make an HttpRequest, it merely simulates the state that your code 
    /// would be in "as if" handling a request. Thus the HttpContext.Current 
    /// property is populated.
    /// </summary>
    public class HttpSimulator : IDisposable
    {
        private const string DefaultPhysicalAppPath = @"c:\InetPub\wwwRoot\";
        private readonly NameValueCollection _cookies = new NameValueCollection();
        private readonly NameValueCollection _formVars = new NameValueCollection();
        private readonly NameValueCollection _headers = new NameValueCollection();
        private string _applicationPath = "/";
        private StringBuilder _builder;
        private string _currentExecutionPath;
        private string _physicalApplicationPath = DefaultPhysicalAppPath;
        private string _physicalPath = DefaultPhysicalAppPath;
        private Uri _referer;
        private TextWriter _responseWriter;
        private SimulatedHttpRequest _workerRequest;


        public HttpSimulator()
            : this("/", DefaultPhysicalAppPath)
        {
        }


        public HttpSimulator(string applicationPath)
            : this(applicationPath, DefaultPhysicalAppPath)
        {
        }


        public HttpSimulator(string applicationPath, string physicalApplicationPath)
        {
            ApplicationPath = applicationPath;
            PhysicalApplicationPath = physicalApplicationPath;
        }


        /// <summary>
        /// The same thing as the IIS Virtual directory. It's 
        /// what gets returned by Request.ApplicationPath.
        /// </summary>
        public string ApplicationPath
        {
            get { return this._applicationPath; }
            set
            {
                this._applicationPath = value ?? "/";
                this._applicationPath = NormalizeSlashes(this._applicationPath);
            }
        }

        public string Host { get; private set; }

        public string LocalPath { get; private set; }

        /// <summary>
        /// Portion of the URL after the application.
        /// </summary>
        public string Page { get; private set; }

        /// <summary>
        /// Physical path to the application (used for simulation purposes).
        /// </summary>
        public string PhysicalApplicationPath
        {
            get { return this._physicalApplicationPath; }
            set
            {
                this._physicalApplicationPath = value ?? DefaultPhysicalAppPath;
                //strip trailing backslashes.
                this._physicalApplicationPath = StripTrailingBackSlashes(this._physicalApplicationPath) + @"\";
            }
        }

        /// <summary>
        /// Physical path to the requested file (used for simulation purposes).
        /// </summary>
        public string PhysicalPath
        {
            get { return this._physicalPath; }
        }

        public int Port { get; private set; }

        /// <summary>
        /// Returns the text from the response to the simulated request.
        /// </summary>
        public string ResponseText
        {
            get { return (this._builder ?? new StringBuilder()).ToString(); }
        }

        public TextWriter ResponseWriter
        {
            get { return this._responseWriter; }
            set { this._responseWriter = value; }
        }

        public SimulatedHttpRequest WorkerRequest
        {
            get { return this._workerRequest; }
        }


        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (HttpContext.Current != null)
                HttpContext.Current = null;
        }


        /// <summary>
        /// Sets the cookie.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Cannot set headers after calling Simulate().</exception>
        public HttpSimulator SetCookie(string name, string value)
        {
            if (this._workerRequest != null)
                throw new InvalidOperationException("Cannot set headers after calling Simulate().");

            this._cookies.Add(name, value);

            return this;
        }


        /// <summary>
        /// Sets a form variable.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HttpSimulator SetFormVariable(string name, string value)
        {
            if (this._workerRequest != null)
                throw new InvalidOperationException("Cannot set form variables after calling Simulate().");

            this._formVars.Add(name, value);

            return this;
        }


        /// <summary>
        /// Sets a header value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HttpSimulator SetHeader(string name, string value)
        {
            if (this._workerRequest != null)
                throw new InvalidOperationException("Cannot set headers after calling Simulate().");

            this._headers.Add(name, value);

            return this;
        }


        /// <summary>
        /// Sets the referer for the request. Uses a fluent interface.
        /// </summary>
        /// <param name="referer"></param>
        /// <returns></returns>
        public HttpSimulator SetReferer(Uri referer)
        {
            if (this._workerRequest != null)
                this._workerRequest.SetReferer(referer);
            this._referer = referer;
            return this;
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a GET request.
        /// </summary>
        /// <remarks>
        /// Simulates a request to http://localhost/
        /// </remarks>
        public HttpSimulator SimulateRequest()
        {
            return SimulateRequest(new Uri("http://localhost/"));
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a GET request.
        /// </summary>
        /// <param name="url"></param>
        public HttpSimulator SimulateRequest(Uri url)
        {
            return SimulateRequest(url, HttpVerb.GET);
        }


        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        public HttpSimulator SimulateRequest(Uri url, HttpVerb httpVerb)
        {
            return SimulateRequest(url, httpVerb, null, null);
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a POST request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formVariables"></param>
        public HttpSimulator SimulateRequest(Uri url, NameValueCollection formVariables)
        {
            return SimulateRequest(url, HttpVerb.POST, formVariables, null);
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a POST request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formVariables"></param>
        /// <param name="headers"></param>
        public HttpSimulator SimulateRequest(Uri url, NameValueCollection formVariables, NameValueCollection headers)
        {
            return SimulateRequest(url, HttpVerb.POST, formVariables, headers);
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        /// <param name="headers"></param>
        public HttpSimulator SimulateRequest(Uri url, HttpVerb httpVerb, NameValueCollection headers)
        {
            return SimulateRequest(url, httpVerb, null, headers);
        }


        protected static HostingEnvironment GetHostingEnvironment()
        {
            HostingEnvironment environment;
            try
            {
                environment = new HostingEnvironment();
            }
            catch (InvalidOperationException)
            {
                //Shoot, we need to grab it via reflection.
                environment = ReflectionHelper.GetStaticFieldValue<HostingEnvironment>("_theHostingEnvironment",
                                                                                       typeof(HostingEnvironment));
            }
            return environment;
        }


        /// <summary>
        /// Sets up the HttpContext objects to simulate a request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpVerb"></param>
        /// <param name="formVariables"></param>
        /// <param name="headers"></param>
        protected virtual HttpSimulator SimulateRequest(Uri url,
                                                        HttpVerb httpVerb,
                                                        NameValueCollection formVariables,
                                                        NameValueCollection headers)
        {
            HttpContext.Current = null;

            ParseRequestUrl(url);

            if (this._responseWriter == null)
            {
                this._builder = new StringBuilder();
                this._responseWriter = new StringWriter(this._builder);
            }

            SetHttpRuntimeInternals();

            string query = ExtractQueryStringPart(url);

            if (formVariables != null)
                this._formVars.Add(formVariables);

            if (this._formVars.Count > 0)
                httpVerb = HttpVerb.POST; //Need to enforce this.

            if (headers != null)
                this._headers.Add(headers);

            this._workerRequest = new SimulatedHttpRequest(ApplicationPath,
                                                           PhysicalApplicationPath,
                                                           PhysicalPath,
                                                           Page,
                                                           query,
                                                           this._responseWriter,
                                                           Host,
                                                           Port,
                                                           httpVerb.ToString());
            this._workerRequest.CurrentExecutionPath = this._currentExecutionPath;
            this._workerRequest.Form.Add(this._formVars);
            this._workerRequest.Headers.Add(this._headers);
            this._workerRequest.Cookies.Add(this._cookies);

            if (this._referer != null)
                this._workerRequest.SetReferer(this._referer);

            InitializeSession();
            InitializeApplication();

            #region Console Debug INfo

            //Console.WriteLine("host: " + _host);
            //Console.WriteLine("virtualDir: " + applicationPath);
            //Console.WriteLine("page: " + _localPath);
            //Console.WriteLine("pathPartAfterApplicationPart: " + _page);
            //Console.WriteLine("appPhysicalDir: " + _physicalApplicationPath);
            //Console.WriteLine("Request.Url.LocalPath: " + HttpContext.Current.Request.Url.LocalPath);
            //Console.WriteLine("Request.Url.Host: " + HttpContext.Current.Request.Url.Host);
            //Console.WriteLine("Request.FilePath: " + HttpContext.Current.Request.FilePath);
            //Console.WriteLine("Request.Path: " + HttpContext.Current.Request.Path);
            //Console.WriteLine("Request.RawUrl: " + HttpContext.Current.Request.RawUrl);
            //Console.WriteLine("Request.Url: " + HttpContext.Current.Request.Url);
            //Console.WriteLine("Request.Url.Port: " + HttpContext.Current.Request.Url.Port);
            //Console.WriteLine("Request.ApplicationPath: " + HttpContext.Current.Request.ApplicationPath);
            //Console.WriteLine("Request.PhysicalPath: " + HttpContext.Current.Request.PhysicalPath);
            //Console.WriteLine("HttpRuntime.AppDomainAppPath: " + HttpRuntime.AppDomainAppPath);
            //Console.WriteLine("HttpRuntime.AppDomainAppVirtualPath: " + HttpRuntime.AppDomainAppVirtualPath);
            //Console.WriteLine("HostingEnvironment.ApplicationPhysicalPath: " + HostingEnvironment.ApplicationPhysicalPath);
            //Console.WriteLine("HostingEnvironment.ApplicationVirtualPath: " + HostingEnvironment.ApplicationVirtualPath);

            #endregion

            return this;
        }


        private static string ExtractQueryStringPart(Uri url)
        {
            string query = url.Query ?? string.Empty;
            if (query.StartsWith("?"))
                return query.Substring(1);
            return query;
        }


        private static void InitializeApplication()
        {
            Type appFactoryType =
                Type.GetType(
                    "System.Web.HttpApplicationFactory, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            var appFactory = ReflectionHelper.GetStaticFieldValue<object>("_theApplicationFactory", appFactoryType);
            ReflectionHelper.SetPrivateInstanceFieldValue("_state", appFactory, HttpContext.Current.Application);
        }


        private static string RightAfter(string original, string search)
        {
            if (search.Length > original.Length || search.Length == 0)
                return original;

            int searchIndex = original.IndexOf(search, 0, StringComparison.OrdinalIgnoreCase);

            if (searchIndex < 0)
                return original;

            return original.Substring(original.IndexOf(search) + search.Length);
        }


        private void InitializeSession()
        {
            HttpContext.Current = new HttpContext(this._workerRequest);
            HttpContext.Current.Items.Clear();
            var session =
                (HttpSessionState)
                ReflectionHelper.Instantiate(typeof(HttpSessionState),
                                             new[] { typeof(IHttpSessionState) },
                                             new FakeHttpSessionState());

            HttpContext.Current.Items.Add("AspSession", session);
        }


        private void ParseRequestUrl(Uri url)
        {
            if (url == null)
                return;
            Host = url.Host;
            Port = url.Port;
            LocalPath = url.LocalPath;
            Page = StripPrecedingSlashes(RightAfter(url.LocalPath, ApplicationPath));
            this._physicalPath = Path.Combine(this._physicalApplicationPath, Page.Replace("/", @"\"));
            this._currentExecutionPath = "/" + StripPrecedingSlashes(url.LocalPath);
        }


        private void SetHttpRuntimeInternals()
        {
            //We cheat by using reflection.

            // get singleton property value
            var runtime = ReflectionHelper.GetStaticFieldValue<HttpRuntime>("_theRuntime", typeof(HttpRuntime));

            // set app path property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppPath", runtime, PhysicalApplicationPath);
            // set app virtual path property value
            const string vpathTypeName =
                "System.Web.VirtualPath, System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            object virtualPath = ReflectionHelper.Instantiate(vpathTypeName,
                                                              new[] { typeof(string) },
                                                              new object[] { ApplicationPath + "/" });
            ReflectionHelper.SetPrivateInstanceFieldValue("_appDomainAppVPath", runtime, virtualPath);

            // set codegen dir property value
            ReflectionHelper.SetPrivateInstanceFieldValue("_codegenDir", runtime, PhysicalApplicationPath);

            HostingEnvironment environment = GetHostingEnvironment();
            ReflectionHelper.SetPrivateInstanceFieldValue("_appPhysicalPath", environment, PhysicalApplicationPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_appVirtualPath", environment, virtualPath);
            ReflectionHelper.SetPrivateInstanceFieldValue("_configMapPath", environment, new ConfigMapPath(this));
        }

        #region --- Text Manipulation Methods for slashes ---

        protected static string NormalizeSlashes(string s)
        {
            if (String.IsNullOrEmpty(s) || s == "/")
                return "/";

            s = s.Replace(@"\", "/");

            //Reduce multiple slashes in row to single.
            string normalized = Regex.Replace(s, "(/)/+", "$1");
            //Strip left.
            normalized = StripPrecedingSlashes(normalized);
            //Strip right.
            normalized = StripTrailingSlashes(normalized);
            return "/" + normalized;
        }


        protected static string StripPrecedingSlashes(string s)
        {
            return Regex.Replace(s, "^/*(.*)", "$1");
        }


        protected static string StripTrailingBackSlashes(string s)
        {
            if (String.IsNullOrEmpty(s))
                return string.Empty;
            return Regex.Replace(s, @"(.*)\\*$", "$1", RegexOptions.RightToLeft);
        }


        protected static string StripTrailingSlashes(string s)
        {
            return Regex.Replace(s, "(.*)/*$", "$1", RegexOptions.RightToLeft);
        }

        #endregion

        #region Nested type: ConfigMapPath

        public class ConfigMapPath : IConfigMapPath
        {
            private readonly HttpSimulator _requestSimulation;


            public ConfigMapPath(HttpSimulator simulation)
            {
                this._requestSimulation = simulation;
            }

            #region IConfigMapPath Members

            public string GetAppPathForPath(string siteID, string path)
            {
                return this._requestSimulation.ApplicationPath;
            }


            public void GetDefaultSiteNameAndID(out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }


            public string GetMachineConfigFilename()
            {
                throw new NotImplementedException();
            }


            public void GetPathConfigFilename(string siteID, string path, out string directory, out string baseName)
            {
                throw new NotImplementedException();
            }


            public string GetRootWebConfigFilename()
            {
                throw new NotImplementedException();
            }


            public string MapPath(string siteID, string path)
            {
                string page = StripPrecedingSlashes(RightAfter(path, this._requestSimulation.ApplicationPath));
                return Path.Combine(this._requestSimulation.PhysicalApplicationPath, page.Replace("/", @"\"));
            }


            public void ResolveSiteArgument(string siteArgument, out string siteName, out string siteID)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion

        #region Nested type: FakeHttpSessionState

        public class FakeHttpSessionState : NameObjectCollectionBase, IHttpSessionState
        {
            private readonly string sessionID = Guid.NewGuid().ToString();
            private readonly HttpStaticObjectsCollection staticObjects = new HttpStaticObjectsCollection();
            private readonly object syncRoot = new Object();
            private bool isNewSession = true;
            private int timeout = 30; //minutes

            #region IHttpSessionState Members

            ///<summary>
            ///Gets or sets a session-state item value by name.
            ///</summary>
            ///
            ///<returns>
            ///The session-state item value specified in the name parameter.
            ///</returns>
            ///
            ///<param name="name">The key name of the session-state item value. </param>
            public object this[string name]
            {
                get { return BaseGet(name); }
                set { BaseSet(name, value); }
            }

            ///<summary>
            ///Gets or sets a session-state item value by numerical index.
            ///</summary>
            ///
            ///<returns>
            ///The session-state item value specified in the index parameter.
            ///</returns>
            ///
            ///<param name="index">The numerical index of the session-state item value. </param>
            public object this[int index]
            {
                get { return BaseGet(index); }
                set { BaseSet(index, value); }
            }

            ///<summary>
            ///Gets or sets the code-page identifier for the current session.
            ///</summary>
            ///
            ///<returns>
            ///The code-page identifier for the current session.
            ///</returns>
            ///
            public int CodePage { get; set; }

            ///<summary>
            ///Gets a value that indicates whether the application is configured for cookieless sessions.
            ///</summary>
            ///
            ///<returns>
            ///One of the <see cref="T:System.Web.HttpCookieMode"></see> values that indicate whether the application is configured for cookieless sessions. The default is <see cref="F:System.Web.HttpCookieMode.UseCookies"></see>.
            ///</returns>
            ///
            public HttpCookieMode CookieMode
            {
                get { return HttpCookieMode.UseCookies; }
            }

            ///<summary>
            ///Gets a value indicating whether the session ID is embedded in the URL or stored in an HTTP cookie.
            ///</summary>
            ///
            ///<returns>
            ///true if the session is embedded in the URL; otherwise, false.
            ///</returns>
            ///
            public bool IsCookieless
            {
                get { return false; }
            }

            ///<summary>
            ///Gets a value indicating whether the session was created with the current request.
            ///</summary>
            ///
            ///<returns>
            ///true if the session was created with the current request; otherwise, false.
            ///</returns>
            ///
            public bool IsNewSession
            {
                get { return this.isNewSession; }
            }

            ///<summary>
            ///Gets a value indicating whether access to the collection of session-state values is synchronized (thread safe).
            ///</summary>
            ///<returns>
            ///true if access to the collection is synchronized (thread safe); otherwise, false.
            ///</returns>
            ///
            public bool IsSynchronized
            {
                get { return true; }
            }

            ///<summary>
            ///Gets or sets the locale identifier (LCID) of the current session.
            ///</summary>
            ///
            ///<returns>
            ///A <see cref="T:System.Globalization.CultureInfo"></see> instance that specifies the culture of the current session.
            ///</returns>
            ///
            public int LCID { get; set; }

            ///<summary>
            ///Gets the current session-state mode.
            ///</summary>
            ///
            ///<returns>
            ///One of the <see cref="T:System.Web.SessionState.SessionStateMode"></see> values.
            ///</returns>
            ///
            public SessionStateMode Mode
            {
                get { return SessionStateMode.InProc; }
            }

            ///<summary>
            ///Gets the unique session identifier for the session.
            ///</summary>
            ///
            ///<returns>
            ///The session ID.
            ///</returns>
            ///
            public string SessionID
            {
                get { return this.sessionID; }
            }

            ///<summary>
            ///Gets a collection of objects declared by &lt;object Runat="Server" Scope="Session"/&gt; tags within the ASP.NET application file Global.asax.
            ///</summary>
            ///
            ///<returns>
            ///An <see cref="T:System.Web.HttpStaticObjectsCollection"></see> containing objects declared in the Global.asax file.
            ///</returns>
            ///
            public HttpStaticObjectsCollection StaticObjects
            {
                get { return this.staticObjects; }
            }

            ///<summary>
            ///Gets an object that can be used to synchronize access to the collection of session-state values.
            ///</summary>
            ///
            ///<returns>
            ///An object that can be used to synchronize access to the collection.
            ///</returns>
            ///
            public object SyncRoot
            {
                get { return this.syncRoot; }
            }

            ///<summary>
            ///Gets and sets the time-out period (in minutes) allowed between requests before the session-state provider terminates the session.
            ///</summary>
            ///
            ///<returns>
            ///The time-out period, in minutes.
            ///</returns>
            ///
            public int Timeout
            {
                get { return this.timeout; }
                set { this.timeout = value; }
            }

            ///<summary>
            ///Gets a value indicating whether the session is read-only.
            ///</summary>
            ///
            ///<returns>
            ///true if the session is read-only; otherwise, false.
            ///</returns>
            ///
            bool IHttpSessionState.IsReadOnly
            {
                get { return true; }
            }


            ///<summary>
            ///Ends the current session.
            ///</summary>
            ///
            public void Abandon()
            {
                BaseClear();
            }


            ///<summary>
            ///Adds a new item to the session-state collection.
            ///</summary>
            ///
            ///<param name="name">The name of the item to add to the session-state collection. </param>
            ///<param name="value">The value of the item to add to the session-state collection. </param>
            public void Add(string name, object value)
            {
                BaseAdd(name, value);
            }


            ///<summary>
            ///Clears all values from the session-state item collection.
            ///</summary>
            ///
            public void Clear()
            {
                BaseClear();
            }


            ///<summary>
            ///Copies the collection of session-state item values to a one-dimensional array, starting at the specified index in the array.
            ///</summary>
            ///
            ///<param name="array">The <see cref="T:System.Array"></see> that receives the session values. </param>
            ///<param name="index">The index in array where copying starts. </param>
            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }


            ///<summary>
            ///Deletes an item from the session-state item collection.
            ///</summary>
            ///
            ///<param name="name">The name of the item to delete from the session-state item collection. </param>
            public void Remove(string name)
            {
                BaseRemove(name);
            }


            ///<summary>
            ///Clears all values from the session-state item collection.
            ///</summary>
            ///
            public void RemoveAll()
            {
                BaseClear();
            }


            ///<summary>
            ///Deletes an item at a specified index from the session-state item collection.
            ///</summary>
            ///
            ///<param name="index">The index of the item to remove from the session-state collection. </param>
            public void RemoveAt(int index)
            {
                BaseRemoveAt(index);
            }

            #endregion
        }

        #endregion
    }
}
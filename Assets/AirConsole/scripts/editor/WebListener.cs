#if !DISABLE_AIRCONSOLE
using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using UnityEngine;

namespace NDream.AirConsole.Editor {
	public class WebListener {

		// private vars
		private HttpListener listener;
		private string startUpPath;
		private string prefix;
		private HttpListenerContext request;
		private Thread t;

		public void SetPath (string path) {
			startUpPath = path;
		}

		public void Start () {

			if (listener == null) {
				listener = new HttpListener ();
			}

			prefix = string.Format ("http://*:{0}/", Settings.webServerPort.ToString ());

			if (!listener.IsListening) {
				listener.Start ();

				if (!listener.Prefixes.Contains (prefix)) {
					listener.Prefixes.Add (prefix);
				}

				if (t != null && t.IsAlive) {
					t.Abort ();
				}

				t = new Thread (new ThreadStart (ClientListener));
				t.Start ();
			}
		}

		public bool IsRunning () {

			if (listener != null) {
				return listener.IsListening;
			}
			return false;
		}

		public void ClientListener () {
			while (true) {
				try {
					request = listener.GetContext ();
					ThreadPool.QueueUserWorkItem (ProcessRequest, request);
				} catch (ThreadAbortException e) {
					// ThreadAbortException is thrown when the webserver gets stopped/restarted
				} catch (Exception e) {
					if (Settings.debug.error) {
						Debug.LogError (e.ToString ());
					}
				}
			}
		}

		public void ProcessRequest (object listenerContext) {

			try {
               
				var context = (HttpListenerContext)listenerContext;
				string rawUrl = context.Request.RawUrl;

				// conditions if editor version gets called
				if (startUpPath.Contains (Settings.WEBTEMPLATE_PATH)) {
					// remove query parameters
					rawUrl = rawUrl.Split ('?') [0];
					// translate screen.html to index.html
					rawUrl = rawUrl.Replace ("screen.html", "index.html");
				}

				string filename = Path.GetFileName (rawUrl);
				string path = startUpPath + System.Uri.UnescapeDataString(rawUrl);;

				byte[] msg;

				if (!File.Exists (path)) {
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					msg = Encoding.UTF8.GetBytes ("<html><head><title>AirConsole Error</title></head><body><h1>AirWebserver can't find resources!</h1></body></html>");

				} else {
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					msg = File.ReadAllBytes (path);
					context.Response.ContentType = ReturnMIMEType (Path.GetExtension (filename));
				}

				context.Response.ContentLength64 = msg.Length;

				using (Stream s = context.Response.OutputStream) {
					s.Write (msg, 0, msg.Length);
				}
			} catch (Exception e) {

				if (Settings.debug.error) {

					if (e.Message != "Write failure") {
						Debug.LogError (e.Message);
					}
				}
			}
		}

		public string ReturnMIMEType (string filename) {

			switch (filename) {
			case ".txt":
				return "text/plain";
			case ".gif":
				return "image/gif";
			case ".png":
				return "image/png";
			case ".jpg":
			case "jpeg":
				return "image/jpeg";
			case ".bmp":
				return "image/bmp";
			case ".wav":
				return "audio/wav";
			case ".mp3":
				return "audio/mp3";
			case ".html":
				return "text/html";
			case ".htm":
				return "text/html";
			case ".css":
				return "text/css";
			case ".js":
				return "application/javascript";
			default:
				return "application/octet-stream";
			}
		}

		public void Stop () {
			t.Abort ();
			listener.Stop ();
		}

		public void Restart () {
			Stop ();
			Start ();
		}
	}
}
#endif
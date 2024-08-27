using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace AI.Avalab.ToolkitEditor
{
    internal class ListenServer
    {
        HttpListener listener;

        ~ListenServer()
        {
            Stop();
        }

        public Action<string> OnReceiveCode;

        public void Start()
        {
            if (listener == null)
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:4444/");
            }
            listener.Start();

            var mainContext = SynchronizationContext.Current;

            void contextCallback(System.IAsyncResult result)
            {
                mainContext.Post((_) =>
                {
                    if (listener == null)
                    {
                        return;
                    }
                    HttpListenerContext context = listener.EndGetContext(result);
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    foreach (var query in request.QueryString.AllKeys)
                    {
                        if (query == "code")
                        {
                            var code = request.QueryString[query];
                            Debug.Log("code is " + code);

                            OnReceiveCode.Invoke(code);
                            break;
                        }
                    }

                    response.StatusCode = 200;

                    var html = "<html><head><meta charset='UTF-8'></head><body>必要なリソースが足りません。AvalabToolkit for Unityを再インポートしてください</body></html>";
                    string[] htmlAssetGuids = AssetDatabase.FindAssets("redirect t:TextAsset");
                    if (htmlAssetGuids.Length > 0)
                    {
                        var htmlAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(htmlAssetGuids[0]));
                        html = htmlAsset.text;
                    }

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);

                    response.Close(buffer, false);

                    listener.BeginGetContext(contextCallback, listener);
                }, null);
            }
            listener.BeginGetContext(contextCallback, listener);
        }

        public void Stop()
        {
            if (listener != null)
            {
                listener.Stop();
                listener.Close();
                listener = null;
            }
        }
    }
}

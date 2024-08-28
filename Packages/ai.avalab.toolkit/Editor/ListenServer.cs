/**
 * Copyright 2024 Nameraka Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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

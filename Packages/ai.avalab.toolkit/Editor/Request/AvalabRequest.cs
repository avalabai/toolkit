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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AI.Avalab.ToolkitEditor
{
    public class AvalabRequest
    {
        protected string method = "GET";
        protected string url = "";
        public Action<float> OnProgress;

        public class ErrorException : Exception
        {
            public UnityWebRequest.Result result;
            public string error;
            public string text;
            public string requestId;
        }
        
        public class None
        {
        }

        protected virtual void CustomRequest(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Avalab-Api-Key", AppConstants.AVALAB_API_KEY);
        }

        public Task<(long responseCode, R responseBody)> Request<P, R>(P param)
        {
            var paramJson = JsonUtility.ToJson(param);
            var request = new UnityWebRequest(url, method);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(paramJson);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            CustomRequest(request);

            var taskCompletionSource = new TaskCompletionSource<(long, R)>();

            var requestOperator = request.SendWebRequest();
            if (OnProgress != null)
            {
                float rate = 0;
                var mainContext = SynchronizationContext.Current;
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (requestOperator.progress >= 1.0f || requestOperator.isDone)
                        {
                            break;
                        }
                        else if (Math.Floor(rate * 100) < Math.Floor(requestOperator.progress * 100))
                        {
                            rate = (float)Math.Floor(requestOperator.progress * 100) * 0.01f;
                            mainContext.Post(_ => OnProgress.Invoke(rate), null);
                        }
                        Thread.Sleep(10);
                    }
                });
            }

            requestOperator.completed += (value) =>
            {
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    var requestId = request.GetResponseHeader("X-Avalab-Request-ID") ?? "";
                    taskCompletionSource.SetException(new ErrorException()
                    {
                        error = request.error,
                        result = request.result,
                        text = request.downloadHandler.text,
                        requestId = requestId,
                    });
                }
                else
                {
                    var text = request.downloadHandler.text;
                    if (text == "null")
                    {
                        text = "{}";
                    }
                    var response = JsonUtility.FromJson<R>(text);
                    taskCompletionSource.SetResult((request.responseCode, response));
                }
            };

            return taskCompletionSource.Task;
        }
    }
}

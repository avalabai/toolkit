using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AI.Avalab.ToolkitEditor
{
    public class ThumbnailUploadRequest : AuthorizedRequest
    {
        string uploadFilePath;

        public ThumbnailUploadRequest(string access_token) : base(access_token)
        {
            method = "PUT";
        }

        protected override void CustomRequest(UnityWebRequest request)
        {
            base.CustomRequest(request);
            request.SetRequestHeader("Content-Type", "image/png");
            request.uploadHandler = (UploadHandler)new UploadHandlerFile(uploadFilePath);
        }

        public async Task<None> Request(string uploadUrl, string uploadFilePath)
        {
            this.url = uploadUrl;
            this.uploadFilePath = uploadFilePath;

            var param = new None();

            var response = await Request<None, None>(param);

            return response.responseBody;
        }
    }
}
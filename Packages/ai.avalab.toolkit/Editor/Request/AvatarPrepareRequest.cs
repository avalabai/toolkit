using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI.Avalab.ToolkitEditor
{
    public class AvatarPrepareRequest : AuthorizedRequest
    {
        [System.Serializable]
        public class Param
        {
            public string thumbnail_image_type = "image/png";
            public string avatar_type = "assetbundle";
        }

        [System.Serializable]
        public class Response
        {
            public string upload_url;
            public string thumbnail_upload_url;
            public string avatar_id;
        }

        public AvatarPrepareRequest(string access_token) : base(access_token)
        {
            method = "POST";
            url = AppConstants.AVALAB_API_HOST + "/v1/avatar/prepare";
        }

        public async Task<Response> Request()
        {
            var param = new Param();

            var response = await Request<Param, Response>(param);

            return response.responseBody;
        }
    }
}

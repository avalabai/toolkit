using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI.Avalab.ToolkitEditor
{
    public class PostLoraRequest : AuthorizedRequest
    {
        [System.Serializable]
        public class Param
        {
            public string avatar_id;
        }

        public PostLoraRequest(string access_token) : base(access_token)
        {
            method = "POST";
            url = AppConstants.AVALAB_API_HOST + "/v1/lora";
        }

        public async Task<None> Request(string avatarId)
        {
            var param = new Param
            {
                avatar_id = avatarId
            };

            var response = await Request<Param, None>(param);

            return response.responseBody;
        }
    }
}

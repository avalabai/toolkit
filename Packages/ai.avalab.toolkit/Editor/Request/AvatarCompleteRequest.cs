using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI.Avalab.ToolkitEditor
{
    public class AvatarCompleteRequest : AuthorizedRequest
    {
        public AvatarCompleteRequest(string access_token) : base(access_token)
        {
            method = "POST";
        }

        public async Task<None> Request(string avatarId)
        {
            url = AppConstants.AVALAB_API_HOST + "/v1/avatar/" + avatarId + "/complete";
            var param = new None();

            var response = await Request<None, None>(param);

            return response.responseBody;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AI.Avalab.ToolkitEditor
{
    public class AuthorizedRequest : AvalabRequest
    {
        protected string access_token;

        public AuthorizedRequest(string access_token)
        {
            this.access_token = access_token;
        }

        protected override void CustomRequest(UnityWebRequest request)
        {
            base.CustomRequest(request);
            request.SetRequestHeader("x-avalab-oauth-token", access_token);
        }
    }
}

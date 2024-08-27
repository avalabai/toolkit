using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI.Avalab.ToolkitEditor
{
    public class GetTokenRequest : AvalabRequest
    {
        [System.Serializable]
        public class Param
        {
            public string code;
            public string grant_type;
            public string client_id;
            public string redirect_uri;
        }

        [System.Serializable]
        public class Response
        {
            public string access_token;
            public string refresh_token;
            public string expires_at;
            public List<string> scope;
        }

        public GetTokenRequest()
        {
            method = "POST";
            url = AppConstants.AVALAB_API_HOST + "/v1/authorize/token";
        }

        public async Task<Response> Request(string code)
        {
            Debug.Log("Request");
            var param = new Param();
            param.grant_type = "authorization_code";
            param.code = code;
            param.client_id = AppConstants.AVALAB_CLIENT_ID;
            param.redirect_uri = "http://localhost:" + AppConstants.AVALAB_AUTH_REDIRECT_PORT + "/";

            var response = await Request<Param, Response>(param);

            Debug.Log("responseBody");
            return response.responseBody;
        }
    }
}

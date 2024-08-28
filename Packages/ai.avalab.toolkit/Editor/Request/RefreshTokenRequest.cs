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

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI.Avalab.ToolkitEditor
{
    public class RefreshTokenRequest : AvalabRequest
    {
        [System.Serializable]
        public class Param
        {
            public string refresh_token;
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

        public RefreshTokenRequest()
        {
            method = "POST";
            url = AppConstants.AVALAB_API_HOST + "/v1/authorize/token";
        }

        public async Task<Response> Request(string refreshToken)
        {
            var param = new Param();
            param.grant_type = "refresh_token";
            param.refresh_token = refreshToken;
            param.client_id = AppConstants.AVALAB_CLIENT_ID;
            param.redirect_uri = "http://localhost:" + AppConstants.AVALAB_AUTH_REDIRECT_PORT + "/";

            var response = await Request<Param, Response>(param);

            return response.responseBody;
        }
    }
}

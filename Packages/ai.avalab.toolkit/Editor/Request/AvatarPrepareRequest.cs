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

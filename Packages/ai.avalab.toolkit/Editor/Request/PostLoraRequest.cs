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

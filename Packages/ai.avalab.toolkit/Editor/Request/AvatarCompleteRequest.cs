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

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

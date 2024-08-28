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
using UnityEngine.Networking;

namespace AI.Avalab.ToolkitEditor
{
    public class GetUserRequest : AuthorizedRequest
    {
        [System.Serializable]
        public class Response
        {
            public string id;
            public string name;
            public string display_name;
            public string last_signed_in_at;
            public int available_points;
            public int generation_left_today;
            public int generation_left_monthly;
            public int model_registration_left;
            public int model_retention_left;
            public bool has_vrh_authz;
            public string preferred_language;
            public class Subscription
            {
                public string plan_id;
                public string last_renewed_at;
                public string since;
                public string expires_at;
                public string title;
                public string description;
                public string cancellation_requested_at;
                public int generation_cap_per_day;
                public int generation_cap_per_month;
                public bool is_no_watermark;
                public bool is_annual;
                public bool variable_image_size;
            };
            public Subscription subscription;
            public bool has_never_subscribed_any_plan;
            public bool is_developer;
        }

        public GetUserRequest(string access_token) : base(access_token)
        {
            method = "GET";
            url = AppConstants.AVALAB_API_HOST + "/v1/user/current";
        }

        public async Task<Response> Request()
        {
            var response = await Request<None, Response>(new None());

            return response.responseBody;
        }
    }
}

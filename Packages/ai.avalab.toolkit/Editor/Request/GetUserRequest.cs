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

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

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace AI.Avalab.ToolkitEditor
{
    public class ToolkitWindow : EditorWindow
    {
        Texture2D AvalabImage;

        public string SelectedAvatarName;
        Vector2 scrollOffset = Vector2.zero;

        readonly ListenServer listenServer = new();
        public GetUserRequest.Response user = null;
        Texture2D testScreenshot1;
        Texture2D testScreenshot2;
        bool avatarUploadSucceeded;

        public string ProcessingName = "";
        public float ProcessingProgress = -1;
        public string ErrorLog = "";

        [System.Serializable]
        class AuthorizationData
        {
            public string access_token;
            public string refresh_token;
            public string expires_at_string;

            public DateTime expires_at
            {
                set
                {
                    expires_at_string = value.ToString("yyyyMMddHHmmss");
                }
                get
                {
                    if (expires_at_string == null || expires_at_string.Length == 0)
                    {
                        return DateTime.MinValue;
                    }
                    return DateTime.ParseExact(expires_at_string, "yyyyMMddHHmmss", CultureInfo.CurrentCulture, DateTimeStyles.None);
                }
            }

            public bool IsExpired()
            {
                return expires_at.CompareTo(DateTime.Now) < 0;
            }
        }

        void OnGUI()
        {
            var titleStyle = EditorStyles.boldLabel;

            // ロゴの表示
            if (AvalabImage == null)
            {
                var guids = AssetDatabase.FindAssets("avalab_logo t:texture");
                if (guids.Length > 0)
                {
                    AvalabImage = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            GUILayout.Label(AvalabImage);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.LinkButton("サポート：support@avalab.ai"))
            {
                Application.OpenURL("mailto:support@avalab.ai");
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.LinkButton("Discord：Avalab"))
            {
                Application.OpenURL("https://discord.gg/k5JvrhNS79");
            }
            GUILayout.EndHorizontal();

            scrollOffset = EditorGUILayout.BeginScrollView(scrollOffset);

            // 認証
            var authorizationData = LoadAuthorizationData();
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Avalab認証", titleStyle);
            if (authorizationData == null)
            {
                if (!HttpListener.IsSupported)
                {
                    EditorGUILayout.HelpBox("このUnityでは認証機能が使用できません。", MessageType.Error);
                }
                else if (GUILayout.Button("Avalab.aiで認証を行う"))
                {
                    SaveAuthorizationData(null);
                    StartAuthServer();
                    Application.OpenURL(AppConstants.AVALAB_APP_HOST + "/authorize?client_id=" + AppConstants.AVALAB_CLIENT_ID + "&redirect_uri=http%3A%2F%2Flocalhost%3A" + AppConstants.AVALAB_AUTH_REDIRECT_PORT + "%2F");
                }
            }
            else
            {
                if (user == null)
                {
                    EditorGUILayout.LabelField("認証済み");
                }
                else
                {
                    EditorGUILayout.LabelField(string.Format("認証済み(アバター登録回数残り {0} 回)", user.model_registration_left));
                }
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("アバター選択", titleStyle);

            // アバター選択
            List<string> avatarNames = AvatarBuilder.ListAvatarNames();
            if (avatarNames.Count == 0)
            {
                EditorGUILayout.Popup(0, new string[1] { "アバターが見つかりません" });
            }
            else
            {
                if (SelectedAvatarName == null || SelectedAvatarName.Length == 0)
                {
                    SelectedAvatarName = avatarNames[0];
                }
                var selectedAvatarIndex = avatarNames.IndexOf(SelectedAvatarName);
                if (selectedAvatarIndex == -1)
                {
                    selectedAvatarIndex = 0;
                }
                var selectAvatarIndex = EditorGUILayout.Popup(selectedAvatarIndex, avatarNames.ToArray());
                if (selectedAvatarIndex != selectAvatarIndex)
                {
                    testScreenshot1 = null;
                    testScreenshot2 = null;
                }

                SelectedAvatarName = avatarNames[selectAvatarIndex];
            }
            GUILayout.EndVertical();

            // アバターを取得
            var animators = UnityEngine.Object.FindObjectsOfType<Animator>();
            var avatarAnimator = Array.Find(animators, animator => animator.name == SelectedAvatarName);

            if (avatarAnimator)
            {
                // Validation系
                EditorGUILayout.Space();
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Validations", titleStyle);
                var noError = LayoutValidationPart();
                GUILayout.EndVertical();

                // サンプルの撮影
                EditorGUILayout.Space();
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("テスト撮影", titleStyle);
                var wrapStyle = new GUIStyle
                {
                    wordWrap = true
                };
                wrapStyle = EditorStyles.wordWrappedLabel;
                EditorGUILayout.LabelField("アバターが正しく学習できるかどうかを確認するためにテスト画像を撮影します。\n「顔のアップ」と「全身ポーズ」の二枚が撮影されますので、アバターアップロード前に確認してください。\n全身ポーズの上下に余白が多すぎる場合、学習精度が低くなる可能性がありますので、ちょうど頭から足がぴったり収まるようにアバターのBoundsを調整してください。", wrapStyle);
                if (noError)
                {
                    if (GUILayout.Button("撮影する"))
                    {
                        var screenshots = AvatarBuilder.TakeTestScreenshot(avatarAnimator);
                        if (screenshots == null)
                        {
                            AddErrorLog("テスト撮影に失敗しました。Consoleのエラーを確認してください");
                        }
                        else
                        {
                            testScreenshot1 = screenshots.bustUp;
                            testScreenshot2 = screenshots.fullBody;
                        }

                    }
                    if (testScreenshot1 != null && testScreenshot2 != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(testScreenshot1, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 20), GUILayout.Height(EditorGUIUtility.currentViewWidth / 2 - 20));
                        GUILayout.Label(testScreenshot2, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2 - 20), GUILayout.Height(EditorGUIUtility.currentViewWidth / 2 - 20));
                        EditorGUILayout.EndHorizontal();
                    }

                }
                else
                {
                    EditorGUILayout.HelpBox("エラーがある場合にはテスト撮影はできません", MessageType.Info);
                }
                GUILayout.EndVertical();

                // ビルド・アップロード
                EditorGUILayout.Space();
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("アバターアップロード", titleStyle);
                if (noError && user != null && user.model_registration_left > 0)
                {
                    if (GUILayout.Button("アバターをアップロードする"))
                    {
                        GUI.FocusControl("");
                        ErrorLog = "";
                        UploadAvatar(avatarAnimator);
                    }
                    if (ProcessingName.Length > 0 && ProcessingProgress >= 0)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        EditorGUI.ProgressBar(rect, ProcessingProgress, ProcessingName);
                    }
                    if (ErrorLog.Length > 0)
                    {
                        EditorGUILayout.HelpBox("アバターアップロードに失敗しました", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("エラーがある場合にはアバターのアップロードはできません", MessageType.Info);
                }
                GUILayout.EndVertical();

                if (avatarUploadSucceeded)
                {
                    EditorGUILayout.Space();
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    GUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("アバターのアップロードが完了しました", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    if (EditorGUILayout.LinkButton("消す"))
                    {
                        avatarUploadSucceeded = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("約1日でアバターの登録が完了し、ご登録のGoogleアカウントにメールが届きます。\nその後、あなたのアバターのイラストが生成できるようになります。\nAvalab.aiでアバターが登録中になっているか確認できます", MessageType.None);

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (EditorGUILayout.LinkButton("Avalab.aiを開く"))
                    {
                        Application.OpenURL(AppConstants.AVALAB_APP_HOST + "/generate");
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("アバターが見つかりません", MessageType.Error);
            }

            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("ログ", titleStyle);

            if (GUILayout.Button("ログをクリア"))
            {
                GUI.FocusControl("");
                ErrorLog = "";
                ProcessingName = "";
            }
            EditorGUILayout.SelectableLabel(ErrorLog, EditorStyles.textArea, GUILayout.MinHeight(90), GUILayout.ExpandHeight(true));

            GUILayout.EndVertical();

#if AVALAB_TOOLKIT_DEVELOPMENT
            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("デバッグ", titleStyle);

            if (GUILayout.Button("GetUser"))
            {
                GetUser();
            }

            if (GUILayout.Button("認証情報クリア"))
            {
                SaveAuthorizationData(null);
            }

            var authorizationDataDisplay = LoadAuthorizationData();
            EditorGUILayout.PropertyField(new SerializedObject(this).FindProperty("user"));

            GUILayout.EndVertical();
#endif

            EditorGUILayout.EndScrollView();
        }

        bool LayoutValidationPart()
        {
            bool isError = false;

            // 認証したかどうか
            var authorizationData = LoadAuthorizationData();
            if (authorizationData == null)
            {
                EditorGUILayout.HelpBox("Avalab認証を行っていません", MessageType.Error);
                isError = true;
            }

            // ユーザーデータあるか
            else if (user == null)
            {
                EditorGUILayout.HelpBox("Avalabサーバーからユーザー情報を取得していません", MessageType.Error);
                if (GUILayout.Button("ユーザー情報を取得する"))
                {
                    GetUser();
                }
            }
            else if (user.model_registration_left < 1)
            {
                EditorGUILayout.HelpBox("アバターの登録回数が足りません。Avalab公式サイトで追加して、ユーザー情報再取得ボタンを押してください", MessageType.Error);
                if (GUILayout.Button("ユーザー情報を再取得する"))
                {
                    GetUser();
                }
            }

            // BuildSupportLinuxのチェック
            bool isSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
#if AVALAB_TOOLKIT_DEVELOPMENT
            isSupported = false;
#endif
            if (!isSupported)
            {
                EditorGUILayout.HelpBox("Avalab Toolkit for Unityでのモデルのアップロードには『Linux Build Support』をインストールする必要があります", MessageType.Error);

                string url = "";
#if UNITY_EDITOR_WIN
                url = string.Format("https://download.unity3d.com/download_unity/{0}/TargetSupportInstaller/UnitySetup-Linux-Mono-Support-for-Editor-{1}.exe", UnityEditorInternal.InternalEditorUtility.GetUnityBuildHash(), UnityEditorInternal.InternalEditorUtility.GetUnityDisplayVersion());
#elif UNITY_EDITOR_OSX
                url = string.Format("https://download.unity3d.com/download_unity/{0}/MacEditorTargetInstaller/UnitySetup-Linux-Mono-Support-for-Editor-{1}.pkg", UnityEditorInternal.InternalEditorUtility.GetUnityBuildHash(), UnityEditorInternal.InternalEditorUtility.GetUnityDisplayVersion());
#elif UNITY_EDITOR_LINUX
                url = string.Format("https://download.unity3d.com/download_unity/{0}/LinuxEditorTargetInstaller/UnitySetup-Linux-IL2CPP-Support-for-Editor-{1}.tar.xz", UnityEditorInternal.InternalEditorUtility.GetUnityBuildHash(), UnityEditorInternal.InternalEditorUtility.GetUnityDisplayVersion());
#endif
                if (url.Length > 0)
                {
                    if (GUILayout.Button("『Linux Build Support』をダウンロードする"))
                    {
                        Application.OpenURL(url);
                    }
                }
                if (GUILayout.Button("Build Support手動インストールページを開く"))
                {
                    Application.OpenURL(string.Format("https://unity.com/ja/releases/editor/whats-new/{0}#installs", UnityEditorInternal.InternalEditorUtility.GetUnityVersion().ToString(3)));
                }
            }

            // アバターを取得
            var avatarNames = AvatarBuilder.ListAvatarNames();
            if (avatarNames == null || avatarNames.Count == 0)
            {
                EditorGUILayout.HelpBox("アバターがありません。RigのAnimationTypeがHumanoidになっているアバターをシーンに配置してください", MessageType.Error);
                isError = true;
                return false;
            }
            if (SelectedAvatarName == null || SelectedAvatarName.Length == 0)
            {
                EditorGUILayout.HelpBox("アバターを選択してください", MessageType.Error);
                isError = true;
                return false;
            }
#if VRC_AVATAR_SDK3
            var avatars = GameObject.FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            var avatar = Array.Find(avatars, avatar => avatar.name == SelectedAvatarName);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox("アバターを選択してください", MessageType.Error);
                isError = true;
                return false;
            }
            if (avatar.GetComponent<Animator>() == null)
            {
                EditorGUILayout.HelpBox("アバターにAnimatorコンポーネントがありません", MessageType.Error);
                isError = true;
                return false;
            }
#endif
            var animators = GameObject.FindObjectsOfType<Animator>();
            var animator = Array.Find(animators, animator => animator.name == SelectedAvatarName);
            if (animator == null)
            {
                EditorGUILayout.HelpBox("アバターを選択してください", MessageType.Error);
                isError = true;
                return false;
            }

            // Humanoidアバターになってるか
            if (!animator.isHuman)
            {
                EditorGUILayout.HelpBox("アバターのAnimationTypeがHumanoidになっていません。RigのAnimationTypeがHumanoidになっているアバターをシーンに配置してください", MessageType.Error);
                isError = true;
                return false;
            }

            // ボーンにSpineかChestがあるか
            if (animator.GetBoneTransform(HumanBodyBones.Chest) == null || animator.GetBoneTransform(HumanBodyBones.Spine) == null)
            {
                EditorGUILayout.HelpBox("アバターにはChestボーンかSpineボーンが必要になります", MessageType.Error);
                isError = true;
            }

            // テクスチャ抜けがないか
            // -> テクスチャのスロットはたくさんあるので判定できない！！

            // RendererのAABBが異常に大きくないかチェック
            // VRCSDKがあるならAvatarDescriptionのViewPointを使用する
            float avatarHeight = AvatarBuilder.GetAvatarHeight(animator);
            Bounds boundsSize = AvatarBuilder.CalculateBoundsForEachChildren(animator.gameObject);
            var limit = avatarHeight * 4;

            if (boundsSize.size.x > limit || boundsSize.size.y > limit || boundsSize.size.z > limit)
            {
                EditorGUILayout.HelpBox(string.Format("アバターAABB(x:{0:N2}, y:{1:N2}, z:{2:N2})が大きすぎます。\nAvalabではAABBが大きすぎると学習の精度に影響が出る可能性があります。使用しているメッシュオブジェクトのBoundsを修正して小さくしてください", boundsSize.size.x, boundsSize.size.y, boundsSize.size.z), MessageType.Info);
            }

            // パーティクスシステムがあったら念のため警告
            if (animator.GetComponentInChildren<ParticleSystem>())
            {
                EditorGUILayout.HelpBox("アバターにParticleSystemが使用されています。Avalabではパーティクルは正しく学習できない可能性があります", MessageType.Warning);
            }

            // MissingComponentをチェック
            var missingComponentObject = AvatarBuilder.GetMissingComponents(animator);
            if (missingComponentObject.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Format("MissingScriptが{0}個あります", missingComponentObject.Count), MessageType.Warning);
                if (GUILayout.Button("MissincScriptのあるオブジェクトを選択する"))
                {
                    Selection.objects = missingComponentObject.ToArray();
                }
            }

            // 念のために削除されるコンポーネントを列挙してお知らせ
            var dennyComponentNames = AvatarBuilder.GetDennyComponentNames(animator);
            if (dennyComponentNames.Count > 0)
            {
                EditorGUILayout.HelpBox("以下のコンポーネントはビルド時にアバターから削除されます\n" + string.Join("\n", dennyComponentNames), MessageType.Info);
            }

            if (!isError)
            {
                EditorGUILayout.HelpBox("選択されたアバターはアップロードが可能です", MessageType.Info);
            }

            return !isError;
        }

        void SetProgress(float progress, string text)
        {
            ProcessingProgress = progress;
            ProcessingName = text;
        }
        void SetProgress(float progress)
        {
            ProcessingProgress = progress;
        }

        void AddErrorLog(string errorLog)
        {
            if (ErrorLog.Length > 0)
            {
                ErrorLog += "\n";
            }
            ErrorLog += errorLog;
        }

        void AddErrorLog(string title, AvalabRequest.ErrorException e)
        {
            AddErrorLog(string.Format("{0}({3})\n{1}\n{2}", title, e.error, e.text, e.requestId));
        }

        async void UploadAvatar(Animator targetAvatar)
        {
            ErrorLog = "";
            SetProgress(0, "アバタービルド中");

            try
            {
                var paths = AvatarBuilder.BuildAvatar(targetAvatar);

                SetProgress(0, "認証情報取得");
                var authorizationData = await GetAuthorizationData();

                SetProgress(0, "アバター登録準備中");

                var prepareData = await new AvatarPrepareRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request();
                Debug.Log(string.Format("prepare : avatar_id = {0}, upload_url = {1}, thumbnail_url = {2}", prepareData.avatar_id, prepareData.upload_url, prepareData.thumbnail_upload_url));

                SetProgress(0, "モデルデータアップロード中");
                await new AvatarUploadRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request(prepareData.upload_url, paths.avatarFilePath);

                SetProgress(0, "アバターサムネイルアップロード中");
                await new ThumbnailUploadRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request(prepareData.thumbnail_upload_url, paths.thumbnailFilePath);

                SetProgress(0, "アバターデータアップロード完了処理中");
                await new AvatarCompleteRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request(prepareData.avatar_id);

                SetProgress(0, "アバター登録申請中");
                await new PostLoraRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request(prepareData.avatar_id);

                SetProgress(0, "ユーザー情報再取得中");
                this.user = await new GetUserRequest(authorizationData.access_token) { OnProgress = p => SetProgress(p) }.Request();

                SetProgress(1, "アバター登録申請完了");

                avatarUploadSucceeded = true;
                if (EditorUtility.DisplayDialog("アバターアップロード完了", "約1日でアバターの登録が完了し、ご登録のGoogleアカウントにメールが届きます。\nその後、あなたのアバターのイラストが生成できるようになります。\nAvalab.aiでアバターが登録中になっているか確認できます", "Avalab.aiを開く", "閉じる"))
                {
                    Application.OpenURL(AppConstants.AVALAB_APP_HOST + "/generate");
                }
            }
            catch (AvalabRequest.ErrorException e)
            {
                SetProgress(1, "アップロードエラー");
                AddErrorLog(string.Format("アップロードエラー", e));
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetProgress(1, "アップロードエラー");
                AddErrorLog(string.Format("アップロードエラー: {0}, {1}", e.Message, e.StackTrace));
                return;
            }
        }


        void StartAuthServer()
        {
            listenServer.OnReceiveCode = (code) =>
            {
                Task<GetTokenRequest.Response> task = null;
                task = new GetTokenRequest().Request(code);
                var awaiter = task.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        var response = awaiter.GetResult();
                        SaveAuthorizationData(new AuthorizationData()
                        {
                            access_token = response.access_token,
                            refresh_token = response.refresh_token,
                            expires_at = DateTime.Parse(response.expires_at),
                        });
                    }
                    catch (AvalabRequest.ErrorException e)
                    {
                        Debug.LogException(e);
                        AddErrorLog("認証情報の取得に失敗しました", e);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        AddErrorLog("認証情報の取得でエラーが発生しました");
                    }
                });
            };
            listenServer.Start();
        }


        void StopAuthServer()
        {
            listenServer.Stop();
        }


        async Task<AuthorizationData> GetAuthorizationData()
        {
            var authorizationData = LoadAuthorizationData();
            if (authorizationData == null)
            {
                throw new Exception("Avalab認証情報がありません。再度認証を行ってください");
            }

            if (authorizationData.IsExpired())
            {
                var response = await new RefreshTokenRequest().Request(authorizationData.refresh_token);
                SaveAuthorizationData(new AuthorizationData()
                {
                    access_token = response.access_token,
                    refresh_token = response.refresh_token,
                    expires_at = DateTime.Parse(response.expires_at),
                });
                authorizationData = LoadAuthorizationData();
            }

            return authorizationData;
        }

        public async void GetUser()
        {
            try
            {
                var authorizationData = await GetAuthorizationData();

                this.user = await new GetUserRequest(authorizationData.access_token).Request();
            }
            catch (AvalabRequest.ErrorException e)
            {
                SaveAuthorizationData(null);
                AddErrorLog("認証情報の取得に失敗しました", e);
            }
            catch (Exception e)
            {
                SaveAuthorizationData(null);
                Debug.LogException(e);
                AddErrorLog("認証情報の取得でエラーが発生しました");
            }
        }

        void SaveAuthorizationData(AuthorizationData data)
        {
            if (data == null)
            {
                PlayerPrefs.DeleteKey(AppConstants.AVALAB_AUTHORIZATION_DATA_KEY);
                return;
            }
            var dataJson = JsonUtility.ToJson(data);
            var bytesString = dataJson;

            byte[] bytes = Encoding.UTF8.GetBytes(dataJson);
            try
            {
                bytes = ProtectedData.Protect(bytes, Encoding.UTF8.GetBytes("avalab"), DataProtectionScope.CurrentUser);
                bytesString = Convert.ToBase64String(bytes);
            }
            catch (CryptographicException e)
            {
                // 例外があれば握り潰し、暗号化せずに保存する
                Debug.LogException(e);
            }
            catch (Exception e)
            {
                // 例外があれば握り潰し、暗号化せずに保存する
                Debug.LogException(e);
            }
            PlayerPrefs.SetString(AppConstants.AVALAB_AUTHORIZATION_DATA_KEY, bytesString);
        }

        AuthorizationData LoadAuthorizationData()
        {
            var protectedString = PlayerPrefs.GetString(AppConstants.AVALAB_AUTHORIZATION_DATA_KEY, "");
            if (protectedString == null || protectedString.Length == 0)
            {
                return null;
            }
            var dataJson = protectedString;

            try
            {
                var protectedBytes = Convert.FromBase64String(protectedString);
                var rawBytes = ProtectedData.Unprotect(protectedBytes, Encoding.UTF8.GetBytes("avalab"), DataProtectionScope.CurrentUser);
                dataJson = Encoding.UTF8.GetString(rawBytes);
            }
            catch (CryptographicException e)
            {
                // 握りつぶす
                Debug.LogException(e);
                PlayerPrefs.SetString(AppConstants.AVALAB_AUTHORIZATION_DATA_KEY, "");
            }
            catch (Exception e)
            {
                // 握りつぶす
                Debug.LogException(e);
                PlayerPrefs.SetString(AppConstants.AVALAB_AUTHORIZATION_DATA_KEY, "");
            }

            try
            {
                var data = JsonUtility.FromJson<AuthorizationData>(dataJson);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private void OnDestroy()
        {
            StopAuthServer();
        }

        [MenuItem("Avalab/コントロールパネル")]
        static void Init()
        {
            ToolkitWindow window = (ToolkitWindow)EditorWindow.GetWindow<ToolkitWindow>("Avalab Toolkit for Unity");
            window.Show();
        }
    }

}
#endif

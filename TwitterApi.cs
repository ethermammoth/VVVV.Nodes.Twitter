﻿using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using TweetSharp;

namespace VVVV.TwitterApi.Nodes
{
    [PluginInfo(Name = "Twitter",
                Category = "Network",
                Version = "",
                Author = "ethermammoth",
                Tags = "twitter",
                Help = "Provides access to twitter through TweetSharp API",
                AutoEvaluate = true)]
    public class TwitterApiAsync : IPluginEvaluate
    {

        //INPUT
        [Input("Auth App", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthApp;

        [Input("Auth User", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthUser;

        [Input("Auth Method", IsSingle = true)]
        ISpread<bool> FAuthMethod;

        [Input("Token Verifier", IsSingle = true)]
        ISpread<string> FTokenVerifier;

        [Input("Token Verifier Entered", IsSingle = true, IsBang = true)]
        ISpread<bool> FTokenVerifierEntered;

        [Input("Callback URL", IsSingle = true)]
        ISpread<string> FCallbackUrl;

        [Input("Callback Token", IsSingle = true)]
        ISpread<string> FCallbackToken;

        [Input("Callback Verifier", IsSingle = true)]
        ISpread<string> FCallbackVerifier;

        [Input("Callback Entered", IsSingle = true, IsBang = true)]
        ISpread<bool> FCallbackEntered;

        [Input("Access Token", IsSingle = true)]
        ISpread<string> FAccessTokenInput;

        [Input("Access Token Secret", IsSingle = true)]
        ISpread<string> FAccessTokenSecretInput;

        [Input("Verify Token", IsSingle = true, IsBang = true)]
        ISpread<bool> FVerifyToken;

        [Input("Tweet Message", IsSingle = true)]
        ISpread<string> FTweetMessage;

        [Input("Send Tweet", IsSingle = true, IsBang = true)]
        ISpread<bool> FSendTweet;

        [Input("Tweet Image", IsSingle = true)]
        ISpread<string> FTweetImage;

        [Input("Send Image Tweet", IsSingle = true, IsBang = true)]
        ISpread<bool> FSendImageTweet;

        [Input("Search Hashtag", IsSingle = true)]
        ISpread<string> FSearchHashtag;

        [Input("Search Count", IsSingle = true)]
        ISpread<int> FSearchCount;

        [Input("Search", IsSingle = true, IsBang = true)]
        ISpread<bool> FSearch;

        [Input("Clear Search", IsSingle = true, IsBang = true)]
        ISpread<bool> FClearSearch;

        [Input("Logout", IsSingle = true, IsBang = true)]
        ISpread<bool> FLogout;

        [Input("Consumer Key", Visibility = PinVisibility.Hidden, IsSingle = true)]
        ISpread<string> FConsumerKey;

        [Input("Consumer Secret", Visibility = PinVisibility.Hidden, IsSingle = true)]
        ISpread<string> FConsumerSecret;

        //OUTPUT
        [Output("Request URL", IsSingle = true)]
        ISpread<string> FRequestUrl;

        [Output("Twitter Id", IsSingle = true)]
        ISpread<int> FUserId;

        [Output("Twitter Name", IsSingle = true)]
        ISpread<string> FUserName;

        [Output("Usage Limit", IsSingle = true)]
        ISpread<string> FUseLimit;

        [Output("Access Token", IsSingle = true)]
        ISpread<string> FAccessToken;

        [Output("Access Token Secret", IsSingle = true)]
        ISpread<string> FAccessTokenSecret;

        [Output("Tweets Id")]
        ISpread<string> FTweetsId;

        [Output("Tweets Author")]
        ISpread<string> FTweetsAuthor;

        [Output("Tweets Author Profile Image")]
        ISpread<string> FTweetsAuthorImage;

        [Output("Tweets Text")]
        ISpread<string> FTweetsText;

        [Output("Tweets Media")]
        ISpread<string> FTweetsMedia;

        [Output("On Search Result", IsSingle = true, IsBang=true)]
        ISpread<bool> FOnSearchResult;

        [Output("Require User Auth", IsSingle = true)]
        ISpread<bool> FNeedUserAuth;

        [Output("Is Authed", IsSingle = true)]
        ISpread<bool> FIsAuthed;

        [Output("Is Logged In", IsSingle = true)]
        ISpread<bool> FIsLoggedIn;

        [Output("Status", IsSingle = true)]
        ISpread<string> FStatus;

        [Import()]
        ILogger FLogger;

        Tvvvvitter twit = new Tvvvvitter();

        public void Evaluate(int SpreadMax)
        {
            if (FAuthApp[0])
            {
                AuthAppAsync auth = new AuthAppAsync(twit.AuthApp);
                IAsyncResult result = auth.BeginInvoke(FConsumerKey[0], 
                    FConsumerSecret[0], new AsyncCallback(AppAuthCallBack), null);
            }

            if (FAuthUser[0] || twit.tokenWasInvalid)
            {
                twit.tokenWasInvalid = false;
                GetRequestTokenAsync request = new GetRequestTokenAsync(twit.GetRequestToken);
                if (FAuthMethod[0])
                {
                    IAsyncResult result = request.BeginInvoke("none", new AsyncCallback(GetRequestTokenCallBack), null);
                }
                else
                {
                    IAsyncResult result = request.BeginInvoke(FCallbackUrl[0], new AsyncCallback(GetRequestTokenCallBack), null);
                }
            }

            if (FTokenVerifierEntered[0] && FAuthMethod[0])
            {
                GetAccessTokenAsync access = new GetAccessTokenAsync(twit.GetAccessToken);
                IAsyncResult result = access.BeginInvoke(FTokenVerifier[0],
                    new AsyncCallback(GetAccessTokenCallBack), null);
            }

            if (FCallbackEntered[0] && !FAuthMethod[0])
            {
                GetCallbackAccessTokenAsync access = new GetCallbackAccessTokenAsync(twit.GetCallbackAccessToken);
                IAsyncResult result = access.BeginInvoke(FCallbackToken[0], FCallbackVerifier[0], 
                    new AsyncCallback(GetCallbackAccessTokenCallBack), null);
            }

            if (FVerifyToken[0])
            {
                VerifyCredentialsAsync cred = new VerifyCredentialsAsync(twit.VerifyCredentials);
                IAsyncResult result = cred.BeginInvoke(FAccessTokenInput[0], 
                    FAccessTokenSecretInput[0], new AsyncCallback(VerifyCredentialsCallBack), null);
            }

            if (FSendTweet[0])
            {
                if (FTweetMessage[0].Length > 0)
                {
                    SendTweetAsync tweet = new SendTweetAsync(twit.SendTweet);
                    IAsyncResult result = tweet.BeginInvoke(FTweetMessage[0],
                        new AsyncCallback(SendTweetCallBack), null);
                }
                else
                {
                    FLogger.Log(LogType.Error, "Twitter error: No Text Message Entered!");
                }
            }

            if (FSendImageTweet[0])
            {
                if (FTweetMessage[0].Length > 0 && File.Exists(FTweetImage[0]))
                {
                    SendImageTweetAsync tweet = new SendImageTweetAsync(twit.SendImageTweet);
                    IAsyncResult result = tweet.BeginInvoke(FTweetMessage[0], FTweetImage[0],
                        new AsyncCallback(SendImageTweetCallBack), null);
                }
                else
                {
                    FLogger.Log(LogType.Error, "Twitter error: No Text Message Entered or File path not valid!");
                }
            }

            if (FSearch[0])
            {
                if (!twit.searchInProgress)
                {
                    int count = FSearchCount[0];
                    if (count < 0 || count == 0 || count == null)
                        count = 10;
                    string hashtag = FSearchHashtag[0];
                    if (hashtag == "" || hashtag == null)
                        hashtag = "vvvv";

                    GetTweetsWithTagAsync search = new GetTweetsWithTagAsync(twit.GetTweetsWithTag);
                    IAsyncResult result = search.BeginInvoke(hashtag, count, new AsyncCallback(GetTweetsWithTagCallBack), null);                     
                }
            }

            if (FLogout[0])
            {
                twit.Logout();
            }

            //update all
            if (twit != null)
            {
                FIsAuthed[0] = twit.appAuthed;
                FIsLoggedIn[0] = twit.hasValidToken;
                FUserId[0] = twit.userId;
                FUserName[0] = twit.userName;
                FRequestUrl[0] = twit.requestUrl;
                FNeedUserAuth[0] = twit.requireUserAuth;
                FStatus[0] = twit.statusCode;
                FUseLimit[0] = twit.rateStatus;
                FAccessToken[0] = twit.accessToken;
                FAccessTokenSecret[0] = twit.accessTokenSecret;
                FOnSearchResult[0] = false;
                if (!twit.searchInProgress && twit.gotNewTweets)
                {
                    SortedDictionary<long, TwitterStatus> results = twit.GetSearchResult();
                    int x = 0;
                    FTweetsId.SliceCount = results.Count;
                    FTweetsAuthor.SliceCount = results.Count;
                    FTweetsAuthorImage.SliceCount = results.Count;
                    FTweetsMedia.SliceCount = results.Count;
                    FTweetsText.SliceCount = results.Count;
                    foreach (KeyValuePair<long, TwitterStatus> p in results)
                    {
                        FTweetsId[x] = p.Key.ToString();
                        FTweetsAuthor[x] = p.Value.User.Name;
                        FTweetsAuthorImage[x] = p.Value.User.ProfileImageUrl;
                        FTweetsText[x] = p.Value.TextDecoded;
                        FTweetsMedia[x] = null;
                        for (int m = 0; m < p.Value.Entities.Media.Count; m++)
                        {
                            FTweetsMedia[x] = p.Value.Entities.Media[m].MediaUrl.ToString();
                        }
                        x++;
                    }
                    twit.ClearSearchResults();
                    //FTweetsText[x] = HttpUtility.HtmlDecode(twit.GetTweetArray()[x]);
                    FOnSearchResult[0] = true;
                }

                if (!twit.searchInProgress && FClearSearch[0])
                {
                    FTweetsId.SliceCount = 0;
                    FTweetsAuthor.SliceCount = 0;
                    FTweetsAuthorImage.SliceCount = 0;
                    FTweetsMedia.SliceCount = 0;
                    FTweetsText.SliceCount = 0;
                    twit.ClearSearchResults();
                    twit.lastIdSearched = 0;
                }

                if (twit.tweetSended)
                {
                    FLogger.Log(LogType.Message, "Twitter: Tweet Sended!");
                    twit.tweetSended = false;
                }
            }

        }
        #region callbacks
        private void AppAuthCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            AuthAppAsync caller = (AuthAppAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void VerifyCredentialsCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            VerifyCredentialsAsync caller = (VerifyCredentialsAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetRequestTokenCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetRequestTokenAsync caller = (GetRequestTokenAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetAccessTokenCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetAccessTokenAsync caller = (GetAccessTokenAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetCallbackAccessTokenCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetCallbackAccessTokenAsync caller = (GetCallbackAccessTokenAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void SendTweetCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            SendTweetAsync caller = (SendTweetAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void SendImageTweetCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            SendImageTweetAsync caller = (SendImageTweetAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetTweetsWithTagCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetTweetsWithTagAsync caller = (GetTweetsWithTagAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }
        #endregion
    }


    public class Tvvvvitter
    {
        //Twitter Objects
        private TwitterService service;
        public string requestToken { get; private set; }
        public string requestTokenSecret { get; private set; }
        public string accessToken { get; private set; }
        public string accessTokenSecret { get; private set; }
        public bool requireUserAuth { get; private set; }
        public bool appAuthed { get; private set; }
        public bool hasValidToken { get; private set; }
        public bool tokenWasInvalid { get; set; }
        public string userName { get; private set; }
        public int userId { get; private set; }
        public string requestUrl { get; private set; }
        public string statusCode { get; private set; }
        public string rateStatus { get; private set; }

        public bool searchInProgress { get; private set; }
        public bool gotNewTweets { get; private set; }
        public long lastIdSearched { get; set; }
        private SortedDictionary<long, TwitterStatus> twSearch;

        public bool tweetSended;

        public Tvvvvitter()
        {
            lastIdSearched = 0;
            appAuthed = false;
            hasValidToken = false;
            requireUserAuth = false;
            tokenWasInvalid = false;
            userId = 0;
            userName = "";
            requestUrl = "";
            statusCode = "";
            rateStatus = "";
            requestToken = "";
            requestTokenSecret = "";
            accessToken = ""; 
            accessTokenSecret = "";
            tweetSended = false;

            gotNewTweets = false;
            searchInProgress = false;

            twSearch = new SortedDictionary<long, TwitterStatus>();

        }

        public SortedDictionary<long, TwitterStatus> GetSearchResult()
        {
            gotNewTweets = false;
            return twSearch;
        }

        public bool AuthApp(string consumer, string secret)
        {
            if (consumer.Length > 2 && secret.Length > 2)
            {
                try
                {
                    service = new TwitterService(consumer, secret);
                    appAuthed = true;
                    return true;
                }
                catch (Exception error)
                {
                    statusCode = "Auth app error: " + error.Message;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool VerifyCredentials(string accessToken, string accessSecret)
        {
            if (!appAuthed)
                return false;
            try
            {
                service.AuthenticateWith(accessToken, accessSecret);
            
                VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
                TwitterUser usr = service.VerifyCredentials(opt);
                if (usr != null)
                {
                    userName = usr.Name;
                    userId = (int)usr.Id;
                    hasValidToken = true;
                    return true;
                }
            }
            catch (Exception error)
            {
                statusCode = "Verify credentials error: " + error.Message;
                tokenWasInvalid = true;
            }

            return false;
        }

        public bool GetRequestToken(string callbackUri="none")
        {
            if (!appAuthed)
                return false;
            try
            {
                OAuthRequestToken rt = new OAuthRequestToken();
                if (callbackUri == "none")
                    rt = service.GetRequestToken();
                else
                    rt = service.GetRequestToken(callbackUri);
                requestToken = rt.Token;
                requestTokenSecret = rt.TokenSecret;
            
                Uri uri = service.GetAuthorizationUri(rt);

                if (requestToken != null && uri != null)
                {
                    requestUrl = uri.ToString();
                    requireUserAuth = true;
                    return true;
                }
            }
            catch (Exception error)
            {
                statusCode = "Get request token error: " + error.Message;
            }
            return false;
        }

        public bool GetCallbackAccessToken(string oauth_token, string oauth_verifier)
        {
            if (!requireUserAuth || !appAuthed)
                return false;

            OAuthRequestToken rt = new OAuthRequestToken { Token = oauth_token }; 
            OAuthAccessToken at = new OAuthAccessToken();

            try
            {
                at = service.GetAccessToken(rt, oauth_verifier);
                accessToken = at.Token;
                accessTokenSecret = at.TokenSecret;

                if (accessToken != null)
                {
                    service.AuthenticateWith(accessToken, accessTokenSecret);

                    VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
                    TwitterUser usr = service.VerifyCredentials(opt);

                    if (usr != null)
                    {
                        userName = usr.Name;
                        userId = (int)usr.Id;
                        hasValidToken = true;
                        requireUserAuth = false;
                        requestUrl = "";
                        return true;
                    }
                }
            }
            catch (Exception error)
            {
                statusCode = "Get callback token error: " + error.Message;
                return false;
            }
            return false;
        }

        public bool GetAccessToken(string verifier)
        {
            if (!requireUserAuth || !appAuthed)
                return false;

            OAuthRequestToken rt = new OAuthRequestToken();
            OAuthAccessToken at = new OAuthAccessToken();
            rt.Token = requestToken;
            rt.TokenSecret = requestTokenSecret;
            try
            {
                at = service.GetAccessToken(rt, verifier);
                accessToken = at.Token;
                accessTokenSecret = at.TokenSecret;
            
                if (accessToken != null)
                {
                    service.AuthenticateWith(accessToken, accessTokenSecret);
               
                    VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
                    TwitterUser usr = service.VerifyCredentials(opt);

                    if (usr != null)
                    {
                        userName = usr.Name;
                        userId = (int)usr.Id;
                        hasValidToken = true;
                        requireUserAuth = false;
                        requestUrl = "";
                        return true;
                    }
                }
            }
            catch (Exception error)
            {
                statusCode = "Get access token error: " + error.Message;
                return false;
            }
            return false;
        }

        public bool SendTweet(string message)
        {
            if (!hasValidToken)
                return false;

            SendTweetOptions opt = new SendTweetOptions();
            opt.Status = message;
            try
            {
                TwitterStatus status = service.SendTweet(opt);
                if (status != null)
                {
                    if (service.Response.StatusCode == HttpStatusCode.OK)
                    {
                        statusCode = service.Response.StatusCode.ToString();
                        rateStatus = service.Response.RateLimitStatus.RemainingHits.ToString();
                        tweetSended = true;
                        return true;
                    }

                    if (service.Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        NeedAuthentication();
                        return false;
                    }
                }
            }
            catch (Exception error)
            {
                statusCode = "Send tweet error: " + error.Message;
            }

            return false;
        }

        public bool SendImageTweet(string message, string path)
        {
            if (!hasValidToken)
                return false;

            using (var stream = new FileStream(path, FileMode.Open))
            {
                SendTweetWithMediaOptions tweetOpts = new SendTweetWithMediaOptions();
                tweetOpts.Status = message;
                tweetOpts.Images = new Dictionary<string, Stream> { { "image", stream } };
                try
                {
                    TwitterStatus status = service.SendTweetWithMedia(tweetOpts);
                    if (status != null)
                    {
                        if (service.Response.StatusCode == HttpStatusCode.OK)
                        {
                            statusCode = service.Response.StatusCode.ToString();
                            rateStatus = service.Response.RateLimitStatus.RemainingHits.ToString();
                            tweetSended = true;
                            return true;
                        }

                        if(service.Response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            NeedAuthentication();
                            return false;
                        }
                    }
                }
                catch (Exception error)
                {
                    statusCode = "Send image tweet error: " + error.Message;
                }
            }
            return false;
        }

        public bool GetTweetsWithTag(string hashtag, int count)
        {
            if (!hasValidToken)
                return false;

            SearchOptions options = new SearchOptions();
            options.Q = hashtag;
            options.Resulttype = TwitterSearchResultType.Recent;
            options.Count = count;
            searchInProgress = true;
            options.SinceId = lastIdSearched;

            try
            {
                var searchResult = service.Search(options);
                if (searchResult != null)
                {
                    foreach (var tweet in searchResult.Statuses)
                    {
                        twSearch.Add(tweet.Id, tweet);
                    }

                    if (twSearch.Count > 0)
                        lastIdSearched = twSearch.Keys.LastOrDefault();

                    gotNewTweets = true;
                    searchInProgress = false;
                    return true;
                }
                else
                {
                    searchInProgress = false;
                    return false;
                }
            }
            catch (Exception error)
            {
                statusCode = "Getting tweets with tag error: " + error.Message;
            }

            return false;
        }

        public void ClearSearchResults()
        {
            if(!searchInProgress)
                twSearch.Clear();
        }
        
        public void Logout()
        {
            NeedAuthentication();
        }
        
        private void NeedAuthentication()
        {
            lastIdSearched = 0;
            hasValidToken = false;
            requireUserAuth = false;
            tokenWasInvalid = false;
            userId = 0;
            userName = "";
            requestUrl = "";
            statusCode = "";
            rateStatus = "";
            requestToken = "";
            requestTokenSecret = "";
            accessToken = "";
            accessTokenSecret = "";
            tweetSended = false;

            gotNewTweets = false;
            searchInProgress = false;
            ClearSearchResults();
        }
    }

    public delegate bool AuthAppAsync(string consumer, string secret);
    public delegate bool VerifyCredentialsAsync(string accessToken, string accessSecret);
    public delegate bool GetRequestTokenAsync(string callbackUri = "none");
    public delegate bool GetAccessTokenAsync(string verifier);
    public delegate bool SendTweetAsync(string message);
    public delegate bool SendImageTweetAsync(string message, string path);
    public delegate bool GetCallbackAccessTokenAsync(string oauth_token, string oauth_verifier);
    public delegate bool GetTweetsWithTagAsync(string hashtag, int count);
}
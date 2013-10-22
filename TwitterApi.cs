using System;
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
    public class TwitterApi : IPluginEvaluate
    {

        //INPUT
        [Input("Auth User", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthUser;

        [Input("Token Verifier", IsSingle = true)]
        ISpread<string> FTokenVerifier;

        [Input("Token Verifier Entered", IsSingle=true, IsBang=true)]
        ISpread<bool> FTokenVerifierEntered;

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

        [Output("Status", IsSingle = true)]
        ISpread<string> FStatus;

        [Import()]
        ILogger FLogger;

        //Twitter Objects
        TwitterService service;
        OAuthRequestToken requestToken;
        OAuthAccessToken accessToken;
        TwitterRateLimitStatus rate;

        private bool appAuthed = false;
        private bool appAuthFailed = false;
        private bool waitingForPin = false;
        private bool userAuthed = false;

        public void Evaluate(int SpreadMax)
        {
            if (!appAuthed && !appAuthFailed)
            {
                appAuthed = AuthApp(FConsumerKey[0], FConsumerSecret[0]);
                if (appAuthed)
                {
                    FStatus[0] = "Twitter: App Auth Success!";
                }
                else
                {
                    appAuthFailed = true;
                    FStatus[0] = "Twitter Error: Trying to auth App, maybe Consumer or Consumer secret are wrong?!";
                }
            }

            if (FAuthUser[0] && appAuthed)
            {
                service.GetRequestToken(GetRequestTokenAsyncResult); //NOT ASYNC!
            }

            if (waitingForPin && FTokenVerifierEntered[0])
            {
                service.GetAccessToken(requestToken, FTokenVerifier[0], GetAccessTokenAsyncResult); //NOT ASYNC!
            }

            if (appAuthed && FVerifyToken[0])
            {
                VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
                service.AuthenticateWith(FAccessTokenInput[0], FAccessTokenSecretInput[0]);
                IAsyncResult result = service.VerifyCredentials(opt, VerifyCredentialsAsyncResult);
            }

            if (FSendTweet[0] && userAuthed)
            {
                SendTweetOptions tweetOpts = new SendTweetOptions();
                tweetOpts.Status = FTweetMessage[0];
                IAsyncResult result = service.SendTweet(tweetOpts, SendTweetAsyncResult);
            }

            if (FSendImageTweet[0] && userAuthed)
            {
                if (File.Exists(FTweetImage[0]))
                {
                    using (var stream = new FileStream(FTweetImage[0], FileMode.Open))
                    {
                        SendTweetWithMediaOptions tweetOpts = new SendTweetWithMediaOptions();
                        tweetOpts.Status = FTweetMessage[0];
                        tweetOpts.Images = new Dictionary<string, Stream> { { "image", stream } };
                        service.SendTweetWithMedia(tweetOpts);
                    }
                }
                else
                {
                    FStatus[0] = "Twitter Error: Image file does not exist!";
                }
            }

            if (userAuthed && rate != null)
            {
                FUseLimit[0] = rate.RemainingHits.ToString() + " / " + rate.HourlyLimit.ToString() + " reset in: " + rate.ResetTime.ToString();
            }

            if (FLogout[0] && userAuthed)
            {
                userAuthed = false;
                waitingForPin = false;
                appAuthed = false;
            }
        }

        private bool AuthApp(string consumer, string secret)
        {
            if (consumer.Length > 2 && secret.Length > 2)
            {
                service = new TwitterService(consumer, secret);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void GetRequestTokenAsyncResult(OAuthRequestToken token, TwitterResponse action)
        {
            if (action.StatusCode == HttpStatusCode.OK)
            {
                requestToken = token;
                Uri uri = service.GetAuthorizationUri(requestToken);
                FRequestUrl[0] = uri.ToString();
                waitingForPin = true;
                rate = action.RateLimitStatus;
            }
            else
            {
                FStatus[0] = "Twitter Error: Getting request token!";
            }
        }

        private void GetAccessTokenAsyncResult(OAuthAccessToken token, TwitterResponse action)
        {
            if (action.StatusCode == HttpStatusCode.OK)
            {
                accessToken = token;
                service.AuthenticateWith(accessToken.Token, accessToken.TokenSecret);
                FUserId[0] = accessToken.UserId;
                FUserName[0] = accessToken.ScreenName;
                FStatus[0] = "Twitter: Auth Success!";
                waitingForPin = false;
                userAuthed = true;
                rate = action.RateLimitStatus;
            }
            else
            {
                FStatus[0] = "Twitter Error: Getting access token!";
            }
        }

        private void VerifyCredentialsAsyncResult(TwitterUser user, TwitterResponse action)
        {
            TwitterError error = action.Error;
            if (error != null)
            {
                FStatus[0] = error.Message;
            }

            if (action.StatusCode == HttpStatusCode.OK)
            {
                FStatus[0] = "Twitter: Current Token is Valid and Verified! - " + action.StatusDescription;
                FUserId[0] = (int)user.Id;
                FUserName[0] = user.Name;
                userAuthed = true;
                rate = action.RateLimitStatus;
            }
            else if (action.StatusCode == HttpStatusCode.Unauthorized)
            {
                FStatus[0] = "Twitter Error: Current Token is not valid! - " + action.StatusDescription;
            }
            else
            {
                //some other error
                FStatus[0] = "Twitter Error: General error verifying token! - " + action.StatusDescription;
            }
        }

        private void SendTweetAsyncResult(TwitterStatus status, TwitterResponse action)
        {
            TwitterError error = action.Error;
            if (error != null)
            {
                FStatus[0] = error.Message;
            }

            if (action.StatusCode == HttpStatusCode.OK)
            {
                FStatus[0] = "Twitter: Normal Tweet sended!";
                rate = action.RateLimitStatus;
            }
            else if (action.StatusCode == HttpStatusCode.Unauthorized)
            {
                
            }
        }

        private void SendTweetWithMediaAsyncResult(TwitterStatus status, TwitterResponse action)
        {
            TwitterError error = action.Error;
            if (error != null)
            {
                FStatus[0] = error.Message;
            }

            if (action.StatusCode == HttpStatusCode.OK)
            {
                FStatus[0] = "Twitter: Media Tweet sended!";
                rate = action.RateLimitStatus;
            }
            else if (action.StatusCode == HttpStatusCode.Unauthorized)
            {
                FStatus[0] = "Twitter Error: No valid access token!";
            }
        }
    }
}
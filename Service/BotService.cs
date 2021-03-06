﻿using isRock.LineBot;
using OpenLineBot.Models.Bot;
using OpenLineBot.Models.System;
using System;
using System.Collections.Generic;

namespace OpenLineBot.Service
{
    public class BotService : BotBase
    {
        isRock.LineBot.Bot _RockBot;
        Event _Event = null; 

        public BotService (string token, string admin, Event evt)
        {
            AdminId = admin;
            ChannelAccessToken = token;
            _RockBot = new Bot(ChannelAccessToken);
            _Event = evt;
        }

        public LineUserInfo UserInfo
        {
            get
            {
                if (_Event.source.type.ToLower() != "user")
                {
                    throw new Exception(new Error(ErrCode.S001).Message);
                }
                else
                {
                    return Utility.GetUserInfo(_Event.source.userId, ChannelAccessToken);
                }
            }
        }

        public Event LineEvent
        {
            get {
                return _Event;
            }
        }

        public void Notify(Exception ex, string userId = null)
        {
            if (!string.IsNullOrEmpty(userId))
                Utility.PushMessage(userId, ex.Message, ChannelAccessToken);

            Utility.PushMessage(AdminId, ex.Message, ChannelAccessToken);
        }

        public void PushMessage(ImageCarouselTemplate response)
        {
            _RockBot.PushMessage(UserInfo.userId, response);
        }


        public void PushMessage(ButtonsTemplate response) {
            _RockBot.PushMessage(UserInfo.userId, response);
        }

        public void PushMessage(ConfirmTemplate response)
        {
            _RockBot.PushMessage(UserInfo.userId, response);
        }

        public void PushMessage(TemplateMessage response)
        {
            _RockBot.PushMessage(UserInfo.userId, response);
        }
        public void PushMessage(string response)
        {
            _RockBot.PushMessage(UserInfo.userId, response);
        }

        public void ReplyMessage(string ReplyToken, string Message){
            _RockBot.ReplyMessage(ReplyToken, Message);
        }

        public void ReplyMessage(string ReplyToken, List<MessageBase> Messages){
            _RockBot.ReplyMessage(ReplyToken, Messages);
        }
    }

}
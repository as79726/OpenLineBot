using isRock.LineBot;
using System;
using System.Collections.Generic;

namespace OpenLineBot.Models.Conversation.Entity
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class TextQuestion : Attribute, IQuestion
    {
        string _Question = "";

        public TextQuestion(string question)
        {
            _Question = question;
        }

        public BotPushType PushType {
            get
            {
                return BotPushType.Text;
            }
        }

        public string TextResponse
        {
            get
            {
                return _Question;
            }
        }

        public string Question {
            get {
                return _Question;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class TextPickerQuestion : Attribute, IQuestion
    {
        string _Question = "";
        string[] _Labels = null;
        ButtonsTemplate _ButtonTemplateResponse = null;

        public TextPickerQuestion(string question, string[] labels)
        {
            _Question = question;
            _Labels = labels;

            var actions = new List<TemplateActionBase>();
            for(var i = 0; i < _Labels.Length; i++) { 
                actions.Add(new PostbackAction()
                { label = _Labels[i], data = _Labels[i] });
            }
            var buttonTemplate = new ButtonsTemplate()
            {
                altText = "替代文字(在無法顯示Button Template的時候顯示)",
                text = _Question,
                actions = actions
            };

            _ButtonTemplateResponse = buttonTemplate;

        }

        public BotPushType PushType
        {
            get
            {
                return BotPushType.TextPicker;
            }
        }

        public ButtonsTemplate ButtonTemplateResponse {
            get
            {
                return _ButtonTemplateResponse;
            }
        }

        public string Question
        {
            get
            {
                return _Question;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DateTimePickerQuestion : Attribute, IQuestion
    {
        string _Question = "";
        ButtonsTemplate _ButtonTemplateResponse = null;

        public DateTimePickerQuestion(string question)
        {
            _Question = question;

            var actions = new List<TemplateActionBase>();
            actions.Add(new DateTimePickerAction() {
                label = "選取日期和時間", mode = "datetime"
            });

            var buttonTemplate = new ButtonsTemplate()
            {
                altText = "替代文字(在無法顯示Button Template的時候顯示)",
                text = _Question,
                actions = actions
            };

            _ButtonTemplateResponse = buttonTemplate;

        }

        public BotPushType PushType
        {
            get
            {
                return BotPushType.DataTimePicker;
            }
        }

        public ButtonsTemplate ButtonTemplateResponse
        {
            get
            {
                return _ButtonTemplateResponse;
            }
        }

        public string Question
        {
            get
            {
                return _Question;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ConfirmQuestion : Attribute, IQuestion
    {
        string _Question = "";
        ConfirmTemplate _ConfirmTemplateResponse = null;

        public ConfirmQuestion(string question)
        {
            _Question = question;

            var actions = new List<TemplateActionBase>();
            actions.Add(new PostbackAction()
            {label = "是", data = "Y"});
            actions.Add(new PostbackAction()
            { label = "否", data = "N" });

            var confirmTemplate = new ConfirmTemplate()
            {
                altText = "替代文字(在無法顯示Button Template的時候顯示)",
                text = _Question,
                actions = actions
            };

            _ConfirmTemplateResponse = confirmTemplate;

        }

        public BotPushType PushType
        {
            get
            {
                return BotPushType.Confirm;
            }
        }

        public ConfirmTemplate ConfirmTemplateResponse
        {
            get
            {
                return _ConfirmTemplateResponse;
            }
        }

        public string Question
        {
            get
            {
                return _Question;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DateTemplateQuestionAttribute : Attribute, IQuestion
    {
     
        public ImageCarouselTemplate imageCarouselTemplate { get; }
        string _Question = "";

        public DateTemplateQuestionAttribute(string altText, string imageUrl, string data = "date", string mode = "date", string label = "選日期")
        {
            _Question = altText;
            if (!imageUrl.StartsWith("https"))
            {
                throw new Exception("imageUrl必須以https開頭...");
            }

            DateTimePickerAction dateTimePickerAction = new DateTimePickerAction() { data = data, label = label, mode = mode };
            List<ImageCarouselColumn> columns = new List<ImageCarouselColumn>();
            columns.Add(new ImageCarouselColumn() { action = dateTimePickerAction, imageUrl = new Uri(imageUrl) });

            imageCarouselTemplate = new ImageCarouselTemplate()
            {
                columns = columns,
                altText = altText
            };
        }

        public BotPushType PushType
        {
            get
            {
                return BotPushType.ImageCarousel;
            }
        }

        public string Question
        {
            get
            {
                return _Question;
            }
        }
    }

}
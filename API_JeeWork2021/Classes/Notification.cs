﻿using DPSinfra.Notifier;
using JeeWork_Core2021.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_JeeWork2021.Classes
{
    public class Notification
    {
        public INotifier _notifier;
        public Notification(INotifier notifier)
        {
            _notifier = notifier;
        }

        public void notification(string sender, string receivers, string message, string html, NotificationMess noti_mess, IConfiguration _configuration)
        {
            string jeework_be = _configuration.GetValue<string>("Host:JeeWork_BE");
            string appcode_jw = _configuration.GetValue<string>("AppConfig:AppCode");
            //jeework_be + notify_model.To_Link_MobileApp  jeework_be + notify_model.To_Link_WebApp;
            socketMessage asyncnotice = new socketMessage()
            {
                sender = sender,
                receivers = new string[] { receivers },
                message_text = message,
                message_html = html,
                message_json = JsonConvert.SerializeObject(noti_mess),
                osTitle = "Thông báo từ JeeWork",
                osMessage = noti_mess.Content,
                osWebURL = jeework_be + noti_mess.Link,
                osAppURL = jeework_be + noti_mess.oslink,
                osIcon = noti_mess.Icon
            };
            _notifier.sendSocket(asyncnotice);
        }
        public async void notificationemail(string access_token)
        {
            emailMessage asyncnotice = new emailMessage()
            {
                access_token = access_token,
                from = "derhades1998@gmail.com",
                to = "derhades1998@gmail.com",
                subject = "Mail test",
                html = "<h1>Hello World</h1>" //nội dung html
            };
            await _notifier.sendEmail(asyncnotice);
        }
        //  public static bool Send(string mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString)
    }
}

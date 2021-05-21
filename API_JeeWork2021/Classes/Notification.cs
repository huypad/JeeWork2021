using DPSinfra.Notifier;
using JeeWork_Core2021.Models;
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

        public void notification(string sender, string receivers, string message, string html, NotificationMess noti_mess)
        {
            socketMessage asyncnotice = new socketMessage()
            {
                sender = sender,
                receivers = new string[] { receivers },
                message_text = message,
                message_html = html,
                message_json = JsonConvert.SerializeObject(noti_mess)
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
    }
}

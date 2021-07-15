using JeeAccount.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace JeeAccount.Services.CommentService
{
    public interface ICommentService
    {
        Task<string> GetTopicObjectIDAsync(string componentName,string Connectionstring);

        Task<HttpResponseMessage> CreateTopicObjectIDAsync(string username, string access_token);

        void SaveTopicID(string componenName, string topicID,string Connectionstring);

        Attach UpdateAllFileToMinio(Attach attachs, string username);

        Task PostCommentKafkaAsync(PostCommentModel postComment);

        Task PostReactionCommentKafkaAsync(ReactionCommentModel reactionCommentModel);
    }
}
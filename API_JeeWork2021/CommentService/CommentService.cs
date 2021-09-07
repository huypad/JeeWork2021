using DPSinfra.Kafka;
using DPSinfra.UploadFile;
using DpsLibs.Data;
using JeeAccount.Controllers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JeeAccount.Services.CommentService
{
    public class CommentService : ICommentService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly IProducer _producer;
        private const string COMMENT_TABLE_NAME = "CommentList";
        private const string COMMENT_TABLE_FIELD_COMPONENTNAME = "ComponentName";
        private const string COMMENT_TABLE_FIELD_OBJECTID = "ObjectID";

        private readonly string HOST_JEECOMMENT_API;
        private readonly string HOST_MINIOSERVER;
        private readonly string TOPIC_JeeplatformPostcomment;

        public CommentService(IConfiguration configuration, IProducer producer)
        {
            _config = configuration;
            _producer = producer;
            _connectionString = configuration.GetValue<string>("AppConfig:Connection");
            HOST_JEECOMMENT_API = configuration.GetValue<string>("Host:JeeComment_API");
            HOST_MINIOSERVER = configuration.GetValue<string>("MinioConfig:MinioServer");
            TOPIC_JeeplatformPostcomment = configuration.GetValue<string>("KafkaConfig:TopicProduce:JeeplatformPostcomment");
        }

        public async Task<string> GetTopicObjectIDAsync(string componentName, string _connectionString)
        {
            DataTable dt = new DataTable();
            string sql = $"select {COMMENT_TABLE_FIELD_OBJECTID} from {COMMENT_TABLE_NAME} where {COMMENT_TABLE_FIELD_COMPONENTNAME}='{componentName}'";
            using (DpsConnection cnn = new DpsConnection(_connectionString))
            {
                dt = await cnn.CreateDataTableAsync(sql);
                if (dt.Rows.Count == 0) return "";
                return dt.Rows[0][$"{COMMENT_TABLE_FIELD_OBJECTID}"].ToString();
            }
        }

        public async Task<HttpResponseMessage> CreateTopicObjectIDAsync(string username, string access_token)
        {
            string url = HOST_JEECOMMENT_API + "/api/comments/addnew";
            var content = new { Username = username };
            var stringContent = await Task.Run(() => JsonConvert.SerializeObject(content));
            var httpContent = new StringContent(stringContent, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(access_token);
                var reponse = await client.PostAsync(url, httpContent);
                return reponse;
            }
        }

        public void UpdateTopicID(DpsConnection cnn,string componenName, string topicID)
        {
            Hashtable val = new Hashtable();
            SqlConditions conditions = new SqlConditions();
            try
            {
                val.Add($"{COMMENT_TABLE_FIELD_OBJECTID}", topicID);
                conditions.Add($"{COMMENT_TABLE_FIELD_COMPONENTNAME}", componenName);

                int x = cnn.Update(val, conditions, $"{COMMENT_TABLE_NAME}");
                if (x <= 0)
                {
                    throw cnn.LastError;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SaveTopicID(string componenName, string topicID, string _connectionString)
        {
            Hashtable val = new Hashtable();
            try
            {
                val.Add($"{COMMENT_TABLE_FIELD_COMPONENTNAME}", componenName);
                val.Add($"{COMMENT_TABLE_FIELD_OBJECTID}", topicID);

                using (DpsConnection cnn = new DpsConnection(_connectionString))
                {
                    string sql = $"select {COMMENT_TABLE_FIELD_COMPONENTNAME} from {COMMENT_TABLE_NAME} where {COMMENT_TABLE_FIELD_COMPONENTNAME}='{componenName}'";
                    var dt = cnn.CreateDataTable(sql);
                    if (dt.Rows.Count > 0)
                    {
                        UpdateTopicID(cnn,componenName, topicID);
                        return;
                    }

                    int x = cnn.Insert(val, $"{COMMENT_TABLE_NAME}");
                    if (x <= 0)
                    {
                        throw cnn.LastError;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Attach UpdateAllFileToMinio(Attach attachs, string username)
        {
            var filesUrl = new Attach();
            if (attachs.Images != null)
            {
                filesUrl.Images = new List<string>();
                foreach (var base64 in attachs.Images)
                {
                    string filename = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
                    upLoadFileModel up = new upLoadFileModel()
                    {
                        bs = Convert.FromBase64String(base64),
                        FileName = $"{filename}.png",
                        Linkfile = $"comments/{username}"
                    };
                    var upload = UploadFile.UploadFileImageMinio(up, _config);
                    if (upload.status) filesUrl.Images.Add($"https://{HOST_MINIOSERVER}{upload.link}");
                }
            }
            filesUrl.Files = attachs.Files;
            filesUrl.Videos = attachs.Videos;
            return filesUrl;
        }

        public async Task PostCommentKafkaAsync(PostCommentModel postComment)
        {
            var kafkaModel = new KafkaCommentModel();

            kafkaModel.IsComment = true;
            kafkaModel.IsAddNew = true;
            kafkaModel.PostComment = postComment;

            await _producer.PublishAsync(TOPIC_JeeplatformPostcomment, JsonConvert.SerializeObject(kafkaModel)).ConfigureAwait(false);
        }

        public async Task PostReactionCommentKafkaAsync(ReactionCommentModel reactionCommentModel)
        {
            var kafkaModel = new KafkaCommentModel();

            kafkaModel.IsReaction = true;
            kafkaModel.ReactionComment = reactionCommentModel;

            await _producer.PublishAsync(TOPIC_JeeplatformPostcomment, JsonConvert.SerializeObject(kafkaModel)).ConfigureAwait(false);
        }
    }
}
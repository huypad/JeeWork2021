﻿using DPSinfra.ConnectionCache;
using DPSinfra.Notifier;
using DPSinfra.UploadFile;
using JeeAccount.Classes;
using JeeAccount.Services.CommentService;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Controllers.Wework;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JeeAccount.Controllers
{
    [ApiController]
    [EnableCors("JeeWorkPolicy")]
    [Route("api/comments")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private JeeWorkConfig _config;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<CommentController> logger, IOptions<JeeWorkConfig> config)
        {
            _config = config.Value;
            _notifier = notifier;
            ConnectionCache = _cache;
            _configuration = configuration;
            this._commentService = commentService;
            _logger = logger;
        }

        [HttpGet("getByComponentName/{componentName}")]
        public async Task<IActionResult> GetByComponentName(string componentName)
        {
            try
            {
                UserJWT customData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
                var access_token = Ulities.GetAccessTokenByHeader(HttpContext.Request.Headers);
                if (customData is null)
                {
                    return Unauthorized(NotFound(MessageReturnHelper.CustomDataKhongTonTai()));
                }
                var username = Ulities.GetUsernameByHeader(HttpContext.Request.Headers);
                string Connectionstring = JeeWorkLiteController.getConnectionString(ConnectionCache, customData.CustomerID, _configuration);
                var topicObjectID = await _commentService.GetTopicObjectIDAsync(componentName, Connectionstring);
                if (!string.IsNullOrEmpty(topicObjectID))
                {
                    return Ok(topicObjectID);
                }
                else
                {
                    var responseMessage = await _commentService.CreateTopicObjectIDAsync(username, access_token);
                    if (!responseMessage.IsSuccessStatusCode) return BadRequest(MessageReturnHelper.Custom($"Error {responseMessage.ReasonPhrase}"));
                    var newTopic = Newtonsoft.Json.JsonConvert.DeserializeObject<ConvertTopic>(responseMessage.Content.ReadAsStringAsync().Result);
                    _commentService.SaveTopicID(componentName, newTopic.Id, Connectionstring);
                    return Ok(newTopic.Id);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(MessageReturnHelper.Exception(ex));
            }
        }    
        
        [HttpGet("getByComponentName_Mobile/{componentName}")]
        public async Task<object> GetByComponentName_Mobile(string componentName)
        {
            UserJWT customData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            var access_token = Ulities.GetAccessTokenByHeader(HttpContext.Request.Headers);
            if (customData is null)
            {
                return JsonResultCommon.DangNhap();
            }
            try
            { 
                var username = Ulities.GetUsernameByHeader(HttpContext.Request.Headers);
                string Connectionstring = JeeWorkLiteController.getConnectionString(ConnectionCache, customData.CustomerID, _configuration);
                var topicObjectID = await _commentService.GetTopicObjectIDAsync(componentName, Connectionstring);
                if (!string.IsNullOrEmpty(topicObjectID))
                {
                    // return Ok(topicObjectID);
                    return JsonResultCommon.ThanhCong(new { id = topicObjectID });
                }
                else
                {
                    var responseMessage = await _commentService.CreateTopicObjectIDAsync(username, access_token);
                    if (!responseMessage.IsSuccessStatusCode) return JsonResultCommon.Custom($"Error {responseMessage.ReasonPhrase}");// BadRequest(MessageReturnHelper.Custom($"Error {responseMessage.ReasonPhrase}"));
                    var newTopic = Newtonsoft.Json.JsonConvert.DeserializeObject<ConvertTopic>(responseMessage.Content.ReadAsStringAsync().Result);
                    _commentService.SaveTopicID(componentName, newTopic.Id, Connectionstring);
                    // return Ok(newTopic.Id);
                    return JsonResultCommon.ThanhCong(new { id = newTopic.Id });
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, customData);
            }
        }

        [HttpPost("postComment")]
        public async Task<IActionResult> PostComment([FromBody] PostCommentModel postComment)
        {
            try
            {
                var customData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
                if (customData is null)
                {
                    return Unauthorized(NotFound(MessageReturnHelper.CustomDataKhongTonTai()));
                }
                var username = Ulities.GetUsernameByHeader(HttpContext.Request.Headers);
                postComment.Username = username;
                if (postComment.Attachs != null)
                {
                    postComment.Attachs = _commentService.UpdateAllFileToMinio(postComment.Attachs, username);
                }
                await _commentService.PostCommentKafkaAsync(postComment);
                return StatusCode(201, postComment);
            }
            catch (Exception ex)
            {
                return BadRequest(MessageReturnHelper.Exception(ex));
            }
        }

        [HttpPost("postReactionComment")]
        public async Task<IActionResult> PostReactionComment([FromBody] ReactionCommentModel reactionCommentModel)
        {
            try
            {
                var customData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
                if (customData is null)
                {
                    return Unauthorized(NotFound(MessageReturnHelper.CustomDataKhongTonTai()));
                }
                reactionCommentModel.Username = Ulities.GetUsernameByHeader(HttpContext.Request.Headers);

                await _commentService.PostReactionCommentKafkaAsync(reactionCommentModel);
                return StatusCode(200, reactionCommentModel);
            }
            catch (Exception ex)
            {
                return BadRequest(MessageReturnHelper.Exception(ex));
            }
        }
    }

    public class KafkaCommentModel
    {
        public bool IsAddNew { get; set; } = false;
        public bool IsUpdate { get; set; } = false;
        public bool IsDelete { get; set; } = false;
        public bool IsComment { get; set; } = false;
        public bool IsReaction { get; set; } = false;
        public PostCommentModel PostComment { get; set; } = null;
        public ReactionCommentModel ReactionComment { get; set; } = null;
    }

    public class ConvertTopic
    {
        public string Id { get; set; }
    }

    public class PostCommentModel
    {
        public string TopicCommentID { get; set; }
        public string CommentID { get; set; }
        public string ReplyCommentID { get; set; }
        public string Text { get; set; }
        public string Username { get; set; }
        public Attach Attachs { get; set; }
    }

    public class ReactionCommentModel
    {
        public string TopicCommentID { get; set; }
        public string CommentID { get; set; }
        public string ReplyCommentID { get; set; }
        public string Username { get; set; }
        public string UserOldReaction { get; set; }
        public string UserReaction { get; set; }
    }

    public class Attach
    {
        public List<string> Images { get; set; }
        public List<string> Files { get; set; }
        public List<string> Videos { get; set; }
    }
}
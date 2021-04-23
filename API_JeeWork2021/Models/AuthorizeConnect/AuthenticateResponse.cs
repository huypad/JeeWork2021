using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Models.AuthorizeConnect
{
    public class AuthenticateResponse
    {
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("userName")]
        [JsonProperty("userName")]
        public string Username { get; set; }

        [JsonPropertyName("token")]
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonPropertyName("when")]
        [JsonProperty("when")]
        public long? When { get; set; }

        [Newtonsoft.Json.JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public AuthenticateResponse(UserJWT user, string jwtToken)
        {
            Id = user.UserID;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
            RefreshToken = string.Empty;
            Token = jwtToken;
            When = DateTime.Now.Ticks;
        }

        public AuthenticateResponse(UserJWT user, string jwtToken, string refreshToken)
        {
            Id = user.UserID;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
            RefreshToken = refreshToken;
            Token = jwtToken;
            When = DateTime.Now.Ticks;
        }

    }
}

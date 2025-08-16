using IdeaCenterExamPrep.Models;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;


namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;
        private string? lastCreatedStoryId;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/api ";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhNzkwZWE1OS05ZGNjLTQ2YzgtOWZlZC0zN2Y2MGQ0YjM4NjQiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjE3OjQ4IiwiVXNlcklkIjoiOTBmYzNkZDktNTYzMC00NjYwLThkZTItMDhkZGRiMWExM2YzIiwiRW1haWwiOiJJdm9uQEl2b24uY29tIiwiVXNlck5hbWUiOiJJdm9uMSIsImV4cCI6MTc1NTM0NjY2OCwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.SDknRkS9Yky7WWRhkUGDVCD2Zx4_lxGOFncp_Kf-7_M";

        private const string LoginUsername = "Ivon1";
        private const string LoginPassword = "Ivon123";

        public object StoryApiTests { get; private set; }

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginUsername, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if(string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
       
        [Order(1)]
        [Test]
        public void CreateStory_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new StoryDTO
            {
                Title = "Test Story",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Id, Is.Not.Null.Or.Empty); // Fix: Use 'Id' instead of 'StoryId'

            // Use the `lastCreatedStoryId` field directly instead of `StoryApiTests.LastCreatedStoryId`.
            lastCreatedStoryId = createResponse.Id; // Fix: Assign 'Id' instead of invoking 'StoryId'
        }

        [Order(2)]
        [Test]
        public void GetAllStory_ShouldReturnListOfStory()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = this.client.Execute(request);

            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);

            lastCreatedIdeaId = responsItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]

        public void EditExistingStory_ShouldReturnSuccess()
        {
            var editRequest = new StoryDTO
            {
                Title = "Edited Story",
                Description = "This is an updated test story description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit",Method.Put);
            request.AddQueryParameter("storyId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteStory_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Story/Delete", Method.Delete);
            request.AddQueryParameter("storyId", lastCreatedStoryId);
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The story is deleted!"));
        }

        [Order(5)]
        [Test]

        public void CreateStory_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "123";
            var editRequest = new StoryDTO
            {
                Title = "Edited Non-Existing Story",
                Description = "This is an updated test story description for a non-existing story.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit", Method.Put);
            request.AddQueryParameter("storyId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such story!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingStory_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "123";
            var request = new RestRequest($"/api/Story/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistingStoryId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such story!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
        this.client?.Dispose();
        }


    }
}
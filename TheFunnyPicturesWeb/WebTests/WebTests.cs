using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Json;
using Xunit;

namespace WebTests
{
    public class WebTestForFunnyPictures : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _applicationFactory;

        public WebTestForFunnyPictures(WebApplicationFactory<Program> applicationFactory) { 
            _applicationFactory = applicationFactory;
        }

        [Fact]
        public async Task TestEmptyImageString()
        {
            var client = _applicationFactory.CreateClient();
            string emptyImageString = "";
            var res = await client.PostAsJsonAsync("api/WebFunnyPictures", emptyImageString);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task TestNullImageString()
        {
            var client = _applicationFactory.CreateClient();
            string? nullImageString = null;
            var res = await client.PostAsJsonAsync("api/WebFunnyPictures", nullImageString);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
        }

    }
}
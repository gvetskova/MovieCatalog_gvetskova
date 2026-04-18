using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using MovieCatalog.DTOs;

namespace MovieCatalog
{
    public class MovieCatalogTests
    {
        private RestClient client;
        private static string? movieId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("greta1@example.com", "gV12345");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:5000")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:5000");
            RestRequest request = new RestRequest("/api/user/authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldSuccess()
        {
            MovieDTO movie = new MovieDTO
            { 
                Id = "",
                Title = "Scary movie",
                Description = "A very scary movie."
            };

            RestRequest request = new RestRequest("/api/movie/create", Method.Post);
            request.AddJsonBody(movie);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);//превръщаме response.Content в обект от тип ApiResponseDTO, който съдържа полетата Msg и FoodId
            
            movie = readyResponse.Movie;

            Assert.That(movie.Id, Is.Not.Null.Or.Empty);
            Assert.That(readyResponse.Msg, Is.EqualTo("Movie created successfully!"));

            movieId = readyResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditCreatedMovie_ShouldSucceed()
        {
            RestRequest request = new RestRequest($"/api/movie/edit/{movieId}", Method.Put);
            request.AddBody(new[]
            {
                new
                {
                    path = "/title",
                    op = "replace",
                    value = "Scary movie edited"
                }
            });
            
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(readyResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllFood_ShouldSucceed()
        {
            RestRequest request = new RestRequest("/api/catalog/all", Method.Get);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            List<MovieDTO> readyResponse = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);
           
            Assert.That(readyResponse, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void DeleteCreatedMovie_ShouldSucceed()
        {
            RestRequest request = new RestRequest($"/api/movie/delete/{movieId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(readyResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            MovieDTO movie = new MovieDTO
            {
                Title = "",
                Description = ""
            };

            RestRequest request = new RestRequest("/api/movie/create", Method.Post);
            request.AddBody(movie);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "11111";
            RestRequest request = new RestRequest($"/api/movie/edit/{nonExistingMovieId}", Method.Put);
            request.AddBody(new[] 
            {
                new
                {
                    path = "/title",
                    op = "replace",
                    value = "Non-existing movie edited"
                }
            });

            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
           
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "12222";
            RestRequest request = new RestRequest($"/api/movie/delete/{nonExistingMovieId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
           
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
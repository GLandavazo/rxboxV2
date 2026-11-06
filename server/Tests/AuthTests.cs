using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using RxBox.Core.DTOs.Responses.Auth;
using RxBox.Core.Entities;

namespace RxBox.Tests
{
    [Collection(ApiCollection.Nombre)]
    public class AuthTests(RxBoxApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Login_con_un_box_regresa_tokens_con_los_claims_del_box()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);

            var login = await LoginOkAsync(usuario.Email.ToUpperInvariant());

            Assert.False(login.RequiereSeleccion);
            Assert.NotNull(login.RefreshToken);
            var claims = new JsonWebToken(login.AccessToken);
            Assert.Equal(usuario.Id.ToString(), claims.Subject);
            Assert.Equal(box.Id.ToString(), claims.GetClaim("box_id").Value);
            Assert.Equal("ADMIN", claims.GetClaim("rol").Value);
        }

        [Fact]
        public async Task Login_con_password_incorrecto_regresa_401()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync());

            var response = await LoginAsync(usuario.Email, "incorrecto");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_con_email_inexistente_regresa_401()
        {
            var response = await LoginAsync("nadie@rxbox.test", RxBoxApiFactory.Password);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_sin_box_activo_regresa_403()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync(activo: false));

            var response = await LoginAsync(usuario.Email, RxBoxApiFactory.Password);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Login_ignora_boxes_inactivos_al_decidir_si_pide_seleccion()
        {
            var activo = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(activo, await factory.CrearBoxAsync(activo: false));

            var login = await LoginOkAsync(usuario.Email);

            Assert.False(login.RequiereSeleccion);
            Assert.Equal(activo.Id.ToString(), new JsonWebToken(login.AccessToken).GetClaim("box_id").Value);
        }

        [Fact]
        public async Task Login_con_dos_boxes_pide_seleccion_y_seleccionar_box_emite_el_box_correcto()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(boxA, boxB);

            var login = await LoginOkAsync(usuario.Email);

            Assert.True(login.RequiereSeleccion);
            Assert.Null(login.AccessToken);
            Assert.Null(login.RefreshToken);
            Assert.Equal([boxA.PublicId, boxB.PublicId], login.Boxes!.Select(b => b.PublicId).Order());
            Assert.False(new JsonWebToken(login.SelectionToken).TryGetClaim("box_id", out _));

            var tokens = await PostOkAsync<TokensResponse>("/api/auth/seleccionar-box",
                new { login.SelectionToken, boxPublicId = boxB.PublicId });

            Assert.Equal(boxB.Id.ToString(), new JsonWebToken(tokens.AccessToken).GetClaim("box_id").Value);
        }

        [Fact]
        public async Task SeleccionarBox_de_un_box_ajeno_regresa_403()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync(), await factory.CrearBoxAsync());
            var ajeno = await factory.CrearBoxAsync();
            var login = await LoginOkAsync(usuario.Email);

            var response = await _client.PostAsJsonAsync("/api/auth/seleccionar-box",
                new { login.SelectionToken, boxPublicId = ajeno.PublicId });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SeleccionarBox_con_un_access_token_regresa_401()
        {
            var box = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(box);
            var login = await LoginOkAsync(usuario.Email);

            var response = await _client.PostAsJsonAsync("/api/auth/seleccionar-box",
                new { selectionToken = login.AccessToken, boxPublicId = box.PublicId });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Selection_token_no_sirve_como_bearer()
        {
            var boxA = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(boxA, await factory.CrearBoxAsync());
            var login = await LoginOkAsync(usuario.Email);

            var response = await PostConBearerAsync("/api/auth/cambiar-box", login.SelectionToken!, new { boxPublicId = boxA.PublicId });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CambiarBox_emite_tokens_del_otro_box()
        {
            var boxA = await factory.CrearBoxAsync();
            var boxB = await factory.CrearBoxAsync();
            var usuario = await factory.CrearUsuarioAsync(boxA, boxB);
            var tokensA = await EntrarAlBoxAsync(usuario, boxA);

            var response = await PostConBearerAsync("/api/auth/cambiar-box", tokensA.AccessToken, new { boxPublicId = boxB.PublicId });

            response.EnsureSuccessStatusCode();
            var tokensB = (await response.Content.ReadFromJsonAsync<TokensResponse>())!;
            Assert.Equal(boxB.Id.ToString(), new JsonWebToken(tokensB.AccessToken).GetClaim("box_id").Value);
        }

        [Fact]
        public async Task CambiarBox_sin_token_regresa_401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/cambiar-box", new { boxPublicId = Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_rota_y_reusar_el_anterior_revoca_la_sesion()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync());
            var login = await LoginOkAsync(usuario.Email);

            var rotado = await PostOkAsync<TokensResponse>("/api/auth/refresh", new { login.RefreshToken });
            Assert.NotEqual(login.RefreshToken, rotado.RefreshToken);

            var reuso = await _client.PostAsJsonAsync("/api/auth/refresh", new { login.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);

            var trasReuso = await _client.PostAsJsonAsync("/api/auth/refresh", new { rotado.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, trasReuso.StatusCode);
        }

        [Fact]
        public async Task Logout_revoca_el_refresh_token()
        {
            var usuario = await factory.CrearUsuarioAsync(await factory.CrearBoxAsync());
            var login = await LoginOkAsync(usuario.Email);

            var logout = await PostConBearerAsync("/api/auth/logout", login.AccessToken!, new { login.RefreshToken });
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

            var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { login.RefreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        [Fact]
        public async Task Logout_no_revoca_tokens_de_otro_usuario()
        {
            var box = await factory.CrearBoxAsync();
            var victima = await LoginOkAsync((await factory.CrearUsuarioAsync(box)).Email);
            var otro = await LoginOkAsync((await factory.CrearUsuarioAsync(box)).Email);

            await PostConBearerAsync("/api/auth/logout", otro.AccessToken!, new { victima.RefreshToken });

            var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { victima.RefreshToken });
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        }

        private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
            _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        private async Task<LoginResponse> LoginOkAsync(string email)
        {
            var response = await LoginAsync(email, RxBoxApiFactory.Password);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        }

        private async Task<TokensResponse> EntrarAlBoxAsync(Usuario usuario, Box box)
        {
            var login = await LoginOkAsync(usuario.Email);
            return await PostOkAsync<TokensResponse>("/api/auth/seleccionar-box",
                new { login.SelectionToken, boxPublicId = box.PublicId });
        }

        private async Task<T> PostOkAsync<T>(string url, object body)
        {
            var response = await _client.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private Task<HttpResponseMessage> PostConBearerAsync(string url, string token, object body) =>
            _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
            });
    }
}

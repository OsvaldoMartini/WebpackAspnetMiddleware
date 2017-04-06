using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Redouble.AspNet.Webpack.Test
{
    public class HotReloadTests
    {
        private const string DEFAULT_RESPONSE = "X-X default response X-X";

        private TestServer CreateServer(IWebpackService webpackService)
        {
            var mockServiceDescriptor = new ServiceDescriptor(typeof(IWebpackService), webpackService);

            var builder = new WebHostBuilder()
               .Configure(app =>
                       {
                           app.UseWebpackHotReload();
                           app.Run(async context =>
                           {
                               await context.Response.WriteAsync(DEFAULT_RESPONSE);
                           });
                       })
               .ConfigureServices(services =>
                       {
                           services.AddLogging();
                           services.Add(mockServiceDescriptor);
                       });
            return new TestServer(builder);
        }

        /*
                [Fact]        
                public async Task DevServer_SetsChunkedEncoding()
                {
                   // Arrange
                   var mock = new WebpackServiceMock();

                    using (var server = CreateServer(mock))
                    {      
                       using (var client = server.CreateClient())
                       {
                        // Assert
                        response.EnsureSuccessStatusCode();
                        Assert.Equal(2, response.Headers.Count());
                        Assert.True(response.Headers.CacheControl.NoTransform);
                        Assert.True(response.Headers.CacheControl.NoCache);
                        Assert.True(response.Headers.Connection.Contains("keep-alive"));
                        Assert.Null(response.Headers.TransferEncodingChunked);
                        Assert.Null(response.Headers.ConnectionClose);                
                        Assert.Null(response.Content.Headers.ContentLength);
                       }         
                    }
                }
         */

        [Fact]
        public async Task DevServer_EmitsHeartbeats()
        {
            // Arrange
            var mock = new WebpackServiceMock();

            using (var server = CreateServer(mock))
            {
                using (var client = server.CreateClient())
                {
                    // Act
                    var stream = await client.GetStreamAsync("/__webpack_hmr");

                    var buffer = new byte[256];
                    var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(14, byteCount1);

                    var cancellationTokenSource = new CancellationTokenSource(10000);
                    var byteCount2 = await stream.ReadAsync(buffer, 0, 256, cancellationTokenSource.Token);

                    Assert.Equal(14, byteCount2);
                    Assert.Equal("data: \uD83D\uDC93\r\n\r\n", System.Text.Encoding.UTF8.GetString(buffer).Substring(0, byteCount2 - 2));
                }
            }
        }

        [Fact]
        public async Task DevServer_EmitsOnValid()
        {
            // Arrange
            var mock = new WebpackServiceMock();
            mock.AddFile("/public/bundle.js", "bundle.js", "js");

            using (var server = CreateServer(mock))
            {
                using (var client = server.CreateClient())
                {
                    // Act
                    var stream = await client.GetStreamAsync("/__webpack_hmr");

                    var buffer = new byte[256];
                    var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(14, byteCount1);

                    var e = new JObject();
                    mock.OnValid(e);

                    // TODO
                    var byteCount2 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(28, byteCount2);
                    Assert.Equal("data: {\"action\":\"built\"}\r\n\r\n", System.Text.Encoding.UTF8.GetString(buffer).Substring(0, byteCount2));
                }
            }
        }
    }
}

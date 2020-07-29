using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncServer.Operations
{
    internal class Async
    {

        private static readonly HttpClient _client = new HttpClient();
        internal static void TestApi( Socket target )
        {
            var apicall = ApiCallTest().Result;
            byte[] data = Encoding.ASCII.GetBytes(apicall);
            target.Send( data );
        }


        private static async Task<string> ApiCallTest()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue( "application/vnd.github.v3+json" ) );
            _client.DefaultRequestHeaders.Add( "User", "HildaShaRe" );

            var stringTask = _client.GetStringAsync("https://randomuser.me/api/");

            var msg = await stringTask;
            return msg;
        }
    }
}

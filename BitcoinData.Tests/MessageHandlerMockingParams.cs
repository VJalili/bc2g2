using System.Net;
using System.Net.Http;

namespace BitcoinData.Tests
{
    internal class MessageHandlerMockingParams
    {
        public string Endpoint { get; }

        // Implementation details: 
        // it is essential to return an new instance
        // everytime this is accessed, becase otherwise
        // the any second reader will continue reading 
        // from the point where the first reader left;
        // if left at the end, the second reader will 
        // read empty string.
        public StringContent Response
        {
            get { return new StringContent(_response); }
        }

        public HttpStatusCode Status { get; }

        private readonly string _response;

        public MessageHandlerMockingParams(
            string endpoint, string response,
            HttpStatusCode status = HttpStatusCode.OK)
        {
            Endpoint = endpoint;
            Status = status;
            _response = response;
        }
    }
}

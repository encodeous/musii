using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Encodeous.Musii.Data
{
    /// <summary>
    /// Represents a music source over the network
    /// </summary>
    public interface IMusicSource
    {
        public Task Refresh(HttpClient client);
        public Task<string> GetStreamUrl(HttpClient client);
        public string GetImageLink();
        public TimeSpan GetDuration();
        public string GetTitle();
        public bool IsStream();
        public IMusicSource CloneSource();
    }
}
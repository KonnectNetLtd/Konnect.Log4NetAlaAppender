using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json;

namespace Konnect.Log4NetAzureLogAnalytics
{
    public class KnlAlaAppender : BufferingAppenderSkeleton
    {
        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogType { get; set; }
        public string ApiVersion {get; set;} = "2016-04-01";
        public bool EnableDebugLog { get; set; } = false;
        
        [Obsolete]
        public bool EnableConsoleLog { get; set; } = false;
        

        private void DebugLog(string message)
        {
            if (EnableConsoleLog || EnableDebugLog)
            {
                Console.WriteLine(message);
                Debug.WriteLine(message);
            }
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            DebugLog("Invoking SendBufferAsync");
            Task.Run(() => SendBufferAsync(events));
        }

        private void SendBufferAsync(LoggingEvent[] events)
        {
            DebugLog("Entering SendBufferAsync");
            try
            {
                var eventsToSend = events.Select(e => new
                {
                    Level = e.Level.Name, 
                    e.LoggerName, 
                    Message = e.RenderedMessage, 
                    e.TimeStamp, 
                    e.Identity,
                    e.ThreadName, 
                    e.UserName,
                    Exception = e.GetExceptionString()
                });
                
                var json = JsonConvert.SerializeObject(eventsToSend);

                var requestUriString =
                    $"https://{WorkspaceId}.ods.opinsights.azure.com/api/logs?api-version={ApiVersion}";
                var dateTime = DateTime.UtcNow;
                var dateString = dateTime.ToString("r");

                String signature;
                var message = $"POST\n{json.Length}\napplication/json\nx-ms-date:{dateString}\n/api/logs";
                var bytes = Encoding.UTF8.GetBytes(message);
                using (var encryptor = new HMACSHA256(Convert.FromBase64String(SharedKey)))
                {
                    signature = $"SharedKey {WorkspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
                }

                var request = (HttpWebRequest) WebRequest.Create(requestUriString);
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Headers["Log-Type"] = LogType;
                request.Headers["x-ms-date"] = dateString;
                request.Headers["Authorization"] = signature;
                var content = Encoding.UTF8.GetBytes(json);
                using (var requestStreamAsync = request.GetRequestStream())
                {
                    requestStreamAsync.Write(content, 0, content.Length);
                }

                using (var responseAsync = (HttpWebResponse) request.GetResponse())
                {
                    if (responseAsync.StatusCode != HttpStatusCode.OK &&
                        responseAsync.StatusCode != HttpStatusCode.Accepted)
                    {
                        var responseStream = responseAsync.GetResponseStream();
                        if (responseStream != null)
                        {
                            using (var streamReader = new StreamReader(responseStream))
                            {
                                throw new Exception(streamReader.ReadToEnd());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog("Exception occurred (rethrowing)");
                DebugLog(ex.ToString());
                throw;
            }
            finally
            {
                DebugLog("Exiting SendBuffer");
            }
        }
    }
}
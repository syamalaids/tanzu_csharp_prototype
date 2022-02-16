using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Demo
{
    public static class HttpCaller
    {
        public static void Start(int iterCount)
        {
            using (Activity myActivity = Program.MyActivitySource.StartActivity("Http calls", ActivityKind.Client))
            {
                for (int i = 0; i < iterCount; ++i)
                {
                    string activityName;
                    string url = GetRequestUrl(i, out activityName);

                    using (Activity childActivity = Program.MyActivitySource.StartActivity(activityName, ActivityKind.Client))
                    {
                        childActivity.SetTag("URI", url);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.KeepAlive = false;
                        request.Timeout = 5000;
                        request.Proxy = null;

                        request.ServicePoint.ConnectionLeaseTimeout = 5000;
                        request.ServicePoint.MaxIdleTime = 5000;

                        try
                        {
                            using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                            {
                                Console.WriteLine($"Iteration:{i + 1} Uri:{url} Status:{webResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            childActivity.SetTag("otel.status_code", "ERROR");
                            childActivity.SetTag("otel.status_description", ex.StackTrace);

                            // Increment the counter by 1
                            Program.WfMetrics.Measure.Counter.Increment(Program.MyExceptionCounter);
                        }


                        request.ServicePoint.CloseConnectionGroup(request.ConnectionGroupName);
                        request = null;
                    }
                }
            }
        }

        private static string GetRequestUrl(int i, out string activityName)
        {
            activityName = "Microsoft call";

            string url = "https://www.microsoft.com";
            if (i % 3 == 1)
            {
                url = "https://www.google.com";
                activityName = "Google call";
            }
            if (i % 3 == 2)
            {
                url = "https://devmnotexist.cyracomdev.com";
                activityName = "Dev call";
            }

            return url;
        }
    }
}

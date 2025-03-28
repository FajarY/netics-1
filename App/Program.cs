using dotenv.net;
using System.Text.Json;

namespace NeticsCICD
{
    public static class Program
    {
        private static IDictionary<string, string> env = new Dictionary<string, string>();
        private static DateTime startTime;

        public static void Main(string[] args)
        {
            startTime = DateTime.Now;

            DotEnv.Load(new DotEnvOptions(
                envFilePaths: new string[]
                {
                    ".env"
                }
            ));
            env = DotEnv.Read();

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:8080");
            var app = builder.Build();

            app.MapGet("/health", Health);

            app.Run();
        }
        public static JsonDocument Health()
        {
            Dictionary<string, object> response = new Dictionary<string, object>();
            response.Add("nama", env["NAMA"]);
            response.Add("nrp", env["NRP"]);
            response.Add("status", "UP");
            response.Add("timestamp", DateTime.Now.ToString());;
            response.Add("uptime", (DateTime.Now - startTime).ToString());

            return JsonSerializer.SerializeToDocument(response);
        }
    }
}
using Microsoft.Extensions.Configuration;

namespace GDS.MultiPageFormData
{
    public static class AppConfig
    {
        public static bool GetMultiPageFormDataStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: false)
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), optional: false)
                .Build();
            try
            {
                return configuration.GetSection("MultiPageFormService").GetSection("Database").Value.ToLower().Equals("redis");
            }
            catch { return false; }
        }
    }
}
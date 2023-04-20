using Microsoft.Extensions.Configuration;

namespace GDS.MultiPageFormData
{
    public static class AppConfig
    {
        public static bool GetMultiPageFormDataStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), optional: true)
                .Build();
            try
            {
                return configuration.GetSection("MultiPageFormService").GetSection("Database").Value.ToLower().Equals("redis");
            }
            catch { return false; }
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Localization;
using OrchardCore.Modules;
using OrchardCore.Setup.Options;

namespace OrchardCore.Setup
{
    public class Startup : StartupBase
    {
        private readonly string _defaultCulture = CultureInfo.InstalledUICulture.Name;
        private string[] _supportedCultures = new string[] {
            "ar", "cs", "da", "de", "el", "en", "es", "fa", "fi", "fr", "he", "hr", "hu", "id", "it", "ja", "ko", "lt", "mk", "nl", "pl", "pt", "ru", "sk", "sl", "sr-cyrl-rs", "sr-latn-rs", "sv", "tr", "uk", "vi", "zh-CN", "zh-TW"
        };
        private readonly IShellConfiguration _shellConfiguration;
        private readonly IConfiguration _configuration;

        public Startup(IShellConfiguration shellConfiguration, IConfiguration configuration)
        {
            _shellConfiguration =shellConfiguration;
            var configurationSection = shellConfiguration.GetSection("OrchardCore.Setup");
            _configuration = configuration;
            _defaultCulture = configurationSection["DefaultCulture"] ?? _defaultCulture;
            _supportedCultures = configurationSection.GetSection("SupportedCultures").Get<string[]>() ?? _supportedCultures;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddPortableObjectLocalization(options => options.ResourcesPath = "Localization");
            services.Replace(ServiceDescriptor.Singleton<ILocalizationFileLocationProvider, ModularPoFileLocationProvider>());
            services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, AutoSetupStartupFilter>();
            var configuration = _shellConfiguration.GetSection("OrchardCore.Setup.AutoSetup");
            services.Configure<AutoSetupOptions>(configuration);
            services.AddSetup();
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var localizationOptions = serviceProvider.GetService<IOptions<RequestLocalizationOptions>>().Value;

            if (!String.IsNullOrEmpty(_defaultCulture))
            {
                localizationOptions.SetDefaultCulture(_defaultCulture);
                _supportedCultures = _supportedCultures.Union(new[] { _defaultCulture }).ToArray();
            }

            if (_supportedCultures?.Length > 0)
            {
                localizationOptions
                    .AddSupportedCultures(_supportedCultures)
                    .AddSupportedUICultures(_supportedCultures);
            }

            app.UseRequestLocalization(localizationOptions);

            routes.MapAreaControllerRoute(
                name: "Setup",
                areaName: "OrchardCore.Setup",
                pattern: "",
                defaults: new { controller = "Setup", action = "Index" }
            );
        }
    }
}

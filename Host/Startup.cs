using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;

namespace Host
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.Development.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            if (Uri.TryCreate(Configuration["AzureKeyVault:Url"], UriKind.Absolute, out _))
            {
                if (string.IsNullOrEmpty(Configuration["AzureKeyVault:ClientId"]))
                {
                    builder.AddAzureKeyVault(Configuration["AzureKeyVault:Url"]);
                }
                else
                {
                    builder
                        .AddAzureKeyVault(
                            Configuration["AzureKeyVault:Url"],
                            Configuration["AzureKeyVault:ClientId"],
                            Configuration["AzureKeyVault:ClientSecret"]);
                }
            }

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var storageAccount = CloudStorageAccount.Parse(Configuration["DataProtectionKeys:connectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(Configuration["DataProtectionKeys:container"]);

            // This fails if the blob "azuredpconf-keys.xml" does not already exist
            // Workaround is to create xml file manually or deploy first time without
            // using ProtectKeysWithAzureKeyVault and then add it in next deploy.
            services
                .AddDataProtection(options =>
                {
                    options.ApplicationDiscriminator = Configuration["DataProtectionKeys:applicationDiscriminator"];
                })
                .PersistKeysToAzureBlobStorage(blobContainer, "azuredpconf-keys.xml")
                .ProtectKeysWithAzureKeyVault(
                    Configuration["AzureKeyVault:DataProtectionKey"],
                    Configuration["AzureKeyVault:ClientId"],
                    Configuration["AzureKeyVault:ClientSecret"]);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Blazor.DevHost.Server
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseDeveloperExceptionPage();
            EnableConfiguredPathbase(app, configuration);

            var clientAssemblyPath = FindClientAssemblyPath(app);
            app.UseBlazor(new BlazorOptions { ClientAssemblyPath = clientAssemblyPath });
        }

        private static void EnableConfiguredPathbase(IApplicationBuilder app, IConfiguration configuration)
        {
            var pathBase = configuration.GetValue<string>("pathbase");
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);

                // To ensure consistency with a production environment, only handle requests
                // that match the specified pathbase.
                app.Use((context, next) =>
                {
                    if (context.Request.PathBase == pathBase)
                    {
                        return next();
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        return context.Response.WriteAsync($"The server is configured only to " +
                            $"handle request URIs within the PathBase '{pathBase}'.");
                    }
                });
            }
        }

        private static string FindClientAssemblyPath(IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var contentRoot = env.ContentRootPath;
            var binDir = FindClientBinDir(contentRoot);
            var appName = Path.GetFileName(contentRoot); // TODO: Allow for the possibility that the assembly name has been overridden
            var assemblyPath = Path.Combine(binDir, $"{appName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Could not locate application assembly at expected location {assemblyPath}");
            }

            return assemblyPath;
        }

        private static string FindClientBinDir(string clientAppSourceRoot)
        {
            var binDebugDir = Path.Combine(clientAppSourceRoot, "bin", "Debug");
            var subdirectories = Directory.GetDirectories(binDebugDir);
            if (subdirectories.Length != 1)
            {
                throw new InvalidOperationException($"Could not locate bin directory for Blazor app. " +
                    $"Expected to find exactly 1 subdirectory in '{binDebugDir}', but found {subdirectories.Length}.");
            }

            return Path.Combine(binDebugDir, subdirectories[0]);
        }
    }
}

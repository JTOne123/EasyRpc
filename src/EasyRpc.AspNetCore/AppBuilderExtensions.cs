﻿using System;
using System.Threading.Tasks;
using EasyRpc.AspNetCore.Documentation;
using EasyRpc.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace EasyRpc.AspNetCore
{
    public static class AppBuilderExtensions
    {
        /// <summary>
        /// Add Easy RPC dependency injection configuration, this is usually only needed if you want to override
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="configuration"></param>
        public static IServiceCollection AddJsonRpc(this IServiceCollection collection, Action<RpcServiceConfiguration> configuration = null )
        {
            collection.TryAddTransient<IJsonRpcMessageProcessor, JsonRpcMessageProcessor>();
            collection.TryAddSingleton<IJsonSerializerProvider, JsonSerializerProvider>();
            collection.TryAddSingleton<INamedParameterToArrayDelegateProvider,NamedParameterToArrayDelegateProvider>();
            collection.TryAddSingleton<IOrderedParameterToArrayDelegateProvider, OrderedParameterToArrayDelegateProvider>();
            collection.TryAddSingleton<IArrayMethodInvokerBuilder, ArrayMethodInvokerBuilder>();
            collection.TryAddSingleton<IInstanceActivator, InstanceActivator>();

            collection.TryAddSingleton<IRpcHeaderContext, RpcHeaderContext>();
            collection.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            collection.TryAddSingleton(new JsonSerializer());

            collection.TryAddSingleton<IXmlDocumentationProvider, XmlDocumentationProvider>();
            collection.TryAddTransient<IDocumentationRequestProcessor,DocumentationRequestProcessor>();
            collection.TryAddTransient<IWebAssetProvider, WebAssetProvider>();
            collection.TryAddTransient<IMethodPackageMetadataCreator, MethodPackageMetadataCreator>();
            collection.TryAddTransient<IVariableReplacementService, VariableReplacementService>();
            collection.TryAddTransient<IReplacementValueProvider, ReplacementValueProvider>();
            collection.TryAddTransient<ITypeDefinitionPackageProvider, TypeDefinitionPackageProvider>();

            collection.Configure(configuration ?? (option => { }));

            return collection;
        }

        /// <summary>
        /// Adds JSON-RPC 2.0 support
        /// </summary>
        /// <param name="appBuilder">app builder</param>
        /// <param name="basePath">base path for api</param>
        /// <param name="configure">configure api</param>
        public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder appBuilder, string basePath,
            Action<IApiConfiguration> configure)
        {
            JsonRpcMiddleware.AttachMiddleware(appBuilder, basePath, configure);
            
            return appBuilder;
        }

        /// <summary>
        /// Redirects all requests to service api for documentation
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public static IApplicationBuilder RedirectToDocumentation(this IApplicationBuilder appBuilder, string basePath)
        {
            appBuilder.Use((context, next) =>
            {
                var response = context.Response;
                var redirectPath = context.Request.PathBase.Value + basePath;

                if (context.Request.Path.Value?.EndsWith( "/favicon.ico") ?? false)
                {
                    response.Headers[HeaderNames.Location] = redirectPath + "favicon.ico";
                }
                else
                {
                    response.Headers[HeaderNames.Location] = redirectPath;
                }

                response.StatusCode = 301;

                return Task.CompletedTask;
            });

            return appBuilder;
        }
    }
}

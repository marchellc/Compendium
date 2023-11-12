using Compendium.Logging;

using Grapevine;

using helpers;
using helpers.Attributes;
using helpers.Random;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compendium.HttpServer
{
    public static class HttpController
    {
        internal static volatile IRestServer _server;

        private static Thread _thread;

        private static CancellationTokenSource _cts;
        private static CancellationToken _ct;

        [Load]
        public static void Load()
        {
            try
            {
                if (Plugin.Config.ApiSetttings.HttpSettings.ServerPrefix == "none")
                    return;

                var services = new ServiceCollection();

                services.AddSingleton(typeof(IConfiguration), DefaultConfig);

                services.AddSingleton<IRestServer, RestServer>();
                services.AddSingleton<IRouter, Router>();
                services.AddSingleton<IRouteScanner, RouteScanner>();

                services.AddTransient<IContentFolder, ContentFolder>();

                services.AddLogging(b => b.AddProvider(new LoggingProvider()));

                services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Warning);

                var provider = services.BuildServiceProvider();
                var server = provider.GetService<IRestServer>();

                server.Router.Services = services;
                server.RouteScanner.Services = services;

                var assembly = typeof(HttpController).Assembly.GetName();

                server.GlobalResponseHeaders.Add("Server", $"{assembly.Name}/{assembly.Version} ({RuntimeInformation.OSDescription})");
                server.Prefixes.Add(Plugin.Config.ApiSetttings.HttpSettings.ServerPrefix);

                services.AddSingleton(server);
                services.AddSingleton(server.Router);
                services.AddSingleton(server.RouteScanner);

                server.SetDefaultLogger(new LoggingFactory());

                _cts = new CancellationTokenSource();
                _ct = _cts.Token;

                _server = server;

                _thread = new Thread(async () => 
                {
                    server.Start();

                    while (!_ct.IsCancellationRequested)
                        await Task.Delay(150);
                });

                _thread.Priority = ThreadPriority.AboveNormal;
                _thread.Start();
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }

        public static string AddRoute(Func<IHttpContext, Task> routeHandler, HttpMethod method, string pattern)
        {
            var id = RandomGeneration.Default.GetReadableString(60);
            var route = new Route(routeHandler, method, pattern, true, id, id);

            _server.Router.Register(route);

            return id;
        }

        public static void AddRoutes<T>()
            => AddRoutes(typeof(T));

        public static void AddRoutes(Type type)
        {
            if (_server is null)
                Calls.OnFalse(() =>
                {
                    var routes = _server.RouteScanner.Scan(type);

                    if (routes != null && routes.Any())
                        _server.Router.Register(routes);
                }, () => _server is null);
            else
            {
                var routes = _server.RouteScanner.Scan(type);

                if (routes != null && routes.Any())
                    _server.Router.Register(routes);
            }
        }

        public static void RemoveRoute(string id)
        {
            if (_server.Router.RoutingTable.TryGetFirst(x => x.Name == id, out var route))
            {
                route.Disable();
                _server.Router.RoutingTable.Remove(route);
            }
        }

        public static void RemoveRoutes<T>()
            => RemoveRoutes(typeof(T));

        public static void RemoveRoutes(Type type)
        {
            if (_server is null)
                return;

            var routes = _server.RouteScanner.Scan(type);

            if (routes is null || !routes.Any())
                return;

            routes.ForEach(x =>
            {
                if (!_server.Router.RoutingTable.TryGetFirst(y => y.Equals(x), out var route))
                    return;

                route.Disable();

                _server.Router.RoutingTable.Remove(route);
            });
        }

        [Unload]
        public static void Stop()
        {
            _cts.Cancel();

            _server.Stop();
            _server = null;
        }

        private static IConfiguration DefaultConfig { get; } = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build();
    }
}
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Vjezba.Model.Tests.Infrastructure
{
    public sealed class PlaywrightWebAppFixture : IAsyncLifetime
    {
        private readonly StringBuilder _output = new();
        private Process? _process;
        private string? _databasePath;

        public string BaseUrl { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            var port = GetFreeTcpPort();
            BaseUrl = $"http://127.0.0.1:{port}";
            _databasePath = Path.Combine(Path.GetTempPath(), $"vjezba-playwright-{Guid.NewGuid():N}.db");

            var projectPath = GetWebProjectPath();
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException("Cannot resolve web project directory.");
            var configuration = GetBuildConfiguration();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --configuration {configuration} --no-build --no-launch-profile",
                WorkingDirectory = projectDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
            startInfo.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={_databasePath}";
            startInfo.Environment["Authentication__Google__ClientId"] = string.Empty;
            startInfo.Environment["Authentication__Google__ClientSecret"] = string.Empty;
            startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

            _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
            _process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);

            if (!_process.Start())
            {
                throw new InvalidOperationException("Could not start the web app process.");
            }

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await WaitForAppAsync();
        }

        public async Task DisposeAsync()
        {
            if (_process is not null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }

                    await _process.WaitForExitAsync();
                }
                catch (InvalidOperationException)
                {
                }
                finally
                {
                    _process.Dispose();
                }
            }

            DeleteDatabaseFiles();
        }

        private async Task WaitForAppAsync()
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(2)
            };

            var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (_process?.HasExited == true)
                {
                    throw new InvalidOperationException(
                        $"The web app exited before it became ready.{Environment.NewLine}{_output}");
                }

                try
                {
                    using var response = await client.GetAsync("/");
                    if ((int)response.StatusCode < 500)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(500);
            }

            throw new TimeoutException(
                $"The web app did not become ready at {BaseUrl}.{Environment.NewLine}{_output}");
        }

        private void DeleteDatabaseFiles()
        {
            if (string.IsNullOrWhiteSpace(_databasePath))
            {
                return;
            }

            foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private void AppendOutput(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lock (_output)
            {
                _output.AppendLine(line);
            }
        }

        private static string GetWebProjectPath()
        {
            var root = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                ".."));
            return Path.Combine(root, "Vjezba.Model", "Vjezba.Model.csproj");
        }

        private static string GetBuildConfiguration()
        {
            var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));
            return baseDirectory.Parent?.Name ?? "Debug";
        }

        private static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}

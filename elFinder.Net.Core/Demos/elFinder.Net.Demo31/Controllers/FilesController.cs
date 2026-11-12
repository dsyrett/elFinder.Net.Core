using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.AspNetCore.Helper;
using elFinder.Net.Core;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace elFinder.Net.Demo31.Controllers {
    [Route("api/files")]
    public class FilesController : Controller {
        private readonly IConnector _connector;
        private readonly IDriver _driver;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FilesController(IConnector connector, IDriver driver, IWebHostEnvironment webHostEnvironment) {
            _connector = connector;
            _driver = driver;
            _webHostEnvironment = webHostEnvironment;

        }

        [Route("connector")]
        public async Task<IActionResult> Connector() {
            await SetupConnectorAsync();
            var cmd = ConnectorHelper.ParseCommand(Request);
            var ccTokenSource = ConnectorHelper.RegisterCcTokenSource(HttpContext);
            var conResult = await _connector.ProcessAsync(cmd, ccTokenSource);
            var actionResult = conResult.ToActionResult(HttpContext);
            return actionResult;
        }

        [Route("thumb/{target}")]
        public async Task<IActionResult> Thumb(string target) {
            await SetupConnectorAsync();
            var thumb = await _connector.GetThumbAsync(target, HttpContext.RequestAborted);
            var actionResult = ConnectorHelper.GetThumbResult(thumb);
            return actionResult;
        }

        private async Task SetupConnectorAsync() {
            var userFilesPath = _webHostEnvironment.WebRootPath + "\\userfiles";

            // Volumes registration
            //for (var i = 0; i < 5; i++) {
            //var volume = new Volume(_driver,
            //    Startup.MapPath($"~/upload/volume-{i}"),
            //    Startup.TempPath,
            //    $"/upload/volume-{i}",
            //    $"/api/files/thumb/",
            //    thumbnailDirectory: PathHelper.GetFullPath("./thumb"))
            //{
            //    StartDirectory = Startup.MapPath($"~/upload/volume-{i}/start"),
            //    Name = $"Volume {i}",
            //    MaxUploadConnections = 3
            //};
            var volume = new Volume(_driver,
               userFilesPath,
               $"{_webHostEnvironment.WebRootPath}\\temp",
               $"/userfiles",
               $"/api/files/thumb/",
               thumbnailDirectory: $"{_webHostEnvironment.WebRootPath}\\thumb"
               ) {
                //StartDirectory = Startup.MapPath($"~/upload/volume-{i}/start"),
                Name = $"Userfiles",
                MaxUploadConnections = 3
            };

            _connector.AddVolume(volume);
            await volume.Driver.SetupVolumeAsync(volume);
            //}

            // Events
            _driver.OnBeforeMove.Add((fileSystem, newDest, isOverwrite) => {
                Console.WriteLine("Move: " + fileSystem.FullName);
                Console.WriteLine("To: " + newDest);
                return Task.CompletedTask;
            });
        }
    }
}

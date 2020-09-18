using ReactiveUI;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Shadowsocks.ViewModels
{
    public class ServerSharingViewModel : ReactiveObject
    {
        /// <summary>
        /// The view model class for the server sharing user control.
        /// </summary>
        public ServerSharingViewModel()
        {
            _config = Configuration.Load();
            _servers = _config.configs;
            _selectedServer = _servers.First();
            //_selectedServerUrlImage = new BitmapImage();
            UpdateUrlAndImage();
        }

        private readonly Configuration _config;

        private List<Server> _servers;
        private Server _selectedServer;
        private string _selectedServerUrl;
        private BitmapImage _selectedServerUrlImage;

        /// <summary>
        /// Called when SelectedServer changed
        /// to update SelectedServerUrl and SelectedServerUrlImage
        /// </summary>
        private void UpdateUrlAndImage()
        {
            // update SelectedServerUrl
            SelectedServerUrl = _selectedServer.GetURL(_config.generateLegacyUrl);

            // generate QR code
            var qrCode = ZXing.QrCode.Internal.Encoder.encode(_selectedServerUrl, ZXing.QrCode.Internal.ErrorCorrectionLevel.L);
            var byteMatrix = qrCode.Matrix;

            // paint bitmap
            int blockSize = Math.Max(1024 / byteMatrix.Height, 1);
            Bitmap drawArea = new Bitmap((byteMatrix.Width * blockSize), (byteMatrix.Height * blockSize));
            using (var graphics = Graphics.FromImage(drawArea))
            {
                graphics.Clear(Color.White);
                using (var solidBrush = new SolidBrush(Color.Black))
                {
                    for (int row = 0; row < byteMatrix.Width; row++)
                    {
                        for (int column = 0; column < byteMatrix.Height; column++)
                        {
                            if (byteMatrix[row, column] != 0)
                            {
                                graphics.FillRectangle(solidBrush, blockSize * row, blockSize * column, blockSize, blockSize);
                            }
                        }
                    }
                }
            }

            // transform to BitmapImage for binding
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                drawArea.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            SelectedServerUrlImage = bitmapImage;
        }

        public List<Server> Servers
        {
            get => _servers;
            set => this.RaiseAndSetIfChanged(ref _servers, value);
        }

        public Server SelectedServer
        {
            get => _selectedServer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedServer, value);
                UpdateUrlAndImage();
            }
        }

        public string SelectedServerUrl
        {
            get => _selectedServerUrl;
            set => this.RaiseAndSetIfChanged(ref _selectedServerUrl, value);
        }

        public BitmapImage SelectedServerUrlImage
        {
            get => _selectedServerUrlImage;
            set => this.RaiseAndSetIfChanged(ref _selectedServerUrlImage, value);
        }
    }
}

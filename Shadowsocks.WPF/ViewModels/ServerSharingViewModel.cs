using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shadowsocks.WPF.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reactive;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Shadowsocks.WPF.ViewModels
{
    public class ServerSharingViewModel : ReactiveObject
    {
        /// <summary>
        /// The view model class for the server sharing user control.
        /// </summary>
        public ServerSharingViewModel(List<Server> servers)
        {
            Servers = servers;
            SelectedServer = Servers[0];

            this.WhenAnyValue(x => x.SelectedServer)
                .Subscribe(_ => UpdateUrlAndImage());

            CopyLink = ReactiveCommand.Create(() => Clipboard.SetText(SelectedServerUrl));
        }

        public ReactiveCommand<Unit, Unit> CopyLink { get; }

        [Reactive]
        public List<Server> Servers { get; private set; }

        [Reactive]
        public Server SelectedServer { get; set; }

        [Reactive]
        public string SelectedServerUrl { get; private set; } = null!;

        [Reactive]
        public BitmapImage SelectedServerUrlImage { get; private set; } = null!;

        /// <summary>
        /// Called when SelectedServer changed
        /// to update SelectedServerUrl and SelectedServerUrlImage
        /// </summary>
        private void UpdateUrlAndImage()
        {
            // update SelectedServerUrl
            SelectedServerUrl = SelectedServer.ToUrl().AbsoluteUri;

            // generate QR code
            var qrCode = ZXing.QrCode.Internal.Encoder.encode(SelectedServerUrl, ZXing.QrCode.Internal.ErrorCorrectionLevel.L);
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
    }
}

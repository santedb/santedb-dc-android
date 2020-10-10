using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Api.Services;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using ZXing;
using ZXing.Mobile;
using ZXing.QrCode;
using A = Android;

namespace SanteDB.DisconnectedClient.Android.Core.Services.Barcoding
{
    /// <summary>
    /// Represents a QR barcode service
    /// </summary>
    /// <summary>
    /// Barcode generator service that generates a QR code
    /// </summary>
    public class QrBarcodeGenerator : IBarcodeProviderService
    {

        /// <summary>
        /// JWS format regex
        /// </summary>
        private readonly Regex m_jwsFormat = new Regex(@"^(.*?)\.(.*?)\.(.*?)$");

        /// <summary>
        /// Get the name of the service
        /// </summary>
        public string ServiceName => "QR Code Barcode Generator";

        /// <summary>
        /// Generate the specified barcode from the information provided
        /// </summary>
        public Stream Generate<TEntity>(IEnumerable<IdentifierBase<TEntity>> identifers) where TEntity : VersionedEntityData<TEntity>, new()
        {
        
            if (!identifers.Any())
                return null; // Cannot generate
            try
            {

                var pointerService = ApplicationServiceContext.Current.GetService<IResourcePointerService>();
                if (pointerService == null)
                    throw new InvalidOperationException("Cannot find resource pointer generator");

                // Generate the pointer
                var identityToken = pointerService.GeneratePointer(identifers);
                // Now generate the token
                var writer = new BarcodeWriter()
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions()
                    {
                        Width = 300,
                        Height = 300,
                        PureBarcode = true,
                        Margin = 1

                    }
                };

                using (var bmp = writer.Write(identityToken.ToString()))
                {
                    var retVal = new MemoryStream();
                    bmp.Compress(A.Graphics.Bitmap.CompressFormat.Png, 30, retVal);
                    retVal.Seek(0, SeekOrigin.Begin);
                    return retVal;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Cannot generate QR code for specified identifier list", e);
            }
        }

    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SanteDB.Core.Api.Services;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using ZXing;
using ZXing.Mobile;
using ZXing.QrCode;
using A = Android;

namespace SanteDB.DisconnectedClient.Android.Core.Services.Barcoding
{
    /// <summary>
    /// Represents a QR barcode service
    /// </summary>
    public class QrBarcodeGenerator : IBarcodeGeneratorService
    {
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
                var sourceKey = identifers.First().SourceEntityKey.Value.ToByteArray();
                var dataSigner = new HMACSHA256(sourceKey);
                // Append the header to the token
                // Append authorities to identifiers
                var header = new
                {
                    alg = "HS256",
                    type = identifers.First().LoadProperty<TEntity>("SourceEntity")?.Type,
                    key = sourceKey.Take(4).ToArray().ToHexString()
                };
                StringBuilder identityToken = new StringBuilder($"{JsonConvert.SerializeObject(header)}.");

                var domainList = new
                {
                    id = identifers.Select(o => new
                    {
                        value = o.Value,
                        domain = new
                        {
                            ns = o.LoadProperty<AssigningAuthority>("Authority").DomainName,
                            oid = o.Authority.Oid
                        }
                    }).ToList()
                };

                identityToken.Append(JsonConvert.SerializeObject(domainList));

                // Sign the data
                var tokenData = Encoding.UTF8.GetBytes(identityToken.ToString());
                var signature = dataSigner.ComputeHash(tokenData);
                identityToken.AppendFormat(".{0}", Convert.ToBase64String(signature));

                // Now generate the token
                var writer = new BarcodeWriter()
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions()
                    {
                        Width = 200,
                        Height = 200,
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
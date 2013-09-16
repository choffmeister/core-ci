/*
 * Copyright (C) 2013 Christian Hoffmeister
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see {http://www.gnu.org/licenses/}.
 */
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Mono.Unix.Native;

namespace CoreCI.Common
{
    public static class RSAExtensions
    {
        public static string ToOpenSshPublicKeyFileString(this RSA rsa, string name)
        {
            RSAParameters publicKey = rsa.ExportParameters(false);
            MemoryStream ms = new MemoryStream();
            OpenSshKeyWriter writer = new OpenSshKeyWriter(ms);

            writer.WriteInt32(7);
            writer.WriteString("ssh-rsa");
            writer.WriteMultiPrecisionInteger(publicKey.Exponent);
            writer.WriteMultiPrecisionInteger(publicKey.Modulus);

            return string.Format("ssh-rsa {0} {1}", ToBase64String(ms), name);
        }

        public static string ToOpenSshPrivateKeyFileString(this RSA rsa)
        {
            RSAParameters privateKey = rsa.ExportParameters(true);

            MemoryStream ms = new MemoryStream();
            OpenSshKeyWriter writer = new OpenSshKeyWriter(ms);

            // version 0 RSA private key
            writer.WriteDerMultiPrecisionInteger(new byte[] { 0 });
            writer.WriteDerMultiPrecisionInteger(privateKey.Modulus);
            writer.WriteDerMultiPrecisionInteger(privateKey.Exponent);
            writer.WriteDerMultiPrecisionInteger(privateKey.D);
            writer.WriteDerMultiPrecisionInteger(privateKey.P);
            writer.WriteDerMultiPrecisionInteger(privateKey.Q);
            writer.WriteDerMultiPrecisionInteger(privateKey.DP);
            writer.WriteDerMultiPrecisionInteger(privateKey.DQ);
            writer.WriteDerMultiPrecisionInteger(privateKey.InverseQ);

            MemoryStream ms2 = new MemoryStream();
            OpenSshKeyWriter writer2 = new OpenSshKeyWriter(ms2);

            writer2.WriteByte(48);
            writer2.WriteDerLength((int)ms.Length);

            ms.Seek(0, SeekOrigin.Begin);
            ms.WriteTo(ms2);

            return string.Format("-----BEGIN RSA PRIVATE KEY-----\n{0}\n-----END RSA PRIVATE KEY-----", ToMultiLineBase64String(ms2));
        }

        private static string ToBase64String(MemoryStream ms)
        {
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string ToMultiLineBase64String(MemoryStream ms)
        {
            string base64 = ToBase64String(ms);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < base64.Length; i += 64)
            {
                sb.Append(base64.Substring(i, Math.Min(base64.Length - i, 64)));

                if (i + 64 < base64.Length)
                    sb.Append("\n");
            }

            return sb.ToString();
        }
    }
}

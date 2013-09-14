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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Renci.SshNet.Common;

namespace CoreCI.Common
{
    public static class RSAExtensions
    {
        public static string ToOpenSshPublicKeyFileString(this RSA rsa, string name)
        {
            RSAParameters publicKey = rsa.ExportParameters(false);
            byte[] mpintExponent = BigIntegerToMpInt(publicKey.Exponent);
            byte[] mpintModulus = BigIntegerToMpInt(publicKey.Modulus);

            List<byte> bytes = new List<byte>();
            bytes.AddRange(Int32ToBigEndian("ssh-rsa".Length));
            bytes.AddRange(Encoding.ASCII.GetBytes("ssh-rsa"));
            bytes.AddRange(Int32ToBigEndian(mpintExponent.Length));
            bytes.AddRange(mpintExponent);
            bytes.AddRange(Int32ToBigEndian(mpintModulus.Length));
            bytes.AddRange(mpintModulus);
            string base64 = Convert.ToBase64String(bytes.ToArray());

            StringBuilder sb = new StringBuilder();
            sb.Append("ssh-rsa");
            sb.Append(" ");
            sb.Append(base64);
            sb.Append(" ");
            sb.Append(name);

            return sb.ToString();
        }

        public static string ToOpenSshPrivateKeyFileString(this RSA rsa)
        {
            RSAParameters privateKey = rsa.ExportParameters(true);

            // adding a zero byte at the end of each BigInteger to ensure, that
            // the number is recognized as positive number
            FixedDerData der = new FixedDerData();
            der.Write(new BigInteger(0));
            der.Write(new BigInteger(privateKey.Modulus.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.Exponent.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.D.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.P.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.Q.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.DP.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.DQ.Reverse().Concat(new byte[] { 0 }).ToArray()));
            der.Write(new BigInteger(privateKey.InverseQ.Reverse().Concat(new byte[] { 0 }).ToArray()));

            byte[] raw = der.Encode();
            string derBase64 = Convert.ToBase64String(raw);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");

            for (int i = 0; i < derBase64.Length; i += 64)
            {
                sb.AppendLine(derBase64.Substring(i, Math.Min(derBase64.Length - i, 64)));
            }

            sb.Append("-----END RSA PRIVATE KEY-----");

            return sb.ToString();
        }

        private static byte[] BigIntegerToMpInt(byte[] bytes)
        {
            // prepend a 0-byte to make the integer positive
            if ((bytes [0] & 128) != 0)
            {
                return new byte[] { 0 }.Concat(bytes).ToArray();
            }

            return bytes;
        }

        private static byte[] Int32ToBigEndian(int i)
        {
            byte a = (byte)((i >> 24) & 255);
            byte b = (byte)((i >> 16) & 255);
            byte c = (byte)((i >> 8) & 255);
            byte d = (byte)((i >> 0) & 255);

            return new byte[] { a, b, c, d };
        }
    }
}

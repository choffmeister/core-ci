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
using System.Text;

namespace CoreCI.Common
{
    internal class OpenSshKeyWriter : IDisposable
    {
        private readonly Stream _stream;

        public OpenSshKeyWriter(Stream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void WriteMultiPrecisionInteger(byte[] i)
        {
            bool isLeadingBitSet = (i [0] & 128) != 0;
            int length = isLeadingBitSet ? i.Length + 1 : i.Length;

            this.WriteInt32(length);

            // prepend a 0-byte to make the integer positive
            if (isLeadingBitSet)
                _stream.WriteByte(0);
            _stream.Write(i, 0, i.Length);
        }

        public void WriteDerMultiPrecisionInteger(byte[] i)
        {
            bool isLeadingBitSet = (i [0] & 128) != 0;
            int length = isLeadingBitSet ? i.Length + 1 : i.Length;

            // 0x02 marks an upcoming integer in DER format
            _stream.WriteByte(2);
            this.WriteDerLength(length);

            // prepend a 0-byte to make the integer positive
            if (isLeadingBitSet)
                _stream.WriteByte(0);
            _stream.Write(i, 0, i.Length);
        }

        public void WriteDerLength(int length)
        {
            if (length > 127)
            {
                int num = 1;
                int num2 = length;
                while ((num2 >>= 8) != 0)
                {
                    num++;
                }

                byte[] array = new byte[num + 1];
                array [0] = (byte)(num | 128);
                int i = (num - 1) * 8;
                int num3 = 1;
                while (i >= 0)
                {
                    array [num3] = (byte)(length >> i);
                    i -= 8;
                    num3++;
                }

                _stream.Write(array, 0, array.Length);
            }
            else
            {
                _stream.WriteByte((byte)length);
            }
        }

        public void WriteInt32(int i)
        {
            byte[] bytes = new byte[4];

            bytes [0] = (byte)((i >> 24) & 255);
            bytes [1] = (byte)((i >> 16) & 255);
            bytes [2] = (byte)((i >> 8) & 255);
            bytes [3] = (byte)((i >> 0) & 255);

            _stream.Write(bytes, 0, 4);
        }

        public void WriteString(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);

            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteByte(byte b)
        {
            _stream.WriteByte(b);
        }
    }
}

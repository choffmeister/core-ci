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
using System.Linq;
using System.Collections.Generic;
using Renci.SshNet.Common;

namespace CoreCI.Common
{
    /// <summary>
    /// A copy of the original <see cref="Renci.SshNet.Common.DerData"/>, but with
    /// fixed <see cref="GetLength"/> method.
    /// </summary>
    internal class FixedDerData
    {
        //
        // Static Fields
        //
        private const byte CONSTRUCTED = 32;
        private const byte SEQUENCE = 16;
        private const byte OBJECTIDENTIFIER = 6;
        private const byte NULL = 5;
        private const byte OCTETSTRING = 4;
        private const byte INTEGER = 2;
        private const byte BOOLEAN = 1;
        //
        // Fields
        //
        private int _lastIndex;
        private int _readerIndex;
        private List<byte> _data;
        //
        // Properties
        //
        public bool IsEndOfData
        {
            get
            {
                return this._readerIndex >= this._lastIndex;
            }
        }
        //
        // Constructors
        //
        public FixedDerData()
        {
            this._data = new List<byte>();
        }

        public FixedDerData(byte[] data)
        {
            this._data = new List<byte>(data);
            this.ReadByte();
            int num = this.ReadLength();
            this._lastIndex = this._readerIndex + num;
        }
        //
        // Methods
        //
        public byte[] Encode()
        {
            int length = this._data.Count<byte>();
            byte[] length2 = this.GetLength(length);
            this._data.InsertRange(0, length2);
            this._data.Insert(0, 48);
            return this._data.ToArray();
        }

        private byte[] GetLength(int length)
        {
            if (length > 127)
            {
                int num = 1;
                int num2 = length;
                while ((num2 >>= 8) != 0)
                {
                    num++;
                }
                // original line: byte[] array = new byte[num];
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
                return array;
            }
            return new byte[]
            {
                (byte)length
            };
        }

        public BigInteger ReadBigInteger()
        {
            byte b = this.ReadByte();
            if (b != 2)
            {
                throw new InvalidOperationException("Invalid data type, INTEGER(02) is expected.");
            }
            int length = this.ReadLength();
            byte[] source = this.ReadBytes(length);
            return new BigInteger(source.Reverse<byte>().ToArray<byte>());
        }

        private byte ReadByte()
        {
            if (this._readerIndex > this._data.Count)
            {
                throw new InvalidOperationException("Read out of boundaries.");
            }
            return this._data [this._readerIndex++];
        }

        private byte[] ReadBytes(int length)
        {
            if (this._readerIndex + length > this._data.Count)
            {
                throw new InvalidOperationException("Read out of boundaries.");
            }
            byte[] array = new byte[length];
            this._data.CopyTo(this._readerIndex, array, 0, length);
            this._readerIndex += length;
            return array;
        }

        public int ReadInteger()
        {
            byte b = this.ReadByte();
            if (b != 2)
            {
                throw new InvalidOperationException("Invalid data type, INTEGER(02) is expected.");
            }
            int num = this.ReadLength();
            byte[] array = this.ReadBytes(num);
            if (num > 4)
            {
                throw new InvalidOperationException("Integer type cannot occupy more then 4 bytes");
            }
            int num2 = 0;
            int num3 = (num - 1) * 8;
            for (int i = 0; i < num; i++)
            {
                num2 |= (int)array [i] << num3;
                num3 -= 8;
            }
            return num2;
        }

        private int ReadLength()
        {
            int num = (int)this.ReadByte();
            if (num == 128)
            {
                throw new NotSupportedException("Indefinite-length encoding is not supported.");
            }
            if (num > 127)
            {
                int num2 = num & 127;
                if (num2 > 4)
                {
                    throw new InvalidOperationException(string.Format("DER length is '{0}' and cannot be more than 4 bytes.", num2));
                }
                num = 0;
                for (int i = 0; i < num2; i++)
                {
                    int num3 = (int)this.ReadByte();
                    num = (num << 8) + num3;
                }
                if (num < 0)
                {
                    throw new InvalidOperationException("Corrupted data - negative length found");
                }
            }
            return num;
        }

        public void Write(BigInteger data)
        {
            List<byte> list = data.ToByteArray().Reverse<byte>().ToList<byte>();
            this._data.Add(2);
            byte[] length = this.GetLength(list.Count);
            this.WriteBytes(length);
            this.WriteBytes(list);
        }

        public void Write(bool data)
        {
            this._data.Add(1);
            this._data.Add(1);
            this._data.Add(data ? (byte)1 : (byte)0);
        }

        public void Write(DerData data)
        {
            byte[] array = data.Encode();
            this._data.AddRange(array);
        }

        public void Write(byte[] data)
        {
            this._data.Add(4);
            byte[] length = this.GetLength(data.Length);
            this.WriteBytes(length);
            this.WriteBytes(data);
        }

        public void Write(ObjectIdentifier identifier)
        {
            ulong[] array = new ulong[identifier.Identifiers.Length - 1];
            array [0] = identifier.Identifiers [0] * 40uL + identifier.Identifiers [1];
            Buffer.BlockCopy(identifier.Identifiers, 16, array, 8, (identifier.Identifiers.Length - 2) * 8);
            List<byte> list = new List<byte>();
            ulong[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                ulong num = array2 [i];
                ulong num2 = num;
                byte[] array3 = new byte[8];
                int num3 = array3.Length - 1;
                byte b = (byte)(num2 & 127uL);
                do
                {
                    array3 [num3] = b;
                    if (num3 < array3.Length - 1)
                    {
                        byte[] expr_93_cp_0 = array3;
                        int expr_93_cp_1 = num3;
                        expr_93_cp_0 [expr_93_cp_1] |= 128;
                    }
                    num2 >>= 7;
                    b = (byte)(num2 & 127uL);
                    num3--;
                }
                while (b > 0);
                for (int j = num3 + 1; j < array3.Length; j++)
                {
                    list.Add(array3 [j]);
                }
            }
            this._data.Add(6);
            byte[] length = this.GetLength(list.Count);
            this.WriteBytes(length);
            this.WriteBytes(list);
        }

        private void WriteBytes(IEnumerable<byte> data)
        {
            this._data.AddRange(data);
        }

        public void WriteNull()
        {
            this._data.Add(5);
            this._data.Add(0);
        }
    }
}

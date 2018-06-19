#region Original License

//New BSD License(BSD)
//
//Copyright(c) 2014-2015 Cyotek Ltd
//Copyright(c) 2012, Alex Regueiro
//All rights reserved.
//
//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright
//      notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright
//      notice, this list of conditions and the following disclaimer in the
//      documentation and/or other materials provided with the distribution.
//    * Neither the name of Cyotek nor the
//      names of its contributors may be used to endorse or promote products
//      derived from this software without specific prior written permission.
//
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED.IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
//DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.CircularBuffer
{
    /// <summary>
    /// Represents a first-in, first-out collection of objects using a fixed buffer.
    /// </summary>
    /// <remarks>
    /// <para>The capacity of a <see cref="ByteCircularBuffer" /> is the number of elements the <see cref="ByteCircularBuffer"/> can hold. </para>
    /// <para>ByteCircularBuffer accepts <c>null</c> as a valid value for reference types and allows duplicate elements.</para>
    /// <para>The <see cref="Get()"/> methods will remove the items that are returned from the ByteCircularBuffer. To view the contents of the ByteCircularBuffer without removing items, use the <see cref="Peek()"/> or <see cref="PeekLast"/> methods.</para>
    /// </remarks>
    public class ByteCircularBuffer
    {
        // based on http://circularbuffer.codeplex.com/
        // http://en.wikipedia.org/wiki/Circular_buffer
        // modified from https://github.com/cyotek/Cyotek.Collections.Generic.CircularBuffer
        // some code taken from https://github.com/xorxornop/RingBuffer
        // and https://github.com/xorxornop/PerfCopy

        #region Instance Fields

        private byte[] _buffer;

        private int _capacity;

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCircularBuffer"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The maximum capcity of the buffer.</param>
        /// <exception cref="System.ArgumentException">Thown if the <paramref name="capacity"/> is less than zero.</exception>
        public ByteCircularBuffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("The buffer capacity must be greater than or equal to zero.",
                    nameof(capacity));
            }

            _buffer = new byte[capacity];
            this.Capacity = capacity;
            this.Size = 0;
            this.Head = 0;
            this.Tail = 0;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold.
        /// </summary>
        /// <value>The total number of elements that the <see cref="ByteCircularBuffer"/> can contain.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the specified new capacity is smaller than the current contents of the buffer.</exception>
        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value != _capacity)
                {
                    if (value < this.Size)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "The new capacity must be greater than or equal to the buffer size.");
                    }

                    var newBuffer = new byte[value];
                    if (this.Size > 0)
                    {
                        this.CopyTo(newBuffer);
                    }

                    _buffer = newBuffer;

                    _capacity = value;
                }
            }
        }

        /// <summary>
        /// Gets the index of the beginning of the buffer data.
        /// </summary>
        /// <value>The index of the first element in the buffer.</value>
        public int Head { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the buffer is empty.
        /// </summary>
        /// <value><c>true</c> if buffer is empty; otherwise, <c>false</c>.</value>
        public virtual bool IsEmpty => this.Size == 0;

        /// <summary>
        /// Gets a value indicating whether the buffer is full.
        /// </summary>
        /// <value><c>true</c> if the buffer is full; otherwise, <c>false</c>.</value>
        /// <remarks>The <see cref="IsFull"/> property always returns <c>false</c> if the <see cref="AllowOverwrite"/> property is set to <c>true</c>.</remarks>
        public virtual bool IsFull => this.Size == this.Capacity;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ByteCircularBuffer"/>.</value>
        public int Size { get; protected set; }

        /// <summary>
        /// Gets the index of the end of the buffer data.
        /// </summary>
        /// <value>The index of the last element in the buffer.</value>
        public int Tail { get; protected set; }

        #endregion

        #region Public Members

        /// <summary>
        /// Removes all items from the <see cref="ByteCircularBuffer" />.
        /// </summary>
        public void Clear()
        {
            this.Size = 0;
            this.Head = 0;
            this.Tail = 0;
            _buffer = new byte[this.Capacity];
        }

        /// <summary>
        /// Determines whether the <see cref="ByteCircularBuffer" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="ByteCircularBuffer" />.</param>
        /// <returns><c>true</c> if <paramref name="item" /> is found in the <see cref="ByteCircularBuffer" />; otherwise, <c>false</c>.</returns>
        public bool Contains(byte item)
        {
            var bufferIndex = this.Head;
            var comparer = EqualityComparer<byte>.Default;
            var result = false;

            for (int i = 0; i < this.Size; i++, bufferIndex++)
            {
                if (bufferIndex == this.Capacity)
                {
                    bufferIndex = 0;
                }

                if (comparer.Equals(_buffer[bufferIndex], item))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Copies the entire <see cref="ByteCircularBuffer"/> to a compatible one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        public void CopyTo(byte[] array)
        {
            this.CopyTo(array, 0);
        }

        /// <summary>
        /// Copies the entire <see cref="ByteCircularBuffer"/> to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(byte[] array, int arrayIndex)
        {
            this.CopyTo(this.Head, array, arrayIndex, Math.Min(this.Size, array.Length - arrayIndex));
        }

        /// <summary>
        /// Copies a range of elements from the <see cref="ByteCircularBuffer"/> to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="index">The zero-based index in the source <see cref="ByteCircularBuffer"/> at which copying begins.</param>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        public virtual void CopyTo(int index, byte[] array, int arrayIndex, int count)
        {
            if (count > this.Size)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    "The read count cannot be greater than the buffer size.");
            }

            var startAnchor = index;
            var dstIndex = arrayIndex;

            while (count > 0)
            {
                int chunk = Math.Min(Capacity - startAnchor, count);
                Buffer.BlockCopy(_buffer, startAnchor, array, dstIndex, chunk);
                startAnchor = (startAnchor + chunk == Capacity) ? 0 : startAnchor + chunk;
                dstIndex += chunk;
                count -= chunk;
            }
        }

        /// <summary>
        /// Removes and returns the specified number of objects from the beginning of the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="count">The number of elements to remove and return from the <see cref="ByteCircularBuffer"/>.</param>
        /// <returns>The objects that are removed from the beginning of the <see cref="ByteCircularBuffer"/>.</returns>
        public byte[] Get(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException("should greater than 0");
            var result = new byte[count];

            this.Get(result);

            return result;
        }

        /// <summary>
        /// Copies and removes the specified number elements from the <see cref="ByteCircularBuffer"/> to a compatible one-dimensional array, starting at the beginning of the target array. 
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <returns>The actual number of elements copied into <paramref name="array"/>.</returns>
        public int Get(byte[] array)
        {
            if (array.Length <= 0) throw new ArgumentOutOfRangeException("should greater than 0");
            return this.Get(array, 0, array.Length);
        }

        /// <summary>
        /// Copies and removes the specified number elements from the <see cref="ByteCircularBuffer"/> to a compatible one-dimensional array, starting at the specified index of the target array. 
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <returns>The actual number of elements copied into <paramref name="array"/>.</returns>
        public virtual int Get(byte[] array, int arrayIndex, int count)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Negative offset specified. Offsets must be positive.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Negative count specified. Count must be positive.");
            }
            if (count > this.Size)
            {
                throw new ArgumentException("Ringbuffer contents insufficient for take/read operation.", nameof(count));
            }
            if (array.Length < arrayIndex + count)
            {
                throw new ArgumentException("Destination array too small for requested output.");
            }
            var bytesCopied = 0;
            var dstIndex = arrayIndex;
            while (count > 0)
            {
                int chunk = Math.Min(Capacity - this.Head, count);
                Buffer.BlockCopy(_buffer, this.Head, array, dstIndex, chunk);
                this.Head = (this.Head + chunk == Capacity) ? 0 : this.Head + chunk;
                this.Size -= chunk;
                dstIndex += chunk;
                bytesCopied += chunk;
                count -= chunk;
            }
            return bytesCopied;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the <see cref="ByteCircularBuffer"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
        /// <remarks>This method is similar to the <see cref="Peek()"/> method, but <c>Peek</c> does not modify the <see cref="ByteCircularBuffer"/>.</remarks>
        public virtual byte Get()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("The buffer is empty.");
            }

            var item = _buffer[this.Head];
            if (++this.Head == this.Capacity)
            {
                this.Head = 0;
            }
            this.Size--;

            return item;
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="ByteCircularBuffer"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="ByteCircularBuffer"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
        public virtual byte Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("The buffer is empty.");
            }

            var item = _buffer[this.Head];

            return item;
        }

        /// <summary>
        /// Returns the specified number of objects from the beginning of the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="count">The number of elements to return from the <see cref="ByteCircularBuffer"/>.</param>
        /// <returns>The objects that from the beginning of the <see cref="ByteCircularBuffer"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
        public virtual byte[] Peek(int count)
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("The buffer is empty.");
            }

            var items = new byte[count];
            this.CopyTo(items);

            return items;
        }

        /// <summary>
        /// Returns the object at the end of the <see cref="ByteCircularBuffer"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="ByteCircularBuffer"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
        public virtual byte PeekLast()
        {
            int bufferIndex;

            if (this.IsEmpty)
            {
                throw new InvalidOperationException("The buffer is empty.");
            }

            if (this.Tail == 0)
            {
                bufferIndex = this.Size - 1;
            }
            else
            {
                bufferIndex = this.Tail - 1;
            }

            var item = _buffer[bufferIndex];

            return item;
        }

        /// <summary>
        /// Copies an entire compatible one-dimensional array to the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the source of the elements copied to <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if buffer does not have sufficient capacity to put in new items.</exception>
        /// <remarks>If <see cref="Size"/> plus the size of <paramref name="array"/> exceeds the capacity of the <see cref="ByteCircularBuffer"/> and the <see cref="AllowOverwrite"/> property is <c>true</c>, the oldest items in the <see cref="ByteCircularBuffer"/> are overwritten with <paramref name="array"/>.</remarks>
        public int Put(byte[] array)
        {
            return this.Put(array, 0, array.Length);
        }

        /// <summary>
        /// Copies a range of elements from a compatible one-dimensional array to the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the source of the elements copied to <see cref="ByteCircularBuffer"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if buffer does not have sufficient capacity to put in new items.</exception>
        /// <remarks>If <see cref="Size"/> plus <paramref name="count"/> exceeds the capacity of the <see cref="ByteCircularBuffer"/> and the <see cref="AllowOverwrite"/> property is <c>true</c>, the oldest items in the <see cref="ByteCircularBuffer"/> are overwritten with <paramref name="array"/>.</remarks>
        public virtual int Put(byte[] array, int arrayIndex, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
            if (this.Size + count > this.Capacity)
            {
                throw new InvalidOperationException("The buffer does not have sufficient capacity to put new items.");
            }

            if (array.Length < arrayIndex + count)
            {
                throw new ArgumentException("Source array too small for requested input.");
            }
            var srcIndex = arrayIndex;
            var bytesToProcess = count;
            while (bytesToProcess > 0)
            {
                int chunk = Math.Min(Capacity - Tail, bytesToProcess);
                Buffer.BlockCopy(array, srcIndex, _buffer, Tail, chunk);
                Tail = (Tail + chunk == Capacity) ? 0 : Tail + chunk;
                this.Size += chunk;
                srcIndex += chunk;
                bytesToProcess -= chunk;
            }

            return count;
        }

        /// <summary>
        /// Adds a byte to the end of the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="item">The byte to add to the <see cref="ByteCircularBuffer"/>. </param>
        /// <exception cref="System.InvalidOperationException">Thrown if buffer does not have sufficient capacity to put in new items.</exception>
        public virtual void Put(byte item)
        {
            if (IsFull)
            {
                throw new InvalidOperationException("The buffer does not have sufficient capacity to put new items.");
            }

            _buffer[this.Tail] = item;

            this.Tail++;
            if (this.Size == this.Capacity)
            {
                this.Head++;
                if (this.Head >= this.Capacity)
                {
                    this.Head -= this.Capacity;
                }
            }

            if (this.Tail == this.Capacity)
            {
                this.Tail = 0;
            }

            if (this.Size != this.Capacity)
            {
                this.Size++;
            }
        }

        /// <summary>
        /// Increments the starting index of the data buffer in the <see cref="ByteCircularBuffer"/>.
        /// </summary>
        /// <param name="count">The number of elements to increment the data buffer start index by.</param>
        public void Skip(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Negative count specified. Count must be positive.");
            }
            if (count > this.Size)
            {
                throw new ArgumentException("Ringbuffer contents insufficient for operation.", nameof(count));
            }

            // Modular division gives new offset position
            this.Head = (this.Head + count) % Capacity;
            this.Size -= count;
        }

        /// <summary>
        /// Copies the <see cref="ByteCircularBuffer"/> elements to a new array.
        /// </summary>
        /// <returns>A new array containing elements copied from the <see cref="ByteCircularBuffer"/>.</returns>
        /// <remarks>The <see cref="ByteCircularBuffer"/> is not modified. The order of the elements in the new array is the same as the order of the elements from the beginning of the <see cref="ByteCircularBuffer"/> to its end.</remarks>
        public byte[] ToArray()
        {
            var result = new byte[this.Size];

            this.CopyTo(result);

            return result;
        }

        #endregion
    }
}
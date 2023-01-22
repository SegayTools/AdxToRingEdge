﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.Collections
{
    public class CircularArray<T>
    {
        private T[] array;
        private int count;

        public int Capacity { get; init; }

        public CircularArray(int capacity)
        {
            Capacity = capacity;
            array = new T[capacity];
            Clear();
        }

        public T this[Index i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array[FixIndex(i)];
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array[FixIndex(index)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FixIndex(int ri) => count >= array.Length ? ((count + ri) % array.Length) : ri;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FixIndex(Index i) => FixIndex(i.IsFromEnd ? (array.Length - i.Value) : i.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T[] buffer) => Fill(buffer.AsMemory());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(Memory<T> buffer)
        {
            var baseIdx = FixIndex(0);

            var arrMem = array.AsMemory();

            arrMem.Slice(baseIdx, array.Length - baseIdx).CopyTo(buffer);
            arrMem.Slice(0, baseIdx).CopyTo(buffer.Slice(array.Length - baseIdx));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            array[count % array.Length] = item;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(array, 0, array.Length);
            count = 0;
        }
    }
}

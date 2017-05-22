﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace EasyRpc.AspNetCore.Data
{
    /// <summary>
    /// Static helper class for linked list
    /// </summary>
    public static class ImmutableLinkedList
    {
        /// <summary>
        /// Empty an immutable linked list in a thread and return the list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">list to empty</param>
        /// <returns></returns>
        public static ImmutableLinkedList<T> ThreadSafeEmpty<T>(ref ImmutableLinkedList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            return Interlocked.Exchange(ref list, ImmutableLinkedList<T>.Empty);
        }

        /// <summary>
        /// Add to a list in a thread safe manner
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        public static void ThreadSafeAdd<T>(ref ImmutableLinkedList<T> list, T value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            var listValue = list;
            var newList = listValue.Add(value);

            if (ReferenceEquals(Interlocked.CompareExchange(ref list, newList, listValue), listValue))
            {
                return;
            }

            var wait = new SpinWait();

            while (true)
            {
                wait.SpinOnce();

                listValue = list;
                newList = listValue.Add(value);

                if (ReferenceEquals(Interlocked.CompareExchange(ref list, newList, listValue), listValue))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Add a range to a list in a thread safe manner
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="values"></param>
        public static void ThreadSafeAddRange<T>(ref ImmutableLinkedList<T> list, IEnumerable<T> values)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (values == null) throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
            {
                ThreadSafeAdd(ref list, value);
            }
        }

        /// <summary>
        /// creates a new immutable linked list from an enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">enumerable of T</param>
        /// <returns>new list</returns>
        public static ImmutableLinkedList<T> From<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            return ImmutableLinkedList<T>.Empty.AddRange(enumerable);
        }

        /// <summary>
        /// Create a new immutable linked list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">values to create list from</param>
        /// <returns></returns>
        public static ImmutableLinkedList<T> Create<T>(params T[] values)
        {
            if (values == null || values.Length == 0)
            {
                return ImmutableLinkedList<T>.Empty;
            }

            return ImmutableLinkedList<T>.Empty.AddRange(values);
        }
    }

    /// <summary>
    /// Immutable linked list class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("{DebuggerDisplayString,nq}")]
    public class ImmutableLinkedList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Empty instance of list
        /// </summary>
        public static readonly ImmutableLinkedList<T> Empty = new ImmutableLinkedList<T>(default(T), null, 0);

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="next"></param>
        /// <param name="count"></param>
        private ImmutableLinkedList(T value, ImmutableLinkedList<T> next, int count)
        {
            Value = value;
            Next = next;
            Count = count;
        }

        /// <summary>
        /// For for link
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Next link in the list
        /// </summary>
        public ImmutableLinkedList<T> Next { get; }

        /// <summary>
        /// Count for list
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Add value to list
        /// </summary>
        /// <param name="value">value to add to list</param>
        /// <returns>new linked list</returns>
        public ImmutableLinkedList<T> Add(T value)
        {
            return new ImmutableLinkedList<T>(value, this, Count + 1);
        }

        /// <summary>
        /// Add range to linked list
        /// </summary>
        /// <param name="range">range to add</param>
        /// <returns>new linked list</returns>
        public ImmutableLinkedList<T> AddRange(IEnumerable<T> range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));

            var current = this;

            foreach (var value in range)
            {
                current = current.Add(value);
            }

            return current;
        }

        /// <summary>
        /// Get an enumerator for list
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new LinkedListEnumerator(this);
        }

        /// <summary>
        /// Get enumerator for list
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Reverse linked list
        /// </summary>
        /// <returns></returns>
        public ImmutableLinkedList<T> Reverse()
        {
            return this.Aggregate(Empty, (current, t) => current.Add(t));
        }

        /// <summary>
        /// Visit all values in list
        /// </summary>
        /// <param name="action">action to call</param>
        /// <param name="startAtEnd">start at the end of the linked list</param>
        public void Visit(Action<T> action, bool startAtEnd = false)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (this == Empty)
            {
                return;
            }

            if (!startAtEnd)
            {
                action(Value);
            }

            if (Next != Empty)
            {
                Next.Visit(action, startAtEnd);
            }

            if (startAtEnd)
            {
                action(Value);
            }
        }

        /// <summary>
        /// Check if list contains value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value)
        {
            if (this == Empty)
            {
                return false;
            }

            var current = this;

            while (current != null && current != Empty)
            {
                if (value.Equals(current.Value))
                {
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        private string DebuggerDisplayString => $"Count: {Count}";

        private class LinkedListEnumerator : IEnumerator<T>
        {
            private readonly ImmutableLinkedList<T> _start;
            private ImmutableLinkedList<T> _current;

            public LinkedListEnumerator(ImmutableLinkedList<T> current)
            {
                _start = current;
            }

            public bool MoveNext()
            {
                if (_current == null)
                {
                    if (_start.Next == null)
                    {
                        return false;
                    }

                    _current = _start;
                }
                else if (_current.Next?.Next == null)
                {
                    return false;
                }
                else
                {
                    _current = _current.Next;
                }

                return true;
            }

            public void Reset()
            {
                _current = _start;
            }

            public T Current => _current.Value;

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }
        }
    }
}

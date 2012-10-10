﻿// ***********************************************************************
// Assembly         : SquirrelDb
// Author           : Steve
// Created          : 10-08-2012
//
// Last Modified By : Steve
// Last Modified On : 10-08-2012
// ***********************************************************************
// <copyright file="KeyTree.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SquirrelDb
{
    /// <summary>
    /// Class KeyTree
    /// </summary>
    [Serializable]
    public class KeyTree
    {
        #region public properties

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        public long Count { get; set; }

        #endregion

        #region private properties

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the root.
        /// </summary>
        /// <value>The root.</value>
        public KeyNode Root { get; set; }

        #endregion

        #region constructors

        /// <summary>
        /// Creates the new.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>KeyTree.</returns>
        public static KeyTree CreateNew(string fileName)
        {
            var kt = new KeyTree { FileName = fileName };
            kt.Save();

            return kt;
        }

        /// <summary>
        /// Loads the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>KeyTree.</returns>
        public static KeyTree Load(string fileName)
        {
            var formatter = new BinaryFormatter();
            KeyTree output;
            using (var fs = File.Open(fileName,FileMode.Open))
            {
                output = formatter.Deserialize(fs) as KeyTree;
            }

            return output;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Inserts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pointer">The pointer.</param>
        public void Insert(long value, DataPointer pointer)
        {
            if (Root == null)
            {
                Count++;
                Root = new KeyNode { Value = value, Pointer = pointer };
                return;
            }

            var freeNode = SeekFreeNode(value);

            if (freeNode.Value.Equals(value) && !freeNode.Pointer.Equals(pointer))
            {
                throw new DuplicateKeyException();
            }

            if (value < freeNode.Value)
            {
                Count++;
                freeNode.Left = new KeyNode { Parent = freeNode, Pointer = pointer, Value = value };
                Save();
                return;
            }

            Count++;
            freeNode.Right = new KeyNode { Parent = freeNode, Pointer = pointer, Value = value };
            Save();
        }

        /// <summary>
        /// Updates the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="newDocumentSize">New size of the document.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public KeyNode Update(long value, int newDocumentSize)
        {
            var existing = SeekNode(Root,value);

            if (existing != null)
            {
                existing.Pointer.UpdateSize(newDocumentSize);
                Save();
                return existing;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        /// Seeks the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="value">The value.</param>
        /// <returns>KeyNode.</returns>
        public KeyNode SeekNode(KeyNode node, long value)
        {
            if (Root == null)
                return null;

            if (node == null)
                node = Root;

            if (node.Value.Equals(value))
                return node;

            var seek = node;
            
            while (seek != null)
            {
                if (seek.Value.Equals(value))
                    return seek;

                if (seek.Left != null && value < seek.Value)
                    seek = seek.Left;

                if (seek.Right != null && value > seek.Value)
                    seek = seek.Right;
            }

            return null;
        }

        /// <summary>
        /// Deletes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Delete(long value)
        {
            var node = SeekNode(null, value);

            if (node == null)
                return;


            Count--;
            if (node == Root)
            {
                if (node.Right != null)
                {
                    Root = node.Right;
                }
                else
                {
                    Root = node.Left;
                    return;
                }

                if (node.Left == null)
                    return;

                if (Root.Left == null)
                {
                    Root.Left = node.Left;
                    return;
                }

                var lastParent = Root;
                while (lastParent.Left != null)
                {
                    lastParent = lastParent.Left;
                }

                lastParent.Left = node.Left;
            }

            if (node.Left == null && node.Right == null)
            {
                if (node.Parent.Left.Equals(node))
                {
                    node.Parent.Left = null;
                    return;
                }

                if (node.Parent.Right.Equals(node))
                {
                    node.Parent.Right = null;
                    return;
                }
            }

            if (node.Right != null)
            {
                if (node.Parent.Right.Equals(node))
                {
                    node.Parent.Right = node.Right;
                    if (node.Left != null)
                    {
                        var newParent = SeekFreeNode(node.Left.Value,node.Right);
                        if (node.Left.Value < newParent.Value)
                        {
                            newParent.Left = node.Left;
                            node.Left.Parent = newParent;
                        }
                        else
                        {
                            newParent.Right = node.Left;
                            node.Left.Parent = newParent;
                        }
                    }
                }
                else
                {
                    node.Parent.Right = node.Right;
                }

                return;
            }

            //left
            if (node.Left != null)
            {
                node.Parent.Right = node.Left;
            }
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            var saveTask = new Task(() =>
            {
                lock (_saveLock)
                {
                    var formatter = new BinaryFormatter();
                    using (var stream = File.Open(FileName, FileMode.OpenOrCreate))
                    {
                        formatter.Serialize(stream, this);
                        stream.Close();
                    }

                }
            });
            saveTask.Start();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Seeks the free node.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="from">From.</param>
        /// <returns>KeyNode.</returns>
        private KeyNode SeekFreeNode(long value, KeyNode from = null)
        {
            var node = from ?? Root;

            if (node.Value.Equals(value))
                return node;

            var seek = node;

            while (seek != null)
            {
                if (seek.Value.Equals(value))
                    return seek;

                if (seek.Left == null && value < seek.Value)
                    return seek;

                if (seek.Right == null && value > seek.Value)
                    return seek;

                if (seek.Left != null && value < seek.Value)
                    seek = seek.Left;

                if (seek.Right != null && value > seek.Value)
                    seek = seek.Right;
            }

            return null;
        }

        /// <summary>
        /// The _save lock
        /// </summary>
        private readonly object _saveLock = new object();

        #endregion
    }
}

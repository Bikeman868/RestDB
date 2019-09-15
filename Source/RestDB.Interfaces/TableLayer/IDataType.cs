using System;
using System.Collections.Generic;
using System.Text;

namespace RestDB.Interfaces.TableLayer
{
    public interface IDataType
    {
        /// <summary>
        /// The name of this data type to use in query languages
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns a list of alternate (equivalent) names for this type. For example
        /// in your query language you can use 'int' or 'integer' interchangeably, they
        /// refer to the same data type
        /// </summary>
        string[] AliasNames { get; }

        /// <summary>
        /// The maximum length of data that can be stored. For most data types
        /// this is 1, but for strings and array this is the maximum number of 
        /// characters or array elements that can be stored. Note that this is
        /// NOT the size in bytes.
        /// </summary>
        ushort MaxLength { get; }

        /// <summary>
        /// True if the field can have no value
        /// </summary>
        bool Nullable { get; }

        /// <summary>
        /// The fundamental type of the data. Requesting data in this
        /// format does not require any conversion and is therefore more
        /// efficient. The data type itself only supports this C# type, the
        /// column definition detects mismatches and performs conversions to
        /// other types.
        /// </summary>
        Type RawType { get; }

        /// <summary>
        /// Calculates the number of bytes used to store fields in database files
        /// </summary>
        /// <param name="length">For data types that support variable length pass
        /// the length here. For example strings should pass the actual number of
        /// characters in the string. Tables can choose to allocate ByteSize(MaxLength)
        /// bytes in the record buffer for this field, or they can reorganize the
        /// record buffer to accomodate different length fields and keep track of how
        /// much space was reserved. This behavior is not defined by the data type</param>
        ushort ByteSize(int length = 1);

        /// <summary>
        /// The default value to use for new fields that are not initialized. The
        /// returned value will be of RawType
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// The value to use for fields that contain no value. The
        /// returned value will be of RawType
        /// </summary>
        object NullValue { get; }

        /// <summary>
        /// Reads bytes from a buffer into a c# struct, array or string
        /// </summary>
        /// <param name="buffer">An array of bytes to extract the value from</param>
        /// <param name="offset">The offset into the buffer to start reading</param>
        object Read(byte[] buffer, uint offset);

        /// <summary>
        /// Tests a field to see if it contains no value
        /// </summary>
        /// <param name="buffer">An array of bytes to examine</param>
        /// <param name="offset">The offset into the buffer to start examining</param>
        bool IsNull(byte[] buffer, uint offset);

        /// <summary>
        /// Writes a c# struct, array of string into a byte buffer
        /// </summary>
        /// <param name="buffer">An array of bytes to store the value into</param>
        /// <param name="offset">The offset into the buffer to start writing</param>
        /// <param name="value">The value to write. Must be of RawType</param>
        void Write(byte[] buffer, uint offset, object value);

        /// <summary>
        /// Writes a null value into a byte buffer
        /// </summary>
        /// <param name="buffer">An array of bytes to store the null value into</param>
        /// <param name="offset">The offset into the buffer to start writing</param>
        void WriteNull(byte[] buffer, uint offset);
    }
}

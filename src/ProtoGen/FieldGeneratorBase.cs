// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// http://github.com/jskeet/dotnet-protobufs/
// Original C++/Java/Python code:
// http://code.google.com/p/protobuf/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using System;
using System.Globalization;
using Google.ProtocolBuffers.Descriptors;

namespace Google.ProtocolBuffers.ProtoGen {
  internal abstract class FieldGeneratorBase : SourceGeneratorBase<FieldDescriptor> {
    protected FieldGeneratorBase(FieldDescriptor descriptor)
        : base(descriptor) {
    }

    private static bool AllPrintableAscii(string text) {
      foreach (char c in text) {
        if (c < 0x20 || c > 0x7e) {
          return false;
        }
      }
      return true;
    }

    protected string DefaultValue {
      get {
        string suffix = "";
        switch (Descriptor.FieldType) {
          case FieldType.Float:  suffix = "F"; break;
          case FieldType.Double: suffix = "D"; break;
          case FieldType.Int64:  suffix = "L"; break;
          case FieldType.UInt64: suffix = "UL"; break;
        }
        switch (Descriptor.FieldType) {
          case FieldType.Float:
          case FieldType.Double:
          case FieldType.Int32:
          case FieldType.Int64:
          case FieldType.SInt32:
          case FieldType.SInt64:
          case FieldType.SFixed32:
          case FieldType.SFixed64:
          case FieldType.UInt32:
          case FieldType.UInt64:
          case FieldType.Fixed32:
          case FieldType.Fixed64:
            // The simple Object.ToString converts using the current culture.
            // We want to always use the invariant culture so it's predictable.
            IConvertible value = (IConvertible) Descriptor.DefaultValue;
            return value.ToString(CultureInfo.InvariantCulture) + suffix;
          case FieldType.Bool:
            return (bool) Descriptor.DefaultValue ? "true" : "false";

          case FieldType.Bytes:
            if (!Descriptor.HasDefaultValue) {
              return "pb::ByteString.Empty";
            }
            return string.Format("(pb::ByteString) {0}.Descriptor.Fields[{1}].DefaultValue", GetClassName(Descriptor.ContainingType), Descriptor.Index);
          case FieldType.String:
            if (AllPrintableAscii(Descriptor.Proto.DefaultValue)) {
              // All chars are ASCII and printable.  In this case we only
              // need to escape quotes and backslashes.
              return "\"" + Descriptor.Proto.DefaultValue
                  .Replace("\\", "\\\\")
                  .Replace("'", "\\'")
                  .Replace("\"", "\\\"")
                  + "\"";
            }
            return string.Format("(string) {0}.Descriptor.Fields[{1}].DefaultValue", GetClassName(Descriptor.ContainingType), Descriptor.Index);
          case FieldType.Enum:
            return TypeName + "." + ((EnumValueDescriptor) Descriptor.DefaultValue).Name;
          case FieldType.Message:
          case FieldType.Group:
            return TypeName + ".DefaultInstance";
          default:
            throw new InvalidOperationException("Invalid field descriptor type");
        }
      }
    }

    protected string PropertyName {
      get {
        return Descriptor.CSharpOptions.PropertyName;
      }
    }

    protected string Name {
      get { return NameHelpers.UnderscoresToCamelCase(GetFieldName(Descriptor)); }
    }

    protected int Number {
      get { return Descriptor.FieldNumber; }
    }

    protected void AddNullCheck(TextGenerator writer) {
      AddNullCheck(writer, "value");
    }

    protected void AddNullCheck(TextGenerator writer, string name) {
      if (IsNullableType) {
        writer.WriteLine("  pb::ThrowHelper.ThrowIfNull({0}, \"{0}\");", name);
      }
    }

    protected void AddClsComplianceCheck(TextGenerator writer) {
      if (!Descriptor.IsCLSCompliant) {
        writer.WriteLine("[global::System.CLSCompliant(false)]");
      }
    }

    /// <summary>
    /// For encodings with fixed sizes, returns that size in bytes.  Otherwise
    /// returns -1. TODO(jonskeet): Make this less ugly.
    /// </summary>
    protected int FixedSize {
      get {
        switch (Descriptor.FieldType) {
          case FieldType.UInt32:
          case FieldType.UInt64:
          case FieldType.Int32:
          case FieldType.Int64:
          case FieldType.SInt32:
          case FieldType.SInt64:
          case FieldType.Enum:
          case FieldType.Bytes:
          case FieldType.String:
          case FieldType.Message:
          case FieldType.Group:
            return -1;
          case FieldType.Float:
            return WireFormat.FloatSize;
          case FieldType.SFixed32:
            return WireFormat.SFixed32Size;
          case FieldType.Fixed32:
            return WireFormat.Fixed32Size;
          case FieldType.Double:
            return WireFormat.DoubleSize;
          case FieldType.SFixed64:
            return WireFormat.SFixed64Size;
          case FieldType.Fixed64:
            return WireFormat.Fixed64Size;
          case FieldType.Bool:
            return WireFormat.BoolSize;
          default:
            throw new InvalidOperationException("Invalid field descriptor type");
        }
      }
    }

    protected bool IsNullableType {
      get {
        switch (Descriptor.FieldType) {
          case FieldType.Float:
          case FieldType.Double:
          case FieldType.Int32:
          case FieldType.Int64:
          case FieldType.SInt32:
          case FieldType.SInt64:
          case FieldType.SFixed32:
          case FieldType.SFixed64:
          case FieldType.UInt32:
          case FieldType.UInt64:
          case FieldType.Fixed32:
          case FieldType.Fixed64:
          case FieldType.Bool:
          case FieldType.Enum:
            return false;
          case FieldType.Bytes:
          case FieldType.String:
          case FieldType.Message:
          case FieldType.Group:
            return true;
          default:
            throw new InvalidOperationException("Invalid field descriptor type");
        }
      }
    }

    protected string TypeName {
      get {
        switch (Descriptor.FieldType) {
          case FieldType.Enum:
            return GetClassName(Descriptor.EnumType);
          case FieldType.Message:
          case FieldType.Group:
            return GetClassName(Descriptor.MessageType);
          default:
            return DescriptorUtil.GetMappedTypeName(Descriptor.MappedType);
        }
      }
    }

    protected string MessageOrGroup {
      get { return Descriptor.FieldType == FieldType.Group ? "Group" : "Message"; }
    }

    /// <summary>
    /// Returns the type name as used in CodedInputStream method names: SFixed32, UInt32 etc.
    /// </summary>
    protected string CapitalizedTypeName {
      get {
        // Our enum names match perfectly. How serendipitous.
        return Descriptor.FieldType.ToString();
      }
    }
  }
}

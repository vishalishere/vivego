// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: Protos.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace vivego.Proto.Messages {

  /// <summary>Holder for reflection information generated from Protos.proto</summary>
  public static partial class ProtosReflection {

    #region Descriptor
    /// <summary>File descriptor for Protos.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ProtosReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgxQcm90b3MucHJvdG8SDHZpdmVnby5Qcm90bxoWUmVmZXJlbmNlL1Byb3Rv",
            "cy5wcm90byJACgROb2RlEhcKA1BJRBgBIAEoCzIKLmFjdG9yLlBJRBIQCghN",
            "ZW1iZXJJZBgCIAEoCRINCgVLaW5kcxgDIAMoCUIYqgIVdml2ZWdvLlByb3Rv",
            "Lk1lc3NhZ2VzYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Proto.ProtosReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::vivego.Proto.Messages.Node), global::vivego.Proto.Messages.Node.Parser, new[]{ "PID", "MemberId", "Kinds" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class Node : pb::IMessage<Node> {
    private static readonly pb::MessageParser<Node> _parser = new pb::MessageParser<Node>(() => new Node());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<Node> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::vivego.Proto.Messages.ProtosReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Node() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Node(Node other) : this() {
      PID = other.pID_ != null ? other.PID.Clone() : null;
      memberId_ = other.memberId_;
      kinds_ = other.kinds_.Clone();
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Node Clone() {
      return new Node(this);
    }

    /// <summary>Field number for the "PID" field.</summary>
    public const int PIDFieldNumber = 1;
    private global::Proto.PID pID_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Proto.PID PID {
      get { return pID_; }
      set {
        pID_ = value;
      }
    }

    /// <summary>Field number for the "MemberId" field.</summary>
    public const int MemberIdFieldNumber = 2;
    private string memberId_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string MemberId {
      get { return memberId_; }
      set {
        memberId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "Kinds" field.</summary>
    public const int KindsFieldNumber = 3;
    private static readonly pb::FieldCodec<string> _repeated_kinds_codec
        = pb::FieldCodec.ForString(26);
    private readonly pbc::RepeatedField<string> kinds_ = new pbc::RepeatedField<string>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<string> Kinds {
      get { return kinds_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as Node);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(Node other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(PID, other.PID)) return false;
      if (MemberId != other.MemberId) return false;
      if(!kinds_.Equals(other.kinds_)) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (pID_ != null) hash ^= PID.GetHashCode();
      if (MemberId.Length != 0) hash ^= MemberId.GetHashCode();
      hash ^= kinds_.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (pID_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(PID);
      }
      if (MemberId.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(MemberId);
      }
      kinds_.WriteTo(output, _repeated_kinds_codec);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (pID_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(PID);
      }
      if (MemberId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(MemberId);
      }
      size += kinds_.CalculateSize(_repeated_kinds_codec);
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(Node other) {
      if (other == null) {
        return;
      }
      if (other.pID_ != null) {
        if (pID_ == null) {
          pID_ = new global::Proto.PID();
        }
        PID.MergeFrom(other.PID);
      }
      if (other.MemberId.Length != 0) {
        MemberId = other.MemberId;
      }
      kinds_.Add(other.kinds_);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            if (pID_ == null) {
              pID_ = new global::Proto.PID();
            }
            input.ReadMessage(pID_);
            break;
          }
          case 18: {
            MemberId = input.ReadString();
            break;
          }
          case 26: {
            kinds_.AddEntriesFrom(input, _repeated_kinds_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code

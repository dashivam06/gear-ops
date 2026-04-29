using System.Runtime.Serialization;
namespace gearOps.Domain.Enums;
public enum ServiceRecordStatus { [EnumMember(Value = "In Progress")] InProgress, Completed, Cancelled }

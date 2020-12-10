using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace DDD.Domain
{
    [Serializable]
    public class ConcurrencyException : Exception
    {
        public const string PROP_MAXVERSION = "MaxVersion";
        public const string PROP_EXPECTEDVERSION = "ExpectedVersion";

        public int ExpectedVersion { get; private set; }

        public int MaxVersion { get; private set; }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public ConcurrencyException(SerializationInfo info, StreamingContext context)
        {
            MaxVersion = info.GetInt32(PROP_MAXVERSION);
            ExpectedVersion = info.GetInt32(PROP_EXPECTEDVERSION);
        }

        public ConcurrencyException(int expectedVersion, int maxVersion)
        {
            this.ExpectedVersion = expectedVersion;
            this.MaxVersion = maxVersion;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue(PROP_MAXVERSION, MaxVersion);
            info.AddValue(PROP_EXPECTEDVERSION, ExpectedVersion);
            base.GetObjectData(info, context);
        }
    }
}

namespace Projector.IO.Implementation.Protocol
{
    static class Constants
    {
        public static class MessageType
        {
            public static byte Subscribe = (byte)'+';
            public static byte Unsubscribe = (byte)'-';
            public static byte Schema = (byte)'s';
            public static byte RowAdded = (byte)'a';
            public static byte RowUpdated = (byte)'u';
            public static byte RowDeleted = (byte)'d';
            public static byte SyncPoint = (byte)'p';
            public static byte Ok = (byte)'o';
        }

        public static class FieldType
        {
            public static byte Int = 0;
            public static byte Long = 1;
            public static byte String = 2;
        }
    }
}

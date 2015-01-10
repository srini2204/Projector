
namespace Projector.IO.Implementation.Protocol
{
    static class Constants
    {
        public static class MessageType
        {
            public static byte Subscribe = (byte)'+';
            public static byte Unsubscribe = (byte)'-';
            public static byte Schema = (byte)'s';
            public static byte BeginAdd = (byte)'b';
            public static byte RowAdded = (byte)'a';
            public static byte EndAdd = (byte)'c';
            public static byte BeginUpdate = (byte)'v';
            public static byte RowUpdated = (byte)'u';
            public static byte EndUpdate = (byte)'w';
            public static byte BeginDelete = (byte)'e';
            public static byte RowDeleted = (byte)'d';
            public static byte EndDelete = (byte)'f';
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


namespace Projector.IO.Implementation.Protocol
{
    static class Constants
    {
        static class MessageType
        {
            static byte Subscribe = (byte)'+';
            static byte Unsubscribe = (byte)'-';
            static byte Schema = (byte)'s';
            static byte RowAdded = (byte)'a';
            static byte RowUpdated = (byte)'u';
            static byte RowDeleted = (byte)'d';
            static byte SyncPoint = (byte)'p';
        }

        static class FieldType
        {
            static byte Int = 0;
            static byte Long = 1;
            static byte String = 2;
        }
    }
}

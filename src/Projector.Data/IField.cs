
using System;

namespace Projector.Data
{
    public interface IField
    {
        Type DataType { get; }

        string Name { get; }
    }

    public interface IWritableField : IField
    {
        void SetCurrentRow(int rowId);

        void EnsureCapacity(int rowId);
    }

    public interface IField<TData> : IField
    {
        TData Value { get; }
    }

    public interface IWritableField<TData> : IField<TData>, IWritableField
    {
        void SetValue(TData value);
    }
}

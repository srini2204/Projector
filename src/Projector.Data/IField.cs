
using System;

namespace Projector.Data
{
    public interface IField
    {
        Type DataType { get; }

        string Name { get; }

        void SetCurrentRow(int rowId);
    }

    public interface IField<TData> : IField
    {
        TData Value { get; }
    }

    public interface IWritableField<TData> : IField
    {
        void SetValue(TData value);
    }
}

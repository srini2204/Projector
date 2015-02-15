using System.Collections.Generic;

namespace Projector.Data.Join
{
    public class Join<TLeft, TRight, TKey, TResult> : DataProviderBase, IDataProvider<TResult>
    {
        private ChangeTracker _leftChangeTracker;
        private ChangeTracker _rightChangeTracker;

        public Join(IDataProvider<TLeft> leftSource, IDataProvider<TRight> rightSource, JoinType joinType)
        {
            _leftChangeTracker = new ChangeTracker(leftSource);
            _leftChangeTracker.OnAdded += _leftChangeTracker_OnAdded;
            _leftChangeTracker.OnDeleted += _leftChangeTracker_OnDeleted;
            _leftChangeTracker.OnUpdated += _leftChangeTracker_OnUpdated;
            _leftChangeTracker.OnSchemaArrived += _leftChangeTracker_OnSchemaArrived;
            _leftChangeTracker.OnSyncPointArrived += _leftChangeTracker_OnSyncPointArrived;

            _rightChangeTracker = new ChangeTracker(rightSource);
        }

        void _leftChangeTracker_OnSyncPointArrived()
        {
            throw new System.NotImplementedException();
        }

        void _leftChangeTracker_OnSchemaArrived(ISchema obj)
        {
            throw new System.NotImplementedException();
        }

        void _leftChangeTracker_OnUpdated(IList<int> arg1, IList<IField> arg2)
        {
            throw new System.NotImplementedException();
        }

        void _leftChangeTracker_OnDeleted(IList<int> obj)
        {
            throw new System.NotImplementedException();
        }

        void _leftChangeTracker_OnAdded(IList<int> obj)
        {
            throw new System.NotImplementedException();
        }


    }
}

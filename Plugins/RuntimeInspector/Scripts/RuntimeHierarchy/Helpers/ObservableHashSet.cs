using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

namespace RuntimeInspectorNamespace
{
	public class ObservableHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T>, IDeserializationCallback, ISerializable
	{
		#region Constructors
		public ObservableHashSet()
			=> hashSet = new HashSet<T>();

		public ObservableHashSet( IEnumerable<T> collection )
			=> hashSet = new HashSet<T>( collection );

		public ObservableHashSet( IEqualityComparer<T> comparer )
			=> hashSet = new HashSet<T>( comparer );

		public ObservableHashSet( IEnumerable<T> collection, IEqualityComparer<T> comparer )
			=> hashSet = new HashSet<T>( collection, comparer );
		#endregion

		#region Operators
		public static implicit operator bool( ObservableHashSet<T> set ) => set != null;
		#endregion

		#region Properties
		public int Count => hashSet.Count;
		// Implements both ICollection<T> and IReadOnlyCollection<T>
		public bool IsReadOnly => ( (ICollection<T>) hashSet ).IsReadOnly;
		#endregion

		#region Fields
		private readonly HashSet<T> hashSet;
		#endregion

		#region Events
		public event NotifyCollectionChangedEventHandlerSlim<T> CollectionChanged;
		#endregion

		#region ICollection
		void ICollection<T>.Add( T item ) => Add( item );

		public void Clear()
		{
			( (ICollection<T>) hashSet ).Clear();
			var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Remove, null );
			CollectionChanged?.Invoke( this, args );
		}

		public bool Contains( T item )
			=> ( (ICollection<T>) hashSet ).Contains( item );

		public void CopyTo( T[] array, int arrayIndex )
			=> ( (ICollection<T>) hashSet ).CopyTo( array, arrayIndex );

		public bool Remove( T item )
		{
			if( ( (ICollection<T>) hashSet ).Remove( item ) )
			{
				var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Remove, hashSet.ToArray() );
				CollectionChanged?.Invoke( this, args );
				return true;
			}
			return false;
		}
		#endregion

		#region IEnumerable<T>
		public IEnumerator<T> GetEnumerator()
			=> ( (IEnumerable<T>) hashSet ).GetEnumerator();
		#endregion

		#region IEnumerable
		IEnumerator IEnumerable.GetEnumerator()
			=> ( (IEnumerable) hashSet ).GetEnumerator();
		#endregion

		#region ISet

		public bool Add( T item )
		{
			// Null entries are not allowed
			if( item == null )
				return false;

			bool success = ( (ISet<T>) hashSet ).Add( item );
			if( success )
			{
				var args = new NotifyCollectionChangedEventArgsSlim<T>(
					NotifyCollectionChangedAction.Add,
					hashSet.ToArray() );
				CollectionChanged?.Invoke( this, args );
			}
			return success;
		}

		public void ExceptWith( IEnumerable<T> other )
		{
			( (ISet<T>) hashSet ).ExceptWith( other );
			var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Remove, hashSet.ToArray() );
			CollectionChanged?.Invoke( this, args );
		}

		public void IntersectWith( IEnumerable<T> other )
		{
			( (ISet<T>) hashSet ).IntersectWith( other );
			var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Remove, hashSet.ToArray() );
			CollectionChanged?.Invoke( this, args );
		}

		public bool IsProperSubsetOf( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).IsProperSubsetOf( other );

		public bool IsProperSupersetOf( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).IsProperSupersetOf( other );

		public bool IsSubsetOf( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).IsSubsetOf( other );

		public bool IsSupersetOf( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).IsSupersetOf( other );
		public bool Overlaps( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).Overlaps( other );


		public bool SetEquals( IEnumerable<T> other )
			=> ( (ISet<T>) hashSet ).SetEquals( other );

		public void SymmetricExceptWith( IEnumerable<T> other )
		{
			( (ISet<T>) hashSet ).SymmetricExceptWith( other );
			var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Replace, hashSet.ToArray() );
			CollectionChanged?.Invoke( this, args );
		}

		public void UnionWith( IEnumerable<T> other )
		{
			( (ISet<T>) hashSet ).UnionWith( other );
			var args = new NotifyCollectionChangedEventArgsSlim<T>( NotifyCollectionChangedAction.Add, hashSet.ToArray() );
			CollectionChanged?.Invoke( this, args );
		}
		#endregion

		#region IDeserializationCallback
		public void OnDeserialization( object sender )
			=> ( (IDeserializationCallback) hashSet ).OnDeserialization( sender );
		#endregion

		#region ISerializable
		public void GetObjectData( SerializationInfo info, StreamingContext context )
			=> ( (ISerializable) hashSet ).GetObjectData( info, context );
		#endregion
	}
}

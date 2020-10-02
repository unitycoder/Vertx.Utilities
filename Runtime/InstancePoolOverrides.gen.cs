//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

namespace Vertx.Utilities
{
	public static partial class InstancePool
	{
		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent</param>
		public static void Warmup<TInstanceType>(TInstanceType prefab, int count, Transform parent = null)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.Warmup(prefab, count, parent);

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent</param>
		public static System.Collections.IEnumerator WarmupCoroutine<TInstanceType>(TInstanceType prefab, int count, Transform parent = null)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.WarmupCoroutine(prefab, count, parent);

		/// <summary>
		/// Retrieves an instance from the pool, positioned at the origin.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public static TInstanceType Get<TInstanceType>(TInstanceType prefab, Transform parent = null)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.Get(prefab, parent);

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public static TInstanceType Get<TInstanceType>(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.World)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.Get(prefab, parent, position, rotation, space);

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="localScale">Local Scale of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public static TInstanceType Get<TInstanceType>(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.Get(prefab, parent, position, rotation, localScale, space);

		/// <summary>
		/// Returns a Component instance to the pool.
		/// </summary>
		/// <param name="prefab">The prefab key used when the instance was retrieved via <see cref="Get(TInstanceType,Transform)"/></param>
		/// <param name="instance">The instance to return to the pool.</param>
		public static void Pool<TInstanceType>(TInstanceType prefab, TInstanceType instance)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.Pool(prefab, instance);

		/// <summary>
		/// If you are temporarily working with pools for prefabs you can remove them from the system by calling this function.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		public static void RemovePrefabPool<TInstanceType>(TInstanceType prefab)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.RemovePrefabPool(prefab);

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/> for all instances shared between the type <see cref="TInstanceType"/>
		/// </summary>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public static void SetCapacities<TInstanceType>(int capacity)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.SetCapacities(capacity);

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/>
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public static void SetCapacity<TInstanceType>(TInstanceType prefab, int capacity)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.SetCapacity(prefab, capacity);

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="defaultCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called</param>
		public static void TrimExcess<TInstanceType>(int defaultCapacity = 20)
			where TInstanceType : Component
			=> InstancePool<TInstanceType>.TrimExcess(defaultCapacity);
	}
}
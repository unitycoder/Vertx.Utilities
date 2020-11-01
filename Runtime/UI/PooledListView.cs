﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Vertx.Utilities
{
	[AddComponentMenu("Layout/Pooled List View")]
	public class PooledListView : ScrollRect
	{
		private enum Snapping
		{
			None,
			Snapped
		}

		[SerializeField] private Snapping snapping;
		
		[SerializeField] private RectTransform prefab;
		[Min(0)]
		[SerializeField] private float elementHeight;

		[System.Serializable]
		public class BindEvent : UnityEvent<int, RectTransform> { }

		[SerializeField] private BindEvent bindItem;
		public BindEvent BindItem => bindItem;

		private LayoutElement startPaddingElement, endPaddingElement;
		private IList list;

		protected override void Start()
		{
			base.Start();

			if (verticalScrollbar != null)
			{
				//I wish I didn't have to remove the listener, but otherwise the Snapped behaviour will not function properly.
				verticalScrollbar.onValueChanged.RemoveAllListeners();
				verticalScrollbar.onValueChanged.AddListener(Position);
			}

			if (content != null)
			{
				var go = content.gameObject;
				if (!go.TryGetComponent(out ContentSizeFitter fitter))
					fitter = go.AddComponent<ContentSizeFitter>();
				fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			}

			horizontal = false;
			vertical = true;
			horizontalScrollbar = null;
		}

		public void Bind(IList elements)
		{
			list = elements;
			Refresh();
		}
		
		void Pool()
		{
			startPaddingElement.transform.SetSiblingIndex(0);
			endPaddingElement.transform.SetSiblingIndex(1);
			foreach (var instance in boundInstances)
				InstancePool.Pool(prefab, instance.Value);

			boundInstances.Clear();
		}

		private readonly Dictionary<int, RectTransform> boundInstances = new Dictionary<int, RectTransform>();

		public void Refresh()
		{
			if (startPaddingElement == null)
			{
				startPaddingElement = new GameObject("Start Padding", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
				startPaddingElement.transform.SetParent(content);
				endPaddingElement = new GameObject("End Padding", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
				endPaddingElement.transform.SetParent(content);
			}

			Pool();

			if (list == null || list.Count == 0)
				return;

			Position(verticalScrollbar.value);
		}

		private readonly List<int> toRemove = new List<int>();

		void Position(float value)
		{
			if (list == null || list.Count == 0)
				return;

			int elementCount = list.Count;

			value = 1 - value; // thanks
			
			float rectHeight = viewport.rect.height;
			float onScreenElements = rectHeight / elementHeight;
			float totalElementHeight = elementHeight * elementCount;
			const float startIndex = 0;
			float endIndex = elementCount - onScreenElements;

			float zeroIndex = Mathf.Lerp(startIndex, endIndex, value);

			if (snapping == Snapping.Snapped)
			{
				zeroIndex = Mathf.RoundToInt(zeroIndex);
				value = Mathf.InverseLerp(startIndex, endIndex, zeroIndex);
				verticalScrollbar.SetValueWithoutNotify(1 - value);
			}
			SetNormalizedPosition(1 - value, 1);

			float zeroIndexInUse = Mathf.Max(0, zeroIndex - 1);
			float endIndexInUse = Mathf.Min(zeroIndex + onScreenElements + 1, elementCount);

			toRemove.Clear();
			float extra = zeroIndexInUse % 1;
			float endIndexInt = endIndexInUse - extra;
			foreach (KeyValuePair<int, RectTransform> pair in boundInstances)
			{
				if (pair.Key >= zeroIndexInUse && pair.Key < endIndexInt)
					continue;

				toRemove.Add(pair.Key);
				InstancePool.Pool(prefab, pair.Value);
			}

			foreach (int i in toRemove)
				boundInstances.Remove(i);

			Selectable prev = null;
			int c = 0;
			for (float v = zeroIndexInUse; v < endIndexInUse; v++, c++)
			{
				int i = Mathf.FloorToInt(v);
				if (!boundInstances.TryGetValue(i, out var instance))
				{
					boundInstances.Add(i, instance = InstancePool.Get(prefab, content, Vector3.zero, Quaternion.identity, Space.Self));
					BindItem.Invoke(i, instance);
				}

				instance.SetSiblingIndex(c + 1);

				//Automatic navigation setup
				if (!instance.TryGetComponent<Selectable>(out var next))
					continue;
				next.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = prev
				};
				if (prev != null)
				{
					Navigation prevNav = prev.navigation;
					prevNav.selectOnDown = next;
					prev.navigation = prevNav;
				}

				prev = next;
			}

			startPaddingElement.transform.SetSiblingIndex(0);
			endPaddingElement.transform.SetSiblingIndex(content.childCount - 1);

			float startPadding = Mathf.Floor(zeroIndexInUse) * elementHeight;
			startPaddingElement.preferredHeight = startPadding;
			((RectTransform) startPaddingElement.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, startPadding);
			float endPadding = totalElementHeight - (startPadding + c * elementHeight);
			endPaddingElement.preferredHeight = endPadding;
			((RectTransform) endPaddingElement.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, endPadding);
		}
	}
}
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	public class InstancePoolDebugger : EditorWindow
	{
		[MenuItem("Window/Analysis/Instance Pool Debugger")]
		private static void Open()
		{
			var instancePoolDebugger = GetWindow<InstancePoolDebugger>();
			instancePoolDebugger.titleContent = new GUIContent("Instance Pool Debugger");
			instancePoolDebugger.Show();
		}

		private const string noElement = "None";

		private Type instancePoolType;
		private ListView listView;
		private readonly List<string> availableTypeNames = new List<string>
		{
			noElement
		};
		private readonly List<string> availableComponentNames = new List<string>
		{
			noElement
		};
		private readonly Dictionary<string, Component> namesToComponents = new Dictionary<string, Component>();

		[SerializeField] private string chosenPoolType;
		private IDictionary poolDictionary;
		private PopupField<string> componentPopup;
		private readonly List<Component> pooledComponents = new List<Component>();

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += ChangedPlayMode;

			var root = rootVisualElement;
			//Padding
			var padding = new VisualElement
			{
				style = { marginBottom = 10, marginLeft = 5, marginRight = 5, marginTop = 10 }, name = "Padding"
			};
			root.Add(padding);
			padding.StretchToParentSize();
			root = padding;

			root.Add(new Label("Instance Pool:") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

			//Type Popup
			PopupField<string> typePopup = new PopupField<string>("Type", availableTypeNames, 0);
			typePopup.RegisterCallback<MouseDownEvent, List<string>>((evt, typeNames) =>
			{
				typeNames.Clear();
				typeNames.Add(noElement);
				IEnumerable componentPools = (IEnumerable)InstancePools.GetValue(null);
				foreach (object componentPool in componentPools)
				{
					Type poolType = componentPool.GetType();
					var genericTypeArgument = poolType.GenericTypeArguments[0];
					typeNames.Add(genericTypeArgument.Name);
				}
			}, availableTypeNames);
			typePopup.RegisterValueChangedCallback(evt =>
			{
				availableComponentNames.Clear();
				availableComponentNames.Add(noElement);
				componentPopup.SetValueWithoutNotify(noElement);

				if (evt.newValue == noElement)
				{
					chosenPoolType = null;
					componentPopup.SetEnabled(false);
					return;
				}

				componentPopup.SetEnabled(true);
				chosenPoolType = evt.newValue;
			});
			root.Add(typePopup);

			//Component Popup
			componentPopup = new PopupField<string>("Component", availableComponentNames, 0);
			componentPopup.RegisterCallback<MouseDownEvent, InstancePoolDebugger>((evt, debugger) => debugger.RebuildComponentDropdown(), this);
			componentPopup.RegisterValueChangedCallback(evt =>
			{
				pooledComponents.Clear();
				if (!namesToComponents.TryGetValue(evt.newValue, out var component) || poolDictionary == null)
				{
					componentPopup.SetValueWithoutNotify(noElement);
					RebuildComponentDropdown();
#if UNITY_2021_2_OR_NEWER
					listView.Rebuild();
#else
					listView.Refresh();
#endif
					return;
				}

				IEnumerable set = (IEnumerable)poolDictionary[component];
				foreach (object o in set)
					pooledComponents.Add((Component)o);
#if UNITY_2021_2_OR_NEWER
				listView.Rebuild();
#else
				listView.Refresh();
#endif
			});
			componentPopup.SetEnabled(false);
			root.Add(componentPopup);

#if UNITY_2020_1_OR_NEWER
			HelpBox container = new HelpBox("Data is not refreshed in realtime.", HelpBoxMessageType.Warning);
			root.Add(container);
#else
			Label container = new Label("Data is not refreshed in realtime.");
			root.Add(container);
#endif

			//List View
			listView = new ListView(pooledComponents, (int)EditorGUIUtility.singleLineHeight, () =>
			{
				VisualElement element = new VisualElement();
				var objectField = new ObjectField { objectType = typeof(Component) };
				element.Add(objectField);
				objectField.Q(className: ObjectField.selectorUssClassName).style.display = DisplayStyle.None;
				objectField.RegisterValueChangedCallback(evt => objectField.SetValueWithoutNotify(evt.previousValue));
				return element;
			}, (element, i) =>
			{
				var objectField = element.Q<ObjectField>();
				objectField.SetValueWithoutNotify(pooledComponents[i]);
			})
			{
				style =
				{
					flexGrow = 1,
					backgroundColor = new Color(0, 0, 0, 0.15f),
					marginTop = 5
				}
			};
			root.Add(listView);

			rootVisualElement.SetEnabled(Application.isPlaying);
		}

		private void OnDisable() => EditorApplication.playModeStateChanged -= ChangedPlayMode;

		private void ChangedPlayMode(PlayModeStateChange obj) => rootVisualElement.SetEnabled(obj == PlayModeStateChange.EnteredPlayMode);

		private void Rebind()
		{
			if (poolDictionary == null)
			{
				chosenPoolType = null;
				pooledComponents.Clear();
#if UNITY_2021_2_OR_NEWER
				listView.Rebuild();
#else
				listView.Refresh();
#endif
				return;
			}

#if UNITY_2021_2_OR_NEWER
			listView.Rebuild();
#else
			listView.Refresh();
#endif
		}

		void RebuildComponentDropdown()
		{
			namesToComponents.Clear();
			availableComponentNames.Clear();
			availableComponentNames.Add(noElement);

			IEnumerable componentPools = (IEnumerable)InstancePools.GetValue(null);
			foreach (object componentPool in componentPools)
			{
				Type poolType = componentPool.GetType();
				var genericTypeArgument = poolType.GenericTypeArguments[0];
				if (genericTypeArgument.Name != chosenPoolType) continue;
				FieldInfo pool = componentPool.GetType().GetField("pool", BindingFlags.Instance | BindingFlags.NonPublic);
				poolDictionary = (IDictionary)pool.GetValue(componentPool);
				foreach (object key in poolDictionary.Keys)
				{
					Component component = (Component)key;
					string originalName = component.name;
					string componentName = originalName;
					int index = 1;
					while (namesToComponents.ContainsKey(componentName))
						componentName = $"{originalName} - {index++}";
					availableComponentNames.Add(componentName);
					namesToComponents.Add(componentName, component);
				}

				componentPopup.SetEnabled(true);
				return;
			}
		}

		#region Reflection

		private static FieldInfo instancePools;
		private static FieldInfo InstancePools => instancePools ?? (instancePools = typeof(InstancePool).GetField("instancePools", BindingFlags.NonPublic | BindingFlags.Static));

		#endregion
	}
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	public interface IAdvancedDropdownItem
	{
		string Name { get; }
		
		/// <summary>
		/// Paths do not contain the name. eg. "Folder/Sub Folder"
		/// </summary>
		string Path { get; }
	}

	public readonly struct AdvancedDropdownElement : IAdvancedDropdownItem
	{
		public string Name { get; }
		public string Path { get; }

		public Type Type { get; }

		public AdvancedDropdownElement(string name, string path, Type type = null)
		{
			Name = name;
			Path = path;
			Type = type;
		}

		public AdvancedDropdownElement(AdvancedDropdownAttribute attribute, Type type)
		{
			Name = attribute.Name;
			Path = attribute.Path;
			Type = type;
		}
	}

	public static class AdvancedDropdownUtils
	{
		private class AdvancedDropdownWithCallacks : AdvancedDropdown
		{
			private readonly Dictionary<int, AdvancedDropdownElement> lookup;
			private readonly Action<AdvancedDropdownElement> onSelected;
			private readonly AdvancedDropdownItem root;

			protected override AdvancedDropdownItem BuildRoot() => root;

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				if (lookup.TryGetValue(item.id, out var value))
					onSelected?.Invoke(value);
			}

			public AdvancedDropdownWithCallacks(
				AdvancedDropdownState state,
				string title,
				List<AdvancedDropdownElement> elements,
				Action<AdvancedDropdownElement> onSelected,
				Func<AdvancedDropdownElement, bool> validateEnabled = null
			) : base(state)
			{
				this.onSelected = onSelected;
				//Create lookup and build root
				(lookup, root) = GetStructure(elements, title, validateEnabled);
			}
		}

		private class AdvancedDropdownElement<T>
			where T : IAdvancedDropdownItem
		{
			public string Name { get; }
			public bool IsItem { get; }
			public T Item { get; }

			private Dictionary<string, AdvancedDropdownElement<T>> children;
			public Dictionary<string, AdvancedDropdownElement<T>> Children => children;

			public AdvancedDropdownElement(string name)
			{
				Name = name;
				IsItem = false;
			}

			public AdvancedDropdownElement(T item)
			{
				Name = item.Name;
				Item = item;
				IsItem = true;
			}

			public AdvancedDropdownElement<T> AddChild(string localPath, T child)
			{
				if (children == null)
					children = new Dictionary<string, AdvancedDropdownElement<T>>();

				//Add as a child if we have reached the bottom of the path.
				if (string.IsNullOrEmpty(localPath))
					return AddChildDirectly(child) ? this : null;

				int indexOfSeparator = localPath.IndexOfAny(separators);

				//----------------------------------------------------------

				string thisPathName;
				string nextPathName;
				if (indexOfSeparator == -1 || indexOfSeparator == localPath.Length - 1)
				{
					thisPathName = localPath;
					nextPathName = null;
				}
				else
				{
					thisPathName = localPath.Substring(0, indexOfSeparator);
					nextPathName = localPath.Substring(indexOfSeparator + 1);
				}

				if (!children.TryGetValue(thisPathName, out var nextChild))
					children.Add(thisPathName, nextChild = new AdvancedDropdownElement<T>(thisPathName));
				return nextChild.AddChild(nextPathName, child);
			}

			public bool AddChildDirectly(T child)
			{
				if (children.ContainsKey(child.Name))
				{
					Debug.LogError($"{child} key already exists in children ({children[child.Name]}).");
					return false;
				}

				children.Add(child.Name, new AdvancedDropdownElement<T>(child));
				return true;
			}
		}

		private static readonly char[] separators = {'/', '\\'};

		public static AdvancedDropdown CreateAdvancedDropdownFromAttribute<T>(
			string title,
			Action<AdvancedDropdownElement> onSelected,
			Func<AdvancedDropdownElement, bool> validateEnabled = null
		) where T : AdvancedDropdownAttribute
		{
			//Generate elements
			var types = TypeCache.GetTypesWithAttribute<T>();
			List<AdvancedDropdownElement> elements = new List<AdvancedDropdownElement>();
			foreach (Type type in types)
				elements.Add(new AdvancedDropdownElement(type.GetCustomAttribute<AdvancedDropdownAttribute>(), type));

			return new AdvancedDropdownWithCallacks(new AdvancedDropdownState(), title, elements, onSelected, validateEnabled);
		}
		
		public static AdvancedDropdown CreateAdvancedDropdownFromType<T>(
			string title,
			Action<AdvancedDropdownElement> onSelected,
			Func<AdvancedDropdownElement, bool> validateEnabled = null,
			bool excludeAbstractTypes = true
		)
		{
			//Generate elements
			var types = TypeCache.GetTypesDerivedFrom<T>();
			StringBuilder stringBuilder = new StringBuilder();
			List<AdvancedDropdownElement> elements = new List<AdvancedDropdownElement>();
			foreach (Type type in types)
			{
				var attribute = type.GetCustomAttribute<AdvancedDropdownAttribute>();
				if (excludeAbstractTypes && type.IsAbstract)
					continue;
				
				elements.Add(attribute != null
					? new AdvancedDropdownElement(attribute, type)
					: new AdvancedDropdownElement(type.Name, ObjectNames.NicifyVariableName(CodeUtils.GenerateTypeNamePath(type, stringBuilder, true, false)), type));
			}

			if (elements.Count > 0)
			{
				//==== Remove shared root from all paths ====
				
				//Calculate shared root
				int sharedStartingCharacter = -1;
				while (true)
				{
					int check = sharedStartingCharacter + 1;
					bool checkFailed = false;
					char sharedChar = default;
					for (var i = 0; i < elements.Count; i++)
					{
						var element = elements[i];
						//Exit if char doesn't exist
						if (element.Path.Length <= check)
						{
							checkFailed = true;
							break;
						}

						//Assign char if first index
						if (i == 0)
						{
							sharedChar = element.Path[check];
							continue;
						}

						//Continue checking if char is the same
						if (element.Path[check] == sharedChar)
							continue;
						
						//Fail if char is not the same.
						checkFailed = true;
						break;
					}

					if(checkFailed)
						break;
					sharedStartingCharacter++;
				}

				if (sharedStartingCharacter >= 0)
				{
					for (var i = 0; i < elements.Count; i++)
					{
						var element = elements[i];
						elements[i] = new AdvancedDropdownElement(element.Name, element.Path.Substring(sharedStartingCharacter + 1), element.Type);
					}
				}
			}

			return new AdvancedDropdownWithCallacks(new AdvancedDropdownState(), title, elements, onSelected, validateEnabled);
		}

		public static (Dictionary<int, T>, AdvancedDropdownItem) GetStructure<T>(IEnumerable<T> items, string rootName, Func<T, bool> validateEnabled = null)
			where T : IAdvancedDropdownItem
		{
			AdvancedDropdownElement<T> rootElement = GenerateItems(items, rootName);
			AdvancedDropdownItem root = ConvertToItems(rootElement, out var lookup, validateEnabled);
			return (lookup, root);
		}

		private static AdvancedDropdownElement<T> GenerateItems<T>(IEnumerable<T> items, string rootName)
			where T : IAdvancedDropdownItem
		{
			//Collect all the items into their respective paths.
			Dictionary<string, List<T>> pathsToItems = new Dictionary<string, List<T>>();

			foreach (T item in items)
			{
				if (!pathsToItems.TryGetValue(item.Path, out List<T> list))
				{
					list = new List<T>();
					pathsToItems.Add(item.Path, list);
				}

				list.Add(item);
			}

			AdvancedDropdownElement<T> root = new AdvancedDropdownElement<T>(rootName);
			foreach (var pathToItems in pathsToItems)
			{
				AdvancedDropdownElement<T> current = null;
				for (int i = 0; i < pathToItems.Value.Count; i++)
				{
					if (i == 0 || current == null)
						current = root.AddChild(pathToItems.Key, pathToItems.Value[i]);
					else
						current.AddChildDirectly(pathToItems.Value[i]);
				}
			}

			return root;
		}

		private static AdvancedDropdownItem ConvertToItems<T>(AdvancedDropdownElement<T> rootElement, out Dictionary<int, T> lookup, Func<T, bool> enabledFunc)
			where T : IAdvancedDropdownItem
		{
			lookup = new Dictionary<int, T>();
			AdvancedDropdownItem root = new AdvancedDropdownItem(rootElement.Name);

			AddChildren(root, rootElement, lookup);

			void AddChildren(AdvancedDropdownItem toTarget, AdvancedDropdownElement<T> toGather, Dictionary<int, T> localLookup)
			{
				foreach (KeyValuePair<string, AdvancedDropdownElement<T>> children in toGather.Children)
				{
					AdvancedDropdownElement<T> element = children.Value;
					if (TryAddEndChild(element))
						continue;
					AdvancedDropdownItem child;
					toTarget.AddChild(child = new AdvancedDropdownItem(element.Name));
					AddChildren(child, element, localLookup);
				}

				bool TryAddEndChild(AdvancedDropdownElement<T> item)
				{
					if (item.Children != null)
						return false;
					if (!item.IsItem)
						return false;
					AdvancedDropdownItem child;
					toTarget.AddChild(child = new AdvancedDropdownItem(item.Name)
					{
						id = $"{item.Item.Path}/{item.Item.Name}".GetHashCode(),
						enabled = enabledFunc?.Invoke(item.Item) ?? true
					});
					localLookup.Add(child.id, item.Item);
					return true;
				}
			}

			return root;
		}
	}
}
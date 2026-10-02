using System;
using System.Collections.Generic;
using System.Linq;

namespace sh.be.shared.Utils
{
    public static class TreeUtils
    {
        public static List<T> BuildTree<T, TKey>(
            IEnumerable<T> items,
            Func<T, TKey> keySelector,
            Func<T, TKey?> parentKeySelector,
            Func<T, List<T>> childrenSelector) where TKey : notnull
        {
            var nodes = items.ToList();
            var nodeByKey = nodes.ToDictionary(keySelector);
            var roots = new List<T>();

            foreach (var node in nodes)
            {
                var parentKey = parentKeySelector(node);
                if (parentKey != null && nodeByKey.TryGetValue(parentKey, out var parent))
                {
                    childrenSelector(parent).Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }
            return roots;
        }

        public static IEnumerable<T> Flatten<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> childrenSelector)
        {
            var stack = new Stack<T>(roots);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;
                foreach (var child in childrenSelector(node))
                {
                    stack.Push(child);
                }
            }
        }

        public static void SyncChecked<T>(
            IEnumerable<T> nodes,
            Func<T, IEnumerable<T>> childrenSelector,
            Func<T, bool> isCheckedSelector,
            Action<T, bool> setChecked,
            bool isParentChecked = false)
        {
            foreach (var node in nodes)
            {
                if (isParentChecked)
                {
                    setChecked(node, true);
                }

                var children = childrenSelector(node).ToList();
                SyncChecked(children, childrenSelector, isCheckedSelector, setChecked, isCheckedSelector(node));

                if (children.Count > 0 && children.All(isCheckedSelector))
                {
                    setChecked(node, true);
                }
            }
        }
    }
}

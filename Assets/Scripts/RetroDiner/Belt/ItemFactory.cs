using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Data;

namespace RetroDiner.Belt
{
    /// <summary>Pooled creation of BeltItems. Visual choice lives in BeltItem; this only manages lifetime.</summary>
    public class ItemFactory
    {
        readonly Transform parent;
        readonly Dictionary<string, Stack<BeltItem>> pool = new Dictionary<string, Stack<BeltItem>>();
        readonly float itemSize;
        readonly Quaternion billboard;

        public ItemFactory(Transform parent, float itemSize, Quaternion billboard)
        {
            this.parent = parent;
            this.itemSize = itemSize;
            this.billboard = billboard;
        }

        public BeltItem Spawn(ItemDefinition def)
        {
            Stack<BeltItem> stack;
            BeltItem item = null;
            if (pool.TryGetValue(def.id, out stack) && stack.Count > 0) item = stack.Pop();
            if (item == null)
            {
                var go = new GameObject("Item");
                go.transform.SetParent(parent, false);
                go.AddComponent<SphereCollider>();
                item = go.AddComponent<BeltItem>();
                item.Init(def, itemSize, billboard);
            }
            else
            {
                item.gameObject.SetActive(true);
                item.Init(def, itemSize, billboard);
            }
            return item;
        }

        public void Release(BeltItem item)
        {
            if (item == null) return;
            item.ResetForPool();
            item.gameObject.SetActive(false);
            string id = item.Definition != null ? item.Definition.id : "?";
            Stack<BeltItem> stack;
            if (!pool.TryGetValue(id, out stack)) { stack = new Stack<BeltItem>(); pool[id] = stack; }
            stack.Push(item);
        }

        public void Clear()
        {
            foreach (var kv in pool) foreach (var it in kv.Value) if (it != null) Object.Destroy(it.gameObject);
            pool.Clear();
        }
    }
}
